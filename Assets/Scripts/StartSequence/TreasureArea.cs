using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreasureArea : MonoBehaviour
{
    [SerializeField] private GameObject hint;
    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;
    
    [Header("InputAction登録")]
    [SerializeField, Tooltip("インタラクト")]
    private InputActionReference _interactAction;
    
    private bool _isPlayerEntered = false;
    private int _cutsceneIdHash;
    private bool _isInteracted = false;

    void Awake()
    {
        _cutsceneIdHash = Animator.StringToHash("GetTreasure");
    }
    
    private void OnEnable()
    {
        if (_interactAction != null)
        {
            _interactAction.action.performed += OnInteract;
            _interactAction.action.canceled += OnInteract;
        }
    }
    
    private void OnDisable()
    {
        if (_interactAction != null)
        {
            _interactAction.action.performed -= OnInteract;
            _interactAction.action.canceled -= OnInteract;
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!_isInteracted && _isPlayerEntered && context.performed)
        {
            _isPlayerEntered = true;
            _isInteracted = true;
            hint.SetActive(false);
            gameObject.SetActive(false);
            cutsceneEventCh.PlayCutscene(_cutsceneIdHash);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!_isInteracted)
            {
                hint.SetActive(true);
                _isPlayerEntered = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            hint.SetActive(false);
            _isPlayerEntered = false;
        }
    }
}
