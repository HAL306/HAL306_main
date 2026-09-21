using Clipper2Lib;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cipper2を使用するためのユーティリティクラス
/// </summary>
public static class CipperUtility
{
    // ポリゴンの交差を求める
    public static List<Vector2[]> PolygonIntersect(List<Vector2[]> mainPaths, Vector2[] intersectPath)
    {
        // Clipper2用の配列に変換
        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathD intersectPathD = VectorPathToPathD(intersectPath);

        // ポリゴン積
        PathsD newPathsD = Clipper.Intersect(mainPathsD, new PathsD { intersectPathD }, Clipper2Lib.FillRule.NonZero);

        // Vector2配列に変換
        return PathsDToVectorPaths(newPathsD);
    }

    // ポリゴンの減算を行う
    public static List<Vector2[]> PolygonDifference(List<Vector2[]> mainPaths, Vector2[] clipPath)
    {
        // Clipper2用の配列に変換
        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathD clipPathD = VectorPathToPathD(clipPath);

        // ポリゴン減算
        PathsD newPathsD = Clipper.Difference(mainPathsD, new PathsD() { clipPathD }, Clipper2Lib.FillRule.NonZero);

        // Vector2配列に変換
        return PathsDToVectorPaths(newPathsD);
    }

    // ポリゴンの減算を行う(一括処理対応)
    public static List<Vector2[]> PolygonDifference(List<Vector2[]> mainPaths, List<Vector2[]> clipPaths)
    {
        if (clipPaths == null || clipPaths.Count == 0) return mainPaths;

        // Clipper2用の配列に変換
        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathsD clipPathsD = VectorPathsToPathsD(clipPaths);

        // ポリゴン減算
        PathsD newPathsD = Clipper.Difference(mainPathsD, clipPathsD, Clipper2Lib.FillRule.NonZero);

        // Vector2配列に変換
        return PathsDToVectorPaths(newPathsD);
    }

    // Vector2のパス配列をClipper2用配列に変換する
    public static PathD VectorPathToPathD(IReadOnlyList<Vector2> vectorPath)
    {
        // Clipper2用配列に変換
        PathD pathD = new PathD(vectorPath.Count);
        for (int i = 0; i < vectorPath.Count; i++)
        {
            Vector2 v = vectorPath[i];
            pathD.Add(new PointD((double)v.x, (double)v.y));
        }
        return pathD;
    }
    public static PathsD VectorPathsToPathsD(IReadOnlyList<Vector2[]> vectorPaths)
    {
        // Clipper2用配列に変換
        PathsD pathsD = new PathsD(vectorPaths.Count);
        for (int i = 0; i < vectorPaths.Count; ++i)
        {
            pathsD.Add(VectorPathToPathD(vectorPaths[i]));
        }
        return pathsD;
    }

    // Clipper2用配列をVector2のパス配列に変換する
    public static Vector2[] PathDToVectorPath(PathD pathD)
    {
        // Vector2のパス配列に変換
        Vector2[] vectorPath = new Vector2[pathD.Count];
        for (int i = 0; i < pathD.Count; i++)
        {
            PointD p = pathD[i];
            vectorPath[i] = new Vector2((float)p.x, (float)p.y);
        }
        return vectorPath;
    }
    public static List<Vector2[]> PathsDToVectorPaths(PathsD pathsD)
    {
        List<Vector2[]> vectorPaths = new List<Vector2[]>(pathsD.Count);
        for (int i = 0; i < pathsD.Count; i++)
        {
            vectorPaths.Add(PathDToVectorPath(pathsD[i]));
        }
        return vectorPaths;
    }

    // エッジループの向きを調べる
    public static bool IsClockwise(IReadOnlyList<Vector2> edgeLoop)
    {
        float area = 0.0f;

        // 符号付き面積を求める
        for (int i = 0; i < edgeLoop.Count; ++i)
        {
            Vector2 a = edgeLoop[i];
            Vector2 b = edgeLoop[(i + 1) % edgeLoop.Count];

            area += a.x * b.y - b.x * a.y;
        }

        // 符号付き面積が負の値なら時計回り
        return area < 0.0f;
    }

    // 面積を求める
    public static float GetArea(IReadOnlyList<Vector2> terrainPath)
    {
        PathD pathD = VectorPathToPathD(terrainPath);
        return Mathf.Abs((float)Clipper.Area(pathD));
    }
    public static float GetArea(List<Vector2[]> terrainPaths)
    {
        PathsD pathsD = VectorPathsToPathsD(terrainPaths);
        return Mathf.Abs((float)Clipper.Area(pathsD));
    }
}
