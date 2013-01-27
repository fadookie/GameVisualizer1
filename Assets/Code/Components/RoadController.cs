using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
public class RoadController : Reactive {

	
	private Texture2D _texture;
	//public Material material;
	public float width;
	public float height;
	private List<Segment> _segments = new List<Segment>();
	public static readonly int NUM_SUBMESH_TYPES = System.Enum.GetNames(typeof(SubmeshType)).Length;
	private List<Polygon>[] _polyRenderQueue = new List<Polygon>[NUM_SUBMESH_TYPES]; 
	public float roadHalfWidth = 200; // half the roads width, easier math if the road spans from -roadWidth to +roadWidth
	public float segmentLength = 200; // length of a single segment
	public float rumbleLength = 3;  // number of segments per red/white rumble strip
	public float numSegments = 3500; //FIXME make this a circular buffer
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
	public float centrifugal = 0.3f;   // centrifugal force multiplier when going around curves
	//public float darkRandomDimValue = 0.3f;
	public bool alwaysRecolorMaterials = true;
	private Color[] _originalMaterialColors = new Color[NUM_SUBMESH_TYPES];
	public float amplitudeColorMapStart = 0;
	public float amplitudeColorMapStop = 1;
	private float _hueOffsetSlide = 0;
	public float hueOffsetSlideAmount = 0.05f;
	public float hueOffsetTest = 0.3f;
	private PlayerVehicleController _playerVehicleController;

	// Use this for initialization
	void Start () {
		accel =  maxSpeed / 5;
		breaking = -maxSpeed;
		decel = -maxSpeed/5;
		//offRoadDecel = -maxSpeed/2;
		offRoadLimit = maxSpeed/4;
		
		_texture = new Texture2D(320, 112);
		_texture.filterMode = FilterMode.Point;
		Debug.Log(string.Format("W:{0} H:{1}", Screen.width, Screen.height));
		
		/*
		width = Camera.current.pixelWidth;
		height = Camera.current.pixelHeight;
		*/
		/*
		width = 1000;
		height = 1000;
		*/
		
		//Init _polyRenderQueue and _originalMaterialColors
		Material[] mats = gameObject.GetComponent<MeshRenderer>().materials;
		for (int i = 0; i < NUM_SUBMESH_TYPES; i++) {
			_polyRenderQueue[i] = new List<Polygon>();
			_originalMaterialColors[i] = new Color(mats[i].color.r, mats[i].color.g, mats[i].color.b);
		}
		
		resetRoad();
		//guiTexture.texture = _texture;
		
		/*
		StringBuilder builder = new StringBuilder();
		foreach(Vector3 v3 in gameObject.GetComponent<MeshFilter>().mesh.vertices) {
			builder.Append(v3.ToString() + ", ");
		}
		Debug.Log(builder);
		*/
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		float screenScaleFactor =  1024.0f / Screen.width; //Fix aspect ratio to 4:3 independent of resolution. Not quite sure why this works, but it does.
		width = Screen.width * screenScaleFactor;
		height = Screen.height * screenScaleFactor;
		
		Segment playerSegment = findSegment(position + playerZOffset);
		
		position += speed * Time.deltaTime;	
		float speedPercent = speed / maxSpeed;
		float dx = Time.deltaTime * 2 * speedPercent; // at top speed, should be able to cross from left to right (-1 to 1) in 1 second
		
		PlayerVehicleController.VehicleOrientation orientation = PlayerVehicleController.VehicleOrientation.STRAIGHT;
		
		//FIXME: super hack since the rendering is currently inverted on the x axis, just flip player controls to compensate.
		if (Input.GetKey(KeyCode.LeftArrow)) {
			playerXOffset += dx;
			orientation = PlayerVehicleController.VehicleOrientation.LEFT;
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			playerXOffset -= dx;
			orientation = PlayerVehicleController.VehicleOrientation.RIGHT;
		}
		
		//Apply "centrifugal" force for cornering
		playerXOffset -= (dx * speedPercent * playerSegment.curve * centrifugal);
		
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
		
		_playerVehicleController.VehicleUpdate(speed, orientation);
		
		Render();
	}
	
	#region PlayerVehicleController registration
	
	public void registerPlayerVehicleController(PlayerVehicleController pvc) {
		_playerVehicleController = pvc;
	}
	
	public void deregisterPlayerVehicleController(PlayerVehicleController pvc) {
		_playerVehicleController = null;
	}
	
	#endregion
	
	#region Road rendering methods and data structures 
	
	/// <summary>
	/// Struct representing a Quad, might generalize it for other types of geometry later.
	/// </summary>
	struct Polygon {
		//Only planning to use this to store quads for now.
		public Vector3[] verts;
		public int[] indices;
		public const MeshTopology topology = MeshTopology.Quads; //Made this static to save memory since I'm not planning on using anything other than quads yet.
	}
	
	/// <summary>
	/// Struct representing a horizontal segment of the pseudo-3D road
	/// </summary>
	struct Segment {
		public int index;
		public float width;
		public int lanes;
		public Polygon[] polygons;
		public SegmentColor color;
		public float curve;
		public Projection p1;
		public Projection p2;
		
		public override string ToString() {
			return string.Format("Segment{{index:{0},width:{1},lanes:{2},color:{3},p1:{4},p2:{5}}}", index, width, lanes, color, p1, p2);
		}
		
	}
	
	/// <summary>
	/// Struct to hold information on a point in screen space
	/// </summary>
	struct ScreenInfo {
		public float x;
		public float y;
		public float scale;
		public float w;
		public ScreenInfo(float x, float y, float scale, float w) {
			this.x = x;
			this.y = y;
			this.scale = scale;
			this.w = w;
		}
		public override string ToString() {
			return string.Format("ScreenInfo{{x:{0},y:{1},scale:{2},w:{3}}}",x,y,scale,w);
		}
	}
	
	/// <summary>
	/// Struct representing a projection of a point through world, camera, and screen space.
	/// </summary>
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
		
		public override string ToString() {
			return string.Format("Projection{{world:{0},camera:{1},screen:{2}}}", world, camera, screen);
		}
	}
	
	/// <summary>
	/// The color of a segment
	/// </summary>
	enum SegmentColor {
		DARK = 0,
		LIGHT
	}
	
	/// <summary>
	/// A list of valid materials for a quad.
	/// The size of this enum determines the size of _polyRenderQueue.
	/// Corresponding materials should be assigned in the Editor in the Materials array, in order.
	/// </summary>
	enum SubmeshType {
		ROAD_ASPHALT_DARK = 0,
		ROAD_ASPHALT_LIGHT,
		ROAD_GRASS_DARK,
		ROAD_GRASS_LIGHT,
		ROAD_RUMBLE_DARK,
		ROAD_RUMBLE_LIGHT,
		ROAD_LANE_SEPARATOR
	}
	
	enum RoadLength {
		NONE = 0,
		SHORT = 25,
		MEDIUM = 50,
		LONG = 100
	}
	
	enum RoadCurve {
		NONE = 0,
		EASY = 2,
		MEDIUM = 4,
		HARD = 6
	}
	
	Segment findSegment(float z) {
		if (_segments.Count < 1) {
			throw new System.Exception("Can't find segment, segments list empty");
		}
		return _segments[Mathf.FloorToInt(z/segmentLength) % _segments.Count];
	}
	
	Projection project(Projection p, float cameraX, float cameraY, float cameraZ, float cameraDepth, float width, float height, float roadWidth) {
		//Projection pOld = new Projection(new Vector3(p.world.x, p.world.y, p.world.z), new Vector3(p.camera.x, p.camera.y, p.camera.z), new ScreenInfo(p.screen.x, p.screen.y, p.screen.scale, p.screen.w));
		p.camera.x     = p.world.x - cameraX;
	    p.camera.y     = p.world.y - cameraY;
	    p.camera.z     = p.world.z - cameraZ;
	    p.screen.scale = cameraDepth/p.camera.z;
	    p.screen.x     = Mathf.Round((width/2.0f)  + (p.screen.scale * p.camera.x  * width/2.0f));
	    p.screen.y     = Mathf.Round((height/2.0f) - (p.screen.scale * p.camera.y  * height/2.0f));
	    p.screen.w     = Mathf.Round(             (p.screen.scale * roadWidth   * width/2.0f));
		//Debug.Log(string.Format("project({0},{1},{2},{3},{4},{5},{6},{7}) => {8}",pOld, cameraX, cameraY, cameraZ, cameraDepth, width, height, roadWidth, p));
	    return p;
	}
	
	void addSegment(float curve) {
		int n = _segments.Count;
		Segment segment = new Segment();
		segment.index = n;
		segment.p1 = new Projection(new Vector3(0, 0, n * segmentLength));
		segment.p2 = new Projection(new Vector3(0, 0, (n+1) * segmentLength));
		segment.curve = curve;
		segment.color = ((Mathf.FloorToInt(n/rumbleLength)%2) != 0) ? SegmentColor.DARK : SegmentColor.LIGHT;
		_segments.Add(segment);
	}
	
	void addRoad(int enter, int hold, int leave, float curve) {
		for(int n = 0 ; n < enter ; n++) {
		  addSegment(Mathfx.Coserp(0f, curve, n/(float)enter));
		}
		for(int n = 0 ; n < hold  ; n++) {
		  addSegment(curve);
		}
		for(int n = 0 ; n < leave ; n++) {
		  addSegment(Mathfx.Hermite(curve, 0f, n/(float)leave));	
		}
	}
	
	void addStraight(RoadLength length) {
		addStraight((int)length);
    }
    void addStraight(int num) {
      addRoad(num, num, num, 0);
	}

    void addCurve(RoadLength length, float curve) {
      addRoad((int)length, (int)length, (int)length, curve);
    }
        
    void addSCurves() {
		addRoad((int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM,  -(float)RoadCurve.EASY);
		addRoad((int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM,   (float)RoadCurve.MEDIUM);
		addRoad((int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM,   (float)RoadCurve.EASY);
		addRoad((int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM,  -(float)RoadCurve.EASY);
		addRoad((int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM, (int)RoadLength.MEDIUM,  -(float)RoadCurve.MEDIUM);
    }
	
	void resetRoad() {
		_segments.Clear();
		
		/*
		for (int n = 0; n < numSegments; n++) { //arbitrary road length
			addSegment(MathHelper.Map(Random.value, 0f, 1f, -6f, 6f));
		}
		*/
		
		addStraight((int)RoadLength.SHORT/4);
		addSCurves();
		addStraight(RoadLength.LONG);
		addCurve(RoadLength.MEDIUM, (float)RoadCurve.MEDIUM);
		addCurve(RoadLength.LONG, (float)RoadCurve.MEDIUM);
		addStraight(RoadLength.MEDIUM);
		addSCurves();
		addCurve(RoadLength.LONG, -(float)RoadCurve.MEDIUM);
		addCurve(RoadLength.LONG, (float)RoadCurve.MEDIUM);
		addStraight(RoadLength.MEDIUM);
		addSCurves();
		addCurve(RoadLength.LONG, -(float)RoadCurve.EASY);

		
		trackLength = _segments.Count * segmentLength;
	}
	
	void Render() {
		Segment baseSegment = findSegment(position);
		float basePercent = MathHelper.percentRemaining(position, segmentLength);
		float dx = -(baseSegment.curve * basePercent); //rate of change of x
		float x = 0; //spine position of road curve
		float maxy = cameraHeight;
		
		for (int n = 0; n < drawDistance; n++) {
		
			Segment segment = _segments[(baseSegment.index + n) % _segments.Count];
			
			//Debug.Log(string.Format("BEFORE p1:{0}\np2:{1}",segment.p1, segment.p2));
			segment.p1 = project(segment.p1, (playerXOffset * roadHalfWidth) - x, 		cameraHeight, position, cameraDepth, width, height, roadHalfWidth);
			segment.p2 = project(segment.p2, (playerXOffset * roadHalfWidth) - x - dx, 	cameraHeight, position, cameraDepth, width, height, roadHalfWidth);
			
			x += dx;
			dx += segment.curve;
			
			//Debug.Log(string.Format("AFTER p1:{0}\np2:{1}",segment.p1, segment.p2));
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
		//DebugLogSegments();
		
		//Now, commit the queued polygons to our mesh filter
		CommitPolyRenderQueueToMesh();
	}
	
	/// <summary>
	/// Processes a segment into polygons which are added to the rendering queue.
	/// Analagous to Render.segment() in the js example
	/// FIXME: this is being drawn upside-down, i'm compensating by rotating the road object in the editor but this inverts the x coordinates.
	/// </summary>
	void queueSegment(float segmentWidth, int lanes, float x1, float y1, float w1, float x2, float y2, float w2, SegmentColor color) {
		//Decoupled in case we want to move this to a different class, etc.
		float r1 = rumbleWidth(w1, lanes);
		float r2 = rumbleWidth(w2, lanes);
		float l1 = laneMarkerWidth(w1, lanes);
		float l2 = laneMarkerWidth(w2, lanes);
		
		SubmeshType grass  = (SegmentColor.DARK == color) ? SubmeshType.ROAD_GRASS_DARK   : SubmeshType.ROAD_GRASS_LIGHT;
		SubmeshType rumble = (SegmentColor.DARK == color) ? SubmeshType.ROAD_RUMBLE_DARK  : SubmeshType.ROAD_RUMBLE_LIGHT;
		SubmeshType road   = (SegmentColor.DARK == color) ? SubmeshType.ROAD_ASPHALT_DARK : SubmeshType.ROAD_ASPHALT_LIGHT;
		SubmeshType lane   = SubmeshType.ROAD_LANE_SEPARATOR;
		bool isLane = (SegmentColor.DARK == color);
		
		//Grass
		_polyRenderQueue[(int)grass].Add(
			makeQuad(
				0, y1,
				segmentWidth, y1,
				segmentWidth, y2,
				0, y2,
				-1 //Z-order
			)
		);
		
		//Left Rumble
		_polyRenderQueue[(int)rumble].Add(
			makeQuad(
				x1-w1-r1, y1,
				x1-w1, y1,
				x2-w2, y2,
				x2-w2-r2, y2,
				1 //Z-order
			)
		);
		
		//Right Rumble
		_polyRenderQueue[(int)rumble].Add(
			makeQuad(
				x1+w1, y1,
				x1+w1+r1, y1,
				x2+w2+r2, y2,
				x2+w2, y2,
				1 //Z-order
			)
		);
		
		//Road
		_polyRenderQueue[(int)road].Add(
			makeQuad(
				x1-w1, y1,
				x1+w1, y1,
				x2+w2, y2,
				x2-w2, y2,
				0 //Z-order
			)
		);
		
		if (isLane) {
			float lanew1, lanew2, lanex1, lanex2;
			lanew1 = w1*2/lanes;
			lanew2 = w2*2/lanes;
			lanex1 = x1 - w1 + lanew1;
			lanex2 = x2 - w2 + lanew2;
			for (int laneIndex = 1; laneIndex < lanes; lanex1 += lanew1, lanex2 += lanew2, laneIndex++) {
				_polyRenderQueue[(int)lane].Add(
					makeQuad(
						lanex1 - l1/2, y1,
						lanex1 + l1/2, y1,
						lanex2 + l2/2, y2,
						lanex2 - l2/2, y2,
						2
					)
				);
			}
			
		}
		
		//Debug.Log(string.Format("queueSegment: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", width,  lanes,  x1,  y1,  w1,  x2,  y2,  w2, color));
		//Debug.Log(string.Format("Road Quad: ({0},{1}) ({2},{3}) ({4},{5}) ({6},{7})", x1-w1, y1, x1+w1, y1, x2+w2, y2, x2-w2, y2));
		
		
		/*Test quads
		_polyRenderQueue[(int)road].Add(
			makeQuad(
				200, 200, //upper right
				200, 0, //lower right
				0, 0, //lower left
				0, 200, //upper left
				0 //Z-order
			)
		);
		_polyRenderQueue[(int)stripe].Add(
			makeQuad(
				-100, -100, //upper right
				-100, 0, //lower right
				0, 0, //lower left
				0, -100, //upper left
				0
			)
		);
		*/
	}
	
	void DebugLogSegments() {
		StringBuilder debug = new StringBuilder("_segments[\n");
		foreach (Segment s in _segments) {
			debug.AppendLine(s + "," );
		}
		debug.Append("]");
		Debug.Log(debug);
	}
	
	/// <summary>
	/// Convenience factory for Polygons representing quads
	/// </summary>
	/// <returns>
	/// New Polygon struct with the specified attributes
	/// </returns>
	Polygon makeQuad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, int zOrder) {
		Polygon poly = new Polygon();
		int z = -zOrder;
		
		//Make vertices
		poly.verts = new Vector3[4];
        poly.verts[0] = new Vector3(x1, y1, z);
        poly.verts[1] = new Vector3(x2, y2, z);
        poly.verts[2] = new Vector3(x3, y3, z);
        poly.verts[3] = new Vector3(x4, y4, z);
		
		return poly;
	}
	
	float rumbleWidth (float projectedRoadWidth, int lanes) {
		return projectedRoadWidth/Mathf.Max(6,  2*lanes);
	}
	
	float laneMarkerWidth (float projectedRoadWidth, int lanes) {
		return projectedRoadWidth/Mathf.Max(32, 8*lanes);
	}
	
	/// <summary>
	/// Processes queued polygons by turning them into a mesh
	/// which will be rendered by Unity on the next frame.
	/// </summary>
	void CommitPolyRenderQueueToMesh() {
		//Initialize mesh
        Mesh mesh =  gameObject.GetComponent<MeshFilter>().mesh;
		int subMeshCount = NUM_SUBMESH_TYPES;
        if (null != mesh) {
			//Re-use existing mesh as reccomended in Unity docs
			mesh = gameObject.GetComponent<MeshFilter>().mesh;
			mesh.Clear();
		} else {
			//Create new mesh
			mesh = new Mesh();
			gameObject.GetComponent<MeshFilter>().mesh = mesh;
		}
        mesh.subMeshCount = subMeshCount;
        mesh.MarkDynamic();
        
		//Copy polygon verts into mesh and generate quad indicies for submeshes
		List<Vector3> verts = new List<Vector3>();
		List<int>[] indices = new List<int>[subMeshCount];
		for (int i = 0; i < indices.Length; i++) {
			indices[i] = new List<int>();
		}
		
		for (int submeshIndex = 0; submeshIndex < subMeshCount; submeshIndex++) { //foreach List<Polygon> polygons in _polyRenderQueue
			List<Polygon> polygons = _polyRenderQueue[submeshIndex];
			
			for (int polyIndex = 0; polyIndex < polygons.Count; polyIndex++) { //foreach Polygon poly in polygons
				Polygon poly = polygons[polyIndex];
				
				//Copy polygon verts into temp list
				int oldVertexCount = verts.Count;
				verts.AddRange(poly.verts);
				
				//Generate quad indicies for submeshes.
				//Each submesh is assigned to a different material in the Mesh Renderer.
				//Assume every poly is a quad
				List<int> currentIndicies = indices[submeshIndex];
				currentIndicies.Add(oldVertexCount);
				currentIndicies.Add(oldVertexCount + 1);
				currentIndicies.Add(oldVertexCount + 2);
				currentIndicies.Add(oldVertexCount + 3);
			}
		}
		//Assign vertices to mesh
		mesh.vertices = verts.ToArray();
		
		//Assign indicies to submeshes
		for (int submeshIndex = 0; submeshIndex < subMeshCount; submeshIndex++) {
	        mesh.SetIndices(indices[submeshIndex].ToArray(), Polygon.topology, submeshIndex);
		}
		
        //Finalize mesh
        mesh.RecalculateNormals();
        //mesh.Optimize();
        
		//Cleanup
        foreach (List<Polygon> polygons in _polyRenderQueue) {
			polygons.Clear();
		}
	}
	
	#endregion
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
		System.Array submeshTypes = System.Enum.GetValues(typeof(SubmeshType));
		MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
		//float amplitudeThreshhold = GameObject.FindGameObjectWithTag("MicController").GetComponent<MicrophoneFFT>().amplitudeThreshhold;
		Material[] materials = renderer.materials;
		foreach(SubmeshType matType in submeshTypes) {
			Material mat =  materials[(int)matType];
			Color matColor;
			
			if ((alwaysRecolorMaterials || overThreshold)) {
				float hueOffset = MathHelper.Map(amp, amplitudeColorMapStart, amplitudeColorMapStop, 0, 1);//Random.value; //hueOffsetTest;
				hueOffset += _hueOffsetSlide;
				//Color randColorLight =  new Color(Random.value, Random.value, Random.value);
				//Color randColorDark = new Color(Mathf.Clamp01(randColorLight.r - darkRandomDimValue), Mathf.Clamp01(randColorLight.g - darkRandomDimValue), Mathf.Clamp01(randColorLight.b - darkRandomDimValue));
				HSBColor hsbColor = new HSBColor(_originalMaterialColors[(int)matType]);
				hsbColor.h = (hsbColor.h + hueOffset) % 1f;
				switch(matType) {
					case SubmeshType.ROAD_ASPHALT_DARK:
					case SubmeshType.ROAD_ASPHALT_LIGHT:
						//hsbColor.s = Mathf.Clamp01(hsbColor.s + 0.5f);
						//hsbColor.b = Mathf.Clamp01(hsbColor.b + 0.5f);
						break;
					default:
						break;
				}
				matColor = hsbColor.ToColor();
				
			} else if (overThreshold && SubmeshType.ROAD_LANE_SEPARATOR == matType) {
				matColor = new Color(Random.value, Random.value, Random.value);
				
			} else {
				matColor = _originalMaterialColors[(int)matType];
			}
			switch(matType) {
				case SubmeshType.ROAD_RUMBLE_DARK:
				case SubmeshType.ROAD_RUMBLE_LIGHT:
				case SubmeshType.ROAD_LANE_SEPARATOR:
					mat.color = matColor;
					break;
				default:
					mat.color = matColor;
					break;
			}
		}
	}
	public override void reactToBeat(float currentBPM) {
		_hueOffsetSlide += hueOffsetSlideAmount;
	}
	
	#endregion
}
