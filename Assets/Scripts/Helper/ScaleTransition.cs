using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ScaleTransition : MonoBehaviour
{
    [SerializeField] AnimationCurve scaleInCurve;
    [SerializeField] AnimationCurve scaleOutCurve;
    [SerializeField] float scaleInTime = 1f;
    [SerializeField] float scaleOutTime = 1f;
    [SerializeField] UnityEvent onScaleInComplete;
    [SerializeField] UnityEvent onScaleOutComplete;


    public void StartScaleIn()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ScaleIn());
    }

    public void StartScaleOut()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleOut());
    }

    IEnumerator ScaleIn()
    {
        float elapsedTime = 0f;
        Vector3 initialScale = transform.localScale;
        Vector3 targetScale = Vector3.one;

        while (elapsedTime < scaleInTime)
        {
            float t = elapsedTime / scaleInTime;
            float scaleValue = scaleInCurve.Evaluate(t);
            transform.localScale = Vector3.LerpUnclamped(initialScale, targetScale, scaleValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        onScaleInComplete?.Invoke();
    }

    IEnumerator ScaleOut()
    {
        float elapsedTime = 0f;
        Vector3 initialScale = transform.localScale;
        Vector3 targetScale = Vector3.zero;

        while (elapsedTime < scaleOutTime)
        {
            float t = elapsedTime / scaleOutTime;
            float scaleValue = scaleOutCurve.Evaluate(t);
            transform.localScale = Vector3.LerpUnclamped(initialScale, targetScale, scaleValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        gameObject.SetActive(false);
        onScaleOutComplete?.Invoke();
    }
}