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
	public bool cycleClip = false;
	public int visibilityFrequency = 1;
	public int playbackFrequency = 1;
	public int materialFrequency = 1;
	public int clipFrequency = 1;
	private int _visibilityCounter = 0;
	private int _playbackCounter = 0;
	private int _materialCounter = 0;
	private int _clipCounter = 0;
	
	public bool disabled = false;
	public bool visible = true; //Should we render? Set externally and by ToggleVisibility(). This ovverrides null material handling in CycleMaterial()
	public bool retrigger = false;
	public bool pauseWhenDisablingMovie = false;
	public FilterMode filterMode = FilterMode.Point;

	public List<MovieTexture> clips = new List<MovieTexture>();
	MovieTexture _currentMovie;
	uint _currentMaterialIndex = 0;
	uint _currentClipIndex = 0;
	
	public List<Material> materials = new List<Material>();
	
	public List<MovieTexture> titleCards = new List<MovieTexture>();
	public Material titleCardMaterial = null;
	
	public string toggleMovieKey = "m";
	public string toggleVisibilityKey = "v";
	public string togglePlaybackKey = "p";
	public string toggleCycleMaterialKey = "c";
	public string bumpModifierKeyA = "left shift";
	public string bumpModifierKeyB = "right shift";
	public string previousClipKey = ",";
	public string nextClipKey = ".";
	public string previousMaterialKey = ";";
	public string nextMaterialKey = "'";
	
	// Use this for initialization
	void Start () {
		//Sanity checks
		if (materials.Count < 1) {
			throw new System.Exception("At least one material must be assigned!");
		}
		
		/*
		Action<ICollection> processMovies = delegate(ICollection collection) {
			foreach (Material mat in collection) {
				if (null != mat && !(mat.mainTexture is MovieTexture)) {
					//throw new System.Exception("All materials must have MovieTextures");
					continue;
				}
			}
		};
		processMovies(materials);
		processMovies(titleCards);
		*/
		
		if (null == titleCardMaterial) throw new System.NullReferenceException("Title card material cannot be null.");
		if (clips.Count < 1) throw new Exception("At least one MovieTexture must be assigned.");
		
		//Cue up frames
		playAll();
		
		//Apply title card material as fallback
		renderer.sharedMaterial = titleCardMaterial;
		
		//Use movie 0 as fallback
		_currentMovie = clips[0];
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		//Process input
		
		//Cache Key events that are checked more than once
		bool bumpModifierKeyDown = Input.GetKey(bumpModifierKeyA) || Input.GetKey(bumpModifierKeyB);
		bool toggleVisibilityKeyDown = Input.GetKeyDown(toggleVisibilityKey);
		bool togglePlaybackKeyDown = Input.GetKeyDown(togglePlaybackKey);
		bool toggleCycleMaterialKeyDown = Input.GetKeyDown(toggleCycleMaterialKey);
		
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
		
		if (toggleVisibilityKeyDown && !bumpModifierKeyDown) {
			toggleVisibility = !toggleVisibility;
		} else if (toggleVisibilityKeyDown && bumpModifierKeyDown) {
			ToggleVisibility();
		}
		
		if (togglePlaybackKeyDown && !bumpModifierKeyDown) {
			togglePlayback = !togglePlayback;
		} else if (togglePlaybackKeyDown && bumpModifierKeyDown) {
			TogglePlayback();
		}
		
		if (toggleCycleMaterialKeyDown) {
			//Right now there's no need to overload this key since materials have their own controls
			cycleMaterial = !cycleMaterial;
		}
		
		if (Input.GetKeyDown(nextClipKey)) {
			CycleClipByAmount(1);
		} else if (Input.GetKeyDown(previousClipKey)) {
			CycleClipByAmount(-1);
		}
		
		if (Input.GetKeyDown(nextMaterialKey)) {
			CycleMaterialForward();
		} else if (Input.GetKeyDown(previousMaterialKey)) {
			CycleMaterialBackward();
		}
		
		//Process other settings
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
		foreach (MovieTexture movie in clips) {
			movie.Pause();
		}
		play = false;
	}
	
	public void stopAll() {
		foreach (MovieTexture movie in clips) {
			movie.Stop();
		}
		play = false;
	}
	
	/// <summary>
	/// You should probably never call this, just putting it in here for debugging
	/// </summary>
	public void playAll() {
		foreach (MovieTexture movie in clips) {
			movie.Play();
		}
		play = true;
	}
	
	#endregion
	
	#region Internal state management
	
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
	
	void CycleClipByAmount(int amount) {
		MovieTexture oldMovie 	= clips[(int)(_currentClipIndex % clips.Count)];
		_currentClipIndex = (uint)((int)_currentClipIndex + amount);
		MovieTexture newMovie 	= clips[(int)(_currentClipIndex % clips.Count)];
		oldMovie.Pause();
		renderer.material.mainTexture = newMovie;
		newMovie.Play();
		_currentMovie = newMovie;
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
			//_currentMovie = (MovieTexture)nextMat.mainTexture;
			nextMat.mainTexture = _currentMovie;
			if (null == oldMat && visible) {
				renderer.enabled = true;
			}
		}
		renderer.sharedMaterial = nextMat;
		//Debug.Log(string.Format("renderer.sharedMaterial = {0} <- {1} (materials[{2}] / {3})", renderer.sharedMaterial, materials[_currentMaterialIndex % materials.Count], _currentMaterialIndex % materials.Count, materials.Count));
	}
	
	#endregion
	
	#region External state control (called by GameManager)
	
	public void showTitleCard(string titleCardName) {
		stopAll();
		MovieTexture titleMovie = null;
		foreach (MovieTexture m in titleCards) {
			if (m.name.Equals(titleCardName)) {
				titleMovie = m;
				break;
			}
		}
		if (null != titleMovie) {
			renderer.enabled = true;
			renderer.sharedMaterial = titleCardMaterial;
			titleCardMaterial.mainTexture = titleMovie;
			_currentMovie = titleMovie;
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
		_currentMovie = clips[MathHelper.Mod((int)(_currentClipIndex), clips.Count)];
		Material currentMaterial = materials[(int)(_currentMaterialIndex % materials.Count)];
		currentMaterial.mainTexture = _currentMovie;
		renderer.sharedMaterial = currentMaterial;
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
	

	
	#endregion	
}
