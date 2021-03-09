/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////

using System;
using PicaVoxel;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

namespace PicaVoxel
{
    [AddComponentMenu("PicaVoxel/Utilities/Destructor")]
    public class Destructor : MonoBehaviour
    {
        public Volume PicaVoxelVolume;
        public float StepDelay = 0.1f;
        public float StartRadius = 0f;
        public float StopRadius = 0f;

        public bool CreateParticles;

        private float radius;
        private float currentTime = 0f;

        // Use this for initialization
        private void Start()
        {
            radius = StartRadius;
            gameObject.SetActive(false);
        }

        // Update is called once per frame
        private void Update()
        {
            if (PicaVoxelVolume == null) return;

            currentTime += Time.deltaTime;
            if (currentTime >= StepDelay)
            {
                currentTime = 0;
                radius += PicaVoxelVolume.VoxelSize;
                Destruct();
            }

            if (radius >= StopRadius) gameObject.SetActive(false);
        }

        private void Destruct()
        {
            Batch batch = new Batch(PicaVoxelVolume);

            Vector3 posZero = PicaVoxelVolume.transform.position +
                              (transform.rotation*
                               (-PicaVoxelVolume.Pivot + (Vector3.one*(PicaVoxelVolume.VoxelSize*0.5f))));
            Vector3 oneX = PicaVoxelVolume.transform.rotation*(new Vector3(PicaVoxelVolume.VoxelSize, 0, 0));
            Vector3 oneY = PicaVoxelVolume.transform.rotation*(new Vector3(0f, PicaVoxelVolume.VoxelSize, 0));
            Vector3 oneZ = PicaVoxelVolume.transform.rotation*(new Vector3(0, 0, PicaVoxelVolume.VoxelSize));

            Vector3 constructorPosition = transform.position;

            Frame frame = PicaVoxelVolume.GetCurrentFrame();

            //int numVox = 0;

            Vector3 checkPos = posZero;
            for (int x = 0; x < PicaVoxelVolume.XSize; x++)
            {
                Vector3 xmult = oneX*x;
                for (int y = 0; y < PicaVoxelVolume.YSize; y++)
                {
                    Vector3 ymult = oneY*y;
                    for (int z = 0; z < PicaVoxelVolume.ZSize; z++)
                    {
                        Vector3 zmult = oneZ*z;
                        checkPos.x = posZero.x + xmult.x + ymult.x + zmult.x;
                        checkPos.y = posZero.y + xmult.y + ymult.y + zmult.y;
                        checkPos.z = posZero.z + xmult.z + ymult.z + zmult.z;

                        if (frame.Voxels[x + PicaVoxelVolume.XSize*(y + PicaVoxelVolume.YSize*z)].Active &&
                            Vector3.Distance(constructorPosition, checkPos) <= radius)
                        {
                            //numVox++;
                            Voxel v = frame.Voxels[x + PicaVoxelVolume.XSize*(y + PicaVoxelVolume.YSize*z)];
                            v.Color *= PicaVoxelVolume.Material.GetColor("_Tint");
                            batch.Add(v, x, y, z, checkPos);
                            frame.SetVoxelStateAtArrayPosition(x,y,z,VoxelState.Hidden);
                            //frame.SetChunkAtVoxelPositionDirty(x, y, z);
                        }
                    }
                }
            }

            frame.UpdateChunks(false);

            if (CreateParticles && batch.Voxels.Count > 0 && VoxelParticleSystem.Instance != null)
                VoxelParticleSystem.Instance.SpawnBatch(batch,
                    pos =>
                        ((transform.position - (pos + Random.insideUnitSphere))*
                         1f));

            batch.Dispose();
        }
    }
}
