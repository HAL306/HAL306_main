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

    //
    private bool IsMoving;

    // 
    private bool IsReturning;

    //
    private bool IsWaiting;

    // 
    private float waitTimer;

    // 戻る場所
    private Transform returnPoint;

    [SerializeField]
    private float waitTime = 3.0f;

    // 拳の初期設定
    public void Initialize(Transform startPoint, Vector2 target, float time, float height, float radius, CrackParameter crack)
    {
        returnPoint = startPoint;

        startPosition = startPoint.position;
        targetPosition = target;
        moveTime = time;
        arcHeight = height;
        destructRadius = radius;
        crackParameter = crack;

        moveTimer = 0.0f;
        waitTimer = 0.0f;
        destructTimer = 0.0f;

        IsWaiting = false;
        IsMoving = true;
        IsReturning = false;

        // 飛んでいる間はBossの子から外す
        transform.SetParent(null, true);

        transform.position = startPosition;
    }
    private void Update()
    {
        if (!IsMoving) return;

        // 着弾後数秒残る
        if (IsWaiting)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                IsWaiting = false;
                IsReturning = true;
                moveTimer = 0.0f;
            }
            return;
        }

        // 時間を進める
        moveTimer += Time.deltaTime;

        // 0~1の割合に変換する
        float t = moveTimer / moveTime;
        t = Mathf.Clamp01(t);

        // 出発地点から着弾点へ移動する
        Vector2 position;

        if (!IsReturning)
        {
            position = Vector2.Lerp(startPosition, targetPosition, t);
            position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            transform.position = position;

            // 着弾したら、帰りに切り替える
            if (t >= 1.0f)
            {
                IsWaiting = true;
                waitTimer = 0.0f;
                moveTimer = 0.0f;
                transform.position = targetPosition;

            }
        }
        else
        {
            // 帰りの移動
            Vector2 nowReturnPosition = returnPoint.position;

            position = Vector2.Lerp(targetPosition, nowReturnPosition, t);

            transform.position = position;

            // 元の位置に戻ったら停止
            if (t >= 1.0f)
            {
                IsWaiting = false;
                IsMoving = false;
                IsReturning = false;
                // Bossの手元に戻す
                transform.position = returnPoint.position;
                transform.SetParent(returnPoint, true);
                transform.localPosition = Vector3.zero;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsMoving) return;

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
