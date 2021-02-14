using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PossessableObject: NetworkBehaviour {
	// Properties
	[SyncVar]
	public bool possessed; 

	//sync
	[SyncVar] 
	protected GameObject _listener; // private, but extended classes can access them (Player)

	float speed = 20.0F; // calculate according to size ?
    float rotationSpeed = 100.0F;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}
	public override void OnStartAuthority() {
		Debug.Log("ON START AUTHORITY"); // this debug is called if you imitate client on unity build
	}
	
	[Server]
	public void InitialiseListener(Player p) {
		Debug.Log("initialise");
		Debug.Log(p); 
		_listener = p.gameObject;
		Debug.Log(_listener);
	}

	[Server]
	virtual public void DestroyListener() {
		_listener = null;
	}

	void OnCollisionEnter(Collision collision)
     {

		 Debug.Log("COLLIDE!!!");

		  if (!hasAuthority) { 
			 return;
		 }
		 Debug.Log("HAS AUTHORITY!!!");

		// we only want the below code to work if a 
		// player is currently controlling this gameobject and has authority over it
		 
		 CallCollide(collision.gameObject);
     }

	 public void CallCollide(GameObject objj) {

		 if (_listener == null) 
		 return; 

		 // if there is a listener, and we collided with another possessable object:
		 if (_listener != null && objj.GetComponent<PossessableObject>() != null && !objj.GetComponent<PossessableObject>().possessed) {
			_listener.GetComponent<Player>().HandleCollisionEvent(objj); // activate collision code - pass back who you collided with
		 }

	  }

	[ClientRpc]
	 public void RpcMove() {
		 if (!hasAuthority) {
			 return;
		 }
		// check localauthority here too or in cmd for movement on PC ? -> reduce stuttering 
		// (server has authority over truck by default so tries to reset its position every netTransfrom update)
		// Debug.Log("move");
		float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        transform.Translate(0, 0, translation);
        transform.Rotate(0, rotation, 0);
	 }	

}