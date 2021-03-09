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
    public struct BatchVoxel
    {
        public Voxel Voxel;
        public PicaVoxelPoint ArrayPosition;
        public Vector3 WorldPosition;

        public BatchVoxel(Voxel voxel, PicaVoxelPoint arrayPoint, Vector3 worldPos)
        {
            Voxel = voxel;
            ArrayPosition = arrayPoint;
            WorldPosition = worldPos;
        }
    }

    public class Batch : IDisposable
    {
        public Volume VoxelObject;
        public List<BatchVoxel> Voxels = new List<BatchVoxel>(10000);
        private int v;

        public Batch(Volume voxelObject)
        {
            VoxelObject = voxelObject;
        }

        public Batch(Volume voxelObject, int initialSize) : this(voxelObject)
        {
            VoxelObject = voxelObject;
            Voxels = new List<BatchVoxel>(initialSize);
        }

        public void Add(Voxel voxel, int arrayX, int arrayY, int arrayZ, Vector3 worldPos)
        {
            Add(voxel, new PicaVoxelPoint(arrayX, arrayY, arrayZ), worldPos);
        }

        public void Add(Voxel voxel, PicaVoxelPoint arrayPoint, Vector3 worldPos)
        {
            Voxels.Add(new BatchVoxel(voxel, arrayPoint, worldPos));
        }

        public void Clear()
        {
            Voxels.Clear();
        }

        public void Dispose()
        {
            Voxels.Clear();
            Voxels = null;
            VoxelObject = null;
        }
    }
}