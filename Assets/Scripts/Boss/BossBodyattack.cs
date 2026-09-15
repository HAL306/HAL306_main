using UnityEngine;

public class BossBodyAttack : BossAttackBase
{
    //プレイヤーの位置
    [SerializeField]
    private Transform player;

    [Header("予告設定")]
    [SerializeField, Tooltip("Projectウィンドウにある予告マーカーのプレハブ")]
    private GameObject chargeMarker;

    // 生成元とは別に、Scene内の実体を管理する
    private GameObject chargeMarkerInstance;
    private bool isAttacking;
    [SerializeField] private float warningTime = 0.7f;
    [SerializeField] private float markerThickness = 1.0f;

    // 突進開始までのplayerとぼすとの距離
    [SerializeField]
    private float distancePosX = 10.0f;
    [SerializeField]
    private float distancePosY = 10.0f;
    //クールダウン時間の設定
    [SerializeField]
    private float coolTime = 0.0f;

    // 射程内の時の時間設定
    [SerializeField]
    private float distanceCountTimer = 0.0f;

    // どのくらいの距離まで攻撃するかを設定する変数
    [SerializeField]
    private float attackRange = 0.0f;

    // 突撃の速度
    [SerializeField]
    private float attackSpeed = 0.0f;

    // PlayerKiller
    [SerializeField]
    private PlayerKiller playerKiller;


    [SerializeField, Tooltip("チャージの時間")]
    private float chargeTime = 3.0f;

    //射程内に入ってからの時間計測
    private float currentDistanceCount = 0.0f;

    private float endTime = -10.0f;

    // 攻撃開始時のプレイヤーのPositionを記録する変数
    private Vector3 playerPosition;

    // 攻撃時のBossの位置を記録する変数
    private Vector3 bossPosition;

    //プレイヤーへのベクトル（正規化）
    private Vector3 directionToPlayer = Vector3.zero;

    // マーカー用の状態管理
    private bool isWarning;
    private float warningTimer;

    protected override void Awake()
    {
        base.Awake();
        if (playerKiller != null) playerKiller.enabled = false;
    }

    public override bool CanExecute()
    {
        if (isAttacking || player == null) return false;
        // クールタイムの判定
        if (Time.time - endTime < coolTime)
            return false;
        if (Time.time - endTime < chargeTime)
            return false;

        //プレイヤーとの距離を計算
        float distanceToPlayerX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceToPlayerY = Mathf.Abs(player.position.y - transform.position.y);

        // 距離内にいる時間の計測
        if (distanceToPlayerX <= distancePosX &&
            distanceToPlayerY <= distancePosY)
        {
            Debug.Log("範囲にいるぞ");
            // 距離内なら毎フレーム時間を加算する
            currentDistanceCount += Time.deltaTime;

            // 一定時間以上経過したら攻撃可能
            if (currentDistanceCount >= distanceCountTimer)
            {
                //攻撃できるで
                return true;
            }
        }
        else
        {
            // 距離外に出たらタイマーをリセットする
            currentDistanceCount = 0.0f;
        }

        // 条件を満たしていない場合はfalse
        return false;
    }

    protected override void OnBegin()
    {
        Debug.Log("テスト攻撃開始");
        DestroyChargeMarker();
        isAttacking = true;
        currentDistanceCount = 0.0f;
        // 予告中は攻撃判定を無効にする
        if (playerKiller != null) playerKiller.enabled = false;
        bossPosition = transform.position;
        playerPosition = player.position;
        isWarning = true;
        warningTimer = 0.0f;

        //プレイヤーへのベクトル（正規化）
        directionToPlayer = (playerPosition - transform.position).normalized;

        if (chargeMarker != null)
        {
            // 親を指定せず生成し、ボスの突進に追従させない
            chargeMarkerInstance = Instantiate(chargeMarker);
            Vector3 markerCenter = transform.position + directionToPlayer * (attackRange / 2.0f);
            chargeMarkerInstance.transform.position = new Vector3(markerCenter.x, markerCenter.y, -1.0f);

            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            chargeMarkerInstance.transform.rotation = Quaternion.Euler(0, 0, angle);

            chargeMarkerInstance.transform.localScale = new Vector3(attackRange, markerThickness, 1.0f);
            chargeMarkerInstance.SetActive(true);
        }
    }

    private void FixedUpdate()
    {
        if (!isAttacking) return;

        // 予告時間の待機処理
        if (isWarning)
        {
            warningTimer += Time.fixedDeltaTime;
            if (warningTimer >= warningTime)
            {
                isWarning = false;
                if (playerKiller != null) playerKiller.enabled = true;
            }
            return; // 待機中は元の移動処理を行わない
        }

        //プレイヤーの方向に向かって移動する
        transform.position += directionToPlayer * attackSpeed;

        //攻撃範囲を超えたら攻撃終了
        if (Vector3.Distance(bossPosition, transform.position) >= attackRange)
        {
            EndAttack();
        }
    }

    protected override void OnEnd()
    {
        DestroyChargeMarker();
        isAttacking = false;
        isWarning = false;
        directionToPlayer = Vector3.zero;
        // 攻撃終了後は攻撃判定を無効にする
        if (playerKiller != null) playerKiller.enabled = false;
        // クールタイム判定用に終了時間を記録
        endTime = Time.time;
    }
    // OnEndから呼び、途中終了でも生成した実体を片付ける
    private void DestroyChargeMarker()
    {
        if (chargeMarkerInstance == null) return;

        chargeMarkerInstance.SetActive(false);
        Destroy(chargeMarkerInstance);
        chargeMarkerInstance = null;
    }
}