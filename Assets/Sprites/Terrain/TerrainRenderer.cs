using UnityEngine;
using System.Collections.Generic;
using LibTessDotNet;


[RequireComponent (typeof(TerrainContext))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TerrainRenderer : MonoBehaviour
{
    private TerrainContext _terrainContext;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Mesh _mesh;

    public void OnChangeTerrain()
    {
        RebuildRenderMesh();
    }


    private void Awake()
    {
        _terrainContext = GetComponent<TerrainContext>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = new Mesh();

        _terrainContext.AddChangeTerrainEvent(OnChangeTerrain);
    }


    // 描画用ポリゴンを再構築する
    private bool RebuildRenderMesh()
    {
        List<EdgeLoop> terrainPaths = _terrainContext.TerrainPolygon.TerrainPath;
        if (terrainPaths == null)
            return false;

        Tess tess = new Tess();

        for (int i = 0; i < terrainPaths.Count; ++i)
        {
            Vector2[] renderMeshPath = terrainPaths[i].points;

            // 回転方向を取得
            ContourOrientation orientation;
            if (terrainPaths[i].isClockwise)
            {
                orientation = ContourOrientation.Clockwise;
            }
            else
            {
                orientation = ContourOrientation.CounterClockwise;
            }

            // エッジループを登録
            tess.AddContour(ToContour(renderMeshPath), orientation);
        }

        // エッジループを三角面化
        tess.Tessellate(WindingRule.EvenOdd, TessElementType.Polygons, 3);

        // 作成した三角面を取り出す
        Vector3[] vertices = new Vector3[tess.Vertices.Length];
        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0.0f);
        }

        // 新しくメッシュを生成
        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = tess.Elements;

        _meshFilter.mesh = _mesh;

        return true;
    }

    // LibTessDotNet用のエッジループに変換
    ContourVertex[] ToContour(Vector2[] edgeLoop)
    {
        var result = new ContourVertex[edgeLoop.Length];

        for (int i = 0; i < edgeLoop.Length; i++)
        {
            result[i].Position = new Vec3(edgeLoop[i].x, edgeLoop[i].y, 0);
        }

        return result;
    }
}
