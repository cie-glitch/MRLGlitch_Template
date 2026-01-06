using UnityEngine;
using UnityEngine.Playables;

public class TimelinePauseController : MonoBehaviour
{
    public float fps = 60f; //Needs to be set accoridingly to the playable asset fps
    public PlayableDirector director;

    bool ready = false;

    void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
    }

    public void PauseTimeline()
    {
        if (ready == true)
            return;
        if (director != null)
            director.Pause();
    }

    public void ResumeTimeline()
    {
        if (director != null)
            director.Play();
    }

    public void SetReadyState(bool state)
    {
        ready = state;
        if (director != null)
            director.Play();
    }

    public void PlayFromFrameTwo()
    {
        
        if (director != null)
        {
            director.time = 2.0 / fps;
            director.Play();
        }
    }

    public void StopAndReset()
    {
        if (director != null)
        {
            director.Stop();
            director.time = 0;
            director.Evaluate();
        }
    }


}
