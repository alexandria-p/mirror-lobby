using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : PossessableObject {
	// Properties - inherited from parent class

	// Methods - inherited from parent class
	override public void DestroyListener() {
		this._listener.UpdatePossessionCooldown(5);
		this._listener = null;
		this.DestroySelf(); // when possession leaves Ghost, destroy GhostBody
	}

	void DestroySelf() {
		Destroy(this.gameObject);
	}


}