using UnityEngine;
using UnityEngine.Events;

public class AudioEvent : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] UnityEvent onAudioPlayed;

    bool waitingForAudioToEnd = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayAudio()
    {
        audioSource.Play();
        waitingForAudioToEnd = true;
    }

    void Update()
    {
        if (waitingForAudioToEnd)
        {
            if (!audioSource.isPlaying)
            {
                onAudioPlayed.Invoke();
                waitingForAudioToEnd = false;
            }
        }
    }
}
