using UnityEngine;
using UnityEngine.InputSystem;

public class InputEnabler : MonoBehaviour
{
    [SerializeField, Tooltip("有効化するInputActionsアセット")]
    private InputActionAsset inputAsset;

    private static InputEnabler _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (_instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("InputEnabler");
        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            Debug.LogError("Resourcesフォルダに 'InputEnabler' プレハブが見つかりません！");
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        inputAsset?.Enable();
    }

    private void OnDisable()
    {
        inputAsset?.Disable();
    }
}