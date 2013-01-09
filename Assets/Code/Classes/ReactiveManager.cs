/// <summary>
/// Reactive manager. Notifies all registered reactive objects of events such as amplitude changes, etc. 
/// Listeners register for numbered channels.
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class ReactiveManager
{
    private static ReactiveManager instance;
    private Dictionary<uint, List<Reactive>> _listeners = new Dictionary<uint, List<Reactive>>(); //FIXME if we have performance issues due to hashtable lookup times just make _listners a List of Lists or fixed-size array
    private List<Reactive> _allChannelListeners = new List<Reactive>(); //Reference to CHANNEL_ALL so we don't have to keep looking it up in the dictionary

	public const uint CHANNEL_ALL = 0;
 
    public ReactiveManager() 
    {
        if (instance != null)
        {
            Debug.LogError ("Cannot have two instances of singleton. Self destruction in 3...");
            return;
        }

		_listeners.Add(CHANNEL_ALL, _allChannelListeners);
		
        instance = this;
    }
 
    public static ReactiveManager Instance {
		get {
			if (instance == null) {
				new ReactiveManager();
			}
 
			return instance;
		}
	}
	
	/// <summary>
	/// Registers the listener to the all channels.
	/// </summary>
	/// <param name='listener'>
	/// Listener.
	/// </param>
	public void registerListener (Reactive listener) {
		this.registerListener(listener, CHANNEL_ALL);
	}
	
	public void registerListener (Reactive listener, uint channel) {
		this.registerListener(listener, new[] { channel });
	}
	
	public void registerListener (Reactive listener, uint[] channels)
	{
		foreach (uint channel in channels) {
			List<Reactive> channelListeners;
			_listeners.TryGetValue(channel, out channelListeners);
			if (null == channelListeners) {
				channelListeners = new List<Reactive>();
				_listeners.Add(channel, channelListeners);
			}
			channelListeners.Add(listener);
		}
	}
	
	
	/// <summary>
	/// Removes the listener from all channels.
	/// </summary>
	/// <param name='listener'>
	/// Listener.
	/// </param>
	public void removeListener (Reactive listener)
	{
		foreach (List<Reactive> channelListeners in _listeners.Values) {
			channelListeners.Remove(listener);
		}
	}
	
	public void removeListener (Reactive listener, uint channel)
	{
		this.removeListener(listener, new uint[] { channel } );
	}
	
	public void removeListener (Reactive listener, uint[] channels) {
		foreach (uint channel in channels) {
			List<Reactive> channelListeners;
			_listeners.TryGetValue(channel, out channelListeners);
			if (null != channelListeners) {
				channelListeners.Remove(listener);
			}
		}
	}
	
	
	#region Reactive event broadcast methods
	
	public void amplitudeEvent(uint channel, float amp, bool overThreshold) {
		//First notify CHANNEL_ALL
		foreach (Reactive reactive in _allChannelListeners) {
			reactive.reactToAmplitude(channel, amp, overThreshold);
		}
		if (CHANNEL_ALL == channel) return; //We're done
		
		//Notify regular listeners
		List<Reactive> channelListeners;
		_listeners.TryGetValue(channel, out channelListeners);
		if (null != channelListeners) {
			foreach (Reactive reactive in channelListeners) {
				if (_allChannelListeners.Contains(reactive)) continue; //Don't notify all channel listeners twice
				reactive.reactToAmplitude(channel, amp, overThreshold);
			}
		}
	}
	
	#endregion
}
