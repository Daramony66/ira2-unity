using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine;

public class URJointSubscriber : MonoBehaviour
{
    public ArticulationBody[] jointArticulationBodies;
    ROSConnection ros;
    private bool firstReceived = false;
    void Start()
    {
        //Debug.Log($"[JOINTS] Start() a t={Time.timeSinceLevelLoad:F2}s"); //Ajouté le 21/07
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>("joint_states", OnJointStatesReceived);
    }

    void OnJointStatesReceived(JointStateMsg msg)
    {
        if (!firstReceived) { firstReceived = true; Debug.Log($"[JOINTS] premier message a t={Time.timeSinceLevelLoad:F2}s"); }

        for (int i = 0; i < msg.position.Length && i < jointArticulationBodies.Length; ++i)
        {
            var joint = jointArticulationBodies[i];
            float angle = (float)msg.position[i];
            var drive = joint.xDrive;
            drive.target = angle * Mathf.Rad2Deg;
            if (i == 2)
            {
                drive.target += 180f;
            }

            joint.xDrive = drive;
        }
    }
}

