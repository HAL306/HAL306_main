using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの弾を発射するコンポーネント
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PlayerShooter : MonoBehaviour
{
    [SerializeField, Tooltip("射程距離")]
    private float _shootRange = 20.0f;

    [SerializeField, Tooltip("ショット待ち時間")]
    private float _shootInterval = 0.5f;

    [SerializeField, Tooltip("着弾時の爆発半径")]
    private float _explodeRadius = 0.2f;


    [SerializeField, Tooltip("攻撃が当たるレイヤー")]
    private LayerMask _hitLayer;


    private bool _inputShoot;           // ショット入力
    private Vector2 _inputAim;          // エイム方向入力 (スティック限定)
    private bool _isMouseAim;           // マウス操作によるエイムを行うフラグ

    private LineRenderer _lineRenderer;

    private Vector2 _shootAimTarget;    // 現在のショットターゲット座標
    private Vector2 _mouseWorldPos;     // マウスのワールド座標
    private float _cooldownTimer;       // ショット待ち時間計測用タイマー

    private float _lineTimer;
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


    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();   
    }

    private void Update()
    {
        _lineTimer -= Time.deltaTime;
        if (_lineTimer < 0.0f)
            _lineRenderer.positionCount = 0;

        MoveAimTarget();

        if (_cooldownTimer > 0.0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if(_inputShoot)
        {
            Shoot();
            _cooldownTimer = _shootInterval;
        }
    }

    // エイム入力モードを自動的に切り替える
    private void ChangeDeviceMode(InputAction.CallbackContext context)
    {
        if (context.control.device.layout == "Mouse")
        {
            _isMouseAim = true;
        }
        else
        {
            _isMouseAim = false;
        }
    }

    // ショットターゲットを移動させる
    private void MoveAimTarget()
    {
        // ターゲット座標取得
        if (_isMouseAim)
        {
            // マウス操作
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            _mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePosition);
            _shootAimTarget = _mouseWorldPos - (Vector2)transform.position;
        }
        else
        {
            // スティック操作
            _shootAimTarget = _inputAim.normalized;
        }

        // ゼロ対策
        if (_shootAimTarget == Vector2.zero)
        {
            _shootAimTarget = Vector2.right;
        }
    }

    private void Shoot()
    {
        RaycastHit2D hit;
        Vector2 origin = transform.position;
        Vector2 dir = _shootAimTarget.normalized;

        // ショットが命中したかを取得する
        hit = Physics2D.Raycast(origin, dir, _shootRange, _hitLayer);
        if(hit)
        {
            HitDestruct(hit.point, dir);

            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position + (Vector3)dir * _shootRange);
        }
        _lineTimer = 0.1f;
    }

    private void HitDestruct(Vector2 center, Vector2 dir)
    {
        // 爆発判定を行う
        Collider2D[] hitColliders;
        hitColliders = Physics2D.OverlapCircleAll(center, _explodeRadius, _hitLayer);

        foreach(Collider2D collider in hitColliders)
        {
            if(collider.TryGetComponent<TerrainContext>(out TerrainContext terrain))
            {
                CrackParameter crack;
                crack.direction = dir;
                crack.angleNoise = 120.0f;
                crack.minCrackCount = 1;
                crack.maxCrackCount = 2;
                terrain.Destruct(center, _explodeRadius, crack);
            }
        }
    }
}
