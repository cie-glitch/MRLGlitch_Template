using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 0.125f;
    [SerializeField] bool followPosition = true;
    [SerializeField] bool followRotation = false;   
    [SerializeField] bool fixYPosition = false;   



    void Update()
    {
        if (target == null) return;

        if (followPosition)
        {
            Vector3 desiredPosition = target.position;
            if (fixYPosition)
                desiredPosition.y = transform.position.y;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }

        if (followRotation)
        {
            Vector3 targetForward = target.forward;
            targetForward.y = 0;
            Quaternion desiredRotation = Quaternion.LookRotation(targetForward);
            Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
            transform.rotation = smoothedRotation;
        }
    }
}
