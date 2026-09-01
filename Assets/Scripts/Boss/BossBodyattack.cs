using Unity.VisualScripting;
using UnityEngine;

public class BossBodyAttack : BossAttackBase
{
    //プレイヤーの位置
    [SerializeField] 
    private Transform player;

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
    //射程内に入ってからの時間計測
    private float currentDistanceCount = 0.0f;

    private float endTime = -10.0f;

    // 攻撃開始時のプレイヤーのPositionを記録する変数
    private Vector3 playerPosition;

    // 攻撃時のBossの位置を記録する変数
    private Vector3 bossPosition;

    //プレイヤーへのベクトル（正規化）
    private Vector3 directionToPlayer = Vector3.zero;
    public override bool CanExecute()
    {
        Debug.Log("aa");
        // クールタイムの判定
        if (Time.time - endTime < coolTime)
            return false;

        //プレイヤーとの距離を計算
        float distanceToPlayerX = Mathf.Abs(player.position.x - transform.position.x );
        float distanceToPlayerY = Mathf.Abs(player.position.y - transform.position.y );

        // 距離内にいる時間の計測
        if (distanceToPlayerX <= distancePosX &&
            distanceToPlayerX <= distancePosY)
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
        //プレイヤーへのベクトル（正規化）
        directionToPlayer = (playerPosition - transform.position).normalized;
    }

    private void FixedUpdate()
    {
        //プレイヤーの方向に向かって移動する
        transform.position += directionToPlayer * attackSpeed;

        //攻撃範囲を超えたら攻撃終了
        if (Vector3.Distance(bossPosition, transform.position) >= attackRange)
        {
            EndAttack();
        }
    }
}
