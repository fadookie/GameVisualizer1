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
	public float minSize = 1.0f;
	public float maxSize = 100.0f;

	// Use this for initialization
	void Start () {
		ReactiveManager.Instance.registerListener(this, getChannels());
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
		}
	}
	
	#endregion
}
