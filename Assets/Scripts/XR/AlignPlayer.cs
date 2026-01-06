using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;

public class AlignPlayer : MonoBehaviour
{
    public static AlignPlayer Instance { get; private set; }

    [SerializeField]
    Transform player;
    
    [SerializeField]
    Transform playerHands;

    OVRSpatialAnchor m_CurrentAlignmentAnchor;
    Coroutine m_AlignCoroutine;
    
    // XR Input Subsystem for boundary change detection
    XRInputSubsystem m_XRInputSubsystem;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        // Subscribe to boundary change events
        SubscribeToBoundaryChanges();
    }

    void OnDestroy()
    {
        // Unsubscribe from boundary change events
        UnsubscribeFromBoundaryChanges();
    }

    public void SetAlignmentAnchor(OVRSpatialAnchor anchor)
    {
        if (m_AlignCoroutine != null)
        {
            StopCoroutine(m_AlignCoroutine);
            m_AlignCoroutine = null;
        }

        Debug.Log($"{nameof(AlignPlayer)}: setting {anchor} as the alignment anchor...");

        if (m_CurrentAlignmentAnchor)
        {
            Debug.Log($"{nameof(AlignPlayer)}: unset {m_CurrentAlignmentAnchor} as the alignment anchor.");
        }

        m_CurrentAlignmentAnchor = null;

        if (player)
        {
            player.SetPositionAndRotation(default, Quaternion.identity);
        }

        if (anchor == null || !player)
            return;

        m_AlignCoroutine = StartCoroutine(RealignRoutine(anchor));
    }

    IEnumerator RealignRoutine(OVRSpatialAnchor anchor)
    {
        yield return null;

        var anchorTransform = anchor.transform;

        player.position = anchorTransform.InverseTransformPoint(Vector3.zero);
        player.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);

        if (playerHands)
        {
            playerHands.SetLocalPositionAndRotation(
                -player.position,
                Quaternion.Inverse(player.rotation)
            );
        }

        m_CurrentAlignmentAnchor = anchor;
        
        StatusText.Instance?.Print($"Player aligned to anchor: {anchor.Uuid.ToString().Substring(0, 8)}...");
        Debug.Log($"{nameof(AlignPlayer)}: finished alignment -> {anchor}");
        m_AlignCoroutine = null;
    }

    void SubscribeToBoundaryChanges()
    {
        // Get the XR Input Subsystem
        var xrInputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(xrInputSubsystems);
        
        if (xrInputSubsystems.Count > 0)
        {
            m_XRInputSubsystem = xrInputSubsystems[0];
            m_XRInputSubsystem.boundaryChanged += OnBoundaryChanged;
            Debug.Log($"{nameof(AlignPlayer)}: Subscribed to XR boundary change events");
        }
        else
        {
            Debug.LogWarning($"{nameof(AlignPlayer)}: No XR Input Subsystem found for boundary monitoring");
        }
    }

    void UnsubscribeFromBoundaryChanges()
    {
        if (m_XRInputSubsystem != null)
        {
            m_XRInputSubsystem.boundaryChanged -= OnBoundaryChanged;
            m_XRInputSubsystem = null;
            Debug.Log($"{nameof(AlignPlayer)}: Unsubscribed from XR boundary change events");
        }
    }

    void OnBoundaryChanged(XRInputSubsystem subsystem)
    {
        // Only respond to boundary changes if we have an active alignment
        if (m_CurrentAlignmentAnchor == null)
            return;

        Debug.Log($"{nameof(AlignPlayer)}: XR Boundary changed detected - re-aligning to anchor");
        StatusText.Instance?.Print("Boundary changed - re-aligning...");
        
        // Re-align to the current anchor
        SetAlignmentAnchor(m_CurrentAlignmentAnchor);
    }

    /// <summary>
    /// Get the current alignment anchor
    /// </summary>
    /// <returns>The current OVRSpatialAnchor being used for alignment, or null if none</returns>
    public OVRSpatialAnchor GetCurrentAlignmentAnchor()
    {
        return m_CurrentAlignmentAnchor;
    }

    /// <summary>
    /// Check if the player is currently aligned to an anchor
    /// </summary>
    /// <returns>True if aligned to an anchor, false otherwise</returns>
    public bool IsAligned()
    {
        return m_CurrentAlignmentAnchor != null;
    }

    /// <summary>
    /// Reset player alignment and clear current anchor
    /// </summary>
    public void ClearAlignment()
    {
        SetAlignmentAnchor(null);
    }

    /// <summary>
    /// Force re-alignment to the current anchor (useful after boundary changes)
    /// </summary>
    public void ForceRealign()
    {
        if (m_CurrentAlignmentAnchor != null)
        {
            Debug.Log($"{nameof(AlignPlayer)}: Force re-aligning to current anchor");
            StatusText.Instance?.Print("Force re-aligning to anchor...");
            SetAlignmentAnchor(m_CurrentAlignmentAnchor);
        }
        else
        {
            Debug.LogWarning($"{nameof(AlignPlayer)}: Cannot force re-align - no current anchor");
        }
    }

    /// <summary>
    /// Check if the anchor is still properly localized and tracked
    /// </summary>
    /// <returns>True if the anchor is localized and being tracked</returns>
    public bool IsAnchorTracked()
    {
        return m_CurrentAlignmentAnchor != null && 
               m_CurrentAlignmentAnchor.Localized && 
               m_CurrentAlignmentAnchor.Created;
    }
}
