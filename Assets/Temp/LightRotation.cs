using UnityEngine;

public class LightRotation : MonoBehaviour
{
    [SerializeField]
    private float _rotateSpeed;

    void Update()
    {
        transform.Rotate(0.0f, 0.0f, _rotateSpeed * Time.deltaTime, Space.World);
    }
}
