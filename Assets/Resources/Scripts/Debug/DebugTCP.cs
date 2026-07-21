// using UnityEngine;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;
// using RosMessageTypes.Geometry;

// public class DebugTCP : MonoBehaviour
// {
//     void Update()
//     {
//         PointMsg rosPosition = transform.position.To<FLU>();
//         QuaternionMsg rosRotation = transform.rotation.To<FLU>();
//         Debug.Log($"Unity: x={transform.position.x:F3}, y={transform.position.y:F3}, z={transform.position.z:F3}");
//         Debug.Log($"Unity rotation: x={transform.rotation.x:F3}, y={transform.rotation.y:F3}, z={transform.rotation.z:F3}, w={transform.rotation.w:F3}");
//         Debug.Log($"Position: x={rosPosition.x:F3}, y={rosPosition.y:F3}, z={rosPosition.z:F3}");
//         Debug.Log($"Rotation: x={rosRotation.x:F3}, y={rosRotation.y:F3}, z={rosRotation.z:F3}, w={rosRotation.w:F3}");
//     }
// }

//Ajouté le 14/04 à 11h50 -- pour afficher pose TCP dans repère unity = repère base robot

using UnityEngine;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;

public class DebugTCP : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PointMsg rosPosition = transform.position.To<FLU>();
            Debug.Log($"Unity global: x={transform.position.x:F3}, y={transform.position.y:F3}, z={transform.position.z:F3}");
            Debug.Log($"ROS position: x={rosPosition.x:F3}, y={rosPosition.y:F3}, z={rosPosition.z:F3}");
        }
    }
}

//Ajouté le 14/04 à 13h15 -- pour afficher pose TCP dans repère base robot (Unity non aligné avec robot)
// using UnityEngine;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;
// using RosMessageTypes.Geometry;

// public class DebugTCP : MonoBehaviour
// {
//     public GameObject robotBase; // drag base_link ici dans l'inspector

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             Vector3 localPosition = robotBase.transform.InverseTransformPoint(transform.position);
//             PointMsg rosPosition = localPosition.To<FLU>();
//             Debug.Log($"Unity local (base_link): x={localPosition.x:F3}, y={localPosition.y:F3}, z={localPosition.z:F3}");
//             Debug.Log($"ROS position: x={rosPosition.x:F3}, y={rosPosition.y:F3}, z={rosPosition.z:F3}");
//         }
//     }
// }