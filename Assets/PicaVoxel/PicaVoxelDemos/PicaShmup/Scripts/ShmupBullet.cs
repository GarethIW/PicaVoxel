using PicaVoxel;
using UnityEngine;

public class ShmupBullet : MonoBehaviour 
{

    public Object Owner;
    public Vector3 Velocity;
    public float Lifetime;

    private ParticleSystem explodeParticles;
    private CollisionDetector detector;


    public void Init(Object owner, Vector3 position, Vector3 velocity, float life)
    {
        detector = GetComponent<CollisionDetector>();

        detector.ClearEvents();
        detector.Collided += detector_Collided;

        Owner = owner;
        transform.position = position;
        Velocity = velocity;
        Lifetime = life;

        gameObject.SetActive(true);
    }
	
	void Update () {
        Lifetime -= Time.deltaTime;
        if (Lifetime <= 0f) gameObject.SetActive(false);

        transform.Translate(Velocity, Space.World);
	}

    private void detector_Collided(Volume collisonObject, Voxel voxel, Vector3 worldPosition)
    {
        if (collisonObject.name.StartsWith("Plane") && Owner is ShmupChopper)
        {
            collisonObject.GetComponent<ShmupPlane>().Die();
            gameObject.SetActive(false);
        }
    }
}
