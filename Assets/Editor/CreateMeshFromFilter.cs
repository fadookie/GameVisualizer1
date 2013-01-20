using UnityEngine;
using UnityEditor;
using System.Collections;
 
public class CreateMeshFromFilter : ScriptableWizard
{
	public MeshFilter meshFilter;
	public string fileName = "Mesh";
 
	[MenuItem("Assets/Create/Mesh from MeshFilter...")]
	static void CreateWizard ()
	{
		ScriptableWizard.DisplayWizard ("Create Mesh from MeshFilter", typeof(CreateMeshFromFilter));
	}
 
	void OnWizardUpdate ()
	{
	}
 
	void OnWizardCreate ()
	{
		if (null != meshFilter) {
			string filepath = string.Format("Assets/Editor/{0}_{1}.asset", name, System.DateTime.Now.ToString("yyyyMMddHHmmssffff"));
			//bool success = AssetDatabase.DeleteAsset(filepath);
			//Debug.Log("delete file: " + success);
			AssetDatabase.CreateAsset (meshFilter.sharedMesh, filepath);
			AssetDatabase.SaveAssets (); 
		}
	}
}