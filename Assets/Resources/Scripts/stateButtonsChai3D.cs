// Script specifique a la scene SPHERE_link_chai3D
// Recoit position depuis Chai3D via UDP et deplace le collider

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.UnityRoboticsIra2;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class stateButtonsChai3D : MonoBehaviour
{
    ROSConnection ros;
    public new GameObject collider;
    public GameObject tool0;
    public GameObject simpleStylus;

    private int button_pressed;

    // UDP - reception depuis Chai3D
    private UdpClient udpReceiver;
    private Thread udpThread;
    private bool udpRunning = false;
    private const int LISTEN_PORT = 5005;

    // Position recue depuis Chai3D (thread-safe)
    private Vector3 receivedPos = Vector3.zero;
    private int receivedButton = 0;
    private bool newDataAvailable = false;
    private readonly object dataLock = new object();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>("haptic_position");
        ros.RegisterPublisher<Int32Msg>("button_pressed");
        ros.RegisterPublisher<BoolMsg>("reset_position");

        // Demarrer reception UDP depuis Chai3D
        udpReceiver = new UdpClient(LISTEN_PORT);
        udpRunning = true;
        udpThread = new Thread(UDPReceiveThread);
        udpThread.IsBackground = true;
        udpThread.Start();

        Debug.Log($"[UDP] En ecoute sur port {LISTEN_PORT}");
    }

    void OnDestroy()
    {
        udpRunning = false;
        if (udpReceiver != null)
            udpReceiver.Close();
        if (udpThread != null)
            udpThread.Join(500);
    }

    private void UDPReceiveThread()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (udpRunning)
        {
            try
            {
                byte[] data = udpReceiver.Receive(ref remoteEP);
                string msg = Encoding.ASCII.GetString(data);
                string[] parts = msg.Trim().Split(',');
                if (parts.Length == 4)
                {
                    float x = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                    int btn = int.Parse(parts[3]);

                    lock (dataLock)
                    {
                        receivedPos = new Vector3(x, y, z);
                        receivedButton = btn;
                        newDataAvailable = true;
                    }
                }
            }
            catch { }
        }
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

    void Update()
    {
        Vector3 pos;
        int btn;
        bool hasData;

        lock (dataLock)
        {
            pos = receivedPos;
            btn = receivedButton;
            hasData = newDataAvailable;
            newDataAvailable = false;
        }

        if (!hasData) return;

        // Mapping Chai3D -> Unity
        // Chai3D X=profondeur, Y=gauche/droite, Z=haut/bas
        // Unity X=gauche/droite, Y=haut/bas, Z=profondeur
        collider.transform.position = new Vector3(pos.y, pos.z, -pos.x);
        simpleStylus.transform.position = new Vector3(pos.y, pos.z, -pos.x);

        // Publier position sur ROS2
        if (btn == 1)
        {
            button_pressed = 1;
            ros.Publish("button_pressed", new Int32Msg(1));
            PublishPosition();
        }
        else
        {
            button_pressed = 0;
            ros.Publish("button_pressed", new Int32Msg(0));
        }
    }
}