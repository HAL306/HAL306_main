using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 地形のコアコンポーネント
/// </summary>
[RequireComponent(typeof(PolygonCollider2D), typeof(MeshFilter), typeof(MeshDotRenderer))]
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

    [Header("サウンド設定")]
    [SerializeField, Tooltip("結晶の破壊音を再生するか")]
    private bool _enableBreakSound = true;

    [SerializeField, Tooltip("小さいひび割れ音（複数からランダム再生）")]
    private AudioClip[] _smallCrackSounds;

    [SerializeField, Tooltip("大きいひび割れ音（複数からランダム再生・完全破壊時にも使用）")]
    private AudioClip[] _bigCrackSounds;

    [SerializeField, Tooltip("小と大のひびを区別する面積のしきい値")]
    private float _bigCrackAreaThreshold = 1.0f;

    [SerializeField, Tooltip("再生ピッチの範囲（最小）")]
    [Range(0.1f, 3.0f)] private float _pitchMin = 0.85f;

    [SerializeField, Tooltip("再生ピッチの範囲（最大）")]
    [Range(0.1f, 3.0f)] private float _pitchMax = 1.15f;

    [SerializeField, Tooltip("再生音量の範囲（最小）")]
    [Range(0.0f, 1.0f)] private float _volumeMin = 0.7f;

    [SerializeField, Tooltip("再生音量の範囲（最大）")]
    [Range(0.0f, 1.0f)] private float _volumeMax = 1.0f;

    [SerializeField, Tooltip("同種の音の最短再生間隔（秒）")]
    [Range(0.0f, 1.0f)] private float _soundInterval = 0.03f;

    private static AudioSource _sfxSource;
    private static readonly Dictionary<SoundEffectType, float> _lastPlayTime = new Dictionary<SoundEffectType, float>();
    private float _lastActionArea;

    private TerrainPolygon _terrainPolygon;         // 地形形状
    private Action _onChangeTerrainEvent;           // 地形変更時イベント

    private PolygonCollider2D _polygonCollider;
    private Rigidbody2D _rigidbody;
    private TerrainDestructEffect _destructEffect;

    private List<Collider2D> _overlapColliderList;  // 重なっているコライダーのリスト
    private float _mass;
    
    private MeshFilter _meshFilter;
    private MeshDotRenderer _dotRenderer;
    private TerrainCollision _collision;

    static private BOSScharge _boss;

    public TerrainSettings TerrainSettings => _terrainSettings;
    public TerrainParameter TerrainParameter => _terrainParameter;
    public MeshDotRenderer DotRenderer => _dotRenderer;
    public TerrainPolygon TerrainPolygon => _terrainPolygon;
    public PolygonCollider2D PolygonCollider => _polygonCollider;
    public Rigidbody2D Rigidbody => _rigidbody;
    public MeshFilter MeshFilter => _meshFilter;
    public float Mass => _mass;

    private enum SoundEffectType
    {
        SMALL_CRACK,
        BIG_CRACK,
    };

    // 分離時の初期化処理
    public void InitializeOnSplit(SplitTerrainData splitTerrain)
    {
        _terrainPolygon.Initialize(this, splitTerrain);
    }

    // 地形破壊処理 (破壊面積を返す)
    public float Destruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        List<SplitTerrainData> splitTerrains = _terrainPolygon.PolygonDestruct(worldCenter, radius, crack);
        float area = _terrainPolygon.GetArea(_terrainPolygon.DestructPaths);
        _lastActionArea = area;

        for (int i = 0; i < splitTerrains.Count; ++i)
        {
            CreateSplitTerrain(splitTerrains[i]);
        }
        OnChangeTerrain();
        return area;
    }

    // 地形にひびを入れる処理 (破壊面積を返す)
    public float Crack(CrackData[] data, CrackParameter crack)
    {
        List<SplitTerrainData> splitTerrains = _terrainPolygon.PolygonCrack(data, crack);
        float area = _terrainPolygon.GetArea(_terrainPolygon.DestructPaths);
        _lastActionArea = area;

        for (int i = 0; i < splitTerrains.Count; ++i)
        {
            CreateSplitTerrain(splitTerrains[i]);
        }
        OnChangeTerrain();
        return area;
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
        _meshFilter = GetComponent<MeshFilter>();
        _dotRenderer = GetComponent<MeshDotRenderer>();

        if (_dotRenderer == null)
        {
            _dotRenderer = gameObject.AddComponent<MeshDotRenderer>();
        }

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

    private void Update()
    {
        if (_rigidbody != null && Camera.main != null)
        {
            // 画面外の場合はRigidbodyを無効化する
            Bounds bounds = _polygonCollider.bounds;
            Vector3 camPos = Camera.main.transform.position;
            float space = 1.0f; // 画面外判定の余白
            float halfHeight = Camera.main.orthographicSize + space;
            float halfWidth = halfHeight * Camera.main.aspect;

            bool inCamera =
                bounds.max.x >= camPos.x - halfWidth &&
                bounds.min.x <= camPos.x + halfWidth &&
                bounds.max.y >= camPos.y - halfHeight &&
                bounds.min.y <= camPos.y + halfHeight;

            _rigidbody.simulated = inCamera;
        }

        // 不要なオブジェクト削除
        if (_boss == null)
        {
            _boss = FindAnyObjectByType<BOSScharge>();
            if (_boss == null)
                return;
        }
        if (_polygonCollider != null && _polygonCollider.bounds.max.x < _boss.transform.position.x)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // MeshDotManager の登録解除処理は不要となったため安全にクリーンアップ
        _onChangeTerrainEvent = null;
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
        newTerrain._overlapColliderList = _overlapColliderList != null ? new List<Collider2D>(_overlapColliderList) : new List<Collider2D>();

        newTerrain.OnChangeTerrain();
    }

    // 地形変更時の処理を行う
    private void OnChangeTerrain()
    {
        // 最小サイズより小さくなったら削除
        if (_terrainPolygon.Area < _terrainSettings.MinArea)
        {
            if (_destructEffect != null)
            {
                _destructEffect.EmitDestructEffect(_terrainPolygon.DestructPaths);
            }
            PlaySoundEffect(SoundEffectType.BIG_CRACK);
            Destroy(this.gameObject);
            return;
        }

        // コライダー形状を更新
        UpdateCollider();

        if (_rigidbody == null)
        {
            if (_overlapColliderList == null)
            {
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
        _onChangeTerrainEvent?.Invoke();

        // メッシュのポリゴン生成およびMeshDotRendererのドット再構築
        if (_meshFilter != null)
        {
            _terrainPolygon.GenerateMesh(_meshFilter);
        }

        if (_dotRenderer != null)
        {
            _dotRenderer.RebuildDots();
        }
    }

    // コライダー形状を更新する
    private void UpdateCollider()
    {
        List<EdgeLoop> terrainPath = _terrainPolygon.TerrainPaths;
        _polygonCollider.pathCount = terrainPath.Count;

        PlaySoundEffect(_lastActionArea >= _bigCrackAreaThreshold ? SoundEffectType.BIG_CRACK : SoundEffectType.SMALL_CRACK);

        for (int i = 0; i < terrainPath.Count; ++i)
        {
            List<Vector2> path = new List<Vector2>(terrainPath[i].points);
            path = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(path, 0.5f);
            _polygonCollider.SetPath(i, path);
        }
    }

    // 重なっているベース地形のコライダーを取得する
    private void GetOverlapCollider()
    {
        _overlapColliderList = new List<Collider2D>();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.layerMask = _baseTerrainLayer;
        filter.useLayerMask = true;
        filter.useTriggers = false;

        _polygonCollider.Overlap(filter, _overlapColliderList);
    }

    // ベース地形との重なりを調べる
    private bool CheckOverlapCollider()
    {
        for (int i = 0; i < _overlapColliderList.Count; ++i)
        {
            if (_overlapColliderList[i] == null)
            {
                _overlapColliderList.RemoveAt(i);
                i--;
                continue;
            }

            ColliderDistance2D distance = _polygonCollider.Distance(_overlapColliderList[i]);

            if (distance.isOverlapped)
            {
                break;
            }
            else
            {
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

        _mass = _terrainPolygon.Area * _terrainParameter.Density;
        _rigidbody.mass = _mass;
    }

    private void PlaySoundEffect(SoundEffectType soundEffect)
    {
        if (!_enableBreakSound)
            return;

        if (_soundInterval > 0.0f)
        {
            if (_lastPlayTime.TryGetValue(soundEffect, out float last) &&
                Time.time - last < _soundInterval)
                return;
            _lastPlayTime[soundEffect] = Time.time;
        }

        AudioClip[] clipPool = soundEffect == SoundEffectType.BIG_CRACK ? _bigCrackSounds : _smallCrackSounds;
        if (clipPool == null || clipPool.Length == 0)
            return;

        AudioClip clip = clipPool[UnityEngine.Random.Range(0, clipPool.Length)];
        if (clip == null)
            return;

        if (_sfxSource == null)
        {
            GameObject go = new GameObject("TerrainSFX");
            _sfxSource = go.AddComponent<AudioSource>();
        }

        _sfxSource.pitch = UnityEngine.Random.Range(_pitchMin, _pitchMax);
        _sfxSource.PlayOneShot(clip, UnityEngine.Random.Range(_volumeMin, _volumeMax));
    }
}