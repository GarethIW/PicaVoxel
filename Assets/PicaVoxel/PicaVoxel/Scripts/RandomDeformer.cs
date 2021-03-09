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
using Random = UnityEngine.Random;

namespace PicaVoxel
{
    [AddComponentMenu("PicaVoxel/Utilities/Random Deformer")]
    [Serializable]
    public class RandomDeformer : MonoBehaviour
    {
        public PicaVoxelBox ConstrainBox;
        public bool ConstrainToBox;
        public bool AddVoxels;

        public float Interval = 1f;
        public int NumVoxels = 1;

        private Volume voxelObject;
        private float currentTime = 0f;

        // Use this for initialization
        private void Start()
        {
            voxelObject = GetComponent<Volume>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (voxelObject == null) return;

            currentTime += Time.deltaTime;
            if (currentTime >= Interval)
            {
                currentTime = 0;

                for (int i = 0; i < NumVoxels; i++)
                {
                    PicaVoxelPoint p =
                        new PicaVoxelPoint(
                            ConstrainToBox
                                ? Random.Range(ConstrainBox.BottomLeftFront.X, ConstrainBox.TopRightBack.X)
                                : Random.Range(0, voxelObject.XSize),
                            ConstrainToBox
                                ? Random.Range(ConstrainBox.BottomLeftFront.Y, ConstrainBox.TopRightBack.Y)
                                : Random.Range(0, voxelObject.YSize),
                            ConstrainToBox
                                ? Random.Range(ConstrainBox.BottomLeftFront.Z, ConstrainBox.TopRightBack.Z)
                                : Random.Range(0, voxelObject.ZSize));

                    voxelObject.SetVoxelAtArrayPosition(p,
                        new Voxel()
                        {
                            State = AddVoxels?VoxelState.Active : VoxelState.Hidden,
                            Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f))
                        });
                }
            }
        }
    }
}