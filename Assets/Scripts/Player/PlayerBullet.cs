using UnityEditor.U2D.Aseprite;
using UnityEngine;

/// <summary>
/// プレイヤーの弾オブジェクトを制御するコンポーネント
/// CircleCastで移動範囲内の衝突を正確に取得する
/// </summary>
public class PlayerBullet : MonoBehaviour
{
    [SerializeField, Tooltip("弾の移動速度")]
    [Range(0.0f, 100.0f)]
    private float _speed = 30.0f;

    [SerializeField, Tooltip("CircleCastの判定半径")]
    [Range(0.0f, 1.0f)]
    private float _radius = 0.1f;

    private Vector2 _direction;        // 移動方向
    private float _explodeRadius;      // 爆発半径
    private LayerMask _hitLayer;       // 衝突レイヤー
    private LayerMask _destructibleLayer;  // 破壊可能地形のレイヤー
    private PlayerFever playerFever;
    public PlayerFever PlayerFever => playerFever;


    /// <summary>
    /// 弾を初期化する
    /// PlayerShooterから発射時に呼び出す
    /// </summary>
    public void Init(Vector2 direction, float explodeRadius, LayerMask hitLayer, float range, LayerMask destructibleLayer, PlayerFever fever)
    {
        _direction = direction.normalized;
        _explodeRadius = explodeRadius;
        _hitLayer = hitLayer;
        _destructibleLayer = destructibleLayer;
        playerFever = fever;

        // 射程距離と速度から生存時間を計算する
        float lifetime = range / _speed;
        Destroy(gameObject, lifetime);
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
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            hitPoint, _explodeRadius, _hitLayer);

        float area = 0.0f;  // 破壊面積

        foreach (Collider2D collider in hitColliders)
        {
            if (collider.TryGetComponent(out TerrainContext terrain))
            {
                CrackParameter crack;
                crack.direction = _direction;
                crack.angleNoise = 240.0f;
                crack.minCrackCount = 1;
                crack.maxCrackCount = 2;
                area += terrain.Destruct(hitPoint, _explodeRadius, crack);
            }
        }

        playerFever.Charge(area);
        Debug.Log(area);
    }
}