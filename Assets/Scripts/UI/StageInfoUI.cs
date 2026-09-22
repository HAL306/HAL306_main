using UnityEngine;
using UnityEngine.SceneManagement;

public class StageInfoUI : MonoBehaviour
{
    [SerializeField] private StageSelectUI _stageSelectUI;
    [SerializeField] private StagePageView _pagePrefab;
    [SerializeField] private Transform _pagesParent;
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private FadeUI _fadeUI;

    private StagePoint _lastSelect;
    private StagePageView _currentPage;

    private void Update()
    {
        if (_stageSelectUI.CurrentSelect != _lastSelect)
        {
            _lastSelect = _stageSelectUI.CurrentSelect;
            SpawnNewPage(_lastSelect);
        }
    }

    private void SpawnNewPage(StagePoint stage)
    {
        if (_currentPage != null)
            Destroy(_currentPage.gameObject);

        _currentPage = Instantiate(_pagePrefab, _pagesParent);
        _currentPage.SetContent(stage.StageInfoText, stage.StageImage);
        _currentPage.transform.SetAsLastSibling();
        _currentPage.PlayEnterAnimation();
        _currentPage.GoButton.onClick.AddListener(OpenConfirm);
    }

    public void OpenConfirm()
    {
        _confirmPanel.SetActive(true);
        _confirmPanel.transform.SetAsLastSibling();
    }

    public void CloseConfirm() => _confirmPanel.SetActive(false);

    public void StageStart()
    {
        GameProgress.SetCurrentPlayingStage(
            _stageSelectUI.CurrentSelect.StageIndex,
            _stageSelectUI.CurrentSelect.NextSceneName);

        _fadeUI.StartFadeOut(() =>
        {
            SceneManager.LoadScene(_stageSelectUI.CurrentSelect.NextSceneName);
        });
    }
}