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
    public class ImageImportWindow : EditorWindow
    {
        private bool centerPivot = false;

        private float voxelSize = 0.1f;
        private int depth = 1;
        private Color cutoutColor = Color.magenta;

        private string objectName = "Image Import";

        public void Init()
        {
            titleContent = new GUIContent("Image Import");
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            objectName = EditorGUILayout.TextField("Volume name: ", objectName);
            EditorGUILayout.Space();
            voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);
            depth = EditorGUILayout.IntField("Volume depth: ", depth);
            centerPivot = EditorGUILayout.ToggleLeft(" Center pivot", centerPivot);
            cutoutColor = EditorGUILayout.ColorField("Cutout color", cutoutColor);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Image and Import"))
            {
                string fn = UnityEditor.EditorUtility.OpenFilePanel("Import Image", "", ";*.png;*.jpg;*.jpeg");
                if (!string.IsNullOrEmpty(fn))
                {
                    ImageImporter.ImageImport(fn, objectName, voxelSize, depth, centerPivot, cutoutColor);

                    Close();
                }
            }
            if (GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }

}