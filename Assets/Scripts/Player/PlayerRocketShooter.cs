using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの弾を発射するコンポーネント
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PlayerRocketShooter : MonoBehaviour
{
    [Header("射撃設定")]
    [SerializeField, Tooltip("射程距離")]
    private float _shootRange = 20.0f;

    [SerializeField, Tooltip("ショット待ち時間")]
    private float _shootInterval = 0.5f;

    [SerializeField, Tooltip("着弾時の爆発半径")]
    private float _explodeRadius = 0.2f;

    [SerializeField, Tooltip("攻撃が当たるレイヤー（ベース地形・破壊可能地形の両方を含む）")]
    private LayerMask _hitLayer;

    [SerializeField, Tooltip("破壊可能地形のレイヤー")]
    private LayerMask _destructibleLayer;

    [SerializeField, Tooltip("発射する弾のプレハブ")]
    private GameObject _bulletPrefab;

    [Header("エイムライン設定")]
    [SerializeField, Tooltip("エイムラインを表示するか")]
    private bool _showAimLine = true;

    [SerializeField, Tooltip("エイムラインの表示時間")]
    private float _lineDisplayDuration = 0.1f;

    // 入力
    private bool _inputShoot;           // ショット入力
    private Vector2 _inputAim;          // エイム方向入力 (スティック限定)
    private bool _isMouseAim;           // マウス操作によるエイムを行うフラグ

    // 内部状態
    private Vector2 _shootAimTarget;    // 現在のショットターゲット座標
    private Vector2 _mouseWorldPos;     // マウスのワールド座標
    private float _cooldownTimer;       // ショット待ち時間計測用タイマー
    private float _lineTimer;

    private LineRenderer _lineRenderer;

    public Vector2 ShootAimTarget => _shootAimTarget;
    public Vector2 MouseWorldPos => _mouseWorldPos;
    public bool IsMouseAim => _isMouseAim;
    public float CooldownTimer => _cooldownTimer;
    public float ShootInterval => _shootInterval;

    // -- 入力イベント --

    public void OnShoot(InputAction.CallbackContext context)
    {
        ChangeDeviceMode(context);
        _inputShoot = context.performed;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        ChangeDeviceMode(context);
        _inputAim = context.ReadValue<Vector2>();
    }

    // -- Unity イベント --

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        // マウスが存在する場合はデフォルトでマウスモードにする
        if (Mouse.current != null)
            _isMouseAim = true;
    }

    private void Update()
    {
        UpdateAimLine();
        UpdateAimTarget();
        UpdateShoot();
    }

    // -- 更新処理 --

    // エイムラインの表示時間を管理する
    private void UpdateAimLine()
    {
        _lineTimer -= Time.deltaTime;
        if (_lineTimer < 0.0f)
            _lineRenderer.positionCount = 0;
    }

    // ショットターゲット座標を更新する
    private void UpdateAimTarget()
    {
        if (_isMouseAim)
        {
            // マウス操作：マウスのワールド座標からプレイヤーへの方向を取得
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            _mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreen);
            _shootAimTarget = _mouseWorldPos - (Vector2)transform.position;
        }
        else
        {
            // スティック操作：入力方向をそのまま使用
            _shootAimTarget = _inputAim.normalized;
        }

        // 入力がゼロの場合は右向きをデフォルトにする
        if (_shootAimTarget == Vector2.zero)
            _shootAimTarget = Vector2.right;
    }

    // 射撃クールダウンと発射処理を管理する
    private void UpdateShoot()
    {
        if (_cooldownTimer > 0.0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if (_inputShoot)
        {
            Shoot();
            _cooldownTimer = _shootInterval;
        }
    }

    // -- 射撃処理 --

    // 弾を生成してエイムラインを更新する
    private void Shoot()
    {
        Vector2 origin = transform.position;
        Vector2 dir = _shootAimTarget.normalized;

        // 弾オブジェクトを生成して初期化
        GameObject bulletObj = Instantiate(_bulletPrefab, origin, Quaternion.identity);
        bulletObj.GetComponent<PlayerRocket>().Init(
            dir, _explodeRadius, _hitLayer, _shootRange, _destructibleLayer);

        // ShotLineの終点を決める
        Vector2 end = origin + dir * _shootRange;

        // プレイヤーの位置からマウス方向に向かってRayを飛ばす
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, _shootRange, _hitLayer);

        //rayに何かが当たっていたら
        if (hit.collider)
        {
            // 終点をRayが当たった座標に上書きする
            end = hit.point;
        }

        // エイムライン描画
        if (_showAimLine)
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, origin + dir * _shootRange);
            _lineTimer = _lineDisplayDuration;
        }
    }

    // デバイスの種類に応じてエイムモードを切り替える
    private void ChangeDeviceMode(InputAction.CallbackContext context)
    {
        _isMouseAim = context.control.device.layout == "Mouse";
    }
}