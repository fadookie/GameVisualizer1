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
	
	int numStartBullets = 40;
	List<BulletInfo> bullets;
	public float radius = 0;
	public float radiusDelta = 0;
	
	void Awake() {
		bullets = new List<BulletInfo>(numStartBullets);
	}

	void Start () {
		for (int i = 0; i < numStartBullets; i++) {
			BulletInfo bullet = new BulletInfo(OT.CreateObject("Bullet"));//, new Vector2(MathHelper.Map(i, 0, numStartBullets, -1, 1), MathHelper.Map(i, 0, numStartBullets, -1, 1)));
			bullet.sprite.transform.parent = transform;
			bullet.sprite.transform.Rotate(0, 0, MathHelper.Map(i, 0, numStartBullets, 0, 360));
			bullets.Add(bullet);
		}
	}
	
	void Update () {
		//TODO everything but don't use heading as velocity	
		radius += radiusDelta;

		foreach (BulletInfo bullet in bullets) {
			//bullet.sprite.position = new Vector2(bullet.sprite.position.x + (bullet.heading.x * Time.deltaTime), bullet.sprite.position.y + (bullet.heading.y * Time.deltaTime));
			bullet.sprite.transform.Translate(0, radiusDelta, 0, Space.Self);
		}
	}
}
