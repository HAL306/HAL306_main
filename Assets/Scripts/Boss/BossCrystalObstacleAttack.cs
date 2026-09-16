using UnityEngine;

public class BossCrystalObstacleAttack : BossAttackBase
{
    [Header("参照")]
    [Tooltip("RaycastObjectSpawner が付いているプレハブ")]
    [SerializeField] private GameObject spawnerPrefab;

    [Tooltip("プレイヤーのTransform")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("ステージのパスとなっている LineRenderer")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("発動条件（プレイヤーとの距離）")]
    [Tooltip("この距離よりプレイヤーが近すぎる場合は発動しない")]
    [SerializeField] private float minPlayerDistance = 3.0f;

    [Tooltip("この距離よりプレイヤーが遠すぎる場合は発動しない")]
    [SerializeField] private float maxPlayerDistance = 25.0f;

    [Header("生成パラメータ")]
    [Tooltip("プレイヤーからどれくらい前方にクリスタルを生やすか（メートル）")]
    [SerializeField] private float spawnForwardDistance = 5.0f;

    [Tooltip("RaycastObjectSpawner を生成する高さのオフセット（地面に向けてRayを飛ばせるよう少し上空に配置）")]
    [SerializeField] private float spawnerHeightOffset = 3.0f;

    [Header("攻撃タイマー設定")]
    [SerializeField] private float coolTime = 5.0f;
    [Tooltip("攻撃が続く時間")]
    [SerializeField] private float attackDuration = 3.0f;

    private float timer = 0.0f;
    private float lastAttackEndTime = -9999.0f;



    public override bool CanExecute()
    {
        // クールタイム中なら実行不可
        if (Time.time - lastAttackEndTime < coolTime)
        {
            return false;
        }

        // 必須参照が欠けている場合は実行しない
        if (spawnerPrefab == null || playerTransform == null || lineRenderer == null)
        {
            return false;
        }

        // プレイヤーとボス本体との距離判定
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer < minPlayerDistance || distanceToPlayer > maxPlayerDistance)
        {
            return false; // 最短〜最長の範囲外なら発動しない
        }

        return true;
    }

    protected override void OnBegin()
    {
        timer = 0.0f;

        // プレイヤーの進行方向前方、LineRendererに沿った位置を求める
        if (TryGetPathAheadPosition(playerTransform.position, spawnForwardDistance, out Vector3 targetPos))
        {
            // RaycastObjectSpawner は rayDirection（デフォルトは下向き Vector2.down）に向けてレイを飛ばすため、
            // 地面にしっかりレイが当たるよう、目標点より少し高い位置にスポナーを置く
            Vector3 spawnOrigin = targetPos + Vector3.up * spawnerHeightOffset;

            // スポナープレハブを生成
            GameObject spawnerObj = Instantiate(spawnerPrefab, spawnOrigin, Quaternion.identity);

            // プレハブ内の RaycastObjectSpawner を取得
            RaycastObjectSpawner spawner = spawnerObj.GetComponent<RaycastObjectSpawner>();
            if (spawner != null)
            {
                // 生成と同時にレイキャストして地面にクリスタルを生やす
                spawner.SpawnObject();
            }

            // スポナー自身が不要なら破棄
            Destroy(spawnerObj, 1.0f);
        }
        else
        {
            Debug.LogWarning("[BossAttack] プレイヤー前方のパス位置を取得できませんでした。");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 攻撃モーションや演出時間の経過で攻撃終了
        if (timer >= attackDuration)
        {
            EndAttack();
        }
    }

    protected override void OnEnd()
    {
        lastAttackEndTime = Time.time;
    }

    /// <summary>
    /// LineRenderer上の各セグメントからプレイヤーの最も近い点を見つけ、
    /// そこから forwardDist 分だけパスを進んだワールド座標を計算する
    /// </summary>
    private bool TryGetPathAheadPosition(Vector3 playerPos, float forwardDist, out Vector3 resultPos)
    {
        resultPos = Vector3.zero;
        int pointCount = lineRenderer.positionCount;
        if (pointCount < 2) return false;

        // LineRenderer 上でプレイヤーに最も近いセグメントとその投影点を探索
        float minDistanceSqr = float.MaxValue;
        int bestSegmentIndex = 0;
        Vector3 closestPointOnPath = playerPos;

        for (int i = 0; i < pointCount - 1; i++)
        {
            Vector3 p1 = lineRenderer.transform.TransformPoint(lineRenderer.GetPosition(i));
            Vector3 p2 = lineRenderer.transform.TransformPoint(lineRenderer.GetPosition(i + 1));

            Vector3 projected = GetClosestPointOnSegment(p1, p2, playerPos);
            float distSqr = (playerPos - projected).sqrMagnitude;

            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closestPointOnPath = projected;
                bestSegmentIndex = i;
            }
        }

        // 最も近い点から、パスに沿って forwardDist 分だけ進む
        float remainingDist = forwardDist;
        Vector3 currentPos = closestPointOnPath;

        for (int i = bestSegmentIndex; i < pointCount - 1; i++)
        {
            Vector3 pNext = lineRenderer.transform.TransformPoint(lineRenderer.GetPosition(i + 1));
            float distToNext = Vector3.Distance(currentPos, pNext);

            if (remainingDist <= distToNext)
            {
                Vector3 dir = (pNext - currentPos).normalized;
                resultPos = currentPos + dir * remainingDist;
                return true;
            }

            remainingDist -= distToNext;
            currentPos = pNext;
        }

        // パスの末端に達した場合は最後の頂点を返す
        resultPos = lineRenderer.transform.TransformPoint(lineRenderer.GetPosition(pointCount - 1));
        return true;
    }

    /// <summary>
    /// 線分(a-b)上で指定した点(p)に最も近い点を取得する
    /// </summary>
    private Vector3 GetClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float lengthSqr = ab.sqrMagnitude;
        if (lengthSqr < 0.0001f) return a;

        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / lengthSqr);
        return a + t * ab;
    }

    // シーンビュー上で距離の範囲を可視化（調整しやすいようにGizmoを追加）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minPlayerDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxPlayerDistance);
    }
}