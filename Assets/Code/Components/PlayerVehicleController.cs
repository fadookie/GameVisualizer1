using UnityEngine;
using System.Collections;
using System;

[RequireComponent (typeof(OTAnimatingSprite))]
public class PlayerVehicleController : Reactive {

	private float _speed = 0;
	private VehicleOrientation _orientation = VehicleOrientation.STRAIGHT;
	private VehicleOrientation _oldOrientation = VehicleOrientation.LEFT;
	private OTAnimatingSprite _sprite;
	private bool _startedPlaying = true; //simulate a stop event on init so sprite starts paused

	public enum VehicleOrientation {
		LEFT = 0,	
		RIGHT,
		STRAIGHT,
		UPHILL_LEFT,
		UPHILL_RIGHT,
		UPHILL_STRAIGHT
	}

	RoadController roadController;
	// Use this for initialization
	void Start () {
		roadController = transform.parent.gameObject.GetComponent<RoadController>();
		if (null == roadController) {
			throw new Exception("PlayerVehicleController attached to object not parented to a RoadController.");
		}
		roadController.registerPlayerVehicleController(this);
		_sprite = gameObject.GetComponent<OTAnimatingSprite>();
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		bool orientationChanged = false;
		if (!_orientation.Equals(_oldOrientation)) {
			//Debug.Log("orient THIS");
			orientationChanged = true;
			switch(_orientation) {
			/*
				case VehicleOrientation.LEFT:
					_sprite.animationFrameset = "left";
					break;
				case VehicleOrientation.RIGHT:
					_sprite.animationFrameset = "right";
					break;
				case VehicleOrientation.UPHILL_LEFT:
					_sprite.animationFrameset = "uphill_left";
					break;
				case VehicleOrientation.UPHILL_RIGHT:
					_sprite.animationFrameset = "uphill_right";
					break;
				case VehicleOrientation.UPHILL_STRAIGHT:
					_sprite.animationFrameset = "uphill_straight";
					break;
			*/
				case VehicleOrientation.STRAIGHT:
				default:
					_sprite.animationFrameset = "straight";
					break;
			}
		}
		
		//_sprite.speed = 1;//(_speed / 1000) * Time.deltaTime;
		if (orientationChanged || (!_startedPlaying && _speed > 0)) {
			_startedPlaying = true;
			_sprite.PlayLoop(_sprite.animationFrameset);
			//Debug.Log("sprite.PlayLoop s:" + _speed);
		} else if (_startedPlaying && _speed <= 0) {
			_sprite.Pauze();
			_startedPlaying = false;
			//Debug.Log("sprite.Pauze s:" + _speed);
		}
		
		_oldOrientation = _orientation;
	}
	
	void OnDestroy() {
		if (null != roadController) roadController.deregisterPlayerVehicleController(this);
	}
	
	#region RoadController event handlers
	
	public void VehicleUpdate(float speed, VehicleOrientation orientation) {
		_speed = speed;
		_orientation = orientation;
	}
	
	#endregion
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion
}
