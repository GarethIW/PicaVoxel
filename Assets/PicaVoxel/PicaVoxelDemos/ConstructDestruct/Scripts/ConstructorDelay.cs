using PicaVoxel;
using UnityEngine;
using System.Collections;

public class ConstructorDelay : MonoBehaviour
{

    public float Delay = 0.5f;

	// Use this for initialization
	void Start () {
	    Invoke("StartConstructor", Delay);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void StartConstructor()
    {
        transform.Find("Constructor").GetComponent<Constructor>().gameObject.SetActive(true);
    }
}
