using UnityEngine;

public class BossPunchFist : MonoBehaviour
{
    // 拳の出発地点
    private Vector2 startPosition;

    // 拳の着弾点
    private Vector2 targetPosition;

    // 拳が飛んでいる時間
    private float moveTimer;

    // 拳が着弾するまでの時間
    private float moveTime;

    // 移動中の高さ
    private float arcHeight;

    // 地形を破壊する範囲
    private float destructRadius;

    // 地形破壊時のひび割れ設定
    private CrackParameter crackParameter;

    // 地形破壊の感覚を管理するタイマー
    private float destructTimer;

    // 拳の初期設定
    public void Initialize(Vector2 start, Vector2 target, float time, float height, float radius, CrackParameter crack)
    {
        startPosition = start;
        targetPosition = target;
        moveTime = time;
        arcHeight = height;
        destructRadius = radius;
        crackParameter = crack;

        moveTimer = 0.0f;

        transform.position = startPosition;
    }

    private void Update()
    {
        // 時間を進める
        moveTimer += Time.deltaTime;

        // 0~1の割合に変換する
        float t = moveTimer / moveTime;
        t = Mathf.Clamp01(t);

        // 出発地点から着弾点へ移動する
        Vector2 position = Vector2.Lerp(startPosition, targetPosition, t);

        // 放物線上に飛ばす
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

        // 位置を反映
        transform.position = position;

        // 着弾したら消す
        if (t >= 1.0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Fieldタグ以外は破壊しない
        if (!collision.CompareTag("Field")) return;

        // 地形破壊用コンポーネントを取得
        TerrainContext terrain = collision.GetComponentInParent<TerrainContext>();

        // TerrainContextがない場合は破壊できない
        if (terrain == null) return;

        // 一定時間ごとに地形を破壊する
        destructTimer += Time.deltaTime;

        if (destructTimer >= 0.1f)
        {
            terrain.Destruct(transform.position, destructRadius, crackParameter);
            destructTimer = 0.0f;
        }
    }
}
