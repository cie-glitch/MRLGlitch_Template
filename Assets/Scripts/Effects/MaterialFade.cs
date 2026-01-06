using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class MaterialFade : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The renderer with the material to fade")]
    public Renderer targetRenderer;
    
    [Tooltip("Material index to fade (if multiple materials)")]
    public int materialIndex = 0;
    
    [Header("Fade Settings")]
    [Tooltip("Duration of fade in seconds")]
    public float fadeDuration = 1f;
    
    [Tooltip("Minimum alpha value (transparent)")]
    public float minAlpha = 0f;
    
    [Tooltip("Maximum alpha value (opaque)")]
    public float maxAlpha = 1f;
    
    [Header("Events")]
    [Tooltip("Event triggered when fade in completes")]
    public UnityEvent onFadeInComplete;
    
    [Tooltip("Event triggered when fade out completes")]
    public UnityEvent onFadeOutComplete;
    
    private Material targetMaterial;
    private Coroutine fadeCoroutine;
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    
    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        
        if (targetRenderer != null && targetRenderer.materials.Length > materialIndex)
        {
            targetMaterial = targetRenderer.materials[materialIndex];

            Color color = targetMaterial.GetColor(BaseColorProperty);
            targetMaterial.SetColor(BaseColorProperty, new Color(color.r, color.g, color.b, 0));    
        }
    }
    
    public void FadeIn()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCoroutine(minAlpha, maxAlpha, true));
    }
    
    public void FadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCoroutine(maxAlpha, minAlpha, false));
    }
    
    public void FadeInWithDuration(float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCoroutine(minAlpha, maxAlpha, true, duration));
    }
    
    public void FadeOutWithDuration(float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeCoroutine(maxAlpha, minAlpha, false, duration));
    }
    
    public void Toggle()
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
    
    public void SetAlpha(float alpha)
    {
        if (targetMaterial == null)
            return;
        
        Color color = targetMaterial.GetColor(BaseColorProperty);
        color.a = Mathf.Clamp(alpha, 0f, 1f);
        targetMaterial.SetColor(BaseColorProperty, color);
    }
    
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, bool isFadeIn, float? customDuration = null)
    {
        if (targetMaterial == null)
        {
            Debug.LogWarning("MaterialFade: No material found to fade!");
            yield break;
        }
        
        float duration = customDuration ?? fadeDuration;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            Color color = targetMaterial.GetColor(BaseColorProperty);
            color.a = alpha;
            targetMaterial.SetColor(BaseColorProperty, color);
            
            yield return null;
        }
        
        // Ensure final alpha is set
        Color finalColor = targetMaterial.GetColor(BaseColorProperty);
        finalColor.a = endAlpha;
        targetMaterial.SetColor(BaseColorProperty, finalColor);
        
        // Trigger completion event
        if (isFadeIn)
        {
            onFadeInComplete?.Invoke();
        }
        else
        {
            onFadeOutComplete?.Invoke();
        }
        
        fadeCoroutine = null;
    }
    
    public bool IsFading()
    {
        return fadeCoroutine != null;
    }
    
    public float GetCurrentAlpha()
    {
        if (targetMaterial == null)
            return 0f;
        
        return targetMaterial.GetColor(BaseColorProperty).a;
    }
}
