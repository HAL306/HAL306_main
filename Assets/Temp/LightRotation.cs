using UnityEngine;

public class LightRotation : MonoBehaviour
{
    [SerializeField]
    private float _rotateSpeed;

    [SerializeField]
    private bool _isActive;

    // Update is called once per frame
    void Update()
    {
        if (_isActive)
            transform.Rotate(0.0f, 0.0f, _rotateSpeed * Time.deltaTime, Space.World);
    }
}
