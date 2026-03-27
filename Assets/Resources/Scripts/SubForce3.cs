// using UnityEngine;
// using RosMessageTypes.Geometry;
// using Unity.Robotics.ROSTCPConnector;
// using System.Runtime.InteropServices;
// using UnityEngine.Events;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;

// //Ajouté le 18/03 à 17h30
// using System;

// public class HapticForce3 : MonoBehaviour
// {

//     [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);


//     public HapticPlugin hapticPlugin;
//     //private string forceTopicName = "/tcp_force";
//     // Commenté le 02/03 à 22h - remplacé par /haptic_force (forces transformées tool0 → base par haptic_control.cpp)
//     //private string forceTopicName = "/force_torque_sensor_broadcaster/wrench";
//     // Ajouté le 02/03 à 22h
//     private string forceTopicName = "/haptic_force";
//     private double MaxForce;

//     // Ajouté le 02/03 à 22h - variable pour le filtre passe-bas
//     private Vector3 filteredForce = Vector3.zero;

//     // Ajouté le 02/03 à 22h - facteur d'échelle de la force (à ajuster selon confort)
//     public float scalingFactor = 0.3f;

//     void Start()
//     {
//         // Vérifier que la référence à HapticPlugin est définie
//         if (hapticPlugin == null)
//         {
//             hapticPlugin = GetComponent<HapticPlugin>();
//             if (hapticPlugin == null)
//             {
//                 Debug.LogError("HapticForceFromROS : Aucun composant HapticPlugin trouvé. Veuillez assigner une référence dans l'inspecteur.");
//                 return;
//             }
//         }
//         else
//         {
//             MaxForce = hapticPlugin.MaxForce;
//             //MaxForce = 10.0;
//             //MaxForce = 1.0; //Ajouté le 27/02 à 16h20
//         }
//         // S'inscrire au topic ROS2
//         ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
//     }


//     void OnDestroy()
//     {
//         // Appliquer une force nulle avant la destruction
//         if (hapticPlugin != null)
//         {
//             double[] forceArray = new double[3] { 0, 0, 0 };
//             double[] torqueArray = new double[3] { 0, 0, 0 };
//             HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
//         }
//     }

//     //Bloc : Ajouté le 18/03 à 17h30

//     private Vector3 contactAnchor = Vector3.zero;
//     private bool inContact = false;

//     private void ReceiveForce(WrenchStampedMsg forceMsg)
//     {
//         if (hapticPlugin == null || hapticPlugin.DeviceIdentifier == null) return;

//         float forceMagnitude = (float)Math.Sqrt(
//             forceMsg.wrench.force.x * forceMsg.wrench.force.x +
//             forceMsg.wrench.force.y * forceMsg.wrench.force.y +
//             forceMsg.wrench.force.z * forceMsg.wrench.force.z);

//         float threshold = 2.0f; // seuil en N pour détecter le contact

//         if (forceMagnitude > threshold) {
//             if (!inContact) {
//                 // Premier contact : on enregistre la position du stylus comme ancre
//                 contactAnchor = hapticPlugin.CurrentPosition;
//                 inContact = true;
//             }
//             // Ressort vers l'ancre — sensation de mur
//             double[] anchor = new double[] { contactAnchor.x, contactAnchor.y, contactAnchor.z };
//             HapticPlugin.setSpringValues(hapticPlugin.DeviceIdentifier, anchor, scalingFactor);
//         } else {
//             if (inContact) {
//                 // Plus de contact : on désactive le ressort
//                 double[] anchor = new double[] { 0, 0, 0 };
//                 HapticPlugin.setSpringValues(hapticPlugin.DeviceIdentifier, anchor, 0.0);
//                 inContact = false;
//             }
//         }
//     }
// }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// using UnityEngine;
// using RosMessageTypes.Geometry;
// using Unity.Robotics.ROSTCPConnector;
// using System.Runtime.InteropServices;
// using UnityEngine.Events;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;
// using System;

// public class HapticForce3 : MonoBehaviour
// {
//     [DllImport("HapticsDirect")] public static extern void setSpringValues(string configName, double[] anchor, double stiffness);

//     public HapticPlugin hapticPlugin;
//     private string forceTopicName = "/haptic_force";
//     private double MaxForce;
//     public float springStiffness = 0.5f; // Ajuste la raideur du mur

//     private Vector3 contactAnchor = Vector3.zero;
//     private bool inContact = false;

//     void Start()
//     {
//         if (hapticPlugin == null)
//         {
//             hapticPlugin = GetComponent<HapticPlugin>();
//             if (hapticPlugin == null)
//             {
//                 Debug.LogError("HapticForce3 : Aucun composant HapticPlugin trouvé. Veuillez assigner une référence dans l'inspecteur.");
//                 return;
//             }
//         }
//         MaxForce = hapticPlugin.MaxForce;
//         ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
//     }

//     void OnDestroy()
//     {
//         if (hapticPlugin != null)
//         {
//             double[] anchor = new double[3] { 0, 0, 0 };
//             setSpringValues(hapticPlugin.DeviceIdentifier, anchor, 0.0);
//         }
//     }

//     private void ReceiveForce(WrenchStampedMsg forceMsg)
//     {
//         if (hapticPlugin == null || hapticPlugin.DeviceIdentifier == null) return;

//         // On ne prend que la composante normale (verticale) pour le contact avec la table
//         float normalForce = (float)forceMsg.wrench.force.z;
//         float threshold = 2.0f; // seuil en N pour détecter le contact

//         if (normalForce < -threshold)
//         {
//             if (!inContact)
//             {
//                 contactAnchor = hapticPlugin.CurrentPosition;
//                 inContact = true;
//             }
//             double[] anchor = new double[] { contactAnchor.x, contactAnchor.y, contactAnchor.z };
//             setSpringValues(hapticPlugin.DeviceIdentifier, anchor, springStiffness);
//         }
//         else
//         {
//             if (inContact)
//             {
//                 double[] anchor = new double[] { 0, 0, 0 };
//                 setSpringValues(hapticPlugin.DeviceIdentifier, anchor, 0.0);
//                 inContact = false;
//             }
//         }
//     }
// }


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using System.Runtime.InteropServices;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class HapticForce3 : MonoBehaviour
{
    public HapticPlugin hapticPlugin;
    public GameObject virtualContactPlane; // Assigner dans l'Inspector

    private string forceTopicName = "/haptic_force";
    private double MaxForce;
    private Vector3 filteredForce = Vector3.zero;
    public float scalingFactor = 0.025f;
    public float contactForceThreshold = 1.0f; // seuil en N pour détecter le contact

    void Start()
    {
        if (hapticPlugin == null)
        {
            hapticPlugin = GetComponent<HapticPlugin>();
            if (hapticPlugin == null)
            {
                Debug.LogError("HapticForce2 : Aucun HapticPlugin trouvé.");
                return;
            }
        }
        else
        {
            MaxForce = hapticPlugin.MaxForce;
        }

        ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
    }

    void OnDestroy()
    {
        if (hapticPlugin != null)
        {
            double[] forceArray = new double[3] { 0, 0, 0 };
            double[] torqueArray = new double[3] { 0, 0, 0 };
            HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
        }
        if (virtualContactPlane != null)
            virtualContactPlane.SetActive(false);
    }

    private void ReceiveForce(WrenchStampedMsg forceMsg)
    {
        if (hapticPlugin == null || hapticPlugin.DeviceIdentifier == null)
            return;

        // Récupérer la force dans le repère base
        Vector3 force = new Vector3(
            -(float)forceMsg.wrench.force.y,
             (float)forceMsg.wrench.force.z,
             (float)forceMsg.wrench.force.x
        );

        // Filtre passe-bas
        float alpha = 0.5f;
        filteredForce = alpha * filteredForce + (1 - alpha) * force;
        force = filteredForce;

        float magnitude = force.magnitude;
        Debug.Log($"Force magnitude : {magnitude}");


        //Commenté le 19/03 à 12h15
        // if (magnitude > contactForceThreshold)
        // {
        //     // Activer le plan virtuel
        //     if (virtualContactPlane != null)
        //     {
        //         virtualContactPlane.SetActive(true);

        //         // Positionner le plan au niveau du stylet
        //         virtualContactPlane.transform.position = hapticPlugin.CollisionMesh.transform.position;

        //         // Orienter le plan perpendiculairement à la force
        //         Vector3 forceDir = force.normalized;
        //         virtualContactPlane.transform.rotation = Quaternion.FromToRotation(Vector3.up, forceDir);

        //         // Mettre à jour la stiffness proportionnellement à la force
        //         HapticMaterial hapMat = virtualContactPlane.GetComponent<HapticMaterial>();
        //         if (hapMat != null)
        //         {
        //             hapMat.hStiffness = Mathf.Clamp(magnitude * scalingFactor, 0.01f, 0.8f);
        //         }
        //     }
        // }

        //Ajouté le 19/03 à 12h15
        if (magnitude > contactForceThreshold)
        {
            //Commenté le 19/03 à 12h24
            // if (!virtualContactPlane.activeSelf)
            // {
            //     // Positionner seulement à l'activation
            //     virtualContactPlane.transform.position = hapticPlugin.CollisionMesh.transform.position;
            //     virtualContactPlane.transform.rotation = Quaternion.FromToRotation(Vector3.up, force.normalized);
            //     virtualContactPlane.SetActive(true);
            // }
            // // Ne plus bouger le plan ensuite

            //Ajouté le 19/03 à 12h24
            if (!virtualContactPlane.activeSelf)
            {
                float offset = 0.01f;
                virtualContactPlane.transform.position = 
                    hapticPlugin.CollisionMesh.transform.position + force.normalized * offset;
                virtualContactPlane.transform.rotation = Quaternion.FromToRotation(Vector3.up, force.normalized);
                virtualContactPlane.SetActive(true);
            }
        }

        else
        {
            // Désactiver le plan quand plus de contact
            if (virtualContactPlane != null)
                virtualContactPlane.SetActive(false);
        }
    }
}