using UnityEngine;
using System.Collections;

public class AlienHit : MonoBehaviour {

	void OnCollisionEnter2D(Collision2D other) {
		if (other.gameObject.CompareTag("Bullet")) {
			Destroy (other.gameObject);
			Destroy (gameObject);
		}
	}
}
