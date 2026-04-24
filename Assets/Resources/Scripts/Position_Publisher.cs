// using UnityEngine;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Geometry;
// using RosMessageTypes.Std;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;


// public class Position_Publisher : MonoBehaviour
// {
//     ROSConnection ros;
//     public string topicName = "cube_position";
//     public GameObject cube;
//     public float publishMessageFrequency = 2f;
//     private float timeElapsed;

//     // Start is called before the first frame update
//     void Start()
//     {
//         // Start the ROS connection
//         ros = ROSConnection.GetOrCreateInstance();
//         ros.RegisterPublisher<PointMsg>(topicName);

//     }

//     // Update is called once per frame
//     private void Update()
//     {
//         timeElapsed += Time.deltaTime;


//         if (timeElapsed > publishMessageFrequency)
//         {

//             Vector3 cubePosition = cube.transform.position;

//             // Unity -> ROS (FLU)
//             PointMsg rosPosition = cubePosition.To<FLU>();

//             ros.Publish(topicName, rosPosition);


//             timeElapsed = 0;
//         }
//     }
// }


// Ajouté le 13/04 à 17h55 -- pour repère Unity aligné avec robot directement

// using UnityEngine;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Geometry;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;

// public class Position_Publisher : MonoBehaviour
// {
//     ROSConnection ros;
//     public string topicName = "cube_position";
//     public GameObject cube;
//     public bool sendPosition = false;  // case à cocher dans l'inspector
//     private bool sent = false;

//     void Start()
//     {
//         ros = ROSConnection.GetOrCreateInstance();
//         ros.RegisterPublisher<PointMsg>(topicName);
//     }

//     void Update()
//     {
//         if (sendPosition && !sent)
//         {
//             Vector3 cubePosition = cube.transform.position;
//             PointMsg rosPosition = cubePosition.To<FLU>();
//             ros.Publish(topicName, rosPosition);
//             sent = true;
//             sendPosition = false;
//             Debug.Log($"Position envoyée : {cubePosition}");
//         }
        
//         // Reset sent quand sendPosition est décoché
//         if (!sendPosition)
//         {
//             sent = false;
//         }
//     }
// }



// // Ajouté le 14/04 à 13h15 -- pour repère Unity NON aligné avec robot

// using UnityEngine;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Geometry;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;

// public class Position_Publisher : MonoBehaviour
// {
//     ROSConnection ros;
//     public string topicName = "cube_position";
//     public GameObject cube;
//     public GameObject robotBase; // drag base_link ici dans l'inspector
//     public bool sendPosition = false;
//     private bool sent = false;

//     public bool startMove = false; //Ajouté le 17/04 à 16h00
//     private bool startMoveSent = false; //Ajouté le 17/04 à 16h00
//     public string startMoveTopicName = "/start_move"; //Ajouté le 17/04 à 16h00

//     void Start()
//     {
//         ros = ROSConnection.GetOrCreateInstance();
//         ros.RegisterPublisher<PointMsg>(topicName);
//         ros.Subscribe<RosMessageTypes.Std.StringMsg>("/masters/command", OnMastersCommand); //Ajouté le 17/04 à 15h30
//         ros.RegisterPublisher<RosMessageTypes.Std.StringMsg>(startMoveTopicName); //Ajouté le 17/04 à 15h30
//     }

//     void Update()
//     {
//         if (sendPosition && !sent)
//         {
//             // Position du cube dans le repère local de base_link
//             Vector3 cubeLocalPosition = robotBase.transform.InverseTransformPoint(cube.transform.position);
//             PointMsg rosPosition = cubeLocalPosition.To<FLU>();
//             ros.Publish(topicName, rosPosition);
//             sent = true;
//             sendPosition = false;
//             Debug.Log($"Position locale envoyée : {cubeLocalPosition}");
//         }

//         // Reset sent quand sendPosition est décoché
//         if (!sendPosition)
//         {
//             sent = false;
//         }

//         if (startMove && !startMoveSent)
//             {
//                 var msg = new RosMessageTypes.Std.StringMsg("go");
//                 ros.Publish(startMoveTopicName, msg);
//                 startMoveSent = true;
//                 startMove = false;
//                 Debug.Log("Signal /start_move envoyé.");
//             }

//             if (!startMove)
//             {
//                 startMoveSent = false;
//             }
//     }

//     void OnMastersCommand(RosMessageTypes.Std.StringMsg msg)
//     {
//         if (msg.data.StartsWith("play:"))
//         {
//             sendPosition = true;
//         }
//     }
// }


// Ajouté le 21/04 à 16h30 // Modifié le 22/04 à 10h00

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Masters;

public class Position_Publisher : MonoBehaviour
{
    ROSConnection ros;
    public GameObject cube;
    public GameObject robotBase;

    public bool startMove = false;
    private bool startMoveSent = false;
    public string startMoveTopicName = "/start_move";

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<StartMoveMsg>(startMoveTopicName);
        ros.ImplementService<ContactPointServiceRequest, ContactPointServiceResponse>("cp_position", HandleContactPointRequest);

        // Ajouté le 22/04 à 10h00 pour les autres services
        ros.ImplementService<AppControlServiceRequest, AppControlServiceResponse>("app_control", HandleAppControlRequest);
        ros.ImplementService<SystemStateServiceRequest, SystemStateServiceResponse>("system_state", HandleSystemStateRequest);
    }

    void Update()
    {
        if (startMove && !startMoveSent)
        {
            var msg = new StartMoveMsg();
            msg.start = true;
            ros.Publish(startMoveTopicName, msg);
            startMoveSent = true;
            startMove = false;
            Debug.Log("Signal /start_move envoyé.");
        }

        if (!startMove)
        {
            startMoveSent = false;
        }
    }

    private ContactPointServiceResponse HandleContactPointRequest(ContactPointServiceRequest request)
    {
        var response = new ContactPointServiceResponse();
        Vector3 cubeLocalPosition = robotBase.transform.InverseTransformPoint(cube.transform.position);
        PointMsg rosPosition = cubeLocalPosition.To<FLU>();
        response.success = true;
        response.position = rosPosition;
        Debug.Log($"cp_position service appelé — position envoyée : {cubeLocalPosition}");
        Debug.Log("types " + response.position.GetType());
        return response;
    }

    // Ajouté le 22/04 à 10h00 pour tester les autres services
    private AppControlServiceResponse HandleAppControlRequest(AppControlServiceRequest request)
    {
        // request.command : 0 = play, 1 = stop
        // TODO : ce qu'on fait avec play/stop dans la scène
        Debug.Log($"app_control reçu — command: {request.command}");
        var response = new AppControlServiceResponse();
        response.success = true;
        return response;
    }

    private SystemStateServiceResponse HandleSystemStateRequest(SystemStateServiceRequest request)
    {
        // request.command : 2 = Punch, 3 = Push, 4 = Touch
        // TODO : changer l'état de la scène Unity
        Debug.Log($"system_state reçu — command: {request.command}");
        var response = new SystemStateServiceResponse();
        response.success = true;
        return response;
    }
}