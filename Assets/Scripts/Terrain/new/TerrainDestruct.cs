using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 分離した地形のデータ
public struct SplitTerrainDataA
{
    public Vector2[] path;      // 分離した地形のパス
    public float area;          // 分離した地形の面積
}

public struct DestructResult
{
    public Vector2[] mainPath;              // 破壊後のメイン地形のパス
    public float mainArea;                  // 破壊後のメイン地形の面積      
    public List<Vector2[]> destructPaths;   // 破壊形状のパス
    public float destructArea;              // 破壊形状の面積
    public List<SplitTerrainDataA> splitTerrainData;    // 分離地形の情報
}


/// <summary>
/// 地形の破壊処理を行うコンポーネント
/// </summary>
public class TerrainDestruct : MonoBehaviour
{
    // 交差判定結果
    struct IntercectResult
    {
        public bool isHit;                  // 交差判定
        public float distance;              // 交差位置までの距離
        public Vector2 point;               // 交差位置
    }

    private TerrainContextA _terrainContext;
    private TerrainShape _terrainShape;


    // ポリゴンの破壊処理
    public DestructResult PolygonDestruct(Vector2 worldCenter, float radius, CrackParameter crack)
    {
        TerrainSettingsA settings = _terrainContext.TerrainSettings;
        TerrainParameterA parameter = _terrainContext.TerrainParameter;
        Vector2 localCenter = _terrainContext.transform.InverseTransformPoint(worldCenter);

        // 計算用の地形パスを作成
        var oldTerrainPaths = new List<Vector2[]>();
        oldTerrainPaths.Add(_terrainShape.Points.ToArray());
        var terrainPaths = new List<Vector2[]>();
        terrainPaths.Add(_terrainShape.Points.ToArray());

        // 破壊範囲の円を生成
        float circleRadius = radius * parameter.Destructibility;
        Vector2[] circlePath = CreateCirclePath(localCenter, circleRadius, settings.CircleVertex);

        // 円形に削る
        terrainPaths = CipperUtility.PolygonDifference(terrainPaths, circlePath);

        // ひび割れ形状の作成
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

        // ひび割れ形状で削る
        if (allCrackPaths.Count > 0)
        {
            terrainPaths = CipperUtility.PolygonDifference(terrainPaths, allCrackPaths);
        }

        // 時計回りのエッジループ(穴のエッジループ)は削除する
        for (int i = terrainPaths.Count - 1; i >= 0; --i)
        {
            if (CipperUtility.IsClockwise(terrainPaths[i]))
            {
                terrainPaths.RemoveAt(i);
            }
        }

        // 破壊結果を作成
        DestructResult destructResult = CreateDestructResult(terrainPaths, oldTerrainPaths);

        return destructResult;
    }

    // 三品怜、芝晃佑
    // ポリゴンにひびを入れる処理
    public DestructResult PolygonCrack(CrackData[] data, CrackParameter crack)
    {
        TerrainSettingsA settings = _terrainContext.TerrainSettings;
        TerrainParameterA parameter = _terrainContext.TerrainParameter;

        // 計算用の地形パスを作成
        var oldTerrainPaths = new List<Vector2[]>();
        oldTerrainPaths.Add(_terrainShape.Points.ToArray());
        var terrainPaths = new List<Vector2[]>();
        terrainPaths.Add(_terrainShape.Points.ToArray());

        // すべてのひび割れ形状を格納するリスト
        List<Vector2[]> allCrackPaths = new List<Vector2[]>(data.Length);

        for (int idx = 0; idx < data.Length; ++idx)
        {
            Vector2 localCenter = _terrainContext.transform.InverseTransformPoint(data[idx].pos);
            Vector2 normalizedDir = data[idx].dir.normalized;
            // ひび割れ処理
            // ひび割れとの最小交差距離を求める
            float minDistance = float.MaxValue;
            for (int i = 0; i < terrainPaths.Count; ++i)
            {
                Vector2[] path = terrainPaths[i];

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
                    result = RaySegmentIntersection(localCenter, normalizedDir, a, b);

                    if (!result.isHit)
                        continue;

                    // 最も近い交差距離を保持
                    if (result.distance < minDistance)
                    {
                        minDistance = result.distance;
                    }
                }
            }

            Vector2[] crackPath;
            // 辺に届いてたら辺までの長さにする、届いてなくてもひびは入れる
            if (minDistance < data[idx].length)
            {
                // ひび割れ形状を作る
                crackPath = CreateCrackPath(localCenter, data[idx].dir, minDistance, 0.0f);
            }
            else
            {
                // ひび割れ形状を作る
                crackPath = CreateCrackPath(localCenter, data[idx].dir, data[idx].length, 0.0f);
            }

            // ひび割れ形状をリストに追加し、後で一括処理
            allCrackPaths.Add(crackPath);
        }

        // リストアップしたひび割れ形状を一括で削る
        if (allCrackPaths.Count > 0)
        {
            terrainPaths = CipperUtility.PolygonDifference(terrainPaths, allCrackPaths);
        }

        // 時計回りのエッジループ(穴のエッジループ)は削除する
        for (int i = terrainPaths.Count - 1; i >= 0; --i)
        {
            if (CipperUtility.IsClockwise(terrainPaths[i]))
            {
                terrainPaths.RemoveAt(i);
            }
        }

        // 破壊結果を作成
        DestructResult destructResult = CreateDestructResult(terrainPaths, oldTerrainPaths);

        return destructResult;
    }

    private void Awake()
    {
        if (_terrainShape == null)
            _terrainShape = GetComponent<TerrainShape>();

        if(_terrainContext == null)
            _terrainContext = GetComponent<TerrainContextA>();
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

    // ひび割れ形状生成処理
    private Vector2[] GenerateCrackPath(List<Vector2[]> mainPaths, Vector2 center, CrackParameter crack)
    {
        TerrainSettingsA settings = _terrainContext.TerrainSettings;
        TerrainParameterA parameter = _terrainContext.TerrainParameter;

        // ひび割れ方向を求める
        Vector2 crackDir = _terrainContext.transform.InverseTransformDirection(crack.direction);
        float rotateAngle = UnityEngine.Random.Range(-crack.angleNoise, crack.angleNoise) * 0.5f;
        crackDir = Quaternion.Euler(0.0f, 0.0f, rotateAngle) * crackDir;
        crackDir.Normalize();

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

        // ひび割れ形状を返す
        if (minDistance < crackDistance)
        {
            return CreateCrackPath(center, crackDir, minDistance);
        }

        return null;
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
    private Vector2[] CreateCrackPath(
        Vector2 origin, Vector2 dir, float distance, float crackNoise = -1.0f)
    {
        TerrainSettingsA settings = _terrainContext.TerrainSettings;

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


        // 基準となる線を作成
        Vector2[] crackPath = new Vector2[end_b + 1];
        crackPath[start_a] = origin - dir * weight - normal * halfWidth;
        crackPath[end_a] = origin + dir * (distance + weight) - normal * halfWidth;
        crackPath[start_b] = origin + dir * (distance + weight) + normal * halfWidth;
        crackPath[end_b] = origin - dir * weight + normal * halfWidth;

        // 細分化しノイズでずらす
        for (int i = 0; i < divisionCount; ++i)
        {
            float maxNoise = distance * crackNoise * 0.5f;

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

    // 地形分離判定
    private DestructResult CreateDestructResult(
        List<Vector2[]> terrainPaths,
        List<Vector2[]> oldTerrainPaths)
    {
        DestructResult result = new DestructResult();

        // 破壊形状を求める
        result.destructPaths = CipperUtility.PolygonDifference(oldTerrainPaths, terrainPaths);
        result.destructArea = CipperUtility.GetArea(result.destructPaths);

        // 地形がなくなった場合は終了
        if (terrainPaths.Count == 0)
        {
            result.mainArea = 0.0f;
            result.mainPath = new Vector2[0];
            result.splitTerrainData = new List<SplitTerrainDataA>();
            return result;
        }

        // 地形の分離を行う
        List<SplitTerrainDataA> splitTerrains = new List<SplitTerrainDataA>();
        for (int i = 0; i < terrainPaths.Count; ++i)
        {
            SplitTerrainDataA splitData;
            splitData.path = terrainPaths[i];
            splitData.area = CipperUtility.GetArea(terrainPaths[i]);
            splitTerrains.Add(splitData);
        }
        result.splitTerrainData = splitTerrains;

        // 最大面積の地形を求める
        float maxArea = 0.0f;
        int maxAreaIndex = 0;
        for (int i = 0; i < result.splitTerrainData.Count; ++i)
        {
            if (result.splitTerrainData[i].area > maxArea)
            {
                maxArea = result.splitTerrainData[i].area;
                maxAreaIndex = i;
            }
        }

        // 最大面積の分離地形を元の地形とする
        result.mainPath = result.splitTerrainData[maxAreaIndex].path;
        result.mainArea = result.splitTerrainData[maxAreaIndex].area;
        result.splitTerrainData.RemoveAt(maxAreaIndex);

        return result;
    }
}
