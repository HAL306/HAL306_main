using System.Collections;
using UnityEngine;

public class BOSSPunchAttack : MonoBehaviour
{
    [Header("デバッグ")]

    // デバッグログを出すかどうか
    [SerializeField]
    private bool debugLog = true;

    // 表示時間ログを何秒ごとに出すか
    [SerializeField]
    private float visibleLogInterval = 1.0f;

    [Header("参照")]

    // プレイヤーの位置
    [SerializeField]
    private Transform player;

    // 拳を発射する位置
    // Bossの手の位置に空オブジェクトを置いて入れる
    [SerializeField]
    private Transform fistStartPoint;

    // 赤い予告マーカー
    [SerializeField]
    private GameObject punchMarker;

    // 元々の拳
    [SerializeField]
    private BossPunchFist fist;


    [Header("攻撃条件")]

    // BossとPlayerがこの距離以上離れていたらパンチ攻撃候補になる
    [SerializeField]
    private float attackDistance = 10.0f;

    // Bossが画面に何秒映っていなかったらパンチするか
    [SerializeField]
    private float attackWaitTime = 5.0f;

    // 次のパンチまでの待ち時間
    [SerializeField]
    private float attackCoolTime = 2.0f;


    [Header("予告")]

    // 赤い予告を表示する時間
    [SerializeField]
    private float warningTime = 0.7f;

    // 予告マーカーの大きさ
    [SerializeField]
    private float markerSize = 4.0f;


    [Header("拳の移動")]

    // 拳が着弾するまでの時間
    [SerializeField]
    private float fistMoveTime = 0.6f;

    // 拳が放物線に飛ぶ高さ
    [SerializeField]
    private float arcHeight = 4.0f;

    // 着弾位置をプレイヤーの周辺に少しずらす範囲
    [SerializeField]
    private float targetRandomX = 2.0f;

    // プレイヤーの少し奥に拳の着弾点を移動する
    [SerializeField]
    private float targetOffsetX = 2.0f;

    [Header("地形破壊")]

    // 拳が地形を破壊する範囲
    [SerializeField]
    private float destructRadius = 3.0f;

    // 地形破壊時のひび割れ設定
    [SerializeField]
    private CrackParameter crackParameter;


    // Bossが画面に映っている時間
    private float visibleTimer;

    // 表示時間ログ用タイマー
    private float visibleDebugTimer;

    // 攻撃中かどうか
    private bool isAttacking;

    // Bossが画面に映っていないか確認するためのRenderer
    private Renderer bossRenderer;

    // 実際に使う拳
    //private BossPunchFist fist;

    private void Start()
    {
        // Bossの見た目を取得
        bossRenderer = GetComponentInChildren<Renderer>();

        // 最初は予告マーカーを非表示にする
        if (punchMarker != null)
        {
            punchMarker.SetActive(false);
        }

        // もともとある拳を開始位置に合わせるだけ
        if (fist != null)
        {
            fist.transform.position = fistStartPoint.position;
        }
    }


    private void Update()
    {
        // 攻撃中は新しい攻撃を始めない
        if (isAttacking)
        {
            return;
        }

        // BossとPlayerの横方向の距離を調べる
        float distanceX = Mathf.Abs(transform.position.x - player.position.x);

        // Bossが画面に映っていないか確認
        bool visible = !IsVisible();

        // Bossが画面に映っていなくて、Playerとある程度離れている時だけカウントする
        if (visible && distanceX >= attackDistance)
        {
            visibleTimer += Time.deltaTime;
            visibleDebugTimer += Time.deltaTime;

            // ログが出すぎないように一定間隔で表示
            if (visibleDebugTimer >= visibleLogInterval)
            {
                Log("パンチ条件カウント中 / 表示時間: " + visibleTimer.ToString("F2") +
                    "秒 / 距離X: " + distanceX.ToString("F2"));

                visibleDebugTimer = 0.0f;
            }
        }
        else
        {
            if (visibleTimer > 0.0f)
            {
                Log("パンチ条件リセット / visible: " + visible +
                    " / 距離X: " + distanceX.ToString("F2"));
            }

            visibleTimer = 0.0f;
            visibleDebugTimer = 0.0f;
        }

        // 一定時間条件を満たしたらパンチ攻撃開始
        if (visibleTimer >= attackWaitTime)
        {
            Log("パンチ攻撃開始条件達成");

            StartCoroutine(LongPunch());
        }
    }


    private IEnumerator LongPunch()
    {
        // 攻撃中にする
        isAttacking = true;

        // 画面表示時間をリセット
        visibleTimer = 0.0f;
        visibleDebugTimer = 0.0f;

        Log("LongPunch 開始");

        // 着弾位置を決める
        Vector2 targetPosition = player.position;

        // プレイヤーの奥に着弾点を設置
        targetPosition.x += targetOffsetX;

        // 毎回同じ場所にならないように、少しランダムで左右にずらす
        float randomX = Random.Range(0.0f, targetRandomX);
        targetPosition.x += randomX;

        Log("着弾位置決定: " + targetPosition + " / ランダムX: " + randomX.ToString("F2"));

        // 予告マーカーを着弾地点に置く
        punchMarker.transform.position =
            new Vector3(targetPosition.x, targetPosition.y, -1.0f);

        // 予告マーカーを攻撃範囲に合わせた大きさにする
        punchMarker.transform.localScale =
            new Vector3(markerSize, markerSize, 1.0f);

        // 予告マーカーを表示する
        punchMarker.SetActive(true);

        Log("予告マーカー表示 / 位置: " + punchMarker.transform.position +
            " / Scale: " + punchMarker.transform.localScale);

        // 予告を少し見せる
        yield return new WaitForSeconds(warningTime);

        Log("拳生成成功 / 出発位置: " + fistStartPoint.position);

        Log("拳発射開始");

        if (fist == null)
        {
            Log("拳が入っていません");
            yield break;
        }

        fist.Initialize(
            fistStartPoint,
            targetPosition,
            fistMoveTime,
            arcHeight,
            destructRadius,
            crackParameter
        );

        Log("拳初期化完了 / 目標位置: " + targetPosition +
            " / 移動時間: " + fistMoveTime +
            " / 山なり高さ: " + arcHeight);

        // 拳が飛び終わるまで待つ
        yield return new WaitForSeconds(fistMoveTime);

        // 予告マーカーを消す
        punchMarker.SetActive(false);

        Log("予告マーカー非表示");

        // 次の攻撃まで待つ
        yield return new WaitForSeconds(attackCoolTime);

        // 攻撃終了
        isAttacking = false;

        Log("LongPunch 終了");
    }

    // Bossが画面に映っているか確認する
    private bool IsVisible()
    {
        if (Camera.main == null)
        {
            Log("Main Camera が見つからない");
            return false;
        }

        if (bossRenderer == null)
        {
            Log("bossRenderer がないため画面内判定できない");
            return false;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        bool visible = GeometryUtility.TestPlanesAABB(planes, bossRenderer.bounds);

        return visible;
    }


    // デバッグログを出す
    private void Log(string message)
    {
        if (!debugLog) return;

        Debug.Log("[BOSSPunchAttack] " + gameObject.name + " : " + message, this);
    }
}