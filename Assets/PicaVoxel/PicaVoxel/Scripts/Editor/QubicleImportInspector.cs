/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;

namespace PicaVoxel
{
    [CustomEditor(typeof (QubicleImport))]
    public class QubicleImportInspector : Editor
    {
        private QubicleImport qubicleImport;

        private void OnEnable()
        {
            qubicleImport = (QubicleImport) target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (!string.IsNullOrEmpty(qubicleImport.ImportedFile) && serializedObject.targetObjects.Length < 2)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Re-import from Qubicle", "Re-import from original .QB/.QBT file")))
                {
                    if (UnityEditor.EditorUtility.DisplayDialog("Warning!",
                        "Re-importing will overwrite any changes made since original import, including hierarchy additions and components. This cannot be undone!",
                        "OK", "Cancel"))
                    {
                        
                        QubicleImporter.QubicleImport(qubicleImport);
                    }
                }
            }

        }
    }
}