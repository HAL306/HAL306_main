using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LineRenderer))]
public class LineNormalizeGUI : Editor
{
    public override void OnInspectorGUI()
    {
        // 元々のInspectorを表示
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Z値の正規化"))
        {
            LineRenderer line = (LineRenderer)target;
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 pos = line.GetPosition(i);
                pos.z = 0.0f;
                line.SetPosition(i, pos);
            }

            EditorUtility.SetDirty(line);
        }
    }
}