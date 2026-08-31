using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class MeshDotRendererA : MonoBehaviour
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

    [Header("Base Assets")]
    [SerializeField] private Material _instancedDotMaterial;
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private Mesh _dotShapeMesh;

    [Header("Shadow Settings")]
    [SerializeField] private ShadowCastingMode _shadowCastingMode = ShadowCastingMode.On;
    [SerializeField] private bool _receiveShadows = true;

    // TerrainParameterA から渡される設定
    private TerrainParameterA _parameter;
    private float _dotSize = 0.125f;
    private float _edgeWidthMultiplier = 1.0f;

    private MeshFilter _meshFilter;
    private MaterialPropertyBlock _propBlock;
    private ComputeBuffer _dotBuffer;
    private ComputeBuffer _argsBuffer;
    private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private int _dotCount = 0;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        RebuildDots();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void LateUpdate()
    {
        if (_dotBuffer == null || _argsBuffer == null || _dotCount == 0 || _instancedDotMaterial == null || _dotShapeMesh == null)
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
            _instancedDotMaterial,
            worldBounds,
            _argsBuffer,
            0,
            _propBlock,
            _shadowCastingMode,
            _receiveShadows,
            gameObject.layer
        );
    }

    /// <summary>
    /// TerrainSettingsA / TerrainParameterA からの設定を適用
    /// </summary>
    public void ApplyConfiguration(TerrainParameterA parameter, float dotSize, float edgeWidthMultiplier)
    {
        _parameter = parameter;
        _dotSize = Mathf.Max(0.005f, dotSize);
        _edgeWidthMultiplier = Mathf.Max(0.1f, edgeWidthMultiplier);

        RebuildDots();
        UpdateProperties();
    }

    private void UpdateProperties()
    {
        if (_propBlock == null || _dotBuffer == null) return;

        _propBlock.SetMatrix("_CustomLocalToWorld", transform.localToWorldMatrix);
        _propBlock.SetBuffer("_DotDataBuffer", _dotBuffer);
        _propBlock.SetFloat("_DotSize", _dotSize);

        if (_parameter == null) return;

        // TerrainParameterA が管理する共有テクスチャをバインド（ベイク処理は Parameter 側で1回のみ実行）
        Texture mainTex = _parameter.GetEffectiveTexture();
        _propBlock.SetTexture("_MainTex", mainTex != null ? mainTex : Texture2D.whiteTexture);

        _propBlock.SetVector("_MainTex_ST", new Vector4(_parameter.UVScale.x, _parameter.UVScale.y, 0.0f, 0.0f));
        _propBlock.SetFloat("_BumpScale", _parameter.NormalStrength);

        Color finalBaseColor = _parameter.BaseColor;
        bool hasNormal = false;

        Material srcMat = _parameter.Material;
        if (srcMat != null)
        {
            if (srcMat.HasProperty("_BaseColor"))
                finalBaseColor *= srcMat.GetColor("_BaseColor");
            else if (srcMat.HasProperty("_Color"))
                finalBaseColor *= srcMat.GetColor("_Color");

            if (srcMat.HasProperty("_BumpMap"))
            {
                Texture nTex = srcMat.GetTexture("_BumpMap");
                if (nTex != null)
                {
                    _propBlock.SetTexture("_BumpMap", nTex);
                    hasNormal = true;
                }
            }
        }

        _propBlock.SetColor("_BaseColor", finalBaseColor);
        _propBlock.SetFloat("_Metallic", _parameter.Metallic);
        _propBlock.SetFloat("_Smoothness", _parameter.Smoothness);
        _propBlock.SetFloat("_EnvLightStrength", _parameter.EnvLightStrength);
        _propBlock.SetFloat("_ShadowColorRetain", _parameter.ShadowColorRetain);
        _propBlock.SetFloat("_Cutoff", _parameter.Cutoff);
        _propBlock.SetFloat("_HasBumpMap", hasNormal ? 1.0f : 0.0f);
    }

    private Vector4[] ExtractBoundaryEdges(Vector3[] vertices, int[] triangles)
    {
        Dictionary<ulong, int> edgeCountMap = new Dictionary<ulong, int>();
        int triCount = triangles.Length / 3;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = triangles[t * 3 + 0];
            int i1 = triangles[t * 3 + 1];
            int i2 = triangles[t * 3 + 2];

            AddEdge(i0, i1, edgeCountMap);
            AddEdge(i1, i2, edgeCountMap);
            AddEdge(i2, i0, edgeCountMap);
        }

        List<Vector4> boundaryList = new List<Vector4>();
        foreach (var kvp in edgeCountMap)
        {
            if (kvp.Value == 1)
            {
                int iA = (int)(kvp.Key >> 32);
                int iB = (int)(kvp.Key & 0xFFFFFFFF);
                boundaryList.Add(new Vector4(vertices[iA].x, vertices[iA].y, vertices[iB].x, vertices[iB].y));
            }
        }

        return boundaryList.Count > 0 ? boundaryList.ToArray() : new Vector4[] { Vector4.zero };
    }

    private void AddEdge(int a, int b, Dictionary<ulong, int> map)
    {
        int min = Math.Min(a, b);
        int max = Math.Max(a, b);
        ulong key = ((ulong)min << 32) | (uint)max;

        if (map.ContainsKey(key)) map[key]++;
        else map[key] = 1;
    }

    public void RebuildDots()
    {
        ReleaseBuffers();

        if (_meshFilter == null || _meshFilter.sharedMesh == null || _computeShader == null)
        {
            return;
        }

        Mesh mesh = _meshFilter.sharedMesh;
        Vector3[] vertices3D = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        if (vertices3D.Length == 0 || triangles.Length == 0) return;

        Vector2[] vertices2D = new Vector2[vertices3D.Length];
        for (int i = 0; i < vertices3D.Length; i++)
        {
            vertices2D[i] = new Vector2(vertices3D[i].x, vertices3D[i].y);
        }

        if (uvs == null || uvs.Length != vertices3D.Length)
        {
            uvs = new Vector2[vertices3D.Length];
        }

        Vector4[] boundaryEdges = ExtractBoundaryEdges(vertices3D, triangles);

        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices2D.Length, sizeof(float) * 2);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
        ComputeBuffer edgeBuffer = new ComputeBuffer(boundaryEdges.Length, sizeof(float) * 4);

        vertexBuffer.SetData(vertices2D);
        triangleBuffer.SetData(triangles);
        uvBuffer.SetData(uvs);
        edgeBuffer.SetData(boundaryEdges);

        Bounds b = mesh.bounds;
        float size = Mathf.Max(0.005f, _dotSize);

        Vector2 gridOffset = new Vector2(
            Mathf.Floor(b.min.x / size) * size + size * 0.5f,
            Mathf.Floor(b.min.y / size) * size + size * 0.5f
        );

        int gridX = Mathf.CeilToInt((b.max.x - gridOffset.x) / size) + 2;
        int gridY = Mathf.CeilToInt((b.max.y - gridOffset.y) / size) + 2;
        int maxCapacity = Mathf.Clamp(gridX * gridY, 64, 262144);

        _dotBuffer = new ComputeBuffer(maxCapacity, Marshal.SizeOf(typeof(DotInstance)), ComputeBufferType.Append);
        _dotBuffer.SetCounterValue(0);

        int kernel = _computeShader.FindKernel("CSGenerateDotsFromMesh");
        _computeShader.SetBuffer(kernel, "_Vertices", vertexBuffer);
        _computeShader.SetBuffer(kernel, "_Triangles", triangleBuffer);
        _computeShader.SetBuffer(kernel, "_UVs", uvBuffer);
        _computeShader.SetBuffer(kernel, "_BoundaryEdges", edgeBuffer);
        _computeShader.SetBuffer(kernel, "_ResultDots", _dotBuffer);

        _computeShader.SetVector("_GridOffset", gridOffset);
        _computeShader.SetVector("_BoundsMin", b.min);
        _computeShader.SetVector("_BoundsMax", b.max);
        _computeShader.SetVector("_GridSpacing", new Vector2(size, size));
        _computeShader.SetFloat("_EdgeSize", _edgeWidthMultiplier);
        _computeShader.SetInt("_TriangleCount", triangles.Length / 3);
        _computeShader.SetInt("_EdgeCount", boundaryEdges.Length);
        _computeShader.SetInts("_GridDimensions", new int[] { gridX, gridY });

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
        edgeBuffer.Release();
    }

    private void ReleaseBuffers()
    {
        if (_dotBuffer != null) { _dotBuffer.Release(); _dotBuffer = null; }
        if (_argsBuffer != null) { _argsBuffer.Release(); _argsBuffer = null; }
        _dotCount = 0;
    }
}