using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class PhotonSyncedEvent : MonoBehaviourPun
{
    [SerializeField] private PlayableDirector performanceTimeline;
    [SerializeField] private UnityEvent onNetworkEvent;
    [SerializeField] private UnityEvent onNetworkStop;

    void Awake()
    {
        performanceTimeline.gameObject.SetActive(false);
    }

    public void StartPlayback()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            onNetworkEvent.Invoke();
            performanceTimeline.gameObject.SetActive(true);
            performanceTimeline.time = 0;
            performanceTimeline.Evaluate();
            performanceTimeline.Play();
            return;
        }

        photonView.RPC(nameof(RPC_RequestPlayback), RpcTarget.MasterClient);
    }

    public void StopPlayback()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            performanceTimeline.Stop();
            performanceTimeline.gameObject.SetActive(false);
            onNetworkStop.Invoke();
            return;
        }

        photonView.RPC(nameof(RPC_RequestStop), RpcTarget.MasterClient);
    }

    [PunRPC]
    void RPC_RequestPlayback(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        double t = PhotonNetwork.Time + 0.2;
        photonView.RPC(nameof(RPC_StartPlayback), RpcTarget.All, t);
    }

    [PunRPC]
    void RPC_RequestStop(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        double t = PhotonNetwork.Time + 0.2;
        photonView.RPC(nameof(RPC_StopPlayback), RpcTarget.All, t);
    }

    [PunRPC]
    void RPC_StartPlayback(double networkStartTime)
    {
        StatusText.Instance?.Print("Synchronized start at " + networkStartTime.ToString("F3") + "s");
        StartCoroutine(PlayAtTime(networkStartTime));
    }

    [PunRPC]
    void RPC_StopPlayback(double networkStopTime)
    {
        StatusText.Instance?.Print("Synchronized stop at " + networkStopTime.ToString("F3") + "s");
        StartCoroutine(StopAtTime(networkStopTime));
    }

    System.Collections.IEnumerator PlayAtTime(double t)
    {
        while (PhotonNetwork.Time < t) yield return null;
        performanceTimeline.gameObject.SetActive(true);
        performanceTimeline.Play();
        onNetworkEvent.Invoke();
    }

    System.Collections.IEnumerator StopAtTime(double t)
    {
        while (PhotonNetwork.Time < t) yield return null;
        performanceTimeline.Stop();
        performanceTimeline.time = 0;
        performanceTimeline.gameObject.SetActive(false);
        onNetworkStop.Invoke();
    }
}