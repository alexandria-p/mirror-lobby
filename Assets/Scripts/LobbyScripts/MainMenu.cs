using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class MainMenu : MonoBehaviour
{

    // [Header("UI")]
    // [SerializeField] private GameObject landingPagePanel = null;

    public void HostLobby()
    {
        //Debug.Log("host lobby");
        // landingPagePanel.SetActive(false);
        var lobbyManager = NetworkManager.singleton as LobbyNetworkManager;
        
        // SceneManager.LoadScene(lobbyManager.lobbyScene); - not needed, as lobby is default online scene
        lobbyManager.StartHost(); // Failing here before because the explicitly linked networkmanager was destroyed
    }
}
