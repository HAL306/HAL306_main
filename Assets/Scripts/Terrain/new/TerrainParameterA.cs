using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 地形のパラメータ（地形の種類ごとに設定）
/// </summary>
[CreateAssetMenu(fileName = "TerrainParameterA", menuName = "Scriptable Objects/TerrainParameterA")]
public class TerrainParameterA : ScriptableObject
{
    // 値変更通知用イベント
    public event Action onValuesChanged;

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

    [Header("破壊用ステータス")]
    [SerializeField, Range(0.0f, 2.0f)] private float _destructibility = 1.0f;
    [SerializeField, Range(0.0f, 2.0f)] private float _fractureMultiplier = 1.0f;
    [SerializeField, Range(0.0f, 20.0f)] private float _density = 5.0f;

    [Header("エフェクト設定")]
    [SerializeField] private ParticleSystem _destructEffect;
    [SerializeField] private float _effectAmount = 30.0f;
    [SerializeField] private GameObject _destructObject;
    [SerializeField] private float _destructObjectAmount = 5.0f;

    [Header("サウンド設定")]
    [SerializeField] private bool _isSoundEnabled = true;

    private RenderTexture _sharedBakedTexture;
    private int _lastBakedFrame = -1;

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

    public float Density => _density;
    public float Destructibility => _destructibility;
    public float FractureMultiplier => _fractureMultiplier;
    public ParticleSystem DestructEffect => _destructEffect;
    public float EffectAmount => _effectAmount;
    public GameObject DestructObject => _destructObject;
    public float DestructObjectAmount => _destructObjectAmount;
    public bool IsSoundEnabled => _isSoundEnabled;

    private void OnDisable()
    {
        ReleaseBakedTexture();
    }

    private void OnDestroy()
    {
        ReleaseBakedTexture();
    }

    private void OnValidate()
    {
        // インスペクターで値が変更されたらキャッシュを破棄し、購読者に即時通知
        ReleaseBakedTexture();
        onValuesChanged?.Invoke();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
    }

    public Texture GetEffectiveTexture()
    {
        if (_material == null) return Texture2D.whiteTexture;

        bool isShaderGraph = _material.shader != null && (_material.shader.name.Contains("Shader Graphs") || _material.shader.name.Contains("Graph"));
        bool needsBake = isShaderGraph || _dynamicUpdate;

        if (!needsBake)
        {
            if (_material.HasProperty("_BaseMap") && _material.GetTexture("_BaseMap") != null)
                return _material.GetTexture("_BaseMap");
            if (_material.HasProperty("_MainTex") && _material.GetTexture("_MainTex") != null)
                return _material.GetTexture("_MainTex");

            return Texture2D.whiteTexture;
        }

        if (_sharedBakedTexture == null || _sharedBakedTexture.width != _renderTextureSize.x || _sharedBakedTexture.height != _renderTextureSize.y)
        {
            ReleaseBakedTexture();
            _sharedBakedTexture = new RenderTexture(_renderTextureSize.x, _renderTextureSize.y, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            _sharedBakedTexture.Create();
            _lastBakedFrame = -1;
        }

        if (_dynamicUpdate || _lastBakedFrame != Time.frameCount)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _sharedBakedTexture;
            GL.Clear(true, true, Color.white);
            _material.SetPass(0);

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0.1f);
            GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0.1f);
            GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0.1f);
            GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0.1f);
            GL.End();
            GL.PopMatrix();

            RenderTexture.active = prev;
            _lastBakedFrame = Time.frameCount;
        }

        return _sharedBakedTexture;
    }

    public void ReleaseBakedTexture()
    {
        if (_sharedBakedTexture != null)
        {
            if (_sharedBakedTexture.IsCreated()) _sharedBakedTexture.Release();
            DestroyImmediate(_sharedBakedTexture);
            _sharedBakedTexture = null;
        }
        _lastBakedFrame = -1;
    }
}