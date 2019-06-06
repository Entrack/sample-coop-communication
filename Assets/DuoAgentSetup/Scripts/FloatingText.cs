using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingText : MonoBehaviour {

	public float destroyTime = 2f;

	// Use this for initialization
	void Start () {
		Destroy(gameObject, destroyTime);
	}
	
}
