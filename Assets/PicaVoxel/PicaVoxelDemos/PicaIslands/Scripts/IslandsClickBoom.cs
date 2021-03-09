using System;
using UnityEngine;
using System.Collections;
using PicaVoxel;
using Random = UnityEngine.Random;

public class IslandsClickBoom : MonoBehaviour
{
	void Start () {
	    
	}
	
	void Update () {
        // Left click
	    if (Input.GetMouseButtonDown(0))
	    {
            // Cast a ray from the camera position outward
	        Ray r = new Ray(GetComponent<Camera>().transform.position,GetComponent<Camera>().transform.forward);
            Debug.DrawRay(r.origin, r.direction*100f, Color.red, 10f);

	        RaycastHit hitInfo;
	        if (Physics.Raycast(r, out hitInfo)) // first, cast the ray using normal Unity physics. Don't forget to include a layer mask if needed
	        {
	            Volume pvo = hitInfo.collider.GetComponentInParent<Volume>();
	            if (pvo != null) // check to see if we have hit a PicaVoxel Volume. because the Hitbox is a child object on the Volume, we use GetComponentInParent
	            {
	                r = new Ray(hitInfo.point, r.direction); // now create a new ray starting at the hit position of the old ray
	                for (float d = 0; d < 50f; d += pvo.VoxelSize*0.5f) // iterate along the ray. we're using a maximum distance of 50 units here, you should adjust this to a sensible value for your scene
	                {
                        Voxel? v = pvo.GetVoxelAtWorldPosition(r.GetPoint(d)); // see if there's a voxel at the ray position
                        if (v.HasValue && v.Value.Active)
                        {
                            // We have a voxel, and it's active so cause an explosion at this location
                            Batch b = pvo.Explode(r.GetPoint(d), 2f, 0, Exploder.ExplodeValueFilterOperation.GreaterThanOrEqualTo);

                            // The delegate function here calculates a random particle velocity based on the position of the explosion
                            VoxelParticleSystem.Instance.SpawnBatch(b, pos =>
                                 (pos - r.GetPoint(d)) * Random.Range(0f, 2f));
                            
                            break;
                        }
                    }
                }
            }

	    }

        // Right click
	    if (Input.GetMouseButtonDown(1))
	    {
            // Cast a ray from the camera position outward
            Ray r = new Ray(GetComponent<Camera>().transform.position, GetComponent<Camera>().transform.forward);
            Debug.DrawRay(r.origin, r.direction * 100f, Color.red, 10f);
            bool found = false;
            // Test points along the ray until a voxel is found
            for (float d = 0; d < 50f; d += 0.05f)
            {
                // Loop through all the Volumes in the scene
                foreach (GameObject o in GameObject.FindGameObjectsWithTag("PicaVoxelVolume"))
                {
                    Volume pvo = o.GetComponent<Volume>();

                    // Attempt to get a voxel at this point on the ray (returns null if no voxel)
                    Voxel? v = pvo.GetVoxelAtWorldPosition(r.GetPoint(d));
                    if (v.HasValue && v.Value.Active)
                    {
                        // We have a voxel, and it's active, but we want to build a voxel so let's back up the ray a step
                        Vector3 buildPos = r.GetPoint(d - 0.05f);

                        // Check that there is a voxel here, and it is inactive
                        Voxel? v2 = pvo.GetVoxelAtWorldPosition(buildPos);
                        if (v2.HasValue && !v2.Value.Active)
                        {
                            // Set this voxel to active, with a random color!
                            pvo.SetVoxelAtWorldPosition(buildPos, new Voxel()
                            {
                                State = VoxelState.Active,
                                Color = new Color(Random.Range(0f,1f),Random.Range(0f,1f), Random.Range(0f,1f)),
                                Value = 128
                            });    
                        }

                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
	    }
	}
}
