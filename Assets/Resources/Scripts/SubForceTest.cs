// // Ajouté le 28/04 - test avec plusieurs paliers

// using UnityEngine;
// using RosMessageTypes.Geometry;
// using Unity.Robotics.ROSTCPConnector;
// using System.Runtime.InteropServices;
// using UnityEngine.Events;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;


// public class SubForceTest : MonoBehaviour
// {

//     [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);

//     [DllImport("HapticsDirect")] public static extern void setForce(string configName, double[] lateral3, double[] torque3); // Ajouté le 19/03 à 09h24


//     public HapticPlugin hapticPlugin;
//     private string forceTopicName = "/haptic_force";
//     private double MaxForce;

//     // Ajouté le 02/03 à 22h - variable pour le filtre passe-bas
//     private Vector3 filteredForce = Vector3.zero;

//     // Ajouté le 02/03 à 22h - facteur d'échelle de la force (à ajuster selon confort)
//     public float scalingFactor = 0.025f;

//     // Ajouté le 21/03 à 12h10
//     private bool buttonPressed = false;
//     private float lastMessageTime = 0f;
//     private float timeoutDuration = 0.5f;

//     // Ajouté le 26/03 à 10h48
//     private int receiveForceCount = 0;
//     private float receiveForceLastTime = 0f;

//     public ForceGraph forceGraph; // Ajouté le 30/03 à 15h05

//     // Ajouté le 31/03 à 17h00
//     private System.IO.StreamWriter _csvWriter;

//     // -------------------------------------------------------
//     // Ajouté le 28/04 - Rendu par plages de force avec hystérésis
//     // Paliers de force rendue au stylet (en N)
//     // Réglables depuis l'inspecteur Unity
//     public float palier0 = 0.0f;   // pas de contact
//     public float palier1 = 0.15f;  // contact léger  (brut ~2-5N)
//     public float palier2 = 0.30f;  // contact moyen  (brut ~5-10N)
//     public float palier3 = 0.50f;  // contact fort   (brut >10N)

//     // Seuils de montée (brut en N)
//     public float seuil1_up = 2.0f;
//     public float seuil2_up = 5.0f;
//     public float seuil3_up = 10.0f;

//     // Seuils de descente avec hystérésis (légèrement inférieurs)
//     public float seuil1_down = 1.0f;
//     public float seuil2_down = 4.0f;
//     public float seuil3_down = 8.0f;

//     // Vitesse de transition entre paliers (N/message)
//     public float maxForceDelta = 0.02f;

//     // État interne
//     private int currentPalier = 0;
//     private float currentRenderedForce = 0f;
//     // -------------------------------------------------------


//     void Start()
//     {
//         if (hapticPlugin == null)
//         {
//             hapticPlugin = GetComponent<HapticPlugin>();
//             if (hapticPlugin == null)
//             {
//                 Debug.LogError("HapticForceFromROS : Aucun composant HapticPlugin trouvé. Veuillez assigner une référence dans l'inspecteur.");
//                 return;
//             }
//         }
//         else
//         {
//             MaxForce = hapticPlugin.MaxForce;

//             string path = Application.dataPath + "/../force_log.csv";
//             _csvWriter = new System.IO.StreamWriter(path, false);
//             _csvWriter.WriteLine("time_s,brut_N,filtered_N,applied_N,ressenti_N");
//         }

//         ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
//         ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Std.Int32Msg>("button_pressed", ReceiveButton);
//     }


//     void OnDestroy()
//     {
//         if (hapticPlugin != null)
//         {
//             double[] forceArray = new double[3] { 0, 0, 0 };
//             double[] torqueArray = new double[3] { 0, 0, 0 };
//             HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
//         }

//         if (_csvWriter != null)
//         {
//             _csvWriter.Close();
//             _csvWriter = null;
//         }
//     }

//     void Update()
//     {
//         if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0 && Time.time - lastMessageTime > timeoutDuration)
//         {
//             filteredForce = Vector3.zero;
//             currentRenderedForce = 0f;
//             currentPalier = 0;

//             double[] zeroDir = new double[3] { 0, 0, 0 };
//             setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
//         }
//     }


//     private void ReceiveForce(WrenchStampedMsg forceMsg)
//     {
//         receiveForceCount++;
//         float now = Time.realtimeSinceStartup;
//         if (now - receiveForceLastTime >= 1f)
//         {
//             Debug.Log($"ReceiveForce fréquence : {receiveForceCount} Hz");
//             receiveForceCount = 0;
//             receiveForceLastTime = now;
//         }

//         if (hapticPlugin != null && hapticPlugin.DeviceIdentifier != null)
//         {
//             lastMessageTime = Time.time;

//             Vector3 force = new Vector3(
//                (float)forceMsg.wrench.force.y,
//                (float)forceMsg.wrench.force.z,
//                -(float)forceMsg.wrench.force.x
//            );
//             float brutMag = force.magnitude;

//             // Filtre EMA
//             float alpha = 0.87f;
//             filteredForce = alpha * filteredForce + (1 - alpha) * force;
//             float filteredMag = filteredForce.magnitude;

//             force = filteredForce;

//             // Scaling
//             force = force * scalingFactor;

//             // Soft clamp
//             float softMax = (float)MaxForce;
//             float magnitude = force.magnitude;
//             if (magnitude > softMax)
//                 force = force.normalized * (softMax + (float)System.Math.Tanh(magnitude - softMax));

//             // Seuil de bruit
//             float noiseThreshold = 0.05f;
//             if (force.magnitude < noiseThreshold)
//                 force = Vector3.zero;

//             Vector3 direction = force.normalized;
//             double ForceMag = force.magnitude;
//             double[] ForceDir = new double[] { direction.x, direction.y, direction.z };

//             // -------------------------------------------------------
//             // Ajouté le 28/04 - Calcul du palier cible avec hystérésis
//             // On utilise brutMag (force brute non scalée) pour décider du palier
//             float targetForce = palier0;

//             if (currentPalier == 0)
//             {
//                 if      (brutMag >= seuil3_up) { currentPalier = 3; targetForce = palier3; }
//                 else if (brutMag >= seuil2_up) { currentPalier = 2; targetForce = palier2; }
//                 else if (brutMag >= seuil1_up) { currentPalier = 1; targetForce = palier1; }
//                 else                           { currentPalier = 0; targetForce = palier0; }
//             }
//             else if (currentPalier == 1)
//             {
//                 if      (brutMag >= seuil3_up)   { currentPalier = 3; targetForce = palier3; }
//                 else if (brutMag >= seuil2_up)   { currentPalier = 2; targetForce = palier2; }
//                 else if (brutMag >= seuil1_up)   { currentPalier = 1; targetForce = palier1; }
//                 else if (brutMag < seuil1_down)  { currentPalier = 0; targetForce = palier0; }
//                 else                             {                     targetForce = palier1; }
//             }
//             else if (currentPalier == 2)
//             {
//                 if      (brutMag >= seuil3_up)   { currentPalier = 3; targetForce = palier3; }
//                 else if (brutMag >= seuil2_up)   { currentPalier = 2; targetForce = palier2; }
//                 else if (brutMag < seuil1_down)  { currentPalier = 0; targetForce = palier0; }
//                 else if (brutMag < seuil2_down)  { currentPalier = 1; targetForce = palier1; }
//                 else                             {                     targetForce = palier2; }
//             }
//             else if (currentPalier == 3)
//             {
//                 if      (brutMag >= seuil3_up)   { currentPalier = 3; targetForce = palier3; }
//                 else if (brutMag < seuil1_down)  { currentPalier = 0; targetForce = palier0; }
//                 else if (brutMag < seuil2_down)  { currentPalier = 1; targetForce = palier1; }
//                 else if (brutMag < seuil3_down)  { currentPalier = 2; targetForce = palier2; }
//                 else                             {                     targetForce = palier3; }
//             }

//             // Transition douce vers le palier cible
//             currentRenderedForce = Mathf.MoveTowards(currentRenderedForce, targetForce, maxForceDelta);
//             // -------------------------------------------------------

//             // Graphe
//             if (forceGraph != null)
//                 forceGraph.PushSample(brutMag, filteredMag, (float)ForceMag, hapticPlugin.MagForce);

//             // CSV
//             if (_csvWriter != null)
//             {
//                 double t = forceMsg.header.stamp.sec + forceMsg.header.stamp.nanosec * 1e-9;
//                 float ressentiMag = hapticPlugin.MagForce;
//                 _csvWriter.WriteLine($"{t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{brutMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{filteredMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{((float)ForceMag).ToString(System.Globalization.CultureInfo.InvariantCulture)},{ressentiMag.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
//             }

//             // Rendu haptique
//             if (buttonPressed)
//             {
//                 setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, currentRenderedForce);
//             }
//             else
//             {
//                 currentRenderedForce = 0f;
//                 currentPalier = 0;
//                 double[] zeroDir = new double[3] { 0, 0, 0 };
//                 setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
//             }
//         }
//     }





//     // Ajouté le 21/03 à 12h10
//     private void ReceiveButton(RosMessageTypes.Std.Int32Msg msg)
//     {
//         buttonPressed = msg.data == 1;
//     }

// }







// Ajouté le 30/04
// Version 2 - Paliers hystérésis + lissage brutMag (alphaBrut) + stabilité avant montée (framesStableRequired)

using UnityEngine;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using System.Runtime.InteropServices;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class SubForceTest : MonoBehaviour
{
    [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);
    [DllImport("HapticsDirect")] public static extern void setForce(string configName, double[] lateral3, double[] torque3);

    public HapticPlugin hapticPlugin;
    private string forceTopicName = "/haptic_force";
    private double MaxForce;

    private Vector3 filteredForce = Vector3.zero;
    public float scalingFactor = 0.025f;

    private bool buttonPressed = false;
    private float lastMessageTime = 0f;
    private float timeoutDuration = 0.5f;

    private int receiveForceCount = 0;
    private float receiveForceLastTime = 0f;

    public ForceGraph forceGraph;
    private System.IO.StreamWriter _csvWriter;

    // Paliers de force ressentie (N)
    public float palier0 = 0.0f;
    public float palier1 = 0.20f;
    public float palier2 = 0.40f;
    public float palier3 = 0.60f;

    // Seuils de montée sur brutMag lissé (N)
    public float seuil1_up = 3.0f;
    public float seuil2_up = 8.0f;
    public float seuil3_up = 15.0f;

    // Seuils de descente avec hystérésis
    public float seuil1_down = 1.5f;
    public float seuil2_down = 5.0f;
    public float seuil3_down = 10.0f;

    // Vitesse de transition
    public float maxForceDelta = 0.01f;

    // Lissage
    public float alphaBrut = 0.95f;

    // Stabilité avant montée de palier
    public int framesStableRequired = 5;

    // Mode test calibration DLL
    public bool testMode = false;
    public float testForce = 0.0f;

    // État interne
    private int currentPalier = 0;
    private float currentRenderedForce = 0f;
    private float smoothedBrut = 0f;
    private int framesAboveSeuil = 0;

    void Start()
    {
        if (hapticPlugin == null)
        {
            hapticPlugin = GetComponent<HapticPlugin>();
            if (hapticPlugin == null)
            {
                Debug.LogError("SubForceTest : Aucun HapticPlugin trouvé.");
                return;
            }
        }
        else
        {
            MaxForce = hapticPlugin.MaxForce;
            string path = Application.dataPath + "/../force_log.csv";
            _csvWriter = new System.IO.StreamWriter(path, false);
            _csvWriter.WriteLine("time_s,brut_N,smoothed_N,applied_N,ressenti_N");
        }

        ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
        ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Std.Int32Msg>("button_pressed", ReceiveButton);
    }

    void OnDestroy()
    {
        if (hapticPlugin != null)
        {
            double[] forceArray = new double[3] { 0, 0, 0 };
            double[] torqueArray = new double[3] { 0, 0, 0 };
            HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
        }
        if (_csvWriter != null) { _csvWriter.Close(); _csvWriter = null; }
    }

    void Update()
    {
        if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0 && Time.time - lastMessageTime > timeoutDuration)
        {
            filteredForce = Vector3.zero;
            currentRenderedForce = 0f;
            currentPalier = 0;
            smoothedBrut = 0f;
            framesAboveSeuil = 0;
            double[] zeroDir = new double[3] { 0, 0, 0 };
            setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
        }
    }

    private void ReceiveForce(WrenchStampedMsg forceMsg)
    {
        receiveForceCount++;
        float now = Time.realtimeSinceStartup;
        if (now - receiveForceLastTime >= 1f)
        {
            Debug.Log($"ReceiveForce : {receiveForceCount} Hz");
            receiveForceCount = 0;
            receiveForceLastTime = now;
        }

        if (hapticPlugin == null || hapticPlugin.DeviceIdentifier == null) return;

        lastMessageTime = Time.time;

        // 1. Force brute
        Vector3 force = new Vector3(
            (float)forceMsg.wrench.force.y,
            (float)forceMsg.wrench.force.z,
            -(float)forceMsg.wrench.force.x
        );
        float brutMag = force.magnitude;

        // 2. Lissage direction
        float alpha = 0.87f;
        filteredForce = alpha * filteredForce + (1 - alpha) * force;
        float filteredMag = filteredForce.magnitude;

        // 3. Lissage fort de brutMag pour décisions de palier
        smoothedBrut = alphaBrut * smoothedBrut + (1 - alphaBrut) * brutMag;

        // 4. Compteur de stabilité
        float nextSeuil = currentPalier == 0 ? seuil1_up :
                          currentPalier == 1 ? seuil2_up :
                          currentPalier == 2 ? seuil3_up : float.MaxValue;

        if (smoothedBrut >= nextSeuil)
            framesAboveSeuil++;
        else
            framesAboveSeuil = 0;

        // 5. Décision de palier
        int targetPalier = currentPalier;

        if (framesAboveSeuil >= framesStableRequired)
        {
            if      (smoothedBrut >= seuil3_up) targetPalier = 3;
            else if (smoothedBrut >= seuil2_up) targetPalier = 2;
            else if (smoothedBrut >= seuil1_up) targetPalier = 1;
            framesAboveSeuil = 0;
        }

        // Descente immédiate avec hystérésis
        if      (smoothedBrut < seuil1_down) targetPalier = 0;
        else if (smoothedBrut < seuil2_down && currentPalier > 1) targetPalier = 1;
        else if (smoothedBrut < seuil3_down && currentPalier > 2) targetPalier = 2;

        currentPalier = targetPalier;

        // 6. Force cible selon palier
        float targetForce = currentPalier == 0 ? palier0 :
                            currentPalier == 1 ? palier1 :
                            currentPalier == 2 ? palier2 : palier3;

        // 7. Transition douce — montée lente, descente rapide
        float delta = targetForce > currentRenderedForce ? maxForceDelta : maxForceDelta * 20f;
        currentRenderedForce = Mathf.MoveTowards(currentRenderedForce, targetForce, delta);

        // 8. Direction
        Vector3 direction = filteredForce.magnitude > 0.01f ? filteredForce.normalized : Vector3.zero;
        double[] ForceDir = new double[] { direction.x, direction.y, direction.z };

        // 9. Graphe
        if (forceGraph != null)
            forceGraph.PushSample(brutMag, smoothedBrut, currentRenderedForce, hapticPlugin.MagForce);

        // 10. CSV
        if (_csvWriter != null)
        {
            double t = forceMsg.header.stamp.sec + forceMsg.header.stamp.nanosec * 1e-9;
            float ressentiMag = hapticPlugin.MagForce;
            _csvWriter.WriteLine($"{t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{brutMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{smoothedBrut.ToString(System.Globalization.CultureInfo.InvariantCulture)},{currentRenderedForce.ToString(System.Globalization.CultureInfo.InvariantCulture)},{ressentiMag.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        // 11. Rendu haptique
        if (testMode)
        {
            double[] testDir3 = new double[] { 0, -1, 0 };
            setConstantForceValues(hapticPlugin.DeviceIdentifier, testDir3, testForce);
        }
        else if (buttonPressed)
        {
            setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, currentRenderedForce);
        }
        else
        {
            currentRenderedForce = 0f;
            currentPalier = 0;
            smoothedBrut = 0f;
            framesAboveSeuil = 0;
            double[] zeroDir = new double[3] { 0, 0, 0 };
            setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
        }
    }

    private void ReceiveButton(RosMessageTypes.Std.Int32Msg msg)
    {
        buttonPressed = msg.data == 1;
    }
}







// Ajouté le 30/04
// Version 3 - Idem Version 2 + inContact public pour freeze Y (Unity) dans stateButtonsTest
// Fonctionne avec Version 1 de stateButtonsTest

// using UnityEngine;
// using RosMessageTypes.Geometry;
// using Unity.Robotics.ROSTCPConnector;
// using System.Runtime.InteropServices;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;

// public class SubForceTest : MonoBehaviour
// {
//     [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);
//     [DllImport("HapticsDirect")] public static extern void setForce(string configName, double[] lateral3, double[] torque3);

//     public HapticPlugin hapticPlugin;
//     private string forceTopicName = "/haptic_force";
//     private double MaxForce;

//     private Vector3 filteredForce = Vector3.zero;
//     public float scalingFactor = 0.025f;

//     private bool buttonPressed = false;
//     private float lastMessageTime = 0f;
//     private float timeoutDuration = 0.5f;

//     private int receiveForceCount = 0;
//     private float receiveForceLastTime = 0f;

//     public ForceGraph forceGraph;
//     private System.IO.StreamWriter _csvWriter;

//     // Paliers
//     public float palier0 = 0.0f;
//     public float palier1 = 0.15f;
//     public float palier2 = 0.30f;
//     public float palier3 = 1.0f;

//     // Seuils montée
//     public float seuil1_up = 5.0f;
//     public float seuil2_up = 30.0f;
//     public float seuil3_up = 60.0f;

//     // Seuils descente
//     public float seuil1_down = 1.0f;
//     public float seuil2_down = 15.0f;
//     public float seuil3_down = 45.0f;

//     // Transition
//     public float maxForceDelta = 0.001f;

//     // Lissage brutMag
//     public float alphaBrut = 0.95f;

//     // Stabilité avant montée
//     public int framesStableRequired = 5;

//     // Mode test
//     public bool testMode = false;
//     public float testForce = 0.0f;

//     // État interne — public pour HapticButtonReader
//     public bool inContact = false;
//     private int currentPalier = 0;
//     private float currentRenderedForce = 0f;
//     private float smoothedBrut = 0f;
//     private int framesAboveSeuil = 0;


//     void Start()
//     {
//         if (hapticPlugin == null)
//         {
//             hapticPlugin = GetComponent<HapticPlugin>();
//             if (hapticPlugin == null)
//             {
//                 Debug.LogError("SubForceTest : Aucun HapticPlugin trouvé.");
//                 return;
//             }
//         }
//         else
//         {
//             MaxForce = hapticPlugin.MaxForce;
//             string path = Application.dataPath + "/../force_log.csv";
//             _csvWriter = new System.IO.StreamWriter(path, false);
//             _csvWriter.WriteLine("time_s,brut_N,smoothed_N,applied_N,ressenti_N");
//         }

//         ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);
//         ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Std.Int32Msg>("button_pressed", ReceiveButton);
//     }

//     void OnDestroy()
//     {
//         if (hapticPlugin != null)
//         {
//             double[] forceArray = new double[3] { 0, 0, 0 };
//             double[] torqueArray = new double[3] { 0, 0, 0 };
//             HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
//         }
//         if (_csvWriter != null) { _csvWriter.Close(); _csvWriter = null; }
//     }

//     void Update()
//     {
//         if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0 && Time.time - lastMessageTime > timeoutDuration)
//         {
//             filteredForce = Vector3.zero;
//             currentRenderedForce = 0f;
//             currentPalier = 0;
//             smoothedBrut = 0f;
//             framesAboveSeuil = 0;
//             inContact = false;
//             double[] zeroDir = new double[3] { 0, 0, 0 };
//             setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
//         }
//     }

//     private void ReceiveForce(WrenchStampedMsg forceMsg)
//     {
//         receiveForceCount++;
//         float now = Time.realtimeSinceStartup;
//         if (now - receiveForceLastTime >= 1f)
//         {
//             Debug.Log($"ReceiveForce : {receiveForceCount} Hz");
//             receiveForceCount = 0;
//             receiveForceLastTime = now;
//         }

//         if (hapticPlugin == null || hapticPlugin.DeviceIdentifier == null) return;

//         lastMessageTime = Time.time;

//         Vector3 force = new Vector3(
//             (float)forceMsg.wrench.force.y,
//             (float)forceMsg.wrench.force.z,
//             -(float)forceMsg.wrench.force.x
//         );
//         float brutMag = force.magnitude;

//         float alpha = 0.87f;
//         filteredForce = alpha * filteredForce + (1 - alpha) * force;
//         float filteredMag = filteredForce.magnitude;

//         smoothedBrut = alphaBrut * smoothedBrut + (1 - alphaBrut) * brutMag;

//         // Stabilité avant montée
//         float nextSeuil = currentPalier == 0 ? seuil1_up :
//                           currentPalier == 1 ? seuil2_up :
//                           currentPalier == 2 ? seuil3_up : float.MaxValue;

//         if (smoothedBrut >= nextSeuil)
//             framesAboveSeuil++;
//         else
//             framesAboveSeuil = 0;

//         int targetPalier = currentPalier;

//         if (framesAboveSeuil >= framesStableRequired)
//         {
//             if      (smoothedBrut >= seuil3_up) targetPalier = 3;
//             else if (smoothedBrut >= seuil2_up) targetPalier = 2;
//             else if (smoothedBrut >= seuil1_up) targetPalier = 1;
//             framesAboveSeuil = 0;
//         }

//         // Descente avec hystérésis
//         if      (smoothedBrut < seuil1_down) targetPalier = 0;
//         else if (smoothedBrut < seuil2_down && currentPalier > 1) targetPalier = 1;
//         else if (smoothedBrut < seuil3_down && currentPalier > 2) targetPalier = 2;

//         currentPalier = targetPalier;

//         // Mise à jour inContact
//         inContact = currentPalier > 0;

//         float targetForce = currentPalier == 0 ? palier0 :
//                             currentPalier == 1 ? palier1 :
//                             currentPalier == 2 ? palier2 : palier3;

//         float delta = targetForce > currentRenderedForce ? maxForceDelta : maxForceDelta * 20f;
//         currentRenderedForce = Mathf.MoveTowards(currentRenderedForce, targetForce, delta);

//         Vector3 direction = filteredForce.magnitude > 0.01f ? filteredForce.normalized : Vector3.zero;
//         double[] ForceDir = new double[] { direction.x, direction.y, direction.z };

//         if (forceGraph != null)
//             forceGraph.PushSample(brutMag, smoothedBrut, currentRenderedForce, hapticPlugin.MagForce);

//         if (_csvWriter != null)
//         {
//             double t = forceMsg.header.stamp.sec + forceMsg.header.stamp.nanosec * 1e-9;
//             float ressentiMag = hapticPlugin.MagForce;
//             _csvWriter.WriteLine($"{t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{brutMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{smoothedBrut.ToString(System.Globalization.CultureInfo.InvariantCulture)},{currentRenderedForce.ToString(System.Globalization.CultureInfo.InvariantCulture)},{ressentiMag.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
//         }

//         if (testMode)
//         {
//             double[] testDir3 = new double[] { 0, -1, 0 };
//             setConstantForceValues(hapticPlugin.DeviceIdentifier, testDir3, testForce);
//         }
//         else if (buttonPressed)
//         {
//             setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, currentRenderedForce);
//         }
//         else
//         {
//             currentRenderedForce = 0f;
//             currentPalier = 0;
//             smoothedBrut = 0f;
//             framesAboveSeuil = 0;
//             inContact = false;
//             double[] zeroDir = new double[3] { 0, 0, 0 };
//             setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
//         }
//     }

//     private void ReceiveButton(RosMessageTypes.Std.Int32Msg msg)
//     {
//         buttonPressed = msg.data == 1;
//     }
// }