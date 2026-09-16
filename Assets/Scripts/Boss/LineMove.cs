
using System;
using UnityEngine;

public class LineMove : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float speed = 1f;

    [Header("Catmull-Rom")]
    [SerializeField] private bool useCatmullRom = true;

    [SerializeField, Range(2, 50)]
    private int resolution = 20;

    private int currentIndex;

    public EventHandler OnEndReached;

    private void Start()
    {
        // Catmull-Rom法
        if (useCatmullRom)
        {
            GenerateCatmullRom();
        }

        Initialize(0, speed, lineRenderer);
    }

    public void Initialize(
        int index,
        float speed,
        LineRenderer lineRenderer)
    {
        enabled = true;

        currentIndex = index;
        this.speed = speed;
        this.lineRenderer = lineRenderer;

        transform.position =
            lineRenderer.transform.TransformPoint(
                lineRenderer.GetPosition(currentIndex)
            );
    }


    private void GenerateCatmullRom()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRendererが設定されていない");
            return;
        }

        int originalCount = lineRenderer.positionCount;

       
        if (originalCount < 2)
        {
            Debug.LogError("頂点不足");
            return;
        }

        
        Vector3[] originalPoints =
            new Vector3[originalCount];

        lineRenderer.GetPositions(originalPoints);

        //曲線用1
        Vector3[] curvePoints =
            new Vector3[
                (originalCount - 1) * resolution + 1
            ];

        int pointIndex = 0;

        for (int i = 0; i < originalCount - 1; i++)
        {
            Vector3 p0 = GetControlPoint(
                originalPoints,
                i - 1
            );

            Vector3 p1 = GetControlPoint(
                originalPoints,
                i
            );

            Vector3 p2 = GetControlPoint(
                originalPoints,
                i + 1
            );

            Vector3 p3 = GetControlPoint(
                originalPoints,
                i + 2
            );

            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;

                curvePoints[pointIndex] =
                    CatmullRom(
                        p0,
                        p1,
                        p2,
                        p3,
                        t
                    );

                pointIndex++;
            }
        }

        
        curvePoints[pointIndex] =
            originalPoints[originalCount - 1];

        // 曲線点置き換え
        lineRenderer.positionCount =
            curvePoints.Length;

        lineRenderer.SetPositions(curvePoints);
    }


    private Vector3 GetControlPoint(
        Vector3[] points,
        int index)
    {
        
        if (index < 0)
        {
            return points[0];
        }

       
        if (index >= points.Length)
        {
            return points[points.Length - 1];
        }

        return points[index];
    }


    private Vector3 CatmullRom(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2.0f * p1
            + (-p0 + p2) * t
            + (2.0f * p0
               - 5.0f * p1
               + 4.0f * p2
               - p3) * t2
            + (-p0
               + 3.0f * p1
               - 3.0f * p2
               + p3) * t3
        );
    }


    public static (
        Vector3 targetPosition,
        bool isEnd
    ) GetTargetPosition(
        ref int index,
        float moveSpeed,
        Vector3 currentPosition,
        LineRenderer lineRenderer)
    {
        int nextIndex = index + 1;

        
        if (lineRenderer.positionCount <= nextIndex)
        {
            return (
                lineRenderer.transform.TransformPoint(
                    lineRenderer.GetPosition(index)
                ),
                true
            );
        }

        // 次の頂点
        Vector3 nextPosition =
            lineRenderer.transform.TransformPoint(
                lineRenderer.GetPosition(nextIndex)
            );

        //次の点までの距離
        float distance =
            Vector3.Distance(
                currentPosition,
                nextPosition
            );

        //移動停止処理
        if (distance < moveSpeed)
        {
            index += 1;

            // 余った移動距離を次の点へ引き継ぐ
            return GetTargetPosition(
                ref index,
                moveSpeed - distance,
                nextPosition,
                lineRenderer
            );
        }
        else
        {
            Vector3 direction =
                (nextPosition - currentPosition).normalized;

            return (
                currentPosition
                + direction * moveSpeed,
                false
            );
        }
    }

    private void Update()
    {
        var result = GetTargetPosition(
            ref currentIndex,
            speed * Time.deltaTime,
            transform.position,
            lineRenderer
        );

        transform.position =
            result.targetPosition;

        if (result.isEnd)
        {
            OnEndReached?.Invoke(
                this,
                EventArgs.Empty
            );

            enabled = false;
        }
    }
}

