using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShooterController : Reactive {
	
	const uint NUM_PLAYERS = 1;
	GameObject[] players = new GameObject[NUM_PLAYERS];
	ShooterPlayerController[] playerControllers = new ShooterPlayerController[NUM_PLAYERS];
	List<OTAnimatingSprite> enemies = new List<OTAnimatingSprite>();
	List<BulletClusterController> bulletClusters = new List<BulletClusterController>(15); //15 seems like a reasonable upper bound for number of clusters on-screen at once

		
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
		GameObject clusterObject = new GameObject("cluster1", typeof(BulletClusterController));
		clusterObject.transform.parent = transform;
//		clusterObject.transform.position = Vector3.zero;
		BulletClusterController cluster = clusterObject.GetComponent<BulletClusterController>();
		if (cluster == null) throw new System.Exception("BulletClusterController cannot be null");
		
		bulletClusters.Add(cluster);
		
		ReactiveManager.Instance.registerListener(this, getChannels());
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	#region Reactive event handlers
	
	public override void reactToAmplitude(uint channel, float amp, bool overThreshold) {
	}
	
	public override void reactToBeat(float currentBPM) {
		foreach(OTAnimatingSprite enemySprite in enemies){
			enemySprite.position = new Vector2(enemySprite.position.x + 1, enemySprite.position.y);
		}
	}
	
	#endregion
}
