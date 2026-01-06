using UnityEngine;

public class WateringCan : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform targetTransform; // The transform to check angle (e.g., spout)
    [SerializeField] private ParticleSystem waterParticles;
    [SerializeField] private AudioSource waterAudioSource;
    
    [Header("Settings")]
    [SerializeField] private float minTiltAngle = 45f; // Minimum angle to start water flow
    [SerializeField] private float maxTiltAngle = 90f; // Angle for maximum water flow
    [SerializeField] private float minEmissionRate = 0f;
    [SerializeField] private float maxEmissionRate = 100f;
    [SerializeField] private float minStartSpeed = 0.5f;
    [SerializeField] private float maxStartSpeed = 5f;
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 1f;
    
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    
    void Start()
    {
        if (waterParticles != null)
        {
            emissionModule = waterParticles.emission;
            mainModule = waterParticles.main;
        }
        
        // If targetTransform is not set, use this transform
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
    }

    void Update()
    {
        if (targetTransform == null || waterParticles == null)
            return;
        
        // Calculate the angle between the target's forward vector and down
        Vector3 forward = targetTransform.forward;
        float angleToDown = Vector3.Angle(forward, Vector3.down);
        
        // Calculate emission rate based on angle
        // angleToDown = 0° when pointing straight down (max water)
        // angleToDown = 90° when pointing straight forward (start water)
        // angleToDown = 180° when pointing straight up (no water)
        
        float emissionRate = 0f;
        float t = 0f;
        
        if (angleToDown >= maxTiltAngle)
        {
            // Pointing forward or up - no water
            emissionRate = minEmissionRate;
        }
        else if (angleToDown <= minTiltAngle)
        {
            // Pointing down - maximum water flow
            emissionRate = maxEmissionRate;
            t = 1f;
        }
        else
        {
            // Interpolate between forward (90°) and down (45°)
            // Invert the interpolation so more tilt = more water
            t = (maxTiltAngle - angleToDown) / (maxTiltAngle - minTiltAngle);
            emissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, t);
        }
        
        // Set the emission rate
        emissionModule.rateOverTime = emissionRate;
        
        // Set the start speed based on the same interpolation factor
        if (angleToDown >= maxTiltAngle)
        {
            mainModule.startSpeed = minStartSpeed;
        }
        else if (angleToDown <= minTiltAngle)
        {
            mainModule.startSpeed = maxStartSpeed;
        }
        else
        {
            float speedValue = Mathf.Lerp(minStartSpeed, maxStartSpeed, t);
            mainModule.startSpeed = speedValue;
        }
        
        // Control audio volume based on the same interpolation
        if (waterAudioSource != null)
        {
            float volume = Mathf.Lerp(minVolume, maxVolume, t);
            waterAudioSource.volume = volume;
            
            // Start playing if water is flowing, stop if not
            if (emissionRate > minEmissionRate && !waterAudioSource.isPlaying)
            {
                waterAudioSource.Play();
            }
            else if (emissionRate <= minEmissionRate && waterAudioSource.isPlaying)
            {
                waterAudioSource.Stop();
            }
        }
    }
}
