using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using System;
using System.Threading.Tasks;

public class Player: NetworkBehaviour {
    
    // Lobby properties
    
    // Syncvars must be updated in a command to run serverside so that server can sync it up
    [SyncVar]
    private string displayName = "Loading...";

    // Properties

    private GameObject model;     // local use only

    // Flags
    [SyncVar]
    public bool allowMovement;
    [SyncVar]
    public bool freezeAllActions;

    // Hooks
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReadyToReturnToLobby = false;
    [SyncVar]
    public bool IsReadyToReturnToMenu = false;

    public static event Action<NetworkConnection> OnPlayerReadyForLobby;

    public void HandleReadyStatusChanged(bool olValue, bool newValue) => TriggerUpdateDisplay();     // client-side

    private void TriggerUpdateDisplay() {
        OnPlayerReadyForLobby?.Invoke(connectionToClient); // invoke OnPlayerReadyForLobby event
    }

    // Singletons
    private LobbyNetworkManager room;
    private LobbyNetworkManager Room
    {
        get
        {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as LobbyNetworkManager;
        }
    }

    private ScoreManager scoreManager;
    private ScoreManager ScoreManager
    {
        get
        {
            if (scoreManager != null) { return scoreManager; }
            return scoreManager = ScoreManager.singleton as ScoreManager;
        }
    }

    
    [Server]
    public void SetDisplayName(string displayName){
        this.displayName = displayName;
    }

    public string GetPlayerDisplayName() {
        return displayName;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        //Debug.Log("start server on Player");

        // Set defaults
        allowMovement = false;
        freezeAllActions = true;
    }

    public override void OnStartClient(){
        // DontDestroyOnLoad(gameObject); // uncomment if you don't want the game player object to be destroyed when changing scenes in your game
        Room.AddGamePlayer(this);
        CmdAddPlayerToScoreManager();
        base.OnStartClient();
    }

    public override void OnStartLocalPlayer () 
    {
        base.OnStartLocalPlayer();

        if (!isLocalPlayer) 
            return;

        model = transform.Find("PlayableObj").gameObject;
        transform.Find("Camera").gameObject.SetActive(true);    // camera only active for your playerprefab
        transform.Find("PauseMenu_Canvas").gameObject.SetActive(true);

        if (transform.Find("Camera").GetComponent<CameraHelper>().GetPlayerOwner() == null)
            transform.Find("Camera").GetComponent<CameraHelper>().UpdatePlayerOwner(this); // first time update player owner    // pause menu only active for your playerprefab	
    }

    public GameObject GetModel() {
        if (isLocalPlayer)
            return model;

        return transform.Find("PlayableObj").gameObject;
    }

	// Update is called once per frame
	void Update () {
        if (!isLocalPlayer) 
            return;
	}

    void FixedUpdate() {
		if (!isLocalPlayer 
		|| !allowMovement
		|| freezeAllActions) {
			return;
		}

        if (!model) return;
        model.GetComponent<PossessableObject>().PerformMovement(); 
	}

    public void HandleCollisionEvent(GameObject input){
        if (!isLocalPlayer) {   
            return;
        }
        // todo - handle your collision
	}

    [Command]
    private void CmdAddPlayerToScoreManager() {
        ScoreManager.AddPlayerToScoreSyncList(this.gameObject);
    }

    [Command] // run serverside
    public void CmdSetPlayerAsReadyToReturnToLobby() {
        // Debug.Log("command set ready for lobby");
        IsReadyToReturnToLobby = true;
    }

    [Command] // run serverside
    public void CmdSetPlayerAsReadyToReturnToMenu() {
        // Debug.Log("command set ready for menu");
        IsReadyToReturnToMenu = true;
    }

    public override void OnStopClient()
    {
        CleanupGamePlayer();
        base.OnStopClient();
    }

    // called when returning to lobby + destroying game instance
    private void CleanupGamePlayer() {
        // Debug.Log("cleanup game player");
        // the client connection is no longer active at this point, so cannot call commands
        Room.CleanupGamePlayer(this);
    }


}
