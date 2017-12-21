using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformScript : MonoBehaviour {

    Transform _start, _finish;
    public float _speed, _moveIter, _fallTimer;
    bool _left;
	
	// Use this for initialization
	void Start () {
        _start = transform.GetChild(1);
        _finish = transform.GetChild(2);
        
        _start.GetComponent<MeshRenderer>().enabled = false;
        _finish.GetComponent<MeshRenderer>().enabled = false;
    }

    // Update is called once per frame
    void Update () {
        _moveIter += Time.deltaTime * _speed / 5f * (_left ? 1f : -1f);
        transform.GetChild(0).localPosition = Vector3.Lerp(_start.localPosition, _finish.localPosition, _moveIter);
        if(_moveIter > 1f || _moveIter < 0f)
        {
            _left = !_left;
        }
	}
	
	public void OnLand(){
		Debug.Log("Landed");
	}
}
