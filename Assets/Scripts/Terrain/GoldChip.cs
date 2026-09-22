using UnityEngine;

public class GoldChip : MonoBehaviour
{
    // 仮実装

    float currentSpeed = -2.0f;
    GameObject player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        currentSpeed += Time.deltaTime * 10.0f;
        if (player != null)
        {
            Vector3 pos = Vector3.MoveTowards(transform.position, player.transform.position, currentSpeed * Time.deltaTime);
            transform.position = pos;
            if(Vector3.Distance(transform.position, player.transform.position) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
