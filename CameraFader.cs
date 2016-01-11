using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CameraFader : MonoBehaviour {

	private RawImage overlay;

	private float fadeTime = 1f;
	private Camera camFrom, camTo;

	private bool fading = false;
	private bool zoomIn;

	private float prevCamSize;

	// Use this for initialization
	void Start () {
		overlay = GetComponent<RawImage>();
	}

	#region UNITY_METHODS
	//===========================================================

	private void Update() {

		if (fading) {
			DoFade();
		}
	}

	//===========================================================
	#endregion


	#region METHODS
	//===========================================================
	public void FadeCamera(Camera camFrom, Camera camTo, float fadeTime, bool zoomIn) {
		//Only do if not fading yet.
		if (!fading) {
			//State fading
			this.fadeTime = fadeTime;
			this.camFrom = camFrom;
			this.camTo = camTo;
			this.zoomIn = zoomIn;
			prevCamSize = camFrom.orthographicSize;

			camTo.enabled = true;
			camTo.targetTexture = null;
			SetTexture();
			camFrom.targetTexture = overlay.texture as RenderTexture;
			overlay.color = Color.white;
			overlay.enabled = true;

			StartFade();
		}
	}

	private void StartFade() {
		fading = true;
		gameObject.SetActive( true );
		this.enabled = true;
		overlay.color = Color.white;
	}

	private void DoFade() {
		Color newAlpha = overlay.color;
		newAlpha.a -= 1 / fadeTime * Time.deltaTime;

		if (zoomIn) {
			camFrom.orthographicSize *= 0.99f;
		}

		else {
			camFrom.orthographicSize *= 1.01f;
		}

		if (newAlpha.a <= 0) {
			EndFade();
			return;
		}

		overlay.color = newAlpha;
	}

	private void EndFade() {
		fading = false;
		overlay.enabled = false;
		ClearTexture();
		camFrom.orthographicSize = prevCamSize;

	}

	private void SetTexture() {
		RenderTexture tex = new RenderTexture( Screen.width, Screen.height, 24 );
		overlay.texture = tex;
	}

	private void ClearTexture() {
		RenderTexture oldTex = overlay.texture as RenderTexture;
		if (oldTex != null) {
			oldTex.Release();
		}
	}


	//===========================================================
	#endregion
}
