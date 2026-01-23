using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class Networking : MonoBehaviourPun, IConnectionCallbacks, IMatchmakingCallbacks
{
    [Header("Connection Settings")]
    public string gameVersion = "1.0";
    public int maxPlayersPerRoom = 10;
    
    [Header("Room Settings")]
    public string roomName = "VRAnchorRoom";
    public int roomTimeoutTime =60 * 60 * 1000;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;

    [SerializeField] UnityEvent OnJoinedRoomEvent;
    
    
    private XRAnchorManager anchorManager;
    private bool hasSharedAnchor = false;
    private bool hasJoinedRoomBefore = false;
    private bool isReconnecting = false;
    private bool pendingRejoin = false;

    [Header("Reconnect")]
    [Tooltip("Initial delay between reconnect attempts (seconds).")]
    [SerializeField] private float reconnectInitialDelaySeconds = 2f;
    [Tooltip("Max delay between reconnect attempts (seconds).")]
    [SerializeField] private float reconnectMaxDelaySeconds = 30f;

    private int reconnectAttemptCount = 0;

    void Start()
    {
        // Get reference to XRAnchorManager
        anchorManager = XRAnchorManager.Instance;
        if (anchorManager == null)
        {
            Debug.LogError("[Networking] XRAnchorManager instance not found!");
            return;
        }

        if (enableDebugLogs)
        {
            PhotonNetwork.LogLevel = PunLogLevel.Full;
            Debug.Log("[Networking] Starting connection process...");
        }

        // Set a random nickname if none exists
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);
        }

        PhotonNetwork.GameVersion = gameVersion;
        
        // Connect to Photon lobby on startup
        ConnectToLobby();
    }

    void ConnectToLobby()
    {
        Debug.Log("[Networking] Connecting to Photon lobby...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Connect()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Networking] Not connected to Photon. Attempting to connect...");
            ConnectToLobby();
            return;
        }

        Debug.Log($"[Networking] Attempting to join room: {roomName}");
        PhotonNetwork.JoinRoom(roomName);
    }

    #region IConnectionCallbacks

    public void OnConnected()
    {
        Debug.Log("[Networking] Connected to Photon Server");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("[Networking] Connected to Master Server and Lobby");
        StatusText.Instance?.Print("Connected to Photon");

        if (pendingRejoin && hasJoinedRoomBefore && !PhotonNetwork.InRoom)
        {
            Debug.Log($"[Networking] Reconnected. Rejoining room: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            Debug.Log("[Networking] Ready to join rooms. Call Connect() to join the VR room.");
        }
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Networking] Disconnected: {cause}");
        StatusText.Instance?.Print($"Disconnected: {cause}");
        hasSharedAnchor = false;
        
        // If we've joined a room before, keep attempting automatic reconnection.
        // Note: OnDisconnected can fire multiple times during reconnect cycles; don't stop after the first attempt.
        if (hasJoinedRoomBefore)
        {
            isReconnecting = true;
            pendingRejoin = true;
            reconnectAttemptCount++;

            float delaySeconds = Mathf.Min(
                reconnectInitialDelaySeconds * Mathf.Pow(2f, reconnectAttemptCount - 1),
                reconnectMaxDelaySeconds
            );

            Debug.Log($"[Networking] Connection lost. Reconnect attempt {reconnectAttemptCount} in {delaySeconds:0.0}s...");
            StatusText.Instance?.Print($"Reconnecting... ({reconnectAttemptCount})");

            CancelInvoke(nameof(AttemptReconnection));
            Invoke(nameof(AttemptReconnection), delaySeconds);
        }
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log("[Networking] Region list received");
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("[Networking] Custom authentication response received");
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.LogWarning($"[Networking] Custom authentication failed: {debugMessage}");
    }

    #endregion

    #region IMatchmakingCallbacks

    public void OnJoinedRoom()
    {
        Debug.Log($"[Networking] Successfully joined room: {PhotonNetwork.CurrentRoom.Name}");
        StatusText.Instance?.Print($"Joined room: {PhotonNetwork.CurrentRoom.PlayerCount} players");
        Debug.Log($"[Networking] Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        
        // Mark that we've successfully joined a room
        hasJoinedRoomBefore = true;
        isReconnecting = false;
        pendingRejoin = false;
        reconnectAttemptCount = 0;

        // List all players currently in the room
        foreach (var player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"[Networking] Player in room: {player.NickName} (ID: {player.ActorNumber})");
        }

        // Always check if we have an anchor to share, regardless of room state
        CheckAndBroadcastAnchor();

        // If we don't have an anchor and we're not the master client, request one
        if (!anchorManager.HasSharedAnchor() && !PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[Networking] No local anchor found - requesting anchor from other players");
            photonView.RPC("RequestAnchor", RpcTarget.Others);
        }
        OnJoinedRoomEvent?.Invoke();
    }

    public void OnLeftRoom()
    {
        Debug.Log("[Networking] Left room");
        hasSharedAnchor = false;
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[Networking] Room doesn't exist yet. Creating room: {roomName}");
        
        // Room doesn't exist, so create it
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            PlayerTtl = roomTimeoutTime,
            EmptyRoomTtl = 0,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[Networking] Failed to create room: {message}");
        
        // If creation failed, try joining again (maybe someone else created it)
        Invoke(nameof(RetryJoinRoom), 2f);
    }

    public void OnCreatedRoom()
    {
        Debug.Log($"[Networking] Successfully created room: {PhotonNetwork.CurrentRoom.Name}");
        StatusText.Instance?.Print("Created new room");
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Networking] New player joined: {newPlayer.NickName} (ID: {newPlayer.ActorNumber})");
        StatusText.Instance?.Print($"Player joined: {newPlayer.NickName}");
        Debug.Log($"[Networking] Total players now: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // Send our anchor to the new player if we have one
        if (hasSharedAnchor)
        {
            BroadcastAnchorToPlayer(newPlayer);
        }
        
        // Request anchor from the new player in case they have a newer one
        photonView.RPC("RequestAnchor", newPlayer);
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Networking] Player left: {otherPlayer.NickName} (ID: {otherPlayer.ActorNumber})");
        StatusText.Instance?.Print($"Player left: {otherPlayer.NickName}");
        Debug.Log($"[Networking] Remaining players: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Networking] Join random failed: {message}");
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        Debug.Log("[Networking] Friend list updated");
    }

    public void OnJoinedLobby()
    {
        Debug.Log("[Networking] Joined lobby");
    }

    public void OnLeftLobby()
    {
        Debug.Log("[Networking] Left lobby");
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[Networking] Room list updated - {roomList.Count} rooms available");
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        Debug.Log("[Networking] Lobby statistics updated");
    }

    #endregion

    #region Anchor Sharing

    void BroadcastAnchorToAll(string groupUUID)
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"[Networking] Broadcasting anchor group UUID to all players: {groupUUID}");
            StatusText.Instance?.Print("Broadcasting anchor to players");
            photonView.RPC("ReceiveAnchor", RpcTarget.Others, groupUUID);
            hasSharedAnchor = true;
        }
    }

    void CheckAndBroadcastAnchor()
    {
        if (anchorManager == null) return;

        // Check if we have a shared anchor using the anchor manager
        string groupUUID = anchorManager.GetSharedAnchorGroupUUID();
        if (!string.IsNullOrEmpty(groupUUID))
        {
            Debug.Log($"[Networking] Broadcasting existing shared anchor with group UUID: {groupUUID}");
            BroadcastAnchorToAll(groupUUID);
        }
        else
        {
            Debug.Log("[Networking] No shared anchor found locally.");
        }
    }

    void BroadcastAnchorToPlayer(Player player)
    {
        string groupUUID = anchorManager.GetSharedAnchorGroupUUID();
        if (!string.IsNullOrEmpty(groupUUID))
        {
            Debug.Log($"[Networking] Sending anchor group UUID to new player {player.NickName}: {groupUUID}");
            photonView.RPC("ReceiveAnchor", player, groupUUID);
        }
    }

    [PunRPC]
    void RequestAnchor()
    {
        // Any client can respond with their anchor if they have one
        string groupUUID = anchorManager.GetSharedAnchorGroupUUID();
        if (!string.IsNullOrEmpty(groupUUID))
        {
            Debug.Log($"[Networking] Received anchor request - sending group UUID: {groupUUID}");
            photonView.RPC("ReceiveAnchor", RpcTarget.Others, groupUUID);
        }
        else
        {
            Debug.Log("[Networking] Anchor request received but no shared anchor available");
        }
    }

    [PunRPC]
    void ReceiveAnchor(string groupUUID)
    {
        Debug.Log($"[Networking] Received anchor group UUID: {groupUUID}");
        StatusText.Instance?.Print("Received shared anchor");
        
        // Check if we already have this anchor
        string currentGroupUUID = anchorManager.GetSharedAnchorGroupUUID();
        if (currentGroupUUID == groupUUID)
        {
            Debug.Log("[Networking] Already have this anchor, ignoring duplicate");
            return;
        }
        
        // Always accept new anchors (prioritize newer ones)
        if (!string.IsNullOrEmpty(currentGroupUUID))
        {
            Debug.Log($"[Networking] Replacing existing anchor {currentGroupUUID} with new anchor {groupUUID}");
        }
        
        if (anchorManager != null)
        {
            // Set the group UUID in the anchor manager
            anchorManager.groupUuid = groupUUID;
            
            // Load the shared anchor using the same method as loading saved anchors
            LoadSharedAnchor(groupUUID);
        }
    }

    void LoadSharedAnchor(string groupUUID)
    {
        Debug.Log($"[Networking] Loading shared anchor with group UUID: {groupUUID}");
        
        if (anchorManager != null)
        {
            // Store the group UUID for future use
            PlayerPrefs.SetString("ReceivedGroupUUID", groupUUID);
            
            // Use the anchor manager to load shared anchors
            // This will use the same localization process as loading saved anchors
            StartCoroutine(LoadSharedAnchorRoutine(groupUUID));
        }
    }

    System.Collections.IEnumerator LoadSharedAnchorRoutine(string groupUUID)
    {
        // Parse the group UUID
        if (!System.Guid.TryParse(groupUUID, out System.Guid groupGuid))
        {
            Debug.LogError($"[Networking] Invalid group UUID format: {groupUUID}");
            yield break;
        }

        Debug.Log($"[Networking] Loading unbound shared anchors for group: {groupUUID}");

        // Load unbound shared anchors using the group UUID
        var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
        var loadTask = OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupGuid, unboundAnchors);

        yield return new WaitUntil(() => loadTask.IsCompleted);

        try
        {
            var result = loadTask.GetResult();
            if (result.Success)
            {
                Debug.Log($"[Networking] Successfully loaded {unboundAnchors.Count} shared anchors");
                StatusText.Instance?.Print($"Loaded {unboundAnchors.Count} shared anchors");
                
                // Process each shared anchor (similar to XRAnchorManager.LoadAnchorsByUuid)
                foreach (var unboundAnchor in unboundAnchors)
                {
                    StartCoroutine(anchorManager.LocalizeAnchorRoutine(unboundAnchor));
                }
            }
            else
            {
                Debug.LogError($"[Networking] Failed to load shared anchors: {result.Status}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Networking] Exception loading shared anchors: {ex.Message}");
        }
    }

    #endregion

    #region Utility Methods

    void AttemptReconnection()
    {
        if (!isReconnecting)
        {
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[Networking] Attempting to reconnect to Photon...");
            ConnectToLobby();
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            Debug.Log("[Networking] Connected. Attempting to rejoin room...");
            PhotonNetwork.JoinRoom(roomName);
            return;
        }

        // If we're connected but not ready yet, or already joining, retry shortly.
        CancelInvoke(nameof(AttemptReconnection));
        Invoke(nameof(AttemptReconnection), 1f);
    }

    void RetryJoinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            Debug.Log("[Networking] Retrying to join room...");
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    // Public method to manually disconnect (useful for testing)
    public void Disconnect()
    {
        Debug.Log("[Networking] Manually disconnecting...");
        isReconnecting = false;
        pendingRejoin = false;
        reconnectAttemptCount = 0;
        CancelInvoke(nameof(AttemptReconnection));
        PhotonNetwork.Disconnect();
    }

    // Public method to load local anchor and share it automatically
    public void LoadLocalAnchorAndShare()
    {
        if (anchorManager != null)
        {
            Debug.Log("[Networking] Loading local anchor and preparing to share...");
            StartCoroutine(LoadLocalAnchorAndShareRoutine());
        }
        else
        {
            Debug.LogError("[Networking] XRAnchorManager not found!");
        }
    }

    System.Collections.IEnumerator LoadLocalAnchorAndShareRoutine()
    {
        // Check if we already have a shared anchor
        if (anchorManager.HasSharedAnchor())
        {
            Debug.Log("[Networking] Already have a shared anchor, broadcasting it");
            CheckAndBroadcastAnchor();
            yield break;
        }

        // Load the local anchor using XRAnchorManager
        anchorManager.LoadAnchor();
        
        // Wait for the anchor to be loaded and potentially shared
        float timeoutTime = 10f; // 10 second timeout
        float elapsedTime = 0f;
        
        while (!anchorManager.HasSharedAnchor() && elapsedTime < timeoutTime)
        {
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }
        
        if (anchorManager.HasSharedAnchor())
        {
            Debug.Log("[Networking] Local anchor loaded successfully, now sharing with network");
            
            // Small delay to ensure anchor is fully loaded and shared
            yield return new WaitForSeconds(1f);
            
            // Broadcast the anchor to all players in the room
            CheckAndBroadcastAnchor();
        }
        else
        {
            Debug.LogWarning("[Networking] Failed to load local anchor within timeout period");
        }
    }

    // Public method to manually share current anchor (now handled automatically by XRAnchorManager)
    public void ShareCurrentAnchor()
    {
        if (anchorManager != null)
        {
            Debug.Log("[Networking] Starting anchor sharing process...");
            anchorManager.ShareAnchor();
            
            // Start monitoring for sharing completion
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                StartCoroutine(MonitorAnchorSharingCompletion());
            }
        }
    }

    System.Collections.IEnumerator MonitorAnchorSharingCompletion()
    {
        string initialGroupUUID = anchorManager.GetSharedAnchorGroupUUID();
        
        // Wait for the SharedGroupUUID to be set or changed (indicating sharing completed)
        while (anchorManager.GetSharedAnchorGroupUUID() == initialGroupUUID)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Sharing completed, broadcast to room
        string newGroupUUID = anchorManager.GetSharedAnchorGroupUUID();
        if (!string.IsNullOrEmpty(newGroupUUID))
        {
            Debug.Log($"[Networking] Anchor sharing completed, broadcasting to room: {newGroupUUID}");
            BroadcastAnchorToAll(newGroupUUID);
        }
    }

    #endregion

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
