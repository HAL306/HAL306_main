using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形の形状を管理するコンポーネント
/// </summary>
[RequireComponent(typeof(TerrainContextA))]
public class TerrainShape : MonoBehaviour
{
    private TerrainContextA _terrainContext;

    private PolygonCollider2D _polygonCollider;
    private MeshFilter _meshFilter;
    private Mesh _mesh;

    private List<Vector2> _points;                      // 頂点リスト
    private List<Collider2D> _overlapColliderList;      // 重なっているコライダーのリスト

    public IReadOnlyList<Vector2> Points => _points;


    // 地形形状の初期化
    public void Initialize(IReadOnlyList<Vector2> points)
    {
        if (_terrainContext == null)
            _terrainContext = GetComponent<TerrainContextA>();

        if (_polygonCollider == null)
            _polygonCollider = GetComponent<PolygonCollider2D>();

        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        _points = new List<Vector2>(points);

        // メッシュはコピー時の共有対策に再生成する
        DestroyMesh();
        GenerateMesh();

        Rebuild();

        // 実行中のみの処理
        if (Application.isPlaying)
        {
            // 重なっているベース地形のコライダーを取得
            GetOverlapCollider();
        }
    }

    // 地形形状の頂点リストを更新
    public void UpdatePoints(IReadOnlyList<Vector2> points)
    {
        _points = new List<Vector2>(points);
        Rebuild();

        // 実行中のみの処理
        if (Application.isPlaying)
        {
            if (!CheckOverlapCollider())
            {
                _terrainContext.OnOverlapEmpty();
            }
        }
    }


    private void Start()
    {
        // 初期化順の関係で、ベース地形と重なっていない通知はStartで行う
        if (_overlapColliderList.Count == 0)
        {
            _terrainContext.OnOverlapEmpty();
        }
    }

    // 地形形状の再構築
    private void Rebuild()
    {
        UpdateMesh();
        UpdateCollider();
    }

    // メッシュを更新
    private void UpdateMesh()
    {
        if (_mesh == null || _meshFilter == null)
            return;

        if (_points == null || _points.Count == 0)
            return;

        // 三角形分割
        var tess = new LibTessDotNet.Tess();
        tess.AddContour(ToContour(_points), LibTessDotNet.ContourOrientation.CounterClockwise);
        tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.TessElementType.Polygons, 3);

        // メッシュ化
        Vector3[] vertices = new Vector3[tess.Vertices.Length];
        for (int i = 0; i < tess.Vertices.Length; ++i)
        {
            vertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0);
        }
        int[] indices = tess.Elements;

        // メッシュを更新
        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = indices;
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        _meshFilter.sharedMesh = _mesh;
    }

    // LibTessDotNetの頂点配列に変換
    private LibTessDotNet.ContourVertex[] ToContour(IReadOnlyList<Vector2> points)
    {
        var contour = new LibTessDotNet.ContourVertex[points.Count];
        for (int i = 0; i < points.Count; ++i)
        {
            contour[i].Position = new LibTessDotNet.Vec3(points[i].x, points[i].y, 0.0f);
        }
        return contour;
    }

    // コライダーを更新
    private void UpdateCollider()
    {
        if (_polygonCollider == null || _points == null) 
            return;

        TerrainSettingsA settings = _terrainContext.TerrainSettings;
        if(settings == null)
            return;

        // コライダー用にパスを簡略化
        List<Vector2> colliderPath;
        colliderPath = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(_points, settings.SimplificationLevel);

        // コライダー形状を更新
        _polygonCollider.SetPath(0, colliderPath.ToArray());
    }

    // メッシュを生成
    private void GenerateMesh()
    {
        // メッシュを生成
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "TerrainMesh";
            _mesh.hideFlags = HideFlags.DontSave;
            _meshFilter.sharedMesh = _mesh;
        }
    }

    // メッシュを破棄
    private void DestroyMesh()
    {
        // メッシュを破棄
        if (_mesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }
            _mesh = null;
        }
    }

    // 重なっているベース地形のコライダーを取得する (厳密な判定は行いません)
    private void GetOverlapCollider()
    {
        _overlapColliderList = new List<Collider2D>();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.layerMask = _terrainContext.TerrainSettings.BaseTerrainLayer;
        filter.useLayerMask = true;
        filter.useTriggers = false;

        // 重なっているベース地形のコライダー取得
        _polygonCollider.Overlap(filter, _overlapColliderList);
    }

    // ベース地形との重なりを調べる
    private bool CheckOverlapCollider()
    {
        for (int i = 0; i < _overlapColliderList.Count; ++i)
        {
            if (_overlapColliderList[i] == null)
            {
                // リストから削除して、インデックスを補正
                _overlapColliderList.RemoveAt(i);
                i--;
            }

            // 重なりを調べる
            ColliderDistance2D distance;
            distance = _polygonCollider.Distance(_overlapColliderList[i]);

            // 重なっていなければ除外
            if (distance.isOverlapped)
            {
                // 一つでも重なっていれば終了
                break;
            }
            else
            {
                // リストから削除して、インデックスを補正
                _overlapColliderList.RemoveAt(i);
                i--;
            }
        }

        return _overlapColliderList.Count != 0;
    }
}
