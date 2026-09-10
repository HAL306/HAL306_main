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
    private MeshDotRendererA _dotRenderer;
    private Mesh _mesh;

    private List<Vector2> _points;
    private List<Collider2D> _overlapColliderList;

    public IReadOnlyList<Vector2> Points => _points;

    public void Initialize(IReadOnlyList<Vector2> points)
    {
        if (_terrainContext == null)
            _terrainContext = GetComponent<TerrainContextA>();

        if (_polygonCollider == null)
            _polygonCollider = GetComponent<PolygonCollider2D>();

        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        if (_dotRenderer == null)
            _dotRenderer = GetComponent<MeshDotRendererA>();

        _points = new List<Vector2>(points);

        DestroyMesh();
        GenerateMesh();

        Rebuild();

        if (Application.isPlaying)
        {
            GetOverlapCollider();
        }
    }

    public void UpdatePoints(IReadOnlyList<Vector2> points)
    {
        _points = new List<Vector2>(points);
        Rebuild();

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
        if (_overlapColliderList != null && _overlapColliderList.Count == 0)
        {
            _terrainContext.OnOverlapEmpty();
        }
    }

    private void Rebuild()
    {
        UpdateMesh();
        UpdateCollider();

        if (_dotRenderer == null)
            _dotRenderer = GetComponent<MeshDotRendererA>();

        if (_dotRenderer != null)
        {
            _dotRenderer.RebuildDots();
        }
    }

    private void UpdateMesh()
    {
        if (_mesh == null || _meshFilter == null)
            return;

        if (_points == null || _points.Count == 0)
            return;

        var tess = new LibTessDotNet.Tess();
        tess.AddContour(ToContour(_points), LibTessDotNet.ContourOrientation.CounterClockwise);
        tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd);

        Vector3[] vertices = new Vector3[tess.Vertices.Length];
        Vector2[] uvs = new Vector2[tess.Vertices.Length];

        for (int i = 0; i < tess.Vertices.Length; ++i)
        {
            float vx = tess.Vertices[i].Position.X;
            float vy = tess.Vertices[i].Position.Y;
            vertices[i] = new Vector3(vx, vy, 0);

            // ローカル座標をそのまま UV 基準値として出力
            uvs[i] = new Vector2(vx, vy);
        }

        int[] indices = tess.Elements;

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = indices;
        _mesh.uv = uvs;
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        _meshFilter.sharedMesh = _mesh;
    }

    private LibTessDotNet.ContourVertex[] ToContour(IReadOnlyList<Vector2> points)
    {
        var contour = new LibTessDotNet.ContourVertex[points.Count];
        for (int i = 0; i < points.Count; ++i)
        {
            contour[i].Position = new LibTessDotNet.Vec3(points[i].x, points[i].y, 0.0f);
        }
        return contour;
    }

    private void UpdateCollider()
    {
        if (_polygonCollider == null || _points == null) 
            return;

        TerrainSettingsA settings = _terrainContext.TerrainSettings;
        if (settings == null)
            return;

        List<Vector2> colliderPath = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(_points, settings.SimplificationLevel);
        _polygonCollider.SetPath(0, colliderPath.ToArray());
    }

    private void GenerateMesh()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "TerrainMesh";
            _mesh.hideFlags = HideFlags.DontSave;
            _meshFilter.sharedMesh = _mesh;
        }
    }

    private void DestroyMesh()
    {
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

    private void GetOverlapCollider()
    {
        _overlapColliderList = new List<Collider2D>();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.layerMask = _terrainContext.TerrainSettings.BaseTerrainLayer;
        filter.useLayerMask = true;
        filter.useTriggers = false;

        _polygonCollider.Overlap(filter, _overlapColliderList);
    }

    private bool CheckOverlapCollider()
    {
        for (int i = 0; i < _overlapColliderList.Count; ++i)
        {
            if (_overlapColliderList[i] == null)
            {
                _overlapColliderList.RemoveAt(i);
                i--;
                continue;
            }

            ColliderDistance2D distance = _polygonCollider.Distance(_overlapColliderList[i]);

            if (distance.isOverlapped)
            {
                break;
            }
            else
            {
                _overlapColliderList.RemoveAt(i);
                i--;
            }
        }

        return _overlapColliderList.Count != 0;
    }
}