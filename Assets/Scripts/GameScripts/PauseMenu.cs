using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;

public class PauseMenu : NetworkBehaviour
{

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel = null;

    private float toggleCooldown = 0.15F;
    private float nextMenuToggle = -1;

    void FixedUpdate() {
		if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer) {
            return;
        }

        if (Input.GetAxis("Tab") == 1 && CheckIfCooldownHasComplete())//if we press the tab button
		{
            nextMenuToggle = Time.time + toggleCooldown;
            Debug.Log("toggle pause menu");
			if (pauseMenuPanel.activeSelf == true) { HideMenu(); } else { ShowMenu(); }
		}
    }

    bool CheckIfCooldownHasComplete() {
        return (Time.time > nextMenuToggle); // Time.time is ok to use here, as it is all happening clientside and doesnt need to be synced up on the network
    }

    private void ShowMenu() {
        if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer) return;
        pauseMenuPanel.SetActive(true);
    }

    private void HideMenu() {  
        if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer) return;
        pauseMenuPanel.SetActive(false);
    }

    public void Resume()
    {
        if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer) return;
        Debug.Log("resume");
        HideMenu();
    }

    public void Settings()
    {
        if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer) return;
        Debug.Log("settings");
    }

    public void ExitLobby()
    {
        Debug.Log("exit lobby, return to main menu");
        // TODO - "Are you sure?" buttons
        if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer) return;
        var roundSystems = FindObjectsOfType<RoundSystem>();
        if (roundSystems.Length == 0)
        {
            Debug.Log("no current roundsystem");
        }
        else {
            roundSystems.First().ReturnToMainMenu();
        }
    }
}
