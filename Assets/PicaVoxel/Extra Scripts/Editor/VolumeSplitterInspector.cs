using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VolumeSplitter))]
public class VolumeSplitterInspector : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Split"))
        {
            ((VolumeSplitter)target).Split();
        }
    }
}
