using UnityEngine;
using System.Collections;

public class RoadController : Reactive {

	private Rect _screenRect = new Rect(0, 0, Screen.width, Screen.height);
	private Texture2D _texture;

	// Use this for initialization
	void Start () {
		_texture = new Texture2D(320, 112);
		_texture.filterMode = FilterMode.Point;
		guiTexture.texture = _texture;
	}
	
	// Update is called once per frame
	void Update () {
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
