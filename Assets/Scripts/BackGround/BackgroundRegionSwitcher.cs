using UnityEngine;

/// <summary>
/// エリアごとに異なる背景画像を切り替える
/// カメラが境界に近づくとクロスフェードして自然に切り替える
/// </summary>
public class BackgroundRegionSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class Region
    {
        public string name;
        public float startX;      // このエリアの開始X座標
        public float endX;        // このエリアの終了X座標
        public Sprite backgroundSprite;
    }

    [SerializeField] private Region[] _regions;
    [SerializeField] private float _transitionDistance = 5f; // 境界の何ユニット手前からフェードを始めるか

    private SpriteRenderer _currentRenderer;
    private SpriteRenderer _nextRenderer;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        _currentRenderer = gameObject.AddComponent<SpriteRenderer>();
        GameObject nextObj = new GameObject("NextRegionLayer");
        nextObj.transform.SetParent(transform);
        nextObj.transform.localPosition = Vector3.zero;
        _nextRenderer = nextObj.AddComponent<SpriteRenderer>();

        _currentRenderer.sortingOrder = 0;
        _nextRenderer.sortingOrder = 1;

        SetInitialRegion();
    }

    private void SetInitialRegion()
    {
        Region region = GetRegionAt(_cam.transform.position.x);
        if (region != null)
            _currentRenderer.sprite = region.backgroundSprite;
    }

    private void LateUpdate()
    {
        float camX = _cam.transform.position.x;
        Region current = GetRegionAt(camX);
        if (current == null) return;

        // 境界に近づいているかチェック
        float distToEnd = current.endX - camX;
        float distToStart = camX - current.startX;

        Region upcoming = null;
        float t = 0f;

        if (distToEnd < _transitionDistance)
        {
            upcoming = GetRegionAt(current.endX + 0.1f);
            t = 1f - Mathf.Clamp01(distToEnd / _transitionDistance);
        }
        else if (distToStart < _transitionDistance)
        {
            upcoming = GetRegionAt(current.startX - 0.1f);
            t = 1f - Mathf.Clamp01(distToStart / _transitionDistance);
        }

        if (upcoming != null && upcoming != current)
        {
            _currentRenderer.sprite = current.backgroundSprite;
            _nextRenderer.sprite = upcoming.backgroundSprite;

            Color c = _nextRenderer.color;
            c.a = t; // 近づくほど次のエリアの画像を濃く表示する
            _nextRenderer.color = c;
        }
        else
        {
            _currentRenderer.sprite = current.backgroundSprite;
            Color c = _nextRenderer.color;
            c.a = 0f;
            _nextRenderer.color = c;
        }
    }

    private Region GetRegionAt(float x)
    {
        foreach (var r in _regions)
        {
            if (x >= r.startX && x <= r.endX)
                return r;
        }
        return null;
    }
}