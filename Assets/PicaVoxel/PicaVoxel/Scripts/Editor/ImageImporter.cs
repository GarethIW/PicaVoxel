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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicaVoxel
{
    public static class ImageImporter
    {
       
        public static void FromImage(Texture2D image, GameObject root, float voxelSize, int depth, bool centerPivot, Color cutoutColor)
        {
          
            if (root != null && image!=null && image.width>0 && image.height>0 && depth>0)
            {
                        
                Volume voxelVolume = root.GetComponent<Volume>();

                voxelVolume.XSize = image.width;
                voxelVolume.YSize = image.height;
                voxelVolume.ZSize = depth;
                voxelVolume.Frames[0].XSize = voxelVolume.XSize;
                voxelVolume.Frames[0].YSize = voxelVolume.YSize;
                voxelVolume.Frames[0].ZSize = voxelVolume.ZSize;
                voxelVolume.Frames[0].Voxels = new Voxel[voxelVolume.XSize * voxelVolume.YSize * voxelVolume.ZSize];
                voxelVolume.VoxelSize = voxelSize;

                if (centerPivot)
                {
                    voxelVolume.Pivot = (new Vector3(voxelVolume.XSize, voxelVolume.YSize, voxelVolume.ZSize) * voxelVolume.VoxelSize) / 2f;
                    voxelVolume.UpdatePivot();
                }

                for(int x=0;x<image.width;x++)
                    for(int y=0;y<image.height;y++)
                    {
                        Color col = image.GetPixel(x, y);
                        for(int z=0;z<depth;z++)
                        {
                            voxelVolume.Frames[0].Voxels[x + voxelVolume.XSize * (y + voxelVolume.YSize * z)] = new Voxel()
                            {
                                State = (col!=cutoutColor && col.a>0f)?VoxelState.Active : VoxelState.Inactive,
                                Color = col,
                                Value = 128
                            };
                        }
                    }

                voxelVolume.CreateChunks();
                voxelVolume.SaveForSerialize();
            }
        }

        public static void ImageImport(string fn, string volumeName, float voxelSize, int depth, bool centerPivot, Color cutoutColor)
        {
            var newObject = Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;

            newObject.name = (volumeName != "Image Import" ? volumeName : Path.GetFileNameWithoutExtension(fn));
            newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
            newObject.GetComponent<Volume>().GenerateBasic(FillMode.None);

            byte[] data = File.ReadAllBytes(fn);
            Texture2D tex = new Texture2D(2,2);
            tex.LoadImage(data);

            FromImage(tex, newObject, voxelSize, depth, centerPivot, cutoutColor);

            newObject.GetComponent<Volume>().ImportedFile = fn;
            newObject.GetComponent<Volume>().ImportedFrom = Importer.Image;
            newObject.GetComponent<Volume>().ImportedCutoutColor = cutoutColor;
        }

        public static void ImageImport(Volume originalVolume)
        {
            byte[] data = File.ReadAllBytes(originalVolume.ImportedFile);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);

            FromImage(tex, originalVolume.gameObject, originalVolume.VoxelSize, originalVolume.ZSize, false, originalVolume.ImportedCutoutColor);
        }
     

    }
}
