using UnityEngine;
using System.Collections;

[RequireComponent (typeof(GUITexture))]
public class RoadController : Reactive {

	
	private Rect _screenRect = new Rect(0, 0, Screen.width, Screen.height);
	private Texture2D _texture;
	//var segments      = [];                      // array of road segments
	public float roadHalfWidth = 2000; // half the roads width, easier math if the road spans from -roadWidth to +roadWidth
	public float segmentLength = 200; // length of a single segment
	public float rumbleLength = 3;  // number of segments per red/white rumble strip
	public int lanes = 3; // number of lanes
	public float fieldOfView = 100; // angle (degrees) for field of view
	public float cameraHeight = 1000; // z height of camera
	public float cameraDepth; // z distance camera is from screen (computed)
	public float drawDistance = 300; // number of segments to draw
	public float playerXOffset = 0; // player x offset from center of road (-1 to 1 to stay independent of roadWidth)
	public float playerZOffset; // player relative z distance from camera (computed)
	public float fogDensity = 5; // exponential fog density
	public float cameraZPosition = 0; // current camera Z position (add playerZ to get player's absolute Z position)
	public float speed = 0; // current speed
	public float maxSpeed = 12000; // top speed (ensure we can't move more than 1 segment in a single frame to make collision detection easier)
	public float accel; // acceleration rate - tuned until it 'felt' right
	public float breaking; // deceleration rate when braking
	public float decel; // 'natural' deceleration rate when neither accelerating, nor braking
	public float offRoadDecel; // off road deceleration is somewhere in between
	public float offRoadLimit; // limit when off road deceleration no longer applies (e.g. you can always go at least this speed even when off road)

	// Use this for initialization
	void Start () {
		accel =  maxSpeed / 5;
		breaking = -maxSpeed;
		decel = -maxSpeed/5;
		offRoadDecel = -maxSpeed/2;
		offRoadLimit = maxSpeed/4;
		
		_texture = new Texture2D(320, 112);
		_texture.filterMode = FilterMode.Point;
		guiTexture.texture = _texture;
	}
	
	// Update is called once per frame
	void Update () {
		cameraZPosition += speed * Time.deltaTime;	
		float dx = Time.deltaTime * 2 * (speed / maxSpeed); // at top speed, should be able to cross from left to right (-1 to 1) in 1 second
		
		if (Input.GetKey(KeyCode.LeftArrow)) {
			playerXOffset -= dx;
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			playerXOffset += dx;
		}
		
		if (Input.GetKey(KeyCode.UpArrow)) {
			speed += accel * Time.deltaTime;
		} else if (Input.GetKey(KeyCode.DownArrow)) {
			speed += breaking * Time.deltaTime;
		} else {
			speed += decel * Time.deltaTime;
		}
		
		if (((playerXOffset < -1) || (playerXOffset > 1)) && (speed > offRoadLimit)) {
			speed += offRoadDecel * Time.deltaTime;
		}
		
		playerXOffset = Mathf.Clamp(playerXOffset, -2, 2); // dont ever let player go too far out of bounds
		speed = Mathf.Clamp(speed, 0, maxSpeed); // or exceed maxSpeed
		
		Render();
	}
	
	void Render() {
		if (null != guiTexture) {
			for (int x = 0; x < _texture.width; x++) {
				for (int y = 0; y < _texture.height; y++) {
					_texture.SetPixel(x, y, new Color(Random.value, Random.value, Random.value, 1));
				}
			}
			_texture.Apply();
		}
	}
	
	void OnRenderObject() {
	/*
		if (null != texture && null != material) {
			GL.PushMatrix();
			Debug.Log(texture);
			GL.LoadPixelMatrix(0, Camera.main.pixelWidth, Camera.main.pixelHeight, 0);
//			GL.LoadPixelMatrix (0, 512, 512, 0); 
			Graphics.DrawTexture(_screenRect, texture, material);
			GL.PopMatrix();
		}
		*/
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion
}
