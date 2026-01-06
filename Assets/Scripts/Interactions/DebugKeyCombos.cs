using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DebugKeyCombos : MonoBehaviour
{
    [Tooltip("List of input actions that must all be held")]
    public List<InputActionReference> inputActions = new List<InputActionReference>();
    
    [Tooltip("Duration in seconds that all keys must be held")]
    public float holdDuration = 1f;
    
    [Tooltip("Event triggered when combo is successfully held")]
    public UnityEvent onComboActivated;
    
    [Tooltip("Optional GameObjects to toggle on/off when combo activates")]
    public List<GameObject> targetObjects = new List<GameObject>();
    
    private float currentHoldTime = 0f;
    private bool wasTriggered = false;
    
    void OnEnable()
    {
        foreach (var actionRef in inputActions)
        {
            if (actionRef != null && actionRef.action != null)
            {
                actionRef.action.Enable();
            }
        }
    }
    
    void OnDisable()
    {
        foreach (var actionRef in inputActions)
        {
            if (actionRef != null && actionRef.action != null)
            {
                actionRef.action.Disable();
            }
        }
    }

    void Update()
    {
        if (inputActions.Count == 0)
            return;
        
        // Check if all input actions are currently held
        bool allKeysHeld = true;
        foreach (var actionRef in inputActions)
        {
            if (actionRef == null || actionRef.action == null || !actionRef.action.IsPressed())
            {
                allKeysHeld = false;
                break;
            }
        }
        
        if (allKeysHeld)
        {
            currentHoldTime += Time.deltaTime;
            
            if (currentHoldTime >= holdDuration && !wasTriggered)
            {
                wasTriggered = true;
                ActivateCombo();
            }
        }
        else
        {
            currentHoldTime = 0f;
            wasTriggered = false;
        }
    }
    
    private void ActivateCombo()
    {
        Debug.Log("Key combo activated!");
        
        onComboActivated?.Invoke();
        
        foreach (var targetObject in targetObjects)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}
