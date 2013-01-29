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

	HashSet<MovieTexture> _movies = new HashSet<MovieTexture>();
	MovieTexture _currentMovie;
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
			_movies.Add((MovieTexture)mat.mainTexture);
		}
		
		_visible = renderer.enabled;
		
		//Apply material 0
		renderer.sharedMaterial = materials[0];
		if (!(renderer.material.mainTexture is MovieTexture)) {
			throw new System.Exception("Texture must be a MovieTexture");
		}
		
		_currentMovie = renderer.material.mainTexture as MovieTexture;
		duration = _currentMovie.duration;
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		if (loop != _currentMovie.loop) {
			_currentMovie.loop = loop;
		}
		if (filterMode != _currentMovie.filterMode) {
			_currentMovie.filterMode = filterMode;
		}
		if (play && !_currentMovie.isPlaying) {
			_currentMovie.Play();	
		} else if (!play && _currentMovie.isPlaying) {
			_currentMovie.Pause();
		}
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
		if (toggleVisibility && (++_visibilityCounter >= visibilityFrequency)) {
				_visibilityCounter = 0;
				ToggleVisibility();
		}
		if (cycleMaterial && (++_materialCounter >= materialFrequency)) {
				_materialCounter = 0;
				CycleMaterial();
		}
		if (togglePlayback && (++_playbackCounter >= playbackFrequency)) {
				_playbackCounter = 0;
				TogglePlayback();
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
		if (_currentMovie.isPlaying) {
			play = false;
			if (!retrigger) {
				_currentMovie.Pause();
			} else {
				_currentMovie.Stop();
			}
		} else {
			play = true;
			_currentMovie.Play();
		}
	}
	
	void CycleMaterial() {
		Material oldMat   = materials[(int)(_currentMaterialIndex % materials.Count)];
		Material nextMat  = materials[(int)(++_currentMaterialIndex % materials.Count)];
		if (null == nextMat) {
			renderer.enabled = false; //No material is set in this slot, so just don't render
		} else {
			_currentMovie = (MovieTexture)nextMat.mainTexture;
			if (null == oldMat && _visible) {
				renderer.enabled = true;
			}
		}
		renderer.sharedMaterial = nextMat;
		//Debug.Log(string.Format("renderer.sharedMaterial = {0} <- {1} (materials[{2}] / {3})", renderer.sharedMaterial, materials[_currentMaterialIndex % materials.Count], _currentMaterialIndex % materials.Count, materials.Count));
	}
	
	#endregion	
}
