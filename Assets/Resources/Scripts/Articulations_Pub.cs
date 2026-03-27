using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine;

public class ArticulationsPub : MonoBehaviour
{
    public ArticulationBody[] jointArticulationBodies;

    private JointStateMsg msg = new JointStateMsg();
    private float[] jointStates = new float[] { -3.14f, -1.57f, 0f, -1.57f, 0f, 0f };
    public float publishMessageFrequency = 0.008f;
    private float timeElapsed;
    // Start is called before the first frame update
    void Start()
    {
        // Start the ROS connection
        ROSConnection ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<JointStateMsg>("joint_states_pub");

        // Initialize joint states
        for (int i = 0; i < jointArticulationBodies.Length; i++)
        {
            var joint = jointArticulationBodies[i];
            float angle = jointStates[i];
            var drive = joint.xDrive;
            drive.target = angle * Mathf.Rad2Deg;
            joint.xDrive = drive;
        }
        
    }

    // Update is called once per frame
    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {
            timeElapsed = 0f; // Reset the timer

            msg.name = new string[jointArticulationBodies.Length];
            msg.position = new double[jointArticulationBodies.Length];

            for (int i = 0; i < jointArticulationBodies.Length; i++)
            {
                var joint = jointArticulationBodies[i];
                msg.name[i] = joint.name;
                if (i == 0)
                {
                    // Adjust the first joint's target angle
                    msg.position[i] = (joint.xDrive.target - 90f) * Mathf.Deg2Rad;
                }
                else
                {
                    msg.position[i] = joint.xDrive.target * Mathf.Deg2Rad;
                }
                
            }

            ROSConnection.GetOrCreateInstance().Publish("joint_states_pub", msg);
        }
        
    }
}
