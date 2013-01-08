using UnityEngine;
using System.Collections;

public class SelectCamera : MonoBehaviour {
	public bool selectCamera = false;

	// Use this for initialization
	void Awake ()
	{
		Debug.Log (this + "start");
		CameraManager.Instance.registerCamera (this);
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (selectCamera) {
			selectCamera = false;
			CameraManager.Instance.activateCamera (this);
		}
	}
	
	void OnDestroy ()
	{
		CameraManager.Instance.deregisterCamera (this);
	}
	
	
}
