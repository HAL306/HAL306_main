using UnityEngine;

public class BOSScharge : MonoBehaviour
{
    [Header("デバッグ")]

    // デバッグログを出すかどうか
    [SerializeField]
    private bool debugLog = true;

    // 表示時間ログを何秒ごとに出すか
    [SerializeField]
    private float visibleLogInterval = 1.0f;

    [Header("地形破壊")]

    // BOSSが地形を削る範囲
    [SerializeField]
    private float destructRadius = 0.5f;

    // 地形破壊時のひび割れ設定
    [SerializeField]
    private CrackParameter crackParameter;

    // 地形を破壊する間隔
    [SerializeField]
    private float destructInterval = 0.2f;


    [Header("参照")]

    // プレイヤーの位置を取得するためのTransform
    [SerializeField]
    private Transform player;

    // 突進する場所を知らせる赤い予告マーカー
    [SerializeField]
    private GameObject chargeMarker;

    [Header("距離による移動")]

    // プレイヤーとこの距離以上離れたら、高速ダッシュで近づく
    [SerializeField]
    private float maxDistance = 20.0f;

    // プレイヤーとこの距離以上離れたら、中速で移動する
    [SerializeField]
    private float middleDistance = 12.0f;

    // ダッシュ後、プレイヤーからどれくらい離れて止まるか
    [SerializeField]
    private float closeDistance = 10.0f;


    [Header("移動速度")]

    // プレイヤーと近い時の通常移動速度
    [SerializeField]
    private float speed = 3.0f;

    // プレイヤーと中距離の時の移動速度
    [SerializeField]
    private float middleSpeed = 8.0f;

    // プレイヤーと離れすぎた時のダッシュ速度
    [SerializeField]
    private float dashSpeed = 30.0f;


    [Header("突進")]

    // 画面に一定時間映った後に行う突進の速度
    [SerializeField]
    private float chargeSpeed = 50.0f;

    // Bossが画面に何秒映ったら突進準備に入るか
    [SerializeField]
    private float chargeWaitTime = 5.0f;

    // 突進前にその場で止まる時間
    [SerializeField]
    private float chargeStopTime = 0.5f;


    [Header("上下移動")]

    // プレイヤーのY座標を中心に、どれくらい上下するか
    [SerializeField]
    private float moveHeight = 2.0f;

    // 上下移動の速さ
    [SerializeField]
    private float moveSpeed = 2.0f;


    // BOSSを物理移動させるためのRigidbody2D
    private Rigidbody2D rb;

    // BOSSのX座標を管理する変数
    private float moveX;

    // 地形破壊の時間間隔を数えるタイマー
    private float destructTimer;

    // BOSSが画面内に映っている時間を数えるタイマー
    private float visibleTimer;

    // 突進前に止まる時間を数えるタイマー
    private float stopTimer;

    // 突進する目標地点のX座標
    private float chargeTargetX;

    // 距離が離れすぎた時のダッシュ中かどうか
    private bool isDashing;

    // 突進前の停止中かどうか
    private bool isChargePreparing;

    // 突進中かどうか
    private bool isCharging;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // ゲーム開始時のX座標を保存する
        moveX = transform.position.x;

        // 予告マーカー非表示
        if (chargeMarker != null)
        {
            chargeMarker.SetActive(false);
        }
    }


    private void FixedUpdate()
    {
        // BOSSが画面に映っている時間を確認する
        CheckVisibleCharge();

        // 突進前に止まっている状態
        if (isChargePreparing)
        {
            ChargePrepare();
            MoveVertical();
            return;
        }

        // 突進中の状態
        if (isCharging)
        {
            BossCharge();
            MoveVertical();
            return;
        }

        // BOSSとプレイヤーの横方向の距離を計算する
        float distanceX = Mathf.Abs(moveX - player.position.x);

        // 距離によって、通常移動・中速移動・ダッシュを切り替える
        ChangeSpeedByDistance(distanceX);

        // プレイヤーのY座標を中心に上下移動する
        MoveVertical();
    }


    // BOSSが画面に映っている時間を確認して、突進準備に入るか判断する
    private void CheckVisibleCharge()
    {
        // BOSSが画面内にいるなら時間を加算する
        if (IsVisible())
        {
            Log("画面内に映っている / visibleTimer: " + visibleTimer.ToString("F2") + "秒");
            visibleTimer += Time.fixedDeltaTime;
        }
        else
        {
            // 画面外に出たらカウントをリセットする
            Log("画面外に出た / visibleTimerをリセット");
            visibleTimer = 0.0f;
        }

        // 一定時間画面に映っている
        // かつ、他の特殊行動中ではない
        // この条件を満たしたら突進準備に入る
        if (visibleTimer >= chargeWaitTime &&
            !isChargePreparing &&
            !isCharging &&
            !isDashing)
        {
            // 突進準備状態にする
            isChargePreparing = true;

            // 停止時間のカウントをリセットする
            stopTimer = 0.0f;

            // 突進先を、この瞬間のプレイヤーX座標に固定する
            chargeTargetX = player.position.x;

            // 突進予告マーカーを表示する
            ShowChargeMarker();
            Debug.Log("突進");
        }
    }


    // 突進予告マーカーを表示する
    private void ShowChargeMarker()
    {
        // 突進する予定地点にマーカーを移動する
        chargeMarker.transform.position =
            new Vector3(chargeTargetX, player.position.y, -1.0f);

        // マーカーを表示する
        chargeMarker.SetActive(true);
    }


    // 突進予告マーカーを非表示にする
    private void HideChargeMarker()
    {
        if (chargeMarker != null)
        {
            chargeMarker.SetActive(false);
        }
    }


    // 突進前に少し止まる処理
    private void ChargePrepare()
    {
        Log("突進準備中 / 停止時間: " + stopTimer.ToString("F2") + "秒");
        // 停止時間を加算する
        stopTimer += Time.fixedDeltaTime;

        // 指定した時間止まったら突進開始
        if (stopTimer >= chargeStopTime)
        {
            // 停止状態を終了
            isChargePreparing = false;

            // 突進状態にする
            isCharging = true;

            // 画面内に映っている時間をリセット
            visibleTimer = 0.0f;
        }
    }


    // 画面に一定時間映った後の突進処理
    private void BossCharge()
    {
        Log("突進中");
        // 保存しておいた突進先へ高速で移動する
        moveX = Mathf.MoveTowards(
            moveX,
            chargeTargetX,
            chargeSpeed * Time.fixedDeltaTime
        );

        // 突進先に到達したら突進終了
        if (Mathf.Abs(moveX - chargeTargetX) <= 0.1f)
        {
            Log("突進終了");
            // 突進状態を解除する
            isCharging = false;

            // 突進予告マーカーを消す
            HideChargeMarker();
        }
    }


    // プレイヤーとの距離に応じてBOSSの移動速度を切り替える
    private void ChangeSpeedByDistance(float distanceX)
    {
        // 最長距離以上離れたらダッシュを開始する
        if (distanceX >= maxDistance)
        {
            Log("遠距離");
            isDashing = true;
        }

        // ダッシュ中なら、プレイヤー付近まで高速で近づく
        if (isDashing)
        {
            Log("ダッシュ中");
            LongDistanceDash();
        }
        else
        {
            // 中距離以上なら中速移動
            if (distanceX >= middleDistance)
            {
                Log("中距離");
                MiddleDistanceMove();
            }
            else
            {
                Log("近距離");
                // 近距離なら通常移動
                ShortDistanceMove();
            }
        }
    }


    // プレイヤーと離れすぎた時のダッシュ処理
    private void LongDistanceDash()
    {
        // プレイヤーより少し後ろの位置を目標にする
        float targetX = player.position.x - closeDistance;

        // 目標位置まで高速で近づく
        moveX = Mathf.MoveTowards(
            moveX,
            targetX,
            dashSpeed * Time.fixedDeltaTime
        );

        // 目標位置に近づいたらダッシュ終了
        if (Mathf.Abs(moveX - targetX) <= 0.1f)
        {
            isDashing = false;
        }
    }


    // プレイヤーと中距離の時の移動
    private void MiddleDistanceMove()
    {
        // 通常より速い速度で右に進む
        moveX += middleSpeed * Time.fixedDeltaTime;
    }


    // プレイヤーと近い時の通常移動
    private void ShortDistanceMove()
    {
        // 通常速度で右に進む
        moveX += speed * Time.fixedDeltaTime;
    }


    // プレイヤーのY座標を中心に上下移動する処理
    private void MoveVertical()
    {
        // プレイヤーのY座標を中心にする
        float y = player.position.y +
                  Mathf.Sin(Time.time * moveSpeed) * moveHeight;

        // moveXで横移動、yで上下移動した位置にBOSSを移動する
        rb.MovePosition(new Vector2(moveX, y));
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        // Fieldタグ以外に触れている場合は処理しない
        if (!collision.CompareTag("Field")) return;

        // 触れている地形からTerrainContextを取得する
        TerrainContext terrain = collision.GetComponentInParent<TerrainContext>();

        // TerrainContextが無ければ破壊できない
        if (terrain == null) return;

        // 地形破壊処理を行う
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


    // BOSSが画面に映っているか確認する処理
    private bool IsVisible()
    {
        // MainCameraが存在しない場合は画面内判定できない
        if (Camera.main == null) return false;

        // BOSSのワールド座標を、カメラの画面内座標に変換する
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);

        return viewPos.x >= 0 &&
               viewPos.x <= 1 &&
               viewPos.y >= 0 &&
               viewPos.y <= 1 &&
               viewPos.z > 0;
    }

    // デバッグログを出す
    private void Log(string message)
    {
        if (!debugLog) return;

        Debug.Log("[BOSSPunchAttack] " + gameObject.name + " : " + message, this);
    }

}