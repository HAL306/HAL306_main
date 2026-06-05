using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshDotRenderer : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material instancedMaterial;
    public Mesh quadMesh;

    public float DotSize { get; set; }
    
    public Vector2 DotOffset { get; set; }

    private MeshFilter _meshFilter;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _triangleBuffer;
    private ComputeBuffer _resultBuffer;
    private ComputeBuffer _argsBuffer;
    
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private int _maxPixelCount = 0;
    
    private MaterialPropertyBlock _mpb;
    
    private static readonly int TriangleCount = Shader.PropertyToID("TriangleCount");
    private static readonly int GridSpacing = Shader.PropertyToID("GridSpacing");
    private static readonly int GridOffset = Shader.PropertyToID("GridOffset");
    private static readonly int UserOffset = Shader.PropertyToID("UserOffset");
    private static readonly int Vertices = Shader.PropertyToID("Vertices");
    private static readonly int Triangles = Shader.PropertyToID("Triangles");
    private static readonly int ResultBuffer = Shader.PropertyToID("ResultBuffer");
    private static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
    private static readonly int PositionBuffer = Shader.PropertyToID("positionBuffer");
    private static readonly int ObjectToWorldMatrix = Shader.PropertyToID("_ObjectToWorldMatrix");

    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _args[0] = (uint)quadMesh.GetIndexCount(0);
        _argsBuffer.SetData(_args);
        
        _mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (!_meshFilter || !_meshFilter.sharedMesh) return;
        Mesh mesh = _meshFilter.sharedMesh;

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
            _resultBuffer = new ComputeBuffer(_maxPixelCount, sizeof(float) * 2, ComputeBufferType.Append);
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

        // ComputeShaderの実行
        _resultBuffer.SetCounterValue(0);
        int kernel = computeShader.FindKernel("CSMain");

        computeShader.SetInt(TriangleCount, triangleCount);
        computeShader.SetVector(GridSpacing, new Vector2(DotSize, DotSize));
        computeShader.SetVector(GridOffset, minBound);
        
        computeShader.SetBuffer(kernel, Vertices, _vertexBuffer);
        computeShader.SetBuffer(kernel, Triangles, _triangleBuffer);
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
            new Bounds(transform.position, Vector3.one * 20f),
            _argsBuffer, 0, _mpb, UnityEngine.Rendering.ShadowCastingMode.Off,
            true, gameObject.layer, null, UnityEngine.Rendering.LightProbeUsage.Off
        );
    }

    void OnDestroy()
    {
        if (_vertexBuffer != null) _vertexBuffer.Release();
        if (_triangleBuffer != null) _triangleBuffer.Release();
        if (_resultBuffer != null) _resultBuffer.Release();
        if (_argsBuffer != null) _argsBuffer.Release();
    }
}