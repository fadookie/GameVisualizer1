using UnityEngine;
using System.Collections;

public class DestroyOnGameStart : MonoBehaviour {

	// Use this for initialization
	void Start () {
		gameObject.SetActive(false); //Disable immediately
		Destroy(gameObject, 2.0f); //Destroy soon, but not immediately to avoid NPEs
	}
}
