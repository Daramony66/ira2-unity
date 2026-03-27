using UnityEngine;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using System.Runtime.InteropServices;
using UnityEngine.Events;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class HapticForce : MonoBehaviour
{

    [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);


    public HapticPlugin hapticPlugin;
    private string forceTopicName = "/tcp_force";
    private double MaxForce;

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
        }
        // S'inscrire au topic ROS2
        ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
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
            Vector3 clampedForce = Vector3.ClampMagnitude(force, (float)MaxForce);
            Debug.Log($"Force importée : x={clampedForce.x}, y={clampedForce.y}, z={clampedForce.z}");

            Vector3 direction = clampedForce.normalized;
            double ForceMag = clampedForce.magnitude;

            double[] ForceDir = new double[] { direction.x, direction.y, direction.z };
            setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, ForceMag);

        }
    }
}