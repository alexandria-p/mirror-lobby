using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSpawnSystem : NetworkBehaviour
{

    [SerializeField] private Player playerPrefab = null;

    private static List<Transform> spawnPoints = new List<Transform>();

    private int nextIndex = 0;

    public static void AddSpawnPoint(Transform transform)
    {
        spawnPoints.Add(transform);

        spawnPoints = spawnPoints.OrderBy(x => x.GetSiblingIndex()).ToList();
    }
    public static void RemoveSpawnPoint(Transform transform) => spawnPoints.Remove(transform);

    public override void OnStartServer()
    {
        // subscribe for cleanup
        LobbyNetworkManager.OnServerStopped += CleanUpServer; // cleanup lobby when server is stopped, as OnDestroy cannot (see notes for OnDestroy)
        // events
        LobbyNetworkManager.OnServerReadied += SpawnPlayer; // when a user is 'readied', subscribe to spawn player
    }

    [ServerCallback] // warning: is not called when server is stopped (as server is not running to process callback)
    private void OnDestroy() => CleanUpServer(); // ondestroy called by monobehaviour objects when destroyed - this can be due to destruction of object in scene, or due to it being destroyed by changing scene
    
    [Server]
    private void CleanUpServer()
    {
        LobbyNetworkManager.OnServerStopped -= CleanUpServer;
        LobbyNetworkManager.OnServerReadied -= SpawnPlayer; // unsubscribe to spawn player
    }

    [Server]
    public void SpawnPlayer(NetworkConnection conn, GameObject roomPlayer)
    {
        Transform spawnPoint = spawnPoints.ElementAtOrDefault(nextIndex);

        if (spawnPoint == null)
        {
            Debug.LogError($"Missing spawn point for player {nextIndex}");
            return;
        }
        // Debug.Log("SPAWN PLAYER");
        // instantiate player instance
        var playerInstance = Instantiate(playerPrefab, spawnPoints[nextIndex].position, spawnPoints[nextIndex].rotation);   

        // pass player info
        // todo -  replace with try/catch?
        if (conn != null && conn.identity != null && roomPlayer != null)
        {
            // Debug.Log("identity is not null");
            var name = roomPlayer.GetComponent<LobbyPlayer>().DisplayName;
            playerInstance.GetComponent<Player>().SetDisplayName((name != null && name.Count() > 0) ? name : "Guest");
        }
        else {
            Debug.Log("spawn - identity is null");
        }
        
        // replace player for connection - replaces room player with game player  
        // Debug.Log("replace player for connection");
        NetworkServer.ReplacePlayerForConnection(conn, playerInstance.gameObject, true); //.Spawn(playerInstance.gameObject, conn);
        nextIndex++; // increments index
    }
}
