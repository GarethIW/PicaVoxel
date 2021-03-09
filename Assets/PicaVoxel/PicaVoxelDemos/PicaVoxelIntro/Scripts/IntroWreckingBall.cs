using System.Collections.Generic;
using PicaVoxel;
using UnityEngine;
using System.Collections;

public class IntroWreckingBall : MonoBehaviour
{
    private Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();  
    private bool explodePlayed = false;
    private float explodeTimer = 0f;

	// Use this for initialization
	void Start ()
	{
        foreach (AudioSource a in GetComponents<AudioSource>())
        {
            sounds.Add(a.clip.name, a);
        }
	}
	
	// Update is called once per frame
	void Update () {
	    transform.Translate(transform.forward * Time.deltaTime * 25f);

	}

    void FixedUpdate()
    {
        explodeTimer += Time.deltaTime;
        if (explodeTimer >= 0.05f)
        {
            explodeTimer = 0f;
            GetComponent<Exploder>().Explode();
        }

    }

    void OnCollisionEnter(Collision col)
    {
        if ((col.collider.transform.root.name == "Island1" || col.collider.transform.root.name == "Logo") && !explodePlayed)
        {
            explodePlayed = true;

            sounds["explosion"].Play();

            GetComponent<Rigidbody>().Sleep();
        }
    }
}
