// using UnityEngine;
// using System.Runtime.InteropServices;
// using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.Geometry;
// using RosMessageTypes.Std;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;
// using RosMessageTypes.BuiltinInterfaces;
// using RosMessageTypes.UnityRoboticsIra2;


// public class HapticButtonReader : MonoBehaviour
// {
//     ROSConnection ros;
//     public new GameObject collider; //Ajouté "new" le 20/03 à 09h20

//     public GameObject tool0;

//     private int button_pressed;
//     private float publishMessageFrequency = 0.008f;
//     private float timeElapsed;
    
//     // Importer la fonction getButtons depuis HapticsDirect
//     [DllImport("HapticsDirect")]
//     public static extern void getButtons(string configName, int[] buttons4, int[] last_buttons4, ref int inkwell);

//     public HapticPlugin hapticPlugin;

//     private int[] buttons = new int[4] { 0, 0, 0, 0 }; // État actuel des boutons
//     private int[] lastButtons = new int[4] { 0, 0, 0, 0 }; // État précédent des boutons
//     private int inkwell = 0; // État du commutateur inkwell

//     private bool button2Pressed = false;

//     private int zeroCount = 0;
//     private const int ZERO_THRESHOLD = 5;

//     void Start()
//     {
//         ros = ROSConnection.GetOrCreateInstance();
//         ros.RegisterPublisher<PoseStampedMsg>("haptic_position");
//         ros.RegisterPublisher<Int32Msg>("button_pressed");
        
//         ros.RegisterPublisher<BoolMsg>("reset_position"); //ajoutée

//         // Vérifier que la référence à HapticPlugin est définie
//         if (hapticPlugin == null)
//         {
//             hapticPlugin = GetComponent<HapticPlugin>();
//             if (hapticPlugin == null)
//             {
//                 Debug.LogError("HapticButtonReader : Aucun composant HapticPlugin trouvé. Veuillez assigner une référence dans l'inspecteur.");
//             }
//         }
//     }

//     private void PublishPosition()
//     {
//         Vector3 colliderPosition = collider.transform.position;
//         Quaternion colliderRotation = collider.transform.rotation;

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
//         Debug.Log($"HapticButtonReader : Bouton {button_pressed} pressé.");
//     }
        
//     private void LateUpdate()
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
//         // Sauvegarder l'état précédent des boutons
//         for (int i = 0; i < 4; i++)
//         {
//             lastButtons[i] = buttons[i];
//         }

//         // Appeler getButtons pour mettre à jour l'état des boutons
//         getButtons(hapticPlugin.DeviceIdentifier, buttons, lastButtons, ref inkwell);

//         // Traiter les événements des boutons
//         if (buttons[1] == 1)
//         {
//             hapticPlugin.freezeStyloTranslation = false;
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

//         // AVANT
//         // if (buttons[0] == 0 && buttons[1] == 0)
//         // {
//         //     ros.Publish("button_pressed", new Int32Msg(0));
//         //     Debug.Log("HapticButtonReader : Aucun bouton pressé.");
//         // }

//         // APRÈS

//         //Commenté le 18/03 à 15h42
        
//         // if (buttons[1] == 0 && buttons[0] == 0)
//         // {
//         //     ros.Publish("reset_position", new BoolMsg(true));
//         //     ros.Publish("button_pressed", new Int32Msg(0));
//         //     //Debug.Log("HapticButtonReader : Aucun bouton pressé.");
//         // }
        

//         //Ajouté le 18/03 à 15h42
//         if (buttons[0] == 0 && buttons[1] == 0)
//         {
//             zeroCount++;
//             if (zeroCount >= ZERO_THRESHOLD)
//             {
//                 ros.Publish("reset_position", new BoolMsg(true));
//                 ros.Publish("button_pressed", new Int32Msg(0));
//                 zeroCount = 0;

//                 //Ajouté le 20/03 à 14h24
//                 // Recaler le stylet sur tool0 au relâchement
//                 if (hapticPlugin != null && tool0 != null)
//                 {
//                     Vector3 offset = hapticPlugin.transform.position - collider.transform.position;
//                     hapticPlugin.transform.position = tool0.transform.position + offset;
//                     hapticPlugin.freezeStyloTranslation = true;
//                     hapticPlugin.styloFrozenTranslation = tool0.transform.position + offset;
//                 }
//             }
//         }
//         else
//         {
//             zeroCount = 0;
//         }

//     }
// }