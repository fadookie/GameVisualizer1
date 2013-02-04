using UnityEngine;
using System;
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
	
	public bool disabled = false;
	public bool retrigger = false;
	public bool pauseWhenDisablingMovie = false;
	public FilterMode filterMode = FilterMode.Point;
	public float duration;
	public List<Material> materials = new List<Material>();
	public List<Material> titleCards = new List<Material>();

	HashSet<MovieTexture> _movies = new HashSet<MovieTexture>();
	MovieTexture _currentMovie;
	public bool visible = true; //Should we render? Set externally and by ToggleVisibility(). This ovverrides null material handling in CycleMaterial()
	uint _currentMaterialIndex = 0;
	
	public string toggleMovieKey = "m";
	public string toggleVisibilityKey = "v";
	public string togglePlaybackKey = "p";
	public string toggleCycleMaterialKey = "c";
	public string previousClipKey = ",";
	public string nextClipKey = ".";
	
	// Use this for initialization
	void Start () {
		//Sanity checks
		if (materials.Count < 1) {
			throw new System.Exception("At least one material must be assigned!");
		}
		
		Action<ICollection> processMovies = delegate(ICollection collection) {
			foreach (Material mat in collection) {
				if (null != mat && !(mat.mainTexture is MovieTexture)) {
					throw new System.Exception("All materials must have MovieTextures");
				}
				_movies.Add((MovieTexture)mat.mainTexture);
			}
		};
		processMovies(materials);
		processMovies(titleCards);
		
		//Cue up frames
		playAll();
		
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
		//Process input
		if (Input.GetKeyDown(toggleMovieKey)) {
			if (disabled) {
				disabled = false;
				renderer.enabled = true;
				play = true;
				visible = true;
			} else {
				disabled = true;
				renderer.enabled = false;
				if (pauseWhenDisablingMovie) play = false;
				visible = false;
			}
		}
		if (Input.GetKeyDown(toggleVisibilityKey)) {
			toggleVisibility = !toggleVisibility;
		}
		if (Input.GetKeyDown(togglePlaybackKey)) {
			togglePlayback = !togglePlayback;
		}
		if (Input.GetKeyDown(toggleCycleMaterialKey)) {
			cycleMaterial = !cycleMaterial;
		}
		if (Input.GetKeyDown(nextClipKey)) {
			CycleMaterialForward();
		} else if (Input.GetKeyDown(previousClipKey)) {
			CycleMaterialBackward();
		}
		
		renderer.enabled = visible;
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
	
	#region Playback control
	
	public void pauseAll() {
		foreach (MovieTexture movie in _movies) {
			movie.Pause();
		}
		play = false;
	}
	
	public void stopAll() {
		foreach (MovieTexture movie in _movies) {
			movie.Stop();
		}
		play = false;
	}
	
	/// <summary>
	/// You should probably never call this, just putting it in here for debugging
	/// </summary>
	public void playAll() {
		foreach (MovieTexture movie in _movies) {
			movie.Play();
		}
		play = true;
	}
	
	#endregion
	
	#region State control (called by GameManager)
	
	public void showTitleCard(string titleCardName) {
		stopAll();
		Material mat = null;
		foreach (Material m in titleCards) {
			if (m.name.Equals(titleCardName)) {
				mat = m;
				break;
			}
		}
		if (null != mat) {
			renderer.enabled = true;
			renderer.sharedMaterial = mat;
			_currentMovie = (MovieTexture)mat.mainTexture;
			_currentMovie.Play();
			play = true;
			visible = true;
		} else {
			Debug.Log(string.Format("Title card \"{0}\" not found.", titleCardName));
		}
	}
	
	public void visualizerMode(bool playNow) {
		stopAll();
		disabled = !playNow;
		play = playNow;
		visible = playNow;
		renderer.enabled = visible;
		renderer.sharedMaterial = materials[(int)(_currentMaterialIndex % materials.Count)];
		_currentMovie = (MovieTexture)renderer.sharedMaterial.mainTexture;
		if (!pauseWhenDisablingMovie) _currentMovie.Play();
	}
	
	#endregion
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
		if (!disabled && GameManager.GameState.Visualizer == GameManager.instance.gameState) {
			if (toggleVisibility && (++_visibilityCounter >= visibilityFrequency)) {
					_visibilityCounter = 0;
					ToggleVisibility();
			}
			if (cycleMaterial && (++_materialCounter >= materialFrequency)) {
					_materialCounter = 0;
					CycleMaterialForward();
			}
			if (togglePlayback && (++_playbackCounter >= playbackFrequency)) {
					_playbackCounter = 0;
					TogglePlayback();
			}
		}
	}
	
	void ToggleVisibility() {
		if (visible) {
			visible = false;
			renderer.enabled = false;
		} else if (null != renderer.sharedMaterial) {
			visible = true;
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
	
	void CycleMaterialForward() {
		Material oldMat   = materials[(int)(_currentMaterialIndex % materials.Count)];
		Material nextMat  = materials[(int)(++_currentMaterialIndex % materials.Count)];
		CycleMaterial(oldMat, nextMat);
	}
	
	void CycleMaterialBackward() {
		Material oldMat   = materials[(int)(_currentMaterialIndex % materials.Count)];
		Material nextMat  = materials[(int)(--_currentMaterialIndex % materials.Count)];
		CycleMaterial(oldMat, nextMat);
	}
	
	void CycleMaterial(Material oldMat, Material nextMat) {
		if (null == nextMat) {
			renderer.enabled = false; //No material is set in this slot, so just don't render
		} else {
			_currentMovie = (MovieTexture)nextMat.mainTexture;
			if (null == oldMat && visible) {
				renderer.enabled = true;
			}
		}
		renderer.sharedMaterial = nextMat;
		//Debug.Log(string.Format("renderer.sharedMaterial = {0} <- {1} (materials[{2}] / {3})", renderer.sharedMaterial, materials[_currentMaterialIndex % materials.Count], _currentMaterialIndex % materials.Count, materials.Count));
	}
	
	#endregion	
}
