using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形のコアコンポーネント
/// </summary>
[RequireComponent(typeof(TerrainShape), typeof(MeshFilter), typeof(MeshDotRendererA))]
public class TerrainContextA : MonoBehaviour
{
    [SerializeField, Tooltip("地形の詳細設定")]
    private TerrainSettingsA _terrainSettings;

    [SerializeField, Tooltip("地形のパラメータ")]
    private TerrainParameterA _terrainParameter;

    private TerrainShape _terrainShape;
    private TerrainDestruct _terrainDestruct;
    private TerrainDestructEffectA _terrainDestructEffect;
    private MeshDotRendererA _dotRenderer;
    private Rigidbody2D _rigidbody;

    private bool _isOverlap = true;
    private float _area = 0.0f;

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

    public void OnOverlapEmpty()
    {
        if (!_isOverlap)
            return;

        _isOverlap = false;
        if (_rigidbody != null)
        {
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            if (_terrainShape != null)
            {
                float mass = CipperUtility.GetArea(_terrainShape.Points) * _terrainParameter.Density;
                _rigidbody.mass = mass;
            }
        }
    }

    private void Awake()
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

        ApplySettingsToRenderer();
    }

    private void Start()
    {
        _area = CipperUtility.GetArea(_terrainShape.Points);
        ApplySettingsToRenderer();
    }

    public void ApplySettingsToRenderer()
    {
        if (_dotRenderer == null || _terrainSettings == null || _terrainParameter == null) return;

        _dotRenderer.ApplyConfiguration(
            _terrainParameter,
            _terrainSettings.DotSize,
            _terrainSettings.EdgeWidthMultiplier
        );
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