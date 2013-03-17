using UnityEngine;
using System.Collections;

public class ShooterPlayerController : Reactive {
	public string horizontalAxisName = "Horizontal";
	public string verticalAxisName = "Vertical";
	public float moveAmount = 3.5f;
	OTAnimatingSprite sprite = null;

	// Use this for initialization
	void Start () {
		sprite = gameObject.GetComponent<OTAnimatingSprite>();
		if (sprite == null) throw new System.Exception("Sprite cannot be null.");
		sprite.onCollision = OnCollision;
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		float horizontalAxis = Input.GetAxis(horizontalAxisName);
		float verticalAxis = Input.GetAxis(verticalAxisName);
		
		horizontalAxis *= moveAmount;
		verticalAxis *= moveAmount;
		
		sprite.position = new Vector2(sprite.position.x + horizontalAxis, sprite.position.y + verticalAxis);
	}
	
	#region Collision handlers
	
	public void OnCollision(OTObject owner) {
		Debug.Log(string.Format("Player onCollision: {0}", owner.collisionObject.ToString()));
	}
	
	#endregion
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion	
}
