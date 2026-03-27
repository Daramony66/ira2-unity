using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class Position_Publisher : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "cube_position";
    public GameObject cube;
    public float publishMessageFrequency = 2f;
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        // Start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PointMsg>(topicName);

    }

    // Update is called once per frame
    private void Update()
    {
        timeElapsed += Time.deltaTime;


        if (timeElapsed > publishMessageFrequency)
        {

            Vector3 cubePosition = cube.transform.position;

            // Unity -> ROS (FLU)
            PointMsg rosPosition = cubePosition.To<FLU>();

            ros.Publish(topicName, rosPosition);


            timeElapsed = 0;
        }
    }
}