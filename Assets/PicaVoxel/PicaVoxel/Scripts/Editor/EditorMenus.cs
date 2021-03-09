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
    public static class EditorMenus
    {
        public const string MENU_MAIN_ROOT = "PicaVoxel";
        public const string MENU_GAMEOBJECT_ROOT = "GameObject/PicaVoxel/";

        [MenuItem(MENU_GAMEOBJECT_ROOT + "PicaVoxel Particle System", false, 13)]
        private static void DoCreateManager()
        {
            if (GameObject.FindObjectOfType<VoxelParticleSystem>() != null)
            {
                UnityEditor.EditorUtility.DisplayDialog("PicaVoxel",
                    "Only one PicaVoxel Particle System can be added to a Scene.", "OK");
                return;
            }

            var psObject =
                Editor.Instantiate(EditorUtility.PicaVoxelParticleSystemPrefab, Vector3.zero, Quaternion.identity) as
                    GameObject;
            if (psObject != null)
            {
                psObject.name = "PicaVoxel Particle System";
                Selection.activeObject = psObject;
                Undo.RegisterCreatedObjectUndo(psObject, "Create PicaVoxel Particle System");
            }
            else Debug.LogError("PicaVoxel: Could not instantiate PicaVoxel Particle System!");
        }

        [MenuItem(MENU_GAMEOBJECT_ROOT + "PicaVoxel Volume", false, 10)]
        private static void DoCreateObject()
        {
            var newObject =
                Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            if (newObject != null)
            {
                newObject.name = "PicaVoxel Volume";
                newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;

                newObject.GetComponent<Volume>().XSize = 32;
                newObject.GetComponent<Volume>().YSize = 32;
                newObject.GetComponent<Volume>().ZSize = 32;

                newObject.GetComponent<Volume>().GenerateBasic(FillMode.AllVoxels);
                Selection.activeObject = newObject;
                Undo.RegisterCreatedObjectUndo(newObject, "Create PicaVoxel Volume");
            }
            else Debug.LogError("PicaVoxel: Could not instantiate PicaVoxel Volume!");
        }

        [MenuItem(MENU_GAMEOBJECT_ROOT + "PicaVoxel Volume (base only)", false, 11)]
        private static void DoCreateObjectBase()
        {
            var newObject =
                Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            if (newObject != null)
            {
                newObject.name = "PicaVoxel Volume";
                newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
                newObject.GetComponent<Volume>().GenerateBasic(FillMode.BaseOnly);
                Selection.activeObject = newObject;
                Undo.RegisterCreatedObjectUndo(newObject, "Create PicaVoxel Volume");
            }
            else Debug.LogError("PicaVoxel: Could not instantiate PicaVoxel Volume!");
        }

        // Multi-part volumes are deprecated, but if you *really* need them they're here i guess
        //[MenuItem(MENU_GAMEOBJECT_ROOT + "PicaVoxel Multi-part Volume", false, 12)]
        //private static void DoCreateMultiPart()
        //{
        //    MultiPartWindow window = (MultiPartWindow)EditorWindow.GetWindowWithRect((typeof(MultiPartWindow)), new Rect(100,100,400,280), true);
        //    window.Init();
        //}

        [MenuItem(MENU_GAMEOBJECT_ROOT + "Import MagicaVoxel .vox", false, 14)]
        private static void DoImportMagica()
        {
            MagicaImportWindow window = (MagicaImportWindow)EditorWindow.GetWindowWithRect((typeof(MagicaImportWindow)), new Rect(100, 100, 400, 100), true);
            window.Init();
            
        }

        [MenuItem(MENU_GAMEOBJECT_ROOT + "Import Qubicle .qb or .qbt", false, 15)]
        private static void DoImportQubicle()
        {
            QubicleImportWindow window = (QubicleImportWindow)EditorWindow.GetWindowWithRect((typeof(QubicleImportWindow)), new Rect(100, 100, 400, 100), true);
            window.Init();

        }

        [MenuItem(MENU_GAMEOBJECT_ROOT + "Import from Image", false, 16)]
        private static void DoImportImage()
        {
            ImageImportWindow window = (ImageImportWindow)EditorWindow.GetWindowWithRect((typeof(ImageImportWindow)), new Rect(100, 100, 400, 140), true);
            window.Init();
        }

        [MenuItem(MENU_GAMEOBJECT_ROOT + "Scan Mesh", false, 17)]
        private static void DoScanMesh()
        {
            if (Selection.activeGameObject != null)
            {
                GameObject meshObject = Selection.activeGameObject;
                if ((meshObject.GetComponent<MeshFilter>() != null && meshObject.GetComponent<MeshRenderer>() != null) ||
                    meshObject.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    MeshScannerWindow window = (MeshScannerWindow)EditorWindow.GetWindowWithRect((typeof(MeshScannerWindow)), new Rect(100, 100, 400, 90), true);
                    window.Init(meshObject);
                }
                else
                    UnityEditor.EditorUtility.DisplayDialog("Scan Mesh",
                    "Please select an object that has EITHER a Mesh Filter + Mesh Renderer OR a Skinned Mesh Renderer!",
                    "OK");
            }
            else
                UnityEditor.EditorUtility.DisplayDialog("Scan Mesh",
                    "Please select an object that has EITHER a Mesh Filter + Mesh Renderer OR a Skinned Mesh Renderer!",
                    "OK");

            //ImageImportWindow window = (ImageImportWindow)EditorWindow.GetWindowWithRect((typeof(ImageImportWindow)), new Rect(100, 100, 300, 140), true);
            //window.Init();
        }

        [MenuItem(MENU_GAMEOBJECT_ROOT + "Generate Terrain", false, 18)]
        private static void DoTerrainGen()
        {

            TerrainGeneratorWindow window = (TerrainGeneratorWindow)EditorWindow.GetWindowWithRect((typeof(TerrainGeneratorWindow)), new Rect(100, 100, 400, 160), true);
            window.Init();
        }

        // Taking out mesh storeage cleaning because it only works for current scene, not all scenes in project.
        //[MenuItem(MENU_GAMEOBJECT_ROOT + "Clean up Mesh Storage", false, 16)]
        //private static void CleanUpMeshStorage()
        //{
        //    string path =Helper.GetMeshStorePath();
        //    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        //    DirectoryInfo di = new DirectoryInfo(path);
        //    foreach (DirectoryInfo d in di.GetDirectories())
        //    {
        //        //int id = Convert.ToInt32(d.Name);
        //        bool found = false;

        //        object[] obj = Resources.FindObjectsOfTypeAll<Chunk>();
        //        foreach (object o in obj)
        //        {
        //            if (((Chunk) o).GetComponent<MeshFilter>().sharedMesh == null) continue;
        //            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(((Chunk) o).GetComponent<MeshFilter>().sharedMesh))) continue;

        //            string assPath = AssetDatabase.GetAssetPath(((Chunk) o).GetComponent<MeshFilter>().sharedMesh);
        //            string testPath = Helper.NormalizePath(Path.GetDirectoryName(assPath));
        //            string thisPath = Helper.NormalizePath(Path.Combine(path, d.Name));
        //            if (testPath == thisPath)
        //            {
        //                found = true;
        //                break;
        //            }
        //        }

        //        if(!found)
        //            d.Delete(true);
        //    }
        //}



//        [MenuItem(MENU_GAMEOBJECT_ROOT + "Import VXS (TEMPORARY)", false, 14)]
//        private static void DoImportVXS()
//        {
//            var newObject =
//                Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
//            if (newObject != null)
//            {
//                newObject.name = "Imported VXS";
//                newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
//                newObject.GetComponent<Volume>().GenerateBasic(FillMode.AllVoxels);
//                Volume voxelVolume = newObject.GetComponent<Volume>();
//
//                VXSImporter.LoadSprite(UnityEditor.EditorUtility.OpenFilePanel("Import VXS", "", "vxs"), voxelVolume);
//
//
//
//            }
//            else Debug.LogError("PicaVoxel: Could not instantiate PicaVoxel Volume!");
//        }

        //[MenuItem(MENU_GAMEOBJECT_ROOT + "Convert 5-byte to 6-byte voxels (TEMPORARY)", false, 14)]
        //private static void ConvertBytes()
        //{
        //    int volcount = 0;
        //    int fcount = 0;

        //    var vols = GameObject.FindObjectsOfType<Volume>();

        //    foreach (var v in vols)
        //    {
        //        if (!v.RuntimOnlyMesh) continue;

        //        v.RuntimOnlyMesh = false;
        //        v.CreateChunks();
        //        v.UpdateAllChunks();

        //        volcount++;
        //        foreach (var f in v.Frames)
        //        {
        //            for (int i = 0; i < f.Voxels.Length; i++)
        //                f.Voxels[i] = new Voxel()
        //                {
        //                    Color = new Color32(f.Voxels[i].Color.r, f.Voxels[i].Color.g, f.Voxels[i].Color.b, 255),
        //                    State = f.Voxels[i].State,
        //                    Value = f.Voxels[i].Value
        //                };
        //            f.SaveForSerialize();
        //            fcount++;
        //        }

        //        v.RuntimOnlyMesh = true;
        //        v.CreateChunks();
        //        v.UpdateAllChunks();

        //        foreach (var f in v.Frames)
        //            f.SaveForSerialize();
        //    }

        //    Debug.Log(volcount + " volumes, " + fcount + " frames converted");
        //}
    }

}