using System;
using UnityEngine;
using UnityEditor;

public class SequenceEditor : EditorWindow {

	[MenuItem ("Window/Sequence Editor")]
	static void ShowWindow()
	{
		SequenceEditor window = (SequenceEditor)EditorWindow.GetWindow(typeof(SequenceEditor));
		window.Show();
	}
	void OnGUI()
	{
	}
}