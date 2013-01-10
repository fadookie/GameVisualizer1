/// <summary>
/// Reactive manager. Notifies all registered reactive objects of events such as amplitude changes, etc. 
/// Listeners register for numbered channels.
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class ReactiveManager
{
    private static ReactiveManager instance;
    private Dictionary<uint, HashSet<Reactive>> _listeners = new Dictionary<uint, HashSet<Reactive>>(); //FIXME if we have performance issues due to hashtable lookup times just make _listners a List of Lists or fixed-size array
    private HashSet<Reactive> _allChannelListeners = new HashSet<Reactive>(); //Reference to CHANNEL_ALL so we don't have to keep looking it up in the dictionary

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
			HashSet<Reactive> channelListeners;
			_listeners.TryGetValue(channel, out channelListeners);
			if (null == channelListeners) {
				channelListeners = new HashSet<Reactive>();
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
		foreach (HashSet<Reactive> channelListeners in _listeners.Values) {
			channelListeners.Remove(listener);
		}
	}
	
	public void removeListener (Reactive listener, uint channel)
	{
		this.removeListener(listener, new uint[] { channel } );
	}
	
	public void removeListener (Reactive listener, uint[] channels) {
		foreach (uint channel in channels) {
			HashSet<Reactive> channelListeners;
			_listeners.TryGetValue(channel, out channelListeners);
			if (null != channelListeners) {
				channelListeners.Remove(listener);
			}
		}
	}
	
	
	#region Reactive event broadcast methods
	
	public void amplitudeEvent(uint channel, float amp, bool overThreshold) {
		HashSet<Reactive> listeners = getListeners(channel);
		foreach (Reactive reactive in listeners) {
			reactive.reactToAmplitude(channel, amp, overThreshold);
		}
	}
	
	public void beatEvent(uint channel, float currentBPM) {
		HashSet<Reactive> listeners = getListeners(channel);
		foreach (Reactive reactive in listeners) {
			reactive.reactToBeat(currentBPM);
		}
	}
	
	/// <summary>
	/// Gets listeners for a channel, plus all-channel listeners.
	/// FIXME: We may be creating new HashSets here, if this becomes a performance issue these can be cached during listener registration/removal
	/// </summary>
	/// <returns>
	/// The listeners.
	/// </returns>
	/// <param name='channel'>
	/// Channel.
	/// </param>
	private HashSet<Reactive> getListeners(uint channel) {
		HashSet<Reactive> listeners = _allChannelListeners;
		
		if (CHANNEL_ALL != channel) {
			//Add in regular listeners
			HashSet<Reactive> channelListeners = null;
			_listeners.TryGetValue(channel, out channelListeners);
			
			if (null != channelListeners) {
				listeners = new HashSet<Reactive>(_allChannelListeners);
				listeners.UnionWith(channelListeners);
			}
		}
		
		return listeners;
	}
	
	#endregion
}
