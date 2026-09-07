using UnityEngine;

public class BossStraightPunch : BossAttackBase
{
    [Header("参照")]
    [SerializeField, Tooltip("プレイヤーの位置")]
    private Transform playerPos;

    [SerializeField, Tooltip("画面内判定用のボスのRenderer")]
    private Renderer bossRenderer;

    [SerializeField, Tooltip("飛ばす拳のオブジェクト")]
    private Transform fist;

    [SerializeField, Tooltip("赤い予告マーカー")]
    private GameObject punchMarker;

    [Header("攻撃設定")]
    [SerializeField, Tooltip("パンチ（突進）の移動速度")]
    private float punchSpeed = 20.0f;

    [SerializeField, Tooltip("拳が戻る時の移動速度")]
    private float returnSpeed = 10.0f;

    [SerializeField, Tooltip("パンチで移動する最大距離")]
    private float punchRange = 15.0f;

    [SerializeField, Tooltip("再攻撃可能になるまでのクールタイム")]
    private float coolTime = 3.0f;

    [Header("予告")]
    [SerializeField, Tooltip("赤い予告を表示する時間")]
    private float warningTime = 0.7f;

    [SerializeField, Tooltip("予告マーカーの太さ")]
    private float markerThickness = 3.0f;

    // 攻撃終了時刻（クールタイム計算用）
    private float endTime = -10.0f;

    // パンチ開始時の位置と方向
    private Vector3 startPos;
    private Vector3 punchDir;

    // 拳の元の状態を記録する変数
    private Transform originalParent;
    private Vector3 localOffset;
    private Quaternion localRotationOffset;

    // ステート管理
    private bool isWarning;
    private bool isPunching;
    private bool isReturning;
    private float warningTimer;

    private BossController bossController;

    protected override void Awake()
    {
        base.Awake();
        bossController = GetComponentInParent<BossController>();

        if (punchMarker != null)
        {
            punchMarker.SetActive(false);
        }
    }

    public override bool CanExecute()
    {
        if (Time.time - endTime < coolTime)
            return false;

        if (IsVisible())
        {
            return true;
        }

        return false;
    }

    protected override void OnBegin()
    {
        isWarning = true;
        isPunching = false;
        isReturning = false;
        warningTimer = 0.0f;

        if (bossController != null)
        {
            bossController.SetIsMove(false);
        }

        startPos = fist.position;

        originalParent = fist.parent;
        localOffset = fist.localPosition;
        localRotationOffset = fist.localRotation;

        if (playerPos != null)
        {
            punchDir = (playerPos.position - startPos).normalized;

            if (punchMarker != null)
            {
                // マーカーの中心位置を、ボスの拳とパンチ到達地点の中間に設定
                Vector3 markerCenter = startPos + punchDir * (punchRange / 2.0f);
                punchMarker.transform.position = new Vector3(markerCenter.x, markerCenter.y, -1.0f);

                // パンチの方向に向けてマーカーを回転させる
                float angle = Mathf.Atan2(punchDir.y, punchDir.x) * Mathf.Rad2Deg;
                punchMarker.transform.rotation = Quaternion.Euler(0, 0, angle);

                // マーカーのスケールをパンチの距離に合わせる
                punchMarker.transform.localScale = new Vector3(punchRange, markerThickness, 1.0f);

                punchMarker.SetActive(true);
            }
        }
        else
        {
            punchDir = Vector3.left;
        }
    }

    private void FixedUpdate()
    {
        if (isWarning)
        {
            warningTimer += Time.fixedDeltaTime;
            if (warningTimer >= warningTime)
            {
                isWarning = false;
                isPunching = true;

                fist.SetParent(null, true);
            }
        }
        else if (isPunching)
        {
            fist.position += punchDir * punchSpeed * Time.fixedDeltaTime;

            if (Vector3.Distance(startPos, fist.position) >= punchRange)
            {
                isPunching = false;
                isReturning = true;

                if (punchMarker != null)
                {
                    punchMarker.SetActive(false);
                }
            }
        }
        else if (isReturning)
        {
            Vector3 targetPos = originalParent.TransformPoint(localOffset);
            fist.position = Vector3.MoveTowards(fist.position, targetPos, returnSpeed * Time.fixedDeltaTime);

            if (Vector3.Distance(fist.position, targetPos) <= 0.01f)
            {
                EndAttack();
            }
        }
    }

    protected override void OnEnd()
    {
        if (fist.parent != originalParent)
        {
            fist.SetParent(originalParent, true);
            fist.localPosition = localOffset;
            fist.localRotation = localRotationOffset;
        }

        if (punchMarker != null)
        {
            punchMarker.SetActive(false);
        }

        isWarning = false;
        isPunching = false;
        isReturning = false;
        endTime = Time.time;

        if (bossController != null)
        {
            bossController.SetIsMove(true);
        }
    }

    private bool IsVisible()
    {
        if (Camera.main == null || bossRenderer == null) return false;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        return GeometryUtility.TestPlanesAABB(planes, bossRenderer.bounds);
    }
}