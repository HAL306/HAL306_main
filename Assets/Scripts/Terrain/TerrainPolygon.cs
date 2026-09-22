using Clipper2Lib;
using System;
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
[System.Serializable]
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

// ひび割れさせるときに渡す構造体
public struct CrackData
{
    [Tooltip("始点")]
    public Vector2 pos;

    [Tooltip("向き")]
    public Vector2 dir;

    [Tooltip("長さ")]
    public float length;
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
/// 地形の形状データおよびメッシュ構築クラス
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
    private List<EdgeLoop> _destructPaths;      // 直前の破壊の形状
    private float _area;                        // 面積

    public List<EdgeLoop> TerrainPaths => _terrainPaths;
    public List<EdgeLoop> DestructPaths => _destructPaths;
    public float Area => _area;

    // 初期化処理
    public void Initialize(TerrainContext terrainContext, List<Vector2[]> terrainPaths)
    {
        _terrainContext = terrainContext;
        _terrainPaths = new List<EdgeLoop>(terrainPaths.Count);
        _destructPaths = new List<EdgeLoop>();
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
        _destructPaths = new List<EdgeLoop>();
        _area = splitTerrainData.area;
    }

    /// <summary>
    /// 現在のパスからメッシュを生成し、MeshFilterに適用する
    /// </summary>
    public void GenerateMesh(MeshFilter meshFilter)
    {
        if (meshFilter == null || _terrainPaths == null || _terrainPaths.Count == 0)
        {
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = null;
            }
            return;
        }

        // LibTessDotNet を使用してポリゴンを三角形分割
        LibTessDotNet.Tess tess = new LibTessDotNet.Tess();

        for (int i = 0; i < _terrainPaths.Count; i++)
        {
            Vector2[] pts = _terrainPaths[i].points;
            if (pts == null || pts.Length < 3) continue;

            LibTessDotNet.ContourVertex[] contour = new LibTessDotNet.ContourVertex[pts.Length];
            for (int j = 0; j < pts.Length; j++)
            {
                contour[j].Position = new LibTessDotNet.Vec3(pts[j].x, pts[j].y, 0);
            }
            tess.AddContour(contour, LibTessDotNet.ContourOrientation.Clockwise);
        }

        // ElementType を指定しないオーバーロードを使用 (デフォルトで三角形ポリゴン分割を実行)
        tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd);

        int numTriangles = tess.ElementCount;
        if (numTriangles == 0)
        {
            meshFilter.sharedMesh = null;
            return;
        }

        Vector3[] vertices = new Vector3[tess.VertexCount];
        Vector2[] uvs = new Vector2[tess.VertexCount];
        int[] triangles = new int[numTriangles * 3];

        Vector2 minUV = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxUV = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < tess.VertexCount; i++)
        {
            float vx = tess.Vertices[i].Position.X;
            float vy = tess.Vertices[i].Position.Y;
            vertices[i] = new Vector3(vx, vy, 0);

            if (vx < minUV.x) minUV.x = vx;
            if (vx > maxUV.x) maxUV.x = vx;
            if (vy < minUV.y) minUV.y = vy;
            if (vy > maxUV.y) maxUV.y = vy;
        }

        float width = Mathf.Max(0.0001f, maxUV.x - minUV.x);
        float height = Mathf.Max(0.0001f, maxUV.y - minUV.y);

        for (int i = 0; i < tess.VertexCount; i++)
        {
            uvs[i] = new Vector2((vertices[i].x - minUV.x) / width, (vertices[i].y - minUV.y) / height);
        }

        for (int i = 0; i < numTriangles; i++)
        {
            triangles[i * 3 + 0] = tess.Elements[i * 3 + 0];
            triangles[i * 3 + 1] = tess.Elements[i * 3 + 1];
            triangles[i * 3 + 2] = tess.Elements[i * 3 + 2];
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "TerrainMesh";
            meshFilter.sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
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

        // 削る形状を取得
        List<Vector2[]> destructPaths = PolygonIntersect(terrainPaths, circlePath);

        // 円形に削る
        terrainPaths = PolygonDifference(terrainPaths, circlePath);

        // ひび割れ処理
        int crackCount = UnityEngine.Random.Range(crack.minCrackCount, crack.maxCrackCount + 1);
        List<Vector2[]> allCrackPaths = new List<Vector2[]>(crackCount);
        for (int i = 0; i < crackCount; ++i)
        {
            Vector2[] crackPath = GenerateCrackPath(terrainPaths, localCenter, crack);
            if (crackPath != null)
            {
                allCrackPaths.Add(crackPath);
            }
        }
        
        if (allCrackPaths.Count > 0)
        {
            terrainPaths = PolygonDifference(terrainPaths, allCrackPaths);
        }

        // 時計回りのエッジループ(穴のエッジループ)は削除する
        for (int i = terrainPaths.Count - 1; i >= 0; --i)
        {
            if (IsClockwise(terrainPaths[i]))
            {
                terrainPaths.RemoveAt(i);
            }
        }

        // 地形パスを更新
        _terrainPaths.Clear();
        for (int i = 0; i < terrainPaths.Count; ++i)
        {
            EdgeLoop edgeLoop = new EdgeLoop();
            edgeLoop.points = terrainPaths[i];
            edgeLoop.isClockwise = false;

            _terrainPaths.Add(edgeLoop);
        }

        // 破壊形状パスを更新
        _destructPaths.Clear();
        for (int i = 0; i < destructPaths.Count; ++i)
        {
            EdgeLoop edgeLoop = new EdgeLoop();
            edgeLoop.points = destructPaths[i];
            edgeLoop.isClockwise = IsClockwise(destructPaths[i]);

            _destructPaths.Add(edgeLoop);
        }

        // パスがなくなったら終了
        List<SplitTerrainData> splitTerrains = new List<SplitTerrainData>();
        if (_terrainPaths.Count == 0)
        {
            _area = 0.0f;
            return splitTerrains;
        }

        // 地形の分離判定
        splitTerrains = SplitTerrainPath();

        return splitTerrains;
    }

    // ポリゴンにひびを入れる処理
    public List<SplitTerrainData> PolygonCrack(CrackData[] data, CrackParameter crack)
    {
        TerrainSettings settings = _terrainContext.TerrainSettings;
        TerrainParameter parameter = _terrainContext.TerrainParameter;

        // 計算用の地形パスを作成
        List<Vector2[]> terrainPaths = new List<Vector2[]>(_terrainPaths.Count);
        for (int i = 0; i < _terrainPaths.Count; ++i)
        {
            terrainPaths.Add(_terrainPaths[i].points);
        }

        // すべてのひび割れ形状を格納するリスト
        List<Vector2[]> allCrackPaths = new List<Vector2[]>(data.Length);
        
        for (int idx = 0; idx < data.Length; ++idx)
        {
            Vector2 localCenter = _terrainContext.transform.InverseTransformPoint(data[idx].pos);
            Vector2 normalizedDir = data[idx].dir.normalized;
            
            // ひび割れとの最小交差距離を求める
            float minDistance = float.MaxValue;
            for (int i = 0; i < terrainPaths.Count; ++i)
            {
                Vector2[] path = terrainPaths[i];
            
                for (int j = 0; j < path.Length; ++j)
                {
                    Vector2 a = path[j];
                    Vector2 b = (j + 1 < path.Length) ? path[j + 1] : path[j + 1 - path.Length];
            
                    IntercectResult result = RaySegmentIntersection(localCenter, normalizedDir, a, b);
            
                    if (!result.isHit) continue;
            
                    if (result.distance < minDistance)
                    {
                        minDistance = result.distance;
                    }
                }
            }

            Vector2[] crackPath;
            if (minDistance < data[idx].length)
            {
                crackPath = CreateCrackPath(localCenter, data[idx].dir, minDistance, 0.0f);
            }
            else
            {
                crackPath = CreateCrackPath(localCenter, data[idx].dir, data[idx].length, 0.0f);
            }
            
            allCrackPaths.Add(crackPath);
        }
        
        if (allCrackPaths.Count > 0)
        {
            terrainPaths = PolygonDifference(terrainPaths, allCrackPaths);
        }

        // 時計回りのエッジループ(穴のエッジループ)は削除する
        for (int i = terrainPaths.Count - 1; i >= 0; --i)
        {
            if (IsClockwise(terrainPaths[i]))
            {
                terrainPaths.RemoveAt(i);
            }
        }

        // 地形パスを更新
        _terrainPaths.Clear();
        for (int i = 0; i < terrainPaths.Count; ++i)
        {
            EdgeLoop edgeLoop = new EdgeLoop();
            edgeLoop.points = terrainPaths[i];
            edgeLoop.isClockwise = false;

            _terrainPaths.Add(edgeLoop);
        }

        // 破壊形状パスを更新
        _destructPaths.Clear();

        List<SplitTerrainData> splitTerrains = new List<SplitTerrainData>();
        if (_terrainPaths.Count == 0)
        {
            _area = 0.0f;
            return splitTerrains;
        }

        // 地形の分離判定
        splitTerrains = SplitTerrainPath();

        return splitTerrains;
    }

    private List<Vector2[]> PolygonIntersect(List<Vector2[]> mainPaths, Vector2[] intersectPath)
    {
        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathD intersectPathD = VectorPathToPathD(intersectPath);

        PathsD newPathsD = Clipper.Intersect(mainPathsD, new PathsD { intersectPathD }, Clipper2Lib.FillRule.NonZero);

        return PathsDToVectorPaths(newPathsD);
    }
    
    private List<Vector2[]> PolygonDifference(List<Vector2[]> mainPaths, Vector2[] clipPath)
    {
        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathD clipPathD = VectorPathToPathD(clipPath);

        PathsD newPathsD = Clipper.Difference(mainPathsD, new PathsD() { clipPathD }, Clipper2Lib.FillRule.NonZero);

        return PathsDToVectorPaths(newPathsD);
    }
    
    private List<Vector2[]> PolygonDifference(List<Vector2[]> mainPaths, List<Vector2[]> clipPaths)
    {
        if (clipPaths == null || clipPaths.Count == 0) return mainPaths;

        PathsD mainPathsD = VectorPathsToPathsD(mainPaths);
        PathsD clipPathsD = VectorPathsToPathsD(clipPaths);

        PathsD newPathsD = Clipper.Difference(mainPathsD, clipPathsD, Clipper2Lib.FillRule.NonZero);

        return PathsDToVectorPaths(newPathsD);
    }

    private PathD VectorPathToPathD(Vector2[] vectorPath)
    {
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
        PathsD pathsD = new PathsD(vectorPaths.Count);
        for (int i = 0; i < vectorPaths.Count; ++i)
        {
            pathsD.Add(VectorPathToPathD(vectorPaths[i]));
        }
        return pathsD;
    }

    private Vector2[] PathDToVectorPath(PathD pathD)
    {
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

    private Vector2[] CreateCirclePath(Vector2 center, float radius, int vertexCount)
    {
        Vector2[] circlePath = new Vector2[vertexCount];
        for (int i = 0; i < vertexCount; ++i)
        {
            float rad = (float)i / vertexCount * Mathf.PI * 2.0f;
            circlePath[i] = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius) + center;
        }
        return circlePath;
    }

    private Vector2[] GenerateCrackPath(List<Vector2[]> mainPaths, Vector2 center, CrackParameter crack)
    {
        TerrainSettings settings = _terrainContext.TerrainSettings;
        TerrainParameter parameter = _terrainContext.TerrainParameter;

        Vector2 crackDir = _terrainContext.transform.InverseTransformDirection(crack.direction);
        float rotateAngle = UnityEngine.Random.Range(-crack.angleNoise, crack.angleNoise) * 0.5f;
        crackDir = Quaternion.Euler(0.0f, 0.0f, rotateAngle) * crackDir;
        crackDir.Normalize();

        float crackDistance = settings.CrackDistance * parameter.FractureMultiplier;
        float minDistance = float.MaxValue;

        for (int i = 0; i < mainPaths.Count; ++i)
        {
            Vector2[] path = mainPaths[i];

            for (int j = 0; j < path.Length; ++j)
            {
                Vector2 a = path[j];
                Vector2 b = (j + 1 < path.Length) ? path[j + 1] : path[j + 1 - path.Length];

                IntercectResult result = RaySegmentIntersection(center, crackDir, a, b);

                if (!result.isHit) continue;

                if (result.distance < minDistance)
                {
                    minDistance = result.distance;
                }
            }
        }

        if (minDistance < crackDistance)
        {
            return CreateCrackPath(center, crackDir, minDistance);
        }

        return null;
    }

    private IntercectResult RaySegmentIntersection(Vector2 rayOrigin, Vector2 rayDir, Vector2 segA, Vector2 segB)
    {
        IntercectResult result = new IntercectResult();

        Vector2 segVec = segB - segA;
        Vector2 normal = new Vector2(segVec.y, -segVec.x);
        float dot = Vector2.Dot(rayDir, normal);
        if (dot <= 0.0f)
        {
            result.isHit = false;
            return result;
        }

        float cross = Cross(rayDir, segVec);
        if (Mathf.Abs(cross) < Mathf.Epsilon)
        {
            result.isHit = false;
            return result;
        }

        Vector2 diff = segA - rayOrigin;
        float u = Cross(diff, rayDir) / cross;
        if (u < 0.0f || u > 1.0f)
        {
            result.isHit = false;
            return result;
        }

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

    private float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private Vector2[] CreateCrackPath(Vector2 origin, Vector2 dir, float distance, float crackNoise = -1.0f)
    {
        TerrainSettings settings = _terrainContext.TerrainSettings;

        if (crackNoise == -1.0f)
        {
            crackNoise = settings.CrackNoise;
        }

        dir = dir.normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        float halfWidth = settings.CrackWidth * 0.5f;
        float weight = settings.CrackWeight;

        int divisionCount = settings.CrackDivision;
        int start_a = 0;
        int end_a = divisionCount + 1;
        int start_b = end_a + 1;
        int end_b = start_b + divisionCount + 1;

        Vector2[] crackPath = new Vector2[end_b + 1];
        crackPath[start_a] = origin - dir * weight - normal * halfWidth;
        crackPath[end_a] = origin + dir * (distance + weight) - normal * halfWidth;
        crackPath[start_b] = origin + dir * (distance + weight) + normal * halfWidth;
        crackPath[end_b] = origin - dir * weight + normal * halfWidth;

        for (int i = 0; i < divisionCount; ++i)
        {
            float maxNoise = distance * crackNoise * 0.5f;
            float noise = UnityEngine.Random.Range(-maxNoise, maxNoise);
            float ratio = (float)(i + 1) / (float)(divisionCount + 1);

            int index_a = start_a + i + 1;
            int index_b = end_b - i - 1;

            crackPath[index_a] = Vector2.Lerp(crackPath[start_a], crackPath[end_a], ratio) + normal * noise;
            crackPath[index_b] = Vector2.Lerp(crackPath[end_b], crackPath[start_b], ratio) + normal * noise;
        }

        return crackPath;
    }

    private bool IsClockwise(Vector2[] edgeLoop)
    {
        float area = 0.0f;
        for (int i = 0; i < edgeLoop.Length; ++i)
        {
            Vector2 a = edgeLoop[i];
            Vector2 b = edgeLoop[(i + 1) % edgeLoop.Length];
            area += a.x * b.y - b.x * a.y;
        }
        return area < 0.0f;
    }

    private List<SplitTerrainData> SplitTerrainPath()
    {
        List<SplitTerrainData> result = new List<SplitTerrainData>();

        for (int i = 0; i < _terrainPaths.Count; ++i)
        {
            SplitTerrainData splitTerrain;
            splitTerrain.paths = new List<EdgeLoop>();
            splitTerrain.paths.Add(_terrainPaths[i]);
            splitTerrain.area = GetArea(splitTerrain.paths);

            result.Add(splitTerrain);
        }

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

        _terrainPaths = result[maxAreaIndex].paths;
        _area = result[maxAreaIndex].area;
        result.RemoveAt(maxAreaIndex);

        return result;
    }

    public float GetArea(List<EdgeLoop> edgeLoops)
    {
        PathsD pathsD = new PathsD(edgeLoops.Count);
        for (int i = 0; i < edgeLoops.Count; ++i)
        {
            pathsD.Add(VectorPathToPathD(edgeLoops[i].points));
        }
        return Mathf.Abs((float)Clipper.Area(pathsD));
    }
}