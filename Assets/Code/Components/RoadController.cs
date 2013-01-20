using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

//[RequireComponent (typeof(GUITexture))]
[RequireComponent (typeof(MeshFilter ))]
[RequireComponent (typeof(MeshRenderer))]
public class RoadController : Reactive {

	
	private Rect _screenRect = new Rect(0, 0, Screen.width, Screen.height);
	private Texture2D _texture;
	//public Material material;
	public float width;
	public float height;
	private List<Segment> _segments = new List<Segment>();
	private List<Segment> _segmentRenderQueue = new List<Segment>();
	public float roadHalfWidth = 2000; // half the roads width, easier math if the road spans from -roadWidth to +roadWidth
	public float segmentLength = 200; // length of a single segment
	public float rumbleLength = 3;  // number of segments per red/white rumble strip
	public float trackLength; // z length of entire track (computed)
	public int lanes = 3; // number of lanes
	public float fieldOfView = 100; // angle (degrees) for field of view
	public float cameraHeight = 1000; // z height of camera
	public float cameraDepth; // z distance camera is from screen (computed)
	public float drawDistance = 300; // number of segments to draw
	public float playerXOffset = 0; // player x offset from center of road (-1 to 1 to stay independent of roadWidth)
	public float playerZOffset; // player relative z distance from camera (computed)
	public float fogDensity = 5; // exponential fog density
	public float position = 0; // current camera Z position (add playerZ to get player's absolute Z position)
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
		
		/*
		width = Camera.current.pixelWidth;
		height = Camera.current.pixelHeight;
		*/
		width = Screen.width;
		height = Screen.height;
		
		resetRoad();
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
		position += speed * Time.deltaTime;	
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
	
	#region Road rendering methods and data structures 
	
	struct Polygon {
		//Only planning to use this to store quads for now.
		public int submeshIndex;
		public Vector3[] verts;
		public int[] indicies;
		public const MeshTopology topology = MeshTopology.Quads; //Made this static to save memory since I'm not planning on using anything other than quads yet.
	}
	
	struct Segment {
		public int index;
		public float width;
		public int lanes;
		public Polygon[] polygons;
		public SegmentColor color;
		public Projection p1;
		public Projection p2;
	}
	
	struct ScreenInfo {
		public float x;
		public float y;
		public float scale;
		public float w;
	}
	
	struct Projection {
		public Vector3 world;
		public Vector3 camera;
		public ScreenInfo screen;
		
		public Projection(Vector3 world) : this(world, new Vector3(), new ScreenInfo()) {
		}
		
		public Projection(Vector3 world, Vector3 camera, ScreenInfo screen) {
			this.world = world;
			this.camera = camera;
			this.screen = screen;
		}
	}
	
	enum SegmentColor {
		DARK = 0,
		LIGHT
	}
	
	enum SubmeshType {
		ROAD_ASPHALT_DARK = 0,
		ROAD_ASPHALT_LIGHT,
		ROAD_GRASS_DARK,
		ROAD_GRASS_LIGHT,
		ROAD_STRIPE
	}
	
	Segment findSegment(float z) {
		if (_segments.Count < 1) {
			throw new Exception("Can't find segment, segments list empty");
		}
		return _segments[Mathf.FloorToInt(z/segmentLength) % _segments.Count];
	}
	
	Projection project(Projection p, float cameraX, float cameraY, float cameraZ, float cameraDepth, float width, float height, float roadWidth) {
		p.camera.x     = p.world.x - cameraX;
	    p.camera.y     = p.world.y - cameraY;
	    p.camera.z     = p.world.z - cameraZ;
	    p.screen.scale = cameraDepth/p.camera.z;
	    p.screen.x     = Mathf.Round((width/2)  + (p.screen.scale * p.camera.x  * width/2));
	    p.screen.y     = Mathf.Round((height/2) - (p.screen.scale * p.camera.y  * height/2));
	    p.screen.w     = Mathf.Round(             (p.screen.scale * roadWidth   * width/2));
	    return p;
	}
	
	void resetRoad() {
		_segments.Clear();
		for (int n = 0; n < 500; n++) { //arbitrary road length
			Segment segment = new Segment();
			segment.index = n;
			segment.p1 = new Projection(new Vector3(0, 0, n * segmentLength));
			segment.p2 = new Projection(new Vector3(0, 0, (n+1) * segmentLength));
			segment.color = ((Mathf.FloorToInt(n/rumbleLength)%2) != 0) ? SegmentColor.DARK : SegmentColor.LIGHT;
			_segments.Add(segment);
		}
		trackLength = _segments.Count * segmentLength;
	}
	
	void Render() {
		Segment baseSegment = findSegment(position);
		float maxy = cameraHeight;
		
		for (int n = 0; n < drawDistance; n++) {
		
			Segment segment = _segments[(baseSegment.index + n) % _segments.Count];
			
			segment.p1 = project(segment.p1, (playerXOffset * roadHalfWidth), cameraHeight, position, cameraDepth, width, height, roadHalfWidth);
			segment.p2 = project(segment.p2, (playerXOffset * roadHalfWidth), cameraHeight, position, cameraDepth, width, height, roadHalfWidth);
			if ((segment.p1.camera.z <= cameraDepth) || // behind us
        		(segment.p2.screen.y >= maxy)) {          // clip by (already rendered) segment
		     	continue;
			}
			
			queueSegment(width, lanes,
				segment.p1.screen.x,
				segment.p1.screen.y,
				segment.p1.screen.w,
				segment.p2.screen.x,
				segment.p2.screen.y,
				segment.p2.screen.w,
				segment.color
			);
			
			maxy = segment.p2.screen.y;
		}
		
		//Now, actually render the polygons
		RenderSegments();
	}
	
	void queueSegment(float width, int lanes, float x1, float y1, float w1, float x2, float y2, float w2, SegmentColor color) {
		//Decoupled in case we want to move this to a different class, etc.
		float r1 = rumbleWidth(w1, lanes);
		float r2 = rumbleWidth(w2, lanes);
		float l1 = laneMarkerWidth(w1, lanes);
		float l2 = laneMarkerWidth(w2, lanes);
		
		float lanew1, lanew2, lanex1, lanex2;
		int lane;
		
		Segment segment = new Segment();
		segment.polygons = new Polygon[2]; //[4]
		//Grass
		/*
		segment.polygons[0] = makeQuad(
			(int)SubmeshType.ROAD_GRASS_LIGHT,
			0, 0,
			0, y2,
			width, y2,
			width, 0
		);
		*/
		
		segment.polygons[0] =  makeQuad(
					0,
					200, 200, //upper right
					200, 0, //lower right
					0, 0, //lower left
					0, 200 //upper left
				);
		segment.polygons[1] = makeQuad(
					1,
					-100, -100, //upper right
					-100, 0, //lower right
					0, 0, //lower left
					0, -100 //upper left
				);
		
		//Rumble 1
		/*
		segment.polygons[1] = makeQuad (
			(int)SubmeshType.ROAD_ASPHALT_LIGHT,
			*/
			
			
		//Rumble 2
		//Road
		
		_segmentRenderQueue.Add(segment);
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

		// Generate quad indices
		poly.indicies = new int[4];
		poly.indicies[0] = submeshIndex * 4;
		poly.indicies[1] = submeshIndex * 4 + 1;
		poly.indicies[2] = submeshIndex * 4 + 2;
		poly.indicies[3] = submeshIndex * 4 + 3;
		
		return poly;
	}
	
	float rumbleWidth (float projectedRoadWidth, int lanes) {
		return projectedRoadWidth/Mathf.Max(6,  2*lanes);
	}
	
	float laneMarkerWidth (float projectedRoadWidth, int lanes) {
		return projectedRoadWidth/Mathf.Max(32, 8*lanes);
	}
	
	void RenderSegments() {
		//Initialize mesh
        Mesh mesh;
		int subMeshCount = 2;
        if (null == gameObject.GetComponent<MeshFilter>().mesh) {
			mesh = new Mesh();
			gameObject.GetComponent<MeshFilter>().mesh = mesh;
		} else {
			//Re-use existing mesh as reccomended in Unity docs
			mesh = gameObject.GetComponent<MeshFilter>().mesh;
			mesh.Clear();
		}
        mesh.subMeshCount = subMeshCount;
        mesh.MarkDynamic();
        
		//List<Polygon> subPolygons = new List<Polygon>(subMeshCount);
		/*
		List<int>[] tris = new List<int>[subMeshCount];
		for (int i = 0; i < subMeshCount; i++) {
			tris[i] = new List<int>();
		}
		*/
		for (int segmentIndex = 0; segmentIndex < _segmentRenderQueue.Count; segmentIndex++) {
			Segment segment = _segmentRenderQueue[segmentIndex];
			List<Vector3> verts = new List<Vector3>();
			
			//Copy quads into mesh vertex data
			for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++) {
				verts.AddRange(segment.polygons[subMeshIndex].verts);
			}
			
			//Create quads
			/*
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
			*/
			
			//Copy quads into mesh vertex data
			/*
			const int vertsPerPoly = 4;
	        Vector3[] verts  = new Vector3[subMeshCount * vertsPerPoly];
			for (int subMeshIndex = 0, vertsIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++, vertsIndex += vertsPerPoly) {
				Polygon poly = subPolygons[subMeshIndex];
				Array.Copy(poly.verts, 0, verts, vertsIndex, vertsPerPoly); 
			}
			*/
			mesh.vertices = verts.ToArray();
	        
			//Each submesh is assigned to a different material in the Mesh Renderer.
	        for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++) {
		        mesh.SetIndices(segment.polygons[submeshIndex].indicies, Polygon.topology, submeshIndex);
			}
			/*
	        for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++) {
	        	CombineInstance[] ci = new CombineInstance[2];
	        	ci[0] = new CombineInstance();
	        	ci[0].mesh = mesh;
	        	ci[0].subMeshIndex = submeshIndex;
	        	ci[0].transform = new Matrix4x4(); //FIXME trying to stop crashes but this shouldn't be neccessary
	        	ci[1] = new CombineInstance();
	        	ci[1].mesh = newMesh;
	        	ci[1].subMeshIndex = submeshIndex;
	        	ci[1].transform = new Matrix4x4();
				mesh.CombineMeshes(ci, false, false);
			}
			*/
		}
        
        mesh.RecalculateNormals();
		_segmentRenderQueue.Clear();
	}
	
	#endregion
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	public override void reactToBeat(float currentBPM) {
	}
	
	#endregion
}
