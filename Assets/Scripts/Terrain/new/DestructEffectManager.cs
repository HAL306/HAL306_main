using System.Collections.Generic;
using UnityEngine;
using LibTessDotNet;

/// <summary>
/// 地形破壊時のエフェクトを一括で生成するコンポーネント
/// </summary>
public class DestructEffectManager : MonoBehaviour
{
    // プレハブとインスタンスのマップ
    Dictionary<ParticleSystem, ParticleSystem> _effectInstanceMap = 
        new Dictionary<ParticleSystem, ParticleSystem>();

    // 破壊エフェクトを生成する
    public void Emit(List<Vector2[]> destructPaths, float destructArea, 
        TerrainParameterA parameter, Transform terrainTransform)
    {
        if (destructPaths == null || destructPaths.Count == 0)
            return;

        Tess tess = new Tess();

        for (int i = 0; i < destructPaths.Count; ++i)
        {
            Vector2[] effectEmitPath = destructPaths[i];

            // エッジループを登録
            tess.AddContour(ToContour(effectEmitPath), ContourOrientation.Original);
        }

        // エッジループを三角面化
        tess.Tessellate(WindingRule.EvenOdd, TessElementType.Polygons, 3);

        // 作成した三角面を取り出す
        Vector2[] vertices = new Vector2[tess.Vertices.Length];
        for (int i = 0; i < tess.VertexCount; i++)
        {
            vertices[i] = new Vector2(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y);
        }

        // 三角面上に均等にエフェクトを発生
        for (int i = 0; i < tess.ElementCount; i += 3)
        {
            int a = tess.Elements[i];
            int b = tess.Elements[i + 1];
            int c = tess.Elements[i + 2];

            // エフェクト生成位置を求める
            float density = parameter.EffectAmount;
            List<Vector2> EmitPoints = GetEmitPos(vertices[a], vertices[b], vertices[c], density);

            // エフェクトを生成する
            for (int j = 0; j < EmitPoints.Count; j++)
            {
                Vector3 emitPos = new Vector3(EmitPoints[j].x, EmitPoints[j].y, -1.0f);
                emitPos = terrainTransform.TransformPoint(emitPos);
                ParticleSystem particleSystem = GetParticleSystemInstance(parameter.DestructEffect);
                EmitParticle(particleSystem, emitPos);
            }

            if (parameter.DestructObject != null)
            {
                // 破壊時オブジェクト生成位置を求める
                float objDensity = parameter.DestructObjectAmount;
                List<Vector2> objPoints = GetEmitPos(vertices[a], vertices[b], vertices[c], objDensity);

                // 破壊時オブジェクトを生成する
                for (int j = 0; j < EmitPoints.Count; j++)
                {
                    Vector3 objPos = new Vector3(EmitPoints[j].x, EmitPoints[j].y, 0.0f);
                    objPos = terrainTransform.TransformPoint(objPos);
                    Instantiate(parameter.DestructObject, objPos, Quaternion.identity);
                }
            }
        }
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
                (r1 * (1.0f - r2)) * b +
                (r1 * r2) * c;

            result.Add(p);
        }

        return result;
    }

    // 三角形の面積を求める
    private float GetTriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
    }

    // エフェクトのプレハブに対応するインスタンスを取得
    private ParticleSystem GetParticleSystemInstance(ParticleSystem effectPrefab)
    {
        // エフェクトのプレハブごとに一つだけインスタンスを生成
        if (!_effectInstanceMap.ContainsKey(effectPrefab))
        {
            ParticleSystem instance = Instantiate(
                effectPrefab, Vector3.zero, Quaternion.identity, this.transform);
            _effectInstanceMap[effectPrefab] = instance;
        }
        return _effectInstanceMap[effectPrefab];
    }

    // パーティクルを生成
    private void EmitParticle(ParticleSystem particleSystem, Vector3 pos)
    {
        // エフェクトを生成する
        var param = new ParticleSystem.EmitParams();
        param.position = pos;
        particleSystem.Emit(param, 1);
    }
}
