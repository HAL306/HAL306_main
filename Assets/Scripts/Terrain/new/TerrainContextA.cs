using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形のコアコンポーネント
/// </summary>
public class TerrainContextA : MonoBehaviour
{
    [SerializeField, Tooltip("地形の詳細設定")]
    private TerrainSettingsA _terrainSettings;

    [SerializeField, Tooltip("地形のパラメータ")]
    private TerrainParameterA _terrainParameter;

    private TerrainShape _terrainShape;
    private TerrainDestruct _terrainDestruct;
    private TerrainDestructEffectA _terrainDestructEffect;

    private Rigidbody2D _rigidbody;

    private bool _isOverlap = true;     // 他の地形と重なっているか
    private float _area = 0.0f;         // 地形の面積

    public TerrainSettingsA TerrainSettings => _terrainSettings;
    public TerrainParameterA TerrainParameter => _terrainParameter;
    public TerrainShape TerrainShape => _terrainShape;
    public TerrainDestruct TerrainDestruct => _terrainDestruct;
    public Rigidbody2D Rigidbody => _rigidbody;
    public float Area => _area;


    // 地形破壊処理
    // 破壊面積を返す
    public float Destruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        if (_terrainDestruct == null)
            return 0.0f;

        DestructResult destructResult;
        destructResult = _terrainDestruct.PolygonDestruct(worldCenter, radius, crack);

        float destructArea = OnDestruct(destructResult);
        return destructArea;
    }

    // 三品怜
    // 地形にひびを入れる処理
    // 破壊面積を返す
    public float Crack(CrackData[] data, CrackParameter crack)
    {
        if (_terrainDestruct == null)
            return 0.0f;

        DestructResult destructResult;
        destructResult = _terrainDestruct.PolygonCrack(data, crack);

        float destructArea = OnDestruct(destructResult);
        return destructArea;
    }

    // 他の地形と重なっていない状態になったときに呼ぶ処理
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
                // 地形の面積から質量を設定
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

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _area = CipperUtility.GetArea(_terrainShape.Points);
    }

    // 地形破壊時の処理
    // 破壊面積を返す
    private float OnDestruct(DestructResult destructResult)
    {
        // 分離地形の生成
        for (int i = 0; i < destructResult.splitTerrainData.Count; ++i)
        {
            if (destructResult.splitTerrainData[i].area < _terrainSettings.MinArea)
                continue;

            SplitTerrainDataA splitData = destructResult.splitTerrainData[i];
            CreateSplitTerrain(splitData.path, splitData.area);
        }

        // 現在の地形形状を変更
        ChangeTerrain(destructResult.mainPath, destructResult.mainArea);

        // 破壊時エフェクトの生成
        if (_terrainDestructEffect != null)
            _terrainDestructEffect.OnDestruct(destructResult.destructPaths, destructResult.destructArea);

        return destructResult.destructArea;
    }

    // 分離地形のオブジェクトを生成する
    private void CreateSplitTerrain(IReadOnlyList<Vector2> terrainPath, float area)
    {
        TerrainContextA newTerrain = Instantiate(
            _terrainSettings.TerrainPrefab, transform.position, transform.rotation);

        // 分離地形の初期化
        newTerrain._terrainSettings = _terrainSettings;
        newTerrain._terrainParameter = _terrainParameter;
        newTerrain._area = area;

        // マテリアル引き継ぎの仮処理
        MeshRenderer meshRenderer = newTerrain.GetComponent<MeshRenderer>();
        if(meshRenderer != null)
        {
            meshRenderer.sharedMaterial = _terrainParameter.Material;
        }

        // 関連コンポーネントの初期化
        if (newTerrain._terrainShape != null)
        {
            newTerrain._terrainShape.Initialize(terrainPath);
        }

        // 重なった地形情報を引き継ぐ
        if (!_isOverlap)
        {
            newTerrain.OnOverlapEmpty();
        }
    }

    // 現在の地形形状を変更する
    private void ChangeTerrain(IReadOnlyList<Vector2> terrainPath, float area)
    {
        // 最小面積以下の場合は地形を削除する
        if (area < _terrainSettings.MinArea)
        {
            Destroy(gameObject);
            return;
        }

        // 関連コンポーネントの更新
        if (_terrainShape != null)
        {
            _terrainShape.UpdatePoints(terrainPath);
        }
    }
}
