using System;
using UnityEngine;
using System.Collections;
using PicaVoxel;
using Object = UnityEngine.Object;

public class InvadersBullet : MonoBehaviour
{
    public Object Owner;
    public Vector3 Velocity;
    public float Lifetime;

    private Exploder exploder;
    private CollisionDetector detector;

	public void Init (Object owner, Vector3 position, Vector3 velocity, float life)
	{
	    exploder = GetComponent<Exploder>();
	    detector = GetComponent<CollisionDetector>();

	    detector.ClearEvents();
        detector.Collided += detector_Collided;

	    Owner = owner;
	    transform.position = position;
	    Velocity = velocity;
	    Lifetime = life;

        gameObject.SetActive(true);
	}

    void detector_Collided(Volume collisonObject, Voxel voxel, Vector3 worldPosition)
    {
        // If the bullet hits an invader and is owned by the player, explode and destroy the invader - and deactivate the bullet
        if (collisonObject.name == "Invader" && Owner is InvadersPlayerShip)
        {
            collisonObject.Destruct(3f, true);
            Destroy(collisonObject.gameObject);
            gameObject.SetActive(false);
        }

        // If we hit a shield, just explode and deactivate the bullet
        if (collisonObject.name == "Shield")
        {
            exploder.ExplosionRadius = 1f;
            exploder.Explode();
            gameObject.SetActive(false);
        }

        // If we hit the player ship
        if (collisonObject.name == "Player Ship")
        {
            collisonObject.Destruct(3f, true);
            collisonObject.GetComponent<InvadersPlayerShip>().Die();
            gameObject.SetActive(false);
        }
    }
	
	void Update ()
	{
	    Lifetime -= Time.deltaTime;
        if(Lifetime<=0f) gameObject.SetActive(false);

	    transform.Translate(Velocity, Space.World);
        transform.rotation = Quaternion.LookRotation(Velocity);
	}


}
