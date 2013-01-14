using UnityEngine;
using System.Collections;
using System.Text;
using System;

//[RequireComponent (typeof(GUITexture))]
[RequireComponent (typeof(MeshFilter ))]
[RequireComponent (typeof(MeshRenderer))]
public class RoadController : Reactive {

	
	private Rect _screenRect = new Rect(0, 0, Screen.width, Screen.height);
	private Texture2D _texture;
	//public Material material;
	//var segments      = [];                      // array of road segments
	public float roadHalfWidth = 2000; // half the roads width, easier math if the road spans from -roadWidth to +roadWidth
	public float segmentLength = 200; // length of a single segment
	public float rumbleLength = 3;  // number of segments per red/white rumble strip
	public int lanes = 3; // number of lanes
	public float fieldOfView = 100; // angle (degrees) for field of view
	public float cameraHeight = 1000; // z height of camera
	public float cameraDepth; // z distance camera is from screen (computed)
	public float drawDistance = 300; // number of segments to draw
	public float playerXOffset = 0; // player x offset from center of road (-1 to 1 to stay independent of roadWidth)
	public float playerZOffset; // player relative z distance from camera (computed)
	public float fogDensity = 5; // exponential fog density
	public float cameraZPosition = 0; // current camera Z position (add playerZ to get player's absolute Z position)
	public float speed = 0; // current speed
	public float maxSpeed = 12000; // top speed (ensure we can't move more than 1 segment in a single frame to make collision detection easier)
	public float accel; // acceleration rate - tuned until it 'felt' right
	public float breaking; // deceleration rate when braking
	public float decel; // 'natural' deceleration rate when neither accelerating, nor braking
	public float offRoadDecel; // off road deceleration is somewhere in between
	public float offRoadLimit; // limit when off road deceleration no longer applies (e.g. you can always go at least this speed even when off road)

	// Use this for initialization
	void Start () {
		accel =  maxSpeed / 5;
		breaking = -maxSpeed;
		decel = -maxSpeed/5;
		offRoadDecel = -maxSpeed/2;
		offRoadLimit = maxSpeed/4;
		
		_texture = new Texture2D(320, 112);
		_texture.filterMode = FilterMode.Point;
		//guiTexture.texture = _texture;
		
		/*
		StringBuilder builder = new StringBuilder();
		foreach(Vector3 v3 in gameObject.GetComponent<MeshFilter>().mesh.vertices) {
			builder.Append(v3.ToString() + ", ");
		}
		Debug.Log(builder);
		*/
	}
	
	// Update is called once per frame
	void Update () {
		cameraZPosition += speed * Time.deltaTime;	
		float dx = Time.deltaTime * 2 * (speed / maxSpeed); // at top speed, should be able to cross from left to right (-1 to 1) in 1 second
		
		if (Input.GetKey(KeyCode.LeftArrow)) {
			playerXOffset -= dx;
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			playerXOffset += dx;
		}
		
		if (Input.GetKey(KeyCode.UpArrow)) {
			speed += accel * Time.deltaTime;
		} else if (Input.GetKey(KeyCode.DownArrow)) {
			speed += breaking * Time.deltaTime;
		} else {
			speed += decel * Time.deltaTime;
		}
		
		if (((playerXOffset < -1) || (playerXOffset > 1)) && (speed > offRoadLimit)) {
			speed += offRoadDecel * Time.deltaTime;
		}
		
		playerXOffset = Mathf.Clamp(playerXOffset, -2, 2); // dont ever let player go too far out of bounds
		speed = Mathf.Clamp(speed, 0, maxSpeed); // or exceed maxSpeed
		
		Render();
	}
	
	struct Polygon {
		//Only planning to use this to store quads for now.
		public int submeshIndex;
		public Vector3[] verts;
		public int[] tris;
	}
	
	Polygon makeQuad(int submeshIndex, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
		Polygon poly = new Polygon();
		poly.submeshIndex = submeshIndex;
		
		//Make vertices
		poly.verts = new Vector3[4];
        poly.verts[0] = new Vector3(x1, y1, 0);
        poly.verts[1] = new Vector3(x2, y2, 0);
        poly.verts[2] = new Vector3(x3, y3, 0);
        poly.verts[3] = new Vector3(x4, y4, 0);

		// Generate triangles indices
		poly.tris = new int[6];
		poly.tris[0] = submeshIndex * 4;
		poly.tris[1] = submeshIndex * 4 + 1;
		poly.tris[2] = submeshIndex * 4 + 2;

		poly.tris[3] = submeshIndex * 4;
		poly.tris[4] = submeshIndex * 4 + 3;
		poly.tris[5] = submeshIndex * 4 + 1;
		
		return poly;
	}
	
	void Render() {
		int subMeshCount = 2;
		Polygon[] subPolygons = new Polygon[subMeshCount];
		
		//Create quads
		{
			int polyIndex = 0;
			subPolygons[polyIndex] = makeQuad(
				polyIndex,
				0, 200, //upper left
				200, 0, //lower right
				0, 0, //lower left
				200, 200 //upper right
			);
			polyIndex++;
			subPolygons[polyIndex] = makeQuad(
				polyIndex,
				0, -100, //upper left
				-100, 0, //lower right
				0, 0, //lower left
				-100, -100 //upper right
			);
		}
		
		//Copy quads into mesh vertex data
		const int vertsPerPoly = 4;
        Vector3[] verts  = new Vector3[subMeshCount * vertsPerPoly];
		for (int subMeshIndex = 0, vertsIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++, vertsIndex += vertsPerPoly) {
			Polygon poly = subPolygons[subMeshIndex];
			Array.Copy(poly.verts, 0, verts, vertsIndex, vertsPerPoly); 
		}
		
		//Initialize mesh
        Mesh mesh;
        if (null == gameObject.GetComponent<MeshFilter>().mesh) {
			mesh = new Mesh();
			gameObject.GetComponent<MeshFilter>().mesh = mesh;
		} else {
			//Re-use existing mesh as reccomended in Unity docs
			mesh = gameObject.GetComponent<MeshFilter>().mesh;
			mesh.Clear();
		}
        mesh.MarkDynamic();
        mesh.vertices = verts;
        
		//Each submesh is assigned to a different material in the Mesh Renderer.
        mesh.subMeshCount = subMeshCount;
        for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++) {
	        mesh.SetTriangles(subPolygons[submeshIndex].tris, submeshIndex);
		}
        mesh.RecalculateNormals();
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion
}
