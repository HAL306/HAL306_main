using UnityEngine;

public class StagePoint : MonoBehaviour
{
    [SerializeField, Tooltip("次のシーン名")]
    private string _nextSceneName;

    [SerializeField, Tooltip("ステージ名")]
    private string _stageName;

    [SerializeField, Tooltip("ステージ説明"), TextArea]
    private string _stageInfoText;

    [SerializeField, Tooltip("ステージ紹介画像")]
    private Sprite _stageImage;

    [SerializeField, Tooltip("ステージ番号（進行順）")]
    private int _stageIndex;

    public int StageIndex => _stageIndex;

    private Animator _animator;
    private Collider2D _collider;

    public Collider2D Collider => _collider;
    public string NextSceneName => _nextSceneName;
    public string StageName => _stageName;
    public string StageInfoText => _stageInfoText;
    public Sprite StageImage => _stageImage;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }
}