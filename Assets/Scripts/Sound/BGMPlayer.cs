using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip BGM;
    [SerializeField] private float volume = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BGMManager.Instance.PlayBGM(BGM,volume);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
