using UnityEngine;

public class EQOverlay : MonoBehaviour {
    public Material baseMat;
    Material eqMat;

    public float lineThickness = 0.03f;
    public float spectrumWidth = 14.6f;
    public float spectrumHeight = 5f;
    public int spectrumBands = 32;

    EffectsController effectsController;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh eqMesh;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    float maxFrequency = 8000f;

    float timer = 0f;
    const float UPDATE_RATE = 1f / 30f;

    Color litEmission;
    Color litBase;
    Color unlitEmission;

    bool glowTimeActive;
    float glowTime;
    bool updateLight;
    bool isLit;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "EQ Overlay");
    }

    void Start() {
        eqMat = new Material(baseMat);

        litEmission = eqMat.GetColor("_EmissionColor") * 0.3f;
        litBase = eqMat.GetColor("_BaseColor");
        unlitEmission =  new Color(0.1f, 0.1f, 0.1f);

        eqMat.SetColor("_EmissionColor", unlitEmission);
        eqMat.SetColor("_BaseColor", Color.black);

        effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        eqMesh = new Mesh();
        eqMesh.name = "EQOverlay";
        meshFilter.mesh = eqMesh;

        meshRenderer.material = eqMat;

        InitializeEQMesh();
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= UPDATE_RATE) {
            timer = 0f;
            UpdateEQCurve();
        }

        if (updateLight) {
            glowTime = 0f;
            glowTimeActive = true;

            if (isLit) {
                eqMat.SetColor("_EmissionColor", litEmission);
                eqMat.SetColor("_BaseColor", litBase);
                UnlightEQ();
            } else {
                eqMat.SetColor("_EmissionColor", unlitEmission);
                eqMat.SetColor("_BaseColor", Color.black);
            }

            updateLight = false;
        }

        if (glowTimeActive) {
            glowTime += Time.deltaTime;
            if (glowTime >= 3f) {
                glowTimeActive = false;
                UnlightEQ();
            }
        }
    }

    void InitializeEQMesh() {
        int curvePoints = spectrumBands * 8;

        vertices = new Vector3[curvePoints * 4];
        triangles = new int[curvePoints * 6];
        uvs = new Vector2[vertices.Length];

        UpdateEQCurve();

        eqMesh.vertices = vertices;
        eqMesh.triangles = triangles;
        eqMesh.uv = uvs;
        eqMesh.RecalculateNormals();
        eqMesh.RecalculateBounds();
    }

    void UpdateEQCurve() {
        float lowGain = (effectsController.GetEffectValue(EffectsController.knobEffects["EQLow"]) - 0.5f) * 24f;
        float lowMidGain = (effectsController.GetEffectValue(EffectsController.knobEffects["EQLowMid"]) - 0.5f) * 24f;
        float highMidGain = (effectsController.GetEffectValue(EffectsController.knobEffects["EQHighMid"]) - 0.5f) * 24f;
        float highGain = (effectsController.GetEffectValue(EffectsController.knobEffects["EQHigh"]) - 0.5f) * 24f;
        float resonance = effectsController.GetEffectValue(EffectsController.knobEffects["EQResonance"]);

        int curvePoints = spectrumBands * 8;
        float stepX = spectrumWidth / (curvePoints - 1);
        float startX = -spectrumWidth * 0.5f;

        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < curvePoints - 1; i++) {
            float x1 = startX + i * stepX;
            float x2 = startX + (i + 1) * stepX;

            float freq1 = FrequencyAtPosition(x1);
            float freq2 = FrequencyAtPosition(x2);

            float gain1 = CalculateEQResponse(freq1, lowGain, lowMidGain, highMidGain, highGain, resonance);
            float gain2 = CalculateEQResponse(freq2, lowGain, lowMidGain, highMidGain, highGain, resonance);

            float y1 = (spectrumHeight * 0.5f) + (gain1 * spectrumHeight * 0.015f);
            float y2 = (spectrumHeight * 0.5f) + (gain2 * spectrumHeight * 0.015f);

            CreateLineQuad(new Vector2(x1, y1), new Vector2(x2, y2), lineThickness, ref vertIndex, ref triIndex);
        }

        eqMesh.vertices = vertices;
        eqMesh.RecalculateBounds();
    }

    float FrequencyAtPosition(float xPos) {
        float normalizedX = (xPos + spectrumWidth * 0.5f) / spectrumWidth;
        normalizedX = Mathf.Clamp01(normalizedX);

        float logMin = Mathf.Log10(20f);
        float logMax = Mathf.Log10(maxFrequency);
        float logFreq = Mathf.Lerp(logMin, logMax, normalizedX);

        return Mathf.Pow(10f, logFreq);
    }

    float CalculateEQResponse(float frequency, float lowGain, float lowMidGain, float highMidGain, float highGain, float resonance) {
        float response = 0f;

        float shelfQ = 0.5f + (resonance * 1.0f);
        float peakQ = 0.5f + (resonance * 4.5f);

        if (frequency <= 300f) {
            float bandwidth = 200f / shelfQ;
            float lowWeight = 1f / (1f + Mathf.Pow((frequency - 100f) / bandwidth, 2f));
            response += lowGain * lowWeight;
        }

        float lowMidBandwidth = 300f / peakQ;
        float lowMidWeight = 1f / (1f + Mathf.Pow((frequency - 400f) / lowMidBandwidth, 2f));
        response += lowMidGain * lowMidWeight;

        float highMidBandwidth = 1000f / peakQ;
        float highMidWeight = 1f / (1f + Mathf.Pow((frequency - 2000f) / highMidBandwidth, 2f));
        response += highMidGain * highMidWeight;

        if (frequency >= 4000f) {
            float bandwidth = 3000f / shelfQ;
            float highWeight = 1f / (1f + Mathf.Pow((frequency - 8000f) / bandwidth, 2f));
            response += highGain * highWeight;
        }

        return response;
    }

    void CreateLineQuad(Vector2 start, Vector2 end, float thickness, ref int vertIndex, ref int triIndex) {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * thickness * 0.5f;

        vertices[vertIndex + 0] = new Vector3(start.x - perpendicular.x, start.y - perpendicular.y, -0.01f);
        vertices[vertIndex + 1] = new Vector3(start.x + perpendicular.x, start.y + perpendicular.y, -0.01f);
        vertices[vertIndex + 2] = new Vector3(end.x + perpendicular.x, end.y + perpendicular.y, -0.01f);
        vertices[vertIndex + 3] = new Vector3(end.x - perpendicular.x, end.y - perpendicular.y, -0.01f);

        uvs[vertIndex + 0] = new Vector2(0, 0);
        uvs[vertIndex + 1] = new Vector2(0, 1);
        uvs[vertIndex + 2] = new Vector2(1, 1);
        uvs[vertIndex + 3] = new Vector2(1, 0);

        triangles[triIndex + 0] = vertIndex + 0;
        triangles[triIndex + 1] = vertIndex + 1;
        triangles[triIndex + 2] = vertIndex + 2;

        triangles[triIndex + 3] = vertIndex + 0;
        triangles[triIndex + 4] = vertIndex + 2;
        triangles[triIndex + 5] = vertIndex + 3;

        vertIndex += 4;
        triIndex += 6;
    }

    public void LightEQ() {
        updateLight = true;
        isLit = true;
    }

    public void UnlightEQ() {
        updateLight = true;
        isLit = false;
    }
}