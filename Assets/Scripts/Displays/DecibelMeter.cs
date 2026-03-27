using UnityEngine;
using TMPro;

public class DecibelMeter : MonoBehaviour {
    AudioController audioController;

    public float meterWidth = 8f;
    public float meterHeight = 1.2f;
    public float displayWidth = 2f;
    public float displayHeight = 1.2f;
    public float spacing = 0.3f;
    public Material baseMaterial;
    public float smoothingSpeed = 8f;
    public float silenceSmoothingSpeed = 20f;

    GameObject meterObject;
    MeshFilter meterMeshFilter;
    MeshRenderer meterMeshRenderer;
    Material meterMaterial;
    TextMeshProUGUI dbText;

    float currentDb = -74.8f;
    float smoothedDb = -74.8f;
    float lastNormalizedValue = -1f;
    float lastDisplayedDb = -74.8f;
    float timer = 0f;
    const float UPDATE_RATE = 1f / 30f;
    const float SILENT_THRESHOLD = -70f;

    void Start() {
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
        dbText = transform.parent.Find("Meter Label").GetComponent<TextMeshProUGUI>();
        CreateMeterVisuals();
    }

    void CreateMeterVisuals() {
        GameObject meterOutline = new GameObject("DecibelMeterOutline");
        meterOutline.transform.SetParent(transform);
        meterOutline.transform.localPosition = Vector3.zero;
        meterOutline.transform.localRotation = Quaternion.identity;
        meterOutline.transform.localScale = Vector3.one;

        MeshFilter meterOutlineMeshFilter = meterOutline.AddComponent<MeshFilter>();
        MeshRenderer meterOutlineMeshRenderer = meterOutline.AddComponent<MeshRenderer>();

        meterOutlineMeshRenderer.material = baseMaterial;
        meterOutlineMeshFilter.mesh = CreateOutlineMesh(meterWidth, meterHeight);

        meterObject = new GameObject("DecibelMeterFill");
        meterObject.transform.SetParent(transform);
        meterObject.transform.localPosition = Vector3.zero;
        meterObject.transform.localRotation = Quaternion.identity;
        meterObject.transform.localScale = Vector3.one;

        meterMeshFilter = meterObject.AddComponent<MeshFilter>();
        meterMeshRenderer = meterObject.AddComponent<MeshRenderer>();

        meterMaterial = new Material(baseMaterial);
        meterMaterial.EnableKeyword("_EMISSION");
        meterMeshRenderer.material = meterMaterial;
        meterMeshFilter.mesh = CreateQuadMesh(meterWidth, meterHeight);

        GameObject displayOutline = new GameObject("DecibelDisplayOutline");
        displayOutline.transform.SetParent(transform);
        displayOutline.transform.localPosition = new Vector3(meterWidth * 0.5f + spacing + displayWidth * 0.5f, 0, 0.01f);
        displayOutline.transform.localRotation = Quaternion.identity;
        displayOutline.transform.localScale = Vector3.one;

        MeshFilter outlineMeshFilter = displayOutline.AddComponent<MeshFilter>();
        MeshRenderer outlineMeshRenderer = displayOutline.AddComponent<MeshRenderer>();

        outlineMeshRenderer.material = baseMaterial;
        outlineMeshFilter.mesh = CreateOutlineMesh(displayWidth, displayHeight);
    }

    Mesh CreateQuadMesh(float width, float height) {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4] {
            new Vector3(-width * 0.5f, -height * 0.5f, 0),
            new Vector3(width * 0.5f, -height * 0.5f, 0),
            new Vector3(width * 0.5f, height * 0.5f, 0),
            new Vector3(-width * 0.5f, height * 0.5f, 0)
        };

        int[] triangles = new int[6] { 0, 2, 1, 0, 3, 2 };

        Vector2[] uvs = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Mesh CreateOutlineMesh(float width, float height) {
        Mesh mesh = new Mesh();
        float lineThickness = 0.08f;
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        Vector3[] vertices = new Vector3[8] {
            new Vector3(-halfW, -halfH, 0),
            new Vector3(halfW, -halfH, 0),
            new Vector3(halfW, halfH, 0),
            new Vector3(-halfW, halfH, 0),
            new Vector3(-halfW + lineThickness, -halfH + lineThickness, 0),
            new Vector3(halfW - lineThickness, -halfH + lineThickness, 0),
            new Vector3(halfW - lineThickness, halfH - lineThickness, 0),
            new Vector3(-halfW + lineThickness, halfH - lineThickness, 0)
        };

        int[] triangles = new int[24] {
            0, 5, 1,
            0, 4, 5,
            1, 6, 2,
            1, 5, 6,
            2, 7, 3,
            2, 6, 7,
            3, 4, 0,
            3, 7, 4
        };

        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++) {
            uvs[i] = new Vector2(0, 0);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= UPDATE_RATE) {
            timer = 0f;

            float peakLevel = audioController.GetCurrentPeakLevel();
            currentDb = 20f * Mathf.Log10(Mathf.Max(peakLevel, 0.00001f));
            currentDb = Mathf.Clamp(currentDb, -74.8f, 0f);

            float targetDb = currentDb;
            if (currentDb <= SILENT_THRESHOLD) {
                targetDb = -74.8f;
            }

            float lerpSpeed = (currentDb <= SILENT_THRESHOLD) ? silenceSmoothingSpeed : smoothingSpeed;
            smoothedDb = Mathf.Lerp(smoothedDb, targetDb, lerpSpeed * Time.deltaTime);

            if (currentDb <= SILENT_THRESHOLD && smoothedDb > -74.8f && smoothedDb < -74.7f) {
                smoothedDb = -74.8f;
            }

            UpdateMeterVisuals();
        }
    }

    void UpdateMeterVisuals() {
        float normalizedDb = (smoothedDb + 74.8f) / 74.8f;
        normalizedDb = Mathf.Clamp01(normalizedDb);
        float curvedValue = Mathf.Pow(normalizedDb, 4f);

        if (Mathf.Abs(curvedValue - lastNormalizedValue) < 0.01f && Mathf.Abs(smoothedDb - lastDisplayedDb) < 0.1f) {
            return;
        }
        lastNormalizedValue = curvedValue;
        lastDisplayedDb = smoothedDb;

        dbText.text = smoothedDb.ToString("F1");

        meterObject.transform.localScale = new Vector3(-curvedValue, 1f, 1f);
        meterObject.transform.localPosition = new Vector3(meterWidth * 0.5f - (curvedValue * meterWidth * 0.5f), 0, 0.01f);

        Color meterColor;
        if (curvedValue < 0.6f) {
            float t = Mathf.Pow(curvedValue / 0.6f, 5f);
            meterColor = Color.Lerp(new Color(0.2f, 1f, 0.5f), new Color(1f, 0.8f, 0.1f), t);
        } else {
            float t = Mathf.Pow(curvedValue / 0.4f, 0.25f);
            meterColor = Color.Lerp(new Color(1f, 0.8f, 0.1f), new Color(1f, 0.2f, 0.2f), t);
        }

        meterMaterial.SetColor("_BaseColor", meterColor * 0.6f);
        float emissionIntensity = Mathf.Lerp(0.5f, 2f, curvedValue);
        meterMaterial.SetColor("_EmissionColor", meterColor * emissionIntensity);
    }

    void OnDestroy() {
        if (meterMaterial != null) {
            DestroyImmediate(meterMaterial);
        }
    }
}