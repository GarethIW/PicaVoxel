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
using UnityEngine;
using System.Collections;

namespace PicaVoxel
{
   // [AddComponentMenu("")]
    public class VoxelParticleSystem : MonoBehaviour
    {
        public static VoxelParticleSystem Instance;

        public float ParticleLifetime;
        public int MaxBatchParticles;
        public ParticleSystem System;

        public string CollisionTag = "PicaVoxelVolume";
        public bool CollidePositiveX;
        public bool CollideNegativeX;
        public bool CollidePositiveY;
        public bool CollideNegativeY;
        public bool CollidePositiveZ;
        public bool CollideNegativeZ;

        public float BounceMultiplier = 0.5f;

        private ParticleSystem.Particle[] parts;

        // Use this for initialization
        private void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            System = GetComponent<ParticleSystem>();
            parts = new ParticleSystem.Particle[System.main.maxParticles];

            for (int i = 0; i < System.main.maxParticles; i++)
            {
                System.Emit(1);
            }
            System.Clear();

        }

        // Update is called once per frame
        private void Update()
        {
            
            int numParts = System.GetParticles(parts);
            if (numParts > 0 && (CollideNegativeX || CollideNegativeY || CollideNegativeZ || CollidePositiveX || CollidePositiveY || CollidePositiveZ))
            {
                foreach (GameObject o in GameObject.FindGameObjectsWithTag(CollisionTag))
                {
                    Volume pvo = o.GetComponent<Volume>();

                    for (int p=0;p<numParts;p++)
                    {
                        if (CollideNegativeX && parts[p].velocity.x < 0)
                        {
                            Voxel? v = pvo.GetVoxelAtWorldPosition(parts[p].position + new Vector3(-parts[p].GetCurrentSize(System), 0, 0));
                            if (v.HasValue && v.Value.Active) parts[p].velocity = new Vector3(-(parts[p].velocity.x * BounceMultiplier), parts[p].velocity.y, parts[p].velocity.z);
                        }
                        if (CollidePositiveX && parts[p].velocity.x > 0)
                        {
                            Voxel? v = pvo.GetVoxelAtWorldPosition(parts[p].position + new Vector3(parts[p].GetCurrentSize(System), 0, 0));
                            if (v.HasValue && v.Value.Active) parts[p].velocity = new Vector3(-(parts[p].velocity.x * BounceMultiplier), parts[p].velocity.y, parts[p].velocity.z);
                        }
                        if (CollideNegativeY && parts[p].velocity.y < 0)
                        {
                            Voxel? v = pvo.GetVoxelAtWorldPosition(parts[p].position + new Vector3(0,-parts[p].GetCurrentSize(System), 0));
                            if (v.HasValue && v.Value.Active) parts[p].velocity = new Vector3(parts[p].velocity.x, -(parts[p].velocity.y * BounceMultiplier), parts[p].velocity.z);
                        }
                        if (CollidePositiveY && parts[p].velocity.y > 0)
                        {
                            Voxel? v = pvo.GetVoxelAtWorldPosition(parts[p].position + new Vector3(0,parts[p].GetCurrentSize(System), 0));
                            if (v.HasValue && v.Value.Active) parts[p].velocity = new Vector3(parts[p].velocity.x, -(parts[p].velocity.y * BounceMultiplier), parts[p].velocity.z);
                        } 
                        if (CollideNegativeZ && parts[p].velocity.z < 0)
                        {
                            Voxel? v = pvo.GetVoxelAtWorldPosition(parts[p].position + new Vector3(0,0,-parts[p].GetCurrentSize(System)));
                            if (v.HasValue && v.Value.Active) parts[p].velocity = new Vector3(parts[p].velocity.x, parts[p].velocity.y, -(parts[p].velocity.z * BounceMultiplier));
                        }
                        if (CollidePositiveZ && parts[p].velocity.z > 0)
                        {
                            Voxel? v = pvo.GetVoxelAtWorldPosition(parts[p].position + new Vector3(0,0,parts[p].GetCurrentSize(System)));
                            if (v.HasValue && v.Value.Active) parts[p].velocity = new Vector3(parts[p].velocity.x, parts[p].velocity.y, -(parts[p].velocity.z * BounceMultiplier));
                        }

                       
                    }
                }
                System.SetParticles(parts,numParts);
            }

        }

        public void SpawnSingle(Vector3 worldPos, Voxel voxel, float voxelSize, Vector3 velocity)
        {
            System.Emit(new ParticleSystem.EmitParams()
            {
                position = worldPos,
                velocity = velocity,
                startSize = voxelSize,
                startLifetime = ParticleLifetime,
                startColor = voxel.Color
            }, 1);
            //worldPos, velocity, voxelSize, ParticleLifetime, voxel.Color)};
        }

        public void SpawnBatch(Batch batch, Func<Vector3, Vector3> velocityFunction)
        {
            int step = batch.Voxels.Count/(MaxBatchParticles >= 0 ? MaxBatchParticles : 100);
            if (step < 1) step = 1;
            for (int i = 0; i < batch.Voxels.Count; i += step)
            {
                SpawnSingle(batch.Voxels[i].WorldPosition, batch.Voxels[i].Voxel, batch.VoxelObject.VoxelSize,
                    velocityFunction(batch.Voxels[i].WorldPosition));
            }
        }
    }
}