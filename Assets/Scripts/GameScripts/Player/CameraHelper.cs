using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// doesnt have to be serverside, only client needs to control and see changes
public class CameraHelper: MonoBehaviour {
    // Properties
    protected Player owner;

    protected float mouseSensitivity = 100f;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
	}

    void FixedUpdate () {
        MoveCamera();
    }

    public void MoveCamera() {

        if (!owner)
            return;

        var target = owner.GetModel();
        if (!target) return;

        // Always look at the target
        transform.position = target.transform.position - target.transform.forward * 10 + target.transform.up * 3;
        transform.LookAt(target.transform.position);
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        target.transform.Rotate(Vector3.up * mouseX);
    }

    public void UpdatePlayerOwner(Player p) {
        owner = p;
    }

    public Player GetPlayerOwner() {
        return owner;
    }
}
