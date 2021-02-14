using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PossessableObject: MonoBehaviour {
	// Properties
	public bool possessed;
	protected Player _listener; // private, but extended classes can access them
	protected Rigidbody rigidBody;
	float speed = 20.0F; // calculate according to size ?
    float rotationSpeed = 100.0F;

	// Use this for initialization
	void Start () {
		rigidBody = GetComponent<Rigidbody>(); // 'doesnt exist'
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void InitialiseListener(Player p) {
		this._listener = p;
	}

	virtual public void DestroyListener() {
		this._listener = null;
	}
	
	void OnCollisionEnter(Collision collision)
     {
		 // if there is a listener, and we collided with another possessable object:
		 if (_listener != null && collision.gameObject.GetComponent<PossessableObject>() != null && !collision.gameObject.GetComponent<PossessableObject>().possessed) {
			_listener.HandleCollisionEvent(collision.gameObject); // activate collision code - pass back who you collided with
		 }
     }

	//[Command]
	 public void CmdMove() {
		float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        transform.Translate(0, 0, translation);
        transform.Rotate(0, rotation, 0);

		/* 
		var x = Input.GetAxis("Horizontal")*0.1f;
        var z = Input.GetAxis("Vertical")*0.1f;

        transform.Translate(x, 0, z);
		*/
	 }	

}