/// <summary>
/// Tempo manager. Keeps track of BPM and reports to ReactiveManager.
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using System;

public class TempoManager : MonoSingleton<TempoManager>
{
	public enum TempoManagerState {
		MANUAL_TAP, //Beats only occur when beat button is pressed by VJ
		STATIC, //Use a BPM set in the editor or by tap capture
		AUTODETECT //Use BPM detection algorithm from audio input
	}
	
	public enum TempoManagerAuxState {
		NONE,
		TAP_SYNC, //Sync static BPM by detecting tap
		TAP_CAPTURE //Sample beat button over time to figure out BPM
	}
	
	public TempoManagerState pendingMainState;
	public TempoManagerAuxState pendingAuxState;
	
	public string beatKey = "space";
	
	private TempoManagerState _mainState = TempoManagerState.STATIC;
	/// <summary>
	/// Sets the state.
	/// </summary>
	/// <param name='newState'>
	/// New state.
	/// </param>
	public TempoManagerState MainState {
		get { return _mainState; }
		set {
			_mainState = value;
			updateTempoManagerStatus();
		}
	}
	
	private TempoManagerAuxState _auxState = TempoManagerAuxState.NONE;
	/// <summary>
	/// Set an auxilary state, i.e. TAP_CAPTURE which occurs during another state
	/// </summary>
	/// <param name='newState'>
	/// New state.
	/// </param>
	public TempoManagerAuxState AuxState {
		get { return _auxState; }
		set {
			_auxState = value;
			updateTempoManagerStatus();
		}
	}
	
	private bool autoBeat = false;
	
	public float pendingBPM;
	private float BPM;
	public bool syncNow = false;
		
	public uint tempoEventChannel = 1u;
	
	public string tempoManagerStatus;
	
    private static TempoManager instance;
    
	public override void Init() {
	}
 
	void Start() {
		switch(_mainState) {
			case TempoManagerState.STATIC:
				break;
		}
	}
	
	void Update() {
		//Process state changes from GUI
		if (pendingBPM != BPM) {
			BPM = pendingBPM;
			syncBPM();
		}
		if (pendingMainState != _mainState) {
			MainState = pendingMainState;
		}
		if (pendingAuxState != _auxState) {
			AuxState = pendingAuxState;
		}
		if (syncNow || Input.GetKeyDown(beatKey)) { //Sync now button/key
			syncBPM();
			syncNow = false;
		}
		
		switch (_mainState) {
			case TempoManagerState.MANUAL_TAP:
				if (autoBeat) {
					CancelInvoke();
					autoBeat = false;
				}
			
				if (Input.GetKeyDown(beatKey)) {
					beat();
				}
				break;
				
			case TempoManagerState.STATIC:
				if (!autoBeat) {
					syncBPM();
				}
				break;
		}
	}
	
	/// <summary>
	/// Restart BPM timer
	/// </summary>
	private void syncBPM() {
		CancelInvoke();
		InvokeRepeating("beat", 0, beatsPerMinuteToDelay(BPM));
		autoBeat = true;	
	}
	
	private float beatsPerMinuteToDelay(float beatsPerMinute) {
		//beats per second = beatsPerMinute / 60
		return 1.0f / (beatsPerMinute / 60.0f);
	}
	
	private void beat() {
		ReactiveManager.Instance.beatEvent(tempoEventChannel, BPM);
	}
	
	private void updateTempoManagerStatus() {
		String mainStateString = Enum.GetName(typeof(TempoManagerState), _mainState);
		String auxStateString = Enum.GetName(typeof(TempoManagerAuxState), _auxState);
		
		tempoManagerStatus = string.Format("{0} : {1}", mainStateString, auxStateString);
	}
	
}
