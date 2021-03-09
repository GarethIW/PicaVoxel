/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////

using System.Threading;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicaVoxel
{
    [AddComponentMenu("PicaVoxel/Utilities/Exploder")]
    public class Exploder : MonoBehaviour
    {
        public enum ExplodeTargets
        {
            All,
            AllButSelf,
            SelfOnly
        }

        public enum ExplodeValueFilterOperation
        {
            LessThan,
            LessThanOrEqualTo,
            EqualTo,
            GreaterThanOrEqualTo,
            GreaterThan
        }

        public string Tag = "PicaVoxelVolume";
        public float ExplosionRadius = 2f;
        public float ParticleVelocity = 2f;
        public ExplodeTargets ExplodeTarget = ExplodeTargets.All;
        public ExplodeValueFilterOperation ValueFilterOperation = ExplodeValueFilterOperation.GreaterThanOrEqualTo;
        public int ValueFilter = 0;

        public void Explode()
        {
            Explode(Vector3.zero);
        }
        public void Explode(Vector3 additionalVelocity)
        {
            foreach (Volume pvo in GameObject.FindObjectsOfType<Volume>())
            {
                if (ExplodeTarget == ExplodeTargets.AllButSelf && pvo.transform.root == transform.root) continue;
                if (ExplodeTarget == ExplodeTargets.SelfOnly && pvo.transform.root != transform.root) continue;

                //Volume pvo = o.GetComponent<Volume>();
                if (pvo == null) continue;
                Vector3 cpob = pvo.Hitbox.ClosestPointOnBounds(transform.position);

                if (Vector3.Distance(transform.position, cpob) <= ExplosionRadius ||
                    pvo.GetVoxelAtWorldPosition(transform.position) != null)
                {
                    Batch batch = pvo.Explode(transform.position, ExplosionRadius, ValueFilter, ValueFilterOperation);

                    if (batch.Voxels.Count > 0 && VoxelParticleSystem.Instance != null)
                        VoxelParticleSystem.Instance.SpawnBatch(batch,
                            pos =>
                                (((pos + Random.insideUnitSphere) - transform.position)*
                                ((Random.Range(ParticleVelocity - 1f, ParticleVelocity + 1f))))+additionalVelocity);

                    batch.Dispose();
                }

            }

        }


    }
}