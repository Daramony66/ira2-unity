// Script specifique a la scene SPHERE_link_chai3D
// Recoit position depuis Chai3D via UDP et deplace le collider
// Recalage du stylus au TCP du robot au relachement du bouton

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
    private int zeroCount = 0;
    private const int ZERO_THRESHOLD = 10;

    // Flag : true = bouton relache, on ignore les positions Chai3D
    private bool isReleased = true;

    // Offset entre position Chai3D et TCP au moment de l'appui
    private Vector3 offset = Vector3.zero;
    private bool firstPress = true;
    private Vector3 lastChai3DPos = Vector3.zero;

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

        udpReceiver = new UdpClient(LISTEN_PORT);
        udpRunning = true;
        udpThread = new Thread(UDPReceiveThread);
        udpThread.IsBackground = true;
        udpThread.Start();

        Debug.Log($"[UDP] En ecoute sur port {LISTEN_PORT}");

        if (tool0 != null)
        {
            collider.transform.position = tool0.transform.position;
            simpleStylus.transform.position = tool0.transform.position;
        }

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
        Vector3 chai3DPosUnity = new Vector3(pos.y, pos.z, -pos.x);

        if (btn == 1)
        {
            zeroCount = 0;

            if (isReleased)
            {
                // Premier appui apres relachement :
                // Calcul de l'offset pour partir du TCP
                offset = tool0.transform.position - chai3DPosUnity;
                isReleased = false;
                firstPress = false;
            }

            // Position avec offset pour partir du TCP
            Vector3 targetPos = chai3DPosUnity + offset;
            collider.transform.position = targetPos;
            simpleStylus.transform.position = targetPos;

            button_pressed = 1;
            ros.Publish("button_pressed", new Int32Msg(1));
            PublishPosition();
        }
        else
        {
            zeroCount++;
            if (zeroCount >= ZERO_THRESHOLD)
            {
                isReleased = true;
                offset = Vector3.zero;
                zeroCount = ZERO_THRESHOLD;
            }

            if (isReleased && tool0 != null)
            {
                collider.transform.position = tool0.transform.position;
                simpleStylus.transform.position = tool0.transform.position;
            }

            ros.Publish("button_pressed", new Int32Msg(0));
            button_pressed = 0;
        }
    }
}