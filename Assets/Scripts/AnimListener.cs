using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimListener : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void AnimEvent(string e){
        switch (e)
        {
            case ("step"):
                PlayerController._Player.Step();
                break;
        }
    }
}
