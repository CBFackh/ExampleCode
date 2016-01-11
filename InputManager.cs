using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InputFocus {
	public ITouchable myTouchable;
	public int fingerID;

	public InputFocus(int fingerID) {
		this.fingerID = fingerID;
		myTouchable = null;
	}

	public void SetFocus(ITouchable target) {
		myTouchable = target;
	}
}


public class InputManager : MonoBehaviour {
	///<summary>
	///
	/// Class Definition and Overview
	/// 
	///</summary>

	#region PROPERTIES
	//===========================================================
	private List<InputFocus> inputFocus;

	float distance;
	enum directions {
		IN,
		OUT,
		NONE
	}
	directions direction = directions.NONE;

	bool inputHandled = false;
	//===========================================================
	#endregion

	#region EVENTS
	//===========================================================

	//Input Events
	public delegate void TouchBegan(bool hitUI);
	public static event TouchBegan OnTouchBegan;

	public delegate void TouchEnded(bool hitUI);
	public static event TouchEnded OnTouchEnded;

	public delegate void TouchCanceled(bool hitUI);
	public static event TouchCanceled OnTouchCanceled;

	//===========================================================
	#endregion

	#region UNITY_METHODS
	//===========================================================

	private void Start() {
		inputFocus = new List<InputFocus>();
	}

	private void Update() {


#if UNITY_EDITOR
		//If I am in Editor only read Mouse Inputs
		HandleMouseInput();
		return;
#endif

#if UNITY_ANDROID
		if (Input.touchCount >= 1) {
			HandleTouchInput();
		}
		return;
#endif

#if UNITY_IOS
		if (Input.touchCount >= 1) {
			HandleTouchInput();
		}
		return;
#else
		HandleMouseInput();
#endif
	}

	//===========================================================
	#endregion

	#region METHODS
	//===========================================================

	/// <summary>
	/// Reads all necessary Mouse Inputs and handles them.
	/// </summary>
	private void HandleMouseInput() {

		if (Input.GetMouseButtonDown(0)) {
			inputFocus.Add(new InputFocus(0));
			HandleInputBegan(0, Input.mousePosition);
		}

		if (Input.GetMouseButtonUp(0)) {
			HandleTouchEnded(0, Input.mousePosition);
			RemoveTouchFocus(0);
		}

		if (Input.GetAxis("Mouse ScrollWheel") > 0) {
			AppManager.Instance.ZoomIn(100);
		}

		if (Input.GetAxis("Mouse ScrollWheel") < 0) {
			AppManager.Instance.ZoomOut(100);
		}
	}

	/// <summary>
	/// Read all necessary Touches and handles them.
	/// </summary>
	private void HandleTouchInput() {

		//Read and handle every touch as single touch
		foreach (Touch touch in Input.touches) {

			if (touch.phase == TouchPhase.Began) {
				inputFocus.Add(new InputFocus(touch.fingerId));
				HandleInputBegan(touch.fingerId, touch.position);
			}

			else if (touch.phase == TouchPhase.Ended) {
				HandleTouchEnded(touch.fingerId, touch.position);
				RemoveTouchFocus(touch.fingerId);
			}
		}

		//Read and handle multiply touches
		if (Input.touchCount >= 2) {
			HandlePinchInput();
		}
	}

	/// <summary>
	/// Reads the first two touche positions and handles them.
	/// </summary>
	//TODO: Move distance handling etc to Game Logic?
	private void HandlePinchInput() {
		float prevTouchDeltaMag = 0;
		float touchDeltaMag = 0;
		float deltaMagnitudeDifference = 0;
		Touch touchZero = Input.GetTouch(0);
		Touch touchOne = Input.GetTouch(1);

		Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
		Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

		prevTouchDeltaMag = ( touchZeroPrevPos - touchOnePrevPos ).magnitude;
		touchDeltaMag = ( touchZero.position - touchOne.position ).magnitude;

		deltaMagnitudeDifference = prevTouchDeltaMag - touchDeltaMag;

		if (Input.touchCount >= 2) {
			if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended) {
				distance = 0;
				direction = directions.NONE;
			}
		}

		if (Mathf.Abs(distance) < 10) {
			distance += deltaMagnitudeDifference;
		}

		if (prevTouchDeltaMag > touchDeltaMag) {
			//Zoom Out (-)
			if (distance >= 10) {
				if (direction == directions.NONE || direction == directions.OUT) {
					distance += deltaMagnitudeDifference;
					AppManager.Instance.ZoomOut(Mathf.Abs(this.distance));
					direction = directions.OUT;
				}
			}
		}

		else if (prevTouchDeltaMag < touchDeltaMag) {
			//Zoom In (+)
			if (distance <= -10) {
				if (direction == directions.NONE || direction == directions.IN) {
					distance += deltaMagnitudeDifference;
					AppManager.Instance.ZoomIn(Mathf.Abs(this.distance));
					direction = directions.IN;
				}
			}
		}

	}

	/// <summary>
	/// Send Event Raycast to UI and check if UI was hit.
	/// </summary>
	/// <param name="position">Screen Position where to check.</param>
	/// <returns></returns>
	private bool CheckForUI(Vector2 position) {
		foreach (RaycastResult hit in CastScreenRay(position, Color.red)) {
			if (hit.gameObject.GetComponent<CanvasRenderer>()) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Casts a Ray from a ScreenPosition into the UI and draws a Debug Ray.
	/// </summary>
	/// <param name="position">Screen Position where to check.</param>
	/// <param name="color">Color for Debug Ray</param>
	/// <returns></returns>
	private List<RaycastResult> CastScreenRay(Vector2 position, Color color) {
		//Create a Ray from TouchInput
		Ray screenRay = AppManager.Instance.CurCamera.ScreenPointToRay(position);
		//Draw a Ray in Unity Editor
#if UNITY_EDITOR
		Debug.DrawRay(screenRay.origin, screenRay.direction * 100, color);
#endif

		//Create EventPointer, set it's Position to Touch position
		PointerEventData pointer = new PointerEventData(EventSystem.current);
		pointer.position = position;

		//Create List and fill list with result from Raycastall (Eventcast)
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointer, results);

		return results;
	}

	/// <summary>
	/// Casts a Ray from a ScreenPosition into the WORLD and draws a Debug Ray.
	/// </summary>
	/// <param name="position">Screen Position where to check.</param>
	/// <param name="color">Color for Debug Ray</param>
	/// <returns></returns>
	private RaycastHit[] CastWorldRay(Vector2 position, Color color) {
		//Create a Ray from TouchInput
		Ray screenRay = AppManager.Instance.CurCamera.ScreenPointToRay(position);
		//Draw a Ray in Unity Editor
#if UNITY_EDITOR
		Debug.DrawRay(screenRay.origin, screenRay.direction * 100, color);
#endif
		RaycastHit[] hits;
		hits = Physics.RaycastAll(screenRay);
		return hits;
	}

	/// <summary>
	/// Handles the beginning of an Input.
	/// </summary>
	/// <param name="fingerID">ID of current Input.</param>
	/// <param name="position">position of current Input.</param>
	private void HandleInputBegan(int fingerID, Vector2 position) {
		bool hitUI = false;
		bool interacted = false;

		//Hit an UI Element (contains Canvas Renderer)?
		hitUI = CheckForUI(position);

		if (hitUI) {
			//Send TouchEnded Event with information that UI was hit
			if (OnTouchBegan != null) {
				OnTouchBegan(hitUI);
			}
			//Cancel Input Handle because UI was hit.
			return;
		}

		//Send Raycast, get GameObjects
		foreach (RaycastHit hit in CastWorldRay(position, Color.green)) {

			//Get ITouchable, if true trigger touch on gameObject
			ITouchable touchObject = hit.collider.gameObject.GetComponent<ITouchable>();
			if (touchObject != null) {
				GetTouchFocus(fingerID).SetFocus(touchObject);
				touchObject.InputStart();
				interacted = true;
			}

			//NOTE: Stop, because only one object shall be hit!
			if (interacted) {
				break;
			}
		}

		//Send OnTouch Event with information that no UI was hit.
		if (OnTouchBegan != null && !interacted) {
			OnTouchBegan(hitUI);
		}
	}

	/// <summary>
	/// Handles the end of an Input.
	/// </summary>
	/// <param name="fingerID">ID of current Input.</param>
	/// <param name="position">Position of current Input.</param>
	private void HandleTouchEnded(int fingerID, Vector2 position) {
		bool hitUI = false;
		bool interacted = false;

		//Hit an UI Element (contains Canvas Renderer)?
		hitUI = CheckForUI(position);

		if (hitUI) {
			//Send TouchEnded Event with information that UI was hit
			if (OnTouchEnded != null) {
				OnTouchEnded(hitUI);
			}
			//Cancel Input Handle because UI was hit.
			return;
		}

		//Send Raycast, get GameObjects
		foreach (RaycastHit hit in CastWorldRay(position, Color.yellow)) {

			//Get ITouchable, if true trigger touch on gameObject
			ITouchable touchObject = hit.collider.gameObject.GetComponent<ITouchable>();

			if (touchObject != null && inputFocus.Count > 0) {
				if (GetTouchFocus(fingerID).myTouchable == touchObject) {
					touchObject.InputEnd();
					interacted = true;
				}
			}

			//NOTE: Stop, because only one object shall be hit!
			if (interacted) {
				break;
			}
		}

		//Send OnTouch Event with information that no UI was hit.
		if (OnTouchEnded != null && !interacted) {
			OnTouchEnded(hitUI);
		}
	}
	
	/// <summary>
	/// Removes Input with current ID from Input List.
	/// </summary>
	/// <param name="fingerID">ID of Input that will be removed.</param>
	private void RemoveTouchFocus(int fingerID) {
		for (int i = 0; i < inputFocus.Count; i++) {
			if (inputFocus[i].fingerID == fingerID) {
				inputFocus.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Returns Input with current ID.
	/// </summary>
	/// <param name="fingerID">ID of Input that will be searched.</param>
	/// <returns></returns>
	private InputFocus GetTouchFocus(int fingerID) {
		foreach (InputFocus focus in inputFocus) {
			if (focus.fingerID == fingerID) {
				return focus;
			}
		}
		return new InputFocus(fingerID);
	}
	//===========================================================
	#endregion

}
