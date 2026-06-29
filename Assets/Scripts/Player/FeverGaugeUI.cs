using UnityEngine;

public class FeverGaugeUI : MonoBehaviour
{
    [SerializeField, Tooltip("ゲージのサイズ")]
    private float size = 300.0f;

    private PlayerFever playerFever;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // シーン内のPlayerShooterを自動で取得する
        playerFever = FindAnyObjectByType<PlayerFever>();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
