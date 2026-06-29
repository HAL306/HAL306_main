using UnityEngine;

public class FeverGaugeUI : MonoBehaviour
{
    [SerializeField, Tooltip("ゲージの横幅")]
    private float width = 300.0f;

    [SerializeField, Tooltip("ゲージの縦")]
    private float height = 60.0f;

    private PlayerFever playerFever;
    private RectTransform rectTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // シーン内のPlayerShooterを自動で取得する
        playerFever = FindAnyObjectByType<PlayerFever>();
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        rectTransform.sizeDelta = new Vector2(width * playerFever.GetRate(), height);
    }
}
