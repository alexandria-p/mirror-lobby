using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class PossessableObject: MonoBehaviour {
	// Properties
	
	private bool isGrounded;

	public GameObject parent;

	// Movement
	float speed = 20.0F; // calculate according to size ?

	// Jumping
	Vector3 jump = new Vector3(0.0f, 2.0f, 0.0f);
	float jumpForce = 2.0F;

	// Use this for initialization
	void Start () {
		parent = transform.parent.gameObject;
	}

	void Update()
	{
	}

	// called by parent on FixedUpdate
	public void PerformMovement() {	
		CheckJump();

		// jittery without deltatime
		float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
		float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * speed;

		transform.Translate(moveX, 0, moveZ); // network transform should handle syncing up 

		// placeholder shortcuts for debugging purposes, don't work well over network
		if (Input.GetAxis("Cancel")==1)//if we press the esc button
		{
			ResetPose();//reset our position
		}

		if (Input.GetAxis("Fire1")==1)//if we press the left ctrl button
		{
			ResetRotation();//reset our rotation
		}

		if (transform.hasChanged)
        {
            transform.hasChanged = false;
        }
	}

	void OnCollisionEnter(Collision collision)
    {
		// we only want the below code to work if a 
		// player is currently controlling this gameobject and has authority over it
		// so code on Player class has isLocalPlayer check on it
		if (parent == null) return;
		parent.GetComponent<Player>().HandleCollisionEvent(collision.gameObject);
    }

	public void CheckJump() {
		if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
		{
			isGrounded = false;
			GetComponent<Rigidbody>().AddForce(jump * jumpForce, ForceMode.Impulse);
		}
	}

	void OnCollisionStay()
    {
        isGrounded = true;
    }

    public void ResetPose()
    {
		transform.position = new Vector3(0,1,0);
		transform.eulerAngles = new Vector3(0,0,0);
    }

	public void ResetRotation()
    {
		transform.eulerAngles = new Vector3(0,0,0);
    }
}