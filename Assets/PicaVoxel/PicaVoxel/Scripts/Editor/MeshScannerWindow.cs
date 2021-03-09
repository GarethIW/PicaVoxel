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
    public class MeshScannerWindow : EditorWindow
    {
        private float voxelSize = 0.1f;

        private string objectName = "";
        private GameObject meshObject;

        public void Init(GameObject mo)
        {
            titleContent = new GUIContent("Mesh Scanner");

            objectName = mo.name + "(Voxels)";
            meshObject = mo;
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            objectName = EditorGUILayout.TextField("Volume name: ", objectName);
            EditorGUILayout.Space();
            voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);
           
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Mesh"))
            {
                MeshScanner.Scan(meshObject, voxelSize);
                Close();
            }
            if (GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }

}