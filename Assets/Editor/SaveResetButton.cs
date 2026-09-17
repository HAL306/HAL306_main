using UnityEngine;
using UnityEditor;

public static class SaveResetButton
{
    [MenuItem("Debug/SaveReset")]
    private static void SaveReset()
    {
        GameProgress.ResetProgress();
    }
}
