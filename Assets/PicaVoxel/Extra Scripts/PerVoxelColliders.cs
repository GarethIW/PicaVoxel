using PicaVoxel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerVoxelColliders : MonoBehaviour {

    // Having a boxcollider per voxel is *really* not recommended, but here we go!
    // This only works with volumes with one animation frame.

    public GameObject ColliderHolder;

    private Volume volume;
    

	// Use this for initialization
	void Start () {
        volume = GetComponent<Volume>();

        if(ColliderHolder == null)
            GenerateColliders();
	}

    public void GenerateColliders()
    {
        if (volume == null) volume = GetComponent<Volume>();

        // Create a gameobject to hold the colliders if needed
        if (ColliderHolder == null)
            ColliderHolder = new GameObject("Per-Voxel Colliders");

        ColliderHolder.transform.SetParent(volume.transform, false);
        ColliderHolder.transform.localPosition = -volume.Pivot;

        // Remove all current colliders
        // If we were going to update when voxels changed, we would probably want to keep the current colliders in a dictionary(?) and remove/add only if needed
        var cols = ColliderHolder.GetComponents<BoxCollider>();
        for (var i = cols.Length - 1; i >= 0; i--)
            DestroyImmediate(cols[i]);

        for (var x = 0; x < volume.XSize; x++)
            for (var y = 0; y < volume.YSize; y++)
                for (var z = 0; z < volume.ZSize; z++)
                {
                    if (volume.Frames[0].Voxels[x + volume.XSize * (y + volume.YSize * z)].Active && HasExposedFace(x, y, z)) AddCollider(new Vector3(x * volume.VoxelSize, y * volume.VoxelSize, z * volume.VoxelSize));
                }
    }

    private void AddCollider(Vector3 center)
    {
        var box = ColliderHolder.AddComponent<BoxCollider>();
        box.center = center + (Vector3.one * volume.VoxelSize*0.5f);
        box.size = Vector3.one * (volume.VoxelSize);
    }

    private bool HasExposedFace(int x, int y, int z)
    {
        // Voxel is on the edge of a volume, so it is exposed
        if (x == 0 || y == 0 || z == 0 || x == volume.XSize - 1 || y == volume.YSize - 1 || z == volume.ZSize - 1) return true;

        var va = volume.Frames[0].Voxels;

        if (!va[(x-1) + volume.XSize * (y + volume.YSize * z)].Active) return true;
        if (!va[(x+1) + volume.XSize * (y + volume.YSize * z)].Active) return true;
        if (!va[x + volume.XSize * ((y-1) + volume.YSize * z)].Active) return true;
        if (!va[x + volume.XSize * ((y+1) + volume.YSize * z)].Active) return true;
        if (!va[x + volume.XSize * (y + volume.YSize * (z-1))].Active) return true;
        if (!va[x + volume.XSize * (y + volume.YSize * (z+1))].Active) return true;

        return false;
    }
	
}


