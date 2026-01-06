using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class TunnelVideo : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The video player to control")]
    public VideoPlayer videoPlayer;
    
    [Tooltip("The renderer with the material to fade")]
    public Renderer targetRenderer;
    
    [Tooltip("Material index to fade (if multiple materials)")]
    public int materialIndex = 0;
    
    [Header("Fade Settings")]
    [Tooltip("Duration of fade in seconds")]
    public float fadeDuration = 1f;
    
    [Tooltip("Start alpha value (transparent)")]
    public float minAlpha = 0f;
    
    [Tooltip("End alpha value (opaque)")]
    public float maxAlpha = 1f;
    
    private Material targetMaterial;
    private Coroutine fadeCoroutine;
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    
    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        
        if (targetRenderer != null && targetRenderer.materials.Length > materialIndex)
        {
            targetMaterial = targetRenderer.materials[materialIndex];
        }
        
        // Initialize with transparent state
        if (targetMaterial != null)
        {
            SetAlpha(minAlpha);
        }
    }
    
    public void FadeIn()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCoroutine(minAlpha, maxAlpha));
    }
    
    public void FadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCoroutine(maxAlpha, minAlpha));
    }
    
    public void ToggleFade()
    {
        if (targetMaterial == null)
            return;
        
        Color currentColor = targetMaterial.GetColor(BaseColorProperty);
        
        if (currentColor.a >= maxAlpha * 0.9f)
        {
            FadeOut();
        }
        else
        {
            FadeIn();
        }
    }
    
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha)
    {
        if (targetMaterial == null)
        {
            Debug.LogWarning("TunnelVideo: No material found to fade!");
            yield break;
        }
        
        float elapsedTime = 0f;
        bool isFadingIn = endAlpha > startAlpha;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            SetAlpha(alpha);
            
            // Start video when fully opaque
            if (isFadingIn && alpha >= maxAlpha && videoPlayer != null && !videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }
            
            yield return null;
        }
        
        // Ensure final alpha is set
        SetAlpha(endAlpha);
        
        // Start video if fading in and fully opaque
        if (isFadingIn && endAlpha >= maxAlpha && videoPlayer != null && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
        
        // Stop video if fading out and fully transparent
        if (!isFadingIn && endAlpha <= minAlpha && videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        fadeCoroutine = null;
    }
    
    private void SetAlpha(float alpha)
    {
        if (targetMaterial == null)
            return;
        
        Color color = targetMaterial.GetColor(BaseColorProperty);
        color.a = alpha;
        targetMaterial.SetColor(BaseColorProperty, color);
    }
}
