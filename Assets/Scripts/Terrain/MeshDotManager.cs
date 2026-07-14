using UnityEngine;
using System.Collections.Generic;

public class MeshDotManager : MonoBehaviour
{
    public static MeshDotManager Instance { get; private set; }

    [Header("Settings")]
    public ComputeShader computeShader;
    public Material instancedMaterial;
    public Mesh quadMesh;
    public float edgeSize = 1.0f;
    public float dotSize = 0.1f;
    
    [Header("Optimization")]
    [Tooltip("画面外の余白（バッファ再構築の調整用）")]
    public float cullingPadding = 5.0f;

    public struct PolygonData
    {
        public Matrix4x4 objectToWorld;
        public int vertexStart;
        public int vertexCount;
        public int triangleStart;
        public int triangleCount;
        public int edgeStart;
        public int edgeCount;
        public Vector2 gridOffset;
        public int gridWidth;
        public int gridHeight;
    }
    
    // 管理中の地形
    private HashSet<TerrainContext> _activeTerrains = new HashSet<TerrainContext>();
    
    // カリング用リスト
    private HashSet<TerrainContext> _previouslyVisible = new HashSet<TerrainContext>();
    private List<TerrainContext> _currentlyVisible = new List<TerrainContext>();
    private List<TerrainContext> _renderedTerrains = new List<TerrainContext>();
    private Camera _mainCamera;

    // データ集約用リスト
    private List<PolygonData> _polygonDataList = new List<PolygonData>();
    private List<Vector2> _allVertices2D = new List<Vector2>();
    private List<int> _allTriangles = new List<int>();
    private List<Vector4> _allBoundaryEdges = new List<Vector4>();

    // 一時キャッシュリスト
    private List<Vector3> _tempVertices = new List<Vector3>();
    private List<int> _tempTriangles = new List<int>();

    // GPUバッファ
    private ComputeBuffer _polygonBuffer;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _triangleBuffer;
    private ComputeBuffer _edgeBuffer;
    private ComputeBuffer _resultBuffer;
    private ComputeBuffer _argsBuffer;

    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private MaterialPropertyBlock _mpb;
    private int _totalGridPixels = 0;
    
    private static readonly int TotalPolygonCount = Shader.PropertyToID("TotalPolygonCount");
    private static readonly int GridSpacing = Shader.PropertyToID("_GridSpacing");
    private static readonly int EdgeSize = Shader.PropertyToID("_EdgeSize");
    private static readonly int Polygons = Shader.PropertyToID("Polygons");
    private static readonly int Vertices = Shader.PropertyToID("Vertices");
    private static readonly int Triangles = Shader.PropertyToID("Triangles");
    private static readonly int BoundaryEdges = Shader.PropertyToID("BoundaryEdges");
    private static readonly int ResultBuffer = Shader.PropertyToID("ResultBuffer");
    private static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
    private static readonly int PositionBuffer = Shader.PropertyToID("positionBuffer");

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        _mainCamera = Camera.main;
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _args[0] = (uint)quadMesh.GetIndexCount(0);
        _argsBuffer.SetData(_args);
        _mpb = new MaterialPropertyBlock();
    }

    public void Register(TerrainContext terrain)
    {
        _activeTerrains.Add(terrain);
    }

    public void Unregister(TerrainContext terrain)
    {
        _activeTerrains.Remove(terrain);
        _previouslyVisible.Remove(terrain);
    }

    void LateUpdate()
    {
        if (_mainCamera == null || _activeTerrains.Count == 0) return;

        // カメラのフラスタム平面を取得
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        
        bool needsRebuild = false;
        _currentlyVisible.Clear();

        // 視界に入っている地形を取得
        foreach (var terrain in _activeTerrains)
        {
            if (terrain == null || terrain.PolygonCollider == null) continue;

            // 地形のBoundsを取得し、余白分広げる
            Bounds bounds = terrain.PolygonCollider.bounds;
            if (bounds.size == Vector3.zero) continue;

            bounds.Expand(cullingPadding);

            // 視界に入っているかチェック
            if (GeometryUtility.TestPlanesAABB(planes, bounds))
            {
                _currentlyVisible.Add(terrain);
                
                // 画面内にある地形に変化があったら再構築フラグを立てる
                if (terrain.IsDirtyDot) needsRebuild = true;
            }
        }

        // 画面内にあるオブジェクトに変化があったかチェック
        if (!needsRebuild)
        {
            if (_currentlyVisible.Count != _previouslyVisible.Count)
            {
                needsRebuild = true;
            }
            else
            {
                foreach (var terrain in _currentlyVisible)
                {
                    if (!_previouslyVisible.Contains(terrain))
                    {
                        needsRebuild = true;
                        break;
                    }
                }
            }
        }

        // 必要に応じてGPUバッファを再構築
        if (needsRebuild)
        {
            RebuildBuffers(_currentlyVisible);

            _previouslyVisible.Clear();
            foreach (var terrain in _currentlyVisible)
            {
                _previouslyVisible.Add(terrain);
                terrain.IsDirtyDot = false;
            }
        }

        // 描画処理
        if (_polygonDataList.Count == 0 || _totalGridPixels == 0) return;
        
        for (int i = 0; i < _renderedTerrains.Count; i++)
        {
            if (_renderedTerrains[i] != null)
            {
                PolygonData pData = _polygonDataList[i];
                pData.objectToWorld = _renderedTerrains[i].transform.localToWorldMatrix;
                _polygonDataList[i] = pData;
            }
        }
        _polygonBuffer.SetData(_polygonDataList);

        _resultBuffer.SetCounterValue(0);
        int kernel = computeShader.FindKernel("CSMainBatch");

        computeShader.SetInt(TotalPolygonCount, _polygonDataList.Count);
        computeShader.SetVector(GridSpacing, new Vector2(dotSize, dotSize));
        computeShader.SetFloat(EdgeSize, edgeSize);

        computeShader.SetBuffer(kernel, Polygons, _polygonBuffer);
        computeShader.SetBuffer(kernel, Vertices, _vertexBuffer);
        computeShader.SetBuffer(kernel, Triangles, _triangleBuffer);
        computeShader.SetBuffer(kernel, BoundaryEdges, _edgeBuffer);
        computeShader.SetBuffer(kernel, ResultBuffer, _resultBuffer);

        int threadGroupsX = Mathf.CeilToInt(_totalGridPixels / 64.0f);
        computeShader.Dispatch(kernel, threadGroupsX, 1, 1);

        ComputeBuffer.CopyCount(_resultBuffer, _argsBuffer, sizeof(uint));
        _mpb.SetFloat(PixelSize, dotSize);
        _mpb.SetBuffer(PositionBuffer, _resultBuffer);
        
        Graphics.DrawMeshInstancedIndirect(
            quadMesh, 0, instancedMaterial,
            new Bounds(_mainCamera.transform.position, Vector3.one * 1000f),
            _argsBuffer, 0, _mpb, UnityEngine.Rendering.ShadowCastingMode.Off,
            true, gameObject.layer, null, UnityEngine.Rendering.LightProbeUsage.Off
        );
    }

    private void RebuildBuffers(List<TerrainContext> targetTerrains)
    {
        _polygonDataList.Clear();
        _renderedTerrains.Clear();
        _allVertices2D.Clear();
        _allTriangles.Clear();
        _allBoundaryEdges.Clear();
        _totalGridPixels = 0;

        foreach (var terrain in targetTerrains)
        {
            MeshFilter mf = terrain.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh)
            {
                terrain.IsDirtyDot = true;
                continue; 
            }
            Mesh mesh = mf.sharedMesh;

            mesh.GetVertices(_tempVertices);
            mesh.GetTriangles(_tempTriangles, 0);

            if (_tempVertices.Count < 3 || _tempTriangles.Count < 3)
            {
                terrain.IsDirtyDot = false;
                continue;
            }
            _renderedTerrains.Add(terrain);

            PolygonData pData = new PolygonData();
            pData.objectToWorld = terrain.transform.localToWorldMatrix;
            pData.vertexStart = _allVertices2D.Count;
            pData.vertexCount = _tempVertices.Count;
            pData.triangleStart = _allTriangles.Count;
            pData.triangleCount = _tempTriangles.Count / 3;

            Vector2 minBound = new Vector2(_tempVertices[0].x, _tempVertices[0].y);
            Vector2 maxBound = minBound;
            
            for (int i = 0; i < _tempVertices.Count; i++)
            {
                Vector2 v2 = new Vector2(_tempVertices[i].x, _tempVertices[i].y);
                _allVertices2D.Add(v2);
                minBound = Vector2.Min(minBound, v2);
                maxBound = Vector2.Max(maxBound, v2);
            }

            for (int i = 0; i < _tempTriangles.Count; i++)
            {
                _allTriangles.Add(_tempTriangles[i]);
            }

            pData.edgeStart = _allBoundaryEdges.Count;
            int edgeCount = 0;
            if (terrain.TerrainPolygon != null && terrain.TerrainPolygon.TerrainPaths != null)
            {
                foreach (var edgeLoop in terrain.TerrainPolygon.TerrainPaths)
                {
                    Vector2[] points = edgeLoop.points;
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector2 a = points[i];
                        Vector2 b = points[(i + 1) % points.Length];
                        _allBoundaryEdges.Add(new Vector4(a.x, a.y, b.x, b.y));
                        edgeCount++;
                    }
                }
            }
            pData.edgeCount = edgeCount;

            minBound.x = Mathf.Floor(minBound.x / dotSize) * dotSize - dotSize;
            minBound.y = Mathf.Floor(minBound.y / dotSize) * dotSize - dotSize;
            maxBound.x = Mathf.Ceil(maxBound.x / dotSize) * dotSize + dotSize;
            maxBound.y = Mathf.Ceil(maxBound.y / dotSize) * dotSize + dotSize;

            pData.gridOffset = minBound;
            pData.gridWidth = Mathf.CeilToInt((maxBound.x - minBound.x) / dotSize);
            pData.gridHeight = Mathf.CeilToInt((maxBound.y - minBound.y) / dotSize);
            
            if (pData.gridWidth <= 0) pData.gridWidth = 1;
            if (pData.gridHeight <= 0) pData.gridHeight = 1;

            _totalGridPixels += pData.gridWidth * pData.gridHeight;
            _polygonDataList.Add(pData);
        }

        AllocateComputeBuffers();
    }

    private void EnsureBufferCapacity(ref ComputeBuffer buffer, int requiredCount, int stride, ComputeBufferType type = ComputeBufferType.Default)
    {
        if (requiredCount == 0) return;
        
        if (buffer == null || buffer.count < requiredCount)
        {
            int currentCount = (buffer != null) ? buffer.count : 0;

            if (buffer != null) 
            {
                buffer.Release();
                buffer = null;
            }

            int newCapacity = Mathf.Max(requiredCount, currentCount == 0 ? 256 : currentCount * 2);
            buffer = new ComputeBuffer(newCapacity, stride, type);
        }
    }

    private void AllocateComputeBuffers()
    {
        EnsureBufferCapacity(ref _polygonBuffer, Mathf.Max(_polygonDataList.Count, 1), sizeof(float) * 16 + sizeof(int) * 8 + sizeof(float) * 2);
        if (_polygonDataList.Count > 0) _polygonBuffer.SetData(_polygonDataList);

        EnsureBufferCapacity(ref _vertexBuffer, Mathf.Max(_allVertices2D.Count, 1), sizeof(float) * 2);
        if (_allVertices2D.Count > 0) _vertexBuffer.SetData(_allVertices2D);

        EnsureBufferCapacity(ref _triangleBuffer, Mathf.Max(_allTriangles.Count, 1), sizeof(int));
        if (_allTriangles.Count > 0) _triangleBuffer.SetData(_allTriangles);

        EnsureBufferCapacity(ref _edgeBuffer, Mathf.Max(_allBoundaryEdges.Count, 1), sizeof(float) * 4);
        if (_allBoundaryEdges.Count > 0) _edgeBuffer.SetData(_allBoundaryEdges);

        EnsureBufferCapacity(ref _resultBuffer, Mathf.Max(_totalGridPixels, 1), sizeof(float) * 14, ComputeBufferType.Append);
    }

    void OnDestroy()
    {
        if (_polygonBuffer != null) _polygonBuffer.Release();
        if (_vertexBuffer != null) _vertexBuffer.Release();
        if (_triangleBuffer != null) _triangleBuffer.Release();
        if (_edgeBuffer != null) _edgeBuffer.Release();
        if (_resultBuffer != null) _resultBuffer.Release();
        if (_argsBuffer != null) _argsBuffer.Release();
    }
}