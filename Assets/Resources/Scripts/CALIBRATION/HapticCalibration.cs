using UnityEngine;
using System.Runtime.InteropServices;

public class HapticCalibration : MonoBehaviour
{
    [DllImport("HapticsDirect")]
    public static extern void setConstantForceValues(string configName, double[] direction, double magnitude);

    // --- Références aux deux bras (à glisser dans l'inspecteur) ---
    public HapticPlugin expertPlugin;
    public HapticPlugin learnerPlugin;

    // --- Point cible physique : centre du workspace device (en mm) ---
    public Vector3 targetPoint = new Vector3(0f, 0f, 0f);

    // --- Paramètres du ressort de calibration ---
    public float calibStiffness = 10f;
    public float calibDamping = 2f;
    public float calibMaxForce = 1.0f;

    // --- Condition de validation ---
    public float distanceThreshold = 5f;   // en mm

    // --- État partagé : LE booléen que les autres scripts regardent ---
    public bool teleopActive = false;

    // --- Zéro commun figé à la validation ---
    [HideInInspector] public Vector3 expertZero;
    [HideInInspector] public Vector3 learnerZero;

    // --- Mémorisation position précédente (pour la vitesse) ---
    private Vector3 prevExpertPos = Vector3.zero;
    private Vector3 prevLearnerPos = Vector3.zero;


    private float startDelay = 1.0f; //Ajouté le 09/07 à 15h00
    private float lastLogTime = 0f;


    //Ajouté le 09/07 à 15h00

    void Start()
    {
        if (expertPlugin != null)  prevExpertPos  = expertPlugin.CurrentPosition;
        if (learnerPlugin != null) prevLearnerPos = learnerPlugin.CurrentPosition;
    }

    void Update()
    {
        if (teleopActive)
            return;   // calibration terminée : ce script ne fait plus rien

        if (Time.timeSinceLevelLoad < startDelay) return;

        // 1) Tirer les deux stylets vers la cible
        ApplyCalibrationForce(expertPlugin, ref prevExpertPos);
        ApplyCalibrationForce(learnerPlugin, ref prevLearnerPos);

        // 2) Condition de distance
        if (expertPlugin == null || learnerPlugin == null) return;

        // float distExpert  = Vector3.Distance(expertPlugin.CurrentPosition, targetPoint);
        // float distLearner = Vector3.Distance(learnerPlugin.CurrentPosition, targetPoint);

        // bool expertReady  = distExpert  < distanceThreshold;
        // bool learnerReady = distLearner < distanceThreshold;

        float ecart = Vector3.Distance(expertPlugin.CurrentPosition, learnerPlugin.CurrentPosition);
        bool brasAlignes = ecart < distanceThreshold;

        if (Time.time - lastLogTime > 0.5f)
        {
            Debug.Log($"[CALIB] Expert:{expertPlugin.CurrentPosition.ToString("F1")} | Learner:{learnerPlugin.CurrentPosition.ToString("F1")} | ecart={ecart:F1}mm");
            lastLogTime = Time.time;
        }

        // 3) Validation : Espace + les deux prêts
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // if (expertReady && learnerReady)
            if (brasAlignes)
            {
                expertZero  = expertPlugin.CurrentPosition;
                learnerZero = learnerPlugin.CurrentPosition;
                teleopActive = true;

                // On coupe la force de calibration sur les deux bras
                setConstantForceValues(expertPlugin.DeviceIdentifier,  new double[] { 1, 0, 0 }, 0.0);
                setConstantForceValues(learnerPlugin.DeviceIdentifier, new double[] { 1, 0, 0 }, 0.0);

                Debug.Log("Calibration validée - téléop active");
            }
            else
            {
                // Debug.Log($"Pas encore prêt. Expert: {distExpert:F1}mm, Learner: {distLearner:F1}mm (seuil {distanceThreshold}mm)");
                Debug.Log($"Pas encore prêt. Écart entre les bras: {ecart:F1}mm (seuil {distanceThreshold}mm)");
            }
        }
    }

    void ApplyCalibrationForce(HapticPlugin plugin, ref Vector3 prevPos)
    {
        if (plugin == null || plugin.DeviceHHD < 0) return;

        // Écart vers la cible (cible - position actuelle) -> force attire vers la cible
        Vector3 error = targetPoint - plugin.CurrentPosition;   // mm

        // Ressort (mm -> m)
        Vector3 force = calibStiffness * (error / 1000f);

        // Amortissement (freine selon la vitesse)
        Vector3 vel = (plugin.CurrentPosition - prevPos) / 1000f / Time.deltaTime;
        force -= calibDamping * vel;
        prevPos = plugin.CurrentPosition;

        // Plafond de sécurité
        if (force.magnitude > calibMaxForce)
            force = force.normalized * calibMaxForce;

        // Application au device
        if (force.magnitude < 0.0001f)
        {
            setConstantForceValues(plugin.DeviceIdentifier, new double[] { 1, 0, 0 }, 0.0);
        }
        else
        {
            Vector3 dir = force.normalized;
            setConstantForceValues(plugin.DeviceIdentifier, new double[] { dir.x, dir.y, dir.z }, force.magnitude);
        }
    }
}