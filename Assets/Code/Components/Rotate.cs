using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour {

	public float xSpeed, ySpeed, zSpeed = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		transform.Rotate(xSpeed * Time.deltaTime, ySpeed * Time.deltaTime, zSpeed * Time.deltaTime);
	}
}
