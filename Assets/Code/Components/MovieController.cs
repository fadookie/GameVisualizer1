using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Material))]
public class MovieController : Reactive {

	public bool play = false;
	public bool loop = false;
	public FilterMode filterMode = FilterMode.Point;

	MovieTexture movie;
	// Use this for initialization
	void Start () {
		// this line of code will make the Movie Texture begin playing
		movie = (MovieTexture)renderer.material.mainTexture;
	}
	
	// Update is called once per frame
	void Update () {
		if (loop != movie.loop) {
			movie.loop = loop;
		}
		if (filterMode != movie.filterMode) {
			movie.filterMode = filterMode;
		}
		if (play && !movie.isPlaying) {
			movie.Play();	
		} else if (!play && movie.isPlaying) {
			movie.Pause();
		}
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion	
}
