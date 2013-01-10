using UnityEngine;
using System.Collections;
using Exocortex.DSP;
using System.Text;

[RequireComponent (typeof(AudioSource))]

public class MicrophoneFFT : MonoBehaviour
{
	//A boolean that flags whether there's a connected microphone  
	private bool micConnected = false;  
  
	//The maximum and minimum available recording frequencies  
	private int minFreq;
	private int maxFreq;  
  
	//A handle to the attached AudioSource  
	private AudioSource goAudioSource = null;
	private int previousAudioBufferPosition = 0;
	private float[] audioData = null;
	private ComplexF[] fftInData = null;
	private LineRenderer graph = null; //Spectograph for debugging
	private int graphCounter = 0; //Simple timer for when to sample for the graph
	private int graphIndex = 0; //Loop counter for when to sample
	
	public float amplitudeThreshhold = 70.0f;
	public uint amplitudeEventChannel = 2u;
  
	//Use this for initialization  
	void Start ()
	{  
		//Check if there is at least one microphone connected  
		if (Microphone.devices.Length <= 0) {  
			//Throw a warning message at the console if there isn't  
			Debug.LogWarning ("Microphone not connected!");  
		} else { //At least one microphone is present  
			//Set 'micConnected' to true  
			micConnected = true;  
  
			//Get the default microphone recording capabilities  
			Microphone.GetDeviceCaps (null, out minFreq, out maxFreq);  
  
			//According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
			if (minFreq == 0 && maxFreq == 0) {  
				//...meaning 44100 Hz can be used as the recording sampling rate  
				maxFreq = 44100;  
			}  
  
			//Get the attached AudioSource component  
			goAudioSource = this.GetComponent<AudioSource> ();  
			graph = gameObject.GetComponent<LineRenderer> ();
		}  
	}
    
	void Update ()
	{
		if (micConnected && (null != goAudioSource) && (null != goAudioSource.clip)) {
			
			int arraySize = goAudioSource.clip.samples * goAudioSource.clip.channels;
			if (null == audioData) {
				audioData = new float[arraySize];
				graph.SetVertexCount (200);
			}
			
			goAudioSource.clip.GetData (audioData, 0); //Copy current audio buffer into audioData
			int audioBufferPosition = Microphone.GetPosition (null); //Get current write position into the audio ring buffer
			int newSamples = Mathf.Abs (audioBufferPosition - previousAudioBufferPosition);
			int fftInDataSize = Mathf.NextPowerOfTwo(newSamples);
			if (fftInDataSize > 4096) {
				//We have too much data for the FFT library to process in a single frame, try to pick it up later
				//FIXME need to profile this but this is probably a dumb and latency-prone way of handling this, better to discard old data and keep pace with realtime events
				fftInDataSize = 4096;
			}
			fftInData = new ComplexF[fftInDataSize];
			
			//Debugging info
			StringBuilder builder = new StringBuilder ();
			int numNonZeroData = 0;
			
			//Track instantaneous amplitude
			{
				float amplitude = audioData[(audioBufferPosition + arraySize - 1) % arraySize];
				
				//Graph it
				float graphAmplitude = MathHelper.Map(amplitude, -0.5f, 0.5f, 0.0f, 200.0f);	
				if (graphAmplitude > amplitudeThreshhold) {
					graph.SetColors(Color.red, Color.red);
				} else {
					graph.SetColors(Color.green, Color.green);
				}
				graph.SetPosition(1, new Vector3(0, graphAmplitude, 0));
				if (audioData[audioBufferPosition] != 0.0f) {
					//Debug.Log(string.Format("Graph = data[{0}] {1} -> {2}", audioBufferPosition, audioData[audioBufferPosition], graphAmplitude));
				}
				
				//Broadcast notification
				ReactiveManager.Instance.amplitudeEvent(amplitudeEventChannel, amplitude, graphAmplitude > amplitudeThreshhold);
			}
			
			//Send the buffered audio through FFT.
			if (previousAudioBufferPosition != audioBufferPosition) {
				int incrementAmount = goAudioSource.clip.channels; //We only process the first (presumably, the left) channel. If set to 1, it will process all channels in the buffer.
				int fftInDataIndex = 0;
				
				//Graphing stuff
				int graphSamplingRate = arraySize / 200;

				
				//Debug.Log (string.Format ("for (i = {0}; i!= {1}; i += {2} % foo)", previousAudioBufferPosition, audioBufferPosition, incrementAmount));
				
				//Catch up with the current position in the ring buffer if possible, at the interval specified by incrementAmount
				//Bail if we're going to overrun the FFT buffer
				//FIXME: an off-by-one error causes an infinite loop here, should make this more robust
				int i = previousAudioBufferPosition; 
				for (;
					i != audioBufferPosition;
					i = (i + incrementAmount) % arraySize, fftInDataIndex++, graphCounter++) { 
					
					//Copy the new data over to the FFT buffer.
					if (fftInDataIndex < fftInDataSize) {
						fftInData [fftInDataIndex].Re = audioData [i];
					} else {
						//We can't push any more data to the FFT, rewind i and abort
						i = (i + arraySize - incrementAmount) % arraySize;
						break;
					}
					
					//Update graph if needed
					/*
					if (graphCounter >= graphSamplingRate && graphIndex < 200) {
						float graphAmplitude = MathHelper.Map (audioData [i], -1.0f, 1.0f, 0.0f, 100.0f);
						graph.SetPosition (
							graphIndex,
							new Vector3 (
								graphIndex,
								graphAmplitude,
								0
							)
						);
						graphIndex++;
						graphCounter = 0;
						builder.Append (graphAmplitude).Append (", ");
					} else if (graphIndex >= 200) {
						graphIndex = 0;
					}
					*/
				
					//Count non-zero rows for debugging so I can watch the buffer fill up
					if (audioData [i] != 0.0f) {
						numNonZeroData++;
					}
				}
			
				//Process FFT.
				//Fourier.FFT_Quick (fftInData, fftInDataIndex + 1, FourierDirection.Forward);
				Fourier.FFT (fftInData, FourierDirection.Forward);
				
				//Debug.Log ("i == " + i + " delta from current: " + (audioBufferPosition - i));
				
				previousAudioBufferPosition = i;
			
				//Debug.Log (string.Format ("Got data, {0}/{1} were non-zero, position={2}", numNonZeroData, arraySize, audioBufferPosition));
				//Debug.Log (builder);
				//Debug.Log ("===============\n\n");
			}
		}
	}
	
	void OnGUI ()
	{  
		//If there is a microphone  
		if (micConnected) {  
			//If the audio from any microphone isn't being captured  
			if (!Microphone.IsRecording(null)) {  
				//Case the 'Record' button gets pressed  
				if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Record")) {  
					//Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource  
					goAudioSource.clip = Microphone.Start(null, true, 20, maxFreq);  
				}  
			} else { //Recording is in progress  
				//Case the 'Stop and Play' button gets pressed  
				if (GUI.Button (new Rect (Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Stop and Play!")) {  
					Microphone.End (null); //Stop the audio recording  
					goAudioSource.Play(); //Playback the recorded audio  
				}  
  
				GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 25, 200, 50), "Recording in progress...");  
			}  
		} else { // No microphone  
			//Print a red "Microphone not connected!" message at the center of the screen  
			GUI.contentColor = Color.red;  
			GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Microphone not connected!");  
		}  
  
	}
}
