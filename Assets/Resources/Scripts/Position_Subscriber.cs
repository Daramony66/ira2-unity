using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class Position_Subscriber : MonoBehaviour
{
    public GameObject cube;
    void Start()
    {
        // Initialize ROS connection and subscribe to the position topic
        ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Geometry.PoseStampedMsg>("tcp_pose_broadcaster/pose", OnPositionReceived);
        
    }
    void OnPositionReceived(PoseStampedMsg positionMessage)
    {
        Vector3 unityPosition = new PointMsg(
            (float)positionMessage.pose.position.x,
            (float)positionMessage.pose.position.y,
            (float)positionMessage.pose.position.z
        ).From<FLU>();

        QuaternionMsg rosRotation = new QuaternionMsg(
            (float)positionMessage.pose.orientation.x,
            (float)positionMessage.pose.orientation.y,
            (float)positionMessage.pose.orientation.z,
            (float)positionMessage.pose.orientation.w
        );

        Quaternion unityRotation = rosRotation.From<FLU>();

        cube.transform.position = unityPosition;
        cube.transform.rotation = unityRotation;
    }

}
