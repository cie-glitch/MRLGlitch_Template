using UnityEngine;

public class GrowAnimatorSetter : MonoBehaviour
{
    public string growStateName = "Grow";

    public void ApplyNormalizedTime(float normalizedTime)
    {
        Animator[] animators = FindObjectsOfType<Animator>(true);


        foreach (var animator in animators)
        {
            Debug.Log($"Setting animator {animator.gameObject.name} to normalized time {normalizedTime}");

            bool wasActive = animator.gameObject.activeSelf;
            if (!wasActive)
                animator.gameObject.SetActive(true);

            
            animator.Play(growStateName, 0, normalizedTime);
            animator.Update(0f);

            if (!wasActive)
                animator.gameObject.SetActive(false);
        }

        WaterablePlant[] plants = GetComponentsInChildren<WaterablePlant>(true);
        foreach (var plant in plants)
        {
            plant.Reset();
        }

    }
}
