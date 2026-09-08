using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Stage/StageData")]
public class StageData : ScriptableObject
{
    public string stageName;          // ステージ説明ラベル
    public Sprite thumbnail;          // ステージ画面（一部）
    [TextArea] public string description; // ステージ説明
    public string sceneName;          // 実際のステージのシーン名
    public Vector2 pinAnchorPosition; // 地図上のピン配置座標
}