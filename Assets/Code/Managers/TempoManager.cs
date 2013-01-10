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
		FIXED, //Use a BPM set in the editor or by tap capture
		AUTODETECT //Use BPM detection algorithm from audio input
	}
	
	public enum TempoManagerAuxState {
		LOCKED, //Keypressed do not affect tempo settings
		TAP_SYNC, //Sync static BPM by detecting tap
		TAP_CAPTURE //Sample beat button over time to figure out BPM
	}
	
	public TempoManagerState pendingMainState;
	public TempoManagerAuxState pendingAuxState;
	
	public string beatKey = "space";
	public string manualModeKey = "m";
	public string fixedModeKey = "f";
	public string tempoCaptureKey = "c";
	public string tempoSyncKey = "s";
	public string tempoLockKey = "l";
	
	private TempoManagerState _mainState = TempoManagerState.FIXED;
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
			pendingMainState = value; //Update state in UI
		}
	}
	
	private TempoManagerAuxState _auxState = TempoManagerAuxState.TAP_SYNC;
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
			pendingAuxState = value; //Update state in UI
		}
	}
	
	private bool autoBeat = false;
	
	public float pendingBPM;
	private float _bpm;
	private float BPM {
		get {return _bpm;}
		set {
			_bpm = value;
			pendingBPM = value;
		}
	}
	public bool syncNow = false;
		
	public uint tempoEventChannel = 1u;
	
	public struct BPMCapturePoint {
		public float timestamp;
		public float runningAverageBPM;
		
		public BPMCapturePoint(float timestamp, float runningAverageBPM) {
			this.timestamp = timestamp;
			this.runningAverageBPM = runningAverageBPM;
		}
		
		public string ToString() {
			return string.Format("BPMCapturePoint{{ timestamp:{0}, runningAverageBPM:{1} }}", timestamp, runningAverageBPM);
		}
	}	
	public List<BPMCapturePoint> bpmCaptureCache = new List<BPMCapturePoint>();
	public int bpmCaptureCacheSize = 10;
	
	public override void Init() {
	}
 
	void Start() {
		switch(_mainState) {
			case TempoManagerState.FIXED:
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
		if (syncNow) { //Sync now button 
			syncBPM();
			syncNow = false;
		}
		
		//Process state change keys if any
		if (Input.GetKeyDown(manualModeKey)) {
			MainState = TempoManagerState.MANUAL_TAP;
		} else if (Input.GetKeyDown(fixedModeKey)) {
			MainState = TempoManagerState.FIXED;
		}
		
		if (Input.GetKeyDown(tempoLockKey)) {
			if (TempoManagerAuxState.TAP_CAPTURE == _auxState) {
				//Transitioning from capture to lock, auto-sync BPM
				syncBPM();
			}
			AuxState = TempoManagerAuxState.LOCKED;
			
		} else if (Input.GetKeyDown(tempoSyncKey)) {
			if (TempoManagerAuxState.TAP_CAPTURE == _auxState) {
				//Transitioning from capture to sync, auto-sync BPM
				syncBPM();
			}
			AuxState = TempoManagerAuxState.TAP_SYNC;
			
		} else if (Input.GetKeyDown(tempoCaptureKey)) {
			AuxState = TempoManagerAuxState.TAP_CAPTURE;
		}
		
		//Logic for main state
		switch (_mainState) {
			case TempoManagerState.MANUAL_TAP:
				if (autoBeat) {
					CancelInvoke();
					autoBeat = false;
				}
				//Automatically enable TAP_SYNC aux mode
				if (TempoManagerAuxState.TAP_SYNC != _auxState) {
					AuxState = TempoManagerAuxState.TAP_SYNC;
				}
				break;
				
			case TempoManagerState.FIXED:
				if (!autoBeat) {
					syncBPM();
				}
				break;
		}
		
		
		//Logic for aux state (input processing mode)
		switch (_auxState) {
			//TempoManagerAuxState.LOCKED has no behavior by definition
			case TempoManagerAuxState.TAP_SYNC:
				if (Input.GetKeyDown(beatKey)) {
					if (TempoManagerState.FIXED == _mainState) {
						syncBPM();
					} else if (TempoManagerState.MANUAL_TAP == _mainState) {
						beat();
					}
				}
				break;
				
			case TempoManagerAuxState.TAP_CAPTURE:
				if (Input.GetKeyDown(beatKey)) {
					bpmCaptureCache.Add(new BPMCapturePoint(Time.time, 0));
					// calculate the current BPM
					if (bpmCaptureCache.Count > 1) {
						//Struct can't be edited so we have to replace it with an updated one
						bpmCaptureCache[bpmCaptureCache.Count - 1] = new BPMCapturePoint(
							bpmCaptureCache[bpmCaptureCache.Count - 1].timestamp,
							60/(bpmCaptureCache[bpmCaptureCache.Count -1].timestamp - bpmCaptureCache[bpmCaptureCache.Count -2].timestamp)
						);
					}
					// bump off the oldest member of the cache (treat as a queue)
					if (bpmCaptureCache.Count > bpmCaptureCacheSize)
					{
						bpmCaptureCache.RemoveAt(0);
					}
					BPM = calculateBPM(bpmCaptureCache);
					Debug.Log(string.Format("BPM = {0}, {1}", BPM, bpmCaptureCache));
				}
				break;
		}
	}
	
	float calculateBPM(List<BPMCapturePoint> theBPMcache)
	{
	    float lowestValue  = 0;
	    float highestValue = 0;
	    float total        = 0;
	    for (int i=0; i<theBPMcache.Count; i++)
	    {
	        total += theBPMcache[i].runningAverageBPM;
	        if (i == 0)
	        {
	            lowestValue = highestValue = theBPMcache[i].runningAverageBPM;
	        } else {
	            lowestValue  = Mathf.Min(lowestValue, theBPMcache[i].runningAverageBPM);
	            highestValue = Mathf.Max(highestValue, theBPMcache[i].runningAverageBPM);
	        }
	    }
	        
	    // toss the lowest and highest values (if more than 2 samples)
	    //These were rounded but I'm keeping them as-is for precision
	    float BPMAverage;
	    if (theBPMcache.Count > 2)
	    {
	        BPMAverage = ((total - lowestValue - highestValue)/(float)(theBPMcache.Count-2));
	    } else {
	        BPMAverage = (total/(float)theBPMcache.Count);
	    }
	    return BPMAverage; /// 2.0f);
	}


	
	/// <summary>
	/// Restart BPM timer
	/// </summary>
	private void syncBPM() {
		CancelInvoke();
		InvokeRepeating("beat", 0, beatsPerMinuteToDelay(BPM));
		autoBeat = true;	
	}
	
	public static float beatsPerMinuteToDelay(float beatsPerMinute) {
		//beats per second = beatsPerMinute / 60
		return 1.0f / (beatsPerMinute / 60.0f);
	}
	
	public static float beatsPerMinuteToAnimSpeed(float beatsPerMinute, float fps) {
		return fps / (beatsPerMinute / 60.0f);
	}
	
	private void beat() {
		ReactiveManager.Instance.beatEvent(tempoEventChannel, BPM);
	}
	
	/// <summary>
	/// A string representing the current state of the Tempo Manager.
	/// </summary>
	/// <value>
	/// The status line.
	/// </value>
	public string StatusLine {
		get {
			return string.Format(
				"{0} : {1}",
				Enum.GetName(typeof(TempoManagerState), _mainState),
				Enum.GetName(typeof(TempoManagerAuxState), _auxState)
			);
		}
	}
	
}

