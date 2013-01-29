using UnityEngine;
using System.Collections;

[RequireComponent (typeof(MeshRenderer))]
[RequireComponent (typeof(Material))]
public class MovieController : Reactive {

	public bool play = false;
	public bool loop = false;
	public MovieBeatBehavior behavior = MovieBeatBehavior.None;
	public bool retrigger = false;
	public FilterMode filterMode = FilterMode.Point;
	public float duration;
	
	public enum MovieBeatBehavior {
		None,
		Toggle
	}

	MovieTexture movie;
	MeshRenderer renderer;
	// Use this for initialization
	void Start () {
		// this line of code will make the Movie Texture begin playing
		renderer = gameObject.GetComponent<MeshRenderer>();
		if (!(renderer.material.mainTexture is MovieTexture)) {
			throw new System.Exception("Texture must be a MovieTexture");
		}
		movie = renderer.material.mainTexture as MovieTexture;
		duration = movie.duration;
		
		ReactiveManager.Instance.registerListener(this, getChannels());
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
		switch (behavior) {
			case MovieBeatBehavior.Toggle:
				if (movie.isPlaying) {
					play = false;
					renderer.enabled = false;
					if (!retrigger) {
						movie.Pause();
					} else {
						movie.Stop();
					}
				} else {
					renderer.enabled = true;
					play = true;
					movie.Play();
				}
				break;
			case MovieBeatBehavior.None:
			default:
				break;
		}
	}
	
	#endregion	
}
