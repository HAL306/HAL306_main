using LibTessDotNet;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

/// <summary>
/// 地形破壊時エフェクトを扱う
/// </summary>
[RequireComponent(typeof(TerrainContext))]
public class TerrainDestructEffect : MonoBehaviour
{
    private TerrainContext _terrainContext;


    private void Awake()
    {
        _terrainContext = GetComponent<TerrainContext>();
        _terrainContext.AddChangeTerrainEvent(OnChangeTerrain);
    }

    // 地形破壊時エフェクトを生成する
    public void EmitDestructEffect(List<EdgeLoop> destructPaths)
    {
        if (destructPaths == null || destructPaths.Count == 0)
            return;

        Tess tess = new Tess();

        for (int i = 0; i < destructPaths.Count; ++i)
        {
            Vector2[] effectEmitPath = destructPaths[i].points;

            // 回転方向を取得
            ContourOrientation orientation;
            if (destructPaths[i].isClockwise)
            {
                orientation = ContourOrientation.Clockwise;
            }
            else
            {
                orientation = ContourOrientation.CounterClockwise;
            }

            // エッジループを登録
            tess.AddContour(ToContour(effectEmitPath), orientation);
        }

        // エッジループを三角面化
        tess.Tessellate(WindingRule.EvenOdd, TessElementType.Polygons, 3);

        // 作成した三角面を取り出す
        Vector3[] vertices = new Vector3[tess.Vertices.Length];
        for (int i = 0; i < tess.VertexCount; i++)
        {
            vertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0.0f);
        }
        for (int i = 0; i < tess.ElementCount; i += 3)
        {
            int a = tess.Elements[i];
            int b = tess.Elements[i + 1];
            int c = tess.Elements[i + 2];

            // エフェクト生成位置を求める
            List<Vector2> EmitPoints = GetEmitPos(vertices[a], vertices[b], vertices[c], 30.0f);

            for (int j = 0; j < EmitPoints.Count; j++)
            {
                // エフェクトを生成する
                Vector3 effectPos = transform.TransformPoint((Vector3)EmitPoints[j]);
                ParticleSystem effectPrefab = _terrainContext.TerrainParameter.DestructEffect;
                Instantiate(effectPrefab, effectPos, Quaternion.identity);
            }
        }
    }

    // 地形破壊時処理
    private void OnChangeTerrain()
    {
        EmitDestructEffect(_terrainContext.TerrainPolygon.DestructPaths);
    }

    // LibTessDotNet用のエッジループに変換
    private ContourVertex[] ToContour(Vector2[] edgeLoop)
    {
        var result = new ContourVertex[edgeLoop.Length];
        for (int i = 0; i < edgeLoop.Length; i++)
        {
            result[i].Position = new Vec3(edgeLoop[i].x, edgeLoop[i].y, 0);
        }
        return result;
    }

    // 三角形の面積を求める
    private float GetTriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
    }

    // エフェクト発生位置を求める
    private List<Vector2> GetEmitPos(Vector2 a, Vector2 b, Vector2 c, float density)
    {
        List<Vector2> result = new List<Vector2>();
        float area = GetTriangleArea(a, b, c);
        float exactCount = area * density;
        int count = Mathf.FloorToInt(exactCount);

        // 端数を確率で繰り上げ
        if (Random.value < exactCount - count)
        {
            count++;
        }

        for (int i = 0; i < count; ++i)
        {
            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;

            // 三角形内のランダムな点を求める
            Vector2 p =
                (1 - r1) * a +
                (r1 * (1 - r2)) * b +
                (r1 * r2) * c;

            result.Add(p);
        }

        return result;
    }
}
