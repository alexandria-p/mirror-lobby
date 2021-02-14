using System;
//using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;
using System.Linq;


// two classes for the player - one for when they are in the lobby, one for when they get into the game
public class LobbyPlayer : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null; // can update UI individually per player rather than for all
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[4]; // showing all names (passed in to gameobject)
    [SerializeField] private TMP_Text[] playerReadyTexts = new TMP_Text[4];
    [SerializeField] private Button startGameButton = null; // reference here to turn is on or off depending on if player is lobby leader

    [SyncVar(hook = nameof(HandleDisplayNameChanged))] // syncvar - can only be changed on the server (server-validated)
    public string DisplayName = "Loading...";
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;
    [SyncVar]
    public bool InGame = false; // set when server decides to change the scene to game scene --> LobbyNetworkManager.ServerChangeScene()

    private bool isLeader;

    public bool IsLeader {
        get {
            return isLeader;
        }
        set {
            isLeader = value;
            startGameButton.gameObject.SetActive(value);
        }
    }

    private LobbyNetworkManager room;
    private LobbyNetworkManager Room {
        get{
            if (room != null) { return room; }
            return room = NetworkManager.singleton as LobbyNetworkManager;
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetDisplayName(PlayerNameInput.DisplayName); // todo - safety checks for string input on the server?
        lobbyUI.SetActive(true);
    }

    public override void OnStartServer()
    {
         // subscribe for cleanup
        LobbyNetworkManager.OnServerStopped += CleanUpServer; // cleanup lobby when server is stopped, as OnDestroy cannot (see notes for OnDestroy)
        // events
        LobbyNetworkManager.OnRoomPlayerAdded += UpdateDisplay;
        LobbyNetworkManager.OnRoomPlayerRemoved += UpdateDisplay;
    }

    [ServerCallback] // warning: is not called when server is stopped (as server is not running to process callback)
    private void OnDestroy() => CleanUpServer(); // ondestroy called by monobehaviour objects when destroyed - this can be due to destruction of object in scene, or due to it being destroyed by changing scene
    
    [Server]
    private void CleanUpServer()
    {
        LobbyNetworkManager.OnServerStopped -= CleanUpServer;
        LobbyNetworkManager.OnRoomPlayerAdded -= UpdateDisplay;
        LobbyNetworkManager.OnRoomPlayerRemoved -= UpdateDisplay;
    }
    public override void OnStartClient(){
       //if () {
           //Debug.Log(Room.RoomPlayers.Count());
            Debug.Log("add me to RoomPlayers");
            Room.AddLobbyPlayer(this);
        //}
        //else {
            // destroy!
        //}
        /*
        if (!Room.RoomPlayers.Any(_ => _.connectionToClient == connectionToClient)) {
            Debug.Log("connection to client does not already exist in existing room player");
            
        }
        else {
            //todo - destroy self
            Debug.Log("connection to client already exists in existing room player");
        }
        */
    }

    public void Start() {
        
        if (Room)
        {
            //Debug.Log("RoomPlayer found a NetworkRoomManager..");
            if (room.dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
            }
        }
        else {
            //Debug.Log("RoomPlayer could not find a NetworkRoomManager. The RoomPlayer requires a NetworkRoomManager object to function. Make sure that there is one in the scene.");
        }
        
    }

// Previously OnNetworkDestroy() before Oct/Nov update Mirror
    public override void OnStopClient(){
        // TECHNICALLY should never need to be removed, as lobbyplayer is set to DontDestroyOnLoad -> instead, see LobbyNetwrokManager for handling disconnects
        //Debug.Log("remove room player onstopclient");
        //CmdRemoveLobbyPlayerOnStopClient();
    }
/*
    [Command]
    void CmdRemoveLobbyPlayerOnStopClient(){
        Room.RoomPlayers.Remove(this);
    }
*/
    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay() {
        // as there will be several instances of LobbyPlayer (representing each connected player), when a player (yourself or other than yourself) calls UpdateDisplay, it will run this method on its own LobbyPlayer object.
        // (calling a method on one particular Player object does not run it on every other Player objectin sync)
        // this other player calls their 'UpdateDisplay' on their LobbyPlayer - on their own computer, is passes the !hasAuthority check and runs the rest of the method
        // for every other player's computer, !hasAuthority fails and it cycles through all avail player objects before finding yours to run UpdateDisplay on your LobbyPlayer object representing the local user.       
        if (!hasAuthority) {
            foreach (var player in Room.RoomPlayers) {
                if (player.hasAuthority) {
                    player.UpdateDisplay();
                    break;
                }
            }
            return;
        }
        // Debug.Log(Room.RoomPlayers.Count);
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (i < Room.RoomPlayers.Count) {
                // todo - optimise (only update changed name/text)
                playerNameTexts[i].text = Room.RoomPlayers[i].DisplayName;
                playerReadyTexts[i].text = Room.RoomPlayers[i].IsReady ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
            }
            else {
                playerNameTexts[i].text = "Waiting For Player...";
                playerReadyTexts[i].text = string.Empty;
            }
        }
    }

    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader) { return; }
        startGameButton.interactable = readyToStart;
        UpdateDisplay();
    }

    [Command] // called on server (Command)
    private void CmdSetDisplayName(string displayName)
    {
        // todo - sanitisation/checking string characters ?
        DisplayName = displayName;
    }

    [Command]
    public void CmdReadyUp()
    {
        IsReady = !IsReady; // updates syncvar value of this player on all clients (as is set in Command)
        Room.NotifyPlayersOfReadyState();
    }

    [Command] // called on server (command)
    public void CmdStartGame()
    {
        // leader may not be first person to join lobby!
        // if (Room.RoomPlayers[0].connectionToClient != connectionToClient) { return; }

        // if this clientconnection is not the leader, return without starting game.
        if (!Room.RoomPlayers.Any(_ => _.isLeader && _.connectionToClient == connectionToClient)) { return; }

        // StartGame
        Room.StartGame();
    }

    // we dont want this called on server, only by local player on their local client.
    public void ExitLobby()
    {
        if (isLocalPlayer) {
           // try {
                Debug.Log("Quitting lobby");
                var lobbyNetworkManager = NetworkManager.singleton as LobbyNetworkManager;
                lobbyNetworkManager.ReturnToMainMenu(connectionToClient);
                /*
            }
            catch {
                Debug.LogWarning("Error quitting lobby");
            }
            */
        }
    }

    [ClientRpc]
    public void RpcHideLobbyUI()
    {
        // Debug.Log("hide Lobby UI");
        if (isLocalPlayer) {
            lobbyUI.SetActive(false);
        }
    }

    [ClientRpc]
    public void RpcShowLobbyUI()
    {
        // Debug.Log("show Lobby UI");
        if (isLocalPlayer) {
            UpdateDisplay();
            lobbyUI.SetActive(true);
        }
    }
}
