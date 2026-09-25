using UnityEngine;
using System.Collections.Generic;

public class GearRotate : MonoBehaviour
{
    [SerializeField, Tooltip("動かすギア")]
    private List<Transform> _gears;

    [SerializeField, Tooltip("回転速度")]
    private float _rotationSpeed = 100.0f;

    [SerializeField, Tooltip("一回の回転量")]
    private float _rotationAmount = 90.0f;

    private float _currentAngle = 0.0f;
    
    public void RotateGears()
    {
        _currentAngle = 0.0f;
    }

    private void Update()
    {
        if (_currentAngle < _rotationAmount)
        {
            float rotation = _rotationSpeed * Time.deltaTime;
            if (rotation > _rotationAmount - _currentAngle)
            {
                rotation = _rotationAmount - _currentAngle;
            }
            foreach (var gear in _gears)
            {
                gear.Rotate(0.0f, 0.0f, rotation);
            }
            _currentAngle += rotation;
        }
    }
}
