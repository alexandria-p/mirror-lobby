using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player: MonoBehaviour {
    // Properties
    private PossessableObject possession;
    private CameraHelper camera;
    private float possessionCooldown;
    private float nextValidPossession;

    // Flags
    private bool isIt;
    private bool repossessing = false;

    // This function is only called on the local player on their client.
    // The OnStartLocalPlayer function is a good place to do initialization that is only for the local player, such as configuring cameras and input.
	public /*override*/ void OnStartLocalPlayer()
    {
        this.camera = transform.Find("Camera").GetComponent<CameraHelper>(); // this SHOULD find child component of player object with the name "Camera"
	}

    // Use this for initialization
    void Start() {
        /*
        if (!isLocalPlayer)
            return;
*/
        OnStartLocalPlayer(); //test this being here
        // GetComponent<PlayerMovement>().enabled = true;
        this.possessionCooldown = 0;
        this.nextValidPossession = -1;
        
        // Test this for multiplayer
        this.possess(transform.Find("GhostBody").GetComponent<Ghost>());
    }
	
	// Update is called once per frame
	void Update () {
        /*
        if (!isLocalPlayer)
            return;
*/
        if (this.possession != null) {
            movePossession();
        }
        else {

        }
        this.CheckIfStillRepossessing();
	}

    void movePossession() {
       this.possession.CmdMove();
    }

    void possess(PossessableObject newObject) {
        
        if (!this.repossessing) {
            Debug.Log("RESET possession");
        // Set cooldown
            this.nextValidPossession = Time.time + this.possessionCooldown;
            this.repossessing = true;
        // Unset old object
            if (this.possession != null) {
                this.possession.DestroyListener();
                this.possession.possessed = false;
            }
        // Setup new object
            newObject.possessed = true;
            this.possession = newObject;
            this.possession.InitialiseListener(this);

        // snap camera
            this.camera.UpdateCameraTarget(this.possession);
            this.CheckIfStillRepossessing();
        }
        else {
            Debug.Log("still cooling down");
        }
    }

    void CheckIfStillRepossessing() {
        if (this.repossessing && Time.time > this.nextValidPossession) {
            this.repossessing = false;
        }
    }

    public void UpdatePossessionCooldown(float newCooldown) {
        this.possessionCooldown = newCooldown;
    }

    public void HandleCollisionEvent(GameObject input){
        this.possess(input.GetComponent<PossessableObject>());
	}

}
