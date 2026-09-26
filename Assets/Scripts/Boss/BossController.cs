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
    [SerializeField] private float attackInterval = 3.0f;  // 攻撃を行う間隔（秒）
    [Tooltip("攻撃選択を行う間隔（秒）")]
    [SerializeField] private float attackCheckInterval = 0.5f;  // 攻撃選択を行う間隔（秒）

    [Tooltip("地形破壊の間隔（秒）")]
    [SerializeField] private float destructInterval = 1.0f;  // 地形破壊の間隔（秒）
    [Tooltip("地形破壊の半径")]
    [SerializeField] private float destructRadius = 1.0f;  // 地形破壊の半径
    [Tooltip("地形破壊のひび割れパラメータ")]
    [SerializeField] private CrackParameter crackParameter;  // 地形破壊のひび割れパラメータ

    private float attackTimer = 0.0f;
    private float attackCheckTimer = 0.0f;

    private float destructTimer = 0.0f;    // 地形破壊のタイマー

    private BossAttackBase currentAttack = null;    // 現在の攻撃

    private bool isMove = true;         // 移動するかどうか

    // BOSSを物理移動させるためのRigidbody2D
    private Rigidbody2D rb;

    // BOSSのX座標を管理する変数
    private float moveX;
    public void SetIsMove(bool move)
    {
        isMove = move;
        var lineMove = GetComponent<LineMove>();
        if (lineMove != null)
        {
            lineMove.enabled = move;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // ゲーム開始時のX座標を保存する
        moveX = transform.position.x;
    }

    void Update()
    {
        //Move();

        // 攻撃中は関数を抜ける
        if (currentAttack != null) return;

        attackTimer += Time.deltaTime;
        attackCheckTimer += Time.deltaTime;

        // 攻撃の間隔が経過していて、攻撃選択の間隔も経過している場合に攻撃を決定する
        if (attackCheckTimer >= attackCheckInterval && attackTimer >= attackInterval)
        {
            attackCheckTimer = 0.0f;
            BossAttackBase nextAttack = DecideNextAttack();

            if (nextAttack != null)
            {
                // 攻撃開始
                currentAttack = nextAttack;

                // 攻撃が始まったので攻撃タイマーをリセットする
                attackTimer = 0.0f;

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
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Fieldタグ以外に触れている場合は処理しない
        if (collision.gameObject.layer != LayerMask.NameToLayer("Field")) return;

        // 触れている地形からTerrainContextを取得する
        TerrainContext terrain = collision.gameObject.GetComponentInParent<TerrainContext>();

        // TerrainContextが無ければ破壊できない
        if (terrain == null) return;

        // 地形破壊処理を行う
            terrain.Destruct(transform.position, destructRadius, crackParameter);
        BreakTerrain(terrain);
    }


    // 地形を一定時間ごとに破壊する処理
    private void BreakTerrain(TerrainContext terrain)
    {
        // 破壊間隔のタイマーを進める
        destructTimer += Time.deltaTime;

        // 指定時間を超えたら地形を破壊する
        if (destructTimer >= destructInterval)
        {
            // BOSSの現在位置を中心に地形を削る
            terrain.Destruct(transform.position, destructRadius, crackParameter);

            // タイマーをリセットする
            destructTimer = 0.0f;
        }
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
