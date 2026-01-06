using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaterablePlant : MonoBehaviour
{
    [SerializeField] private int targetParticleCount = 100;
    [SerializeField] private UnityEvent onWatered;
    [SerializeField] private Renderer potRenderer;
    [SerializeField] private float wetMaterialSmoothness;
    [SerializeField] private Color wetMaterialColor;
    private float dryMaterialSmoothness;
    private Color dryMaterialColor;
    [SerializeField] private int materialIndex = 1;
    
    [Header("Drying Settings")]
    [SerializeField] private float dryingTime = 10f;
    [Tooltip("Event triggered when plant becomes completely dry again")]
    [SerializeField] private UnityEvent onDry;
    [SerializeField] private bool multipleWaterings = false;

    private bool isWatered = false;
    private float currentWetness = 0f;
    private int waterParticleCount = 0;

    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();


    Collider collider;

    void Start()
    {
        dryMaterialColor = potRenderer.materials[materialIndex].GetColor("_BaseColor");
        dryMaterialSmoothness = potRenderer.materials[materialIndex].GetFloat("_Smoothness");
        collider = GetComponent<Collider>();
    }
    
    void Update()
    {
        if (isWatered && currentWetness > 0f && multipleWaterings)
        {
            // Dry over time
            currentWetness -= Time.deltaTime / dryingTime;
            currentWetness = Mathf.Max(0f, currentWetness);
            
            SetMaterialWetnessFromValue(currentWetness);
            
            // Check if completely dry
            if (currentWetness <= 0f)
            {
                isWatered = false;
                onDry?.Invoke();
            }
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (isWatered)
            return;
        ParticleSystem ps = other.GetComponent<ParticleSystem>();
        if (ps == null)
            return;

        int numCollisionEvents = ps.GetCollisionEvents(gameObject, collisionEvents);

        waterParticleCount += numCollisionEvents;
        SetMaterialWetness();
        if (waterParticleCount >= targetParticleCount)
        {
            onWatered.Invoke();
            isWatered = true;
            currentWetness = 1f;
            waterParticleCount = 0;
        }
    }

    void SetMaterialWetness()
    {
        float amount = Mathf.Clamp01((float)waterParticleCount / targetParticleCount);
        SetMaterialWetnessFromValue(amount);
    }
    
    void SetMaterialWetnessFromValue(float amount)
    {
        float smoothness = Mathf.Lerp(dryMaterialSmoothness, wetMaterialSmoothness, amount);
        Color color = Color.Lerp(dryMaterialColor, wetMaterialColor, amount);
        potRenderer.materials[materialIndex].SetFloat("_Smoothness", smoothness);
        potRenderer.materials[materialIndex].SetColor("_BaseColor", color);
    }

    public void Reset(){
        isWatered = false;
        waterParticleCount = 0;
        currentWetness = 0f;
        SetMaterialWetnessFromValue(0f);
    }


}
