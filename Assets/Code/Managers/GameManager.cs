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
	
	public string ToString() {
		return string.Format("SongPreset{{name:{0}, notes:{1}, titleCardName:{2}, BPM:{3}}}", songName, notes, titleCardName, BPM);
	}
}
	
public class GameManager : MonoSingleton<GameManager>
{
	public GameState gameState = GameState.TitleCard;
	public int _currentPreset = 0;
	public SongPreset currentPreset {
		get { return songPresets[_currentPreset]; }
	}
	public SongPreset[] songPresets;
	public string skipBackKey = "[";
	public string skipForwardKey = "]";
	public string toggleTitleKey = "t";
	
	public enum GameState {
		TitleCard = 0,
		Visualizer
	}
	
	
	public override void Init() {
	}
	
	void Start() {
		logPreset();
	}
	
	void Update() {
		//Process input
		bool presetChanged = false;
		if (Input.GetKeyDown(skipBackKey)) {
			_currentPreset = MathHelper.Mod(_currentPreset - 1, songPresets.Length);
			presetChanged = true;
		} else if (Input.GetKeyDown(skipForwardKey)) {
			_currentPreset = MathHelper.Mod(_currentPreset + 1, songPresets.Length);
			presetChanged = true;
		}
		if (Input.GetKeyDown(toggleTitleKey)) {
			gameState = (gameState == GameState.TitleCard) ? GameState.Visualizer : GameState.TitleCard;
		}
		
		//Process state change
		if (presetChanged) {
			SongPreset currentPreset = songPresets[_currentPreset];
			logPreset();
			//Push state to MovieController
			MovieController movieController = GameObject.FindGameObjectWithTag("MovieController").GetComponent<MovieController>();
			
			//Push state to TempoManager
			TempoManager.instance.pendingBPM = currentPreset.BPM;
		}
	}
	
	void logPreset() {
		Debug.Log("Activate " + songPresets[_currentPreset].ToString());
	}
}
