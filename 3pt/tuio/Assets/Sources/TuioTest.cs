using UnityEngine;
using System.Collections;
using TUIO;
public class TuioTest : MonoBehaviour {

	private TuioUnityListener fis;
	// Use this for initialization
	void Start () {
		fis = new TuioUnityListener();
	}
	
	// Update is called once per frame
	void Update () {
		if(fis.cursors.Count > 0)
		{
//			TuioCursor ske = (TuioCursor)fis.cursors[0];
	//		print( ske.getPosition().getX() );
		}
	}
	
	void OnGUI() {
		if(fis.cursors.Count > 0)
		{
			foreach (TuioCursor ske in fis.cursors)
			{
				GUI.Label(new Rect(ske.getPosition().getX()*Screen.width, ske.getPosition().getY()*Screen.height, 50,20), "prut");
			}
		}
		if(fis.objects.Count > 0)
		{
			foreach (TuioObject ske in fis.objects)
			{
				GUI.Label(new Rect(ske.getPosition().getX()*Screen.width, ske.getPosition().getY()*Screen.height, 50,20), "Fidu");
			}
		}
	}
}
