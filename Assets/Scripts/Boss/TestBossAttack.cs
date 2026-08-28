using UnityEngine;

public class TestBossAttack : BossAttackBase
{
    [SerializeField] private float coolTime = 0.0f;
    [SerializeField] private SpriteRenderer sRenderer = null;
    [SerializeField] private float attackTime = 0.0f;

    private float attackTimer = 0.0f;
    private float endTime = -1000.0f;

    public override bool CanExecute()
    {
        // クールタイムの判定
        if (Time.time - endTime < coolTime)
            return false;

        return true;
    }

    // BeginAttack時に呼ばれる
    protected override void OnBegin()
    {
        Debug.Log("テスト攻撃開始");
        sRenderer.enabled = false;
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer > attackTime)
        {
            // 攻撃終了
            EndAttack();
        }
    }

    // EndAttack時に呼ばれる（必要なら）
    protected override void OnEnd()
    {
        attackTimer = 0.0f;
        sRenderer.enabled = true;

        // 終了時刻を記録する
        endTime = Time.time;

        Debug.Log("テスト攻撃終了");
    }
}