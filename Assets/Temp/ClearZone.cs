using UnityEngine;

public class ClearZone : MonoBehaviour
{
    [SerializeField]
    Boss _boss;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            _boss.gameObject.SetActive(false);
            GetComponent<SpriteRenderer>().enabled = true;
            this.enabled = false;
        }
    }
}
