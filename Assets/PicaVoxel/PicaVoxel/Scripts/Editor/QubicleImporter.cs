/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////
//using ICSharpCode.SharpZipLib.GZip;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicaVoxel
{
    public static class QubicleImporter
    {
        private static void FromQubicleQB(BinaryReader stream, GameObject root, float voxelSize)
        {
            //const int MAX_VOLUME_DIMENSION = 64;

            uint sizex=0;
            uint sizey=0;
            uint sizez=0;

//            uint version;
            uint colorFormat;
            uint zAxisOrientation;
            uint compressed;
//            uint visibilityMaskEncoded;
            uint numMatrices;

            uint i;
            uint j;
            uint x;
            uint y;
            uint z;

            int posX;
            int posY;
            int posZ;
            uint[,,] matrix;
            List<uint[,,]> matrixList = new List<uint[,,]>();
            uint index;
            uint data;
            uint count;
            const uint CODEFLAG = 2;
            const uint NEXTSLICEFLAG = 6;
 
           //version = stream.ReadUInt32();
            stream.ReadUInt32();
           colorFormat = stream.ReadUInt32();
           zAxisOrientation = stream.ReadUInt32();
           compressed = stream.ReadUInt32();
           //visibilityMaskEncoded = stream.ReadUInt32();
            stream.ReadUInt32();
           numMatrices = stream.ReadUInt32();

            string volumeName = root.name;
            string path = Helper.GetMeshStorePath();
            for (int d = root.transform.childCount - 1; d >= 0; d--)
            {
                Volume vol = root.transform.GetChild(d).GetComponent<Volume>();
                if (vol != null)
                {
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        DirectoryInfo[] dirs = di.GetDirectories();
                        for (int dir=0;dir<dirs.Length;dir++)
                        {
                            if (dirs[dir].Name.ToLower() == vol.AssetGuid.ToLower())
                            {
                                dirs[dir].Delete(true);
                                break;
                            }
                        }
                    }
                    GameObject.DestroyImmediate(root.transform.GetChild(d).gameObject);
                }
            }

            for (i = 0; i < numMatrices; i++) // for each matrix stored in file
            {
                 // read matrix name
                 byte nameLength = stream.ReadByte();
                 string name = new string(stream.ReadChars(nameLength));

                 // read matrix size 
                 sizex = stream.ReadUInt32();
                 sizey = stream.ReadUInt32();
                 sizez = stream.ReadUInt32();
   
                 // read matrix position (in this example the position is irrelevant)
                 posX = stream.ReadInt32();
                 posY = stream.ReadInt32();
                 posZ = stream.ReadInt32();
   
                 // create matrix and add to matrix list
                 matrix = new uint[sizex,sizey,sizez];
                 matrixList.Add(matrix);
   
                 if (compressed == 0) // if uncompressd
                 {
                   for(z = 0; z < sizez; z++)
                     for(y = 0; y < sizey; y++)
                         for (x = 0; x < sizex; x++)
                             matrix[x, y, z] = stream.ReadUInt32();
                 }
                 else // if compressed
                 { 
                   z = 0;

                   while (z < sizez) 
                   {
                
                     index = 0;
       
                     while (true) 
                     {
                       data = stream.ReadUInt32();
         
                       if (data == NEXTSLICEFLAG)
                         break;
                       else if (data == CODEFLAG) 
                       {
                         count = stream.ReadUInt32();
                         data = stream.ReadUInt32();
           
                         for(j = 0; j < count; j++) 
                         {
                           x = index % sizex ; // mod = modulo e.g. 12 mod 8 = 4
                           y = index / sizex ; // div = integer division e.g. 12 div 8 = 1
                           index++;
                           matrix[x ,y ,z] = data;
                         }
                       }
                       else 
                       {
                         x = index % sizex;
                         y = index / sizex ;
                         index++;
                         matrix[x,y ,z] = data;
                       }
                     }
                     z++;
                   }
                 }


                var newObject =
                    Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as
                        GameObject;
                if (newObject != null)
                {
                    newObject.name = name!=""?name:volumeName;
                    newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
                    newObject.GetComponent<Volume>().GenerateBasic(FillMode.None);
                    Volume voxelVolume = newObject.GetComponent<Volume>();

                    voxelVolume.XSize = Convert.ToInt32(sizex);
                    voxelVolume.YSize = Convert.ToInt32(sizey);
                    voxelVolume.ZSize = Convert.ToInt32(sizez);
                    voxelVolume.Frames[0].XSize = Convert.ToInt32(sizex);
                    voxelVolume.Frames[0].YSize = Convert.ToInt32(sizey);
                    voxelVolume.Frames[0].ZSize = Convert.ToInt32(sizez);
                    voxelVolume.Frames[0].Voxels = new Voxel[sizex * sizey * sizez];
                    for (int v = 0; v < voxelVolume.Frames[0].Voxels.Length; v++) voxelVolume.Frames[0].Voxels[v].Value = 128;
                    voxelVolume.VoxelSize = voxelSize;

                    if (zAxisOrientation == 0)
                    {
                        voxelVolume.Pivot = new Vector3(sizex, 0, 0)*voxelSize;
                        voxelVolume.UpdatePivot();
                    }

                    for (z = 0; z < sizez; z++)
                        for (y = 0; y < sizey; y++)
                            for (x = 0; x < sizex; x++)
                            {

                                Color col = UIntToColor(matrix[x, y, z], colorFormat);

                                if (matrix[x, y, z] != 0)
                                    voxelVolume.Frames[0].Voxels[(zAxisOrientation == 0 ? sizex - 1 - x : x) + sizex * (y + sizey * z)] = new Voxel()
                                    {
                                        State = VoxelState.Active,
                                        Color = col,
                                        Value = 128
                                    };
                            }


                    voxelVolume.CreateChunks();
                    voxelVolume.SaveForSerialize();

                    newObject.transform.position = (new Vector3((zAxisOrientation == 0 ? -posX : posX), posY, posZ) * voxelSize);
                    newObject.transform.parent = root.transform;
                }
           // }
            }


            
        }

        private static void FromQubicleQBT(BinaryReader stream, GameObject root, float voxelSize)
        {
            uint i;
            Color32[] palette = null;

            // Header stuff
            var magic = stream.ReadInt32();
            stream.ReadByte();
            stream.ReadByte();

            if (magic != 0x32204251) return;

            // Global scale, PicaVoxel does not support scaled volumes - use voxel size instead.
            stream.ReadSingle();
            stream.ReadSingle();
            stream.ReadSingle();

            // Colormap header
            stream.ReadChars(8);
            var colorCount = stream.ReadUInt32();
            if (colorCount > 0)
            {
                palette = new Color32[colorCount];
                for (i = 0; i < colorCount; i++)
                {
                    var r = stream.ReadByte();
                    var g = stream.ReadByte();
                    var b = stream.ReadByte();
                    var a = stream.ReadByte();
                    palette[i] = new Color32(r, g, b, a);
                }

            }

            // Datatree header
            stream.ReadChars(8);
            LoadNode(stream, root, voxelSize, true, palette);
        }

        public static void LoadNode(BinaryReader stream, GameObject root, float voxelSize, bool first, Color32[] palette)
        {
            

            var type = stream.ReadUInt32();
            var size = stream.ReadUInt32();

            switch(type)
            {
                case 0:
                    LoadMatrix(stream, root, voxelSize, first, palette);
                    break;
                case 1:
                    LoadModel(stream, root, voxelSize, first, palette);
                    break;
                case 2:
                    stream.ReadBytes((int)size);
                    break;
                default:
                    stream.ReadBytes((int)size);
                    break;
            }
        }

        public static void LoadModel(BinaryReader stream, GameObject root, float voxelSize, bool first, Color32[] palette)
        {
            // Remove old volumes and stored meshes if re-import
            string path = Helper.GetMeshStorePath();
            for (int d = root.transform.childCount - 1; d >= 0; d--)
            {
                Volume vol = root.transform.GetChild(d).GetComponent<Volume>();
                if (vol != null)
                {
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        DirectoryInfo[] dirs = di.GetDirectories();
                        for (int dir = 0; dir < dirs.Length; dir++)
                        {
                            if (dirs[dir].Name.ToLower() == vol.AssetGuid.ToLower())
                            {
                                dirs[dir].Delete(true);
                                break;
                            }
                        }
                    }
                    GameObject.DestroyImmediate(root.transform.GetChild(d).gameObject);
                }
            }

            if (!first)
            {
                GameObject child = null;

                // use existing gameobject if we're re-importing
                child = root.transform.Find(root.name).gameObject;
                if (child == null)
                {
                    child = new GameObject();
                    child.transform.SetParent(root.transform);
                    child.name = root.name;
                }
                child.transform.localPosition = Vector3.zero;

                var childcount = stream.ReadUInt32();
                for (var i = 0; i < childcount; i++)
                {
                    LoadNode(stream, child, voxelSize, false, palette);
                }
            }
            else
            {
                first = false;

                var childcount = stream.ReadUInt32();
                for (var i = 0; i < childcount; i++)
                {
                    LoadNode(stream, root, voxelSize, false, palette);
                }
            }

            
        }

        public static void LoadMatrix(BinaryReader stream, GameObject root, float voxelSize, bool first, Color32[] palette)
        {
            var nameLength = stream.ReadInt32();
            var name = new string(stream.ReadChars(nameLength));

            var px = stream.ReadInt32();
            var py = stream.ReadInt32();
            var pz = stream.ReadInt32();

            px = -px;

            // PicaVoxel doesn't support scaling of volumes, sorry!
            stream.ReadInt32();
            stream.ReadInt32();
            stream.ReadInt32();

            var pivx = stream.ReadSingle();
            var pivy = stream.ReadSingle();
            var pivz = stream.ReadSingle();

            var sizeX = (int)stream.ReadUInt32();
            var sizeY = (int)stream.ReadUInt32();
            var sizeZ = (int)stream.ReadUInt32();

            pivx = sizeX - pivx;

            var child = Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            child.transform.SetParent(root.transform);
            child.name = name;
            child.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
            child.GetComponent<Volume>().GenerateBasic(FillMode.None);
            Volume voxelVolume = child.GetComponent<Volume>();

            voxelVolume.XSize = Convert.ToInt32(sizeX);
            voxelVolume.YSize = Convert.ToInt32(sizeY);
            voxelVolume.ZSize = Convert.ToInt32(sizeZ);
            voxelVolume.Frames[0].XSize = Convert.ToInt32(sizeX);
            voxelVolume.Frames[0].YSize = Convert.ToInt32(sizeY);
            voxelVolume.Frames[0].ZSize = Convert.ToInt32(sizeZ);
            voxelVolume.Frames[0].Voxels = new Voxel[sizeX * sizeY * sizeZ];
            voxelVolume.VoxelSize = voxelSize;

            var dataSize = (int)stream.ReadUInt32();
            var compressedData = stream.ReadBytes(dataSize);

            using (var ms = new MemoryStream(compressedData))
            {
                using (var gzs = new ZlibStream(ms, CompressionMode.Decompress, true))
                {
                    for (var x = 0; x < sizeX; x++)
                        for (var z = 0; z < sizeZ; z++)
                            for (var y = 0; y < sizeY; y++)
                            {
                                var r = (byte)gzs.ReadByte();
                                var g = (byte)gzs.ReadByte();
                                var b = (byte)gzs.ReadByte();
                                var m = (byte)gzs.ReadByte();
                                if (m > 0)
                                    voxelVolume.Frames[0].Voxels[(sizeX - 1 - x) + sizeX * (y + sizeY * z)] = new Voxel() { Color = palette==null?new Color32(r, g, b, 255):palette[r], Value = 128, State = VoxelState.Active };
                            }
                }
            }

            voxelVolume.CreateChunks();
            voxelVolume.SaveForSerialize();

            var pivot = new Vector3(pivx, pivy, pivz) * voxelSize;
            voxelVolume.Pivot = pivot;
            voxelVolume.UpdatePivot();

            voxelVolume.transform.localPosition = (new Vector3(-sizeX+px, py, pz) * voxelSize) + pivot;
        }

        public static void LoadCompound(BinaryReader stream, GameObject root, float voxelSize, bool first, Color32[] palette)
        {
            var nameLength = stream.ReadInt32();
            var name = new string(stream.ReadChars(nameLength));

            var px = stream.ReadInt32();
            var py = stream.ReadInt32();
            var pz = stream.ReadInt32();

            px = -px;

            // PicaVoxel doesn't support scaling of volumes, sorry!
            stream.ReadInt32();
            stream.ReadInt32();
            stream.ReadInt32();

            var pivx = stream.ReadSingle();
            var pivy = stream.ReadSingle();
            var pivz = stream.ReadSingle();

            var sizeX = (int)stream.ReadUInt32();
            var sizeY = (int)stream.ReadUInt32();
            var sizeZ = (int)stream.ReadUInt32();

            pivx = sizeX - pivx;

            var child = Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            child.transform.SetParent(root.transform);
            child.name = name;
            child.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
            child.GetComponent<Volume>().GenerateBasic(FillMode.None);
            Volume voxelVolume = child.GetComponent<Volume>();

            voxelVolume.XSize = Convert.ToInt32(sizeX);
            voxelVolume.YSize = Convert.ToInt32(sizeY);
            voxelVolume.ZSize = Convert.ToInt32(sizeZ);
            voxelVolume.Frames[0].XSize = Convert.ToInt32(sizeX);
            voxelVolume.Frames[0].YSize = Convert.ToInt32(sizeY);
            voxelVolume.Frames[0].ZSize = Convert.ToInt32(sizeZ);
            voxelVolume.Frames[0].Voxels = new Voxel[sizeX * sizeY * sizeZ];
            voxelVolume.VoxelSize = voxelSize;

            var dataSize = (int)stream.ReadUInt32();
            var compressedData = stream.ReadBytes(dataSize);

            using (var ms = new MemoryStream(compressedData))
            {
                using (var gzs = new ZlibStream(ms, CompressionMode.Decompress, true))
                {
                    for (var x = 0; x < sizeX; x++)
                    for (var z = 0; z < sizeZ; z++)
                    for (var y = 0; y < sizeY; y++)
                    {
                        var r = (byte)gzs.ReadByte();
                        var g = (byte)gzs.ReadByte();
                        var b = (byte)gzs.ReadByte();
                        var m = (byte)gzs.ReadByte();
                        if (m > 0)
                            voxelVolume.Frames[0].Voxels[(sizeX - 1 - x) + sizeX * (y + sizeY * z)] = new Voxel() { Color = palette == null ? new Color32(r, g, b, 255) : palette[r], Value = 128, State = VoxelState.Active };
                    }
                }
            }

            voxelVolume.CreateChunks();
            voxelVolume.SaveForSerialize();

            var pivot = new Vector3(pivx, pivy, pivz) * voxelSize;
            voxelVolume.Pivot = pivot;
            voxelVolume.UpdatePivot();

            voxelVolume.transform.localPosition = (new Vector3(-sizeX + px, py, pz) * voxelSize) + pivot;

            var childCount = stream.ReadUInt32();
            for (var i = 0; i < childCount; i++)
            {
                LoadNode(stream, child, voxelSize, first, palette);
            }
        }

        public static void QubicleImport(string fn, string volumeName, float voxelSize)
        {
            var newObject = new GameObject();

            var extension = Path.GetExtension(fn);

            newObject.name = (volumeName != "Qubicle Import" ? volumeName : Path.GetFileNameWithoutExtension(fn));
            newObject.AddComponent<QubicleImport>();
            newObject.GetComponent<QubicleImport>().ImportedFile = fn;
            newObject.GetComponent<QubicleImport>().ImportedVoxelSize = voxelSize;

            using (BinaryReader stream = new BinaryReader(new FileStream(fn, FileMode.Open)))
            {
                if(extension.ToLower()==".qb")
                    FromQubicleQB(stream, newObject, voxelSize);

                if(extension.ToLower()==".qbt")
                    FromQubicleQBT(stream, newObject, voxelSize);

            }
        }
        public static void QubicleImport(QubicleImport existingImport)
        {
            var extension = Path.GetExtension(existingImport.ImportedFile);

            using (BinaryReader stream = new BinaryReader(new FileStream(existingImport.ImportedFile, FileMode.Open)))
            {

               if(extension.ToLower() == ".qb")
                    FromQubicleQB(stream, existingImport.gameObject, existingImport.ImportedVoxelSize);

                if (extension.ToLower() == ".qbt")
                    FromQubicleQBT(stream, existingImport.gameObject, existingImport.ImportedVoxelSize);
            }
        }

        private static Color32 UIntToColor(uint color, uint format)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            if (format == 0)
            {
                r = (byte)(color >> 0);
                g = (byte)(color >> 8);
                b = (byte)(color >> 16);
            }
            else
            {
                r = (byte)(color >> 16);
                g = (byte)(color >> 8);
                b = (byte)(color >> 0);
            }

            return new Color32(r, g, b, 255);
        }

    }
}
