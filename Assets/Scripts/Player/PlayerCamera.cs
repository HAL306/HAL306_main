using UnityEngine;

/// <summary>
/// プレイヤーを追従するカメラコンポーネント
/// 照準方向に少しオフセットをかけて、狙っている方向をより広く見せる
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [SerializeField, Tooltip("追従するプレイヤーのTransform")]
    private Transform _playerTransform;

    [SerializeField, Tooltip("プレイヤーの射撃コンポーネント")]
    private PlayerShooter _playerShooter;

    [SerializeField, Tooltip("照準方向へのオフセット距離")]
    [Range(0.0f, 10.0f)]
    private float _aimOffset = 2.0f;

    [SerializeField, Tooltip("カメラ追従のスムーズ速度")]
    [Range(0.0f, 20.0f)]
    private float _smoothSpeed = 5.0f;

    private void LateUpdate()
    {
        FollowPlayer();
    }

    // プレイヤーを追従し、照準方向にオフセットをかける
    private void FollowPlayer()
    {
        Vector2 aimDir = _playerShooter.ShootAimTarget.normalized;

        // 目標座標 = プレイヤー座標 + 照準方向 × オフセット距離
        Vector3 targetPos = _playerTransform.position
                          + (Vector3)(aimDir * _aimOffset)
                          + new Vector3(0.0f, 0.0f, -10.0f);

        // スムーズに目標座標へ移動
        transform.position = Vector3.Lerp(
            transform.position, targetPos, Time.deltaTime * _smoothSpeed);
    }
}