using UnityEngine;
using System.Collections;
using PicaVoxel;

public class InvadersInvader : MonoBehaviour
{
    public InvadersWave Wave;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        transform.Translate((Wave.Speed * Wave.Direction) * Time.deltaTime, 0f, 0f, Space.World);

        // Check if we've reached the width boundary of the wave
	    if (transform.position.x >= Wave.Width || transform.position.x <= -Wave.Width)
	    {
	        Wave.BoundaryReached(transform.position.x);
	    }

        // Rotate in direction of movement slightly
	    if (Wave.Direction < 0f)
	        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, 20, 0), 3f);
        if (Wave.Direction > 0f)
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0, 340, 0), 3f);

        // Maybe fire a bullet?
	    if (Random.Range(0, 1000) == 0)
	    {
            InvadersBulletManager.Instance.Spawn(this, transform.position + new Vector3(0f, -0.5f, 0f), new Vector3(0f, -0.1f, 0f), 3f);
	    }
	}
}
