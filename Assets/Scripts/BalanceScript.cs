using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalanceScript : MonoBehaviour {

    Rigidbody _rb;

	// Use this for initialization
	void Start () {
        _rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        _rb.AddTorque(Vector3.Cross(transform.up, Vector3.up).normalized * 5f);
	}
}
