using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonScript : MonoBehaviour {

    Transform _button, _door;
    float _iter;
    
    Vector3 _doorSavePos;
    int _cn;
    
	// Use this for initialization
	void Start () {
		_button = transform;
        _door = transform.parent.GetChild(1);
        
        _doorSavePos = _door.position;
	}
	
	// Update is called once per frame
	void Update () {
        if(_cn > 0){
            _iter = Mathf.Clamp(_iter + Time.deltaTime * 1f, 0f, 1.5f);
        }else{
            _iter = Mathf.Clamp(_iter - Time.deltaTime * 0.5f, 0f, 1.5f);
        }
        
        _door.position = Vector3.Lerp(_doorSavePos, new Vector3(_doorSavePos.x, _doorSavePos.y - 10f, _doorSavePos.z), _iter);
  	}
    
    void OnCollisionEnter(Collision c){
        _cn++;
    }
    
    void OnCollisionExit(Collision c){
        _cn--;
    }
}
