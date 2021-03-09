using PicaVoxel;
using UnityEngine;
using System.Collections;

public class DestructorDelay : MonoBehaviour
{

    public float Delay = 0.5f;

	// Use this for initialization
	void Start () {
        Invoke("StartDestructor", Delay);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void StartDestructor()
    {
        transform.Find("Destructor").GetComponent<Destructor>().gameObject.SetActive(true);
    }
}
