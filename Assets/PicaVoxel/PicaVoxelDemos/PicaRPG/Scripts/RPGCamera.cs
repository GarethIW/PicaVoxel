using System;
using UnityEngine;
using System.Collections;

public class RPGCamera : MonoBehaviour
{
    public enum CameraType
    {
        Orthographic,
        Isometric
    }

    public Transform Character;

    public CameraType Type;

    public Vector3 OffsetOrthographic;
    public Vector3 OffsetIsometric;

    public Vector3 MinOrthographic;
    public Vector3 MaxOrthographic;
    public Vector3 MinIsometric;
    public Vector3 MaxIsometric;


	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update ()
	{
	    Vector3 target = Vector3.zero;

	    switch (Type)
	    {
	        case CameraType.Orthographic:
	            target = Character.position + OffsetOrthographic;
	            target.x = Mathf.Clamp(target.x, MinOrthographic.x, MaxOrthographic.x);
	            target.y = Mathf.Clamp(target.y, MinOrthographic.y, MaxOrthographic.y);
	            target.z = Mathf.Clamp(target.z, MinOrthographic.z, MaxOrthographic.z);

	            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(45f, 0f, 0f), 0.1f);

	            break;
	        case CameraType.Isometric:
                target = Character.position + OffsetIsometric;
	            target.x = Mathf.Clamp(target.x, MinIsometric.x, MaxIsometric.x);
                target.y = Mathf.Clamp(target.y, MinIsometric.y, MaxIsometric.y);
                target.z = Mathf.Clamp(target.z, MinIsometric.z, MaxIsometric.z);

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(45f, 45f, 0f), 0.1f);
	            break;
	    }
	    transform.position = Vector3.Lerp(transform.position, target, 0.1f);

	    if (Input.GetAxis("Mouse ScrollWheel") < 0)
	        GetComponent<Camera>().orthographicSize += 0.5f;

        if (Input.GetAxis("Mouse ScrollWheel") > 0)
            GetComponent<Camera>().orthographicSize -= 0.5f;

	    if (Input.GetKeyDown(KeyCode.Space))
	        Type = (Type == CameraType.Isometric) ? CameraType.Orthographic : CameraType.Isometric;

	}
}
