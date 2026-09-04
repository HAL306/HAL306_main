using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ベース地形用レンダラー
/// 各レイヤーごとに独立したマテリアルサンプリング、PBR、Shadow/Color、ベイク設定を持ち、
/// ワールド空間グリッド配置とエッジ距離に応じた多層合成を行います。
/// </summary>
[ExecuteAlways]
public class BaseTerrainRenderer : MeshDotRendererA
{
    public const int MaxLayerCount = 4;

    [System.Serializable]
    public class TerrainMaterialLayer
    {
        [Tooltip("層の有効フラグ（チェックを入れるとこのレイヤーが描画・合成されます）")]
        public bool enabled = true;

        [Header("Distance Blend Settings")]
        [Tooltip("ブレンド開始距離（エッジからのメートル）")]
        public float startDistance = 0.0f;

        [Tooltip("ブレンド終了距離（エッジからのメートル。Start Distance以下の場合は距離無限として100%不透明描画）")]
        public float endDistance = 0.0f;

        [Header("Threshold Settings")]
        [Tooltip("境界のディゾルブ用閾値テクスチャ")]
        public Texture2D thresholdTexture;

        [Tooltip("マテリアルタイリングに対する閾値テクスチャのタイリング倍率")]
        public float thresholdTilingMultiplier = 1.0f;

        [Header("Ramp Settings")]
        [Tooltip("ソフトライト合成するランプテクスチャ (任意)")]
        public Texture2D rampTexture;

        [Header("マテリアルサンプリング設定")]
        [SerializeField, Tooltip("地形のサンプリング元マテリアル (Shader Graph等)")]
        private Material _material;

        [SerializeField, Tooltip("毎フレームマテリアルをベイク・更新するか（アニメーション・時間変化用）")]
        private bool _dynamicUpdate = false;

        [SerializeField, Tooltip("ベイク用RenderTextureの解像度")]
        private Vector2Int _renderTextureSize = new Vector2Int(512, 512);

        [SerializeField, Tooltip("法線マップの適用強度")]
        [Range(0.0f, 2.0f)]
        private float _normalStrength = 1.0f;

        [Header("UV設定")]
        [SerializeField, Tooltip("UVのスケール（タイリング密度。値が小さいほど大きく表示されます）")]
        private Vector2 _uvScale = new Vector2(1.0f, 1.0f);

        [Header("PBR設定")]
        [SerializeField, Range(0.0f, 1.0f), Tooltip("金属度")]
        private float _metallic = 0.0f;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("滑らかさ")]
        private float _smoothness = 0.5f;

        [Header("Shadow and Color設定")]
        [SerializeField, Tooltip("ティントカラー")]
        private Color _baseColor = Color.white;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("環境光の強さ（黒潰れ防止）")]
        private float _envLightStrength = 0.1f;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("影領域のベースカラー維持率（擬似自己発光）")]
        private float _shadowColorRetain = 0.2f;

        [SerializeField, Range(0.0f, 1.0f), Tooltip("アルファカットオフのしきい値")]
        private float _cutoff = 0.01f;

        // レイヤー固有のベイク用キャッシュ
        [NonSerialized] private RenderTexture _bakedTexture;
        [NonSerialized] private int _lastBakedFrame = -1;

        // プロパティ公開
        public Material Material => _material;
        public bool DynamicUpdate => _dynamicUpdate;
        public Vector2Int RenderTextureSize => _renderTextureSize;
        public float NormalStrength => _normalStrength;
        public Vector2 UVScale => _uvScale;
        public float Metallic => _metallic;
        public float Smoothness => _smoothness;
        public Color BaseColor => _baseColor;
        public float EnvLightStrength => _envLightStrength;
        public float ShadowColorRetain => _shadowColorRetain;
        public float Cutoff => _cutoff;

        /// <summary>
        /// レイヤーのマテリアルから有効なテクスチャを取得（ベイクが必要な場合は内部実行）
        /// </summary>
        public Texture GetEffectiveTexture()
        {
            if (_material == null) return Texture2D.whiteTexture;

            // テクスチャの有無をチェック
            Texture directTex = null;
            if (_material.HasProperty("_BaseMap") && _material.GetTexture("_BaseMap") != null)
                directTex = _material.GetTexture("_BaseMap");
            else if (_material.HasProperty("_MainTex") && _material.GetTexture("_MainTex") != null)
                directTex = _material.GetTexture("_MainTex");

            bool isShaderGraph = _material.shader != null && (_material.shader.name.Contains("Shader Graphs") || _material.shader.name.Contains("Graph"));
            // テクスチャを持たないプロシージャルマテリアル、またはShaderGraph、または動的更新がONの場合のみベイク
            bool needsBake = (directTex == null && isShaderGraph) || _dynamicUpdate;

            if (!needsBake)
            {
                return directTex != null ? directTex : Texture2D.whiteTexture;
            }

            // ベイク用RenderTextureの生成
            if (_bakedTexture == null || _bakedTexture.width != _renderTextureSize.x || _bakedTexture.height != _renderTextureSize.y)
            {
                ReleaseRenderTexture();
                _bakedTexture = new RenderTexture(_renderTextureSize.x, _renderTextureSize.y, 0, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat
                };
                _bakedTexture.Create();
                _lastBakedFrame = -1;
            }

            // 同一フレーム内での重複更新を防止
            if (_dynamicUpdate || _lastBakedFrame != Time.frameCount)
            {
                BakeMaterialSafe(_material, _bakedTexture);
                _lastBakedFrame = Time.frameCount;
            }

            return _bakedTexture;
        }

        /// <summary>
        /// URPのGraphics.Blit不具合を回避し、Shader Graph等を安全にRenderTextureに書き込む処理
        /// </summary>
        private static void BakeMaterialSafe(Material mat, RenderTexture targetRT)
        {
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = targetRT;

            GL.Clear(true, true, Color.white); // 白クリア（透明化防止）

            mat.SetPass(0); // Pass 0 (Forward / Unlit) をアクティブにする

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0.1f);
            GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0.1f);
            GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0.1f);
            GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0.1f);
            GL.End();

            GL.PopMatrix();

            RenderTexture.active = prevActive;
        }

        public void ReleaseRenderTexture()
        {
            if (_bakedTexture != null)
            {
                if (_bakedTexture.IsCreated()) _bakedTexture.Release();
                DestroyImmediate(_bakedTexture);
                _bakedTexture = null;
            }
            _lastBakedFrame = -1;
        }
    }

    [Header("Edge Normal Settings")]
    [SerializeField, Tooltip("エッジ法線を算出する幅（メートル）。0ならエッジ法線を計算せず正面向きにします")]
    private float _edgeNormalWidth = 0.0f;

    [Header("Base Terrain Layers (Max 4)")]
    [SerializeField, Tooltip("エッジ距離に応じた多層マテリアル（最大4個まで追加可能）")]
    private List<TerrainMaterialLayer> _layers = new List<TerrainMaterialLayer>()
    {
        new TerrainMaterialLayer { enabled = true, startDistance = 0f, endDistance = 0f } // デフォルト最下層ベース
    };

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_layers != null && _layers.Count > MaxLayerCount)
        {
            _layers.RemoveRange(MaxLayerCount, _layers.Count - MaxLayerCount);
        }

        // 非再生時にインスペクターのレイヤー数値やマテリアルを変えたら即時反映
        if (!Application.isPlaying)
        {
            RebuildDots();
            UpdateProperties();
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ReleaseAllLayerTextures();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ReleaseAllLayerTextures();
    }

    private void ReleaseAllLayerTextures()
    {
        if (_layers == null) return;
        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i]?.ReleaseRenderTexture();
        }
    }

    public override void ApplyConfiguration(TerrainParameterA parameter, float dotSize, float edgeWidthMultiplier)
    {
        _dotSize = Mathf.Max(0.005f, dotSize);
        RebuildDots();
    }

    protected override void LateUpdate()
    {
        if (Application.isPlaying)
        {
            RenderIndirect(null);
        }
    }

    protected override void RenderIndirect(Camera targetCam)
    {
        if (_dotBuffer == null || _argsBuffer == null || _dotCount == 0 || _instancedDotMaterial == null || _dotShapeMesh == null)
        {
            return;
        }

        UpdateProperties();

        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds localB = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 10f);
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
            gameObject.layer,
            targetCam
        );
    }

    protected override void UpdateProperties()
    {
        if (_propBlock == null || _dotBuffer == null) return;

        _propBlock.SetBuffer("_DotDataBuffer", _dotBuffer);
        _propBlock.SetFloat("_DotSize", _dotSize);

        int layerCount = _layers != null ? Mathf.Min(_layers.Count, MaxLayerCount) : 0;

        for (int i = 0; i < MaxLayerCount; i++)
        {
            string prefix = $"_Layer{i}_";

            if (i < layerCount && _layers[i] != null && _layers[i].enabled)
            {
                var l = _layers[i];
                Material mat = l.Material;

                // 1. テクスチャ取得 (ベイク必要時は実行)
                Texture mainTex = l.GetEffectiveTexture();
                Texture bumpMap = Texture2D.normalTexture;
                float normalScale = l.NormalStrength;

                Color finalBaseColor = l.BaseColor;

                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor"))
                        finalBaseColor *= mat.GetColor("_BaseColor");
                    else if (mat.HasProperty("_Color"))
                        finalBaseColor *= mat.GetColor("_Color");

                    if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
                    {
                        bumpMap = mat.GetTexture("_BumpMap");
                    }
                }

                _propBlock.SetTexture(prefix + "Tex", mainTex);
                _propBlock.SetTexture(prefix + "BumpMap", bumpMap);
                _propBlock.SetVector(prefix + "ST", new Vector4(l.UVScale.x, l.UVScale.y, 0, 0));
                _propBlock.SetFloat(prefix + "BumpScale", normalScale);

                // 2. 閾値テクスチャの設定 (指定がなければ whiteTexture を割り当て、有効フラグを 0 にする)
                bool hasThreshold = l.thresholdTexture != null;
                _propBlock.SetTexture(prefix + "ThresholdTex", hasThreshold ? l.thresholdTexture : Texture2D.whiteTexture);
                _propBlock.SetVector(prefix + "ThresholdParams", new Vector4(Mathf.Max(0.01f, l.thresholdTilingMultiplier), hasThreshold ? 1.0f : 0.0f, 0, 0));

                // 3. ランプテクスチャの設定 (指定がなければホワイトテクスチャ、かつ useRamp フラグを 0 にして合成完全スキップ)
                bool useRamp = l.rampTexture != null;
                _propBlock.SetTexture(prefix + "RampTex", useRamp ? l.rampTexture : Texture2D.whiteTexture);

                // DistRange: x=startDist, y=endDist, z=useRamp(1/0), w=active(1)
                _propBlock.SetVector(prefix + "DistRange", new Vector4(l.startDistance, l.endDistance, useRamp ? 1.0f : 0.0f, 1.0f));

                // 4. PBR / Shadow パラメータ
                _propBlock.SetColor(prefix + "BaseColor", finalBaseColor);
                _propBlock.SetFloat(prefix + "Metallic", l.Metallic);
                _propBlock.SetFloat(prefix + "Smoothness", l.Smoothness);
                _propBlock.SetFloat(prefix + "EnvLightStrength", l.EnvLightStrength);
                _propBlock.SetFloat(prefix + "ShadowColorRetain", l.ShadowColorRetain);
                _propBlock.SetFloat(prefix + "Cutoff", l.Cutoff);
            }
            else
            {
                _propBlock.SetVector(prefix + "DistRange", Vector4.zero);
            }
        }
    }

    public override void RebuildDots()
    {
        ReleaseBuffers();

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null || _computeShader == null) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] localVertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        if (localVertices.Length == 0 || triangles.Length == 0) return;

        // 頂点をワールド座標系に変換（回転に追従させない）
        Vector2[] worldVertices2D = new Vector2[localVertices.Length];
        for (int i = 0; i < localVertices.Length; i++)
        {
            Vector3 wPos = transform.TransformPoint(localVertices[i]);
            worldVertices2D[i] = new Vector2(wPos.x, wPos.y);
        }

        Vector4[] worldEdges = ExtractWorldBoundaryEdges(worldVertices2D, triangles);

        ComputeBuffer vertexBuffer = new ComputeBuffer(worldVertices2D.Length, sizeof(float) * 2);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length > 0 ? uvs.Length : worldVertices2D.Length, sizeof(float) * 2);
        ComputeBuffer edgeBuffer = new ComputeBuffer(worldEdges.Length, sizeof(float) * 4);

        vertexBuffer.SetData(worldVertices2D);
        triangleBuffer.SetData(triangles);
        uvBuffer.SetData(uvs.Length > 0 ? uvs : worldVertices2D);
        edgeBuffer.SetData(worldEdges);

        Vector2 minW = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxW = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < worldVertices2D.Length; i++)
        {
            minW = Vector2.Min(minW, worldVertices2D[i]);
            maxW = Vector2.Max(maxW, worldVertices2D[i]);
        }

        float size = Mathf.Max(0.005f, _dotSize);

        Vector2 gridOffset = new Vector2(
            Mathf.Floor(minW.x / size) * size + size * 0.5f,
            Mathf.Floor(minW.y / size) * size + size * 0.5f
        );

        int gridX = Mathf.CeilToInt((maxW.x - gridOffset.x) / size) + 2;
        int gridY = Mathf.CeilToInt((maxW.y - gridOffset.y) / size) + 2;
        int maxCapacity = Mathf.Clamp(gridX * gridY, 64, 524288);

        _dotBuffer = new ComputeBuffer(maxCapacity, 52, ComputeBufferType.Append);
        _dotBuffer.SetCounterValue(0);

        int kernel = _computeShader.FindKernel("CSGenerateBaseTerrainDots");
        _computeShader.SetBuffer(kernel, "_Vertices", vertexBuffer);
        _computeShader.SetBuffer(kernel, "_Triangles", triangleBuffer);
        _computeShader.SetBuffer(kernel, "_UVs", uvBuffer);
        _computeShader.SetBuffer(kernel, "_BoundaryEdges", edgeBuffer);
        _computeShader.SetBuffer(kernel, "_ResultDots", _dotBuffer);

        _computeShader.SetVector("_GridOffset", gridOffset);
        _computeShader.SetVector("_GridSpacing", new Vector2(size, size));
        _computeShader.SetFloat("_EdgeNormalWidth", Mathf.Max(0.0f, _edgeNormalWidth));
        _computeShader.SetInt("_TriangleCount", triangles.Length / 3);
        _computeShader.SetInt("_EdgeCount", worldEdges.Length);
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

    private Vector4[] ExtractWorldBoundaryEdges(Vector2[] worldVertices, int[] triangles)
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
                boundaryList.Add(new Vector4(worldVertices[iA].x, worldVertices[iA].y, worldVertices[iB].x, worldVertices[iB].y));
            }
        }

        return boundaryList.Count > 0 ? boundaryList.ToArray() : new Vector4[] { Vector4.zero };
    }
}