using PicaVoxel;
using UnityEngine;
using System.Collections;

public class ShmupChopper : MonoBehaviour
{
    public float Speed;
    public float WeaponCooldown = 0.1f;
    public Vector3 MovementExtents;

    private Exploder grassExploder;
    private float explodeTime;
    private float cooldownTimer;
    private ParticleSystem explodeParticleSystem;

	void Start ()
	{
        GetComponent<CollisionDetector>().Collided += ShmupChopper_Collided;
        grassExploder = transform.Find("GrassExploder").GetComponent<Exploder>();
        explodeParticleSystem = GameObject.Find("Explode Particles").GetComponent<ParticleSystem>();
	}

	void Update () {
        // Horizontal movement / tilting
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f).normalized;

	    if (Input.GetAxis("Horizontal") > 0f)
	    {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, 340, 0), Time.deltaTime*Speed * 15f);
            if(transform.position.x<MovementExtents.x) transform.Translate(new Vector3(movement.x, 0, 0f) * (Time.deltaTime * Speed), Space.World);
	    }
        else if (Input.GetAxis("Horizontal") < 0f)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, 20, 0), Time.deltaTime * Speed * 15f);
            if (transform.position.x > -MovementExtents.x) transform.Translate(new Vector3(movement.x, 0, 0f) * (Time.deltaTime * Speed), Space.World);
        }
        else transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(transform.localRotation.eulerAngles.x, 0, 0), Time.deltaTime * Speed * 15f);

        // Verticel movement / tilting
        if (Input.GetAxis("Vertical") > 0f)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(20, 0, 0), Time.deltaTime * Speed * 15f);
            if (transform.position.y < MovementExtents.y) transform.Translate(new Vector3(0f, movement.y, 0f) * (Time.deltaTime * Speed), Space.World);
        }
        else if (Input.GetAxis("Vertical") < 0f)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(340, 0, 0), Time.deltaTime * Speed * 15f);
            if (transform.position.y > -MovementExtents.y) transform.Translate(new Vector3(0f, movement.y , 0f) * (Time.deltaTime * Speed), Space.World);
        }
        else transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0), Time.deltaTime * Speed * 15f);

        // Shootin'
	    cooldownTimer -= Time.deltaTime;
	    if (Input.GetButton("Jump") && cooldownTimer <= 0f)
	    {
	        cooldownTimer = WeaponCooldown;
            ShmupBulletManager.Instance.Spawn(this, transform.position + new Vector3(1.5f, 0f, 0.2f), new Vector3(0.5f, 0f, 0f), 1f);
	    }

        // Handle the destruction of shrubberies!
        explodeTime += Time.deltaTime;
        if (explodeTime >= 0.25f)
        {         
            explodeTime = 0f;
            grassExploder.Explode(new Vector3(0, 0, -3f));
        }
	}

    void ShmupChopper_Collided(Volume collisonObject, Voxel voxel, Vector3 worldPosition)
    {
        if (collisonObject.gameObject == gameObject) return;
        if(!(collisonObject.name.StartsWith("Playfield") || collisonObject.name.StartsWith("Plane"))) return;

        // Destruct the volume a few times with different velocities to make lots of particles!
        GetComponent<Volume>().Destruct(0f, false);
        GetComponent<Volume>().Destruct(5f, false);
        explodeParticleSystem.transform.position = transform.position;
        explodeParticleSystem.Emit(30);
        gameObject.SetActive(false);
        Invoke("Respawn", 3f);
    }

    void Respawn()
    {
        transform.position = new Vector3(-8f, 0f, -4f);
        gameObject.SetActive(true);
    }
}
