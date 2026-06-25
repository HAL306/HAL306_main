using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField, Tooltip("追従対象オブジェクト")]
    private GameObject _followTarget;

    [SerializeField, Tooltip("追従オフセット座標")]
    private Vector2 _followOffset;

    [SerializeField]
    private PlayerShooter _playerShooter;

    [SerializeField, Tooltip("エイム方向へのカメラ移動距離")]
    private Vector2 _aimOffsetDistance = new Vector2(2.0f, 1.0f);

    [SerializeField, Tooltip("エイム方向へのカメラ移動速度")]
    private float _aimOffsetSpeed = 2.0f;

    private Vector2 _aimOffset;

    private void LateUpdate()
    {
        Vector2 aimOffsetTarget = _playerShooter.ShootAimTarget * 0.5f;
        aimOffsetTarget.x = Mathf.Clamp(aimOffsetTarget.x, -_aimOffsetDistance.x, _aimOffsetDistance.x);
        aimOffsetTarget.y = Mathf.Clamp(aimOffsetTarget.y, -_aimOffsetDistance.y, _aimOffsetDistance.y);
        _aimOffset = Vector2.Lerp(_aimOffset, aimOffsetTarget, Time.deltaTime * _aimOffsetSpeed);

        Vector3 pos = _followTarget.transform.position + (Vector3)_followOffset;
        pos.z = transform.position.z;
        pos += (Vector3)_aimOffset;
        transform.position = pos;
    }

    private void OnValidate()
    {
        if( _followTarget == null )
            return;

        Vector3 pos = _followTarget.transform.position + (Vector3)_followOffset;
        pos.z = transform.position.z;
        transform.position = pos;
    }
}
