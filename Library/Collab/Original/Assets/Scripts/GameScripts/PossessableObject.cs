using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PossessableObject: NetworkBehaviour {
	// Properties
	[SyncVar]
	public bool possessed; 
	[SyncVar] 
	protected GameObject _listener; // private, but extended classes can access them (represents the Player)

	float speed = 20.0F; // calculate according to size ?
    float rotationSpeed = 100.0F;

	[Server]
	public void InitialiseListener(Player p) {
		_listener = p.gameObject;
	}

	[Server]
	virtual public void DestroyListener() {
		_listener = null;
	}

	void OnCollisionEnter(Collision collision)
    {
		if (!hasAuthority) { 
			return;
		}

		// we only want the below code to work if a 
		// player is currently controlling this gameobject and has authority over it
		CallCollide(collision.gameObject);
    }

	public void CallCollide(GameObject obstruction) {

		if (_listener == null) 
		return; 

		var possessableObject = obstruction.GetComponent<PossessableObject>(); // will return null if our obstruction does not have a 'PossessableObject' component.
		
		if (_listener.GetComponent<Player>().isIt) {
			// if we are 'it', and we have collided with another player's possessed object:
			if (possessableObject != null && possessableObject.possessed) {
				_listener.GetComponent<Player>().HandleTag(possessableObject._listener); // activate collision code - pass back who you collided with
			}
		}
		else {
			// if we are not 'it', and we collided with another possessable object:
			if (possessableObject != null && !possessableObject.possessed) {
				_listener.GetComponent<Player>().HandleCollisionEvent(obstruction); // activate collision code - pass back who you collided with
			}
		}
	}

	[ClientRpc]
	 public void RpcMove() {
		if (!hasAuthority) {
			return;
		}

		// check localauthority here too or in cmd for movement on PC ? -> reduce stuttering 
		// (server has authority over truck by default so tries to reset its position every netTransfrom update)

		float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        transform.Translate(0, 0, translation);
        transform.Rotate(0, rotation, 0);
	 }	

}