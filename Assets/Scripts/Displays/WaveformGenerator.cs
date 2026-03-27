using UnityEngine;

public class WaveformMeshGenerator : MonoBehaviour {
    AudioController audioController;

    public float waveformWidth = 15f;
    public float waveformHeight = 5f;
    public float lineThickness = 0.02f;
    public Material waveformMaterial;

    public float sensitivity = 0.5f;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh waveformMesh;

    GameObject centerLineObject;
    MeshFilter centerLineMeshFilter;
    MeshRenderer centerLineMeshRenderer;
    Mesh centerLineMesh;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    float[] waveformData;
    float timer = 0f;
    const float UPDATE_RATE = 1f / 60f;

    void Start() {
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        waveformMesh = new Mesh();
        waveformMesh.name = "Waveform";
        meshFilter.mesh = waveformMesh;

        meshRenderer.material = waveformMaterial;

        waveformData = new float[2048];

        InitializeMesh();
        CreateCenterLine();
    }

    void InitializeMesh() {
        int dataPoints = waveformData.Length - 1;

        int totalQuads = dataPoints;

        vertices = new Vector3[totalQuads * 4];
        triangles = new int[totalQuads * 6];
        uvs = new Vector2[vertices.Length];

        UpdateMeshGeometry();

        waveformMesh.vertices = vertices;
        waveformMesh.triangles = triangles;
        waveformMesh.uv = uvs;
        waveformMesh.RecalculateNormals();
        waveformMesh.RecalculateBounds();
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= UPDATE_RATE) {
            timer = 0f;
            waveformData = audioController.GetWaveformData();
            UpdateMeshGeometry();

            waveformMesh.vertices = vertices;
            waveformMesh.RecalculateBounds();
        }
    }

    void UpdateMeshGeometry() {
        int dataPoints = waveformData.Length;
        float stepX = waveformWidth / (dataPoints - 1);

        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < dataPoints - 1; i++) {
            float x1 = i * stepX - waveformWidth * 0.5f;
            float x2 = (i + 1) * stepX - waveformWidth * 0.5f;
            float y1 = ApplySensitivityCurve(waveformData[i]) * waveformHeight * 0.5f;
            float y2 = ApplySensitivityCurve(waveformData[i + 1]) * waveformHeight * 0.5f;

            CreateLineQuad(new Vector2(x1, y1), new Vector2(x2, y2), lineThickness, ref vertIndex, ref triIndex);
        }
    }

    void CreateLineQuad(Vector2 start, Vector2 end, float thickness, ref int vertIndex, ref int triIndex) {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * thickness * 0.5f;

        vertices[vertIndex + 0] = new Vector3(start.x - perpendicular.x, start.y - perpendicular.y, 0);
        vertices[vertIndex + 1] = new Vector3(start.x + perpendicular.x, start.y + perpendicular.y, 0);
        vertices[vertIndex + 2] = new Vector3(end.x + perpendicular.x, end.y + perpendicular.y, 0);
        vertices[vertIndex + 3] = new Vector3(end.x - perpendicular.x, end.y - perpendicular.y, 0);

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

    void CreateCenterLine() {
        centerLineObject = new GameObject("CenterLine");
        centerLineObject.transform.SetParent(transform);
        centerLineObject.transform.localPosition = new Vector3(0, 0, -0.1f);

        centerLineMeshFilter = centerLineObject.AddComponent<MeshFilter>();
        centerLineMeshRenderer = centerLineObject.AddComponent<MeshRenderer>();

        centerLineMesh = new Mesh();
        centerLineMesh.name = "CenterLine";
        centerLineMeshFilter.mesh = centerLineMesh;

        centerLineMeshRenderer.material = waveformMaterial;

        Vector3[] centerVertices = new Vector3[4];
        int[] centerTriangles = new int[6];
        Vector2[] centerUvs = new Vector2[4];

        float halfWidth = waveformWidth * 0.5f;
        float halfThickness = lineThickness * 0.5f;

        centerVertices[0] = new Vector3(-halfWidth, -halfThickness, 0);
        centerVertices[1] = new Vector3(-halfWidth, halfThickness, 0);
        centerVertices[2] = new Vector3(halfWidth, halfThickness, 0);
        centerVertices[3] = new Vector3(halfWidth, -halfThickness, 0);

        centerUvs[0] = new Vector2(0, 0);
        centerUvs[1] = new Vector2(0, 1);
        centerUvs[2] = new Vector2(1, 1);
        centerUvs[3] = new Vector2(1, 0);

        centerTriangles[0] = 0;
        centerTriangles[1] = 1;
        centerTriangles[2] = 2;
        centerTriangles[3] = 0;
        centerTriangles[4] = 2;
        centerTriangles[5] = 3;

        centerLineMesh.vertices = centerVertices;
        centerLineMesh.triangles = centerTriangles;
        centerLineMesh.uv = centerUvs;
        centerLineMesh.RecalculateNormals();
        centerLineMesh.RecalculateBounds();
    }

    float ApplySensitivityCurve(float input) {
        float sign = Mathf.Sign(input);
        float absInput = Mathf.Abs(input);

        if (absInput == 0f) return 0f;

        float result = Mathf.Pow(absInput, sensitivity);

        return sign * result;
    }
}