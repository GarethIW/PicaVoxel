using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PerVoxelColliders))]
class PerVoxelCollidersInspector : Editor
{
    private PerVoxelColliders pvc;

    private void OnEnable()
    {
        pvc = (PerVoxelColliders)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate colliders"))
        {

            pvc.GenerateColliders();

        }


    }
}
