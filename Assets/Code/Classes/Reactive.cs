/// <summary>
/// Common interface for all reactive components.
/// 
/// NB: I'd rather use an interface than an abstract class, but implementing an interface in my component
/// was confusing the editor and causing my public variables to not display in the editor.
/// </summary>
using UnityEngine;
using System;

public abstract class Reactive : MonoBehaviour {
	public int[] channels; //The inspector doesn't work with uints. It's probably going to wrap if you set one of these to negative so I'd avoid it.
	
	abstract public void reactToAmplitude(uint channel, float amp, bool overThreshold);
	
	void OnDestroy() {
		ReactiveManager.Instance.removeListener(this);	
	}
	
	protected uint[] getChannels() {
		uint[] channelReg = new uint[channels.Length];
		for (int i = 0; i < channels.Length; i++) {
			channelReg[i] = (uint)channels[i];
		}
		return channelReg;
	}
}
