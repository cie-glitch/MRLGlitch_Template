using UnityEngine;

[ExecuteAlways]
public class SharedMaterialAnimationBridge : MonoBehaviour
{
    [SerializeField] private Material sharedMat;
    
    [Header("Animated Shader Properties")]
    [SerializeField, Range(0, 1)] private float alpha = 1f;
    [SerializeField, Range(0, 1)] private float secondaryAlpha = 1f;
    [SerializeField] private Vector4 downsample = new Vector4(100, 100, 100, 0);
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.01f, 0.01f);
    [SerializeField] private float waveFrequency = 1f;
    [SerializeField] private float waveSpeed = 1f;
    [SerializeField] private float waveAmount = 0f;
    
    // Shader property IDs (cached for performance)
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");
    private static readonly int SecondaryAlphaID = Shader.PropertyToID("_SecondaryAlpha");
    private static readonly int DownsampleID = Shader.PropertyToID("_Downsample");
    private static readonly int ScrollSpeedID = Shader.PropertyToID("_ScrollSpeed");
    private static readonly int WaveFrequencyID = Shader.PropertyToID("_WaveFrequency");
    private static readonly int WaveSpeedID = Shader.PropertyToID("_WaveSpeed");
    private static readonly int WaveAmountID = Shader.PropertyToID("_WaveAmount");
    
    
    void Update()
    {
        if (sharedMat != null)
        {
            ApplyPropertiesToSharedMaterial();
        }
    }
    
    private void ApplyPropertiesToSharedMaterial()
    {
        sharedMat.SetFloat(AlphaID, alpha);
        sharedMat.SetFloat(SecondaryAlphaID, secondaryAlpha);
        sharedMat.SetVector(DownsampleID, downsample);
        sharedMat.SetVector(ScrollSpeedID, new Vector4(scrollSpeed.x, scrollSpeed.y, 0, 0));
        sharedMat.SetFloat(WaveFrequencyID, waveFrequency);
        sharedMat.SetFloat(WaveSpeedID, waveSpeed);
        sharedMat.SetFloat(WaveAmountID, waveAmount);
    }
    
    // Public properties for animation (optional, if you prefer property animation)
    public float Alpha
    {
        get => alpha;
        set
        {
            alpha = Mathf.Clamp01(value);
            if (sharedMat != null)
                sharedMat.SetFloat(AlphaID, alpha);
        }
    }
    
    public float SecondaryAlpha
    {
        get => secondaryAlpha;
        set
        {
            secondaryAlpha = Mathf.Clamp01(value);
            if (sharedMat != null)
                sharedMat.SetFloat(SecondaryAlphaID, secondaryAlpha);
        }
    }
    
    public Vector4 Downsample
    {
        get => downsample;
        set
        {
            downsample = value;
            if (sharedMat != null)
                sharedMat.SetVector(DownsampleID, downsample);
        }
    }
    
    public Vector2 ScrollSpeed
    {
        get => scrollSpeed;
        set
        {
            scrollSpeed = value;
            if (sharedMat != null)
                sharedMat.SetVector(ScrollSpeedID, new Vector4(scrollSpeed.x, scrollSpeed.y, 0, 0));
        }
    }
    
    public float WaveFrequency
    {
        get => waveFrequency;
        set
        {
            waveFrequency = value;
            if (sharedMat != null)
                sharedMat.SetFloat(WaveFrequencyID, waveFrequency);
        }
    }
    
    public float WaveSpeed
    {
        get => waveSpeed;
        set
        {
            waveSpeed = value;
            if (sharedMat != null)
                sharedMat.SetFloat(WaveSpeedID, waveSpeed);
        }
    }
    
    public float WaveAmount
    {
        get => waveAmount;
        set
        {
            waveAmount = value;
            if (sharedMat != null)
                sharedMat.SetFloat(WaveAmountID, waveAmount);
        }
    }

}