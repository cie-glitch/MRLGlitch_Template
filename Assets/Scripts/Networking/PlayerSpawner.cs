using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviourPun
{
    [Header("Player Prefab")]
    public GameObject vrPlayerPrefab;
    
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public float spawnRadius = 2f;

    private bool hasSpawned = false;

    // Called by NetworkManager when ready to spawn
    public void SpawnPlayerNow()
    {
        if (hasSpawned)
        {
            Debug.Log("[PlayerSpawner] Player already spawned, skipping...");
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[PlayerSpawner] Not ready to spawn player yet");
            return;
        }

        Debug.Log("[PlayerSpawner] Spawning VR player...");
        
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;
        
        GameObject player = PhotonNetwork.Instantiate(
            vrPlayerPrefab.name, 
            spawnPosition, 
            spawnRotation
        );
        
        hasSpawned = true;
        Debug.Log($"[PlayerSpawner] Player spawned at {spawnPosition}");
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Length > 0)
        {
            // Distribute players evenly among spawn points
            int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            return spawnPoints[index].position;
        }
        
        // Create a circle of spawn positions based on player number
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        float angle = playerIndex * (360f / 10) * Mathf.Deg2Rad; // Using 10 as max players
        
        Vector3 spawnPos = new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            0,
            Mathf.Sin(angle) * spawnRadius
        );
        
        return spawnPos;
    }

    // Optional: Method to respawn player (for testing)
    public void RespawnPlayer()
    {
        hasSpawned = false;
        SpawnPlayerNow();
    }
}