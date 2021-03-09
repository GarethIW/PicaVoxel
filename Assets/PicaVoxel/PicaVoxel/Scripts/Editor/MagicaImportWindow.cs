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
    public class MagicaImportWindow : EditorWindow
    {
        private bool centerPivot = false;

        private float voxelSize = 0.1f;

        private string objectName = "MagicaVoxel Import";

        public void Init()
        {
            titleContent = new GUIContent("MagicaVoxel Import");
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            objectName = EditorGUILayout.TextField("Volume name: ", objectName);
            EditorGUILayout.Space();
            voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);
            centerPivot = EditorGUILayout.ToggleLeft(" Center Pivot", centerPivot);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select .vox and Import"))
            {
                string fn = UnityEditor.EditorUtility.OpenFilePanel("Import VOX", "", "vox");
                if (!string.IsNullOrEmpty(fn))
                {
                    MagicaVoxelImporter.MagicaVoxelImport(fn, objectName, voxelSize, centerPivot);
                    Close();




                }
            }
            if (GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }

}