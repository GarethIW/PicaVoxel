using UnityEngine;
using System.Collections;
using PicaVoxel;
using UnityEngine.SceneManagement;

public class InvadersPlayerShip : MonoBehaviour
{
    public float XClamp = 14f;
    public float BulletCooldown = 0.5f;

    private float cooldownTime = 0f;

    private bool dead = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
	    if (dead) return;

        if (Input.GetAxis("Horizontal")<0f && transform.position.x>-XClamp) transform.Translate(new Vector3(-0.2f, 0f, 0f));
        if (Input.GetAxis("Horizontal")>0f && transform.position.x < XClamp) transform.Translate(new Vector3(0.2f, 0f, 0f));

	    if (Input.GetButton("Jump") && cooldownTime <= 0f)
	    {
	        cooldownTime = BulletCooldown;
	        InvadersBulletManager.Instance.Spawn(this, transform.position + new Vector3(0f,1f,0f), new Vector3(0f,0.3f,0f), 2f);
	    }

	    cooldownTime -= Time.deltaTime;
	}

    public void Die()
    {
        dead = true;
        Invoke("Reset", 3f);
    }

    void Reset()
    {
        SceneManager.LoadScene(0);
    }
}
