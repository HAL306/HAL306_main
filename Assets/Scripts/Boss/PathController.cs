
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathController : MonoBehaviour
{
    [Header("Control Points")]
    [SerializeField]
    private List<Transform> controlPoints = new();

    [Header("Curve Settings")]
    [SerializeField, Range(2, 50)]
    private int resolution = 20;

    [Header("Debug")]
    [SerializeField]
    private bool updateEveryFrame = true;

    private LineRenderer lineRenderer;

    // 実際の移動に使用する曲線上の点
    private List<Vector3> curvePoints = new();

    public IReadOnlyList<Vector3> CurvePoints => curvePoints;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        GenerateCurve();
    }

    private void Update()
    {
        // Scene上でControlPointを動かしたときに
        // リアルタイムで曲線を更新する
        if (updateEveryFrame)
        {
            GenerateCurve();
        }
    }

    /// <summary>
    /// ControlPointからCatmull-Rom曲線を生成する
    /// </summary>
    public void GenerateCurve()
    {
        curvePoints.Clear();

        if (controlPoints.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // ControlPointが2つの場合は直線
        if (controlPoints.Count == 2)
        {
            curvePoints.Add(controlPoints[0].position);
            curvePoints.Add(controlPoints[1].position);
        }
        else
        {
            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector3 p0 = GetPoint(i - 1);
                Vector3 p1 = GetPoint(i);
                Vector3 p2 = GetPoint(i + 1);
                Vector3 p3 = GetPoint(i + 2);

                for (int j = 0; j < resolution; j++)
                {
                    float t = j / (float)resolution;

                    Vector3 point = CatmullRom(
                        p0,
                        p1,
                        p2,
                        p3,
                        t
                    );

                    curvePoints.Add(point);
                }
            }

            // 最後のControlPointを追加
            curvePoints.Add(
                controlPoints[controlPoints.Count - 1].position
            );
        }

        // LineRendererへ反映
        lineRenderer.positionCount = curvePoints.Count;
        lineRenderer.SetPositions(curvePoints.ToArray());
    }

    /// <summary>
    /// 範囲外のControlPointを補間用に補正する
    /// </summary>
    private Vector3 GetPoint(int index)
    {
        // 最初より前 → 最初の点
        if (index < 0)
        {
            return controlPoints[0].position;
        }

        // 最後より後 → 最後の点
        if (index >= controlPoints.Count)
        {
            return controlPoints[controlPoints.Count - 1].position;
        }

        return controlPoints[index].position;
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
            2.0f * p1 +
            (-p0 + p2) * t +
            (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
            (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
        );
    }
}

