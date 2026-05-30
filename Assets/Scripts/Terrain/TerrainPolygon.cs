using Clipper2Lib;
using System.Collections.Generic;
using UnityEngine;


// エッジループ
public class EdgeLoop
{
    [Tooltip("頂点座標")]
    public Vector2[] points;

    [Tooltip("回転方向")]
    public bool isClockwise;
}


// ひび割れパラメータ
public struct CrackParameter
{
    [Tooltip("ひび割れの基準方向")]
    public Vector2 direction;

    [Tooltip("ひび割れ方向の角度ノイズ")]
    public float angleNoise;

    [Tooltip("最大ひび割れ本数")]
    public int maxCrackCount;

    [Tooltip("最小ひび割れ本数")]
    public int minCrackCount;
}


// 分離した地形のデータ
public struct SplitTerrainData
{
    [Tooltip("地形の形状パス")]
    public List<EdgeLoop> paths;

    [Tooltip("地形の面積")]
    public float area;
}


/// <summary>
/// 地形の形状データ
/// </summary>
public class TerrainPolygon
{
    // 交差判定結果
    struct IntercectResult
    {
        public bool isHit;                  // 交差判定
        public float distance;              // 交差位置までの距離
        public Vector2 point;               // 交差位置
    }


    private TerrainContext _terrainContext;
    private List<EdgeLoop> _terrainPaths;       // 地形形状
    private float _area;                        // 面積


    public List<EdgeLoop> TerrainPath => _terrainPaths;
    public float Area => _area;


    // 初期化処理
    public void Initialize(TerrainContext terrainContext, List<Vector2[]> terrainPaths)
    {
        _terrainContext = terrainContext;
        _terrainPaths = new List<EdgeLoop>(terrainPaths.Count);
        for (int i = 0; i < terrainPaths.Count; ++i)
        {
            EdgeLoop edgeLoop = new EdgeLoop();
            edgeLoop.points = terrainPaths[i];
            edgeLoop.isClockwise = IsClockwise(terrainPaths[i]);
            _terrainPaths.Add(edgeLoop);
        }
        _area = GetArea(_terrainPaths);
    }
    public void Initialize(TerrainContext terrainContext, SplitTerrainData splitTerrainData)
    {
        _terrainContext = terrainContext;
        _terrainPaths = splitTerrainData.paths;
        _area = splitTerrainData.area;
    }

    // ポリゴンの破壊処理
    public List<SplitTerrainData> PolygonDestruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        TerrainSettings settings = _terrainContext.TerrainSettings;
        TerrainParameter parameter = _terrainContext.TerrainParameter;
        Vector2 localCenter = _terrainContext.transform.InverseTransformPoint(worldCenter);

        // 計算用の地形パスを作成
        List<Vector2[]> terrainPaths = new List<Vector2[]>(_terrainPaths.Count);
        for (int i = 0; i < _terrainPaths.Count; ++i)
        {
            terrainPaths.Add(_terrainPaths[i].points);
        }

        // 破壊範囲の円を生成
        float circleRadius = radius * parameter.Destructibility;
        Vector2[] circlePath = CreateCirclePath(localCenter, circleRadius, settings.CircleVertex);

        // 円形に削る
        terrainPaths = PolygonDifference(terrainPaths, circlePath);

        // ひび割れ処理
        int crackCount = UnityEngine.Random.Range(crack.minCrackCount, crack.maxCrackCount + 1);
        for (int i = 0; i < crackCount; ++i)
        {
            terrainPaths = CrackDestruct(terrainPaths, localCenter, crack);
        }

        // 時計回りのエッジループ(穴のエッジループ)は削除する
        terrainPaths.RemoveAll(x => IsClockwise(x));

        // 地形パスを更新
        _terrainPaths.Clear();
        for (int i = 0; i < terrainPaths.Count; ++i)
        {
            EdgeLoop edgeLoop = new EdgeLoop();
            edgeLoop.points = terrainPaths[i];
            edgeLoop.isClockwise = false;

            _terrainPaths.Add(edgeLoop);
        }

        // パスがなくなったら終了
        List<SplitTerrainData> splitTerrains = new List<SplitTerrainData>();
        if(_terrainPaths.Count == 0)
        {
            _area = 0.0f;
            return splitTerrains;
        }

        // 地形の分離判定
        splitTerrains = SplitTerrainPath();

        return splitTerrains;
    }


    // ポリゴンの減算を行う
    private List<Vector2[]> PolygonDifference(List<Vector2[]> mainPaths, Vector2[] clipPath)
    {
        // Clipper2用の配列に変換
        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathD clipPathD = VectorPathToPathD(clipPath);

        // ポリゴン減算
        PathsD newPathsD = Clipper.Difference(mainPathsD, new PathsD() { clipPathD }, FillRule.NonZero);

        // Vector2配列に変換
        return PathsDToVectorPaths(newPathsD);
    }

    // Vector2のパス配列をClipper2用配列に変換する
    private PathD VectorPathToPathD(Vector2[] vectorPath)
    {
        // Clipper2用配列に変換
        PathD pathD = new PathD(vectorPath.Length);
        for (int i = 0; i < vectorPath.Length; i++)
        {
            Vector2 v = vectorPath[i];
            pathD.Add(new PointD((double)v.x, (double)v.y));
        }
        return pathD;
    }
    private PathsD VectorPathsToPathsD(List<Vector2[]> vectorPaths)
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
    private Vector2[] PathDToVectorPath(PathD pathD)
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
    private List<Vector2[]> PathsDToVectorPaths(PathsD pathsD)
    {
        List<Vector2[]> vectorPaths = new List<Vector2[]>(pathsD.Count);
        for (int i = 0; i < pathsD.Count; i++)
        {
            vectorPaths.Add(PathDToVectorPath(pathsD[i]));
        }
        return vectorPaths;
    }

    // 円形のパスを生成する
    private Vector2[] CreateCirclePath(Vector2 center, float radius, int vertexCount)
    {
        // 破壊範囲の円を生成
        Vector2[] circlePath = new Vector2[vertexCount];
        for (int i = 0; i < vertexCount; ++i)
        {
            float rad = (float)i / vertexCount;
            rad *= Mathf.PI * 2.0f;

            Vector2 pos;
            pos.x = Mathf.Cos(rad) * radius;
            pos.y = Mathf.Sin(rad) * radius;
            pos += center;

            circlePath[i] = pos;
        }

        return circlePath;
    }

    // ひび割れ破壊処理
    private List<Vector2[]> CrackDestruct(List<Vector2[]> mainPaths, Vector2 center, CrackParameter crack)
    {
        TerrainSettings settings = _terrainContext.TerrainSettings;
        TerrainParameter parameter = _terrainContext.TerrainParameter;

        // ひび割れ方向を求める
        Vector2 crackDir = _terrainContext.transform.InverseTransformDirection(crack.direction);
        float rotateAngle = UnityEngine.Random.Range(-crack.angleNoise, crack.angleNoise) * 0.5f;
        crackDir = Quaternion.Euler(0.0f, 0.0f, rotateAngle) * crackDir;

        // ひび割れ距離を求める
        float crackDistance = settings.CrackDistance * parameter.FractureMultiplier;

        // ひび割れとの最小交差距離を求める
        float minDistance = float.MaxValue;
        for (int i = 0; i < mainPaths.Count; ++i)
        {
            Vector2[] path = mainPaths[i];

            // 全ての辺に対してひび割れとの交差を求める
            for (int j = 0; j < path.Length; ++j)
            {
                Vector2 a = path[j];
                Vector2 b;
                if (j + 1 < path.Length)
                {
                    b = path[j + 1];
                }
                else
                {
                    b = path[j + 1 - path.Length];
                }

                // 辺とひび割れとの交差判定
                IntercectResult result;
                result = RaySegmentIntersection(center, crackDir, a, b);

                if (!result.isHit)
                    continue;

                // 最も近い交差距離を保持
                if (result.distance < minDistance)
                {
                    minDistance = result.distance;
                }
            }
        }

        // ひび割れ形状でポリゴンを削る
        if (minDistance < crackDistance)
        {
            Vector2[] crackPath = CreateCrackPath(center, crackDir, minDistance);
            mainPaths = PolygonDifference(mainPaths, crackPath);
        }

        return mainPaths;
    }

    // レイと線分の交差判定を行う
    private IntercectResult RaySegmentIntersection(
        Vector2 rayOrigin, Vector2 rayDir, Vector2 segA, Vector2 segB)
    {
        IntercectResult result = new IntercectResult();
        rayDir = rayDir.normalized;

        // ポリゴン内部に入る交差はスキップ
        Vector2 segVec = segB - segA;
        Vector2 normal = new Vector2(segVec.y, -segVec.x);
        float dot = Vector2.Dot(rayDir, normal);
        if (dot <= 0.0f)
        {
            result.isHit = false;
            return result;
        }

        // レイ方向と線分ベクトルの外積をとる
        float cross = Cross(rayDir, segVec);

        // 平行判定
        if (Mathf.Abs(cross) < Mathf.Epsilon)
        {
            result.isHit = false;
            return result;
        }

        // レイ始点から線分始点へのベクトルを求める
        Vector2 diff = segA - rayOrigin;

        // 線分を何倍すればRayと交差するか調べる
        float u = Cross(diff, rayDir) / cross;
        if (u < 0.0f || u > 1.0f)
        {
            result.isHit = false;
            return result;
        }

        // レイの始点からの交差位置までの距離を求める
        float t = Cross(diff, segVec) / cross;
        if (t < 0.0f)
        {
            result.isHit = false;
            return result;
        }

        result.isHit = true;
        result.distance = t;
        result.point = rayOrigin + rayDir * t;

        return result;
    }

    // 2D外積を求める
    private float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    // ひび割れ形状のパスを生成する
    private Vector2[] CreateCrackPath(Vector2 origin, Vector2 dir, float distance)
    {
        TerrainSettings settings = _terrainContext.TerrainSettings;

        dir = dir.normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        float halfWidth = settings.CrackWidth * 0.5f;
        float weight = settings.CrackWeight;

        int divisionCount = settings.CrackDivision;
        int start_a = 0;
        int end_a = divisionCount + 1;
        int start_b = end_a + 1;
        int end_b = start_b + divisionCount + 1;


        // 基準となる線を作成
        Vector2[] crackPath = new Vector2[end_b + 1];
        crackPath[start_a] = origin - dir * weight - normal * halfWidth;
        crackPath[end_a] = origin + dir * (distance + weight) - normal * halfWidth;
        crackPath[start_b] = origin + dir * (distance + weight) + normal * halfWidth;
        crackPath[end_b] = origin - dir * weight + normal * halfWidth;

        // 細分化しノイズでずらす
        for (int i = 0; i < divisionCount; ++i)
        {
            float maxNoise = distance * settings.CrackNoise * 0.5f;
            float noise = UnityEngine.Random.Range(-maxNoise, maxNoise);
            float ratio = (float)(i + 1) / (float)(divisionCount + 1);

            int index_a = start_a + i + 1;
            int index_b = end_b - i - 1;

            crackPath[index_a] = Vector2.Lerp(crackPath[start_a], crackPath[end_a], ratio);
            crackPath[index_b] = Vector2.Lerp(crackPath[end_b], crackPath[start_b], ratio);
            crackPath[index_a] += normal * noise;
            crackPath[index_b] += normal * noise;
        }

        return crackPath;
    }

    // エッジループの向きを調べる
    private bool IsClockwise(Vector2[] edgeLoop)
    {
        float area = 0.0f;

        // 符号付き面積を求める
        for (int i = 0; i < edgeLoop.Length; ++i)
        {
            Vector2 a = edgeLoop[i];
            Vector2 b = edgeLoop[(i + 1) % edgeLoop.Length];

            area += a.x * b.y - b.x * a.y;
        }

        // 符号付き面積が負の値なら時計回り
        return area < 0.0f;
    }

    // 地形分離判定
    private List<SplitTerrainData> SplitTerrainPath()
    {
        List<SplitTerrainData> result = new List<SplitTerrainData>();

        // 地形の分離を行う
        for (int i = 0; i < _terrainPaths.Count; ++i)
        {
            SplitTerrainData splitTerrain;
            splitTerrain.paths = new List<EdgeLoop>();
            splitTerrain.paths.Add(_terrainPaths[i]);
            splitTerrain.area = GetArea(splitTerrain.paths);

            result.Add(splitTerrain);
        }

        // 最大面積の分離地形を求める
        float maxArea = 0.0f;
        int maxAreaIndex = 0;
        for (int i = 0; i < result.Count; ++i)
        {
            if (result[i].area > maxArea)
            {
                maxArea = result[i].area;
                maxAreaIndex = i;
            }
        }

        // 最大面積の分離地形を元の地形とする
        _terrainPaths = result[maxAreaIndex].paths;
        _area = result[maxAreaIndex].area;
        result.RemoveAt(maxAreaIndex);

        return result;
    }

    // 面積を求める
    private float GetArea(List<EdgeLoop> edgeLoops)
    {
        PathsD pathsD = new PathsD(edgeLoops.Count);
        for (int i = 0; i < edgeLoops.Count; ++i)
        {
            pathsD.Add(VectorPathToPathD(edgeLoops[i].points));
        }
        return (float)Clipper.Area(pathsD);
    }
}
