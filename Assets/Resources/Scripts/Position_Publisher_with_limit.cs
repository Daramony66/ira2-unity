using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class Position_Publisher_with_limit : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "unity_position";
    public GameObject cube;
    public float publishMessageFrequency = 2f;
    private float timeElapsed;

    private float maxDistance = 0.8f;
    private float minDistance = 0.2f;
    // Start is called before the first frame update
    void Start()
    {
        // Start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseMsg>(topicName);
        ros.RegisterPublisher<BoolMsg>("limit_out");

    }

    // Update is called once per frame
    private void Update()
    {
        timeElapsed += Time.deltaTime;


        if (timeElapsed > publishMessageFrequency)
        {
            float distance = cube.transform.position.magnitude;

            if (distance > maxDistance || distance < minDistance || cube.transform.position.y < 0)
            {
                //Debug.LogWarning("Cube is out of bounds! Current distance: " + distance);
                cube.GetComponent<Renderer>().material.color = Color.red; // Change color to red if out of bounds

                var boolMsg = new BoolMsg { data = true };
                ros.Publish("limit_out", boolMsg);
                return; // Skip publishing if the cube is out of bounds

            }
            else
            {
                cube.GetComponent<Renderer>().material.color = Color.blue; // Reset color to blue if within bounds

                Vector3 cubePosition = cube.transform.position;
                Quaternion cubeRotation = cube.transform.rotation;

                PoseMsg cubePos = new PoseMsg
                {
                    position = cubePosition.To<FLU>(),
                    orientation = cubeRotation.To<FLU>()
                };

                ros.Publish(topicName, cubePos);

                var boolMsg = new BoolMsg { data = false };
                ros.Publish("limit_out", boolMsg);
            }
            timeElapsed = 0;
        }
    }
}