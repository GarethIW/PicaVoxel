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
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace PicaVoxel
{

    public delegate void OnVoxelCollision(Volume collisonObject, Voxel voxel, Vector3 worldPosition);

    [AddComponentMenu("PicaVoxel/Utilities/Collision Detector")]
    public class CollisionDetector : MonoBehaviour
    {
        public List<Vector3> DetectionPoints = new List<Vector3>() {Vector3.zero};

        public event OnVoxelCollision Collided;

        private void Start()
        {

        }

        private void FixedUpdate()
        {
            if (!gameObject.activeSelf || DetectionPoints.Count == 0 || Collided == null) return;

            foreach (GameObject o in GameObject.FindGameObjectsWithTag("PicaVoxelVolume"))
            {
                if(o==gameObject || !o.activeSelf) continue;

                Volume pvo = o.GetComponent<Volume>();

                foreach (Vector3 pos in DetectionPoints)
                {
                    Voxel? pv = pvo.GetVoxelAtWorldPosition(transform.position + pos);
                    if (pv.HasValue && pv.Value.Active)
                    {
                        Collided(pvo, pv.Value, transform.position + pos);
                        break;
                    }
                }

            }
        }

        public void ClearEvents()
        {
            if (Collided == null) return;

            foreach (Delegate d in Collided.GetInvocationList())
            {
                Collided -= (OnVoxelCollision)d;
            }
        }

        // Use DetectCollision to detect hits manually
        public bool DetectCollision(Vector3 worldPos, out Voxel voxel, out Volume hitObject)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag("PicaVoxelObject"))
            {
                Volume pvo = o.GetComponent<Volume>();
                Voxel? pv = pvo.GetVoxelAtWorldPosition(worldPos);
                if (pv.HasValue && pv.Value.Active)
                {
                    hitObject = pvo;
                    voxel = pv.Value;
                    return true;
                }
            }

            hitObject = null;
            voxel = new Voxel();
            return false;
        }

       
    }
}