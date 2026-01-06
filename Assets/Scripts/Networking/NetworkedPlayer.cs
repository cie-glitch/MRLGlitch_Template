using UnityEngine;
using Photon.Pun;
using Unity.XR.CoreUtils;

public class NetworkedPlayer : MonoBehaviourPun, IPunObservable
{
    [Header("Visual Representations (Remote Players Only)")]
    public GameObject remotePlayerVisuals;
    public GameObject remoteHeadVisual;
    public GameObject remoteLeftHandVisual;
    public GameObject remoteRightHandVisual;
    
    [Header("Network Settings")]
    public float lerpRate = 15f;
    
    // XR Rig references (found automatically)
    private Transform xrCamera;
    private Transform xrLeftController;
    private Transform xrRightController;
    
    // Network interpolation data (for remote players)
    private Vector3 networkHeadPos, networkLeftPos, networkRightPos;
    private Quaternion networkHeadRot, networkLeftRot, networkRightRot;
    
    void Start()
    {
        Debug.Log($"[VRPlayer] Initializing player - IsLocal: {photonView.IsMine}");
        
        if (photonView.IsMine)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
    }
    
    void SetupLocalPlayer()
    {
        Debug.Log("[VRPlayer] Setting up local player - finding XR components");
        
        // Find XR components automatically
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            xrCamera = xrOrigin.Camera.transform;
            
            // Try to find controllers
            Transform cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;
            xrLeftController = cameraOffset.Find("Left Controller");
            xrRightController = cameraOffset.Find("Right Controller");
            
            // Alternative: Find by component type
            if (xrLeftController == null || xrRightController == null)
            {
                var controllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRController>();
                foreach (var controller in controllers)
                {
                    if (controller.name.Contains("Left"))
                        xrLeftController = controller.transform;
                    else if (controller.name.Contains("Right"))
                        xrRightController = controller.transform;
                }
            }
        }
        
        if (xrCamera == null)
            Debug.LogWarning("[VRPlayer] Could not find XR Camera!");
        if (xrLeftController == null)
            Debug.LogWarning("[VRPlayer] Could not find Left Controller!");
        if (xrRightController == null)
            Debug.LogWarning("[VRPlayer] Could not find Right Controller!");
        
        Debug.Log("[VRPlayer] Local player setup complete");
    }
    
    void SetupRemotePlayer()
    {
        Debug.Log($"[VRPlayer] Setting up remote player for {photonView.Owner.NickName}");
        
        // Show remote visuals
        remotePlayerVisuals?.SetActive(true);
        
        Debug.Log("[VRPlayer] Remote player setup complete");
    }
    
    void Update()
    {
        // Only update visuals for remote players
        if (!photonView.IsMine)
        {
            if (remoteHeadVisual != null)
            {
                remoteHeadVisual.transform.position = Vector3.Lerp(remoteHeadVisual.transform.position, networkHeadPos, Time.deltaTime * lerpRate);
                remoteHeadVisual.transform.rotation = Quaternion.Lerp(remoteHeadVisual.transform.rotation, networkHeadRot, Time.deltaTime * lerpRate);
            }
            
            if (remoteLeftHandVisual != null)
            {
                remoteLeftHandVisual.transform.position = Vector3.Lerp(remoteLeftHandVisual.transform.position, networkLeftPos, Time.deltaTime * lerpRate);
                remoteLeftHandVisual.transform.rotation = Quaternion.Lerp(remoteLeftHandVisual.transform.rotation, networkLeftRot, Time.deltaTime * lerpRate);
            }
            
            if (remoteRightHandVisual != null)
            {
                remoteRightHandVisual.transform.position = Vector3.Lerp(remoteRightHandVisual.transform.position, networkRightPos, Time.deltaTime * lerpRate);
                remoteRightHandVisual.transform.rotation = Quaternion.Lerp(remoteRightHandVisual.transform.rotation, networkRightRot, Time.deltaTime * lerpRate);
            }
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send XR tracking data from local player
            if (xrCamera != null)
            {
                stream.SendNext(xrCamera.position);
                stream.SendNext(xrCamera.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            if (xrLeftController != null)
            {
                stream.SendNext(xrLeftController.position);
                stream.SendNext(xrLeftController.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
            
            if (xrRightController != null)
            {
                stream.SendNext(xrRightController.position);
                stream.SendNext(xrRightController.rotation);
            }
            else
            {
                stream.SendNext(Vector3.zero);
                stream.SendNext(Quaternion.identity);
            }
        }
        else
        {
            // Receive tracking data for remote players
            networkHeadPos = (Vector3)stream.ReceiveNext();
            networkHeadRot = (Quaternion)stream.ReceiveNext();
            networkLeftPos = (Vector3)stream.ReceiveNext();
            networkLeftRot = (Quaternion)stream.ReceiveNext();
            networkRightPos = (Vector3)stream.ReceiveNext();
            networkRightRot = (Quaternion)stream.ReceiveNext();
        }
    }
}