using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

public class PhotonSyncedEvent : MonoBehaviourPun
{

    [SerializeField] private GameObject performanceObject;
    [SerializeField] private UnityEvent onNetworkEvent;

    void Awake(){
        performanceObject.SetActive(false);
    }

    public void StartPlayback()
    {
        // If not connected or not in a room, invoke the event directly as a fallback
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.Log("[PhotonSyncedEvent] Not connected to Photon - invoking event directly");
            onNetworkEvent.Invoke();
            performanceObject.SetActive(true);
            return;
        }

        photonView.RPC(
            "RPC_RequestPlayback",
            RpcTarget.MasterClient
        );
    }

    [PunRPC]
    void RPC_RequestPlayback(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        double startTime = PhotonNetwork.Time + 0.2;

        photonView.RPC(
            "RPC_StartPlayback",
            RpcTarget.All,
            startTime
        );
    }

    [PunRPC]
    void RPC_StartPlayback(double networkStartTime)
    {
        StartCoroutine(PlayAtTime(networkStartTime));
    }

    System.Collections.IEnumerator PlayAtTime(double networkStartTime)
    {
        while (PhotonNetwork.Time < networkStartTime)
            yield return null;
        performanceObject.SetActive(true);
        onNetworkEvent.Invoke();
    }
}
