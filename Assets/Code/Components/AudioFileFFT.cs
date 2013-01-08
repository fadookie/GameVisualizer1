using UnityEngine;
using System.Collections;
using Exocortex.DSP;

[RequireComponent (typeof(AudioSource))]

public class AudioFileFFT : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
		Invoke ("ProcessAudio", 10);
	}
	
	void ProcessAudio()
	{
		audio.Stop ();
		
		float[] audioData = new float[audio.clip.samples * audio.clip.channels];
		audio.clip.GetData (audioData, 0);
		
		Debug.Log ("got audioData: " + audioData);
	}
	
	// Update is called once per frame
	void Update ()
	{
	}
}
