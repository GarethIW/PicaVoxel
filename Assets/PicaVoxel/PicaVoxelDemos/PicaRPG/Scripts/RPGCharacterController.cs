using PicaVoxel;
using UnityEngine;
using System.Collections;

public class RPGCharacterController : MonoBehaviour
{
    public BasicAnimator TopAnimator;
    public BasicAnimator BottomAnimator;

    private bool falling = false;
    private bool climbing = false;
    private float fallSpeed = 0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetAxis("Horizontal") < 0f)
	    {
	        transform.rotation = Quaternion.Euler(0, 270, 0);
	       // transform.Translate(new Vector3(-0.05f, 0f, 0f));
	    }
        if (Input.GetAxis("Horizontal") > 0f) 
	    {
            transform.rotation = Quaternion.Euler(0, 90, 0);
           // transform.Translate(new Vector3(0.05f, 0f, 0f));
	    }
	    if (Input.GetAxis("Vertical") >0f)
	    {
            transform.rotation = Quaternion.Euler(0,0, 0);
	       // transform.Translate(new Vector3(0f, 0f, -0.05f));
	    }
	    if (Input.GetAxis("Vertical") < 0f)
	    {
            transform.rotation = Quaternion.Euler(0, 180, 0);
	       // transform.Translate(new Vector3(0f, 0f, 0.05f));
	    }

        if ((Mathf.Abs(Input.GetAxis("Horizontal")) >= 0.1f || Mathf.Abs(Input.GetAxis("Vertical"))>= 0.1f) && !falling && !climbing)
        {
            Vector3 speed = CheckCollisions(Vector3.forward*0.075f);
            transform.Translate(speed);
	        TopAnimator.Play(); 
            BottomAnimator.Play();
	    }
        else
        { TopAnimator.Pause(); BottomAnimator.Pause(); }

        // Falling
	    CheckFallCollisions();
	    if (falling)
	    {
	        fallSpeed += 0.01f;
	        transform.Translate(Vector3.down*fallSpeed);
	    }
	    else
	    {
            // We're not falling, so find the height of the ground beneath our feet and set the "climbing" flag
            //for(float y=0.1f;y<2f;y+=0.1f)
	        if (IsVoxelAtPoint(transform.position + new Vector3(0, 0.1f, 0)))
	        {
	            climbing = true;
	            transform.Translate(Vector3.up*(0.1f));
	        }
	        else climbing = false;
	        fallSpeed = 0f;
	    }
	}

    Vector3 CheckCollisions(Vector3 speed)
    {
        // We'll check at 0.8f above the character's base. This will allow the character to climb a 1 voxel height but collide with anything higher.
        // Our forward speed is 0.075, so we'll multiply by 5 to get a decent unit distance in front of the character to check for collisions.

        for (float y = 0.8f; y < 2f; y += 0.1f)
        {
            Vector3 checkPos = transform.TransformPoint((speed*5f) + (Vector3.up*y));

            Debug.DrawLine(transform.position, checkPos, Color.red);

            if (IsVoxelAtPoint(checkPos)) 
                speed = Vector3.zero;
        }

        return speed;
    }

    void CheckFallCollisions()
    {
        if (!IsVoxelAtPoint(transform.position + new Vector3(-0.2f, 0f, 0f)) &&
            !IsVoxelAtPoint(transform.position + new Vector3(0.2f, 0f, 0f)) &&
            !IsVoxelAtPoint(transform.position + new Vector3(0f, -0.2f, 0f)) &&
            !IsVoxelAtPoint(transform.position + new Vector3(0f, 0.2f, 0f)))
        {
            if (!falling) fallSpeed = 0.1f;
            falling = true;
        }
        else falling = false;
    }

    bool IsVoxelAtPoint(Vector3 checkPos)
    {
        foreach (GameObject o in GameObject.FindGameObjectsWithTag("PicaVoxelVolume"))
        {
            // Dont collide with this character's own voxel volumes!
            if (o.transform.parent == this.transform) continue;

            Volume pvo = o.GetComponent<Volume>();

            Voxel? pv = pvo.GetVoxelAtWorldPosition(checkPos);
            if (pv.HasValue && pv.Value.Active) return true;
        }

        return false;
    }
}
