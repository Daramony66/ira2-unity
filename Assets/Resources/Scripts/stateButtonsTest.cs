// //Ajouté le 29/04 - test pour bloquer Z du robot quand contact

// // Fonctionne avec la 3ème version du SubForceTest

// using UnityEngine;
// using System.Runtime.InteropServices;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Geometry;
// using RosMessageTypes.Std;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;
// using RosMessageTypes.BuiltinInterfaces;
// using RosMessageTypes.UnityRoboticsIra2;


// public class stateButtonsTest : MonoBehaviour
// {
//     ROSConnection ros;
//     public new GameObject collider;
//     public GameObject tool0;

//     private int button_pressed;
//     private float publishMessageFrequency = 0.008f;
//     private float timeElapsed;

//     [DllImport("HapticsDirect")]
//     public static extern void getButtons(string configName, int[] buttons4, int[] last_buttons4, ref int inkwell);

//     public HapticPlugin hapticPlugin;

//     private int[] buttons = new int[4] { 0, 0, 0, 0 };
//     private int[] lastButtons = new int[4] { 0, 0, 0, 0 };
//     private int inkwell = 0;

//     private bool button2Pressed = false;

//     private int zeroCount = 0;
//     private const int ZERO_THRESHOLD = 10;

//     // Ajouté le 29/04 - Freeze Y au contact
//     public HapticForce2 hapticForce2;
//     private bool yLocked = false;
//     private float yContact = 0f;
//     public float yMargin = 0.01f;


//     void Start()
//     {
//         ros = ROSConnection.GetOrCreateInstance();
//         ros.RegisterPublisher<PoseStampedMsg>("haptic_position");
//         ros.RegisterPublisher<Int32Msg>("button_pressed");
//         ros.RegisterPublisher<BoolMsg>("reset_position");

//         if (hapticPlugin == null)
//         {
//             hapticPlugin = GetComponent<HapticPlugin>();
//             if (hapticPlugin == null)
//             {
//                 Debug.LogError("stateButtonsTest : Aucun composant HapticPlugin trouvé.");
//             }
//         }
//     }

//     private void PublishPosition()
//     {
//         Vector3 colliderPosition = collider.transform.position;
//         Quaternion colliderRotation = collider.transform.rotation;

//         // Ajouté le 29/04 - Freeze Y au contact
//         if (hapticForce2 != null && hapticForce2.inContact)
//         {
//             if (!yLocked)
//             {
//                 yContact = colliderPosition.y;
//                 yLocked = true;
//             }
//             // Empêcher de descendre sous yContact
//             colliderPosition.y = Mathf.Max(colliderPosition.y, yContact);
//         }
//         else if (yLocked && colliderPosition.y > yContact + yMargin)
//         {
//             yLocked = false;
//         }

//         TimeMsg rostime = new TimeMsg
//         {
//             sec = (int)Time.time,
//             nanosec = (uint)((Time.time % 1) * 1e9)
//         };

//         PoseStampedMsg colliderPoseStamped = new PoseStampedMsg
//         {
//             header = new HeaderMsg
//             {
//                 stamp = rostime,
//                 frame_id = "world"
//             },
//             pose = new PoseMsg(
//                 new Vector3(colliderPosition.x, colliderPosition.y, colliderPosition.z).To<FLU>(),
//                 new Quaternion(colliderRotation.x, colliderRotation.y, colliderRotation.z, colliderRotation.w).To<FLU>()
//             )
//         };

//         ros.Publish("haptic_position", colliderPoseStamped);
//     }

//     private void ButtonPressed(Int32Msg msg)
//     {
//         button_pressed = msg.data;
//         ros.Publish("button_pressed", new Int32Msg(button_pressed));
//     }

//     private void Update()
//     {
//         timeElapsed += Time.deltaTime;
//         if (timeElapsed > publishMessageFrequency)
//         {
//             if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0)
//             {
//                 UpdateButtonStatus();
//             }
//             timeElapsed = 0;
//         }
//     }

//     private void UpdateButtonStatus()
//     {
//         for (int i = 0; i < 4; i++)
//         {
//             lastButtons[i] = buttons[i];
//         }

//         getButtons(hapticPlugin.DeviceIdentifier, buttons, lastButtons, ref inkwell);

//         if (buttons[1] == 1)
//         {
//             if (hapticPlugin.freezeTranslation == true)
//             {
//                 hapticPlugin.transform.parent.position += tool0.transform.position - collider.transform.position;
//             }
//             hapticPlugin.FreezeStyloTranslation(false, tool0.transform.position);
//             ButtonPressed(new Int32Msg(1));
//             PublishPosition();
//         }

//         if (buttons[0] == 1 && !button2Pressed)
//         {
//             button2Pressed = true;
//             ButtonPressed(new Int32Msg(2));
//         }
//         else if (buttons[0] == 0 && button2Pressed)
//         {
//             button2Pressed = false;
//         }

//         if (buttons[0] == 0 && buttons[1] == 0)
//         {
//             zeroCount++;
//             if (zeroCount >= ZERO_THRESHOLD)
//             {
//                 ros.Publish("reset_position", new BoolMsg(true));
//                 ros.Publish("button_pressed", new Int32Msg(0));
//                 zeroCount = 0;

//                 // Ajouté le 29/04 - Reset du freeze Y au relâchement
//                 yLocked = false;
//                 yContact = 0f;

//                 if (hapticPlugin != null && tool0 != null)
//                 {
//                     hapticPlugin.transform.parent.position += tool0.transform.position - collider.transform.position;
//                     hapticPlugin.FreezeStyloTranslation(true, tool0.transform.position);
//                 }
//             }
//         }
//         else
//         {
//             zeroCount = 0;
//         }
//     }
// }














// Ajouté le 30/04 - Tentative envoi UDP vers Chai3D - architecture abandonnée.

using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.UnityRoboticsIra2;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class stateButtonsTest : MonoBehaviour
{
    ROSConnection ros;
    public new GameObject collider;
    public GameObject tool0;

    private int button_pressed;
    private float publishMessageFrequency = 0.008f;
    private float timeElapsed;

    [DllImport("HapticsDirect")]
    public static extern void getButtons(string configName, int[] buttons4, int[] last_buttons4, ref int inkwell);

    public HapticPlugin hapticPlugin;

    private int[] buttons = new int[4] { 0, 0, 0, 0 };
    private int[] lastButtons = new int[4] { 0, 0, 0, 0 };
    private int inkwell = 0;

    private bool button2Pressed = false;
    private int zeroCount = 0;
    private const int ZERO_THRESHOLD = 10;

    // UDP vers Chai3D
    private UdpClient udpClient;
    private IPEndPoint chai3dEndPoint;
    private const string CHAI3D_IP = "127.0.0.1";
    private const int CHAI3D_PORT = 5005;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>("haptic_position");
        ros.RegisterPublisher<Int32Msg>("button_pressed");
        ros.RegisterPublisher<BoolMsg>("reset_position");

        if (hapticPlugin == null)
        {
            hapticPlugin = GetComponent<HapticPlugin>();
            if (hapticPlugin == null)
            {
                Debug.LogError("stateButtonsTest : Aucun composant HapticPlugin trouve.");
            }
        }

        // Init UDP
        udpClient = new UdpClient();
        chai3dEndPoint = new IPEndPoint(IPAddress.Parse(CHAI3D_IP), CHAI3D_PORT);
        Debug.Log($"UDP vers Chai3D : {CHAI3D_IP}:{CHAI3D_PORT}");
    }

    void OnDestroy()
    {
        if (udpClient != null)
            udpClient.Close();
    }

    private void SendToChai3D(Vector3 pos, int btnState)
    {
        // Format : "x,y,z,button\n"
        string msg = $"{pos.x:F4},{pos.y:F4},{pos.z:F4},{btnState}\n";
        byte[] data = Encoding.ASCII.GetBytes(msg);
        udpClient.Send(data, data.Length, chai3dEndPoint);
    }

    private void PublishPosition()
    {
        Vector3 colliderPosition = collider.transform.position;
        Quaternion colliderRotation = collider.transform.rotation;

        TimeMsg rostime = new TimeMsg
        {
            sec = (int)Time.time,
            nanosec = (uint)((Time.time % 1) * 1e9)
        };

        PoseStampedMsg colliderPoseStamped = new PoseStampedMsg
        {
            header = new HeaderMsg
            {
                stamp = rostime,
                frame_id = "world"
            },
            pose = new PoseMsg(
                new Vector3(colliderPosition.x, colliderPosition.y, colliderPosition.z).To<FLU>(),
                new Quaternion(colliderRotation.x, colliderRotation.y, colliderRotation.z, colliderRotation.w).To<FLU>()
            )
        };

        ros.Publish("haptic_position", colliderPoseStamped);
    }

    private void ButtonPressed(Int32Msg msg)
    {
        button_pressed = msg.data;
        ros.Publish("button_pressed", new Int32Msg(button_pressed));
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed > publishMessageFrequency)
        {
            if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0)
            {
                UpdateButtonStatus();
            }
            timeElapsed = 0;
        }
    }

    private void UpdateButtonStatus()
    {
        for (int i = 0; i < 4; i++)
            lastButtons[i] = buttons[i];

        getButtons(hapticPlugin.DeviceIdentifier, buttons, lastButtons, ref inkwell);

        if (buttons[1] == 1)
        {
            if (hapticPlugin.freezeTranslation == true)
                hapticPlugin.transform.parent.position += tool0.transform.position - collider.transform.position;

            hapticPlugin.FreezeStyloTranslation(false, tool0.transform.position);
            ButtonPressed(new Int32Msg(1));
            PublishPosition();

            // Envoyer position + bouton pressé à Chai3D
            SendToChai3D(collider.transform.position, 1);
        }

        if (buttons[0] == 1 && !button2Pressed)
        {
            button2Pressed = true;
            ButtonPressed(new Int32Msg(2));
        }
        else if (buttons[0] == 0 && button2Pressed)
        {
            button2Pressed = false;
        }

        if (buttons[0] == 0 && buttons[1] == 0)
        {
            zeroCount++;
            if (zeroCount >= ZERO_THRESHOLD)
            {
                ros.Publish("reset_position", new BoolMsg(true));
                ros.Publish("button_pressed", new Int32Msg(0));
                zeroCount = 0;

                if (hapticPlugin != null && tool0 != null)
                {
                    hapticPlugin.transform.parent.position += tool0.transform.position - collider.transform.position;
                    hapticPlugin.FreezeStyloTranslation(true, tool0.transform.position);
                }

                // Envoyer bouton relache a Chai3D
                SendToChai3D(collider.transform.position, 0);
            }
        }
        else
        {
            zeroCount = 0;
        }
    }
}