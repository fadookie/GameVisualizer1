using UnityEngine;
using System.Collections;

public class ShooterController : Reactive {

	// Use this for initialization
	void Start () {
		ReactiveManager.Instance.registerListener(this, getChannels());
		Debug.Log("This is your mom's shooter ;)");
		GameObject enemySprite =  OT.CreateObject("SpaceInvader1");
		enemySprite.transform.parent = transform;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion
}
