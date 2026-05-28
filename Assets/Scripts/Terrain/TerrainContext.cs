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

    [SerializeField, Tooltip("ベース地形のレイヤー")]
    private LayerMask _baseTerrainLayer;


    private TerrainPolygon _terrainPolygon;         // 地形形状
    private Action _onChangeTerrainEvent;           // 地形変更時イベント

    private PolygonCollider2D _polygonCollider;
    private Rigidbody2D _rigidbody;
    private TerrainDestructEffect _destructEffect;

    private List<Collider2D> _overlapColliderList;      // 重なっているコライダーのリスト
    private float _mass;


    public TerrainSettings TerrainSettings => _terrainSettings;
    public TerrainParameter TerrainParameter => _terrainParameter;
    public TerrainPolygon TerrainPolygon => _terrainPolygon;
    public PolygonCollider2D PolygonCollider => _polygonCollider;
    public Rigidbody2D Rigidbody => _rigidbody;
    public float Mass => _mass;


    // 分離時の初期化処理
    public void InitializeOnSplit(SplitTerrainData splitTerrain)
    {
        _terrainPolygon.Initialize(this, splitTerrain);
    }

    // 地形破壊処理
    public void Destruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        List<SplitTerrainData> splitTerrains;
        splitTerrains = _terrainPolygon.PolygonDestruct(worldCenter, radius, crack);

        for(int i = 0;i< splitTerrains.Count;++i)
        {
            // 地形分離
            CreateSplitTerrain(splitTerrains[i]);
        }
        OnChangeTerrain();
    }

    // 地形変更時イベントを登録する
    public void AddChangeTerrainEvent(Action onDestructEvent)
    {
        _onChangeTerrainEvent += onDestructEvent;
    }

    private void Awake()
    {
        _terrainPolygon = new TerrainPolygon();
        _polygonCollider = GetComponent<PolygonCollider2D>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _destructEffect = GetComponent<TerrainDestructEffect>();

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


    // 分離地形のオブジェクトを生成する
    private void CreateSplitTerrain(SplitTerrainData splitTerrain)
    {
        TerrainContext newTerrain = Instantiate(
            _terrainSettings.BaseTerrainPrefab, transform.position, transform.rotation);

        // 分離地形の初期化
        newTerrain.InitializeOnSplit(splitTerrain);
        newTerrain._terrainSettings = _terrainSettings;
        newTerrain._terrainParameter = _terrainParameter;
        newTerrain._overlapColliderList = new List<Collider2D>(_overlapColliderList);

        newTerrain.OnChangeTerrain();
    }

    // 地形変更時の処理を行う
    private void OnChangeTerrain()
    {
        // 最小サイズより小さくなったら削除
        if (_terrainPolygon.Area < _terrainSettings.MinArea)
        {
            if (_destructEffect != null)
                _destructEffect.EmitDestructEffect(_terrainPolygon.TerrainPaths);

            Destroy(this.gameObject);
            return;
        }

        // コライダー形状を更新
        UpdateCollider();

        if (_rigidbody == null)
        {
            if (_overlapColliderList == null)
            {
                // 重なったコライダーを取得する
                GetOverlapCollider();

                if (_overlapColliderList.Count == 0)
                    AddRigidbody();
            }
            else
            {
                if (!CheckOverlapCollider())
                    AddRigidbody();
            }
        }
        else
        {
            // 重さを設定
            _mass = _terrainPolygon.Area * _terrainParameter.Density;
            _rigidbody.mass = _mass;
        }

        // 他のコンポーネントの地形破壊時イベント呼び出し
        if (_onChangeTerrainEvent != null)
            _onChangeTerrainEvent.Invoke();
    }

    // コライダー形状を更新する
    private void UpdateCollider()
    {
        List<EdgeLoop> terrainPath = _terrainPolygon.TerrainPaths;
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

    // 重なっているベース地形のコライダーを取得する (厳密な判定は行いません)
    private void GetOverlapCollider()
    {
        _overlapColliderList = new List<Collider2D>();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.layerMask = _baseTerrainLayer;
        filter.useLayerMask = true;
        filter.useTriggers = false;

        // 重なっているベース地形のコライダー取得
        _polygonCollider.Overlap(filter, _overlapColliderList);
    }

    // ベース地形との重なりを調べる
    private bool CheckOverlapCollider()
    {
        for(int i = 0; i < _overlapColliderList.Count ; ++i)
        {
            if (_overlapColliderList[i] == null)
            {
                // リストから削除して、インデックスを補正
                _overlapColliderList.RemoveAt(i);
                i--;
            }

            // 重なりを調べる
            ColliderDistance2D distance;
            distance = _polygonCollider.Distance(_overlapColliderList[i]);

            // 重なっていなければ除外
            if(distance.isOverlapped)
            {
                // 一つでも重なっていれば終了
                break;
            }
            else
            {
                // リストから削除して、インデックスを補正
                _overlapColliderList.RemoveAt(i);
                i--;
            }
        }

        return _overlapColliderList.Count != 0;
    }

    // Rigidbodyコンポーネントを追加し、初期設定を行う
    private void AddRigidbody()
    {
        if (_rigidbody != null)
            return;

        _rigidbody = gameObject.AddComponent<Rigidbody2D>();

        // 重さを設定
        _mass = _terrainPolygon.Area * _terrainParameter.Density;
        _rigidbody.mass = _mass;
    }
}
