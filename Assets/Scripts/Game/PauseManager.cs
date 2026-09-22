using UnityEngine;
using UnityEngine.InputSystem;


public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseUI;

    private InputAction _PauseAction;

    private bool isPaused = false;

    private void Awake()
    {
        _PauseAction = InputSystem.actions.FindAction("Pause");
    }

    private void OnEnable()
    {
        _PauseAction.Enable();
    }

    private void OnDisable()
    {
        _PauseAction.Disable();
    }

    private void Update()
    {
        if (_PauseAction.WasPressedThisFrame())
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseUI.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseUI.SetActive(false);
    }
}