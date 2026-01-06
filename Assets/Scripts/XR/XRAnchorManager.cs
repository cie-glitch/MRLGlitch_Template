using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;

public class XRAnchorManager : MonoBehaviour
{
    public static XRAnchorManager Instance;

    public Transform anchorCursor;

    public float anchorFollowSpeed = 5f;

    OVRSpatialAnchor currentAnchor;
    
    [Header("Group Sharing")]
    public string groupUuid = ""; // Will be generated if empty

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Generate a group UUID if not set
        if (string.IsNullOrEmpty(groupUuid))
        {
            groupUuid = System.Guid.NewGuid().ToString();
            Debug.Log($"Generated new group UUID: {groupUuid}");
        }
    }

    public void CreateAnchorAtCursor()
    {
        StatusText.Instance?.Print("Creating anchor at cursor position...");
        StartCoroutine(CreateSpatialAnchorRoutine());
    }

    IEnumerator CreateSpatialAnchorRoutine()
    {
        var go = new GameObject();
        go.transform.position = anchorCursor.position;
        go.transform.rotation = anchorCursor.rotation;
        var anchor = go.AddComponent<OVRSpatialAnchor>();

        // Wait for the async creation
        yield return new WaitUntil(() => anchor.Created);

        Debug.Log($"Created anchor {anchor.Uuid}");
        StatusText.Instance?.Print($"Anchor created: {anchor.Uuid.ToString().Substring(0, 8)}...");
        
        // Don't align the player who created the anchor - they should stay in place
        // The anchor is created at their desired position and other players will align to it

        currentAnchor = anchor;

        SaveAnchors(new List<OVRSpatialAnchor> { currentAnchor });
        
        // Automatically share the anchor after creation
        yield return new WaitForSeconds(0.5f);
        ShareAnchor();
    }

    async void SaveAnchors(IEnumerable<OVRSpatialAnchor> anchors)
    {

        var result = await OVRSpatialAnchor.SaveAnchorsAsync(anchors);
        if (result.Success)
        {
            Debug.Log($"Anchors saved successfully.");
            StatusText.Instance?.Print("Anchor saved successfully");
            PlayerPrefs.SetString("AnchorUUID", currentAnchor.Uuid.ToString());
        }
        else
        {
            Debug.LogError($"Failed to save anchor(s) with error {result.Status}");
        }
    }

    public void ShareAnchor()
    {
        if (currentAnchor == null)
        {
            Debug.LogError("No anchor to share. Create an anchor first.");
            return;
        }

        if (!currentAnchor.Created)
        {
            Debug.LogError("Anchor is not created yet. Wait for anchor creation to complete.");
            return;
        }

        StartCoroutine(ShareAnchorRoutine());
    }

    IEnumerator ShareAnchorRoutine()
    {
        Debug.Log($"Starting to share anchor {currentAnchor.Uuid} with group {groupUuid}");

        // Ensure anchor is saved before sharing
        if (!PlayerPrefs.HasKey("AnchorUUID") || PlayerPrefs.GetString("AnchorUUID") != currentAnchor.Uuid.ToString())
        {
            Debug.Log("Anchor not saved yet. Saving before sharing...");
            var saveTask = OVRSpatialAnchor.SaveAnchorsAsync(new List<OVRSpatialAnchor> { currentAnchor });
            
            yield return new WaitUntil(() => saveTask.IsCompleted);
            
            try
            {
                var saveResult = saveTask.GetResult();
                if (!saveResult.Success)
                {
                    Debug.LogError($"Failed to save anchor before sharing: {saveResult.Status}");
                    yield break;
                }
                Debug.Log("Anchor saved successfully before sharing.");
                PlayerPrefs.SetString("AnchorUUID", currentAnchor.Uuid.ToString());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception during anchor save: {ex.Message}");
                yield break;
            }
        }

        // Parse group UUID
        if (!System.Guid.TryParse(groupUuid, out System.Guid groupGuid))
        {
            Debug.LogError($"Invalid group UUID format: {groupUuid}");
            yield break;
        }

        // Share the anchor with the group
        var anchorsToShare = new List<OVRSpatialAnchor> { currentAnchor };
        var shareTask = OVRSpatialAnchor.ShareAsync(anchorsToShare, groupGuid);

        yield return new WaitUntil(() => shareTask.IsCompleted);

        try
        {
            var shareResult = shareTask.GetResult();
            if (shareResult.Success)
            {
                Debug.Log($"Anchor shared successfully! Group UUID: {groupUuid}");
                StatusText.Instance?.Print("Anchor shared successfully!");
                
                // Store the group UUID for later retrieval
                PlayerPrefs.SetString("SharedGroupUUID", groupUuid);
                
                // Log sharing details
                Debug.Log($"Shared anchor UUID: {currentAnchor.Uuid}");
                Debug.Log($"Group UUID for sharing: {groupUuid}");
            }
            else
            {
                Debug.LogError($"Failed to share anchor: {shareResult.Status}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during anchor sharing: {ex.Message}");
        }
    }


    public void LoadAnchor()
    {
        string uuid = PlayerPrefs.GetString("AnchorUUID", string.Empty);
        if (string.IsNullOrEmpty(uuid))
        {
            Debug.Log("No anchor UUID found in PlayerPrefs.");
            return;
        }
        LoadAnchorsByUuid(uuid);
    }

    List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();

    async void LoadAnchorsByUuid(string uuid)
    {
        IEnumerable<Guid> uuids = new List<Guid> { Guid.Parse(uuid) };
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, _unboundAnchors);

        if (result.Success)
        {
            Debug.Log($"Anchors loaded successfully.");
            StatusText.Instance?.Print("Local anchors loaded");

            // Note result.Value is the same as _unboundAnchors passed to LoadUnboundAnchorsAsync
            foreach (var unboundAnchor in result.Value)
            {
                // Step 2: Localize and wait for completion
                StartCoroutine(LocalizeAnchorRoutine(unboundAnchor));
            }

        }
        else
        {
            Debug.LogError($"Load failed with error {result.Status}.");
        }
    }
    
    void Update()
    {
        if(currentAnchor == null)
            return;
        anchorCursor.position = UnityEngine.Vector3.Lerp(anchorCursor.position, currentAnchor.transform.position, Time.deltaTime * anchorFollowSpeed);
        Quaternion newRotation = UnityEngine.Quaternion.Slerp(anchorCursor.rotation, currentAnchor.transform.rotation, Time.deltaTime * anchorFollowSpeed);
        anchorCursor.rotation = GetRotationAroundY(newRotation);

    }

    Quaternion GetRotationAroundY(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0;
        return Quaternion.LookRotation(forward);
    }

    public IEnumerator LocalizeAnchorRoutine(OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        Debug.Log($"Starting localization for anchor {unboundAnchor.Uuid}");
        
        // Start the localization process
        var localizeTask = unboundAnchor.LocalizeAsync();
        
        // Wait for localization to complete
        yield return new WaitUntil(() => localizeTask.IsCompleted);
        
        // Check if localization was successful
        bool localizationSuccessful = false;
        try
        {
            localizationSuccessful = localizeTask.GetResult();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during localization: {ex.Message}");
            yield break;
        }
        
        if (localizationSuccessful)
        {
            Debug.Log($"Anchor {unboundAnchor.Uuid} localized successfully");
            StatusText.Instance?.Print($"Anchor localized: {unboundAnchor.Uuid.ToString().Substring(0, 8)}...");
            
            // Create a GameObject for the anchor
            var anchorObject = new GameObject($"LocalizedAnchor_{unboundAnchor.Uuid}");
            var spatialAnchor = anchorObject.AddComponent<OVRSpatialAnchor>();
            
            // Bind the unbound anchor to the spatial anchor component
            unboundAnchor.BindTo(spatialAnchor);
            
            // Wait for the anchor to be localized (positioned in world space)
            yield return new WaitUntil(() => spatialAnchor.Localized);
            
            Debug.Log($"Anchor {unboundAnchor.Uuid} is now localized at position: {spatialAnchor.transform.position}");
            
            // Check if we received this anchor from a network share
            string receivedGroupUUID = PlayerPrefs.GetString("ReceivedGroupUUID", "");
            bool isSharedAnchor = !string.IsNullOrEmpty(receivedGroupUUID);
            
            // Always position cursor at the anchor position to align the world
            // This aligns the world (child of anchorCursor) to the anchor position
            anchorCursor.position = spatialAnchor.transform.position;
            anchorCursor.rotation = GetRotationAroundY(spatialAnchor.transform.rotation);   
            
            if (isSharedAnchor)
            {
                Debug.Log("[XRAnchorManager] Loading shared anchor from another player - world aligned via cursor");
                StatusText.Instance?.Print("World aligned to shared anchor");
            }
            else
            {
                Debug.Log("[XRAnchorManager] Loading local anchor - cursor positioned");
            }
            
            // Store reference to current anchor
            currentAnchor = spatialAnchor;
            
          
            
            // Automatically share the loaded anchor if it's not from a shared group
            if (string.IsNullOrEmpty(receivedGroupUUID) || receivedGroupUUID != groupUuid)
            {
                Debug.Log("[XRAnchorManager] Loaded local anchor, automatically sharing it");
                yield return new WaitForSeconds(0.5f); // Small delay to ensure anchor is fully ready
                ShareAnchor();
            }
            else
            {
                Debug.Log("[XRAnchorManager] Loaded shared anchor from network, not re-sharing");
            }
        }
        else
        {
            Debug.LogError($"Failed to localize anchor {unboundAnchor.Uuid}");
        }
    }

    /// <summary>
    /// Check if there is a shared anchor ready for networking
    /// </summary>
    /// <returns>The group UUID if a shared anchor is available, empty string otherwise</returns>
    public string GetSharedAnchorGroupUUID()
    {
        // Check if we have a current anchor that was shared
        if (currentAnchor != null && currentAnchor.Created && !string.IsNullOrEmpty(groupUuid))
        {
            // Verify that the anchor was actually shared by checking PlayerPrefs
            string sharedGroupUUID = PlayerPrefs.GetString("SharedGroupUUID", "");
            if (!string.IsNullOrEmpty(sharedGroupUUID))
            {
                return sharedGroupUUID;
            }
        }
        return "";
    }

    /// <summary>
    /// Check if the XRAnchorManager has a shared anchor available
    /// </summary>
    /// <returns>True if a shared anchor is available</returns>
    public bool HasSharedAnchor()
    {
        return !string.IsNullOrEmpty(GetSharedAnchorGroupUUID());
    }

    /// <summary>
    /// Load a shared anchor received from another player via networking
    /// This will set the appropriate flags to ensure player alignment occurs
    /// </summary>
    /// <param name="groupUuidFromNetwork">The group UUID received from the network</param>
    public void LoadSharedAnchorFromNetwork(string groupUuidFromNetwork)
    {
        Debug.Log($"[XRAnchorManager] Loading shared anchor from network with group UUID: {groupUuidFromNetwork}");
        
        // Store that this anchor came from the network
        PlayerPrefs.SetString("ReceivedGroupUUID", groupUuidFromNetwork);
        PlayerPrefs.Save();
        
        // Update our group UUID to match the shared one
        groupUuid = groupUuidFromNetwork;
        
        // Now load the anchor normally - the localization logic will handle alignment
        LoadAnchor();
    }

    /// <summary>
    /// Remove all local anchors and clear all shared PlayerPrefs
    /// </summary>
    public void RemoveAllAnchors()
    {
        Debug.Log("[XRAnchorManager] Starting removal of all anchors and clearing PlayerPrefs...");
        StatusText.Instance?.Print("Removing all anchors...");
        StartCoroutine(RemoveAllAnchorsRoutine());
    }

    IEnumerator RemoveAllAnchorsRoutine()
    {
        // Clear current anchor reference
        if (currentAnchor != null)
        {
            Debug.Log($"[XRAnchorManager] Destroying current anchor GameObject: {currentAnchor.name}");
            Destroy(currentAnchor.gameObject);
            currentAnchor = null;
        }

        // Clear all anchor-related PlayerPrefs
        Debug.Log("[XRAnchorManager] Clearing all anchor-related PlayerPrefs...");
        PlayerPrefs.DeleteKey("AnchorUUID");
        PlayerPrefs.DeleteKey("SharedGroupUUID");
        PlayerPrefs.DeleteKey("ReceivedGroupUUID");
        PlayerPrefs.Save();

        // Load all saved anchors to erase them
        var allAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
        // Use empty GUID list to load all anchors
        var emptyGuids = new List<System.Guid>();
        var loadTask = OVRSpatialAnchor.LoadUnboundAnchorsAsync(emptyGuids, allAnchors);

        yield return new WaitUntil(() => loadTask.IsCompleted);

        var loadResult = loadTask.GetResult();
        if (loadResult.Success && allAnchors.Count > 0)
        {
            Debug.Log($"[XRAnchorManager] Found {allAnchors.Count} saved anchors to erase");

            // Bind and erase each anchor
            var anchorsToErase = new List<OVRSpatialAnchor>();
            var uuidsToErase = new List<System.Guid>();
            
            foreach (var unboundAnchor in allAnchors)
            {
                // Create temporary GameObject to bind the anchor for erasing
                var tempObject = new GameObject($"TempAnchor_{unboundAnchor.Uuid}");
                var spatialAnchor = tempObject.AddComponent<OVRSpatialAnchor>();
                unboundAnchor.BindTo(spatialAnchor);
                anchorsToErase.Add(spatialAnchor);
                uuidsToErase.Add(unboundAnchor.Uuid);
            }

            // Wait a bit for bindings to complete
            yield return new WaitForSeconds(1f);

            // Erase all anchors
            var eraseTask = OVRSpatialAnchor.EraseAnchorsAsync(anchorsToErase, uuidsToErase);
            yield return new WaitUntil(() => eraseTask.IsCompleted);

            var eraseResult = eraseTask.GetResult();
            if (eraseResult.Success)
            {
                Debug.Log($"[XRAnchorManager] Successfully erased {anchorsToErase.Count} anchors");
            }
            else
            {
                Debug.LogError($"[XRAnchorManager] Failed to erase anchors: {eraseResult.Status}");
            }

            // Clean up temporary GameObjects
            foreach (var anchor in anchorsToErase)
            {
                if (anchor != null && anchor.gameObject != null)
                {
                    Destroy(anchor.gameObject);
                }
            }
        }
        else
        {
            Debug.Log("[XRAnchorManager] No saved anchors found to erase");
        }

        // Generate a new group UUID for fresh start
        groupUuid = System.Guid.NewGuid().ToString();
        Debug.Log($"[XRAnchorManager] Generated new group UUID for fresh start: {groupUuid}");

        // Reset player alignment and world parent position
        if (AlignPlayer.Instance != null)
        {
            AlignPlayer.Instance.SetAlignmentAnchor(null);
        }

        Debug.Log("[XRAnchorManager] All anchors removed and PlayerPrefs cleared successfully");
        StatusText.Instance?.Print("All anchors removed successfully");
    }

}