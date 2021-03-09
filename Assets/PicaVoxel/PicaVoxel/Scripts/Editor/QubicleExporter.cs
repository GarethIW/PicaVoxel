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
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicaVoxel
{
    public static class QubicleExporter
    {
        private static void ToQubicle(BinaryWriter stream, Volume vol)
        {

            stream.Write(new byte[] {1,1,0,0}); // version
            stream.Write(Convert.ToUInt32(0)); // colorFormat
            stream.Write(Convert.ToUInt32(1)); // zAxisOrientation
            stream.Write(Convert.ToUInt32(0)); // compressed
            stream.Write(Convert.ToUInt32(0)); // vis mask
            stream.Write(Convert.ToUInt32(1)); // num matrices

            var name = "";
            if (vol.gameObject.name.Length > 255)
                name = vol.gameObject.name.Substring(0, 255);
            else
                name = vol.gameObject.name;
            stream.Write((byte)name.Length); // write name length
            stream.Write(UTF8Encoding.UTF8.GetBytes(name));

            stream.Write(Convert.ToUInt32(vol.XSize)); // matrix size
            stream.Write(Convert.ToUInt32(vol.YSize));
            stream.Write(Convert.ToUInt32(vol.ZSize));

            stream.Write(Convert.ToInt32(0)); // matrix position (as we're only exporting one matrix, we'll leave it at 0,0,0
            stream.Write(Convert.ToInt32(0));
            stream.Write(Convert.ToInt32(0));

            // write uncompressed data
            for (var z = 0; z < vol.ZSize; z++)
                for(var y = 0; y < vol.YSize; y++)
                    for (var x = 0; x < vol.XSize; x++)
                    {
                        var v = vol.GetVoxelAtArrayPosition(x, y, z);
                        if (v.HasValue)
                        {
                            if(v.Value.Active)
                                stream.Write(ColorToUInt(v.Value.Color));
                            else
                                stream.Write(Convert.ToUInt32(0));
                        }
                        else stream.Write(Convert.ToUInt32(0));
                    }

            stream.Flush();

        }

        public static void QubicleExport(Volume vol)
        {
            var fn = UnityEditor.EditorUtility.SaveFilePanel("Export QB", "", "", "qb");
            if (string.IsNullOrEmpty(fn)) return;

            using (BinaryWriter stream = new BinaryWriter(new FileStream(fn, FileMode.Create)))
            {
                ToQubicle(stream, vol);
            }
        }

        private static uint ColorToUInt(Color32 col)
        {
            uint color = (uint) (col.r << 0 | col.g << 8 | col.b << 16 | 255 << 24);

            return color;
        }

    }
}
