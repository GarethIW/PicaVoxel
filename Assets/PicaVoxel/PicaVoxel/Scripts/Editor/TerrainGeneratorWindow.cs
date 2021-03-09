/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace PicaVoxel
{
    public class TerrainGeneratorWindow : EditorWindow
    {
        private float voxelSize = 1f;
        private int xSize = 128;
        private int zSize = 128;
        private int ySize = 64;
        private float smoothness = 0.5f;

        public void Init()
        {
            titleContent = new GUIContent("Terrain Generator");
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            xSize = EditorGUILayout.IntField("X Size: ", xSize);
            EditorGUILayout.Space();
            zSize = EditorGUILayout.IntField("Z Size: ", zSize);
            EditorGUILayout.Space();
            ySize = EditorGUILayout.IntField("Y Size (Depth): ", ySize);
            EditorGUILayout.Space();
            smoothness = EditorGUILayout.Slider("Smoothness: ", smoothness, 0f, 1f);
            EditorGUILayout.Space();
            voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);

            if (xSize <= 0) xSize = 1;
            if (ySize <= 0) ySize = 1;
            if (zSize <= 0) zSize = 1;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate"))
            {
                TerrainGenerator.Generate(xSize,ySize,zSize, smoothness, voxelSize);
                Close();
            }
            if (GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }

}