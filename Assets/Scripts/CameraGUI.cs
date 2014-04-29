using UnityEngine;
using System.Collections;

public class CameraGUI : MonoBehaviour {

	void OnDrawGizmos() {
		Gizmos.DrawWireCube(new Vector3(0,0,0), new Vector3(12.8f, 7.7f));
	}
}
