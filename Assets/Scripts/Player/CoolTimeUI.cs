using UnityEngine;

public class CoolTimeUI : MonoBehaviour
{
    [SerializeField]
    private float maxSize = 8.0f;

    private PlayerRocketShooter playerShooter;
    private Transform transform;
    private float coolTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform = GetComponent<Transform>();
        transform.localScale = new Vector3(maxSize,1.0f,1.0f);

        playerShooter = GetComponent<PlayerRocketShooter>();
        if (playerShooter != null)
            coolTime = playerShooter.ShootInterval;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerShooter == null)
            return;

        float timer = playerShooter.CooldownTimer;

        float ratio = Mathf.Clamp( timer / coolTime,0.0f,1.0f);


        transform.localScale = new Vector3(maxSize * ratio, 1.0f, 1.0f);
    }
}
