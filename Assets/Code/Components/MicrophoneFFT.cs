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
	private float[] audioData = null;
	private ComplexF[] fftInData = null;
	LineRenderer graph = null; //Spectograph for debugging
  
	//Use this for initialization  
	void Start ()
	{  
		//Check if there is at least one microphone connected  
		if (Microphone.devices.Length <= 0) {  
			//Throw a warning message at the console if there isn't  
			Debug.LogWarning("Microphone not connected!");  
		} else { //At least one microphone is present  
			//Set 'micConnected' to true  
			micConnected = true;  
  
			//Get the default microphone recording capabilities  
			Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);  
  
			//According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
			if (minFreq == 0 && maxFreq == 0) {  
				//...meaning 44100 Hz can be used as the recording sampling rate  
				maxFreq = 44100;  
			}  
  
			//Get the attached AudioSource component  
			goAudioSource = this.GetComponent<AudioSource>();  
			graph = gameObject.GetComponent<LineRenderer>();
		}  
	}
    
	void Update ()
	{
		if (micConnected && (null != goAudioSource) && (null != goAudioSource.clip)) {
			
			int arraySize = goAudioSource.clip.samples * goAudioSource.clip.channels;
			if (null == audioData) {
				audioData = new float[arraySize];
				//Initialize the FFT in buffer for only once channel for now
				fftInData = new ComplexF[goAudioSource.clip.samples];
				graph.SetVertexCount (200);
			}
			
			goAudioSource.clip.GetData (audioData, 0); //Copy current audio buffer into audioData
			int writePosition = Microphone.GetPosition (null); //Get current write position into the audio ring buffer
			
			//Debugging info
			StringBuilder builder = new StringBuilder ();
			int numNonZeroData = 0;
			
			//Loop over the ring buffer
			{
				int graphIndex = 0;
				//int incrementAmount = 1;
				int incrementAmount = goAudioSource.clip.channels; //This would be so we only process the first (presumably, the left) channel
				int fftSamplingRate = arraySize / 200;
				int fftSampleCounter = 0;
				Debug.Log (string.Format ("i = {0} inc = {1}", (writePosition + 1), incrementAmount));
				
				//Loop over the ring buffer, at the interval specified by incrementAmount
				for (int i = writePosition + 1; i != writePosition; i = (i + incrementAmount) % arraySize, fftSampleCounter++) { 
					if (fftSampleCounter >= fftSamplingRate && graphIndex < 200) {
						graph.SetPosition (
							graphIndex,
							new Vector3 (
								graphIndex,
								MathHelper.Map(audioData[i], -1.0f, 1.0f, 0.0f, 100.0f),
								0
							)
						);
						graphIndex++;
						fftSampleCounter = 0;
						//builder.Append (audioData[i] * 100).Append (", ");
					}
					if (audioData [i] != 0.0f) {
						numNonZeroData++;
					}
				}
				Debug.Log ("i == " + writePosition);
			}
			
			Debug.Log (string.Format ("Got data, {0}/{1} were non-zero, position={2}", numNonZeroData, arraySize, writePosition));
			Debug.Log (builder);
			Debug.Log ("===============\n\n");
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
