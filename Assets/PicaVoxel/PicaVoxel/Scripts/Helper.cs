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
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PicaVoxel
{
    public enum RotateAxis
    {
        X,
        Y,
        Z
    }

    public enum FillMode
    {
        AllVoxels,
        BaseOnly,
        None
    }

    public static class Helper
    {
        public static void CopyVoxelsInBox(ref Voxel[] source, ref Voxel[] dest, PicaVoxelPoint srcSize, PicaVoxelPoint destSize, bool activeOnly)
        {
            CopyVoxelsInBox(ref source, ref dest,
                new PicaVoxelBox(0, 0, 0, srcSize.X-1, srcSize.Y-1, srcSize.Z-1),
                new PicaVoxelBox(0, 0, 0, destSize.X - 1, destSize.Y - 1, destSize.Z - 1), srcSize, destSize,  activeOnly);
        }

        public static void CopyVoxelsInBox(ref Voxel[] source, ref Voxel[] dest,
            PicaVoxelBox sourceBox, PicaVoxelBox destBox, PicaVoxelPoint srcSize, PicaVoxelPoint destSize, bool activeOnly)
        {
            int dx = destBox.BottomLeftFront.X;
            int dy = destBox.BottomLeftFront.Y;
            int dz = destBox.BottomLeftFront.Z;
           
            for (int x = sourceBox.BottomLeftFront.X; x <= sourceBox.TopRightBack.X; x++)
            {
                dy = destBox.BottomLeftFront.Y;
                for (int y = sourceBox.BottomLeftFront.Y; y <= sourceBox.TopRightBack.Y; y++)
                {
                    dz = destBox.BottomLeftFront.Z;
                    for (int z = sourceBox.BottomLeftFront.Z; z <= sourceBox.TopRightBack.Z; z++)
                    {
                       
                            if (x >= 0 && y >= 0 && z >= 0 && x < srcSize.X && y < srcSize.Y && z < srcSize.Z)
                                if (source[x + srcSize.X * (y + srcSize.Y * z)].Active || !activeOnly)
                                    if (dx >= 0 && dy >= 0 && dz >= 0 && dx < destSize.X && dy < destSize.Y && dz < destSize.Z)
                                        dest[dx + destSize.X * (dy + destSize.Y * dz)] = source[x + srcSize.X * (y + srcSize.Y * z)];
                        dz++;
                    }
                    dy++;
                }
                dx++;
            }
        }

   
        public static void RotateVoxelArrayX(ref Voxel[] source, PicaVoxelPoint srcSize)
        {
            Voxel[] dest = new Voxel[srcSize.X * srcSize.Y * srcSize.Z];
            for (int x = 0; x < srcSize.X; x++)
                for (int y = 0; y < srcSize.Y; y++)
                    for (int z = 0; z < srcSize.Z; z++)
                        dest[x + srcSize.X * ((srcSize.Z-1-z) + srcSize.Z * y)] = source[x + srcSize.X * (y + srcSize.Y * z)];

            source = dest;
        }

        public static void RotateVoxelArrayY(ref Voxel[] source, PicaVoxelPoint srcSize)
        {
            Voxel[] dest = new Voxel[srcSize.Z * srcSize.Y * srcSize.X];
            for (int x = 0; x < srcSize.X; x++)
                for (int y = 0; y < srcSize.Y; y++)
                    for (int z = 0; z < srcSize.Z; z++)
                        dest[z + srcSize.Z * (y +srcSize.Y * (srcSize.X - 1 - x))] = source[x + srcSize.X * (y + srcSize.Y * z)];

            source = dest;
        }

        public static void RotateVoxelArrayZ(ref Voxel[] source, PicaVoxelPoint srcSize)
        {
            Voxel[] dest = new Voxel[srcSize.Y * srcSize.X * srcSize.Z];
            for (int x = 0; x < srcSize.X; x++)
                for (int y = 0; y < srcSize.Y; y++)
                    for (int z = 0; z < srcSize.Z; z++)
                        dest[y + srcSize.Y * ((srcSize.X- 1 - x) + srcSize.X * z)] = source[x + srcSize.X * (y + srcSize.Y * z)];

            source = dest;
        }

        #if UNITY_EDITOR
        public static string GetMeshStorePath()
        {
            var guids = AssetDatabase.FindAssets("PicaVoxelVolume", null);
            if(guids.Length>0)
            {
                var meshAssetPath = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0])));
                meshAssetPath = Path.Combine(meshAssetPath, "MeshStore");
                return meshAssetPath;
            }

            return "PicaVoxel/MeshStore";
        }

        public static string NormalizePath(string path)
        {
            return path.Replace('\\','/').TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
        }

#endif
    }

    [Serializable]
    public class PicaVoxelPoint : IEquatable<PicaVoxelPoint>
    {
        public int X;
        public int Y;
        public int Z;

        public PicaVoxelPoint(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public PicaVoxelPoint(Vector3 point)
        {
            X = (int) point.x;
            Y = (int) point.y;
            Z = (int) point.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        bool IEquatable<PicaVoxelPoint>.Equals(PicaVoxelPoint other)
        {
            return (other.X == X && other.Y == Y && other.Z == Z);
        }
    }

    [Serializable]
    public class PicaVoxelBox : IEquatable<PicaVoxelBox>
    {
        public PicaVoxelPoint BottomLeftFront;
        public PicaVoxelPoint TopRightBack;

        public PicaVoxelBox(PicaVoxelPoint corner1, PicaVoxelPoint corner2)
            : this(corner1.X, corner1.Y, corner1.Z, corner2.X, corner2.Y, corner2.Z)
        {
        }

        public PicaVoxelBox(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            BottomLeftFront = new PicaVoxelPoint(x2 < x1 ? x2 : x1, y2 < y1 ? y2 : y1, z2 < z1 ? z2 : z1);
            TopRightBack = new PicaVoxelPoint(x2 >= x1 ? x2 : x1, y2 >= y1 ? y2 : y1, z2 >= z1 ? z2 : z1);
        }

        public bool Equals(PicaVoxelBox other)
        {
            return (BottomLeftFront == other.BottomLeftFront && TopRightBack == other.TopRightBack);
        }
    }





}