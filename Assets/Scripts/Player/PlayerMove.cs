using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの移動を行うコンポーネント
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField, Tooltip("接地判定を取るレイヤー")]
    private LayerMask _groundLayer;

    [SerializeField, Tooltip("地上移動速度")]
    [Range(0.0f, 100.0f)]
    private float _groundSpeed = 5.0f;

    [SerializeField, Tooltip("空中移動速度")]
    [Range(0.0f, 100.0f)]
    private float _airSpeed = 3.0f;

    [SerializeField, Tooltip("ジャンプ速度")]
    [Range(0.0f, 100.0f)]
    private float _jumpPower = 10.0f;

    [SerializeField, Tooltip("重力加速度")]
    [Range(0.0f, 100.0f)]
    private float _gravity = 30.0f;


    [Header("詳細設定")]
    [SerializeField, Tooltip("地上移動加速度")]
    [Range(0.0f, 100.0f)]
    public float _groundAcceleration = 50.0f;

    [SerializeField, Tooltip("空中移動加速度")]
    [Range(0.0f, 100.0f)]
    public float _airAcceleration = 30.0f;

    [SerializeField, Tooltip("落下初速")]
    [Range(0.0f, 100.0f)]
    public float _fallStartSpeed = 2.0f;

    [SerializeField, Tooltip("最大落下速度")]
    [Range(0.0f, 100.0f)]
    public float _maxFallSpeed = 20.0f;

    [SerializeField, Tooltip("コヨーテタイム (地面から離れてからジャンプできる猶予時間)")]
    [Range(0.0f, 1.0f)]
    public float _coyoteTime = 0.1f;

    [SerializeField, Tooltip("先行入力の猶予時間")]
    [Range(0.0f, 1.0f)]
    private float _inputBufferDuration = 0.1f;

    [SerializeField, Tooltip("坂道とみなす最大角度")]
    [Range(0.0f, 90.0f)]
    private float _maxSlopeAngle = 50.0f;

    [SerializeField, Tooltip("坂道とみなす最小角度\nMaxSlopeAngleより小さい値を推奨します")]
    [Range(0.0f, 90.0f)]
    private float _minSlopeAngle = 10.0f;

    [Header("InputAction登録")]
    [SerializeField, Tooltip("移動")]
    private InputActionReference _moveAction;
    [SerializeField, Tooltip("ジャンプ")]
    private InputActionReference _jumpAction;

    [SerializeField, Tooltip("自重でクリスタル破壊可能 == true")]
    //True : 破壊可能
    public bool _isDestroyCrystalByWeight = false;

    [SerializeField, Tooltip("ベース地形の乗り越え")]
    //True : 乗り越え可能
    public bool _isOvercomeBaseTerrain = false;

    [SerializeField, Tooltip("壁を駆け上がる速度")]
    private float _climbUpSpeed = 7.0f;


    [Header("参照登録")]
    [SerializeField]
    private CutsceneState _cutsceneState;
    [SerializeField]
    private PlayerMoveOverrideState _playerMoveOverrideState;
    [Header("フィーバー中の設定")]
    [SerializeField, Tooltip("地上移動速度倍率")]
    [Range(0.0f, 5.0f)]
    public float _groundSpeedRatio = 1.2f;

    [SerializeField, Tooltip("空中移動速度倍率")]
    [Range(0.0f, 5.0f)]
    public float _airSpeedRatio = 1.2f;

    [SerializeField, Tooltip("空中移動加速度倍率")]
    [Range(0.0f, 5.0f)]
    public float _airAccelerationRatio = 1.2f;



 
    private Rigidbody2D _rigidbody;

    Animator animator;                          // アニメーター

    private Vector2 _inputMove;                 // 移動入力
    private bool _inputJump;                    // ジャンプ入力

    private Vector2 _currentVelicity;           // 現在の移動速度
    private Vector2 _groundNormal;              // 地面の法線方向

    private bool _isGround;                     // 接地判定フラグ
    private bool _wasGround;                    // 前フレームの接地判定フラグ
    private bool _isJumping;                    // ジャンプ中フラグ
    private bool _isLandingSlope;               // 坂に着地した瞬間のフラグ

    private float _coyoteTimer;                 // コヨーテタイム計測用タイマー
    private float _jumpBufferTimer;             // ジャンプ先行入力計測用タイマー

    private float _rotateLockTimer;             // 向き固定時間計測用タイマー
    private bool isFever = false;  

    private bool _wasOverrideMoveTargetReachedPrevFrame;    // オーバーライドされた移動の目標地点に前のフレームで到達していたか

    private int _contactWallDir = 0; // 1:右に壁がある, -1:左に壁がある, 0:壁なし

    private void OverrideInput()
    {
        if (_playerMoveOverrideState is null)
        {
            return;
        }

        switch (_playerMoveOverrideState.MoveOverridingState)
        {
            case PlayerMoveOverrideState.OverrideState.Speed:
            {
                float distanceX = _playerMoveOverrideState.OverrideMoveTargetX - transform.position.x;

                float threshold = Mathf.Max(0.05f, Mathf.Abs(_currentVelicity.x) * Time.fixedDeltaTime * 1.5f);

                if (Mathf.Abs(distanceX) < threshold)
                {
                    _inputMove = Vector2.zero;
                    _currentVelicity.x = 0f;
                    _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
                    
                    if (_wasOverrideMoveTargetReachedPrevFrame)
                    {
                        _playerMoveOverrideState.FinishOverride();
                        _wasOverrideMoveTargetReachedPrevFrame = false;
                    }
                    else
                    {
                        _wasOverrideMoveTargetReachedPrevFrame = true;
                    }
                    
                    // transform.position = new Vector3(_playerMoveOverrideState.OverrideMoveTargetX, transform.position.y, transform.position.z);
                    return;
                }

                _wasOverrideMoveTargetReachedPrevFrame = false;

                float dirX = Mathf.Sign(distanceX);
                _inputMove = new Vector2(dirX * _playerMoveOverrideState.OverrideMoveSpeed, 0f);
                break;
            }
            case PlayerMoveOverrideState.OverrideState.Time:
            {
                _playerMoveOverrideState.OverrideMoveTime = Mathf.Max(_playerMoveOverrideState.OverrideMoveTime - Time.fixedDeltaTime, 0.0f);
                
                float distanceX = _playerMoveOverrideState.OverrideMoveTargetX - transform.position.x;

                if (_playerMoveOverrideState.OverrideMoveTime <= 0f)
                {
                    _inputMove = Vector2.zero;
                    _currentVelicity.x = 0f;
                    _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
                    
                    _playerMoveOverrideState.FinishOverride();
                    return;
                }

                float remainingTime = Mathf.Max(_playerMoveOverrideState.OverrideMoveTime, Time.fixedDeltaTime);
                
                float requiredVelocity = distanceX / remainingTime;
                
                float speedInput = requiredVelocity / Mathf.Max(_groundSpeed, 0.1f);

                _inputMove = new Vector2(Mathf.Clamp(speedInput, -2.0f, 2.0f), 0f);
                break;
            }
            case PlayerMoveOverrideState.OverrideState.None:
            {
                if (_cutsceneState.IsPlaying)
                {
                    _inputMove = Vector2.zero;
                    _inputJump = false;
                }
                break;
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    { 
        _inputMove = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!_cutsceneState.IsPlaying)
        {
            _inputJump = context.performed;
            _jumpBufferTimer = _inputBufferDuration;
        }
    }

    // 向きを変更し一定時間固定する
    public void RotateLock(bool isRight, float duration)
    {
        _rotateLockTimer = duration;

        // 向き変更
        if (isRight)
        {
            transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
        }
        else
        {
            transform.eulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
        animator = GetComponent<Animator>();    // アニメーターの取得
    }

    private void OnEnable()
    {
        if (_moveAction != null)
        {
            _moveAction.action.started += OnMove;
            _moveAction.action.performed += OnMove;
            _moveAction.action.canceled += OnMove;
        }
        if (_jumpAction != null)
        {
            _jumpAction.action.performed += OnJump;
            _jumpAction.action.canceled += OnJump;
        }
    }
    
    private void OnDisable()
    {
        if (_moveAction != null)
        {
            _moveAction.action.started -= OnMove;
            _moveAction.action.performed -= OnMove;
            _moveAction.action.canceled -= OnMove;
        }
        if (_jumpAction != null)
        {
            _jumpAction.action.performed -= OnJump;
            _jumpAction.action.canceled -= OnJump;
        }
    }

    private void FixedUpdate()
    {
        // 現在の速度を取得
        _currentVelicity = _rigidbody.linearVelocity;

        // 移動処理
        OverrideInput();
        CheckBaseTerrainWall();// ベース地形の壁に接触しているか判定

        if (!OvercomeWall()) // 壁を乗り越える処理が行われた場合は、Move, Jump, AddGravityの処理をスキップ
        {
            Move();
            Jump();
            AddGravity();
        }
        _rigidbody.linearVelocity = _currentVelicity;

        // 各種タイマー・フラグ更新
        UpdateAnimator();       // アニメーターの更新
        UpdateTimer(Time.fixedDeltaTime);
        UpdateFlags();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 接地判定
        CheckGround(collision);
    }


    // 移動処理
    private void Move()
    {
        if (_isGround && !_isJumping)
        {
            // 地上移動
            Vector2 rightMoveDir;       // 右入力時の移動方向
            float tangentVelocity;      // 地面に沿った移動速度
            float targetVelocity;       // 目標速度

            // 地面の向きから移動方向を求める
            rightMoveDir = new Vector2(_groundNormal.y, -_groundNormal.x);
            rightMoveDir = rightMoveDir.normalized;

            // 現在の地面に沿った移動速度を求める
            tangentVelocity = Vector2.Dot(rightMoveDir, _currentVelicity);

            // 目標速度を求める
            if(isFever)
            {
                targetVelocity = _inputMove.x * _groundSpeed * _groundSpeedRatio;
            }
            else
            {
                targetVelocity = _inputMove.x * _groundSpeed;
            }

            if(_isLandingSlope)
            {
                // 坂道に着地時にRigidbodyの速度を一瞬リセットしているので即座に目標速度に変化させる
                tangentVelocity = targetVelocity;
            }
            else
            {
                // 移動速度を目標速度に近づける
                tangentVelocity = Mathf.MoveTowards(tangentVelocity, targetVelocity, _groundAcceleration * Time.fixedDeltaTime);
            }
            _currentVelicity = rightMoveDir * tangentVelocity;
        }
        else
        {
            // 空中移動
            float targetVelocity;       // 目標速度

            // 目標速度を求める
            if (isFever)
            {
                targetVelocity = _inputMove.x * _airSpeed * _airSpeedRatio;
                // 移動速度を目標速度に近づける
                _currentVelicity.x = Mathf.MoveTowards(_currentVelicity.x, targetVelocity, _airAcceleration * _airAccelerationRatio * Time.fixedDeltaTime);
            }
            else
            {
                targetVelocity = _inputMove.x * _airSpeed;
                // 移動速度を目標速度に近づける
                _currentVelicity.x = Mathf.MoveTowards(_currentVelicity.x, targetVelocity, _airAcceleration * Time.fixedDeltaTime);
            }
        }

        // 向き変更
        if (_rotateLockTimer <= 0.0f)
        {
            if (_inputMove.x > 0.0f)
            {
                transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
            }
            else if (_inputMove.x < 0.0f)
            {
                transform.eulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
            }
        }
    }

    // ジャンプ処理
    private void Jump()
    {
        // ジャンプ可能か判定
        if (_isJumping || _coyoteTimer <= 0.0f)
            return;

        // ジャンプ処理
        if (_inputJump && _jumpBufferTimer > 0.0f)
        {
            _currentVelicity.y = _jumpPower;
            _isJumping = true;
            _inputJump = false;
        }
    }

    // 重力処理
    private void AddGravity()
    {
        // 重力
        if (_isGround)
        {
            // Collisionイベントを発生させるために微小な重力を与える
            _currentVelicity.y -= 0.001f;
        }
        else
        {
            if (_wasGround && !_isJumping)
            {
                // ジャンプ以外で地面から離れた瞬間に落下初速を与える
                _currentVelicity.y = -_fallStartSpeed;
            }
            else
            {
                _currentVelicity.y -= _gravity * Time.fixedDeltaTime;
            }
        }

        // 落下速度上限
        if (_currentVelicity.y < -_maxFallSpeed)
        {
            _currentVelicity.y = -_maxFallSpeed;
        }
    }

    // 各種タイマー更新
    private void UpdateTimer(float delta)
    {
        //コヨーテタイムの処理
        if (_isJumping)
        {
            _coyoteTimer = 0.0f;
        }
        else
        {
            if (_isGround)
            {
                _coyoteTimer = _coyoteTime;
            }
            else
            {
                _coyoteTimer = Mathf.Max(0.0f, _coyoteTimer - delta);
            }
        }

        //ジャンプバッファの処理
        if (_jumpBufferTimer > 0.0f)
        {
            _jumpBufferTimer = Mathf.Max(0.0f, _jumpBufferTimer - delta);
        }

        // 向き固定タイマーの処理
        if(_rotateLockTimer > 0.0f)
        {
            _rotateLockTimer = Mathf.Max(0.0f, _rotateLockTimer - delta);
        }
    }

    // 各種フラグ更新
    private void UpdateFlags()
    {
        // ジャンプ中フラグ更新
        if (_currentVelicity.y <= 0.0f && _isGround)
        {
            _isJumping = false;
        }

        // 接地状態更新
        _wasGround = _isGround;
        _isGround = false;
        _isLandingSlope = false;
    }

    // 接地判定
    private void CheckGround(Collision2D collision)
    {
        // 地面レイヤー判定
        if ((_groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        float minGroundAngle = 90.0f;
        foreach (var contact in collision.contacts)
        {
            // 地面として扱う角度か判定
            float angle = Vector2.Angle(contact.normal, Vector2.up);

            // 最大角度より大きい場合は地面として扱わない
            if (angle > _maxSlopeAngle)
                continue;
      
            _isGround = true;

            // 最も水平に近い地面の角度と法線方向を記録
            if (angle < minGroundAngle)
            {
                minGroundAngle = angle;
                _groundNormal = contact.normal;
            }

        }

        //坂道に着地した瞬間は一瞬速度をゼロにする(滑り落ち対策)
        if (_isGround && !_wasGround &&
            minGroundAngle > _minSlopeAngle)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _isLandingSlope = true;
        }
    }

    //baseTerrainの壁に接触しているか判定
    private void CheckBaseTerrainWall()
    {
        if (!_isOvercomeBaseTerrain)
        {
            _contactWallDir = 0;
            return;
        }

        float dir = Mathf.Sign(_inputMove.x);
        if (Mathf.Abs(_inputMove.x) < 0.1f)
        {
            _contactWallDir = 0;
            return;
        }
        // コライダーの情報を取得
        Collider2D col = GetComponent<Collider2D>();

        Bounds bounds = col.bounds; // ワールド座標での実際の範囲を取得
        float rayLength = 1.0f;
        int terrainLayerMask = 1 << LayerMask.NameToLayer("BaseTerrain");

        // コライダーの下端 + 0.1f を始点にする
        Vector2 pos = transform.position;
        Vector2 origin = new Vector2(pos.x, bounds.min.y + 0.1f); // 足元少し上
        bool isHit = false;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dir, rayLength, terrainLayerMask);
        if (hit.collider != null)
        {
            isHit = true;
        }

        _contactWallDir = isHit ? (int)dir : 0;
    }

    // 壁を乗り越える処理
    private bool OvercomeWall()
    {
        if (!_isOvercomeBaseTerrain || _contactWallDir == 0) return false;

        // 右入力があって右に壁がある、または左入力があって左に壁がある場合
        bool isPushingWall = (_inputMove.x > 0.1f && _contactWallDir == 1) ||
                             (_inputMove.x < -0.1f && _contactWallDir == -1);

        if (isPushingWall)
        {
            // 壁登り中は速度を強制固定（Moveや重力に邪魔されないようにする）
            _currentVelicity.y = _climbUpSpeed;
            _currentVelicity.x = _inputMove.x * _groundSpeed;
            return true;
        }

        return false;
    }
    public void SetFever(bool fever)
    {
        isFever = fever;
    }


    // アニメーターの更新
    private void UpdateAnimator()
    {
        if (_currentVelicity.y <= -2.1f)
        {
            animator.SetBool("IsGround", false);

        }
        else
        {
            animator.SetBool("IsGround", true);
        }

        animator.SetBool("IsJump", _isJumping);

        if (_inputMove.x >= 0.5f)
        {
            animator.SetBool("IsSprint", true);
            animator.SetBool("IsIdle", false);
        }
        else if (_inputMove != Vector2.zero)
        {
            animator.SetBool("IsSprint", false);
            animator.SetBool("IsMove", true);
            animator.SetBool("IsIdle", false);
        }
        else
        {
            animator.SetBool("IsSprint", false);
            animator.SetBool("IsMove", false);
            animator.SetBool("IsIdle", true);
        }

    }
}
