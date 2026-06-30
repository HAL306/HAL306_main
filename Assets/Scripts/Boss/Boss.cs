using UnityEngine;

public class Boss : MonoBehaviour
{
    // 地形を破壊する範囲
    [SerializeField]
    private float destructRadius = 0.5f;
    // 地形破壊時のひび割れ設定
    [SerializeField]
    private CrackParameter crackParameter;

    // プレイヤーの位置を取得するための参照
    [SerializeField]
    private Transform player;
    // この距離以上プレイヤーと離れたら突進する
    [SerializeField]
    private float maxDistance = 20.0f;
    // この距離以上プレイヤーと離れたらスピードが速くなる
    [SerializeField]
    private float middleDistance = 12.0f;
    // 突進後、プレイヤーからどれくらい離れた位置で止まるか
    [SerializeField]
    private float closeDistance = 10.0f;
    // 突進するときの速度
    [SerializeField]
    private float dashSpeed = 30.0f;
    // 早い時の速度
    [SerializeField]
    private float middleSpeed = 8.0f;
    // 通常時の右移動速度
    public float speed = 3.0f;
    // 上下移動の幅
    public float moveHeight = 2.0f;
    // 上下移動の速さ
    public float moveSpeed = 2.0f;
    // 地形破壊の間隔を管理するタイマー
    private float destructTimer;
    // 突進中かどうか
    private bool isDashing;
    // BossのX座標管理用
    private float moveX;

    // Rigidbody2D
    private Rigidbody2D rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 現在のX座標を保存
        moveX = transform.position.x;
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            Debug.Log("playerが入っていない");
            return;
        }

        // プレイヤーとの横方向の距離
        float distanceX = Mathf.Abs(moveX - player.position.x);

        BOSSSpeedState(distanceX);
        BOSSMoveY();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Field")) return;
        // Fieldタグの地形に触れている間だけ処理する
        if (collision.CompareTag("Field"))
        {
            TerrainContext terrain = collision.GetComponent<TerrainContext>();

            // TerrainContextが無ければ破壊できない
            if (terrain == null)
            {
                return;
            }

            BreakTerrain(terrain);
        }
    }

    //  BOSSの動きの変化
    private void BOSSSpeedState(float distanceX)
    {
        // 一定以上離れたら突進開始
        if (distanceX >= maxDistance)
        {
            isDashing = true;
        }

        if (isDashing)
        {
            LongBOSSSpeed();
        }
        else
        {
            if (distanceX >= middleDistance)
            {
                MiddleBOSSSpeed();
            }
            else
            {
                ShortBOSSSpeed();
            }
        }

    }
    // BOSSの移動処理
    private void BOSSMoveY()
    {
        // プレイヤーのY座標を中心に上下移動する
        float y = player.position.y + Mathf.Sin(Time.time * moveSpeed) * moveHeight;

        // 計算した位置へ移動
        rb.MovePosition(new Vector2(moveX, y));
    }
    // BOSSとPlayerが遠い時
    private void LongBOSSSpeed()
    {
        // プレイヤーの少し後ろを目標位置にする
        float targetX = player.position.x - closeDistance;

        // 目標位置まで高速で近づく
        moveX = Mathf.MoveTowards(
            moveX,
            targetX,
            dashSpeed * Time.fixedDeltaTime
        );

        // 目標位置に近づいたら突進終了
        if (Mathf.Abs(moveX - targetX) <= 0.1f)
        {
            isDashing = false;
        }
    }
    // BOSSとPlayerが中間の時
    private void MiddleBOSSSpeed()
    {
        moveX += middleSpeed * Time.fixedDeltaTime;
    }
    // BOSSとPlayerが近い時
    private void ShortBOSSSpeed()
    {
        moveX += speed * Time.fixedDeltaTime;
    }

    // 破壊設定
    private void BreakTerrain(TerrainContext terrain)
    {
        // 一定時間ごとに破壊する
        destructTimer += Time.deltaTime;

        if (destructTimer >= 0.2f)
        {
            terrain.Destruct(transform.position, destructRadius, crackParameter);

            destructTimer = 0.0f;
        }
    }
}