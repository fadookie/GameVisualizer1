using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShooterController : Reactive {
	
	const uint NUM_PLAYERS = 1;
	GameObject[] players = new GameObject[NUM_PLAYERS];
	ShooterPlayerController[] playerControllers = new ShooterPlayerController[NUM_PLAYERS];
	List<OTAnimatingSprite> enemies = new List<OTAnimatingSprite>();
	List<BulletClusterController> bulletClusters = new List<BulletClusterController>(15); //15 seems like a reasonable upper bound for number of clusters on-screen at once
	private uint _beatCount = 0;
	private uint _clusterCount = 0;
	float clusterRadiusDelta = 0;

		
	// Use this for initialization
	void Start () {
		//Spawn players
		for (int i = 0; i < players.Length; i++) {
			players[i] = OT.CreateObject("Player");
			if (players[i] == null) throw new System.Exception("Player cannot be null");
			players[i].transform.parent = transform;
			players[i].AddComponent("ShooterPlayerController");
			playerControllers[i] = players[i].GetComponent<ShooterPlayerController>();
			if (playerControllers[i] == null) throw new System.Exception("PlayerController cannot be null");
		}
		
		//Spawn initial enemies
		GameObject enemyObject =  OT.CreateObject("SpaceInvader1");
		enemyObject.transform.parent = transform;
		OTAnimatingSprite enemySprite = enemyObject.GetComponent<OTAnimatingSprite>();
		if(null == enemySprite) throw new System.Exception("Enemy object must have an OTAnimatingSprite");
		enemies.Add(enemySprite);
		
		//Spawn test cluster
		//spawnCluster();
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
		clusterRadiusDelta = 10 * Time.deltaTime;
		//Cleanup
		for (int i = bulletClusters.Count - 1; i >= 0; i--) {
			BulletClusterController cluster = bulletClusters[i];
			/*
			if (cluster.radius > 100) {
				Debug.Log("Remove cluster ");
				bulletClusters.RemoveAt(i);
				Destroy(cluster.gameObject);
			} else {
			*/
				//Move it
				cluster.radiusDelta = clusterRadiusDelta;
			//}
		}
	}
	
	void spawnClusterAtPosition(Vector3 position) {
		GameObject clusterObject = new GameObject(string.Format("cluster_{0}", ++_clusterCount), typeof(BulletClusterController));
		clusterObject.transform.parent = transform;
		clusterObject.transform.Translate(position);//localPosition = position; //FIXME: Why the fuck doesn't this work?
//		clusterObject.transform.position = Vector3.zero;
		BulletClusterController cluster = clusterObject.GetComponent<BulletClusterController>();
		if (cluster != null) {//throw new System.Exception("BulletClusterController cannot be null");
			bulletClusters.Add(cluster);
		}
	}
	
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
		_beatCount++;
		/*
		if (_beatCount % 2 == 0) {
			clusterRadiusDelta = 10 * Time.deltaTime;
		} else {
			clusterRadiusDelta = 0;
		}
		*/
		if (_beatCount % 4 == 0) spawnClusterAtPosition(new Vector3(_beatCount * 10, 0, 0));
		foreach(OTAnimatingSprite enemySprite in enemies){
			enemySprite.position = new Vector2(enemySprite.position.x + 1, enemySprite.position.y);
		}
	}
	
	#endregion
}
