using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveComponent : MonoBehaviour
{
    [SerializeField]
    private float speed;

    private Vector2 moveDirection;

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;
    }

    private void Update()
    {
        transform.position +=
            (Vector3)(moveDirection * speed * Time.deltaTime);
    }
}