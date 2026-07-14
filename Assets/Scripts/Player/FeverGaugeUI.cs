using UnityEngine;

public class FeverGaugeUI : MonoBehaviour
{
    private float width = 0.0f;   // サイズ
    private float height = 0.0f;

    private PlayerFever playerFever;
    private RectTransform rectTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // シーン内のPlayerShooterを自動で取得する
        playerFever = FindAnyObjectByType<PlayerFever>();
        rectTransform = GetComponent<RectTransform>();
        width = rectTransform.rect.width;
        height = rectTransform.rect.height;
    }

    // Update is called once per frame
    void Update()
    {
        rectTransform.sizeDelta = new Vector2(width * playerFever.GetRate(), height);
    }
}
