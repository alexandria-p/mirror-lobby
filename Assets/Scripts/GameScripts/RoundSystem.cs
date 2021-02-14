using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;

public class RoundSystem : NetworkBehaviour
{
    // if you dislike making things public(and you should), you can use the SerializeField attribute instead to make your private variables available to edit in the inspector. 
    [SerializeField] private Animator animator; // use animator to drive the round system (eg 3,2,1, go)
    [SerializeField] private GameObject roundTimer;
    [SerializeField] private GameObject scoreUI;

    [SerializeField] private GameObject endRoundCanvas;
    [SerializeField] private GameObject endRoundUI;

    [SyncVar(hook = nameof(HandleRoundTimerChanged))]
    private int timeLeft;

    [SerializeField] private int roundLength = 15; // can be set on Unity UI

    [SyncVar]
    private bool pregamePhaseActive = false;

    [SyncVar]
    private bool roundActive = false;

    [SyncVar(hook = nameof(HandleWinnerChanged))]
    private string winningPlayerName;
    [SyncVar(hook = nameof(HandleWinningScoreChanged))]
    private int winningPlayerScore;


    private string localWinningPlayerName;
    [SyncVar]
    private int localWinningPlayerScore;

    private LobbyNetworkManager room;
    private LobbyNetworkManager Room
    {
        get {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as LobbyNetworkManager; // cache networkmanager singleton as lobby
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

    void Update () {
        if ( roundActive && timeLeft <= 0 )     
        {         
            EndRound();     
        } 
	}

    #region Server
// subscribe to events
    public override void OnStartServer()
    {
        pregamePhaseActive = true;
        LobbyNetworkManager.OnServerStopped += CleanUpServer; // cleanup lobby when server is stopped, as OnDestroy cannot (see notes for OnDestroy)
        LobbyNetworkManager.OnGamePlayerAdded += CheckAllPlayersAreIn; // when someone has readied on the server - essentially every time someone loads in (does not mean 'when the server is ready')
        LobbyNetworkManager.OnGamePlayerRemoved += CheckAllPlayersAreIn; //fi a player times out on scenechange, their game player will be removed and everyone needs to check if the game can be started now
        // Evoked when a player votes to return to the lobby
        Player.OnPlayerReadyForLobby += HandleReturnToLobby;
        // Evoked when player quits to menu and also when player disconnects midgame
        LobbyNetworkManager.OnGamePlayerRemoved += HandlePlayerLeavingGame;
    }

    [ServerCallback] // warning: is not called when server is stopped (as server is not running to process callback)
    private void OnDestroy() => CleanUpServer(); // ondestroy called by monobehaviour objects when destroyed - this can be due to destruction of object in scene, or due to it being destroyed by changing scene
    
    [Server]
    private void CleanUpServer()
    {
        LobbyNetworkManager.OnServerStopped -= CleanUpServer;
        LobbyNetworkManager.OnGamePlayerAdded -= CheckAllPlayersAreIn;        
        LobbyNetworkManager.OnGamePlayerRemoved -= CheckAllPlayersAreIn;
        Player.OnPlayerReadyForLobby -= HandleReturnToLobby;
        LobbyNetworkManager.OnGamePlayerRemoved -= HandlePlayerLeavingGame;

    }

    [Server]
    private void CheckAllPlayersAreIn(NetworkConnection conn)
    {
        if (!pregamePhaseActive) return;
        // becuase players aren't allowed to join midgame in this example, this should only be triggered at the start of a round
        if (!Room.GamePlayers.Any() ||
            Room.GamePlayers.Count(x => x.connectionToClient.isReady) 
            != Room.RoomPlayers.Count(x => x.InGame == true) ) 
        {
            Debug.Log("not enough players");
            return; 
        }
        //Debug.Log("start round - all players are here");
        //Debug.Log(Room.RoomPlayers.Count());
        //Debug.Log(Room.RoomPlayers.Count(x => x.InGame == true));
        SetupNewRound();
    }

    [Server]
    private void SetupNewRound()
    {
        // TODO - perform setup if required for players eg allocate roles
        RpcStartCountdown();
        // todo - set winning player to nil
    }


    [ServerCallback]
    public void CountdownEnded() // called when countdown has complete
    {
        SetupRoundTimer();
        RpcStopCountdown();
    }

    void SetupRoundTimer() {
        //Debug.Log("start round!");
        timeLeft = roundLength;
        pregamePhaseActive = false;
        roundActive = true;
        foreach (Player player in Room.GamePlayers)
        {
            ScoreManager.SetDefaultValues(player.gameObject);
            ScoreManager.UnfreezeScore(player.gameObject); // starts coroutine for score increment
        }
        StartCoroutine("LoseTime");
        RpcStartTimer();
    }


    [ServerCallback]
    public void StartRound() // called on 0 countdown
    {
        AllowPlayerMovement();
    }

    [Server]
    public void EndRound()
    {
        foreach (Player player in Room.GamePlayers)
        {
            ScoreManager.FreezeScore(player.gameObject);
        }
        roundActive = false;
        SetWinner();
        RpcEndRound();
        // Debug.Log("end round on server!");
        DisallowPlayerMovement();
    }

    // run serverside
    private void SetWinner() {
        PlayerScoreStruct winningPlayer = ScoreManager.GetWinningPlayer(); // todo - handle tie?
        winningPlayerName = winningPlayer.player.GetPlayerDisplayName(); 
        winningPlayerScore = winningPlayer.score; 
    }

    #endregion

    #region Client

    [ClientRpc]
    private void RpcStartCountdown() {
        animator.enabled = true;
    }

    [ClientRpc]
    private void RpcStopCountdown() {
        animator.enabled = false;
    }

// client rpc wont sync syncvar as its not serverside
// if you try to assign value to a SyncVar from code that is being executed on a client, like in a [ClientRpc] method, the SyncVar will not sync it's value to all other clients.
    [ClientRpc]
    private void RpcStartRound()
    {
        AllowPlayerMovement();
    }

    [ClientRpc]
    private void RpcEndRound()
    {
        Debug.Log("end round for client!");
        DisallowPlayerMovement(); 
        // display winner
        endRoundUI.GetComponent<TMP_Text>().text = "Winner is " + localWinningPlayerName?.ToString() + " with a score of " + localWinningPlayerScore.ToString();
        endRoundCanvas.SetActive(true);
    }
 
    [ClientRpc]
    private void RpcStartTimer() {
        // show timer & score
        roundTimer.SetActive(true); // starts as disabled in editor
        scoreUI.SetActive(true); // starts as disabled in editor
    }
    
    IEnumerator LoseTime()
    {
        while (timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft--;
        }
    }

    void AllowPlayerMovement()
    {
        foreach (Player player in Room.GamePlayers)
        {
            player.allowMovement = true;
            player.freezeAllActions = false;
        }
    }

    void DisallowPlayerMovement()
    {
        foreach (Player player in Room.GamePlayers)
        {
            player.allowMovement = false;
            player.freezeAllActions = true;
        }
    }

// called locally by clicking button
    public void ReturnToMainMenu() {
        // Debug.Log("return to main menu!");
       
        foreach (var player in Room.GamePlayers) {
            if (player.isLocalPlayer) {
                // Debug.Log("is local player");
                player.CmdSetPlayerAsReadyToReturnToMenu();
                Room.ReturnToMainMenu(player.connectionToClient);
                break;
            }
        }
    }

// Called by clicking 'Return to lobby' button
// ran locally
    public void PlayerContinueToLobby() {
        // Debug.Log("continue to lobby click");
            foreach (var player in Room.GamePlayers) {
                if (player.isLocalPlayer) {
                    // Debug.Log("is local player");
                    endRoundCanvas.transform.GetChild(1).gameObject.SetActive(false);
                    endRoundCanvas.transform.GetChild(2).gameObject.SetActive(false);
                    player.CmdSetPlayerAsReadyToReturnToLobby();
                    break;
                }
            }
            return;
    }

    public bool PlayersReadyToReturnToLobby() {
        foreach (var player in Room.GamePlayers)
        {
            // this will not be synced unless run on server for some reason
            
            if (!player.IsReadyToReturnToLobby)
                return false;
                
        }

        return true;
    }


// EVENT callback

    public void HandlePlayerLeavingGame(NetworkConnection clientConnection) {
        // Debug.Log("handle player leaving game");
        ScoreManager.RemovePlayerFromScoreSyncList(clientConnection);

        if (pregamePhaseActive || roundActive) return;

        // if you are the player that exited to main menu, dont run this locally, otherwise every other player should refresh their check to see if everyone can move to lobby
        foreach (var player in Room.GamePlayers) {
            if (player.isLocalPlayer && !player.IsReadyToReturnToMenu) {
                    HandleReturnToLobby(); // trigger this if you are waiting to make a selection or waiitng to return to lobby
            }
        } 
    }

// RUN SERVERSIDE
    public void HandleReturnToLobby(NetworkConnection clientConnection = null) {

        if (pregamePhaseActive || roundActive) return;

        // Debug.Log("round not active, check if all players are waiting to return to lobby");
        if (!PlayersReadyToReturnToLobby()) {
            // if this is a callback from a player who has readied up, show waiting for players message:
            if (clientConnection != null) ShowWaitingForPlayersMessage(clientConnection);
            return;
        }
        Room.ServerChangeScene(Room.lobbyScene);
    }

// RUN SERVERSIDE
    private void ShowWaitingForPlayersMessage(NetworkConnection clientConnection) {
        // Debug.Log("show waiting for players message");
        RpcHandleWaitingForPlayers(clientConnection);
    }

    [TargetRpc]
    void RpcHandleWaitingForPlayers(NetworkConnection target) {
        try {
            endRoundCanvas.transform.GetChild(3).gameObject.SetActive(true);
            Debug.Log("targetRPC - waiting for players");
        } 
        catch {
            Debug.Log("failed RpcHandleWaitingForPlayers"); 
        }
    }

    // hooks -> are all run client-side
    void HandleRoundTimerChanged(int oldValue, int newValue) {
        // Debug.Log(newValue.ToString());
        roundTimer.GetComponent<TMP_Text>().text = newValue.ToString();
    }

    void HandleWinnerChanged(string oldValue, string newValue) {
        localWinningPlayerName = newValue;
        endRoundUI.GetComponent<TMP_Text>().text = "Winner is " + localWinningPlayerName?.ToString() + " with a score of " + localWinningPlayerScore.ToString(); 
    }

    void HandleWinningScoreChanged(int oldValue, int newValue) {
        localWinningPlayerScore = newValue;
        endRoundUI.GetComponent<TMP_Text>().text = "Winner is " + localWinningPlayerName?.ToString() + " with a score of " + localWinningPlayerScore.ToString();
    }
    #endregion

}
