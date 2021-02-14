using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHelper: MonoBehaviour {
    // Properties
    private PossessableObject target;

	// Use this for initialization
	void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        this.moveCamera();
	}

    public void moveCamera() {
    if (!target)
        return;

    // Always look at the target
    transform.position = target.transform.position - target.transform.forward * 10 + target.transform.up * 3;
    transform.LookAt (target.transform.position);
    }

    public void UpdateCameraTarget(PossessableObject t) {
        this.target = t;
    }

}
