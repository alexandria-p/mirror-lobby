using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// doesnt have to be serverside, only client needs to control and see changes
public class CameraHelper: MonoBehaviour {
    // Properties

    protected Player owner; 
    // point towards player, use player.possession reference instead ?

	// Use this for initialization
	void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        moveCamera();
	}

    public void moveCamera() {
        // BOTH NULL
        // is this because target is set on the serverside?
    
    if (!owner || !owner.possession)
        return;

    //Debug.Log(owner.possession);
    var target = owner.possession;
    // Always look at the target
    transform.position = target.transform.position - target.transform.forward * 10 + target.transform.up * 3;
    transform.LookAt (target.transform.position);
    }

// keep this class clientside. If updatecameratarget called clientside, it will not run with clientside scoped data


    public void UpdatePlayerOwner(Player p) {
        Debug.Log(p);
        owner = p;
    }

}
