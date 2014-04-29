using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
	private int _currentAlienMoveDelay = 0;
	private int _totalPossibleGuns = 20; 
	private int _totalActiveGuns = 0;
	private int _gameOverCount = 100;

	public int AlienMoveDelay = 120;

	public bool GameOver {
		get;
		private set;
	}

	public bool YouWin {
		get;
		private set;
	}

	public static GameManager Get() {
		if (_instance == null) {
			GameObject singleton = new GameObject ();
			_instance = singleton.AddComponent<GameManager> ();
			singleton.name = "(singleton) " + typeof(GameManager).ToString ();
			
			DontDestroyOnLoad (singleton);
		}
		return _instance;
	}

	public enum GunHeatState {
		Cold,			// green
		LowHeat,		// light green
		MediumHeat,		// brown
		HighHeat,		// orange
		DangerousHeat,	// red
		Explode,		// BYEEEEE
		Destroyed,		// gone
		Gun				// Woo hoo a gun!
	}

	public class Gun {
		public GameObject groundBlock = null;
		public int overheatDelay = 0;
		public int currentOverheatTime = 0;
		public GunHeatState state = GunHeatState.Cold;

		public void Transition() {
			GameObject newBlock = null;

			switch (state) {
			case GunHeatState.Cold:
				state = GunHeatState.LowHeat;
				// create replacement block of correct colour, replace in scene, delete old.
				newBlock = (GameObject)Instantiate(Resources.Load("LowHeatBlock_64x64"));
				break;
			case GunHeatState.LowHeat:
				state = GunHeatState.MediumHeat;
				newBlock = (GameObject)Instantiate(Resources.Load("MediumHeatBlock_64x64"));
				break;
			case GunHeatState.MediumHeat:
				state = GunHeatState.HighHeat;
				newBlock = (GameObject)Instantiate(Resources.Load("HighHeatBlock_64x64"));
				break;
			case GunHeatState.HighHeat:
				state = GunHeatState.DangerousHeat;
				newBlock = (GameObject)Instantiate(Resources.Load("DangerousHeatBlock_64x64"));
				overheatDelay = 120; // imminent explosion always takes the same time
				break;
			case GunHeatState.DangerousHeat:
				state = GunHeatState.Explode;
				break;
			case GunHeatState.Explode:
				state = GunHeatState.Destroyed;
				break;
			case GunHeatState.Gun:
				// Do not change state
				break;
			}
			currentOverheatTime = overheatDelay;

			if (newBlock != null) {
				newBlock.transform.position = groundBlock.transform.position;
				Destroy(groundBlock);
				groundBlock = newBlock;
			}
		}
	}

	private static GameManager _instance = null;

	private List<Gun> _guns;
	private List<GameObject> _aliens;

	void Update() {
		if (Application.loadedLevel == 0){
			if (Input.anyKeyDown)
				Application.LoadLevel(1);
		}

//		Debug.Log("Active guns: " + _totalActiveGuns);
//		Debug.Log("Possible guns: " + _totalPossibleGuns);
		if (_totalActiveGuns == _totalPossibleGuns) {
			// change to alien invasion mode!
			if (_aliens == null) {
				GenerateAliens();
				ReadyGuns();
			}

			FireGuns();
		}
	}

	void FixedUpdate() {
		if (GameOver || YouWin) {
			if (_gameOverCount == 0) {
				if (Input.anyKeyDown) {
					_currentAlienMoveDelay = 0;
					_totalPossibleGuns = 20;
					_totalActiveGuns = 0;
					_gameOverCount = 100;
					_guns = null;
					_aliens = null;
					Application.LoadLevel(1); // restart
					GameOver = YouWin = false;
				}
			} else {
				_gameOverCount--;
			}
			return;
		}

		if (_guns != null) {
			List<Gun> gunsToDestroy = new List<Gun>();

			foreach (Gun gun in _guns) {
				if (gun.currentOverheatTime == 0) {
					gun.Transition();
					if (gun.state == GunHeatState.Destroyed) {
						gunsToDestroy.Add(gun);
					}
				}
				gun.currentOverheatTime--;
			}

			foreach (Gun gun in gunsToDestroy) {
				PlayGunExplosion(gun.groundBlock);
				Destroy (gun.groundBlock);
				_guns.Remove(gun);
				_totalPossibleGuns--;
			}
		}
		if (_aliens != null) {
			if (_aliens.Count == 0 && YouWin == false) {
				YouWin = true;
				GameObject camera = GameObject.Find("Main Camera");
				Vector3 pos = camera.transform.position;
				pos.z = 0;
				GameObject youWinLogo = (GameObject)Instantiate(Resources.Load("YouWin"));
				youWinLogo.transform.position = pos;
				return;
			}

			if (_currentAlienMoveDelay == 0) {
				foreach (GameObject alien in _aliens) {
					Vector3 alienPos = alien.transform.position;
					alienPos.y -= 0.64f;
					alien.transform.position = alienPos;

					alien.audio.Play();

					if (alienPos.y <= -2.88 + 0.64f) { // ground level? aliens won
						GameOver = true;
						GameObject camera = GameObject.Find("Main Camera");
						Vector3 pos = camera.transform.position;
						pos.z = 0;
						GameObject gameOverLogo = (GameObject)Instantiate(Resources.Load("GameOver"));
						gameOverLogo.transform.position = pos;
					}
				}
				_currentAlienMoveDelay = AlienMoveDelay;
			} else {
				_currentAlienMoveDelay--;
			}
		}
	}

	void PlayGunExplosion(GameObject groundBlock) {
		GameObject go = GameObject.Find("ExplosionSound");

		GameObject newObj = (GameObject)GameObject.Instantiate(go);
		newObj.transform.position = groundBlock.transform.position;
		newObj.audio.PlayOneShot(newObj.audio.clip, 0.8f);
	}

	public void GenerateGuns() {
		_guns = new List<Gun>();

		GameObject[] groundBlocks = GameObject.FindGameObjectsWithTag("Ground");
		Debug.Log(groundBlocks.Length);

		int i = 0;
		while (_guns.Count < 20) {
			GameObject groundBlock = groundBlocks[i];

			if (groundBlock != null && Random.value > 0.5f) {
				int delay = Mathf.FloorToInt(Random.value * 1000);
				delay = Mathf.Max(delay, 100);

				Gun gun = new Gun();
				gun.groundBlock = groundBlock;
				gun.overheatDelay = delay;
				gun.currentOverheatTime = gun.overheatDelay; // initialise
				gun.state = GunHeatState.Cold;

				_guns.Add(gun);

				groundBlocks[i] = null; // don't handle again
			}

			if (++i == groundBlocks.Length) {
				i = 0;
			}
		}
	}

	public Gun FindGunForGround(GameObject groundBlock) {
		foreach (Gun gun in _guns) {
			if (gun.groundBlock == groundBlock) {
				return gun;
			}
		}

		return null;
	}

	public void TransformIntoGun(Gun gun) {
		if (gun.state == GunHeatState.Gun) return;

		gun.state = GunHeatState.Gun;
		GameObject newBlock = (GameObject)Instantiate(Resources.Load("Gun_64x64"));

		newBlock.transform.position = gun.groundBlock.transform.position;
		Destroy(gun.groundBlock);
		gun.groundBlock = newBlock;
		_totalActiveGuns++;
	}

	public void DestroyAlien(GameObject alien)
	{
		GameObject go = GameObject.Find("AlienExplosionSound");
		
		GameObject newObj = (GameObject)GameObject.Instantiate(go);
		newObj.transform.position = alien.transform.position;
		newObj.audio.PlayOneShot(newObj.audio.clip, 0.8f);

		_aliens.Remove(alien);
		Destroy (alien);
	}

	private void GenerateAliens() {
		List<int> positionsTaken = new List<int>();
		for (int i = 0; i <= 59; i++) {
			positionsTaken.Add (i);
		}

		while (positionsTaken.Count > 20) {
			int posToRemove = Mathf.FloorToInt(Random.value * positionsTaken.Count);
			posToRemove = Mathf.Min (59, posToRemove);
			positionsTaken.RemoveAt(posToRemove);
		}

		_aliens = new List<GameObject>();
		foreach (int pos in positionsTaken) {
			GameObject newAlien = (GameObject)Instantiate(Resources.Load("Alien_64x64"));
			float xPos = -18.24f + (0.64f * pos) - 0.64f;
			newAlien.transform.position = new Vector3(xPos, 3.52f,0);
			_aliens.Add(newAlien);
		}

		_currentAlienMoveDelay = AlienMoveDelay;
	}

	private void ReadyGuns()
	{
		foreach (Gun gun in _guns) {
			GameObject newBlock = (GameObject)Instantiate(Resources.Load("GrassBlock_64x64"));
			newBlock.transform.position = gun.groundBlock.transform.position;
			gun.groundBlock.transform.Translate(new Vector3(0, 0.64f, 0));
		}
	}

	private void FireGuns() {
		if (_currentAlienMoveDelay == 0)  {
			// Fire
			foreach (Gun gun in _guns) {
				GameObject newBullet = (GameObject)Instantiate(Resources.Load("Bullet"));
				newBullet.transform.position = gun.groundBlock.transform.position;
				newBullet.transform.Translate(0, 0.32f, 0);
			}
		} else {
			// Move existing bullets
			GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
			foreach (GameObject bullet in bullets) {
				bullet.transform.Translate(0, 0.08f, 0);
			}
		}
	}
}
