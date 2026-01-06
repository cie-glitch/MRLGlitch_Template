using UnityEngine;
using UnityEngine.InputSystem;

public class UIAnchorMoveController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private Transform referenceTransform; // If null, uses main camera
    
    [Header("Input Settings")]
    [SerializeField] private InputActionReference moveAction;
    
    [Header("Target GameObject")]
    [SerializeField] private GameObject targetObject; // The object to move
    
    private Camera mainCamera;
    private InputAction moveInputAction;
    
    private void Awake()
    {
        // Cache the input action
        if (moveAction != null)
            moveInputAction = moveAction.action;
    }
    
    private void Start()
    {
        // Find reference transform if not assigned
        if (referenceTransform == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
                referenceTransform = mainCamera.transform;
        }
    }
    
    private void OnEnable()
    {
        // Enable input action
        if (moveInputAction != null)
            moveInputAction.Enable();
    }
    
    private void OnDisable()
    {
        // Disable input action
        if (moveInputAction != null)
            moveInputAction.Disable();
    }
    
    private void Update()
    {
        HandleMovementInput();
    }
    
    private void HandleMovementInput()
    {
        // Check if we have a valid target object to move
        if (targetObject == null)
        {
            return;
        }
            
        // Check if input action is available
        if (moveInputAction == null)
            return;
            
        // Get input from new Input System
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        float horizontal = moveInput.x;
        float vertical = moveInput.y;
        
        // Only process if there's input
        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
            return;
            
        // Calculate movement direction
        Vector3 moveDirection = CalculateMovementDirection(horizontal, vertical);
        
        // Apply movement
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
        ApplyMovement(movement);
    }
    
    private Vector3 CalculateMovementDirection(float horizontal, float vertical)
    {
        Vector3 direction = Vector3.zero;
        
        // Always move relative to the camera/reference transform
        if (referenceTransform != null)
        {
            Vector3 forward = referenceTransform.forward;
            Vector3 right = referenceTransform.right;
            
            // Flatten to horizontal plane (remove Y component)
            forward.y = 0;
            right.y = 0;
            
            forward.Normalize();
            right.Normalize();
            
            // Map controller input to camera-relative directions
            // Vertical input -> camera forward (flattened)
            // Horizontal input -> camera right (flattened)
            direction = (right * horizontal) + (forward * vertical);
        }
        else
        {
            // Fallback to world space if no reference transform
            direction = new Vector3(horizontal, 0, vertical);
        }
        
        return direction.normalized;
    }
    
    private void ApplyMovement(Vector3 movement)
    {
        if (targetObject != null)
        {
            Vector3 newPosition = targetObject.transform.position + movement;
            targetObject.transform.position = newPosition;
            
            Debug.Log($"Moving target object to {newPosition}");
        }
    }
    
    /// <summary>
    /// Set the target object to move
    /// </summary>
    /// <param name="target">The GameObject to move</param>
    public void SetTargetObject(GameObject target)
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
    
    // Public methods for external control
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0, newSpeed);
    }
    
    public void SetReferenceTransform(Transform reference)
    {
        referenceTransform = reference;
    }
    
    public void SetMoveAction(InputActionReference newMoveAction)
    {
        // Disable old action
        if (moveInputAction != null)
            moveInputAction.Disable();
            
        // Set new action
        moveAction = newMoveAction;
        moveInputAction = newMoveAction?.action;
        
        // Enable new action if component is active
        if (enabled && moveInputAction != null)
            moveInputAction.Enable();
    }
}