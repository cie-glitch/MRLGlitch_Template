using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class XRInput : MonoBehaviour
{
    [Header("Input Action")]
    [SerializeField] private InputActionReference buttonAction;
    
    [Header("Events")]
    public UnityEvent onButtonStart;
    public UnityEvent onButtonEnd;
    
    private void OnEnable()
    {
        if (buttonAction != null && buttonAction.action != null)
        {
            buttonAction.action.started += OnButtonStarted;
            buttonAction.action.canceled += OnButtonCanceled;
            buttonAction.action.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (buttonAction != null && buttonAction.action != null)
        {
            buttonAction.action.started -= OnButtonStarted;
            buttonAction.action.canceled -= OnButtonCanceled;
            buttonAction.action.Disable();
        }
    }
    
    private void OnButtonStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Button Pressed");
        onButtonStart?.Invoke();
    }
    
    private void OnButtonCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Button Released");
        onButtonEnd?.Invoke();
    }
}
