using PicaVoxel;
using UnityEngine;
using System.Collections;

public class ShmupPlane : MonoBehaviour
{
    public Vector3 Velocity;

    private ParticleSystem jetParticleSystem;
    private ParticleSystem explodeParticleSystem;

    void Start()
    {
        jetParticleSystem = GameObject.Find("Plane Jet Particles").GetComponent<ParticleSystem>();
        explodeParticleSystem = GameObject.Find("Explode Particles").GetComponent<ParticleSystem>();
    }

    void Update()
    {
        transform.Translate(Velocity * Time.deltaTime, Space.World);
        transform.Rotate(Time.deltaTime * 360f, 0f, 0f, Space.World);

        if (transform.position.x < -30f) gameObject.SetActive(false);
        jetParticleSystem.transform.position = transform.position + new Vector3(1f, 0f, 0f);
        jetParticleSystem.Emit(3);
    }

    public void Init(Vector3 position, Vector3 velocity)
    {
        transform.position = position;
        Velocity = velocity;
        transform.rotation = Quaternion.identity;

        gameObject.SetActive(true);
    }

    public void Die()
    {
        GetComponent<Volume>().Destruct(2f, false);
        explodeParticleSystem.transform.position = transform.position;
        explodeParticleSystem.Emit(30);
        gameObject.SetActive(false);
    }
}
