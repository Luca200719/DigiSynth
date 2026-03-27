using UnityEngine;

public class SpectrumAnalyzer : MonoBehaviour {
    AudioController audioController;

    public int spectrumBands = 32;
    public float spectrumWidth = 14.6f;
    public float spectrumHeight = 5f;
    public float barSpacing = 0.1f;
    public Material baseMaterial;

    public float smoothingSpeed = 10f;
    public float sensitivity = 25f;
    public float fallSpeed = 5f;

    public float maxEmissionIntensity = 2f;
    public float minEmissionIntensity = 0.5f;

    GameObject[] bandObjects;
    MeshFilter[] meshFilters;
    MeshRenderer[] meshRenderers;
    Material[] bandMaterials;
    Mesh[] bandMeshes;

    float[] spectrumData;
    float[] smoothedBands;
    bool[] inAudioThreshold;

    float timer = 0f;
    const float UPDATE_RATE = 1f / 60f;

    void Start() {
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();

        spectrumData = new float[spectrumBands];
        smoothedBands = new float[spectrumBands];
        inAudioThreshold = new bool[spectrumBands];

        CreateBandObjects();
    }

    void CreateBandObjects() {
        bandObjects = new GameObject[spectrumBands];
        meshFilters = new MeshFilter[spectrumBands];
        meshRenderers = new MeshRenderer[spectrumBands];
        bandMaterials = new Material[spectrumBands];
        bandMeshes = new Mesh[spectrumBands];

        float barWidth = (spectrumWidth - (spectrumBands - 1) * barSpacing) / spectrumBands;
        float startX = -spectrumWidth * 0.5f;

        for (int i = 0; i < spectrumBands; i++) {
            bandObjects[i] = new GameObject($"SpectrumBand_{i}");
            bandObjects[i].transform.SetParent(transform);

            meshFilters[i] = bandObjects[i].AddComponent<MeshFilter>();
            meshRenderers[i] = bandObjects[i].AddComponent<MeshRenderer>();

            bandMaterials[i] = new Material(baseMaterial);

            Color rainbowColor = GetColor(i);

            bandMaterials[i].SetColor("_BaseColor", rainbowColor * 0.3f);

            Color emissionColor = rainbowColor * minEmissionIntensity;
            bandMaterials[i].SetColor("_EmissionColor", emissionColor);

            bandMaterials[i].EnableKeyword("_EMISSION");

            meshRenderers[i].material = bandMaterials[i];

            bandMeshes[i] = CreateBandMesh(i, barWidth, startX);
            meshFilters[i].mesh = bandMeshes[i];

            spectrumData[i] = 0.1f;
        }
    }

    Mesh CreateBandMesh(int bandIndex, float barWidth, float startX) {
        Mesh mesh = new Mesh();
        mesh.name = $"SpectrumBand_{bandIndex}";

        float x = startX + bandIndex * (barWidth + barSpacing);
        float initialHeight = 0.1f;

        Vector3[] vertices = new Vector3[4] {
            new Vector3(0, 0, 0),
            new Vector3(barWidth, 0, 0),
            new Vector3(barWidth, initialHeight, 0),
            new Vector3(0, initialHeight, 0)
        };

        int[] triangles = new int[6] {
            0, 2, 1,
            0, 3, 2
        };

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

    void Update() {
        timer += Time.deltaTime;
        if (timer >= UPDATE_RATE) {
            timer = 0f;

            float[] rawSpectrumData = audioController.GetSpectrumData();
            ProcessSpectrumData(rawSpectrumData);
            UpdateBandVisuals();
        }
    }

    void ProcessSpectrumData(float[] rawData) {
        for (int i = 0; i < spectrumBands; i++) {
            if (i < rawData.Length) {
                float currentValue = rawData[i] * sensitivity;

                float enterThreshold = 0.001f;
                float exitThreshold = 0.00001f;

                if (!inAudioThreshold[i] && rawData[i] > enterThreshold) {
                    inAudioThreshold[i] = true;
                } else if (inAudioThreshold[i] && rawData[i] < exitThreshold) {
                    inAudioThreshold[i] = false;
                }

                if (inAudioThreshold[i] && currentValue > smoothedBands[i]) {
                    float riseSpeed = smoothingSpeed;

                    if (rawData[i] > 0.01f) {
                        riseSpeed *= 1.5f;
                    }

                    smoothedBands[i] = Mathf.Lerp(smoothedBands[i], currentValue, riseSpeed * Time.deltaTime);
                } else if (inAudioThreshold[i]) {
                    smoothedBands[i] = Mathf.Lerp(smoothedBands[i], currentValue, (smoothingSpeed * 0.3f) * Time.deltaTime);
                } else {
                    smoothedBands[i] = Mathf.Max(smoothedBands[i] - fallSpeed * 0.016f, 0f);
                }

                spectrumData[i] = smoothedBands[i];
            } else {
                spectrumData[i] = 0f;
            }
        }
    }

    void UpdateBandVisuals() {
        float pixelsPerUnit = 64f;
        float barWidth = (spectrumWidth - (spectrumBands - 1) * barSpacing) / spectrumBands;
        float startX = -spectrumWidth * 0.5f;

        for (int i = 0; i < spectrumBands; i++) {
            float normalizedIntensity = Mathf.Clamp01(spectrumData[i]);
            float scaledIntensity = Mathf.Pow(normalizedIntensity, 0.5f);
            float barHeight = Mathf.Clamp(scaledIntensity * spectrumHeight, 0.1f, spectrumHeight);

            UpdateBandMesh(i, barWidth, barHeight);

            Color rainbowColor = GetColor(i);

            float curvedIntensity = Mathf.Pow(normalizedIntensity, 0.25f);
            float emissionIntensity = Mathf.Lerp(minEmissionIntensity, maxEmissionIntensity, curvedIntensity);

            Color emissionColor = rainbowColor * emissionIntensity;
            bandMaterials[i].SetColor("_EmissionColor", emissionColor);

            float x = startX + (i * (barWidth + barSpacing));
            float pixelAlignedX = Mathf.Round(x * pixelsPerUnit) / pixelsPerUnit;

            bandObjects[i].transform.localPosition = new Vector3(pixelAlignedX, 0, 0);
        }
    }

    void UpdateBandMesh(int bandIndex, float barWidth, float barHeight) {
        Vector3[] vertices = bandMeshes[bandIndex].vertices;

        vertices[2] = new Vector3(barWidth, barHeight, 0);
        vertices[3] = new Vector3(0, barHeight, 0);

        bandMeshes[bandIndex].vertices = vertices;
        bandMeshes[bandIndex].RecalculateBounds();
    }

    void OnDestroy() {
        if (bandMaterials != null) {
            for (int i = 0; i < bandMaterials.Length; i++) {
                if (bandMaterials[i] != null) {
                    DestroyImmediate(bandMaterials[i]);
                }
            }
        }
    }

    Color GetColor(int bandIndex) {
        float progress = (float)bandIndex / (float)(spectrumBands - 1);

        Color[] keyColors = {
            new Color(1.0f, 0.2f, 0.2f),   // Red
            new Color(1.0f, 0.5f, 0.1f),   // Orange
            new Color(1.0f, 0.8f, 0.1f),   // Yellow
            new Color(0.5f, 1.0f, 0.2f),   // Yellow-Green
            new Color(0.2f, 1.0f, 0.5f),   // Green
            new Color(0.2f, 0.8f, 1.0f),   // Cyan
            new Color(0.3f, 0.5f, 1.0f),   // Blue
            new Color(0.6f, 0.3f, 1.0f)    // Violet
        };

        float scaledProgress = progress * (keyColors.Length - 1);
        int segmentIndex = Mathf.FloorToInt(scaledProgress);
        float segmentProgress = scaledProgress - segmentIndex;

        segmentIndex = Mathf.Clamp(segmentIndex, 0, keyColors.Length - 2);

        Color baseColor = Color.Lerp(keyColors[segmentIndex], keyColors[segmentIndex + 1], segmentProgress);

        return baseColor;
    }
}