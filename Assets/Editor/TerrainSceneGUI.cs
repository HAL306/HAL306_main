using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainShapeSerialize))]
public class TerrainSceneGUI : Editor
{
    private void OnEnable()
    {
        var blockShape = (TerrainShapeSerialize)target;
        blockShape.Rebuild();
    }

    private void OnSceneGUI()
    {
        var blockShape = (TerrainShapeSerialize)target;
        Color oldColor = Handles.color;
        int hoveredIndex = -1;      // マウスオーバーしている頂点のインデックス

        // オブジェクト原点を描画
        Handles.color = Color.orange;
        Vector3 objPos = blockShape.transform.position;
        float size = HandleUtility.GetHandleSize(objPos) * 0.05f;
        Handles.DotHandleCap(0, objPos, Quaternion.identity, size, EventType.Repaint);

        // 頂点のハンドル
        for (int i = 0; i < blockShape.Points.Count; ++i)
        {
            EditorGUI.BeginChangeCheck();

            // 頂点のワールド座標を取得
            int nextIndex = (i + 1) % blockShape.Points.Count;
            Vector3 currentWorldPos = blockShape.transform.TransformPoint(blockShape.Points[i]);
            Vector3 nextWorldPos = blockShape.transform.TransformPoint(blockShape.Points[nextIndex]);

            // 頂点の移動
            currentWorldPos = VertexHandle(currentWorldPos, out bool isHovered);
            if (isHovered)
                hoveredIndex = i;

            // 辺の描画
            Handles.DrawLine(currentWorldPos, nextWorldPos);

            // 変更の適用
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(blockShape, "BlockShapeGUI");
                EditorUtility.SetDirty(blockShape);
                blockShape.Points[i] = blockShape.transform.InverseTransformPoint(currentWorldPos);
                blockShape.Rebuild();
            }
        }

        // 右クリックメニュー
        if (hoveredIndex != -1 &&
            Event.current.type == EventType.MouseDown &&
            Event.current.button == 1)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("頂点を追加"), false, () =>
            {
                // 選択された頂点と次の頂点との中間点に新しい頂点を追加
                int nextIndex = (hoveredIndex + 1) % blockShape.Points.Count;
                Vector2 newPoint = Vector2.Lerp(
                    blockShape.Points[hoveredIndex], blockShape.Points[nextIndex], 0.5f);

                Undo.RecordObject(blockShape, "BlockShapeGUI");
                EditorUtility.SetDirty(blockShape);
                blockShape.Points.Insert(hoveredIndex + 1, newPoint);
                blockShape.Rebuild();
            });

            menu.AddItem(new GUIContent("頂点を削除"), false, () =>
            {
                // 選択された頂点を削除
                Undo.RecordObject(blockShape, "BlockShapeGUI");
                EditorUtility.SetDirty(blockShape);
                blockShape.Points.RemoveAt(hoveredIndex);
                blockShape.Rebuild();
            });

            menu.ShowAsContext();
            Event.current.Use();
        }

        Handles.color = oldColor;
    }

    private Vector3 VertexHandle(Vector3 pos, out bool isHovered)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        isHovered = HandleUtility.nearestControl == id;

        // 頂点のハンドル
        float size = HandleUtility.GetHandleSize(pos) * 0.05f;
        Handles.color = Color.blue;
        if (isHovered)
            size *= 1.5f;
        pos = Handles.FreeMoveHandle(id, pos, size, Vector3.zero, Handles.DotHandleCap);

        return pos;
    }
}
