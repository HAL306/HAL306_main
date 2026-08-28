using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float speed;

    [Header("上下移動")]
    [SerializeField]
    private float moveHeight = 2.0f;    // プレイヤーのY座標を中心に、どれくらい上下するか

    [SerializeField]
    private float moveSpeed = 2.0f;     // 上下移動の速さ

    [Header("ボスが行う攻撃のリスト(上の方が優先度が高い)")]
    [Tooltip("ボス攻撃のリスト")]
    [SerializeField] private BossAttackBase[] attacks;          // 攻撃の配列（上が優先度高）

    [Tooltip("攻撃のクールタイム")]
    [SerializeField] private float attackCheckInterval = 0.5f;  // 判定を行う間隔（秒）

    private float attackCheckTimer = 0.0f;

    private BossAttackBase currentAttack = null;    // 現在の攻撃

    private bool isMove = true;         // 移動するかどうか

    // BOSSを物理移動させるためのRigidbody2D
    private Rigidbody2D rb;

    // BOSSのX座標を管理する変数
    private float moveX;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // ゲーム開始時のX座標を保存する
        moveX = transform.position.x;
    }

    void Update()
    {
        Move();

        // 攻撃中は関数を抜ける
        if (currentAttack != null) return;

        attackCheckTimer += Time.deltaTime;
        if (attackCheckTimer >= attackCheckInterval)
        {
            attackCheckTimer = 0.0f;
            BossAttackBase nextAttack = DecideNextAttack();

            if (nextAttack != null)
            {
                // 攻撃開始
                currentAttack = nextAttack;

                // 攻撃が終わったら currentAttack を null に戻すコールバックを渡す
                currentAttack.BeginAttack(() => currentAttack = null);
            }
        }
    }

    private BossAttackBase DecideNextAttack()
    {
        // 配列を上から順番に見て、自身の判定関数(CanExecute)が true のものを返す
        foreach (var attack in attacks)
        {
            if (attack.CanExecute())
            {
                return attack;
            }
        }
        return null;
    }

    private void Move()
    {
        if (!isMove) return;


        moveX += speed * Time.fixedDeltaTime;

        // プレイヤーのY座標を中心にする
        float y = player.position.y +
                  Mathf.Sin(Time.time * moveSpeed) * moveHeight;

        // moveXで横移動、yで上下移動した位置にBOSSを移動する
        rb.MovePosition(new Vector2(moveX, y));
    }
}
