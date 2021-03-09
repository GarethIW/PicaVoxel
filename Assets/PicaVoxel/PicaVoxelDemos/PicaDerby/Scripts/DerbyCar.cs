using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using PicaVoxel;
using UnityEngine.UI;
using Random = System.Random;

public class DerbyCar : MonoBehaviour
{
    private Exploder exploder;

    public Color Color;

    public bool IsPlayerControlled;

    public List<AxleInfo> axleInfos; // the information about each individual axle
    public float maxMotorTorque; // maximum torque the motor can apply to wheel
    public float maxSteeringAngle; // maximum steer angle the wheel can have


    private float steerAmount;

    private float resetTime = 0f;
    private bool resetting = false;
    private Vector3 targetResetPosition;
    private Quaternion targetResetRotation;

    private Camera mainCamera;
    private Vector3 cameraStartPosition;

    private Rigidbody rigidBody;

    // AI vars
    private DerbyCar targetCar;
    private float stuckTime = 0f;
    private float reverseTime = 0f;


  
	void Start ()
	{
        // Find this car's Exploder so we can destroy some voxels when it hits something
	    exploder = transform.Find("Exploder").GetComponent<Exploder>();

	    rigidBody = GetComponent<Rigidbody>();
	    StartCoroutine(InitPhysics());

        // Set the car's tint color!
        Material m = new Material(transform.Find("Body").GetComponent<Volume>().Material);
        m.SetColor("_Tint", Color);
	    transform.Find("Body").GetComponent<Volume>().Material = m;
        transform.Find("Body").GetComponent<Volume>().UpdateAllChunks();

        // If this car is player controllerd, it should have a camera attached
	    if (IsPlayerControlled)
	    {
	        mainCamera = transform.Find("Main Camera").GetComponent<Camera>();
	        cameraStartPosition = mainCamera.transform.localPosition;
	    }
	}

    // This is a "hack" to get the wheelcolliders working correctly after the car's runtime-only volume meshes are regenerated.
    IEnumerator InitPhysics()
    {
        transform.Translate(Vector3.up * 0.01f);

        yield return new WaitForSeconds(2);
    }

    void Update ()
	{
	    float motor = 0f;
	    float steering = 0f;

        if (!resetting)
	    {
	        if (IsPlayerControlled)
	        {
                // Turn the wheels and add acceleration/braking as per player input
                motor = maxMotorTorque* Input.GetAxis("Vertical");
                steering = maxSteeringAngle* Input.GetAxis("Horizontal");

         
                // Do something with the camera
	            mainCamera.transform.localPosition = Vector3.Slerp(mainCamera.transform.localPosition, cameraStartPosition + new Vector3(steerAmount*(rigidBody.velocity.magnitude*0.1f), 0f, 0f), Time.deltaTime * 10f);
                mainCamera.transform.localRotation = Quaternion.RotateTowards(mainCamera.transform.localRotation, Quaternion.Euler(20, 0, steerAmount*5f), Time.deltaTime * 10f);
	        }
	        else
	        {
                // Do some AI!

                // Find a car to target (but not myself!)
	            if (targetCar == null)
	            {
	                GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");
	                int newTarget = UnityEngine.Random.Range(0, cars.Length);
	                if (cars[newTarget] != gameObject)
	                {
	                    targetCar = cars[newTarget].GetComponent<DerbyCar>();
	                }
	            }
	            else
	            {
                    // Attempt to steer toward target car
	                Vector3 v3angle = targetCar.transform.position - transform.position;
	                float angle = Vector3.Cross(transform.forward, v3angle).y;
	             
	                if (angle > 2f)
	                {
	                    steerAmount += 0.025f;
	                }
                    else if (angle < 2f)
                    {
                        steerAmount -= 0.025f;
                    }
                    else if (steerAmount < 0f) steerAmount += 0.1f;
                    else if (steerAmount > 0f) steerAmount -= 0.1f;
	                steerAmount = Mathf.Clamp(steerAmount, -1f, 1f);

	                if (reverseTime <= 0f)
	                    motor = maxMotorTorque*1f;
                    else
                        motor = maxMotorTorque * -1f;


                    steering = maxSteeringAngle*steerAmount;
	            }

                // If we're not moving, add to the stuck timer
                if (rigidBody.velocity.magnitude > 1f)
	                stuckTime = 0f;
	            else
	                stuckTime += Time.deltaTime;

                // If we've been stuck for 3 seconds, allow us to reverse for 5 seconds
	            if (stuckTime > 3f)
	            {
	                stuckTime = 0f;
	                reverseTime = 5f;
	            }

                // If we've been stuck for 8 seconds, change target
	            if (stuckTime > 8f)
	            {
	                targetCar = null;
	            }

	            if (reverseTime > 0f)
	                reverseTime -= Time.deltaTime;

                // Randomly change target now and then
	            if (UnityEngine.Random.Range(0, 500) == 0)
	                targetCar = null;
	        }

	    }

        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
            if (axleInfo.motor)
            {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }
        }

        // Steering
        //   steerAmount = Mathf.Clamp(steerAmount, -1f, 1f);
        transform.Find("Wheels/WheelFL").localRotation = Quaternion.Euler(0, 0f + (steering), 0f);
        transform.Find("Wheels/WheelFR").localRotation = Quaternion.Euler(0, 0f + (steering), 0f);

        //Check to see if the car has fallen over (greater than 80 degrees of tilt around Z)
        // If it has, wait 3 seconds before resetting the position
        if (Mathf.Abs(Mathf.DeltaAngle(transform.rotation.eulerAngles.z, 0f)) > 80f && resetTime <= 0f && !resetting)
            resetTime = 3f;

        // We've been stuck for long enough!
        if (resetTime > 0f)
        {
            resetTime -= Time.deltaTime;
            if (resetTime <= 0f)
            {
                resetting = true;

                // Set position and rotation targets to lerp towards
                targetResetPosition = transform.position + new Vector3(0f, 2f, 0f);
                targetResetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
            }
        }

        // Reset the car's position
        if (resetting)
        {
            // Turn off gravity while we're being lifted up
            GetComponent<Rigidbody>().Sleep();
            GetComponent<Rigidbody>().useGravity = false;

            // Lerp towards the correct rotation and a raised position
            transform.position = Vector3.Slerp(transform.position, targetResetPosition, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetResetRotation, Time.deltaTime * 5f);

            // Once the rotation is upright again, drop the car
            if (Vector3.Distance(transform.position, targetResetPosition) < 0.2f && Mathf.Abs(Mathf.DeltaAngle(transform.rotation.eulerAngles.z, 0f)) < 5f)
            {
                resetting = false;
                GetComponent<Rigidbody>().WakeUp();
                GetComponent<Rigidbody>().useGravity = true;
            }
        }
    }

    void OnCollisionEnter(Collision col)
    {
        //return;
        // Something has collided with the car, so we'll find out where in the scene the collision occured
        // We'll average out the contact points to give a median position
        Vector3 avg = Vector3.zero;
        foreach (ContactPoint cp in col.contacts) avg += cp.point;
        if(col.contacts.Length>1)
            avg /= (float)col.contacts.Length;
            
        // Set the Exploder's position to the average collision position
        exploder.transform.position = avg;

        // Just for effect, we're going to move the collision point up a couple of voxels:
        exploder.transform.position += new Vector3(0f,0.25f,0f);

        // We'll give our explosion particles some upward velocity - also for effect
        exploder.Explode(new Vector3(0f,7f,0f));
    }

    [System.Serializable]
    public class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor; // is this wheel attached to motor?
        public bool steering; // does this wheel apply steer angle?
    }

}
