using PicaVoxel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxCount : MonoBehaviour {

	// Use this for initialization
	void Start () {
        var frame = GetComponent<Volume>().GetCurrentFrame();

        // With LINQ
        Debug.Log(frame.Voxels.Count(v => v.State == VoxelState.Active));

        // Simple count
        var active = 0;
        for (var i = 0; i < frame.Voxels.Length; i++)
            if (frame.Voxels[i].State == VoxelState.Active) active++;
        Debug.Log(active);

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
