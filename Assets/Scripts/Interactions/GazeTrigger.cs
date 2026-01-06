using UnityEngine;
using UnityEngine.Events;

public class GazeTrigger : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The camera to check (defaults to main camera if not set)")]
    public Camera targetCamera;
    
    [Header("Position Check")]
    [Tooltip("Enable position checking")]
    public bool checkPosition = true;
    
    [Tooltip("Target position the camera should be at")]
    public Transform targetPosition;
    
    [Tooltip("Maximum distance from target position to trigger")]
    public float positionThreshold = 1f;
    
    [Header("Gaze Check")]
    [Tooltip("Enable gaze direction checking")]
    public bool checkGaze = true;
    
    [Tooltip("Target object the camera should be looking at")]
    public GameObject targetObject;
    
    [Tooltip("Maximum angle in degrees to enter trigger")]
    public float gazeAngleEnter = 10f;
    
    [Header("Hysteresis")]
    [Tooltip("Distance multiplier for exit (should be larger than 1.0)")]
    public float positionExitMultiplier = 1.2f;
    
    [Header("Events")]
    [Tooltip("Event triggered when conditions are met")]
    public UnityEvent onGazeTriggerEnter;
    
    [Tooltip("Event triggered when conditions are no longer met")]
    public UnityEvent onGazeTriggerExit;
    
    private bool isTriggered = false;
    
    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("GazeTrigger: No camera assigned and no main camera found!");
        }
    }
    
    void Update()
    {
        if (targetCamera == null)
            return;
        
        bool conditionsMet = CheckConditions();
        
        // Trigger enter event
        if (conditionsMet && !isTriggered)
        {
            isTriggered = true;
            onGazeTriggerEnter?.Invoke();
        }
        // Trigger exit event
        else if (!conditionsMet && isTriggered)
        {
            isTriggered = false;
            onGazeTriggerExit?.Invoke();
        }
    }
    
    private bool CheckConditions()
    {
        bool positionCheck = true;
        bool gazeCheck = true;
        
        // Check position if enabled (with hysteresis)
        if (checkPosition)
        {
            if (targetPosition == null)
            {
                positionCheck = false;
            }
            else
            {
                float distance = Vector3.Distance(targetCamera.transform.position, targetPosition.position);
                float threshold = isTriggered ? positionThreshold * positionExitMultiplier : positionThreshold;
                positionCheck = distance <= threshold;
            }
        }
        
        // Check gaze if enabled (with hysteresis)
        if (checkGaze)
        {
            if (targetObject == null)
            {
                gazeCheck = false;
            } else if(isTriggered){
                gazeCheck = true;
            } else
            {
                Vector3 directionToTarget = (targetObject.transform.position - targetCamera.transform.position).normalized;
                float angle = Vector3.Angle(targetCamera.transform.forward, directionToTarget);
                float threshold = gazeAngleEnter;
                gazeCheck = angle <= threshold;
            }
        }
        
        return positionCheck && gazeCheck;
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        
        if (targetCamera == null)
            return;
        
        // Draw position threshold sphere
        if (checkPosition && targetPosition != null)
        {
            bool inPosition = Vector3.Distance(targetCamera.transform.position, targetPosition.position) <= positionThreshold;
            Gizmos.color = inPosition ? new Color(0, 1, 1, 0.3f) : new Color(0, 1, 1, 0.1f);
            Gizmos.DrawSphere(targetPosition.position, positionThreshold);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition.position, positionThreshold);
            
            // Draw line from camera to target position
            Gizmos.color = inPosition ? Color.green : Color.red;
            Gizmos.DrawLine(targetCamera.transform.position, targetPosition.position);
        }
        
        // Draw gaze direction and cone
        if (checkGaze && targetObject != null)
        {
            Vector3 directionToTarget = (targetObject.transform.position - targetCamera.transform.position).normalized;
            float angle = Vector3.Angle(targetCamera.transform.forward, directionToTarget);
            bool lookingAtTarget = isTriggered || angle <= gazeAngleEnter;
            
            // Draw line to target object
            Gizmos.color = lookingAtTarget ? Color.green : Color.yellow;
            Gizmos.DrawLine(targetCamera.transform.position, targetObject.transform.position);
            
            // Draw camera forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(targetCamera.transform.position, targetCamera.transform.forward * 5f);
            
            // Draw enter cone (green)
            DrawGazeCone(targetCamera.transform.position, targetCamera.transform.forward, gazeAngleEnter, 5f, new Color(0, 1, 0, 0.1f));
            
            // Draw target object indicator
            Gizmos.color = lookingAtTarget ? Color.green : Color.red;
            Gizmos.DrawWireSphere(targetObject.transform.position, 0.2f);
        }
        
        // Draw overall trigger state at camera position
        Gizmos.color = isTriggered ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(targetCamera.transform.position, 0.3f);
    }
    
    void OnDrawGizmosSelected()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        
        if (targetCamera == null)
            return;
        
        // Draw more detailed info when selected
        if (checkGaze && targetObject != null)
        {
            // Draw angle arc
            Vector3 directionToTarget = (targetObject.transform.position - targetCamera.transform.position).normalized;
            float angle = Vector3.Angle(targetCamera.transform.forward, directionToTarget);
            
            // Draw angle value visualization
            Gizmos.color = Color.white;
            Vector3 midPoint = targetCamera.transform.position + targetCamera.transform.forward * 2f;
            Gizmos.DrawWireSphere(midPoint, 0.1f);
        }
    }
    
    private void DrawGazeCone(Vector3 origin, Vector3 direction, float angleThreshold, float distance, Color color)
    {
        int segments = 16;
        float radius = Mathf.Tan(angleThreshold * Mathf.Deg2Rad) * distance;
        Vector3 endCenter = origin + direction * distance;
        
        Gizmos.color = color;
        
        // Draw cone circle at end
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right == Vector3.zero)
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        Vector3 prevPoint = endCenter + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 point = endCenter + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            
            Gizmos.DrawLine(prevPoint, point);
            Gizmos.DrawLine(origin, point);
            
            prevPoint = point;
        }
    }
}
