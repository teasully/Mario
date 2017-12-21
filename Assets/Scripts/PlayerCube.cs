using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour {

    Rigidbody _rb;

    bool _stick;
    Vector3 _stickPos, _stickPoint;
    Transform _stickObj;
    float _unstickTime, _lastHold, _size;
    
    CubeType _type = CubeType.NORMAL;
    
    enum CubeType{
        NORMAL,
        LOW_GRAVITY
    }

	// Use this for initialization
	void Start () {
		_rb = GetComponent<Rigidbody>();

        _size = transform.lossyScale.x;
	}
	
	// Update is called once per frame
	void Update () {
		if(_stick){
            Vector3 dis = _stickPos - _stickObj.position;
            _stickPos = _stickObj.position;
            _stickPoint -= dis;
            transform.position -= dis;
        }
	}
    
    void OnCollisionEnter(Collision c){
        Debug.Log(c.collider.name);
        switch(c.collider.name){
            case("Stick"):
                Stick(c);                
                break;
        }
    }
    
    void Stick(Collision c){
        if(_stick || Time.time - _unstickTime < 0.5f)return;
        _stick = true;
        _rb.isKinematic = true;
        _stickObj = c.collider.transform;
        _stickPos = _stickObj.position;
        _stickPoint = c.contacts[0].point;
    }
    
    void Unstick(){
        if(!_stick)return;
        _stick = false;
        _rb.isKinematic = false;
        _rb.AddForce((transform.position - _stickPoint).normalized * 200f + new Vector3(0f, 1f, 0f) * 400f);
        _unstickTime = Time.time;
        _stickObj = null;
    }
    
    // Fired when square button pressed
    public void Press(){
        // If stick, unstick
        if(_stick) {
            Unstick();
            return;
        }
        // Else, jump cube
        if(Mathf.Abs(_rb.velocity.y) < 0.1f){
            _rb.AddForce(new Vector3(0f, 1f, 0f) * 400f);
        }
    }
    
    public bool OnHold(){
        // Make sure it was not just held
        if(Time.time - _lastHold < 0.5f) return false;
        // Change var
        _stick = false;
        
        if(_type == CubeType.LOW_GRAVITY){
            PlayerController._Player._gravityModifier = 0.65f;
        }
        
        return true;
    }
    
    public void OnThrow(){
        _lastHold = Time.time;
        
        if(_type == CubeType.LOW_GRAVITY){
            PlayerController._Player._gravityModifier = 1f;
        }
    }

    public float GetSize()
    {
        return _size;
    }
}
