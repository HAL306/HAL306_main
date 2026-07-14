using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class MeshDotRenderer : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material instancedMaterial;
    public Mesh quadMesh;
    public float edgeSize;

    public float DotSize { get; set; }
    
    public Vector2 DotOffset { get; set; }

    private MeshFilter _meshFilter;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _triangleBuffer;
    private ComputeBuffer _resultBuffer;
    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _edgeBuffer;
    
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private int _maxPixelCount = 0;
    private MaterialPropertyBlock _mpb;
    
    private Dictionary<ulong, int> _edgeCounts = new Dictionary<ulong, int>();
    private Dictionary<ulong, Vector2Int> _edgeOriginalDirs = new Dictionary<ulong, Vector2Int>();
    private List<Vector4> _boundaryEdges = new List<Vector4>();
    private Camera _mainCamera;

    private static readonly int TriangleCount = Shader.PropertyToID("TriangleCount");
    private static readonly int GridSpacing = Shader.PropertyToID("GridSpacing");
    private static readonly int GridOffset = Shader.PropertyToID("GridOffset");
    private static readonly int Vertices = Shader.PropertyToID("Vertices");
    private static readonly int Triangles = Shader.PropertyToID("Triangles");
    private static readonly int ResultBuffer = Shader.PropertyToID("ResultBuffer");
    private static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
    private static readonly int PositionBuffer = Shader.PropertyToID("positionBuffer");
    private static readonly int ObjectToWorldMatrix = Shader.PropertyToID("_ObjectToWorldMatrix");
    private static readonly int BoundaryEdgeCount = Shader.PropertyToID("BoundaryEdgeCount");
    private static readonly int BoundaryEdges = Shader.PropertyToID("BoundaryEdges");
    private static readonly int EdgeSize = Shader.PropertyToID("EdgeSize");

    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _args[0] = (uint)quadMesh.GetIndexCount(0);
        _argsBuffer.SetData(_args);
        
        _mpb = new MaterialPropertyBlock();
        
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!_meshFilter || !_meshFilter.sharedMesh) return;
        Mesh mesh = _meshFilter.sharedMesh;
        
        Bounds bounds = mesh.bounds;
        bounds.center = transform.TransformPoint(bounds.center);
        bounds.extents = Vector3.Scale(bounds.extents, transform.lossyScale);

        // 画面外外なら描画しない
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        if (!GeometryUtility.TestPlanesAABB(planes, bounds))
        {
            return; 
        }

        // 動的メッシュの頂点とインデックスを取得
        Vector3[] vertices3D = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        int vertexCount = vertices3D.Length;
        int triangleCount = triangles.Length / 3;

        if (vertexCount < 3 || triangleCount < 1) return;

        // AABBの計算と2D配列への変換
        Vector2 minBound = new Vector2(vertices3D[0].x, vertices3D[0].y);
        Vector2 maxBound = minBound;
        Vector2[] vertices2D = new Vector2[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            Vector2 v = new Vector2(vertices3D[i].x, vertices3D[i].y);
            vertices2D[i] = v;
            minBound = Vector2.Min(minBound, v);
            maxBound = Vector2.Max(maxBound, v);
        }
        
        _edgeCounts.Clear();
        _edgeOriginalDirs.Clear();
        _boundaryEdges.Clear();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            AddEdge(triangles[i], triangles[i + 1]);
            AddEdge(triangles[i + 1], triangles[i + 2]);
            AddEdge(triangles[i + 2], triangles[i]);
        }

        foreach (var kvp in _edgeCounts)
        {
            if (kvp.Value == 1)
            {
                Vector2Int orig = _edgeOriginalDirs[kvp.Key];
                Vector2 vA = vertices2D[orig.x];
                Vector2 vB = vertices2D[orig.y];
                _boundaryEdges.Add(new Vector4(vA.x, vA.y, vB.x, vB.y));
            }
        }

        minBound.x = Mathf.Floor(minBound.x / DotSize) * DotSize - DotSize;
        minBound.y = Mathf.Floor(minBound.y / DotSize) * DotSize - DotSize;
        maxBound.x = Mathf.Ceil(maxBound.x / DotSize) * DotSize + DotSize;
        maxBound.y = Mathf.Ceil(maxBound.y / DotSize) * DotSize + DotSize;

        int gridWidth = Mathf.CeilToInt((maxBound.x - minBound.x) / DotSize);
        int gridHeight = Mathf.CeilToInt((maxBound.y - minBound.y) / DotSize);

        // GPUバッファの動的確保
        int requiredPixels = gridWidth * gridHeight;
        if (_resultBuffer == null || requiredPixels > _maxPixelCount)
        {
            if (_resultBuffer != null) _resultBuffer.Release();
            _maxPixelCount = Mathf.NextPowerOfTwo(requiredPixels);
            _resultBuffer = new ComputeBuffer(_maxPixelCount, 20, ComputeBufferType.Append);
        }

        if (_vertexBuffer == null || _vertexBuffer.count != vertexCount)
        {
            if (_vertexBuffer != null) _vertexBuffer.Release();
            _vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 2);
        }
        _vertexBuffer.SetData(vertices2D);

        if (_triangleBuffer == null || _triangleBuffer.count != triangles.Length)
        {
            if (_triangleBuffer != null) _triangleBuffer.Release();
            _triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        }
        _triangleBuffer.SetData(triangles);
        
        if (_edgeBuffer == null || _edgeBuffer.count != _boundaryEdges.Count)
        {
            if (_edgeBuffer != null) _edgeBuffer.Release();
            _edgeBuffer = new ComputeBuffer(Mathf.Max(_boundaryEdges.Count, 1), sizeof(float) * 4);
        }
        if (_boundaryEdges.Count > 0)
        {
            _edgeBuffer.SetData(_boundaryEdges);
        }

        // ComputeShaderの実行
        _resultBuffer.SetCounterValue(0);
        int kernel = computeShader.FindKernel("CSMain");

        computeShader.SetInt(TriangleCount, triangleCount);
        computeShader.SetInt(BoundaryEdgeCount, _boundaryEdges.Count);
        computeShader.SetVector(GridSpacing, new Vector2(DotSize, DotSize));
        computeShader.SetVector(GridOffset, minBound);
        computeShader.SetFloat(EdgeSize, edgeSize);
        
        computeShader.SetBuffer(kernel, Vertices, _vertexBuffer);
        computeShader.SetBuffer(kernel, Triangles, _triangleBuffer);
        computeShader.SetBuffer(kernel, BoundaryEdges, _edgeBuffer);
        computeShader.SetBuffer(kernel, ResultBuffer, _resultBuffer);

        int threadGroupsX = Mathf.CeilToInt(gridWidth / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(gridHeight / 8.0f);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        // 描画
        ComputeBuffer.CopyCount(_resultBuffer, _argsBuffer, sizeof(uint));
        
        _mpb.SetFloat(PixelSize, DotSize);
        _mpb.SetBuffer(PositionBuffer, _resultBuffer);
        _mpb.SetMatrix(ObjectToWorldMatrix, transform.localToWorldMatrix);
        
        Graphics.DrawMeshInstancedIndirect(
            quadMesh, 0, instancedMaterial,
            bounds,
            _argsBuffer, 0, _mpb, UnityEngine.Rendering.ShadowCastingMode.Off,
            true, gameObject.layer, null, UnityEngine.Rendering.LightProbeUsage.Off
        );
    }
    
    private void AddEdge(int v1, int v2)
    {
        int min = Mathf.Min(v1, v2);
        int max = Mathf.Max(v1, v2);
        ulong key = ((ulong)min << 32) | (uint)max;

        if (_edgeCounts.ContainsKey(key))
        {
            _edgeCounts[key]++;
        }
        else
        {
            _edgeCounts[key] = 1;
            _edgeOriginalDirs[key] = new Vector2Int(v1, v2);
        }
    }

    void OnDestroy()
    {
        if (_vertexBuffer != null) _vertexBuffer.Release();
        if (_triangleBuffer != null) _triangleBuffer.Release();
        if (_resultBuffer != null) _resultBuffer.Release();
        if (_argsBuffer != null) _argsBuffer.Release();
        if(_edgeBuffer != null) _edgeBuffer.Release();
    }
}