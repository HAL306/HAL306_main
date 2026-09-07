using UnityEngine;

public class BossStreetPunch : BossAttackBase
{
    // プレイヤーの位置
    [SerializeField] private Transform playerPos;
    Transform playerDir;
    [SerializeField]　private Renderer bossRenderer;

    // 攻撃の瞬間プレイヤーの位置を記録するための変数
    public override bool CanExecute()
    {
        if (IsVisible())
        {
            Debug.Log("Bossが画面内にいるため、StreetPunchに移行");
            return true;
        }
        else
        {
            Debug.Log("Bossが画面外のため、StreetPunchに移行しない");
            return false;
        }
    }
        // Bossが画面に映っているか確認する
    private bool IsVisible()
    {
        if (Camera.main == null)
        {
            Debug.Log("Main Camera が見つからない");
            return false;
        }

        if (bossRenderer == null)
        {
            Debug.Log("bossRenderer がないため画面内判定できない");
            return false;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        bool visible = GeometryUtility.TestPlanesAABB(planes, bossRenderer.bounds);

        return visible;
    }
    protected override void OnBegin()
    {
        Debug.Log("テスト攻撃開始");
        //プレイヤーへのベクトル（正規化）
        playerDir = (playerPos.position - transform.position).normalized;
    }
}
