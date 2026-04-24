using UnityEngine;

public class ForceGraph : MonoBehaviour
{
    [Header("Graphe")]

    public HapticPlugin hapticPlugin;
    private float[] _sdkHistory;
    public int historySize = 300;
    public float maxForceDisplay = 20f;
    public float graphHeight = 200f;

    private float[] _brutHistory;
    private float[] _appliedHistory;
    private int _index = 0;
    private bool _initialized = false;

    private Material _glMat;

    private float _displayBrut = 0f;
    private float _displayApplied = 0f;
    private float _displaySdk = 0f;
    private float _displayAlpha = 0.1f; // lissage pour l'affichage

    //Ajouté le 31/03 à 15h10
    private float[] _filteredHistory;
    private float _displayFiltered = 0f;

    //Ajouté le 31/03 à 15h30
    private bool showBrut = true;
    private bool showFiltered = true;
    private bool showApplied = true;
    private bool showSdk = true;

    void Start()
    {
        _brutHistory    = new float[historySize];
        _appliedHistory = new float[historySize];
        _sdkHistory = new float[historySize];
        _filteredHistory = new float[historySize]; //Ajouté le 31/03 à 15h10

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        _glMat = new Material(shader);
        _glMat.hideFlags = HideFlags.HideAndDontSave;
        _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glMat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
        _glMat.SetInt("_ZWrite", 0);

        _initialized = true;
    }

    public void PushSample(float brutMag, float filteredMag, float appliedMag, float sdkMag) //Ajouté "float filteredMag" le 31/03 à 15h10
    {
        if (!_initialized) return;
        _brutHistory[_index]    = brutMag;
        _filteredHistory[_index] = filteredMag; //Ajouté le 31/03 à 15h10
        _appliedHistory[_index] = appliedMag;
        _sdkHistory[_index]     = sdkMag;
        _index = (_index + 1) % historySize;
        _displayBrut    = _displayAlpha * brutMag    + (1 - _displayAlpha) * _displayBrut;
        _displayFiltered = _displayAlpha * filteredMag + (1 - _displayAlpha) * _displayFiltered; //Ajouté le 31/03 à 15h10
        _displayApplied = _displayAlpha * appliedMag + (1 - _displayAlpha) * _displayApplied;
        _displaySdk     = _displayAlpha * sdkMag     + (1 - _displayAlpha) * _displaySdk;
    }

    void OnGUI()
    {
        //if (Event.current.type != EventType.Repaint) return; //Commenté le 31/03 à 15h40
        bool isRepaint = Event.current.type == EventType.Repaint; //Ajouté le 31/03 à 15h40

        float headerHeight = 60f;
        float totalHeight  = headerHeight + graphHeight;
        float x = 0f;
        float y = Screen.height - totalHeight;
        float w = Screen.width;

        float curveX = x + 80f;
        float curveW = w - 80f;

        // --- Fond principal ---
        if (isRepaint) //Boucle if Ajouté le 31/03 à 15h40
        {
            GUI.color = new Color(0f, 0f, 0f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, totalHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // --- Fond colonne label ---
            GUI.color = new Color(0f, 0f, 0f, 0.95f);
            GUI.DrawTexture(new Rect(x, y + headerHeight, 80f, graphHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // --- Titre ---
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.normal.textColor = Color.white;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize  = 18;
        //titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(x + 25f, y + 8f, 350f, 30f), "Force Debug — magnitudes (N)", titleStyle);

        // --- Légende ---
        GUIStyle legendStyle = new GUIStyle(GUI.skin.label);
        legendStyle.fontSize = 18;

        //Commenté le 31/03 à 15h10
        // legendStyle.normal.textColor = Color.red;
        // GUI.Label(new Rect(500f, y + 8f, 200f, 30f), $"— Force brute: {_displayBrut:F2}N", legendStyle);
        // legendStyle.normal.textColor = new Color(1f, 0.5f, 0f); //Ajouté le 31/03 à 15h10
        // GUI.Label(new Rect(710f, y + 8f, 250f, 30f), $"— Force lissée IIR: {_displayFiltered:F2}N", legendStyle); //Ajouté le 31/03 à 15h10
        // legendStyle.normal.textColor = Color.green;
        // GUI.Label(new Rect(710f, y + 8f, 220f, 30f), $"— Force filtrée: {_displayApplied:F2}N", legendStyle);
        // legendStyle.normal.textColor = Color.yellow;
        // GUI.Label(new Rect(940f, y + 8f, 220f, 30f), $"— Force ressentie: {_displaySdk:F2}N", legendStyle);

        //Ajouté le 31/03 à 15h10 décaler les autres labels pour faire de la place à la légende de la force lissée
        // legendStyle.normal.textColor = Color.red;
        // GUI.Label(new Rect(500f, y + 8f, 200f, 30f), $"— Force brute: {_displayBrut:F2}N", legendStyle);
        // legendStyle.normal.textColor = new Color(1f, 0.5f, 0f);
        // GUI.Label(new Rect(720f, y + 8f, 250f, 30f), $"— Force lissée IIR: {_displayFiltered:F2}N", legendStyle);
        // legendStyle.normal.textColor = Color.green;
        // GUI.Label(new Rect(990f, y + 8f, 220f, 30f), $"— Force filtrée: {_displayApplied:F2}N", legendStyle);
        // legendStyle.normal.textColor = Color.yellow;
        // GUI.Label(new Rect(1230f, y + 8f, 220f, 30f), $"— Force ressentie: {_displaySdk:F2}N", legendStyle);

        //Ajouté le 31/03 à 15h30 - cases à cocher pour afficher/masquer les courbes
        showBrut = GUI.Toggle(new Rect(463f, y + 15f, 24f, 24f), showBrut, "");
        legendStyle.normal.textColor = showBrut ? Color.red : new Color(1f, 0f, 0f, 0.3f);
        GUI.Label(new Rect(468f, y + 11f, 230f, 30f), $"— Force brute: {_displayBrut:F2}N", legendStyle);

        showFiltered = GUI.Toggle(new Rect(723f, y + 15f, 24f, 24f), showFiltered, "");
        legendStyle.normal.textColor = showFiltered ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.5f, 0f, 0.3f);
        GUI.Label(new Rect(728f, y + 11f, 250f, 30f), $"— Force lissée IIR: {_displayFiltered:F2}N", legendStyle);

        showApplied = GUI.Toggle(new Rect(1003f, y + 15f, 24f, 24f), showApplied, "");
        legendStyle.normal.textColor = showApplied ? Color.green : new Color(0f, 1f, 0f, 0.3f);
        GUI.Label(new Rect(1008f, y + 11f, 240f, 30f), $"— Force scalée: {_displayApplied:F2}N", legendStyle);

        showSdk = GUI.Toggle(new Rect(1273f, y + 15f, 24f, 24f), showSdk, "");
        legendStyle.normal.textColor = showSdk ? Color.yellow : new Color(1f, 1f, 0f, 0.3f);
        GUI.Label(new Rect(1278f, y + 11f, 260f, 30f), $"— Force ressentie: {_displaySdk:F2}N", legendStyle);


        // --- Labels axe Y ---
        GUIStyle axisStyle = new GUIStyle(GUI.skin.label);
        axisStyle.fontSize = 18;
        axisStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        float graphY = y + headerHeight;
        //GUI.Label(new Rect(x + 2f, graphY, 40f, 16f), $"{maxForceDisplay:F0}N", axisStyle);
        //GUI.Label(new Rect(x + 2f, graphY + graphHeight - 18f, 40f, 16f), "0N", axisStyle);
        //GUI.Label(new Rect(x + 2f, graphY, 78f, 24f), $"{maxForceDisplay:F0}N", axisStyle);
        GUI.Label(new Rect(x + 2f, graphY - 10f, 78f, 24f), $"{maxForceDisplay:F0}N", axisStyle);
        GUI.Label(new Rect(x + 2f, graphY + graphHeight - 34f, 78f, 24f), "0N", axisStyle);

        // --- GL ---
        if (isRepaint) //Boucle if Ajouté le 31/03 à 15h40
        {
            _glMat.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            // Grille horizontale
            GL.Begin(GL.LINES);
            GL.Color(new Color(0.4f, 0.4f, 0.4f, 0.5f));
            for (int i = 0; i <= 4; i++)
            {
                float gy = graphY + (graphHeight * i / 4f);
                GL.Vertex3(curveX,          gy, 0);
                GL.Vertex3(curveX + curveW, gy, 0);
            }
            GL.End();

            // Ligne pointillée 3.3N
            float maxDeviceForce = 3.3f;
            //float lineY = graphY + graphHeight - (maxDeviceForce / maxForceDisplay) * (graphHeight - 10f);
            float paddingBottom = 20f;
            float lineY = graphY + graphHeight - paddingBottom - (maxDeviceForce / maxForceDisplay) * (graphHeight - paddingBottom);
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0f, 0f, 0.8f));
            int dashCount = 40;
            for (int i = 0; i < dashCount; i++)
            {
                if (i % 2 == 0)
                {
                    float px1 = curveX + (i / (float)dashCount) * curveW;
                    float px2 = curveX + ((i + 1) / (float)dashCount) * curveW;
                    GL.Vertex3(px1, lineY, 0);
                    GL.Vertex3(px2, lineY, 0);
                }
            }
            GL.End();

            //Commenté le 31/03 à 15h30
            // DrawCurve(_brutHistory, _index, curveX, graphY, curveW, Color.red);
            // DrawCurve(_filteredHistory, _index, curveX, graphY, curveW, new Color(1f, 0.5f, 0f)); //Ajouté le 31/03 à 15h10
            // DrawCurve(_appliedHistory, _index, curveX, graphY, curveW, Color.green);
            // DrawCurve(_sdkHistory, _index, curveX, graphY, curveW, Color.yellow);

            //Ajouté le 31/03 à 15h30 - afficher/masquer les courbes selon les cases cochées
            if (showBrut)     DrawCurve(_brutHistory, _index, curveX, graphY, curveW, Color.red);
            if (showFiltered) DrawCurve(_filteredHistory, _index, curveX, graphY, curveW, new Color(1f, 0.5f, 0f));
            if (showApplied)  DrawCurve(_appliedHistory, _index, curveX, graphY, curveW, Color.green);
            if (showSdk)      DrawCurve(_sdkHistory, _index, curveX, graphY, curveW, Color.yellow);


            GL.PopMatrix();
        }

    }

    private void DrawCurve(float[] history, int currentIndex, float x, float graphY, float w, Color color)
    {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(new Color(color.r, color.g, color.b, 1f));

        for (int i = 0; i < historySize; i++)
        {
            int idx = (currentIndex + i) % historySize;
            float val = Mathf.Clamp(history[idx], 0f, maxForceDisplay);
            float px  = x + (i / (float)(historySize - 1)) * w;
            float paddingBottom = 20f;
            float py = graphY + graphHeight - paddingBottom - (val / maxForceDisplay) * (graphHeight - paddingBottom);
            GL.Vertex3(px, py, 0);
        }

        GL.End();
    }
}