using UnityEngine;
using UnityEngine.Events;

public class ProximityTrigger : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnProximityEnter;
    public UnityEvent OnProximityExit;
    
    [Header("Time Delay Settings")]
    [Tooltip("Require the collider to stay inside for this duration before triggering enter event")]
    public bool useDelayedTrigger = false;
    
    [Tooltip("Time in seconds the collider must remain inside")]
    public float requiredTime = 2f;
    
    private bool isInside = false;
    private float timeInside = 0f;
    private bool hasTriggered = false;

    void Update()
    {
        if (useDelayedTrigger && isInside && !hasTriggered)
        {
            timeInside += Time.deltaTime;
            
            if (timeInside >= requiredTime)
            {
                hasTriggered = true;
                OnProximityEnter?.Invoke();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        isInside = true;
        
        if (!useDelayedTrigger)
        {
            OnProximityEnter?.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        isInside = false;
        timeInside = 0f;
        hasTriggered = false;
        
        OnProximityExit?.Invoke();
    }
}
