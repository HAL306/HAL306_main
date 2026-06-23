using UnityEngine;

/// <summary>
/// 照準UIを制御するコンポーネント
/// コントローラー操作時はスティック方向×一定距離に照準を移動させる
/// マウス操作時はマウスのワールド座標に照準を移動させる
/// </summary>
public class ShootTargetUI : MonoBehaviour
{  
    [SerializeField, Tooltip("照準のサイズ")]
    [Range(0.1f, 5.0f)]
    private float _reticleSize = 1.0f;

    [SerializeField, Tooltip("プレイヤーの射撃コンポーネント")]
    private PlayerShooter _playerShooter;

    [SerializeField, Tooltip("コントローラー操作時の照準距離")]
    [Range(0.0f, 20.0f)]
    private float _controllerAimDist = 5.0f;

    private void Update()
    {
        UpdateReticlePosition();
    }
    private void Start()
    {
        transform.localScale = Vector3.one * _reticleSize;
    }

    // 照準の位置を更新する
    private void UpdateReticlePosition()
    {
        if (_playerShooter.IsMouseAim)
        {
            // マウス操作：マウスのワールド座標に照準を移動
            transform.position = _playerShooter.MouseWorldPos;
        }
        else
        {
            // コントローラー操作：プレイヤー座標からスティック方向×距離に照準を移動
            Vector2 playerPos = _playerShooter.transform.position;
            Vector2 aimDir = _playerShooter.ShootAimTarget.normalized;
            transform.position = playerPos + aimDir * _controllerAimDist;
        }
    }
}