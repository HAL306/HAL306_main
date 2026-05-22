using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField, Tooltip("追従対象オブジェクト")]
    private GameObject _followTarget;

    [SerializeField, Tooltip("追従オフセット座標")]
    private Vector2 _followOffset;


    private void LateUpdate()
    {
        Vector3 pos = _followTarget.transform.position + (Vector3)_followOffset;
        pos.z = transform.position.z;
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
