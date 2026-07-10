using UnityEngine;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using System.Runtime.InteropServices;
using UnityEngine.Events;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class HapticForceLearner_V2 : MonoBehaviour
{

    [DllImport("HapticsDirect")] public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);

    [DllImport("HapticsDirect")] public static extern void setForce(string configName, double[] lateral3, double[] torque3); // Ajouté le 19/03 à 09h24


    public HapticPlugin hapticPlugin;

    public HapticCalibration calibration;   // à glisser dans l'inspecteur // Ajouté le 09/07 à 12h45

    //private string forceTopicName = "/tcp_force";
    // Commenté le 02/03 à 22h - remplacé par /haptic_force (forces transformées tool0 → base par haptic_control.cpp)
    //private string forceTopicName = "/force_torque_sensor_broadcaster/wrench";
    // Ajouté le 02/03 à 22h
    private string forceTopicName = "/learner/haptic_force";
    private double MaxForce;

    // Ajouté le 02/03 à 22h - variable pour le filtre passe-bas
    private Vector3 filteredForce = Vector3.zero;

    private Vector3 prevCorr = Vector3.zero;

    private Vector3 prevLearnerPos = Vector3.zero;
    public float correctionDamping = 0.5f;  // amortissement (anti-oscillation)




    // Ajouté le 02/03 à 22h - facteur d'échelle de la force (à ajuster selon confort)
    public float scalingFactor = 0.025f;



    // ----- Correction expert -> apprenant (Test 1 : ressort relatif permanent) -----
    [Header("Couplage expert->apprenant")]

    public float correctionStiffness = 50f;  // raideur k du ressort (a regler)
    public float correctionMaxForce = 1.0f;  // limite de force de correction (securite)

    public HapticPlugin expertPlugin;  // HapticPlugin de l'expert (a glisser dans l'Inspector)


    private Vector3 expertStart;
    private Vector3 learnerStart;
    private bool correctionAnchored = false;


    private Vector3 filteredCorr = Vector3.zero;

    private Vector3 robotForce = Vector3.zero;        // derniere force robot (stockee, appliquee dans Update)
    private Vector3 correctionForce = Vector3.zero;   // derniere force de correction



    //Ajouté le 21/03 à 12h10
    private bool buttonPressed = false;



    private bool expertButtonPressed = false;



    private float lastMessageTime = 0f;
    private float timeoutDuration = 0.5f; // 500ms sans message = force nulle

    //Ajouté le 26/03 à 10h48
    private int receiveForceCount = 0;
    private float receiveForceLastTime = 0f;

    public ForceGraph forceGraph; //Ajouté le 30/03 à 15h05

    //Ajouté le 31/03 à 17h00
    private System.IO.StreamWriter _csvWriter;
    //private float _csvStartTime = 0f; //Commenté le 01/04 à 10h15 - on utilise maintenant le timestamp ROS pour l'axe temporel du log

    // Ajouté le 10/07
    public float gravityComp = 0.15f;   // compense le poids du bras (axe Y) //Ajouté le 10/07


    void Start()
    {
        // Vérifier que la référence à HapticPlugin est définie
        if (hapticPlugin == null)
        {
            hapticPlugin = GetComponent<HapticPlugin>();
            if (hapticPlugin == null)
            {
                Debug.LogError("HapticForceFromROS : Aucun composant HapticPlugin trouvé. Veuillez assigner une référence dans l'inspecteur.");
                return;
            }
        }
        else
        {
            MaxForce = hapticPlugin.MaxForce;
            //MaxForce = 10.0;
            //MaxForce = 1.0; //Ajouté le 27/02 à 16h20

            //Ajouté le 31/03 à 17h00 - initialisation du fichier de log CSV
            string path = Application.dataPath + "/../force_log_learner.csv";
            _csvWriter = new System.IO.StreamWriter(path, false);
            _csvWriter.WriteLine("time_s,brut_N,filtered_N,applied_N,ressenti_N"); //Ajouté le 01/04 à 14h30
            //_csvStartTime = Time.realtimeSinceStartup;  //Commenté le 01/04 à 10h15 - on utilise maintenant le timestamp ROS pour l'axe temporel du log

        }
        // S'inscrire au topic ROS2
        ROSConnection.GetOrCreateInstance().Subscribe<WrenchStampedMsg>(forceTopicName, ReceiveForce);

        ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Std.Int32Msg>("learner/button_pressed", ReceiveButton); //Ajouté le 21/03 à 12h10
    


        ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Std.Int32Msg>("expert/button_pressed", ReceiveExpertButton);

        if (expertPlugin != null && hapticPlugin != null)
        {
            expertStart = expertPlugin.CurrentPosition;
            learnerStart = hapticPlugin.CurrentPosition;
            correctionAnchored = true;
        }
    
    }


    void OnDestroy()
    {
        // Appliquer une force nulle avant la destruction
        if (hapticPlugin != null)
        {
            double[] forceArray = new double[3] { 0, 0, 0 };
            double[] torqueArray = new double[3] { 0, 0, 0 };
            HapticPlugin.setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
        }

        if (_csvWriter != null)
        {
            _csvWriter.Close();
            _csvWriter = null;
        }
    }

    //Ajouté le 21/03 à 12h10 - forcer la force à zéro si aucun message reçu depuis un certain temps (timeout)
    // void Update()
    // {
    //     if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0 && Time.time - lastMessageTime > timeoutDuration)
    //     {
    //         //Ajouté le 25/03 à 12h14
    //         filteredForce = Vector3.zero;

    //         //Commenté le 24/03 à 17h55 //Décommenté le 25/03 le 11h17
    //         double[] zeroDir = new double[3] { 0, 0, 0 };
    //         setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);

    //         //Ajouté le 24/03 à 17h55 //Commenté le 25/03 le 11h17
    //         // double[] forceArray = new double[3] { 0, 0, 0 };
    //         // double[] torqueArray = new double[3] { 0, 0, 0 };
    //         // setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
    //     }
    // }

    void Update()
    {

        if (calibration != null && !calibration.teleopActive) return; //Ajouté le 09/07 à 12h45 - ne pas appliquer de force si la calibration n'est pas terminée

        // 1) Calcul de la force de correction (seulement si l'expert corrige)
        correctionForce = Vector3.zero;

        // >>> AJOUT : timeout de securite (coupe la force robot figee si plus de messages)
        if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0 &&
            Time.time - lastMessageTime > timeoutDuration)
        {
            filteredForce = Vector3.zero;
            robotForce = Vector3.zero;
        }
        // <<< FIN AJOUT

        if (hapticPlugin != null && hapticPlugin.DeviceHHD >= 0 &&
            correctionAnchored && expertPlugin != null && expertButtonPressed)
        {


            // Écart direct entre les deux bras (mm -> m)
            Vector3 erreur = (expertPlugin.CurrentPosition - hapticPlugin.CurrentPosition) / 1000f;

            // Ressort : tire l'apprenant vers l'expert
            Vector3 corr = correctionStiffness * erreur;


            // Amortissement (anti-oscillation)
            Vector3 learnerVel = (hapticPlugin.CurrentPosition - prevLearnerPos) / 1000f / Time.deltaTime;
            corr -= correctionDamping * learnerVel;
            prevLearnerPos = hapticPlugin.CurrentPosition;

            // Filtre passe-bas
            filteredCorr = 0.9f * filteredCorr + 0.1f * corr;

            // Slew-rate (anti a-coups)
            Vector3 deltaCorr = filteredCorr - prevCorr;
            float maxStep = 0.05f;
            if (deltaCorr.magnitude > maxStep)
                filteredCorr = prevCorr + deltaCorr.normalized * maxStep;
            prevCorr = filteredCorr;

            corr = filteredCorr;

            // Deadband
            if (corr.magnitude < 0.05f)
                corr = Vector3.zero;

            // Limite de securite
            if (corr.magnitude > correctionMaxForce)
                corr = corr.normalized * correctionMaxForce;

            correctionForce = corr;
        }

        // 2) Fusion : force robot + force de correction
        if (hapticPlugin != null && hapticPlugin.DeviceIdentifier != null)
        {
            //Vector3 total = robotForce + correctionForce;
            Vector3 total = robotForce + correctionForce + new Vector3(0f, gravityComp, 0f); //Ajouté le 10/07


            if (total.magnitude < 0.0001f)
            {
                // Rien a appliquer : on n'envoie pas de direction nulle (perturbe le device)
                setConstantForceValues(hapticPlugin.DeviceIdentifier, new double[] { 1, 0, 0 }, 0.0);
            }
            else
            {
                Vector3 dir = total.normalized;
                double mag = total.magnitude;
                setConstantForceValues(hapticPlugin.DeviceIdentifier, new double[] { dir.x, dir.y, dir.z }, mag);
            }
        }
    }


    private void ReceiveForce(WrenchStampedMsg forceMsg)
    {

        if (calibration != null && !calibration.teleopActive) return; //Ajouté le 09/07 à 12h45 - ne pas appliquer de force si la calibration n'est pas terminée

        //Ajouté le 26/03 à 10h48 - calcul de la fréquence de réception des messages de force
        receiveForceCount++;
        float now = Time.realtimeSinceStartup;
        if (now - receiveForceLastTime >= 1f)
        {
            Debug.Log($"ReceiveForce fréquence : {receiveForceCount} Hz");
            receiveForceCount = 0;
            receiveForceLastTime = now;
        }

        // Appliquer la force au dispositif haptique
        if (hapticPlugin != null && hapticPlugin.DeviceIdentifier != null)
        {
            lastMessageTime = Time.time; //Ajouté le 21/03 à 12h10 - mise à jour du timestamp du dernier message reçu

            Vector3 force = new Vector3(
               (float)forceMsg.wrench.force.y, //Modifié "-" en "+" le 24/03 à 17h19
               (float)forceMsg.wrench.force.z,
               -(float)forceMsg.wrench.force.x // Modifié "-" en "+" le 24/03 à 17h19
           );
            float brutMag = force.magnitude; // Ajouté le 30/03 à 15h05
            //Debug.Log($"Forces brutes : x={force.x}, y={force.y}, z={force.z}");
            //Debug.Log($"Force brute : {force.magnitude}"); //Ajouté le 30/03 à 12h27

            
            // Ajouté le 02/03 à 22h - filtre passe-bas pour éliminer le bruit et les forces parasites
            // 80% de la force précédente + 20% de la nouvelle = lissage des vibrations
            //filteredForce = 0.8f * filteredForce + 0.2f * force; // Modifié le 03/03 à 14h30
            //filtre plus agressif pour atténuer les forces d'inertie pendant le mouvement
            float alpha = 0.87f; // fc = 10 Hz, fs = 500 Hz, justifié par analyse FFT (01/04 à 14h00)
            filteredForce = alpha * filteredForce + (1 - alpha) * force;

            float filteredMag = filteredForce.magnitude; // Ajouté le 31/03 à 15h10
            //Debug.Log($"Force filtree mag : {filteredForce.magnitude}"); //Ajouté le 30/03 à 12h27

            force = filteredForce;

            // Ajouté le 02/03 à 22h - scaling de la force (réduction de l'intensité ressentie)
            // scalingFactor réglable depuis l'inspecteur Unity
            force = force * scalingFactor;

            // Vector3 clampedForce = Vector3.ClampMagnitude(force, (float)MaxForce);

            // // Commenté le 09/03 à 13h07
            // // Debug.Log($"Force importée : x={clampedForce.x}, y={clampedForce.y}, z={clampedForce.z}");

            // Vector3 direction = clampedForce.normalized;
            // double ForceMag = clampedForce.magnitude;

            // double[] ForceDir = new double[] { direction.x, direction.y, direction.z };
            // setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, ForceMag);

            // Clamp progressif (soft clamp)
            float softMax = (float)MaxForce;
            float magnitude = force.magnitude;
            if (magnitude > softMax)
                force = force.normalized * (softMax + (float)System.Math.Tanh(magnitude - softMax));

            // Seuil de bruit
            float noiseThreshold = 0.05f; // à ajuster
            if (force.magnitude < noiseThreshold)
                force = Vector3.zero;

            // Debug
            //Debug.Log($"Force filtrée : {force}");

            //Commenté le 19/03 à 09h24
            Vector3 direction = force.normalized;
            double ForceMag = force.magnitude;
            double[] ForceDir = new double[] { direction.x, direction.y, direction.z };

            //Debug.Log($"Force filtrée : {ForceMag}"); //Ajouté le 30/03 à 12h27


            //setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, ForceMag); //Commenté le 21/03 à 12h10
            
            //Ajouté le 30/03 à 15h05 // Commenté le 31/03 à 15h10
            //if (forceGraph != null)
            //    forceGraph.PushSample(brutMag, (float)ForceMag, hapticPlugin.MagForce);

            //Ajouté le 31/03 à 15h10
            if (forceGraph != null)
                forceGraph.PushSample(brutMag, filteredMag, (float)ForceMag, hapticPlugin.MagForce);    


            //Ajouté le 31/03 à 17h00 - log des forces dans un fichier CSV pour analyse postérieure
            if (_csvWriter != null)
            {
                //float t = Time.realtimeSinceStartup - _csvStartTime; //Commenté le 01/04 à 10h15
                double t = forceMsg.header.stamp.sec + forceMsg.header.stamp.nanosec * 1e-9; //Ajouté le 01/04 à 10h15
                float ressentiMag = hapticPlugin.MagForce; //Ajouté le 01/04 à 14h30
                //_csvWriter.WriteLine($"{t:F4},{brutMag:F4},{filteredMag:F4},{ForceMag:F4}");
                //_csvWriter.WriteLine($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{brutMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{filteredMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{((float)ForceMag).ToString(System.Globalization.CultureInfo.InvariantCulture)}"); //Commenté le 01/04 à 10h15
                //_csvWriter.WriteLine($"{t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{brutMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{filteredMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{((float)ForceMag).ToString(System.Globalization.CultureInfo.InvariantCulture)}"); //Ajouté le 01/04 à 10h15 //Commenté à 14h30
                _csvWriter.WriteLine($"{t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{brutMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{filteredMag.ToString(System.Globalization.CultureInfo.InvariantCulture)},{((float)ForceMag).ToString(System.Globalization.CultureInfo.InvariantCulture)},{ressentiMag.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); //Ajouté le 01/04 à 14h30

            }

            //Ajouté le 21/03 à 12h10 - appliquer la force seulement si le bouton est pressé, sinon forcer à zéro
            // pour fonction SetConstantForceValues
            // //Commenté le 24/03 à 17h52 //Décommenté le 25/03 le 11h16

            // if (buttonPressed)
            // {
            //     setConstantForceValues(hapticPlugin.DeviceIdentifier, ForceDir, ForceMag);
            // }
            // else
            // {
            //     double[] zeroDir = new double[3] { 0, 0, 0 };
            //     setConstantForceValues(hapticPlugin.DeviceIdentifier, zeroDir, 0.0);
            // }



            // On ne fait plus l'appel ici : on stocke la force robot, l'application se fait dans Update (fusion)
            //if (buttonPressed)
            //    robotForce = force;
            //else
            //    robotForce = Vector3.zero;

            robotForce = force;   // on applique la force robot tout le temps (le suiveur sent aussi le contact) //Ajouté le 10/07

            // pour fonction SetForce
            //Ajouté le 24/03 à 17h54 //Commenté le 25/03 à 11h16 
            // if (buttonPressed)
            // {
            //     double[] forceArray = new double[3] { force.x, force.y, force.z };
            //     double[] torqueArray = new double[3] { 0, 0, 0 };
            //     setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
            // }
            // else
            // {
            //     double[] forceArray = new double[3] { 0, 0, 0 };
            //     double[] torqueArray = new double[3] { 0, 0, 0 };
            //     setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);
            // }


            //Ajouté le 19/03 à 09h24 // Commenté à 10h01
            // double[] forceArray = new double[3] { force.x, force.y, force.z };
            // double[] torqueArray = new double[3] { 0, 0, 0 };
            // setForce(hapticPlugin.DeviceIdentifier, forceArray, torqueArray);

        }
    }

    //Ajouté le 21/03 à 12h10
    private void ReceiveButton(RosMessageTypes.Std.Int32Msg msg)
    {
        buttonPressed = msg.data == 1;
    }



    private void ReceiveExpertButton(RosMessageTypes.Std.Int32Msg msg)
    {
        bool nowPressed = msg.data == 1;

        // Front montant : ré-ancrage (pas de saut)
        if (nowPressed && !expertButtonPressed && expertPlugin != null && hapticPlugin != null)
        {
            expertStart = expertPlugin.CurrentPosition;
            learnerStart = hapticPlugin.CurrentPosition;
            filteredCorr = Vector3.zero;
            prevCorr = Vector3.zero;
        }

        // Front descendant : on coupe la force (sinon la derniere force reste active)
        if (!nowPressed && expertButtonPressed && hapticPlugin != null)
        {
            filteredCorr = Vector3.zero;
            prevCorr = Vector3.zero;
            setConstantForceValues(hapticPlugin.DeviceIdentifier, new double[] { 1, 0, 0 }, 0.0);
        }

        expertButtonPressed = nowPressed;
    }

}