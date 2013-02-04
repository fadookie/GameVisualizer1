/// <summary>
/// Master class that manages the 'game'
/// </summary>
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SongPreset {
	public string songName;
	public string notes;
	public string titleCardName;
	public int BPM;
	public bool autoShowTitleCard = true;
	public bool playMovieOnStart;
	public bool toggleVisibility;
	public bool togglePlayback;
	public bool cycleMaterial;
	public int visibilityFrequency = 1;
	public int playbackFrequency = 1;
	public int materialFrequency = 1;
	
	public override string ToString() {
		return string.Format("SongPreset{{name:{0}, notes:{1}, titleCardName:{2}, BPM:{3}}}", songName, notes, titleCardName, BPM);
	}
}
	
public class GameManager : MonoSingleton<GameManager>
{
	public GameState gameState = GameState.TitleCard;
	private GameState _gameState; //Actual GameState so we can detect changes in the editor
	public int _currentPreset = 0;
	public SongPreset currentPreset {
		get { return songPresets[_currentPreset]; }
	}
	public SongPreset[] songPresets;
	public string skipBackKey = "[";
	public string skipForwardKey = "]";
	public string toggleTitleKey = "t";
	private bool _initialUpdate = true;
	
	GameObject _roadControllerObject = null;
	
	public enum GameState {
		TitleCard = 0,
		Visualizer
	}
	
	
	public override void Init() {
	}
	
	void Start() {
		_roadControllerObject = GameObject.FindGameObjectWithTag("RoadController");
	}
	
	void Update() {
		//Process input
		bool presetChanged = false;
		bool gameStateChanged = false;
		if (_initialUpdate) {
			_initialUpdate = false;
			presetChanged = true;
			gameStateChanged = true;
		}
		if (Input.GetKeyDown(skipBackKey)) {
			_currentPreset = MathHelper.Mod(_currentPreset - 1, songPresets.Length);
			presetChanged = true;
		} else if (Input.GetKeyDown(skipForwardKey)) {
			_currentPreset = MathHelper.Mod(_currentPreset + 1, songPresets.Length);
			presetChanged = true;
		}
		if (Input.GetKeyDown(toggleTitleKey)) {
			gameState = (gameState == GameState.TitleCard) ? GameState.Visualizer : GameState.TitleCard;
			_gameState = gameState;
			gameStateChanged = true;
		} else if (_gameState != gameState) {
			Debug.Log("GameState changed");
			_gameState = gameState;
			gameStateChanged = true;
		}
		
		//Process preset change
		if (presetChanged) {
			SongPreset currentPreset = songPresets[_currentPreset];
			logPreset();
			//Push state to TempoManager
			TempoManager.instance.pendingBPM = currentPreset.BPM;
			
			if (currentPreset.autoShowTitleCard) {
				gameState = GameState.TitleCard;
				_gameState = gameState;
			} else {
				//Default state if no auto-title is visualizer
				gameState = GameState.Visualizer;
				_gameState = gameState;
			}
			Debug.Log("presetChanged: " + _gameState);
		}
		
		//Process gameState change - if only the preset changed we also need to push updates here
		if (gameStateChanged || presetChanged) {
			//Push conditional state
			MovieController movieController = GameObject.FindGameObjectWithTag("MovieController").GetComponent<MovieController>();
			switch (_gameState) {
				case GameState.TitleCard:
					movieController.showTitleCard(currentPreset.titleCardName);
					if (null != _roadControllerObject) _roadControllerObject.SetActive(false);
					//Debug.Log("TITLECARD ON");
					break;
				case GameState.Visualizer:
					movieController.toggleVisibility = currentPreset.toggleVisibility;
					movieController.togglePlayback = currentPreset.togglePlayback;
					movieController.cycleMaterial = currentPreset.cycleMaterial;
					movieController.visibilityFrequency = currentPreset.visibilityFrequency;
					movieController.playbackFrequency = currentPreset.playbackFrequency;
					movieController.materialFrequency = currentPreset.materialFrequency;
					movieController.visualizerMode(currentPreset.playMovieOnStart);
					if (null != _roadControllerObject) _roadControllerObject.SetActive(true);
					//Debug.Log("VISUALIZER ON");
					//Automatic tempo sync
					TempoManager.instance.syncBPM();
					break;
			}
		}
	}
	
	void logPreset() {
		Debug.Log("Activate " + songPresets[_currentPreset].ToString());
	}
}
