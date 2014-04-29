using UnityEngine;
using System.Collections;

public class InitialiseGameLevel : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GameManager.Get ().GenerateGuns();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
