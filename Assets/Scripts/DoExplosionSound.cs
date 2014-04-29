using UnityEngine;
using System.Collections;

public class DoExplosionSound : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnExplosionSound() {
		GameObject go = GameObject.Find("ExplosionSound");
		AudioSource source = go.GetComponent<AudioSource>();
		//audio.Stop();
		audio.PlayOneShot(source.clip, 0.8f);
	}
}
