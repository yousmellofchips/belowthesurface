using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {
	const float kDiggingHeight = 0.48f;
	const float kJumpHeight = 0.96f;
	const float kFallingStep = 0.16f;
	const int kDiggingDuration = 5;
	const int kHeavyMoveDuration = 20;

	private bool _left = false;
	private bool _right = false;
	private bool _jump = false;
	private bool _digging = false;
	private int _currentMovementDelay = 0;
	private int _diggingState = 0; //0 for off, 1 for first, 2 for second, 3 for final
	private int _currentStateDuration = 0;
	private int _currentJumpDuration = 0;
	private float _groundLevelY = 0f;
	private GameObject _camera;
	private int _currentDigDuration = 0;
	private float _currentDigGroundHeight = 0f;
	private int _heavyMoveDelay = 0;


	public float MovementStepSize = 0.64f;
	public int MovementDelay = 5;
	public int InitialJumpDelay = 30;
	public int SecondJumpDelay = 10;
	public int FinalJumpDelay = 5;
	public int SecondStateDuration = 3;
	public int JumpDuration = 20;

	// Use this for initialization
	void Start () {
		_groundLevelY = transform.position.y;

		_camera = GameObject.Find("Main Camera");
	}
	
	// Update is called once per frame
	void Update () {
		_left = Input.GetKey(KeyCode.LeftArrow);
		_right = Input.GetKey(KeyCode.RightArrow);
		if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.UpArrow)) {
			_jump = true;
		}
		if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.UpArrow)) {
			_jump = false;
		}
		if (Input.GetKeyDown(KeyCode.DownArrow)) {
			_digging = true;
		}
		if (Input.GetKeyUp(KeyCode.DownArrow)) {
			_digging = false;
		}

	}

	void FixedUpdate() {
		if (GameManager.Get().GameOver || GameManager.Get().YouWin) return;

		float x = transform.position.x;
		float y = transform.position.y;

		if (y > _groundLevelY - 0.64f) {
			if (!_jump) {
				groundPlayer(x, ref y, kFallingStep);
			}
		}

		if (_digging && _jump) { // not allowed
			groundPlayer(x, ref y);
		}

		if (!_digging) {
			HandleJump(x, ref y);
		}

		if (_left) {
			Vector2 rayDirection = new Vector3(-1f, 0, 0);
			ShiftGuns(rayDirection);
			if (checkForGroundBlock(x, y, rayDirection) == null) { // okay to move

				if (_currentMovementDelay == 0) {
					x += -MovementStepSize;
					_currentMovementDelay = MovementDelay;
					PlayMovementBeep();
				} else {
					_currentMovementDelay--;
				}
			}
		} else if (_right) {
			Vector2 rayDirection = new Vector3(1f, 0, 0);
			ShiftGuns(rayDirection);
			if (checkForGroundBlock(x, y, rayDirection) == null) { // okay to move

				if (_currentMovementDelay == 0) {
					x += MovementStepSize;
					_currentMovementDelay = MovementDelay;
					PlayMovementBeep();
				} else {
					_currentMovementDelay--;
				}
			}
		} else if (!_jump && _digging) {
			if (_currentMovementDelay == 0) {
				PlayDiggingBeep();

				if (_currentDigGroundHeight == 0) {
					_currentDigGroundHeight = y;
				}
				y = _currentDigGroundHeight + kDiggingHeight;

				switch (_diggingState) {
				case 0:
					_currentMovementDelay = InitialJumpDelay;
					_currentStateDuration = SecondStateDuration;
					_diggingState++;
					break;
				case 1:
					_currentMovementDelay = SecondJumpDelay;
					if (_currentStateDuration == 0) {
						_currentDigDuration = kDiggingDuration;
						_diggingState++;
					} else {
						_currentStateDuration--;
					}
					break;
				case 2:
					_currentMovementDelay = FinalJumpDelay; // stay in state until key release (or dig finishes)
					if (_currentDigDuration == 0) {
						// remove block, replace with gun, or if just ground, dig hole, drop player.
						GameObject ground = checkForGroundBlock(x,_groundLevelY, new Vector2(0,-1f));
						if (ground != null) {
							// check if gun block, change state to Gun... else
							GameManager.Gun gun = GameManager.Get().FindGunForGround(ground);
							if (gun != null) {
								GameManager.Get ().TransformIntoGun(gun);
							} else {
								Destroy (ground);
								groundPlayer(x, ref y);
							}
							_diggingState = 0;
							_currentDigGroundHeight = 0f;
						}

					} else {
						_currentDigDuration--;
					}
					break;
				}
			} else {
				_currentMovementDelay--;
				_currentDigGroundHeight = 0f;
			}
		} else {
			_currentMovementDelay = 0; // reset when no left/right key is held.
			_diggingState = 0;
			_currentDigGroundHeight = 0f;
		}

		x = Mathf.Max (x, -18.24f - 0.64f); // 6.08 x 3 multiples, plus one more character width since pos is from centre
		x = Mathf.Min (x, 18.24f + 0.64f);

		// Update camera
		float cameraX = _camera.transform.position.x;
		if ((cameraX - x) > 2.56f && x > -15.64f) { // at scroll trigger point?
			cameraX -= 0.64f;
			_camera.transform.position = new Vector3(cameraX, _camera.transform.position.y, -10);
		} else if ((x - cameraX) > 2.56f && x < 15.64f) {
			cameraX += 0.64f;
			_camera.transform.position = new Vector3(cameraX, _camera.transform.position.y, -10);
		}

		transform.position = new Vector3(x, y, 0f);
	}

	GameObject checkForGroundBlock(float x, float y, Vector2 rayDirection) {
		float rayDistance = 0.64f;
		return checkForGroundBlock(x, y, rayDirection, rayDistance);
	}

	GameObject checkForGroundBlock(float x, float y, Vector2 rayDirection, float rayDistance) {
		GameObject obj = null;

		var ray = new Vector2( x, y );
		Debug.DrawRay( ray, rayDirection * rayDistance, Color.red );
		var raycastHit = Physics2D.Raycast( ray, rayDirection, rayDistance, 1 << 9 ); // ground layer only
		if (raycastHit) {
			obj = raycastHit.collider.gameObject;
		}
		return obj;
	}

	void HandleJump(float x, ref float y) {
		if (_jump) {
			if (_currentJumpDuration == 0) {
				PlayDiggingBeep();
				y = _groundLevelY + kJumpHeight;
				_currentJumpDuration = JumpDuration;
			}
			else {
				_currentJumpDuration--;
				if (_currentJumpDuration == 0) {
					groundPlayer (x, ref y, kFallingStep);
					_currentJumpDuration--; // hack - to stop you from jumping until release key
					_jump = false;
				}
			}
		} else {
			_currentJumpDuration = 0;
			groundPlayer (x, ref y, kFallingStep);
			_jump = false;
		}
	}

	void groundPlayer (float x, ref float y) {
		groundPlayer(x, ref y, 0.64f);
	}

	void groundPlayer (float x, ref float y, float dropHeight)
	{
		Vector2 rayDirection = new Vector3 (0, -1f, 0);
		GameObject ground = checkForGroundBlock (x, y, rayDirection, 0.32f);
		if (ground != null) {
			y = ground.transform.position.y + 0.64f;
		} else {
			y -= dropHeight;
			y = Mathf.Max (y, _groundLevelY - 0.64f);
		}
	}

	void PlayMovementBeep() {
		AudioSource source = GetComponent<AudioSource>();
		source.PlayOneShot(source.clip);
	}

	void PlayDiggingBeep() {
		GameObject diggingComponent = GameObject.Find("jumpBeep");
		AudioSource source = diggingComponent.GetComponent<AudioSource>();
		audio.Stop();
		audio.PlayOneShot(source.clip, 0.5f);
	}

	// Player can shift any number of linked guns (TODO: or maybe a max of three?)
	// Call this when a block is present, and left and right is pressed, and "heavypushcount" is zero.
	void ShiftGuns(Vector2 shiftDirection) {
		if (_heavyMoveDelay > 0) {
			_heavyMoveDelay--;
			return;
		}
		_heavyMoveDelay = kHeavyMoveDuration;

		float rayDistance = 0.32f;

		// Create a list of guns to process (empty)
		List<GameManager.Gun> gunsToShift = new List<GameManager.Gun>();

		float x = gameObject.transform.position.x + ((0.02f + rayDistance) * shiftDirection.x); // needs fudge factor for float :(
		float y = gameObject.transform.position.y;

		// Hit GUN CHECK:-
		while (true) {
			var rayStart = new Vector2( x, y );

			// Temp ray will leave play area?
			float newX = x + rayDistance * shiftDirection.x;
			if ((Mathf.Sign(newX) * newX) >= (18.88f + 0.64f)) {
				// Stop and cancel - cannot move, at edge of play area.
				return;
			}

			// Cast ray from player, store as temp ray.
			Debug.DrawRay( rayStart, shiftDirection * rayDistance, Color.yellow );
			var raycastHit = Physics2D.Raycast( rayStart, shiftDirection, rayDistance, 1 << 9 ); // ground layer only
				
			// Temp ray finds no gun?
			if (!raycastHit) {
				// Stop and jump to hit gun processing.
				Debug.Log("Found gap - handling guns to move");
				break;
			}

			GameManager.Gun gun = GameManager.Get ().FindGunForGround(raycastHit.collider.gameObject); // TODO: SLOW!
			if (gun == null) {
				// Stop and cancel - obstacle found.
				Debug.Log("No gun found for object");
				return;
			}

			// Hits a gun? Add gun to list of hits.
			gunsToShift.Add(gun);
			Debug.Log("Gun added");

			// Cast ray from gun.
			// Store as temp ray
			// back to Hit GUN CHECK
			x += shiftDirection.x * 0.64f; // skip a block
		}
		
		// Hit Gun Processing
		Vector2 downwards = new Vector2(0, -1f);

		// Work through each gun
		foreach (GameManager.Gun gun in gunsToShift) {

			// Move left/right by 0.64f (block size)
			gun.groundBlock.transform.Translate(0.64f * shiftDirection.x, 0, 0);

			// next downwards ray off play area?
			float newY = y + rayDistance * downwards.y;
			if (newY < -3.52) {
				// skip to next gun
				Debug.Log("Skipping gun");
				continue;
			}

			// Cast ray downwards from gun
			var rayStart = new Vector2( gun.groundBlock.transform.position.x,
			                           gun.groundBlock.transform.position.y + (0.02f + rayDistance * downwards.x) ); //fudge
			Debug.DrawRay( rayStart, downwards * rayDistance, Color.blue );
			var raycastHit = Physics2D.Raycast( rayStart, downwards, rayDistance, 1 << 9 ); // ground layer only

			// hit block/gun?
			if (!raycastHit) {
				// if no, move down by block size (0.64f)
				gun.groundBlock.transform.Translate(0, -0.64f, 0);
			}
		}
	}
}
