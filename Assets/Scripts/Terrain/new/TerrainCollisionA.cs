using UnityEngine;


/// <summary>
/// 地形の衝突時処理を行うコンポーネント
/// </summary>
[RequireComponent(typeof(TerrainContextA))]
public class TerrainCollisionA : MonoBehaviour
{
    private TerrainContextA _terrainContext;


    private void Awake()
    {
        if (_terrainContext == null)
            _terrainContext = GetComponent<TerrainContextA>();
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        //プレイヤーか判定
        if (collision.gameObject.tag == "Player")
        {
            // プレイヤーのスクリプトを取得する
            if (collision.gameObject.TryGetComponent(out PlayerMove player))
            {
                // プレイヤー側の破壊フラグがOFFなら処理を中断する
                if (!player._isDestroyCrystalByWeight) return;
            }
        }

        float impact = GetImpact(collision);
        if (impact < 5.0f)
            return;

        CrackParameter crack;
        crack.direction = collision.relativeVelocity.normalized;
        crack.angleNoise = 90.0f;
        crack.minCrackCount = 1;
        crack.maxCrackCount = 2;

        // さらに補正して破壊範囲作成
        float radius = Mathf.Pow(impact, 0.3f) * 0.1f;
        _terrainContext.Destruct(collision.contacts[0].point, radius, crack);
    }


    // 衝撃の強さを求める
    private float GetImpact(Collision2D collision)
    {
        Rigidbody2D otherRigid = collision.rigidbody;

        float speed = collision.relativeVelocity.magnitude;

        // ぶつかったオブジェクトの質量比を求める
        float massRatio = 1.0f;
        if (otherRigid != null)
        {
            massRatio = otherRigid.mass / _terrainContext.Area;
        }

        // 重さにより変化が大きくなりすぎないよう補正
        massRatio = Mathf.Sqrt(massRatio);
        return massRatio * speed / Mathf.Pow(_terrainContext.Area, 0.2f);
    }
}
