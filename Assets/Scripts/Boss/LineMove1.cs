
using System;
using UnityEngine;

public class LineMove1 : MonoBehaviour
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
        // Catmull-RomでLineRendererの頂点を増やす
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

    /// <summary>
    /// LineRenderer上の点をCatmull-Rom曲線で補間する
    /// </summary>
    private void GenerateCatmullRom()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRendererが設定されていません。");
            return;
        }

        int originalCount = lineRenderer.positionCount;

        // 点が2個未満なら曲線を作れない
        if (originalCount < 2)
        {
            return;
        }

        // 元のLineRendererの座標を保存
        Vector3[] originalPoints =
            new Vector3[originalCount];

        lineRenderer.GetPositions(originalPoints);

        // 新しい曲線用の点を格納
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

        // 最後の点を追加
        curvePoints[pointIndex] =
            originalPoints[originalCount - 1];

        // LineRendererを曲線点に置き換える
        lineRenderer.positionCount =
            curvePoints.Length;

        lineRenderer.SetPositions(curvePoints);
    }

    /// <summary>
    /// Catmull-Rom用の制御点を取得
    /// </summary>
    private Vector3 GetControlPoint(
        Vector3[] points,
        int index)
    {
        // 最初より前なら最初の点を使用
        if (index < 0)
        {
            return points[0];
        }

        // 最後より後なら最後の点を使用
        if (index >= points.Length)
        {
            return points[points.Length - 1];
        }

        return points[index];
    }

    /// <summary>
    /// Catmull-Rom補間
    /// </summary>
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

    /// <summary>
    /// 現在位置から移動先を計算する
    /// </summary>
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

        // 最後まで到達
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

        // 現在位置から次の点までの距離
        float distance =
            Vector3.Distance(
                currentPosition,
                nextPosition
            );

        // 1フレームの移動距離より次の点までの距離が短い
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

