using UnityEngine;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using System.Runtime.InteropServices;
using UnityEngine.Events;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

using RosMessageTypes.Std; //Ajouté le 19/03 à 20h53


public class HapticForce4 : MonoBehaviour
{

    [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);

    //[DllImport("HapticsDirect")] public static extern void setForce(string configName, double[] lateral3, double[] torque3); // Ajouté le 19/03 à 09h24


    public HapticPlugin hapticPlugin;
    //private string forceTopicName = "/tcp_force";
    // Commenté le 02/03 à 22h - remplacé par /haptic_force (forces transformées tool0 → base par haptic_control.cpp)
    //private string forceTopicName = "/force_torque_sensor_broadcaster/wrench";
    // Ajouté le 02/03 à 22h
    private string forceTopicName = "/haptic_force";
    private double MaxForce;
    
    //Ajouté 19/03 à 20h57
    public GameObject virtualContactPlane;
    public float contactForceThreshold = 5.0f;
    private float currentStiffness = 0.2f;
    private Vector3 filteredForce = Vector3.zero;

    // Ajouté le 02/03 à 22h - variable pour le filtre passe-bas
    //private Vector3 filteredForce = Vector3.zero;

    // Ajouté le 02/03 à 22h - facteur d'échelle de la force (à ajuster selon confort)
    public float scalingFactor = 0.025f;

    void Start()
    {
        // Vérifier que la référence à HapticPlugin est définie
        if (hapticPlugin == null)
        {
            hapticPlugin = GetComponent<HapticPlugin>();
            if (hapticPlugin == null)
            {
                Debug.LogError("HapticForceFromROS : Aucun composant HapticPlugin trouvé. Veuillez assigner une référence dans l'inspecteur.");
                return;
            }
        }
        else
        {
            MaxForce = hapticPlugin.MaxForce;
            //MaxForce = 10.0;
            //MaxForce = 1.0; //Ajouté le 27/02 à 16h20
        }
        // S'inscrire au topic ROS2
        ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);

        //Ajotué le 19/03 à 20h57
        ROSConnection.GetOrCreateInstance().Subscribe<Float64Msg>("/haptic_stiffness", ReceiveStiffness);
    }


    void OnDestroy()
    {
        // Appliquer une force nulle avant la destruction
        if (hapticPlugin != null)
        {
            double[] forceArray = new double[3] { 0, 0, 0 };
            double[] torqueArray = new double[3] { 0, 0, 0 };
            HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
        }
    }


    private void ReceiveForce(WrenchStampedMsg forceMsg)
    {
        // Appliquer la force au dispositif haptique
        if (hapticPlugin != null && hapticPlugin.DeviceIdentifier != null)
        {
            Vector3 force = new Vector3(
               -(float)forceMsg.wrench.force.y,
               (float)forceMsg.wrench.force.z,
               (float)forceMsg.wrench.force.x
           );
            Debug.Log($"Force importée : x={force.x}, y={force.y}, z={force.z}");


            // Ajouté le 02/03 à 22h - filtre passe-bas pour éliminer le bruit et les forces parasites
            // 80% de la force précédente + 20% de la nouvelle = lissage des vibrations
            //filteredForce = 0.8f * filteredForce + 0.2f * force; // Modifié le 03/03 à 14h30
            //filtre plus agressif pour atténuer les forces d'inertie pendant le mouvement
            float alpha = 0.98f; // à ajuster
            filteredForce = alpha * filteredForce + (1 - alpha) * force;
            force = filteredForce;

            // Ajouté le 02/03 à 22h - scaling de la force (réduction de l'intensité ressentie)
            // scalingFactor réglable depuis l'inspecteur Unity
            force = force * scalingFactor;

            // Vector3 clampedForce = Vector3.ClampMagnitude(force, (float)MaxForce);

            // // Commenté le 09/03 à 13h07
            // // Debug.Log($"Force importée : x={clampedForce.x}, y={clampedForce.y}, z={clampedForce.z}");

            // Vector3 direction = clampedForce.normalized;
            // double ForceMag = clampedForce.magnitude;

            // double[] ForceDir = new double[] { direction.x, direction.y, direction.z };
            // setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, ForceMag);

            // Clamp progressif (soft clamp)
            float softMax = (float)MaxForce;
            float magnitude = force.magnitude;
            if (magnitude > softMax)
                force = force.normalized * (softMax + (float)System.Math.Tanh(magnitude - softMax));

            // Seuil de bruit
            float noiseThreshold = 0.05f; // à ajuster
            if (force.magnitude < noiseThreshold)
                force = Vector3.zero;


            // Debug
            Debug.Log($"Force appliquée : {force}");

            //Ajouté le 19/03 à 21h00
            //float magnitude = force.magnitude;
            if (magnitude > contactForceThreshold)
            {
                if (virtualContactPlane != null)
                {
                    if (!virtualContactPlane.activeSelf)
                    {
                        float offset = 0.01f;
                        virtualContactPlane.transform.position = 
                            hapticPlugin.CollisionMesh.transform.position + force.normalized * offset;
                        virtualContactPlane.transform.rotation = 
                            Quaternion.FromToRotation(Vector3.up, force.normalized);
                        virtualContactPlane.SetActive(true);
                    }
                    HapticMaterial hapMat = virtualContactPlane.GetComponent<HapticMaterial>();
                    if (hapMat != null)
                        hapMat.hStiffness = currentStiffness;
                }
            }
            else
            {
                if (virtualContactPlane != null)
                    virtualContactPlane.SetActive(false);
            }

            //Commenté le 19/03 à 09h24
            Vector3 direction = force.normalized;
            double ForceMag = force.magnitude;
            double[] ForceDir = new double[] { direction.x, direction.y, direction.z };
            setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, ForceMag);

            //Ajouté le 19/03 à 09h24 // Commenté à 10h01
            // double[] forceArray = new double[3] { force.x, force.y, force.z };
            // double[] torqueArray = new double[3] { 0, 0, 0 };
            // setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);

        }
    }

    //Ajouté le 19/03 à 20h58
    private void ReceiveStiffness(Float64Msg msg)
    {
        currentStiffness = Mathf.Clamp((float)msg.data * 10.0f, 0.01f, 0.9f);
    }
}