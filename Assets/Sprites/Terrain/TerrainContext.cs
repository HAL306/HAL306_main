using UnityEngine;
using System;
using System.Collections.Generic;


/// <summary>
/// 地形のコアコンポーネント
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class TerrainContext : MonoBehaviour
{
    [SerializeField, Tooltip("地形の詳細設定")]
    private TerrainSettings _terrainSettings;

    [SerializeField, Tooltip("地形のパラメータ")]
    private TerrainParameter _terrainParameter;

    [SerializeField, Tooltip("開始地点で存在している地形フラグ")]
    private bool _isStartTerrain = false;


    private TerrainPolygon _terrainPolygon;         // 地形形状

    private PolygonCollider2D _polygonCollider;
    private Rigidbody2D _rigidbody;
    private Action _onDestructEvent;                // 地形変更時イベント


    public TerrainSettings TerrainSettings => _terrainSettings;
    public TerrainParameter TerrainParameter => _terrainParameter;
    public TerrainPolygon TerrainPolygon => _terrainPolygon;
    public Rigidbody2D Rigidbody => _rigidbody;


    // 分離時の初期化処理
    public void InitializeOnSplit(SplitTerrainData splitTerrain)
    {
        _terrainPolygon.Initialize(this, splitTerrain);
        OnChangeTerrain();
    }

    // 地形破壊処理
    public void Destruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        List<SplitTerrainData> splitTerrains;
        splitTerrains = _terrainPolygon.PolygonDestruct(worldCenter, radius, crack);

        for(int i = 0;i< splitTerrains.Count;++i)
        {
            CreateSplitTerrain(splitTerrains[i]);
        }
        OnChangeTerrain();
    }

    // 地形変更時イベントを登録する
    public void AddChangeTerrainEvent(Action onDestructEvent)
    {
        _onDestructEvent += onDestructEvent;
    }


    private void Awake()
    {
        _terrainPolygon = new TerrainPolygon();
        _polygonCollider = GetComponent<PolygonCollider2D>();
        _rigidbody = GetComponent<Rigidbody2D>();

        if (_isStartTerrain)
        {
            // コライダー形状を地形パスとして利用
            List<Vector2[]> terrainPath = new List<Vector2[]>(_polygonCollider.pathCount);
            for (int i = 0; i < _polygonCollider.pathCount; ++i)
            {
                terrainPath.Add(_polygonCollider.GetPath(i));
            }
            _terrainPolygon.Initialize(this, terrainPath);
        }
    }

    private void Start()
    {
        if (_isStartTerrain)
        {
            OnChangeTerrain();
        }
    }


    private void CreateSplitTerrain(SplitTerrainData splitTerrain)
    {
        TerrainContext newTerrain = Instantiate(
            _terrainSettings.BaseTerrainPrefab, transform.position, transform.rotation);

        newTerrain.InitializeOnSplit(splitTerrain);
        newTerrain._terrainParameter = _terrainParameter;
    }

    // 地形変更時の処理を行う
    private void OnChangeTerrain()
    {
        // コライダー形状を更新
        UpdateCollider();

        if (_rigidbody != null)
        {
            // 重さを設定
            _rigidbody.mass = _terrainPolygon.Area * _terrainParameter.Density;
        }

        // 最小サイズより小さくなったら削除
        if (_terrainPolygon.Area < _terrainSettings.MinArea)
        {
            Destroy(this.gameObject);
            return;
        }

        // 他のコンポーネントの地形破壊時イベント呼び出し
        if (_onDestructEvent != null)
            _onDestructEvent.Invoke();
    }

    // コライダー形状を更新する
    private void UpdateCollider()
    {
        List<EdgeLoop> terrainPath = _terrainPolygon.TerrainPath;
        _polygonCollider.pathCount = terrainPath.Count;
        for (int i = 0; i < terrainPath.Count; ++i)
        {
            // エッジループ簡略化
            List<Vector2> path = new List<Vector2>(terrainPath[i].points);
            path = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(path, 0.1f);

            // コライダー形状を更新
            _polygonCollider.SetPath(i, path);
        }
    }
}
