using UnityEngine;
using UnityEngine.InputSystem;

public class AnchorCursor : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;

    [SerializeField] private Transform cursorTransform;
    [SerializeField] private float moveSpeed = 1;
    [SerializeField] private float rotationSpeed = 100;
    public Transform leftHandController;

    private Transform cameraTransform;

    private InputAction moveInputAction;

    private bool moving = false;
    private bool rotating = false;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        moveInputAction = moveAction.action;

        moveInputAction.Enable();
    }

    // Update is called once per frame
    public void PlaceAtController()
    {
        Debug.Log("Placing cursor at controller position");
        Vector3 targetPosition = leftHandController.position;
        targetPosition.y = 0;
        cursorTransform.position = targetPosition;

    }

    public void SetMoving()
    {
        moving = true;
        rotating = false;
    }

    public void SetRotating()
    {
        moving = false;
        rotating = true;
    }

    void Update(){
        if (moving || rotating)
            HandleMovementInput();
    }

    private void HandleMovementInput()
    {


        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        float horizontal = moveInput.x;
        float vertical = moveInput.y;

        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
            return;
        if (moving)
            HandleMovement(moveInput);
        if (rotating)
            HandleRotation(moveInput);
    }

    void HandleMovement(Vector2 moveInput)
    {
        Vector3 forwardDirection = cameraTransform.forward;
        forwardDirection.y = 0;
        forwardDirection.Normalize();
        cursorTransform.position += forwardDirection * moveInput.y * moveSpeed * Time.deltaTime;

        Vector3 rightDirection = cameraTransform.right;
        rightDirection.y = 0;
        rightDirection.Normalize();
        cursorTransform.position += rightDirection * moveInput.x * moveSpeed * Time.deltaTime;
    }

    void HandleRotation(Vector2 moveInput)
    {
        cursorTransform.rotation *= Quaternion.Euler(Vector3.up * moveInput.x * rotationSpeed * Time.deltaTime);
    }
}
