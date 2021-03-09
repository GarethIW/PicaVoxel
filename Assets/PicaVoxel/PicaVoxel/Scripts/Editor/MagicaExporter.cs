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
    public static class MagicaExporter
    {
        private struct MagicaVoxelData
        {
            public byte x;
            public byte y;
            public byte z;
            public byte color;
        }

        private static void ToMagica(BinaryWriter stream, Volume vol)
        {
            var pal = new List<Color32>();
            pal.Add(new Color32());

            var size = 0;

            List<MagicaVoxelData> voxelData = null;
            List<MagicaVoxelData[]> frames = new List<MagicaVoxelData[]>();
            for (var f = 0; f < vol.Frames.Count; f++)
            {
                voxelData = new List<MagicaVoxelData>();
                for (int x = 0; x < vol.XSize; x++)
                    for (int y = 0; y < vol.YSize; y++)
                        for (int z = 0; z < vol.ZSize; z++)
                        {
                            var v = vol.Frames[f].Voxels[x + vol.XSize * (y + vol.YSize * z)];
                            var ci = 0;
                            if (pal.Contains(v.Color)) ci = pal.IndexOf(v.Color);
                            else { pal.Add(v.Color); ci = pal.Count - 1; }
                            if (v.Active) voxelData.Add(new MagicaVoxelData() { x = (byte)x, y = (byte)z, z = (byte)y, color = (byte)(ci + 1) });
                        }

                size += (voxelData.Count * 4) + 16;

                frames.Add(voxelData.ToArray());
            }

           

            stream.Write(UTF8Encoding.UTF8.GetBytes("VOX "));
            stream.Write((Int32)150);

            stream.Write(UTF8Encoding.UTF8.GetBytes("MAIN"));
            stream.Write((Int32)0);
            stream.Write((Int32)(size+1024+(frames.Count*24)+12+16));

            stream.Write(UTF8Encoding.UTF8.GetBytes("PACK"));
            stream.Write((Int32)4);
            stream.Write((Int32)0);
            stream.Write((Int32)frames.Count);

            

            

            foreach (var fr in frames)
            {
                stream.Write(UTF8Encoding.UTF8.GetBytes("SIZE"));
                stream.Write((Int32)4 * 3);
                stream.Write((Int32)0);
                stream.Write((Int32)vol.XSize);
                stream.Write((Int32)vol.ZSize);
                stream.Write((Int32)vol.YSize);

                stream.Write(UTF8Encoding.UTF8.GetBytes("XYZI"));
                stream.Write((Int32)(fr.Length * 4) + 4);
                stream.Write((Int32)0);
                stream.Write((Int32)fr.Length);
                foreach (var d in fr)
                {
                    stream.Write(d.x);
                    stream.Write(d.y);
                    stream.Write(d.z);
                    stream.Write(d.color);
                }
            }

            stream.Write(UTF8Encoding.UTF8.GetBytes("RGBA"));
            stream.Write((Int32)256 * 4); // chunk length
            stream.Write((Int32)0); // child chunks
            for (var i = 0; i < 256; i++)
            {
                var c = new Color32();
                if (i < pal.Count)
                    c = pal[i];
                stream.Write(c.r);
                stream.Write(c.g);
                stream.Write(c.b);
                stream.Write(c.a);
            }

            

            stream.Flush();

        }

        public static void MagicaExport(Volume vol)
        {
            var fn = UnityEditor.EditorUtility.SaveFilePanel("Export VOX", "", "", "vox");
            if (string.IsNullOrEmpty(fn)) return;

            using (BinaryWriter stream = new BinaryWriter(new FileStream(fn, FileMode.Create)))
            {
                ToMagica(stream, vol);
            }
        }

      
    }
}
