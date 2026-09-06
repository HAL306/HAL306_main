using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 地形のコアコンポーネント
/// エディタ非再生中（Edit Mode）でも ScriptableObject の変更を検知して即時描画更新を行います。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TerrainShape), typeof(MeshFilter))]
public class TerrainContextA : MonoBehaviour
{
    [SerializeField, Tooltip("地形の詳細設定")]
    private TerrainSettingsA _terrainSettings;

    [SerializeField, Tooltip("地形のパラメータ（BaseTerrainRenderer 使用時は空でも可）")]
    private TerrainParameterA _terrainParameter;

    private TerrainShape _terrainShape;
    private TerrainDestruct _terrainDestruct;
    private TerrainDestructEffectA _terrainDestructEffect;
    private MeshDotRendererA _dotRenderer;
    private Rigidbody2D _rigidbody;

    private bool _isOverlap = true;
    private float _area = 0.0f;

    // 購読状態をトラッキング（Inspector でのアセット差し替え対応）
    private TerrainSettingsA _subscribedSettings;
    private TerrainParameterA _subscribedParameter;

    public TerrainSettingsA TerrainSettings => _terrainSettings;
    public TerrainParameterA TerrainParameter => _terrainParameter;
    public TerrainShape TerrainShape => _terrainShape;
    public TerrainDestruct TerrainDestruct => _terrainDestruct;
    public MeshDotRendererA DotRenderer => _dotRenderer;
    public Rigidbody2D Rigidbody => _rigidbody;
    public float Area => _area;

    public float Destruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        if (_terrainDestruct == null)
            return 0.0f;

        DestructResult destructResult = _terrainDestruct.PolygonDestruct(worldCenter, radius, crack);
        return OnDestruct(destructResult);
    }

    public float Crack(CrackData[] data, CrackParameter crack)
    {
        if (_terrainDestruct == null)
            return 0.0f;

        DestructResult destructResult = _terrainDestruct.PolygonCrack(data, crack);
        return OnDestruct(destructResult);
    }

    private void Awake()
    {
        InitComponents();
    }

    private void OnEnable()
    {
        InitComponents();
        SubscribeEvents();
        ApplySettingsToRenderer();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Start()
    {
        if (_terrainShape != null)
        {
            _area = CipperUtility.GetArea(_terrainShape.Points);
        }
        ApplySettingsToRenderer();
    }

    private void OnValidate()
    {
        InitComponents();
        SubscribeEvents();
        ApplySettingsToRenderer();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
    }

    private void InitComponents()
    {
        if (_terrainShape == null)
            _terrainShape = GetComponent<TerrainShape>();

        if (_terrainDestruct == null)
            _terrainDestruct = GetComponent<TerrainDestruct>();

        if (_terrainDestructEffect == null)
            _terrainDestructEffect = GetComponent<TerrainDestructEffectA>();

        if (_dotRenderer == null)
            _dotRenderer = GetComponent<MeshDotRendererA>();

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void SubscribeEvents()
    {
        // TerrainSettingsA の変更購読
        if (_subscribedSettings != _terrainSettings)
        {
            if (_subscribedSettings != null)
                _subscribedSettings.onValuesChanged -= OnScriptableObjectChanged;

            if (_terrainSettings != null)
                _terrainSettings.onValuesChanged += OnScriptableObjectChanged;

            _subscribedSettings = _terrainSettings;
        }

        // TerrainParameterA の変更購読
        if (_subscribedParameter != _terrainParameter)
        {
            if (_subscribedParameter != null)
                _subscribedParameter.onValuesChanged -= OnScriptableObjectChanged;

            if (_terrainParameter != null)
                _terrainParameter.onValuesChanged += OnScriptableObjectChanged;

            _subscribedParameter = _terrainParameter;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_subscribedSettings != null)
        {
            _subscribedSettings.onValuesChanged -= OnScriptableObjectChanged;
            _subscribedSettings = null;
        }
        if (_subscribedParameter != null)
        {
            _subscribedParameter.onValuesChanged -= OnScriptableObjectChanged;
            _subscribedParameter = null;
        }
    }

    private void OnScriptableObjectChanged()
    {
        ApplySettingsToRenderer();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
    }

    public void ApplySettingsToRenderer()
    {
        if (_dotRenderer == null)
            _dotRenderer = GetComponent<MeshDotRendererA>();

        // 設定アセットまたはレンダラーがなければ中断
        if (_dotRenderer == null || _terrainSettings == null)
            return;

        // 通常の MeshDotRendererA の場合は TerrainParameterA が必須
        // BaseTerrainRenderer の場合は TerrainParameterA が null でも動作可能
        bool isBaseRenderer = _dotRenderer is BaseTerrainRenderer;
        if (!isBaseRenderer && _terrainParameter == null)
            return;

        _dotRenderer.ApplyConfiguration(
            _terrainParameter,
            _terrainSettings.DotSize,
            _terrainSettings.EdgeWidthMultiplier
        );
    }

    public void OnOverlapEmpty()
    {
        if (!_isOverlap)
            return;

        _isOverlap = false;
        if (_rigidbody != null)
        {
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            if (_terrainShape != null && _terrainParameter != null)
            {
                float mass = CipperUtility.GetArea(_terrainShape.Points) * _terrainParameter.Density;
                _rigidbody.mass = mass;
            }
        }
    }

    private float OnDestruct(DestructResult destructResult)
    {
        for (int i = 0; i < destructResult.splitTerrainData.Count; ++i)
        {
            if (destructResult.splitTerrainData[i].area < _terrainSettings.MinArea)
                continue;

            SplitTerrainDataA splitData = destructResult.splitTerrainData[i];
            CreateSplitTerrain(splitData.path, splitData.area);
        }

        ChangeTerrain(destructResult.mainPath, destructResult.mainArea);

        if (_terrainDestructEffect != null)
            _terrainDestructEffect.OnDestruct(destructResult.destructPaths, destructResult.destructArea);

        return destructResult.destructArea;
    }

    private void CreateSplitTerrain(IReadOnlyList<Vector2> terrainPath, float area)
    {
        TerrainContextA newTerrain = Instantiate(
            _terrainSettings.TerrainPrefab, transform.position, transform.rotation);

        newTerrain.transform.localScale = transform.localScale;

        newTerrain._terrainSettings = _terrainSettings;
        newTerrain._terrainParameter = _terrainParameter;
        newTerrain._area = area;

        newTerrain.ApplySettingsToRenderer();

        if (newTerrain._terrainShape != null)
        {
            newTerrain._terrainShape.Initialize(terrainPath);
        }

        if (!_isOverlap)
        {
            newTerrain.OnOverlapEmpty();
        }
    }

    private void ChangeTerrain(IReadOnlyList<Vector2> terrainPath, float area)
    {
        if (area < _terrainSettings.MinArea)
        {
            Destroy(gameObject);
            return;
        }

        if (_terrainShape != null)
        {
            _terrainShape.UpdatePoints(terrainPath);
        }
    }
}