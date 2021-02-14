using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.IO;

// when user tries to connect using IP
// this is on the client, not on the server
public class JoinLobbyMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel = null;
    [SerializeField] private TMP_InputField ipAddressInputField = null;
    [SerializeField] private Button joinButton = null;

    private void OnEnable()
    {
        LobbyNetworkManager.OnClientConnected += HandleClientConnected; // subscribing to events
        LobbyNetworkManager.OnClientDisconnected += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        LobbyNetworkManager.OnClientConnected -= HandleClientConnected;  // unsubscribing to events
        LobbyNetworkManager.OnClientDisconnected -= HandleClientDisconnected;
    }

// JOIN LOBBY
    public void JoinLobby()
    {
        // Debug.Log("click join lobby");
        string ipAddress = ipAddressInputField.text;

        var lobbyManager = NetworkManager.singleton as LobbyNetworkManager;
        // was Failing here before because the explicitly linked networkmanager was destroyed

        joinButton.interactable = false;
        
        if (!string.IsNullOrEmpty(ipAddress)) {
            lobbyManager.networkAddress = ipAddress;
            lobbyManager.StartClient(); // will log warning and get stuck here if no ipAddress
        }
        else {
            joinButton.interactable = true;
            Debug.LogError("Failed to join lobby");
        }
    }

    public void BackToMenu()
    {
        gameObject.SetActive(false);        // popup for entering ip address
        landingPagePanel.SetActive(true);   // menu options
    }

    private void HandleClientConnected() {
        joinButton.interactable = true;

        gameObject.SetActive(false);        // popup for entering ip address
        landingPagePanel.SetActive(false);

        // SceneManager.LoadScene(NetworkManager.singleton.lobbyScene); - should not need this as online scene is set to Lobby by default
    }

    private void HandleClientDisconnected()
    {
        joinButton.interactable = true;
    }
}
