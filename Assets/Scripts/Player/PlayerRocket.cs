using UnityEngine;

/// <summary>
/// プレイヤーの弾オブジェクトを制御するコンポーネント
/// CircleCastで移動範囲内の衝突を正確に取得する
/// </summary>
public class PlayerRocket : MonoBehaviour
{
    [SerializeField, Tooltip("弾の移動速度")]
    [Range(0.0f, 100.0f)]
    private float _speed = 30.0f;

    [SerializeField, Tooltip("CircleCastの判定半径")]
    [Range(0.0f, 1.0f)]
    private float _radius = 0.1f;


    [SerializeField, Tooltip("ひびの数")]
    [Range(3.0f, 10.0f)]
    private int crackNum = 3;

    [SerializeField, Tooltip("ひびの範囲の半径")]
    [Range(0.1f, 10.0f)]
    private float crackRadius = 0.1f;

    [SerializeField, Tooltip("爆風の範囲の半径")]
    [Range(0.1f, 10.0f)]
    private float windRadius = 0.1f;

    [SerializeField, Tooltip("爆風の強さ")]
    [Range(0.1f, 100.0f)]
    private float windPower = 0.1f;

    [SerializeField, Tooltip("ロケランチャージ倍率")]
    private float rocketChrgeRatio = 0.9f;

    private Vector2 _direction;        // 移動方向
    private float _explodeRadius;      // 爆発半径
    private LayerMask _hitLayer;       // 衝突レイヤー
    private LayerMask _destructibleLayer;  // 破壊可能地形のレイヤー

    [SerializeField]
    private CrackData[] crackDatas;     // ひびのデータ

    private PlayerFever playerFever;

    private PlayerRocketShooter playerRocketShooter;

    /// <summary>
    /// 弾を初期化する
    /// PlayerShooterから発射時に呼び出す
    /// </summary>
    public void Init(Vector2 direction, float explodeRadius, LayerMask hitLayer, float range, LayerMask destructibleLayer,
        PlayerFever fever,PlayerRocketShooter shooter)
    {
        _direction = direction.normalized;
        _explodeRadius = explodeRadius;
        _hitLayer = hitLayer;
        _destructibleLayer = destructibleLayer;
        playerFever = fever;
        playerRocketShooter = shooter;

        // 射程距離と速度から生存時間を計算する
        float lifetime = range / _speed;
        Destroy(gameObject, lifetime);

        // ひびのデータを初期化
        crackDatas = new CrackData[crackNum * 2];

        // 放射状にのびるひび
        float angle = 0.0f;
        float angleDelta = (360.0f / crackNum) * Mathf.Deg2Rad;
        for (int i = 0; i < crackNum; i++)
        {
            crackDatas[i].pos = new Vector2(0.0f, 0.0f);
            crackDatas[i].dir.x = Mathf.Cos(angle);
            crackDatas[i].dir.y = Mathf.Sin(angle);
            crackDatas[i].length = crackRadius;
            angle += angleDelta;
        }

        // 円周を繋ぐひび
        angle = (90.0f * Mathf.Deg2Rad + angleDelta * 0.5f);
        float temp = (Mathf.PI - angleDelta) * 0.5f;
        for (int i = 0; i < crackNum; i++)
        {
            crackDatas[crackNum + i].pos = crackDatas[i].pos + crackDatas[i].dir * crackDatas[i].length;

            crackDatas[crackNum + i].dir.x = Mathf.Cos(angle);
            crackDatas[crackNum + i].dir.y = Mathf.Sin(angle);

            crackDatas[crackNum + i].length = crackRadius * Mathf.Sin(angleDelta) / Mathf.Sin(temp);
            angle += angleDelta;
        }
    }

    private void Update()
    {
        Move();
    }

    // 移動処理 + CircleCastによる衝突判定
    private void Move()
    {
        float moveDist = _speed * Time.deltaTime;

        // 移動範囲内の衝突をCircleCastで取得
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, _radius, _direction, moveDist, _hitLayer);

        if (hit.collider != null)
        {
            // 破壊可能地形の場合のみ破壊処理を呼び出す
            bool isDestructible = (_destructibleLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
            if (isDestructible)
            {
                HitDestruct(hit.point);
            }

            // どの地形に当たっても弾は消滅する
            Destroy(gameObject);
            return;
        }

        // 衝突なし：移動を継続
        transform.Translate(_direction * moveDist);
    }

    // 破壊処理
    private void HitDestruct(Vector2 hitPoint)
    {
        float area = 0.0f;  // 破壊面積

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            hitPoint, _explodeRadius, _hitLayer);

        foreach (Collider2D collider in hitColliders)
        {
            if (collider.TryGetComponent(out TerrainContext terrain))
            {
                CrackParameter crack;
                crack.direction = _direction;
                crack.angleNoise = 240.0f;
                crack.minCrackCount = 0;
                crack.maxCrackCount = 0;
                area += terrain.Destruct(hitPoint, _explodeRadius, crack);
            }
        }

        // ひびいれる
        Collider2D[] crackColliders = Physics2D.OverlapCircleAll(
            hitPoint, crackRadius, _destructibleLayer);

        // 位置更新
        for (int i = 0; i < crackDatas.Length; i++)
        {
            crackDatas[i].pos.x += transform.position.x;
            crackDatas[i].pos.y += transform.position.y;
        }
        
        foreach (Collider2D collider in crackColliders)
        {
            if (collider.TryGetComponent(out TerrainContext terrain))
            {
                CrackParameter crack;
                crack.direction = _direction;
                crack.angleNoise = 240.0f;
                crack.minCrackCount = 0;
                crack.maxCrackCount = 0;
                // ひび入れる
                area += terrain.Crack(crackDatas, crack);
            }
        }

        playerFever.Charge(area * rocketChrgeRatio);
        playerRocketShooter.Charge(area);

        // 爆風
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            hitPoint, windRadius, _destructibleLayer);


        foreach (Collider2D collider in colliders)
        {
            if (collider.TryGetComponent(out TerrainContext terrain))
            {
                // 向き計算
                Vector2 dir = Vector2.zero;
                dir.x = collider.bounds.center.x - hitPoint.x;
                dir.y = collider.bounds.center.y - hitPoint.y;
                dir.Normalize();

                if(collider.attachedRigidbody != null)
                    collider.attachedRigidbody.linearVelocity = dir * windPower;
            }
        }

    }
}