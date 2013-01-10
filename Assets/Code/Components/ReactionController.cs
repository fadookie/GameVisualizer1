/// <summary>
/// This script is intended to be the conduit through which all audio synchronization-based events flow through.
/// It may need to be forked or subclassed later for special-case logic on certain game objects.
/// </summary>

using UnityEngine;
using System.Collections;

public class ReactionController : Reactive {

	public bool amplitudeControlsColor = false;
	public Color underAmplitudeThresholdColor = Color.green;
	public Color overAmplitudeThresholdColor = Color.red;
	
	public bool amplitudeControlsSize = false;
	public float minSizeMultiplier = 1.0f;
	public float maxSizeMultiplier = 100.0f;
	
	public bool beatPulsesSize = false;
	public float beatPulseDuration = 0.5f;
	private bool pulseInProgress = false;
	
	private Vector3 defaultScale;

	// Use this for initialization
	void Start () {
		ReactiveManager.Instance.registerListener(this, getChannels());
		defaultScale = new Vector3(gameObject.transform.lossyScale.x, gameObject.transform.lossyScale.y, gameObject.transform.lossyScale.z);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
		//Color based on threshold
		if (amplitudeControlsColor) {
			if (overThreshold) {
				gameObject.GetComponent<OTAnimatingSprite>().tintColor = overAmplitudeThresholdColor;
				//gameObject.renderer.material.color = overAmplitudeThresholdColor;
			} else {
				gameObject.GetComponent<OTAnimatingSprite>().tintColor = underAmplitudeThresholdColor;
				//gameObject.renderer.material.color = underAmplitudeThresholdColor;
			}
		}
		
		//Sprite size based on amplitude
		if (amplitudeControlsSize) {
			float newScale = MathHelper.Map(amp, -0.5f, 0.5f, minSizeMultiplier, maxSizeMultiplier);
			gameObject.transform.localScale = new Vector3(defaultScale.x * newScale, defaultScale.y * newScale, defaultScale.z * newScale);
		}
	}
	
	public override void reactToBeat(float currentBPM) {
		Debug.Log("beat recieved, bpm = " + currentBPM);
		if (beatPulsesSize) {
			gameObject.transform.localScale = new Vector3(defaultScale.x * maxSizeMultiplier, defaultScale.y * maxSizeMultiplier, defaultScale.z * maxSizeMultiplier);
			if (pulseInProgress) {
				CancelInvoke();
			}
			Invoke("resetSize", beatPulseDuration);
			pulseInProgress = true;
		}
	}
	
	void resetSize() {
		pulseInProgress = false;
		gameObject.transform.localScale = new Vector3(defaultScale.x, defaultScale.y, defaultScale.z);
	}
	
	#endregion
}
