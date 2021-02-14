using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Linq;


public struct PlayerScoreStruct {
    public Player player {get; set;} // todo -  security around SET
    public NetworkConnection connectionToClient {get; set;}
    public uint playerNetId { 
        get {
            return player.netId;
        }
    }
    public int score {get; set;}
    public bool scoreIsFrozen {get; set;}
}

public class ScoreManager: NetworkBehaviour {

    // Properties
    // synclists updated in Oct Mirror update - no longer abstract (so need to be initialized)
    public SyncList<PlayerScoreStruct> playersScoreInfo = new SyncList<PlayerScoreStruct>();

    private const int minScore = 0;

    private const float baseScorePerTick = 1;

    [SerializeField] private GameObject scoreUI;

    private GameObject ScoreUI
    {
        get
        {
                if (scoreUI != null) { return scoreUI; }
                return scoreUI = GameObject.FindWithTag("ScoreUI");
        }
    }

    #region SingletonSetup
    public static ScoreManager singleton { get; private set; }

    /// <summary>
    /// A flag to control whether the ScoreManager object is destroyed when the scene changes.
    /// <para>This should be set if your game has a single ScoreManager that exists for the lifetime of the process. If there is a ScoreManager in each scene, then this should not be set.</para>
    /// </summary>
    public bool dontDestroyOnLoad = false;

    public void Awake()
    {
        // Don't allow collision-destroyed second instance to continue.
        if (!InitializeSingleton()) return;

        playersScoreInfo.Callback += OnPlayersScoreSyncListUpdated;
    }

    private bool InitializeSingleton()
    {
        if (singleton != null && singleton == this) return true;

        if (dontDestroyOnLoad)
        {
            if (singleton != null)
            {
                Debug.LogWarning("Multiple ScoreManagers detected in the scene. Only one ScoreManager can exist at a time. The duplicate ScoreManager will be destroyed.");
                Destroy(gameObject);

                // Return false to not allow collision-destroyed second instance to continue.
                return false;
            }
            Debug.Log("ScoreManager created singleton (DontDestroyOnLoad)");
            singleton = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("ScoreManager created singleton (ForScene)");
            singleton = this;
        }

        return true;
    }

    /// <summary>
    /// This is the only way to clear the singleton, so another instance can be created.
    /// </summary>
    public static void Shutdown()
    {
        if (singleton == null)
            return;

        // todo: cleanup

        singleton = null;
    }

    #endregion

    void Update() {
    }

    public override void OnStartServer()
    {
    }

    private void OnPlayersScoreSyncListUpdated(SyncList<PlayerScoreStruct>.Operation op, int index, PlayerScoreStruct oldItem, PlayerScoreStruct newItem)
    {
        var networkConnection = newItem.connectionToClient;
        switch (op)
        {
            case SyncList<PlayerScoreStruct>.Operation.OP_ADD:
                // index is where it got added in the list
                // item is the new item
                 // Debug.Log("playerScores SyncList added to");
                break;
            case SyncList<PlayerScoreStruct>.Operation.OP_CLEAR:
                // list got cleared
                break;
            case SyncList<PlayerScoreStruct>.Operation.OP_INSERT:
                // index is where it got added in the list
                // item is the new item
                break;
            case SyncList<PlayerScoreStruct>.Operation.OP_REMOVEAT:
                // index is where it got removed in the list
                // item is the item that was removed
                break;
            case SyncList<PlayerScoreStruct>.Operation.OP_SET:
                // index is the index of the item that was updated
                // item is the previous item
               //  Debug.Log("playerScores SyncList updated");
                break;
        }
    }

    private PlayerScoreStruct CreateNewStructFromStruct(PlayerScoreStruct oldStruct) {
        return new PlayerScoreStruct{
            player = oldStruct.player,
            connectionToClient = oldStruct.connectionToClient,
            score = oldStruct.score,
            scoreIsFrozen = oldStruct.scoreIsFrozen,
        };
    }

// SHOULD BE WITHIN SERVER
    public void SetDefaultValues(GameObject playerObject) {
        try {
        var indexOfPS = playersScoreInfo.FindIndex(_ => _.playerNetId == playerObject.GetComponent<Player>()?.netId);

        PlayerScoreStruct psNew = CreateNewStructFromStruct(playersScoreInfo[indexOfPS]);
        psNew.score = 1;

        playersScoreInfo[indexOfPS] = psNew; // triggers SET callback for SyncLists
        }
        catch {
            Debug.Log("err: ScoreManager.SetDefaultValues");
            Debug.Log(playersScoreInfo.Count());
        }
    }

    // should run on serverside
    private void AddScore(int amount, GameObject playerObject)
    {
        var indexOfPS = playersScoreInfo.FindIndex(_ => _.playerNetId == playerObject.GetComponent<Player>()?.netId);

        if (playersScoreInfo[indexOfPS].scoreIsFrozen)
            return;

        PlayerScoreStruct psNew = CreateNewStructFromStruct(playersScoreInfo[indexOfPS]);

        psNew.score += amount; 
        playersScoreInfo[indexOfPS] = psNew; // triggers SET callback for SyncLists
    }

    // should run on serverside
    private void LoseScore(int amount, GameObject playerObject)
    {
        var indexOfPS = playersScoreInfo.FindIndex(_ => _.playerNetId == playerObject.GetComponent<Player>()?.netId);

        if (playersScoreInfo[indexOfPS].scoreIsFrozen)
            return;

        PlayerScoreStruct psNew = CreateNewStructFromStruct(playersScoreInfo[indexOfPS]);

        var tempScore = psNew.score - amount; 
        psNew.score = (tempScore <= minScore) ? minScore : tempScore;

        playersScoreInfo[indexOfPS] = psNew; // triggers SET callback for SyncLists
    }

    [Server] // should run on serverside
    public void AddPlayerToScoreSyncList(GameObject playerObject)
    {
        Debug.Log("Add player to playerInfo");
        var playerComponent = playerObject.GetComponent<Player>();
        var playerInfo = new PlayerScoreStruct {
            player = playerComponent,
            connectionToClient = playerObject.GetComponent<NetworkIdentity>().connectionToClient,
            scoreIsFrozen = false // should start TRUE and be unfrozen by RoundSystem.cs
        };
        playersScoreInfo.Add(playerInfo);
    }

    [Server] // should run on serverside
    // currently called by RoundSystem
    public void RemovePlayerFromScoreSyncList(NetworkConnection clientConnection)
    {
        try {
            Debug.Log("Remove player from score player list");
            var playerInfo = playersScoreInfo.First(_ => _.connectionToClient == clientConnection);
            playersScoreInfo.Remove(playerInfo);
            // Debug.Log(playersScoreInfo.Count());
        }
        catch {
            Debug.Log("Could not find player to remove");
        }
    }

    [Server] // should be called from serverside
    public void SetAddScore(GameObject playerObject, int gainedScore)
    {   
        AddScore(gainedScore, playerObject);
    }

    [Server] // should be called from serverside
    public void SetLoseScore(GameObject playerObject, int lostScore)
    {        
        LoseScore(lostScore, playerObject);
    }

    // should run on serverside
    public void FreezeScore(GameObject playerObject)
    {
        //todo - SAFETY - try/catch
        var indexOfPS = playersScoreInfo.FindIndex(_ => _.playerNetId == playerObject.GetComponent<Player>()?.netId);
        PlayerScoreStruct psNew = CreateNewStructFromStruct(playersScoreInfo[indexOfPS]);
        psNew.scoreIsFrozen = true;
        playersScoreInfo[indexOfPS] = psNew; 
    }

    // should run on serverside
    public void UnfreezeScore(GameObject playerObject)
    {
        var indexOfPS = playersScoreInfo.FindIndex(_ => _.playerNetId == playerObject.GetComponent<Player>()?.netId);
        PlayerScoreStruct psNew = CreateNewStructFromStruct(playersScoreInfo[indexOfPS]);
        psNew.scoreIsFrozen = false;
        playersScoreInfo[indexOfPS] = psNew; 
        StartCoroutine("IncrementScoreEverySecond", psNew.player.gameObject);
    }

    [Server]
    IEnumerator IncrementScoreEverySecond(GameObject playerObject)
    {
        // when playerobject is destroyed by player leaving round, exits loop
        while (true && playerObject != null && playerObject.GetComponent<Player>()) 
        {
            yield return new WaitForSeconds(1);
            HandleScoreOnTick(playerObject);
        }
    }

    private void HandleScoreOnTick(GameObject playerObject) {
        try {
            var netId = playerObject.GetComponent<Player>().netId; // may fail if client drops out mid-tick
            var playerInfo = playersScoreInfo.First(_ => _.playerNetId == netId); // throws error if not found

            AddScore((int)(baseScorePerTick), playerObject); // int will floor / round down to nearest integer value
        }
        catch {
            Debug.LogWarning("Player not found for score coroutine on tick");
        }
    }

    public int ReadScore(GameObject playerObject)
    {
        var playerInfo = playersScoreInfo.First(_ => _.playerNetId == playerObject.GetComponent<Player>()?.netId);
        return playerInfo.score;
    }

    public PlayerScoreStruct GetWinningPlayer()
    {
        // TODO - handle tie
        var playerInfo = playersScoreInfo.OrderByDescending(_ => _.score).First(); // only gets first object in case of tie
        return playerInfo;
    }

#region TargetRpc

    // hook -> run client-side
    [TargetRpc]
    void RpcHandleScoreChanged(NetworkConnection target, int newValue) {
        try {
            ScoreUI.GetComponent<TMP_Text>().text = newValue.ToString();
        } 
        catch {
            // todo handle where no UI yet
            Debug.Log("failed RpcHandleScoreChanged");
        }
    }

    #endregion
}
