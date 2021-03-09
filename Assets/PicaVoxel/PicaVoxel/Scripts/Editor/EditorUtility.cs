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
using Object = UnityEngine.Object;

namespace PicaVoxel
{
    public enum AnchorX
    {
        Left,
        Center,
        Right
    }

    public enum AnchorY
    {
        Bottom,
        Center,
        Top
    }

    public enum AnchorZ
    {
        Front,
        Center,
        Back
    }

    [InitializeOnLoad]
    public static class EditorUtility
    {
        public static Object VoxelVolumePrefab;
        public static Object ChunkPrefab;
        public static Object PicaVoxelParticleSystemPrefab;
        public static Material PicaVoxelDiffuseMaterial;

        public static Dictionary<string, Texture2D> Buttons = new Dictionary<string, Texture2D>();

        private static bool assetsLoaded = false;
        private static int assetLoadRetries = 0;

        static EditorUtility()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!assetsLoaded) LoadAssets();
        }

        private static void LoadAssets()
        {
            assetLoadRetries++;
            assetsLoaded = true;

            // Add our volume tag if not present
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals("PicaVoxelVolume")) { found = true; break; }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
                n.stringValue = "PicaVoxelVolume";
            }
            tagManager.ApplyModifiedProperties();

            var guids = AssetDatabase.FindAssets("PicaVoxel", null);
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof (GameObject));
                if (asset)
                {
                    if (asset.name == "PicaVoxelVolume") VoxelVolumePrefab = asset;
                    if (asset.name == "PicaVoxelChunk") ChunkPrefab = asset;
                    if (asset.name == "PicaVoxelParticleSystem") PicaVoxelParticleSystemPrefab = asset;

                }

                Material material =
                    (Material) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof (Material));
                if (material && material.name == "PicaVoxel PBR") PicaVoxelDiffuseMaterial = material;

            }

            if (VoxelVolumePrefab == null || ChunkPrefab == null || PicaVoxelParticleSystemPrefab == null)
                assetsLoaded = false;

            if (!assetsLoaded && assetLoadRetries == 3)
            {
                assetsLoaded = true;
                Debug.LogError("PicaVoxel: Unable to find and load one or more PicaVoxel prefabs!");
            }


            guids = AssetDatabase.FindAssets("pvButton", null);
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof (Texture));
                if (!Buttons.ContainsKey(asset.name)) Buttons.Add(asset.name, (Texture2D) asset);
            }
        }

        public static void SkinnedLabel(string text)
        {
            EditorGUILayout.LabelField(text, new GUIStyle() {  fontStyle = FontStyle.Bold, normal = new GUIStyleState() {textColor = (EditorGUIUtility.isProSkin ? new Color(0.7f,0.7f,0.7f) : Color.black) } });
        }
    }

    

}