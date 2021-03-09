using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using PicaVoxel;

public class ShmupBulletManager : MonoBehaviour
{
    public static ShmupBulletManager Instance;
    public Object BulletPrefab;

    public List<ShmupBullet> Bullets;

    private const int MAX_BULLETS = 50;

	// Use this for initialization
	void Start () {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

	    for (int i = 0; i < MAX_BULLETS; i++)
	    {
            ShmupBullet b = ((GameObject)Instantiate(BulletPrefab)).GetComponent<ShmupBullet>();
	        b.transform.parent = transform;
            b.gameObject.SetActive(false);
	        Bullets.Add(b);
	    }
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public void Spawn(Object owner, Vector3 position, Vector3 velocity, float life)
    {
        ShmupBullet b = Bullets.FirstOrDefault(bul => !bul.gameObject.activeSelf);
        if (b)
            b.Init(owner, position, velocity, life);
    }
}
