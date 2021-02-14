using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManagerAssistance : MonoBehaviour
{
    public GameObject networkManager;

    void Start()
    {
        var netManagers  = FindObjectsOfType<LobbyNetworkManager>();

        if (netManagers.Length == 0)
        {
            Debug.Log("no current networkmanager");
            networkManager.SetActive(true);
        }
        else {
            Debug.Log("already existing networkmanager");
        }

        //Otherwise don't enable the disabled one, it should delete itself if there is a duplicate
    }
}