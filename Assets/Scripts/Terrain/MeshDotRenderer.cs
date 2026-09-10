using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class MeshDotRenderer : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DotInstance
    {
        public Vector3 localPosition;
        public Vector4 color;
        public Vector2 uv;
        public Vector3 localNormal;
        public float isEdge;
    }

    [Header("Assets")]
    [SerializeField] private Material _dotMaterial;
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private Mesh _dotShapeMesh;

    [Header("Source Materials & Textures")]
    [Tooltip("Shader Graph 等で作成したマテリアル")]
    [SerializeField] private Material _sourceMaterial;
    [SerializeField] private bool _dynamicUpdate = false;
    [SerializeField] private Vector2Int _renderTextureSize = new Vector2Int(512, 512);

    [Header("Direct Texture Overrides")]
    [SerializeField] private Texture2D _mainTexture;
    [SerializeField] private Texture2D _normalTexture;
    [Range(0f, 2f)] [SerializeField] private float _normalStrength = 1.0f;

    [Header("Dot Configuration")]
    [SerializeField] private float _dotSize = 0.125f;
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color _edgeColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);

    private MeshFilter _meshFilter;
    private MaterialPropertyBlock _propBlock;
    private ComputeBuffer _dotBuffer;
    private ComputeBuffer _argsBuffer;
    private RenderTexture _bakedColorTex;
    private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private int _dotCount = 0;

    public Material Material
    {
        get => _dotMaterial;
        set => _dotMaterial = value;
    }

    public Material SourceMaterial
    {
        get => _sourceMaterial;
        set { _sourceMaterial = value; BakeSourceMaterial(); }
    }

    public Texture2D Texture
    {
        get => _mainTexture;
        set { _mainTexture = value; UpdateProperties(); }
    }

    public Texture2D CustomNormalTexture
    {
        get => _normalTexture;
        set { _normalTexture = value; UpdateProperties(); }
    }

    public Color Color
    {
        get => _baseColor;
        set { _baseColor = value; UpdateProperties(); }
    }

    public float DotSize
    {
        get => _dotSize;
        set { _dotSize = Mathf.Max(0.005f, value); RebuildDots(); }
    }

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        
        BakeSourceMaterial();
        RebuildDots();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
        ReleaseRenderTextures();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
        ReleaseRenderTextures();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
        {
            RebuildDots();
        }
    }

    private void Update()
    {
        if (_dynamicUpdate && _sourceMaterial != null)
        {
            BakeSourceMaterial();
        }
    }

    private void LateUpdate()
    {
        if (_dotBuffer == null || _argsBuffer == null || _dotCount == 0 || _dotMaterial == null || _dotShapeMesh == null)
        {
            return;
        }

        UpdateProperties();

        Bounds localB = _meshFilter.sharedMesh != null ? _meshFilter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 10f);
        Vector3 worldCenter = transform.TransformPoint(localB.center);
        Vector3 worldSize = Vector3.Scale(localB.size, transform.lossyScale);
        float maxDim = Mathf.Max(Mathf.Abs(worldSize.x), Mathf.Max(Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)));
        Bounds worldBounds = new Bounds(worldCenter, Vector3.one * (maxDim + 2f));

        Graphics.DrawMeshInstancedIndirect(
            _dotShapeMesh,
            0,
            _dotMaterial,
            worldBounds,
            _argsBuffer,
            0,
            _propBlock,
            ShadowCastingMode.Off,
            false,
            gameObject.layer
        );
    }

    public void BakeSourceMaterial()
    {
        if (_sourceMaterial == null) return;

        if (_bakedColorTex == null || _bakedColorTex.width != _renderTextureSize.x)
        {
            ReleaseRenderTextures();
            _bakedColorTex = new RenderTexture(_renderTextureSize.x, _renderTextureSize.y, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _bakedColorTex.Create();
        }

        Graphics.Blit(null, _bakedColorTex, _sourceMaterial, 0);
    }

    private void UpdateProperties()
    {
        if (_propBlock == null || _dotBuffer == null) return;

        _propBlock.SetMatrix("_CustomLocalToWorld", transform.localToWorldMatrix);
        _propBlock.SetBuffer("_DotDataBuffer", _dotBuffer);
        _propBlock.SetFloat("_DotSize", _dotSize); // 描画サイズを配置間隔と完全に一致
        _propBlock.SetColor("_BaseColor", _baseColor);
        _propBlock.SetColor("_EdgeColor", _edgeColor);
        _propBlock.SetFloat("_BumpScale", _normalStrength);

        if (_bakedColorTex != null)
        {
            _propBlock.SetTexture("_MainTex", _bakedColorTex);
        }
        else if (_mainTexture != null)
        {
            _propBlock.SetTexture("_MainTex", _mainTexture);
        }

        if (_normalTexture != null)
        {
            _propBlock.SetTexture("_BumpMap", _normalTexture);
        }
        else if (_sourceMaterial != null && _sourceMaterial.HasProperty("_BumpMap"))
        {
            _propBlock.SetTexture("_BumpMap", _sourceMaterial.GetTexture("_BumpMap"));
        }
    }

    public void RebuildDots()
    {
        ReleaseBuffers();

        if (_meshFilter == null || _meshFilter.sharedMesh == null || _computeShader == null)
        {
            return;
        }

        Mesh mesh = _meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        if (vertices.Length == 0 || triangles.Length == 0) return;

        if (uvs == null || uvs.Length != vertices.Length)
        {
            uvs = new Vector2[vertices.Length];
        }

        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);

        vertexBuffer.SetData(vertices);
        triangleBuffer.SetData(triangles);
        uvBuffer.SetData(uvs);

        Bounds b = mesh.bounds;
        float size = Mathf.Max(0.005f, _dotSize);
        int gridX = Mathf.CeilToInt(b.size.x / size) + 2;
        int gridY = Mathf.CeilToInt(b.size.y / size) + 2;
        int maxCapacity = Mathf.Clamp(gridX * gridY, 64, 262144);

        _dotBuffer = new ComputeBuffer(maxCapacity, Marshal.SizeOf(typeof(DotInstance)), ComputeBufferType.Append);
        _dotBuffer.SetCounterValue(0);

        int kernel = _computeShader.FindKernel("CSGenerateDotsFromMesh");
        _computeShader.SetBuffer(kernel, "_Vertices", vertexBuffer);
        _computeShader.SetBuffer(kernel, "_Triangles", triangleBuffer);
        _computeShader.SetBuffer(kernel, "_UVs", uvBuffer);
        _computeShader.SetBuffer(kernel, "_ResultDots", _dotBuffer);

        _computeShader.SetVector("_BoundsMin", b.min);
        _computeShader.SetVector("_BoundsMax", b.max);
        _computeShader.SetFloat("_DotSize", size);
        _computeShader.SetInt("_TriangleCount", triangles.Length / 3);
        _computeShader.SetInts("_GridDimensions", new int[] { gridX, gridY, 1 });

        int threadGroupsX = Mathf.CeilToInt(gridX / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(gridY / 8.0f);

        if (threadGroupsX > 0 && threadGroupsY > 0)
        {
            _computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        }

        _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(_dotBuffer, _argsBuffer, sizeof(uint));
        _argsBuffer.GetData(_args);

        _args[0] = _dotShapeMesh != null ? _dotShapeMesh.GetIndexCount(0) : 0;
        _args[2] = _dotShapeMesh != null ? _dotShapeMesh.GetIndexStart(0) : 0;
        _args[3] = _dotShapeMesh != null ? _dotShapeMesh.GetBaseVertex(0) : 0;
        _argsBuffer.SetData(_args);

        _dotCount = (int)_args[1];

        vertexBuffer.Release();
        triangleBuffer.Release();
        uvBuffer.Release();
    }

    private void ReleaseBuffers()
    {
        if (_dotBuffer != null) { _dotBuffer.Release(); _dotBuffer = null; }
        if (_argsBuffer != null) { _argsBuffer.Release(); _argsBuffer = null; }
        _dotCount = 0;
    }

    private void ReleaseRenderTextures()
    {
        if (_bakedColorTex != null)
        {
            if (_bakedColorTex.IsCreated()) _bakedColorTex.Release();
            DestroyImmediate(_bakedColorTex);
            _bakedColorTex = null;
        }
    }
}