using System.Collections.Generic;
using UnityEngine;

public class CameraManager
{
    private static CameraManager instance;
    private List<SelectCamera> _cameras = new List<SelectCamera>();
    private SelectCamera _activeCamera = null;
 
    public CameraManager () 
    {
        if (instance != null)
        {
            Debug.LogError ("Cannot have two instances of singleton. Self destruction in 3...");
            return;
        }
 
        instance = this;
    }
 
    public static CameraManager Instance {
		get {
			if (instance == null) {
				new CameraManager ();
			}
 
			return instance;
		}
	}
	
	public void registerCamera (SelectCamera newCamera)
	{
		_cameras.Add (newCamera);
	}
	
	
	public void deregisterCamera (SelectCamera newCamera)
	{
		_cameras.Remove (newCamera);
		if (_activeCamera == newCamera) {
			if (_cameras.Count > 0) {
				_activeCamera = _cameras [0];
			} else {
				_activeCamera = null;
			}
		}
	}
	
	public void activateCamera (SelectCamera newCamera)
	{
		_activeCamera = newCamera;
		notifyCameras();
	}
	
	private void notifyCameras ()
	{
		foreach (SelectCamera camera in _cameras) {
			bool cameraIsActive = (camera == _activeCamera);
			camera.gameObject.camera.enabled = cameraIsActive;
			camera.gameObject.GetComponent<AudioListener>().enabled = cameraIsActive;
			if (!cameraIsActive) camera.selectCamera = false;
		}
	}
	
}
