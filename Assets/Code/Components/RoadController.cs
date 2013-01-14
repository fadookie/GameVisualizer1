using UnityEngine;
using System.Collections;

public class RoadController : Reactive {

	public Texture2D texture;
	private Rect _screenRect = new Rect(0, 0, Screen.width, Screen.height);
	public Material material;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
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
