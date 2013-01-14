using UnityEngine;
using System.Collections;
using System.Text;

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
	
	void Render() {
		int subMeshCount = 2;
        Vector3[] verts  = new Vector3[8];
        //Vector2[] uv  = new Vector2[8];
        int[][] tri  = new int[subMeshCount][];

        verts[0] = new Vector3(0 /*ul.x*/, 0, 1 /*ul.y*/);
        verts[1] = new Vector3(1 /*lr.x*/, 0, 0 /*lr.y*/);
        verts[2] = new Vector3(0 /*ll.x*/, 0, 0 /*ll.y*/);
        verts[3] = new Vector3(1 /*ur.x*/, 0, 1 /*ur.y*/);
        
        verts[4] = new Vector3(0, 0, -1);
        verts[5] = new Vector3(-1, 0, 0);
        verts[6] = new Vector3(0, 0, 0);
        verts[7] = new Vector3(-1, 0, -1);
        
        /*
        verts[4] = new Vector3(0, 0, 1);
        verts[5] = new Vector3(1, 0, 0);
        verts[6] = new Vector3(0, 0, 0);
        verts[7] = new Vector3(1, 0, 1);
        */

/*
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        
        uv[4] = new Vector2(0, 0);
        uv[5] = new Vector2(1, 0);
        uv[6] = new Vector2(0, 1);
        uv[7] = new Vector2(1, 1);
        */

	/*
	var submeshTris = new int[(sections.length - 1) * 2 * 3];
	for (i=0;i<submeshTris.length / 6;i++)
	*/
		// Generate triangles indices
        for (int i = 0; i < subMeshCount; i++) {
			int[] submeshTris = new int[6];
			tri[i] = submeshTris;
			submeshTris[0] = i * 4;
			submeshTris[1] = i * 4 + 1;
			submeshTris[2] = i * 4 + 2;
	
			submeshTris[3] = i * 4;
			submeshTris[4] = i * 4 + 3;
			submeshTris[5] = i * 4 + 1;
		}

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
        //mesh.triangles = tri;
        
		//Each submesh is assigned to a different material in the Mesh Renderer.
        mesh.subMeshCount = subMeshCount;
        for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++) {
	        mesh.SetTriangles(tri[submeshIndex], submeshIndex);
		}
        //mesh.uv = uv;
        mesh.RecalculateNormals();

/*
        Color[] color = new Color[4];
        color[2] = new Color(1, 0, 0);
        color[1] = new Color(0, 1, 0);
        color[0] = new Color(0, 0, 1);
        color[3] = new Color(0, 1, 0);

        mesh.colors = color;
        */
        //Graphics.DrawMeshNow(mesh, transform.worldToLocalMatrix);
        //Graphics.DrawMesh(mesh, transform.worldToLocalMatrix, material, 9);
		
	/*
		if (null != guiTexture) {
			for (int x = 0; x < _texture.width; x++) {
				for (int y = 0; y < _texture.height; y++) {
					_texture.SetPixel(x, y, new Color(Random.value, Random.value, Random.value, 1));
				}
			}
			_texture.Apply();
		}
		*/
	}
	
	void OnGUI() {
	/*
		if (EventType.Repaint == Event.current.type) {
			if (null != guiTexture.texture && null != material) {
				GL.PushMatrix();
				material.SetPass(0);
				GL.LoadOrtho();
				//GL.LoadPixelMatrix(0, Camera.main.pixelWidth, Camera.main.pixelHeight / 2f, 0);
	//			GL.LoadPixelMatrix (0, 512, 512, 0); 
				//Graphics.DrawTexture(_screenRect, guiTexture.texture, material);
				Mesh mesh = new Mesh();
				
				/*
				GL.Begin(GL.TRIANGLES);
				GL.Color(new Color(1,1,1,1));
				GL.Vertex3(0.5f, 0.25f, 0f);
				GL.Vertex3(0.25f, 0.25f, 0f);
				GL.Vertex3(0.375f, 0.5f, 0f);
				GL.End();
				*//*
				GL.PopMatrix();
		}
		}
		*/
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion
}
