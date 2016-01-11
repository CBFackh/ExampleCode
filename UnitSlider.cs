using UnityEngine;
using System.Collections;
using UnityEngine.UI;

//Out of date: Use in case of 2 slider menues
//[RequireComponent (typeof (UnitSliderStateBombers))]
//[RequireComponent(typeof(UnitSliderStateProphets))]

public class UnitSlider : MonoBehaviour {

	///<summary>
	///
	/// Basic Class for all Slider
	/// 
	///</summary>


	#region PROPERTIES
	//===========================================================

	// The Slider I controll
	private Slider _mySlider;
	public Slider MySlider {
		get {
			if (_mySlider == null) {
				_mySlider = GetComponent<Slider>();
			}
			return _mySlider;
		}

		private set {
			_mySlider = value;
		}
	}
	
	// The Type of Goblins my Slider handles.
	[SerializeField]
	private GoblinType _myGoblinType;
	public GoblinType MyGoblinType {
		get {
			return _myGoblinType;
		}
	}
	
	// Animator
	public Animator anim;
	// Particle Controller
	private SparkController sparkleController;
	// Interface images
	public GameObject sendImage, recruitImage;
	
	// Audio Components
	public AudioClip dragSound;
	public AudioClip stepSound;
	public AudioClip releaseSound;
	public AudioSource[] audio;
	public AudioSource audioLoop;
	public AudioSource audioStep;

	
	// Is my Slider available in Game?
	public bool _unlocked = true;
	public bool Unlocked {
		get {
			return _unlocked;
		}

		set {
			_unlocked = value;
			SetInteractable(value);
		}
	}
	
	// The time it takes before the slider is in default position again.
	[SerializeField]
	private float timeToReset;
	
	// Bool, whether the slider dies retract
	private bool retractSlider = false;
	// The amount, the Slider will be retracted per second.
	private float retractValue = 0f;

	// Amount of Sliders that are currently handled.
	private int OnTouchEventCounter = 0;

	// Setter for my Sliders value. If value is set 0 or less, reactivate all other Slider.
	public float SliderValue {
		get {
			return GetComponent<Slider>().value;
		}

		private set {
			GetComponent<Slider>().value = value;
			if (GetComponent<Slider>().value <= 0) {

				retractSlider = false;
				GetComponent<Slider>().value = 0;
				retractValue = 0f;
				SetAllSliderInteractive(true);

				if (AppManager.Instance.Game.GameState.IsRunning && OnTouchEventCounter > 0) {
					InputManager.OnTouchEnded -= ExecuteOrder;
					OnTouchEventCounter--;
				}
			}
		}
	}

	// The amount of Goblins that will be spent as resource
	[SerializeField]
	private Text _goblinResourceCount;
	public Text GoblinResourceCount {
		get {
			return _goblinResourceCount;
		}
	}

	// The amount of Scrap that will be spent as resource
	[SerializeField]
	private Text _scrapResourceCount;
	public Text ScrapResourceCount {
		get {
			return _scrapResourceCount;
		}
	}

	//
	[SerializeField]
	private Text _unitCount;
	public Text UnitCount {
		get {
			return _unitCount;
		}

		set {
			_unitCount = value;
		}
	}

	// Values to handle Sound Loops
	private float valueBefore;
	int framesWaited;

	//===========================================================
	#endregion

	#region UNITY_METHODS
	//===========================================================
	
	private void Start() {
		audio = GetComponents<AudioSource>();
		audioLoop = audio[0];
		audioStep = audio[1];
		sparkleController = transform.FindChild("Sparks", true).GetComponent<SparkController>();
		sparkleController.gameObject.SetActive(false);
		anim = GetComponent<Animator>();
	}

	private void Update() {
		if (retractSlider) {
			RetractSlider();
		}

		if (valueBefore == MySlider.value)
		{
			// Play Loop Sound for maximum of 3 Frames if Slider is not getting pulled or retracted
			framesWaited += 1;
			if (framesWaited >= 3)
			{
				audioLoop.Stop();
				framesWaited = 0;
			}
		}
	}

	public void SetInteractable(bool value) {
		if (!Unlocked)
		{
			MySlider.interactable = false;
			return;
		}
		MySlider.interactable = value;
	}

	//===========================================================
	#endregion

	#region METHODS
	//===========================================================
	
	/// <summary>
	/// Execution if player releases Slider.
	/// The Slider Execution is getting handled an the Slider retracts.
	/// </summary>
	/// <param name="hitUI"> Did the last Input hit an UI Element? </param>
	private void ExecuteOrder(bool hitUI) {
		audioLoop.Stop();
		//SetSound
		audioStep.volume = 0.75f;
		audioStep.clip = releaseSound;

		if (MySlider.value >= 0.5f)
		{
			//Start Loop
			audioStep.Play();
		}

		AppManager.Instance.Game.HandleUnitSlider(MyGoblinType, (int)SliderValue);
		retractValue = GetComponent<Slider>().value / timeToReset;
		retractSlider = true;
		sparkleController.StartAnimation();
		UpdateUnitCount();

		//TODO: WORKAROUND FOR TUTORIAL. Rework asap
		if (AppManager.Instance.showTutorial == true && Tutorial.instance.curStep.name == TutorialSteps.ProduceProphets) {
			Tutorial.ShowStep(TutorialSteps.ZoomOut);
		}

	}

	// Retracts Slider to default position. Plays Sound and Particle Effect
	private void RetractSlider() {
		SliderValue -= retractValue * Time.deltaTime;
		MySlider.enabled = true;
		if (SliderValue <= 0) {
			sparkleController.EndAnimation();
			anim.SetTrigger("Crush");
		}
	}

	/// <summary>
	/// When Slider is being dragged, deactivates other Slider and adds Execution to Event Handler.
	/// </summary>
	public void StartDrag() {
		SetOtherSliderInteractive(false, MySlider.GetComponent<UnitSlider>());
		if (AppManager.Instance.Game.GameState.IsRunning && OnTouchEventCounter == 0) {
			InputManager.OnTouchEnded += ExecuteOrder;			
			OnTouchEventCounter++;
		}
	}

	/// <summary>
	/// Activates or Deactivates all Sliders but one.
	/// </summary>
	/// <param name="value"> The value the Sliders shall be set to (true/false) </param>
	/// <param name="exception"> Slider that shall not be deactivated. </param>
	public void SetOtherSliderInteractive(bool value, UnitSlider exception) {
		foreach (UnitSlider slider in AppManager.Instance.Game.UnitSliders) {
			if (slider == exception) { continue; }
			slider.SetInteractable(value);
		}
	}

	// Dectivate Slider
	public void DisableSlider() {
		SetInteractable(false);
	}

	// Activate Slider
	public void EnableSlider() {
		SetInteractable(true);
	}

	/// <summary>
	/// Activates or Deactivates all Sliders.
	/// </summary>
	/// <param name="value"> The value the Sliders shall be set to (true/false) </param>
	public void SetAllSliderInteractive(bool value)
	{
		foreach (UnitSlider slider in AppManager.Instance.Game.UnitSliders)
		{
			slider.SetInteractable(value);
		}
	}

	// Shows amount of Resources that shall be required.
	public void UpdateResourceCount() {
		int goblinCount = (int)SliderValue;
		GoblinResourceCount.text = goblinCount.ToString();
	}

	// Shows amount of Goblins that shall be handled.
	public void UpdateUnitCount() {
		UnitCount.text = Game.CountUnitsInCamp(MyGoblinType).ToString();
	}

	/// <summary>
	/// Clamps the slider to the available amount of Goblins
	/// </summary>
	public void ClampSlider() {
		float minVal = 0.49f;
		if (MySlider.value >= minVal) {

			if (AppManager.Instance.Game.GameScreen is GameScreenStateMap) {
				if (AppManager.Instance.Game.CurSelection == null) {
					MySlider.value = Mathf.Clamp(MySlider.value, 0, minVal);
					return;
				}
				MySlider.value = Mathf.Clamp(MySlider.value, minVal, Mathf.Min(Game.CountUnitsInCamp(MyGoblinType) + minVal, MySlider.maxValue));
			}

			else if (AppManager.Instance.Game.GameScreen is GameScreenStateWorld) {
				MySlider.value = Mathf.Clamp(MySlider.value, minVal, Mathf.Min(Game.CountUnitsInCamp(GoblinType.BaseGoblin) + minVal, MySlider.maxValue));
			}
		}
	}

	// Switches UI Images to Recruiting State
	public void SwitchToRecruit() {
			sendImage.SetActive(false);
			recruitImage.SetActive(true);
	}

	// Switches UI Images to Sending State
	public void SwitchToSend() {
			sendImage.SetActive(true);
			recruitImage.SetActive(false);
	}

	// Plays Sound
	public void PlayDragSound()
	{
		framesWaited = 0;

		if (MySlider.value >= MySlider.maxValue)
		{
			audioLoop.Stop();
			return;
		}

		if ((int)valueBefore != (int)MySlider.value && audioLoop.isPlaying)
		{
			//SetSound
			audioStep.clip = stepSound;
			audioStep.volume = 0.55f;
			//Start Loop
			audioStep.Play();
		}

		if (MySlider.value > valueBefore && MySlider.value >= 0.5f && !audioLoop.isPlaying)
		{
			//Play Sound
			//audioLoop.volume = 0;
			audioLoop.Play();
			valueBefore = MySlider.value;
			return;
		}

		else if (MySlider.value <= valueBefore  && audioLoop.isPlaying)
		{
			//Stop Sound
			audioLoop.Stop();
		}
		valueBefore = MySlider.value;
	}
	//===========================================================
	#endregion

}
