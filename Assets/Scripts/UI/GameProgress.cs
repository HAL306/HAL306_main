using UnityEngine;

public static class GameProgress
{
    private const string NextStageKey = "NextStageIndex";

    public static int NextStageIndex
    {
        get => PlayerPrefs.GetInt(NextStageKey, 0);
        private set
        {
            PlayerPrefs.SetInt(NextStageKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool HasProgress => NextStageIndex > 0;

    public static int CurrentPlayingStageIndex { get; private set; }

    public static void SetCurrentPlayingStage(int stageIndex)
    {
        CurrentPlayingStageIndex = stageIndex;
    }

    public static void CompleteCurrentStage()
    {
        CompleteStage(CurrentPlayingStageIndex);
    }

    public static void CompleteStage(int stageIndex)
    {
        if (stageIndex >= NextStageIndex)
            NextStageIndex = stageIndex + 1;
    }

    public static void ResetProgress()
    {
        NextStageIndex = 0;
    }
}
