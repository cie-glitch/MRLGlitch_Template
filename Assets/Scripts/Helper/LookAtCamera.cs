using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The camera to look at (defaults to main camera if not set)")]
    public Camera targetCamera;
    
    [Tooltip("Rotation speed (0 = instant, higher = slower)")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Invert the facing direction (look away from camera)")]
    public bool invert = false;
    
    [Tooltip("Lock X axis rotation")]
    public bool lockX = false;
    
    [Tooltip("Lock Y axis rotation")]
    public bool lockY = false;
    
    [Tooltip("Lock Z axis rotation")]
    public bool lockZ = false;
    
    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
    
    void Update()
    {
        if (targetCamera == null)
            return;
        
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;
        
        if (invert)
            directionToCamera = -directionToCamera;
        
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            
            // Apply axis locks
            if (lockX || lockY || lockZ)
            {
                Vector3 currentEuler = transform.rotation.eulerAngles;
                Vector3 targetEuler = targetRotation.eulerAngles;
                
                if (lockX) targetEuler.x = currentEuler.x;
                if (lockY) targetEuler.y = currentEuler.y;
                if (lockZ) targetEuler.z = currentEuler.z;
                
                targetRotation = Quaternion.Euler(targetEuler);
            }
            
            if (smoothSpeed > 0f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }
    }
}
