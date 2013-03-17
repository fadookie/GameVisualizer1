using UnityEngine;
using System.Collections.Generic;

public class BulletClusterController : MonoBehaviour { //TODO make a Reactive
	struct BulletInfo {
		public GameObject gameObject;
		public OTSprite sprite;
		public Vector2 heading;
		
		public BulletInfo(GameObject gameObject) : this(gameObject, Vector2.zero) {}
		public BulletInfo(GameObject gameObject, Vector2 heading) {
			if (gameObject == null) throw new System.Exception("gameObject was null");
			this.gameObject = gameObject;
			this.sprite = gameObject.GetComponent<OTSprite>();
			if (this.sprite == null) throw new System.Exception("gameObject didn't have a sprite");
			this.heading = heading;
		}
	}
	
	int numStartBullets = 50;
	List<BulletInfo> bullets;
	
	void Awake() {
		bullets = new List<BulletInfo>(numStartBullets);
	}

	void Start () {
		for (int i = 0; i < numStartBullets; i++) {
			BulletInfo bullet = new BulletInfo(OT.CreateObject("Bullet"), new Vector2(i, 0));
			bullet.sprite.transform.parent = transform;
			bullets.Add(bullet);
		}
	}
	
	void Update () {
		//TODO everything but don't use heading as velocity	

		foreach (BulletInfo bullet in bullets) {
			bullet.sprite.position = new Vector2(bullet.sprite.position.x + (bullet.heading.x * Time.deltaTime), bullet.sprite.position.y + (bullet.heading.y * Time.deltaTime));
		}
	}
}
