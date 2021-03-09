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
    public class QubicleImportWindow : EditorWindow
    {
        private float voxelSize = 0.1f;

        private string objectName = "Qubicle Import";

        public void Init()
        {
            titleContent = new GUIContent("Qubicle Import");
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            objectName = EditorGUILayout.TextField("Volume name: ", objectName);
            EditorGUILayout.Space();
            voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select .qb/.qbt and Import"))
            {
                QubicleImporter.QubicleImport(UnityEditor.EditorUtility.OpenFilePanelWithFilters("Import QB/QBT", "", new [] {"Qubicle","qb,qbt"}), objectName, voxelSize);

                Close();
            }
            if (GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }

}