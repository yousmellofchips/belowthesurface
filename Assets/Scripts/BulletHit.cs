using UnityEngine;
using System.Collections;

public class BulletHit : MonoBehaviour {

	void OnTriggerEnter2D(Collider2D other) { // This one works, with kinematic turned off
		if (other.gameObject.CompareTag("Alien")) {
			GameManager.Get ().DestroyAlien(other.gameObject);
			Destroy (gameObject);
		}
		if (other.gameObject.CompareTag("Player")) {
			GameObject go = GameObject.Find("AlienExplosionSound");
			
			GameObject newObj = (GameObject)GameObject.Instantiate(go);
			newObj.transform.position = other.gameObject.transform.position;
			newObj.audio.PlayOneShot(newObj.audio.clip, 0.8f);

			Destroy(other.gameObject);
			Destroy (gameObject);
		}
	}
}
