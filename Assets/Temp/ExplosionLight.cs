using UnityEngine;
using UnityEngine.InputSystem;

public class ExplosionLight : MonoBehaviour
{
    [SerializeField, Tooltip("生存時間")]
    private float lifeTime;

    private float timer;

    private Light light;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        light = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        float rate = Mathf.Clamp01(timer / lifeTime);

        light.intensity *= (1.0f - rate);

        if (timer > lifeTime)
        {
            Destroy(gameObject);
        }

    }
}
