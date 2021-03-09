using PicaVoxel;
using UnityEngine;
using System.Collections;
using UnityEngineInternal;

[AddComponentMenu("PicaVoxel/PicaVoxel Multi-part Volume")]
public class MultiPartVolume : MonoBehaviour
{
    public int XSize, YSize, ZSize;

    public Volume[,,] Volumes;

    public float VoxelSize = 0.1f;

    public Vector3 Pivot = Vector3.zero;

    public MeshingMode MeshingMode;
    public MeshingMode MeshColliderMeshingMode;
    public bool GenerateMeshColliderSeparately = false;
    public Material Material;

    public CollisionMode CollisionMode;
    public float SelfShadingIntensity = 0.2f;

	// Use this for initialization
	void Start ()
	{
	     GetPartReferences();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void RepositionParts()
    {
        if(Volumes==null) GetPartReferences();
        Vector3 currentPos = -Pivot;
        for (int x = 0; x < XSize; x++)
        {
            for (int y = 0; y < YSize; y++)
            {

                for (int z = 0; z < ZSize; z++)
                {
                    if (Volumes[x, y, z] != null)
                    {
                        Volumes[x, y, z].transform.position = currentPos;

                        currentPos.z += Volumes[x, y, z].ZSize*Volumes[x, y, z].VoxelSize;

                    }
                }
                if (Volumes[x, y, 0]!=null)
                    currentPos.y += Volumes[x, y, 0].ZSize*Volumes[x, y, 0].VoxelSize;
                currentPos.z = -Pivot.z;
            }
            if (Volumes[x, 0, 0] != null)
                currentPos.x += Volumes[x, 0, 0].ZSize * Volumes[x, 0, 0].VoxelSize;
            currentPos.y = -Pivot.y;
        }
    }

    public void SetPivotToCenter()
    {
        if(Volumes==null) GetPartReferences();

        Vector3 largestSize = Vector3.zero;
        Vector3 totalSize = Vector3.zero;

        for (int y = 0; y < YSize; y++)
        {
            for (int z = 0; z < ZSize; z++)
            {
                totalSize.x = 0;
                for(int x=0;x<XSize;x++)
                    if (Volumes[x, y, z] != null) totalSize.x += Volumes[x, y, z].XSize*Volumes[x, y, z].VoxelSize;

                if (totalSize.x > largestSize.x) largestSize.x = totalSize.x;
            }
        }

        for (int x = 0; x < XSize; x++)
        {
            for (int z = 0; z < ZSize; z++)
            {
                totalSize.y = 0;
                for (int y = 0; y < YSize; y++)
                    if (Volumes[x, y, z] != null) totalSize.y += Volumes[x, y, z].YSize * Volumes[x, y, z].VoxelSize;

                if (totalSize.y > largestSize.y) largestSize.y = totalSize.y;
            }
        }

        for (int x = 0; x < XSize; x++)
        {
            for (int y = 0; y < ZSize; y++)
            {
                totalSize.z = 0;
                for (int z = 0; z < ZSize; z++)
                    if (Volumes[x, y, z] != null) totalSize.z += Volumes[x, y, z].ZSize * Volumes[x, y, z].VoxelSize;

                if (totalSize.z > largestSize.z) largestSize.z = totalSize.z;
            }
        }

        Pivot = largestSize/2f;
        RepositionParts();
    }

    public void GetPartReferences()
    {
        Volumes = new Volume[XSize,YSize,ZSize];
        for (int i = 0; i < transform.childCount; i++)
        {
            Volume v = transform.GetChild(i).GetComponent<Volume>();
            if (v != null)
            {
                string location = v.name.Substring(v.name.IndexOf('('), v.name.Length - v.name.IndexOf('('));
                for(int x=0;x<XSize;x++)
                    for(int y=0;y<YSize;y++)
                        for(int z=0;z<=ZSize;z++)
                            if (location == "(" + x + "," + y + "," + z + ")")
                                Volumes[x, y, z] = v;
                            
            }
        }
    }
}
