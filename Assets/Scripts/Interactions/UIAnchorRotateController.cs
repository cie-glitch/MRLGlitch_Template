using UnityEngine;
using UnityEngine.InputSystem;

public class UIAnchorRotateController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 90.0f; // degrees per second
    
    [Header("Input Settings")]
    [SerializeField] private InputActionReference rotateAction;
    
    [Header("Target GameObject")]
    [SerializeField] private GameObject targetObject; // The object to rotate
    
    private InputAction rotateInputAction;
    
    private void Awake()
    {
        // Cache the input action
        if (rotateAction != null)
            rotateInputAction = rotateAction.action;
    }
    
    private void Start()
    {
        // Nothing to initialize for the simplified version
    }
    
    private void OnEnable()
    {
        // Enable input action
        if (rotateInputAction != null)
            rotateInputAction.Enable();
    }
    
    private void OnDisable()
    {
        // Disable input action
        if (rotateInputAction != null)
            rotateInputAction.Disable();
    }
    
    private void Update()
    {
        HandleRotationInput();
    }
    
    private void HandleRotationInput()
    {
        // Check if we have a target object
        if (targetObject == null)
            return;
            
        // Check if input action is available
        if (rotateInputAction == null)
            return;
            
        // Get input from new Input System (only use X-axis)
        Vector2 rotateInput = rotateInputAction.ReadValue<Vector2>();
        float rotationInputX = rotateInput.x;
        
        // Only process if there's meaningful input
        if (Mathf.Abs(rotationInputX) < 0.01f)
            return;
            
        // Calculate rotation around world up (Y axis)
        float rotationAmount = rotationInputX * rotationSpeed * Time.deltaTime;
        
        Quaternion rotationDelta = Quaternion.AngleAxis(rotationAmount, Vector3.up);
        Quaternion newRotation = rotationDelta * targetObject.transform.rotation;
        
        targetObject.transform.rotation = newRotation;
        
        Debug.Log($"Rotating object {targetObject.name} to {newRotation.eulerAngles}");
    }
    
    // Public methods for external control
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = Mathf.Max(0, newSpeed);
    }
    
    public void RotateObjectBy(float degrees)
    {
        if (targetObject == null)
            return;
            
        Quaternion rotationDelta = Quaternion.AngleAxis(degrees, Vector3.up);
        Quaternion newRotation = rotationDelta * targetObject.transform.rotation;
        targetObject.transform.rotation = newRotation;
        
        Debug.Log($"Manually rotating object {targetObject.name} by {degrees} degrees");
    }
    
    /// <summary>
    /// Set the target anchor to rotate by ID
    /// </summary>
    /// <param name="target">The GameObject to rotate</param>
    public void SetTarget(GameObject target)
    {
        targetObject = target;
    }
    
    /// <summary>
    /// Get the current target object
    /// </summary>
    /// <returns>The currently targeted GameObject</returns>
    public GameObject GetTargetObject()
    {
        return targetObject;
    }
    
    public void SetRotateAction(InputActionReference newRotateAction)
    {
        // Disable old action
        if (rotateInputAction != null)
            rotateInputAction.Disable();
            
        // Set new action
        rotateAction = newRotateAction;
        rotateInputAction = newRotateAction?.action;
        
        // Enable new action if component is active
        if (enabled && rotateInputAction != null)
            rotateInputAction.Enable();
    }
}