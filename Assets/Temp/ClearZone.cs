using UnityEngine;

public class ClearZone : MonoBehaviour
{
    [SerializeField]
    BOSScharge _boss;

    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            _boss.gameObject.SetActive(false);
            cutsceneEventCh.PlayCutscene("StageClear");
            this.enabled = false;
        }
    }
}
