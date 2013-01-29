using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof(MeshRenderer))]
public class MovieController : Reactive {

	public bool play = false;
	public bool loop = false;
	
	public bool toggleVisibility = false;
	public bool togglePlayback = false;
	public bool cycleMaterial = false;
	public int visibilityFrequency = 1;
	public int playbackFrequency = 1;
	public int materialFrequency = 1;
	private int _visibilityCounter = 0;
	private int _playbackCounter = 0;
	private int _materialCounter = 0;
	
	public bool retrigger = false;
	public FilterMode filterMode = FilterMode.Point;
	public float duration;
	public List<Material> materials = new List<Material>();

	MovieTexture movie;
	MeshRenderer renderer;
	bool _visible = true; //Is ToggleVisibility() in effect? This ovverrides null material handling in CycleMaterial()
	uint _currentMaterialIndex = 0;
	// Use this for initialization
	void Start () {
		renderer = gameObject.GetComponent<MeshRenderer>();
		
		//Sanity checks
		if (materials.Count < 1) {
			throw new System.Exception("At least one material must be assigned!");
		}
		foreach (Material mat in materials) {
			if (null != mat && !(mat.mainTexture is MovieTexture)) {
				throw new System.Exception("All materials must have MovieTextures");
			}
		}
		
		_visible = renderer.enabled;
		
		//Apply material 0
		renderer.sharedMaterial = materials[0];
		if (!(renderer.material.mainTexture is MovieTexture)) {
			throw new System.Exception("Texture must be a MovieTexture");
		}
		
		movie = renderer.material.mainTexture as MovieTexture;
		duration = movie.duration;
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		if (loop != movie.loop) {
			movie.loop = loop;
		}
		if (filterMode != movie.filterMode) {
			movie.filterMode = filterMode;
		}
		if (play && !movie.isPlaying) {
			movie.Play();	
		} else if (!play && movie.isPlaying) {
			movie.Pause();
		}
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
		if (toggleVisibility) {
			if (++_visibilityCounter >= visibilityFrequency) {
				_visibilityCounter = 0;
				ToggleVisibility();
			}
		}
		if (cycleMaterial) {
			if (++_materialCounter >= materialFrequency) {
				_materialCounter = 0;
				CycleMaterial();
			}
		}
		if (togglePlayback) {
			if (++_playbackCounter >= playbackFrequency) {
				_playbackCounter = 0;
				TogglePlayback();
			}
		}
	}
	
	void ToggleVisibility() {
		if (_visible) {
			_visible = false;
			renderer.enabled = false;
		} else if (null != renderer.sharedMaterial) {
			_visible = true;
			renderer.enabled = true;
		}
	}
	
	void TogglePlayback() {
		if (movie.isPlaying) {
			play = false;
			if (!retrigger) {
				movie.Pause();
			} else {
				movie.Stop();
			}
		} else {
			play = true;
			movie.Play();
		}
	}
	
	void CycleMaterial() {
		Material oldMat   = materials[(int)(_currentMaterialIndex % materials.Count)];
		Material nextMat  = materials[(int)(++_currentMaterialIndex % materials.Count)];
		if (null == nextMat) {
			renderer.enabled = false; //No material is set in this slot, so just don't render
		} else if (null == oldMat && _visible) {
			renderer.enabled = true;
		}
		renderer.sharedMaterial = nextMat;
		//Debug.Log(string.Format("renderer.sharedMaterial = {0} <- {1} (materials[{2}] / {3})", renderer.sharedMaterial, materials[_currentMaterialIndex % materials.Count], _currentMaterialIndex % materials.Count, materials.Count));
	}
	
	#endregion	
}
