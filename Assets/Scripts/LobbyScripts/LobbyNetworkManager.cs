using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using System.ComponentModel;
using UnityEngine.Serialization;

/*
	Documentation: https://mirror-networking.com/docs/Components/NetworkManager.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public enum DisconnectType: int {

    [Description("Server has been shut down")] 
    ServerTerminatedSession = 0,

    [Description("Connection to server was lost")] 
    ServerConnectionLost, // client unexpectedly lost connection

    [Description("Server refused the connection - the room is already full")] 
    ServerRefusedTooManyPlayers,

    [Description("Server refused the connection - game is in progress")] 
    ServerRefusedMidgame,
    
    [Description("Client has been voluntarily disconnected")] 
    ClientVoluntaryDisconnect,
}

public struct LobbyPlayerStruct {
    public LobbyPlayer player {get; set;} // todo -  security around SET
}

public struct GamePlayerStruct {
    public Player player {get; set;} // todo -  security around SET
}

public class LobbyNetworkManager : NetworkManager
{
    [SerializeField] private int minPlayers = 2;
    // reference to scene
    [Scene] [SerializeField] public string lobbyScene = string.Empty; // drag in the scene in the inspector for this variable
    [Scene] [SerializeField] public string offlineMenuScene = string.Empty; // drag in the scene in the inspector for this variable
    
    [Header("Room")] // property attribute, adds a header in the Unity Inspector above its fields
    [SerializeField] private LobbyPlayer roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private Player gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;
    
    public List<GameObject> nonNetworkedSpawnablePrefabs = new List<GameObject>();
    
// NOT A DEEP LINK - if player properties change or are added/leave, they need to be re-set into this (sync)list
public List<LobbyPlayer> RoomPlayers { get; } = new List<LobbyPlayer>();
public List<Player> GamePlayers { get; } = new List<Player>();
//public SyncList<LobbyPlayerStruct> RoomPlayers = new SyncList<LobbyPlayerStruct>();
//public SyncList<GamePlayerStruct> GamePlayers = new SyncList<GamePlayerStruct>();

    public static event Action OnClientConnected; // can be listened in on by other classes as they are public (ie by menuUi)
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnection, GameObject> OnServerReadied; // need to know on the server when someone has readied - also needs a timeout incase someone disconnects while joining game
    public static event Action OnServerStopped; // need to know on the server when a player has stopped or gone back to menu - cleanup listeners + trigger roundsystem to check state of game and if round is now over

    public static event Action<NetworkConnection> OnGamePlayerAdded;
    public static event Action<NetworkConnection> OnGamePlayerRemoved;
    public static event Action OnRoomPlayerAdded;
    public static event Action OnRoomPlayerRemoved;

    private AlertManager alertManager;
    private AlertManager AlertManager {
        get{
            if (alertManager != null) { return alertManager; }
            var alertManagers = FindObjectsOfType<AlertManager>();
            if (alertManagers.Length == 0)
            {
                Debug.Log("no current alertManager");
                return null; //todo - catch this safely
            }
            else {
            return alertManagers.First();
            }
        }
    }

    public void AddLobbyPlayer(LobbyPlayer player) {
        // Debug.Log(GamePlayers.Count());
        RoomPlayers.Add(player);
        Debug.Log(RoomPlayers.Count());
        OnRoomPlayerAdded?.Invoke(); // adds a new lobby player
    }

    public void AddGamePlayer(Player player) {
        // Debug.Log("create game player");
        GamePlayers.Add(player);
       //  Debug.Log(GamePlayers.Count());
        // Debug.Log(RoomPlayers.Count());
        OnGamePlayerAdded?.Invoke(player.connectionToClient); 
    }

// should be called when player leaves midway through game
    public void RemoveGamePlayer(Player player) {
        // Debug.Log("removing gameplayer");
        CleanupGamePlayer(player);
    }

// should be called by itself when game ends and players return to lobby
// serverside
    public void CleanupGamePlayer(Player player) {
        if (GamePlayers.Any(_ => _ == player)) {
            // Debug.Log("cleanup gameplayer");
            GamePlayers.Remove(player);
            OnGamePlayerRemoved?.Invoke(player.connectionToClient);
        }
    }

    [Scene] [SerializeField] private string gameScene = string.Empty; // drag in the scene in the inspector for this variable

    public void NotifyPlayersOfReadyState() {
        foreach (var player in RoomPlayers) {
            player.HandleReadyToStart(IsReadyToStart());
        }    
    }

    public bool IsReadyToStart() {
        if (numPlayers < minPlayers || RoomPlayers.Any(_ => _.InGame)) 
            return false;

        foreach (var player in RoomPlayers)
        {
            if (!player.IsReady)
                return false;
        }

        return true;
    }

    public void StartGame() {
         if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(lobbyScene)) 
        {
            if(!IsReadyToStart())
                return;

            ServerChangeScene(gameScene);
        }
    }

/* OVERRIDES*/

    /// <summary>
    /// This is invoked when a server is started - including when a host is started.
    /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    // PROGRAMMATICALLY, for all prefabs in this location. It is done this way instead of using 'Registered Spawnable Prefabs' in the Unity editor for NetworkManager.
    // if you want to be able to spawn a prefab programmatically, it must be registered here.
    public override void OnStartServer() 
    { 
        spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();
    }

    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient()
    {
        // Debug.Log("onStartClient networkmanager");

        nonNetworkedSpawnablePrefabs = Resources.LoadAll<GameObject>("NonNetworkedSpawnablePrefabs").ToList();

        var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs"); // a folder containing all prefabs that will be spawned over the network

        foreach (var prefab in spawnablePrefabs)
        {
            ClientScene.RegisterPrefab(prefab);
        }
    }

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientConnect(NetworkConnection conn)
    {
       base.OnClientConnect(conn);

       OnClientConnected?.Invoke();
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        // Clientside - Called when server goes down, but not when client chooses to leave and runs StopClient()
        OnClientDisconnected?.Invoke();        
        Debug.Log("Hey <client>, you've been disconnected!");
        AlertManager?.ShowDisconnectMessage((int)DisconnectType.ServerConnectionLost);
    }
   
    /// <summary>
    /// Called on the server when a new client connects.
    /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerConnect(NetworkConnection conn) 
    {
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            AlertManager?.ShowDisconnectMessage((int)DisconnectType.ServerRefusedTooManyPlayers);
            return;
        }
        // TODO: stops players joining game when game is in progress (ie - the menuscene is not the active scene)
        if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(gameScene)) //  lobby
        {
            // Debug.Log("disconnect cause game is in progress");
            conn.Disconnect();
            AlertManager?.ShowDisconnectMessage((int)DisconnectType.ServerRefusedMidgame);
            return;
        }

        base.OnServerConnect(conn);
    }



// should be called when a player disconnects, drops out or quits while in game or lobby
    public void PlayerHasDisconnected(NetworkConnection conn) {
       //  Debug.Log("Player disconnected");
        // there should be no GamePlayers if in lobby (this should only fire if ingame)
        if (GamePlayers.Any(_ => _.connectionToClient == conn)) {
           //  Debug.Log("remove gameplayer");
            RemoveGamePlayer(GamePlayers.First(_ => _.connectionToClient == conn)); 
        }
        try {
            // Debug.Log("remove lobby player"); 
            RoomPlayers.Remove(RoomPlayers.First(_ => _.connectionToClient == conn));
        }
        catch {
            Debug.LogWarning("error removing lobby player");
        }
        if (!RoomPlayers.Any(_ => _.IsLeader == true) && RoomPlayers.Count() >= 1) {
            // player already removed - make next player leader
            Debug.Log("pass host");
            RoomPlayers.First().IsLeader = true; // todo check - THIS MUST SYNC for all players (especially the player who is the leader)
        }
        Debug.Log(RoomPlayers.Count());
        NotifyPlayersOfReadyState(); // refreshes display for all players
        OnRoomPlayerRemoved?.Invoke();
    }


    // called on server when a client disconnects
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn.identity != null) {
            var roomPlayer = conn.identity.GetComponent<LobbyPlayer>();

            // if client has disconnected midgame:
            if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(gameScene)) 
            {
                // Debug.Log("client or host disconnected while game is in progress"); 
                PlayerHasDisconnected(conn); // also handles if gameplayer exists
                base.OnServerDisconnect(conn);    // run cleanup after letting PlayerHasDisconnected handle what it can
                conn.Disconnect();
                return;
            }
            else if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(lobbyScene)) 
            {
                // Debug.Log("client or host disconnected while players were in lobby");
                PlayerHasDisconnected(conn);
                base.OnServerDisconnect(conn);    // run cleanup after letting PlayerHasDisconnected handle what it can
                conn.Disconnect();
                return;
            }
        }
    }

// calls stopclient/stophost accordingly when player leaves
    public void ReturnToMainMenu(NetworkConnection conn) {
        // if host, StopHost else StopClient
        if (conn?.identity?.isServer == true) {
            // Debug.Log("is host");
            StopHost();
        }
        else {
           //  Debug.Log("is client");
            StopClient();
        }
        
        NetworkManager.singleton.StopServer();
    }

    public override void OnStopClient() {
        // Debug.Log("client or host has voluntarily or programmatically called StopClient");
        RoomPlayers.Clear();
        GamePlayers.Clear();

        var dt = DisconnectType.ClientVoluntaryDisconnect;
        var dtint = (int)dt;
        AlertManager?.ShowDisconnectMessage(dtint);
        base.OnStopClient();    
        // returns self to MainMenu because that is the default offline scene
    }

// called when server/host disconnects
// OnServerDisconnect -> Player.OnStopClient -> OnStopClient -> OnStopServer
    public override void OnStopServer()
    {
        Debug.Log("server or host has disconnected"); 
        RoomPlayers.Clear();
        GamePlayers.Clear();

        base.OnStopServer();   
        OnServerStopped?.Invoke(); 

        AlertManager?.ShowDisconnectMessage((int)DisconnectType.ServerTerminatedSession);
        // returns self to MainMenu because that is the default offline scene
    }

    /// <summary>
    /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    // 11.01.21 - called automatically after OnStartClient when 'Auto Create Player' is ticked in the Unity Inspector pane
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        // Debug.Log("server auto add player");
        // IF lobby, AND player does not already exist with DO NOT DESTROY, create player
        if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(lobbyScene)) // menuscene
        {
            if (!RoomPlayers.Any(_ => _.connectionToClient == conn)) {
                bool isLeader = RoomPlayers.Count == 0; // given player is the first to join lobby, make them leader
                LobbyPlayer roomPlayerInstance = Instantiate(roomPlayerPrefab); // instantiates lobby player object
                roomPlayerInstance.IsLeader = isLeader; // means 'start game' button is shown to leader. Server will still validate if they are in fact the leader when they make the call.
                NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject); // tying together the player's connection (conn) and player gameobject
                // Debug.Log("server add lobby player for connection");
            }
            else {
                Debug.Log("no need to auto create player in lobby as one already exists for this user");
            }
        }

        
    }

    public override void ServerChangeScene(string newSceneName)
    {
        // Debug.Log("changing scenes");

        // From menu to game
        if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(lobbyScene) 
            && newSceneName.StartsWith(gameScene)) // todo - change from 'startswith' ?
        {
            //Debug.Log("we are changing to gamescene");
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                RoomPlayers[i].InGame = true;
                RoomPlayers[i].RpcHideLobbyUI(); // must instruct client to hide their own UI (rpc)
            }
            
        }

     // From game to menu
        if (newSceneName.StartsWith(lobbyScene)) // todo - change from 'startswith' ?
        {
            Debug.Log("we are changing to menuscene");
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                if (RoomPlayers[i] == null)
                    continue;

                var conn = RoomPlayers[i].connectionToClient;
                NetworkIdentity identity = conn.identity;
                
                // need to make lobbyPlayer the localplayer again
                if (NetworkServer.active)
                {
                   //  Debug.Log("replace player for connection here -> localplayer flag becomes true on LobbyPlayer object, instead of GamePlayer object");

                    NetworkServer.ReplacePlayerForConnection(identity.connectionToClient, RoomPlayers[i].gameObject, true); // an override to keep authority (true)

                     // re-add the lobby UI (needs authority)
                    RoomPlayers[i].InGame = false;
                    RoomPlayers[i].IsReady = false;     
                    RoomPlayers[i].RpcShowLobbyUI();
                }
            }
            
        }
        base.ServerChangeScene(newSceneName);
    }

// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene()
    public override void OnServerSceneChanged(string sceneName) {

        // spawn players using spawn system if gamescene
        if (sceneName.StartsWith(gameScene))
        {
            // Debug.Log("changed to gamescene");
            GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
            NetworkServer.Spawn(playerSpawnSystemInstance); // server has ownership (as we are not passing in player conn)

            // todo - start timeout check, to see if we lose anyone while swapping scenes.

        }
    }

// 16.01.21 - experimental
// Cleansup clientside (as we are not using syncvar for game/roomplayer lists in network manager)
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) { 
    if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(gameScene) 
            && newSceneName.StartsWith(lobbyScene)) // todo - change from 'startswith' ?
        {
            // Debug.Log("we are changing to lobbyscene from game");
            // cleanup gameplayers on clientside, as gameplayers arent being destroyed clientside by change in scenes
            for (int i = GamePlayers.Count - 1; i >= 0; i--)
            {
                if (GamePlayers[i] == null)
                    continue;
                    
                CleanupGamePlayer(GamePlayers[i]);
            }
        }
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }


    public override void OnServerReady(NetworkConnection conn)
    {
        //Debug.Log("NetworkRoomManager OnServerReady");
        base.OnServerReady(conn);

        if (conn != null && conn.identity != null)
        {
            //Debug.Log("conn is not null");
            GameObject roomPlayer = conn.identity.gameObject;

            // if null or not a room player, dont replace it
            if (roomPlayer != null && roomPlayer.GetComponent<LobbyPlayer>() != null) {
                //Debug.Log("roomplayer is not null, invoke + spawn");
                SceneLoadedForPlayer(conn, roomPlayer);
            }
        }
        

    }

    void SceneLoadedForPlayer(NetworkConnection conn, GameObject roomPlayer)
    {
        if (LogFilter.Debug) Debug.LogFormat("NetworkRoom SceneLoadedForPlayer scene: {0} {1}", SceneManager.GetActiveScene().path, conn);
        // Debug.Log("scene loaded");
        OnServerReadied?.Invoke(conn, roomPlayer); // spawns player (Invoke event triggers PlayerSpawnSystem.SpawnPlayer)
       
    }
}
