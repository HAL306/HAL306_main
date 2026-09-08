
using System.Collections.Generic;
using UnityEngine;

public class PathMover : MonoBehaviour
{
    [Header("Path")]
    [SerializeField]
    private PathController path;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 5.0f;

    [SerializeField]
    private bool loop = false;

    private int currentPointIndex = 0;

    private void Update()
    {
        if (path == null)
            return;

        IReadOnlyList<Vector3> points = path.CurvePoints;

        if (points == null || points.Count < 2)
            return;

        Move(points);
    }

    private void Move(IReadOnlyList<Vector3> points)
    {
        if (currentPointIndex >= points.Count - 1)
        {
            if (loop)
            {
                currentPointIndex = 0;
            }
            else
            {
                return;
            }
        }

        Vector3 target = points[currentPointIndex + 1];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        // 次の点に到達したら次へ
        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            currentPointIndex++;
        }
    }

    public void StartMove()
    {
        currentPointIndex = 0;

        if (path != null && path.CurvePoints.Count > 0)
        {
            transform.position = path.CurvePoints[0];
        }
    }
}


