using UnityEngine;

public class EnvelopeOverlay : MonoBehaviour {
    public Material lineMat;
    public Material dotMat;

    Material[] sectionMaterials;
    Material connectionPointMaterial;

    public float lineThickness = 0.03f;
    public float graphWidth = 14.6f;
    public float graphHeight = 5f;
    public int curveResolution = 512;
    public float margin;

    public float maxEmissionIntensity = 2f;
    public float minEmissionIntensity = 0.5f;
    public float connectionPointRadius = 0.08f;
    public int connectionPointSegments = 16;

    float visualMargin;
    float startX;
    float endX;
    float outerSectionWidth;
    float innerSectionWidth;

    EffectsController effectsController;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh mainMesh;

    GameObject[] connectionPointObjects;
    MeshFilter[] connectionPointFilters;
    MeshRenderer[] connectionPointRenderers;
    Mesh[] connectionPointMeshes;

    Vector3[] allVertices;
    int[] allTriangles;
    Vector2[] allUvs;
    int[] sectionVertexCounts;
    int[] sectionTriangleCounts;
    int[] sectionVertexOffsets;
    int[] sectionTriangleOffsets;

    float timer = 0f;
    const float UPDATE_RATE = 1f / 30f;

    float lastAttack = -1f;
    float lastDecay = -1f;
    float lastSustain = -1f;
    float lastRelease = -1f;

    Vector2[] connectionPoints = new Vector2[5];

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Envelope Overlay");
    }

    void Start() {
        effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        mainMesh = new Mesh();
        mainMesh.name = "EnvelopeOverlay";
        meshFilter.mesh = mainMesh;

        sectionMaterials = new Material[4];
        string[] sectionNames = { "Attack", "Decay", "Sustain", "Release" };
        Color[] sectionColors = {
            GetSpectrumColor(0.0f),
            GetSpectrumColor(0.33f),
            GetSpectrumColor(0.66f),
            GetSpectrumColor(1.0f)
        };

        for (int i = 0; i < 4; i++) {
            sectionMaterials[i] = new Material(lineMat);
            Color sectionColor = sectionColors[i];
            sectionMaterials[i].SetColor("_BaseColor", sectionColor * 0.3f);
            Color emissionColor = sectionColor * minEmissionIntensity;
            sectionMaterials[i].SetColor("_EmissionColor", emissionColor);
        }
        meshRenderer.materials = sectionMaterials;

        CreateConnectionPoints();

        innerSectionWidth = graphWidth / 4f;

        visualMargin = graphWidth * margin;
        startX = -graphWidth * 0.5f + visualMargin;
        endX = graphWidth * 0.5f - visualMargin;
        outerSectionWidth = (endX - startX) / 4f;

        InitializeEnvelopeMeshes();
    }

    void CreateConnectionPoints() {
        connectionPointObjects = new GameObject[5];
        connectionPointFilters = new MeshFilter[5];
        connectionPointRenderers = new MeshRenderer[5];
        connectionPointMeshes = new Mesh[5];

        connectionPointMaterial = new Material(dotMat);
        connectionPointMaterial.SetColor("_BaseColor", Color.white * 0.5f);
        connectionPointMaterial.SetColor("_EmissionColor", Color.white * 2f);

        string[] pointNames = { "Start", "AttackPeak", "DecayEnd", "SustainEnd", "ReleaseEnd" };

        for (int i = 0; i < 5; i++) {
            connectionPointObjects[i] = new GameObject($"ConnectionPoint_{pointNames[i]}");
            connectionPointObjects[i].transform.SetParent(transform, false);
            connectionPointObjects[i].transform.localPosition = Vector3.zero;
            connectionPointObjects[i].transform.localRotation = Quaternion.identity;
            connectionPointObjects[i].transform.localScale = Vector3.one;

            connectionPointFilters[i] = connectionPointObjects[i].AddComponent<MeshFilter>();
            connectionPointRenderers[i] = connectionPointObjects[i].AddComponent<MeshRenderer>();
            connectionPointRenderers[i].material = connectionPointMaterial;

            connectionPointMeshes[i] = new Mesh();
            connectionPointMeshes[i].name = $"ConnectionPoint_{pointNames[i]}";
            connectionPointFilters[i].mesh = connectionPointMeshes[i];
        }
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= UPDATE_RATE) {
            timer = 0f;

            float attack = effectsController.GetEffectValue(EffectsController.knobEffects["Attack"]);
            float decay = effectsController.GetEffectValue(EffectsController.knobEffects["Decay"]);
            float sustain = effectsController.GetEffectValue(EffectsController.knobEffects["Sustain"]);
            float release = effectsController.GetEffectValue(EffectsController.knobEffects["Release"]);

            if (attack != lastAttack || decay != lastDecay ||
                sustain != lastSustain || release != lastRelease) {

                lastAttack = attack;
                lastDecay = decay;
                lastSustain = sustain;
                lastRelease = release;

                UpdateEnvelopeCurve(attack, decay, sustain, release);
                UpdateSectionEmission(attack, decay, sustain, release);
                UpdateConnectionPoints();
            }
        }
    }

    void InitializeEnvelopeMeshes() {
        int pointsPerSection = curveResolution / 4;

        sectionVertexCounts = new int[4];
        sectionTriangleCounts = new int[4];
        sectionVertexOffsets = new int[4];
        sectionTriangleOffsets = new int[4];

        for (int i = 0; i < 4; i++) {
            sectionVertexCounts[i] = pointsPerSection * 4;
            sectionTriangleCounts[i] = pointsPerSection * 6;
        }

        int totalVertices = 0;
        int totalTriangles = 0;
        for (int i = 0; i < 4; i++) {
            sectionVertexOffsets[i] = totalVertices;
            sectionTriangleOffsets[i] = totalTriangles;
            totalVertices += sectionVertexCounts[i];
            totalTriangles += sectionTriangleCounts[i];
        }

        allVertices = new Vector3[totalVertices];
        allTriangles = new int[totalTriangles];
        allUvs = new Vector2[totalVertices];

        UpdateEnvelopeCurve(0.333f, 0.492f, 0.6f, 0.726f);

        mainMesh.vertices = allVertices;
        mainMesh.uv = allUvs;

        mainMesh.subMeshCount = 4;
        for (int i = 0; i < 4; i++) {
            int triStart = sectionTriangleOffsets[i];
            int triCount = sectionTriangleCounts[i];
            int[] subTriangles = new int[triCount];
            System.Array.Copy(allTriangles, triStart, subTriangles, 0, triCount);
            mainMesh.SetTriangles(subTriangles, i);
        }

        mainMesh.RecalculateNormals();
        mainMesh.RecalculateBounds();

        UpdateConnectionPoints();
    }

    void UpdateEnvelopeCurve(float attack, float decay, float sustain, float release) {
        int pointsPerSection = curveResolution / 4;
        float normalizedStartX = startX - (visualMargin / 2);

        connectionPoints[0] = new Vector2(normalizedStartX, 0f);
        connectionPoints[1] = new Vector2(normalizedStartX + outerSectionWidth, graphHeight);
        connectionPoints[2] = new Vector2(normalizedStartX + outerSectionWidth + innerSectionWidth, sustain * graphHeight);
        connectionPoints[3] = new Vector2(normalizedStartX + outerSectionWidth + (innerSectionWidth * 2), sustain * graphHeight);
        connectionPoints[4] = new Vector2(normalizedStartX + (outerSectionWidth + innerSectionWidth) * 2, 0f);

        UpdateAttackSection(normalizedStartX, outerSectionWidth, pointsPerSection, connectionPoints[0], connectionPoints[1]);
        UpdateDecaySection(normalizedStartX + outerSectionWidth, innerSectionWidth, pointsPerSection, sustain, connectionPoints[1], connectionPoints[2]);
        UpdateSustainSection(normalizedStartX + outerSectionWidth + innerSectionWidth, innerSectionWidth, pointsPerSection, sustain, connectionPoints[2], connectionPoints[3]);
        UpdateReleaseSection(normalizedStartX + outerSectionWidth + (innerSectionWidth * 2), outerSectionWidth, pointsPerSection, sustain, connectionPoints[3], connectionPoints[4]);

        mainMesh.vertices = allVertices;
        mainMesh.RecalculateBounds();
    }

    void UpdateAttackSection(float startXPos, float sectionWidth, int points, Vector2 startPoint, Vector2 endPoint) {
        int vertIndex = sectionVertexOffsets[0];
        int triIndex = sectionTriangleOffsets[0];
        float norm = 1f - Mathf.Exp(-4f);

        for (int i = 0; i < points - 1; i++) {
            float t1 = i / (float)(points - 1);
            float t2 = (i + 1) / (float)(points - 1);

            float x1 = Mathf.Lerp(startPoint.x, endPoint.x, t1);
            float x2 = Mathf.Lerp(startPoint.x, endPoint.x, t2);

            float y1 = Mathf.Lerp(startPoint.y, endPoint.y, (1f - Mathf.Exp(-t1 * 4f)) / norm);
            float y2 = Mathf.Lerp(startPoint.y, endPoint.y, (1f - Mathf.Exp(-t2 * 4f)) / norm);

            CreateLineQuad(new Vector2(x1, y1), new Vector2(x2, y2), lineThickness, ref vertIndex, ref triIndex);
        }
    }

    void UpdateDecaySection(float startXPos, float sectionWidth, int points, float sustain, Vector2 startPoint, Vector2 endPoint) {
        int vertIndex = sectionVertexOffsets[1];
        int triIndex = sectionTriangleOffsets[1];

        for (int i = 0; i < points - 1; i++) {
            float t1 = i / (float)(points - 1);
            float t2 = (i + 1) / (float)(points - 1);

            float x1 = Mathf.Lerp(startPoint.x, endPoint.x, t1);
            float x2 = Mathf.Lerp(startPoint.x, endPoint.x, t2);

            float y1 = Mathf.Lerp(startPoint.y, endPoint.y, 1f - Mathf.Exp(-t1 * 5f));
            float y2 = Mathf.Lerp(startPoint.y, endPoint.y, 1f - Mathf.Exp(-t2 * 5f));

            CreateLineQuad(new Vector2(x1, y1), new Vector2(x2, y2), lineThickness, ref vertIndex, ref triIndex);
        }
    }

    void UpdateSustainSection(float startXPos, float sectionWidth, int points, float sustain, Vector2 startPoint, Vector2 endPoint) {
        int vertIndex = sectionVertexOffsets[2];
        int triIndex = sectionTriangleOffsets[2];

        for (int i = 0; i < points - 1; i++) {
            float t1 = i / (float)(points - 1);
            float t2 = (i + 1) / (float)(points - 1);

            float x1 = Mathf.Lerp(startPoint.x, endPoint.x, t1);
            float x2 = Mathf.Lerp(startPoint.x, endPoint.x, t2);

            CreateLineQuad(new Vector2(x1, startPoint.y), new Vector2(x2, startPoint.y), lineThickness, ref vertIndex, ref triIndex);
        }
    }

    void UpdateReleaseSection(float startXPos, float sectionWidth, int points, float sustain, Vector2 startPoint, Vector2 endPoint) {
        int vertIndex = sectionVertexOffsets[3];
        int triIndex = sectionTriangleOffsets[3];

        for (int i = 0; i < points - 1; i++) {
            float t1 = i / (float)(points - 1);
            float t2 = (i + 1) / (float)(points - 1);

            float x1 = Mathf.Lerp(startPoint.x, endPoint.x, t1);
            float x2 = Mathf.Lerp(startPoint.x, endPoint.x, t2);

            float y1 = Mathf.Lerp(endPoint.y, startPoint.y, Mathf.Exp(-t1 * 5f));
            float y2 = Mathf.Lerp(endPoint.y, startPoint.y, Mathf.Exp(-t2 * 5f));

            CreateLineQuad(new Vector2(x1, y1), new Vector2(x2, y2), lineThickness, ref vertIndex, ref triIndex);
        }
    }

    void UpdateConnectionPoints() {
        float scaledRadius = connectionPointRadius * transform.localScale.x;
        for (int i = 0; i < 5; i++) {
            connectionPointObjects[i].transform.localPosition = new Vector3(connectionPoints[i].x, connectionPoints[i].y, 0f);
            CreateCircleMesh(Vector2.zero, scaledRadius, connectionPointSegments, connectionPointMeshes[i]);
        }
    }

    void CreateCircleMesh(Vector2 center, float radius, int segments, Mesh mesh) {
        Vector3[] verts = new Vector3[segments + 1];
        int[] tris = new int[segments * 3];
        Vector2[] uvs = new Vector2[segments + 1];

        verts[0] = new Vector3(center.x, center.y, -0.0001f);
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++) {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            verts[i + 1] = new Vector3(x, y, -0.0001f);
            uvs[i + 1] = new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f);
        }

        for (int i = 0; i < segments; i++) {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = (i + 1) % segments + 1;
            tris[i * 3 + 2] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void UpdateSectionEmission(float attack, float decay, float sustain, float release) {
        float[] values = { attack, decay, sustain, release };

        Color[] sectionColors = {
            GetSpectrumColor(0.0f),
            GetSpectrumColor(0.33f),
            GetSpectrumColor(0.66f),
            GetSpectrumColor(1.0f)
        };

        for (int i = 0; i < 4; i++) {
            Color sectionColor = sectionColors[i];
            float normalizedIntensity = values[i];
            float curvedIntensity = Mathf.Pow(normalizedIntensity, 0.25f);
            float emissionIntensity = Mathf.Lerp(minEmissionIntensity, maxEmissionIntensity, curvedIntensity);

            Color emissionColor = sectionColor * emissionIntensity;
            sectionMaterials[i].SetColor("_EmissionColor", emissionColor);
        }
    }

    void CreateLineQuad(Vector2 start, Vector2 end, float thickness, ref int vertIndex, ref int triIndex) {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * thickness * 0.5f;

        float overlapAmount = 0.001f;
        Vector2 extendedStart = start - direction * overlapAmount;
        Vector2 extendedEnd = end + direction * overlapAmount;

        allVertices[vertIndex + 0] = new Vector3(extendedStart.x - perpendicular.x, extendedStart.y - perpendicular.y, 0f);
        allVertices[vertIndex + 1] = new Vector3(extendedStart.x + perpendicular.x, extendedStart.y + perpendicular.y, 0f);
        allVertices[vertIndex + 2] = new Vector3(extendedEnd.x + perpendicular.x, extendedEnd.y + perpendicular.y, 0f);
        allVertices[vertIndex + 3] = new Vector3(extendedEnd.x - perpendicular.x, extendedEnd.y - perpendicular.y, 0f);

        allUvs[vertIndex + 0] = new Vector2(0, 0);
        allUvs[vertIndex + 1] = new Vector2(0, 1);
        allUvs[vertIndex + 2] = new Vector2(1, 1);
        allUvs[vertIndex + 3] = new Vector2(1, 0);

        allTriangles[triIndex + 0] = vertIndex + 0;
        allTriangles[triIndex + 1] = vertIndex + 1;
        allTriangles[triIndex + 2] = vertIndex + 2;

        allTriangles[triIndex + 3] = vertIndex + 0;
        allTriangles[triIndex + 4] = vertIndex + 2;
        allTriangles[triIndex + 5] = vertIndex + 3;

        vertIndex += 4;
        triIndex += 6;
    }

    Color GetSpectrumColor(float progress) {
        Color[] keyColors = {
            new Color(1.0f, 0.2f, 0.2f),
            new Color(1.0f, 0.5f, 0.1f),
            new Color(1.0f, 0.8f, 0.1f),
            new Color(0.5f, 1.0f, 0.2f),
            new Color(0.2f, 1.0f, 0.5f),
            new Color(0.2f, 0.8f, 1.0f),
            new Color(0.3f, 0.5f, 1.0f),
            new Color(0.6f, 0.3f, 1.0f)
        };

        float scaledProgress = progress * (keyColors.Length - 1);
        int segmentIndex = Mathf.FloorToInt(scaledProgress);
        float segmentProgress = scaledProgress - segmentIndex;

        segmentIndex = Mathf.Clamp(segmentIndex, 0, keyColors.Length - 2);

        return Color.Lerp(keyColors[segmentIndex], keyColors[segmentIndex + 1], segmentProgress);
    }

    void OnDestroy() {
        if (sectionMaterials != null) {
            for (int i = 0; i < sectionMaterials.Length; i++) {
                if (sectionMaterials[i] != null) {
                    DestroyImmediate(sectionMaterials[i]);
                }
            }
        }
        if (connectionPointMaterial != null) {
            DestroyImmediate(connectionPointMaterial);
        }
    }
}