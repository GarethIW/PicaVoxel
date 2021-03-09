/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////


#if UNITY_EDITOR
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace PicaVoxel
{
    [CustomEditor(typeof(MultiPartVolume))]
    public class MultiPartVolumeEditor : Editor
    {
        private MultiPartVolume mpvObject;

        private SerializedProperty voxelSizeProperty;
        private SerializedProperty collisionMode;
        private SerializedProperty meshingMode;
        private SerializedProperty colliderMeshingMode;
        private SerializedProperty separateColliderMesh;
        private SerializedProperty pivotProperty;
        private SerializedProperty material;
        private SerializedProperty selfShadeInt;

        private float voxelSize;
        private Vector3 pivot;

        private bool allEditorsEnabled;
        private bool allRuntimeOnlyMesh;

        private bool rtomCheckBox;

        private void OnEnable()
        {
            mpvObject = (MultiPartVolume)target;

            voxelSizeProperty = serializedObject.FindProperty("VoxelSize");
            collisionMode = serializedObject.FindProperty("CollisionMode");
            meshingMode = serializedObject.FindProperty("MeshingMode");
            colliderMeshingMode = serializedObject.FindProperty("MeshColliderMeshingMode");
            separateColliderMesh = serializedObject.FindProperty("GenerateMeshColliderSeparately");
            pivotProperty = serializedObject.FindProperty("Pivot");

            voxelSize = voxelSizeProperty.floatValue;
            pivot = pivotProperty.vector3Value;

            selfShadeInt = serializedObject.FindProperty("SelfShadingIntensity");

            material = serializedObject.FindProperty("Material");

            allEditorsEnabled = AllEditorsEnabled();
            allRuntimeOnlyMesh = AllRuntimeOnlyMesh();
            rtomCheckBox = allRuntimeOnlyMesh;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor", new GUIStyle() { fontStyle = FontStyle.Bold });

            if (!allRuntimeOnlyMesh)
            {
                if (GUILayout.Button(allEditorsEnabled ? "Stop Editing All" : "Start Editing All"))
                {

                    foreach (Volume v in mpvObject.Volumes)
                    {
                        v.IsEnabledForEditing = !allEditorsEnabled;
                        v.PaintMode = EditorPaintMode.Color;

                        v.UpdateAllChunks();
                    }

                    SceneView.RepaintAll();
                    allEditorsEnabled = AllEditorsEnabled();
                }
            }

           rtomCheckBox = EditorGUILayout.ToggleLeft(new GUIContent(" Runtime-Only Mesh"),
                rtomCheckBox);
            if (rtomCheckBox != allRuntimeOnlyMesh)
            {
               
                foreach (Volume v in mpvObject.Volumes)
                {
                    v.IsEnabledForEditing = false;
                    v.RuntimeOnlyMesh = !allRuntimeOnlyMesh;

                    v.CreateChunks();
                    v.UpdateAllChunks();
                }

                allRuntimeOnlyMesh = AllRuntimeOnlyMesh();
                allEditorsEnabled = AllEditorsEnabled();
              
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Size", new GUIStyle() { fontStyle = FontStyle.Bold });
            float size = EditorGUILayout.FloatField("Voxel Size:", voxelSizeProperty.floatValue);
            if (size != voxelSizeProperty.floatValue && size > 0f)
            {
                voxelSizeProperty.floatValue = size;
                voxelSize = voxelSizeProperty.floatValue;
                foreach (Volume v in mpvObject.Volumes)
                {
                    v.VoxelSize = voxelSize;

                    v.CreateChunks();
                }

                mpvObject.RepositionParts();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pivot", new GUIStyle() { fontStyle = FontStyle.Bold });
            pivotProperty.vector3Value = EditorGUILayout.Vector3Field("", pivotProperty.vector3Value, null);
            if (pivotProperty.vector3Value != mpvObject.Pivot)
            {
                pivot = pivotProperty.vector3Value;
                mpvObject.Pivot = pivot;
                mpvObject.RepositionParts();
            }
            if (GUILayout.Button("Set to Center"))
            {
                mpvObject.SetPivotToCenter();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Individual Pivots", new GUIStyle() { fontStyle = FontStyle.Bold });

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set all to Center"))
            {
                foreach (Volume v in mpvObject.Volumes)
                {
                    Vector3 piv = (new Vector3(v.XSize, v.YSize, v.ZSize) * v.VoxelSize) / 2f;
                    v.Pivot = piv;
                    v.UpdatePivot();
                }
            }
            if (GUILayout.Button("Set all to Zero"))
            {
                foreach (Volume v in mpvObject.Volumes)
                {
                    v.Pivot = Vector3.zero;
                    v.UpdatePivot();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collision Mode", new GUIStyle() { fontStyle = FontStyle.Bold });
            collisionMode.enumValueIndex =
                Convert.ToInt16(EditorGUILayout.EnumPopup((CollisionMode)collisionMode.enumValueIndex));
            if (collisionMode.enumValueIndex != Convert.ToInt16(mpvObject.CollisionMode))
            {
                foreach (Volume v in mpvObject.Volumes)
                {
                   v.ChangeCollisionMode((CollisionMode)collisionMode.enumValueIndex);
                }
            }
            if (collisionMode.enumValueIndex > 0)
            {
                separateColliderMesh.boolValue = EditorGUILayout.ToggleLeft(new GUIContent(" Generate collider mesh separately (Edit-time only)"),
                    separateColliderMesh.boolValue);
                if (separateColliderMesh.boolValue != mpvObject.GenerateMeshColliderSeparately)
                {
                    foreach (Volume v in mpvObject.Volumes)
                    {
                        v.GenerateMeshColliderSeparately = separateColliderMesh.boolValue;
                        v.UpdateAllChunks();
                    }
                }

                if (separateColliderMesh.boolValue)
                {
                    EditorGUILayout.LabelField("Collider Meshing Mode", new GUIStyle() { fontStyle = FontStyle.Bold });
                    colliderMeshingMode.enumValueIndex =
                        Convert.ToInt16(EditorGUILayout.EnumPopup((MeshingMode)colliderMeshingMode.enumValueIndex));
                    if (colliderMeshingMode.enumValueIndex != Convert.ToInt16(mpvObject.MeshColliderMeshingMode))
                    {
                        foreach (Volume v in mpvObject.Volumes)
                        {
                            v.MeshColliderMeshingMode = (MeshingMode)colliderMeshingMode.enumValueIndex;
                            v.UpdateAllChunks();
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering", new GUIStyle() { fontStyle = FontStyle.Bold });
            EditorGUILayout.LabelField("Meshing Mode", new GUIStyle() { fontStyle = FontStyle.Bold });
            meshingMode.enumValueIndex =
                Convert.ToInt16(EditorGUILayout.EnumPopup((MeshingMode)meshingMode.enumValueIndex));
            if (meshingMode.enumValueIndex != Convert.ToInt16(mpvObject.MeshingMode))
            {
                foreach (Volume v in mpvObject.Volumes)
                {
                    v.MeshingMode = (MeshingMode)meshingMode.enumValueIndex;
                    v.UpdateAllChunks();
                }
            }

            selfShadeInt.floatValue = EditorGUILayout.Slider("Self-Shading Intensity", selfShadeInt.floatValue, 0, 1);
            if (selfShadeInt.floatValue != mpvObject.SelfShadingIntensity)
            {
                foreach (Volume v in mpvObject.Volumes)
                {
                    v.SelfShadingIntensity = selfShadeInt.floatValue;
                    v.UpdateAllChunks();
                }
            }

            material.objectReferenceValue = EditorGUILayout.ObjectField("Material: ", material.objectReferenceValue, typeof(Material),
                false);
            if (material.objectReferenceValue != mpvObject.Material)
            {
                foreach (Volume v in mpvObject.Volumes)
                {
                    v.Material = (Material)material.objectReferenceValue;
                    v.UpdateAllChunks();
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Mesh-Only Copy", "Create a copy of this Volume with mesh(es) only")))
            {
                GameObject newMPV = new GameObject(target.name + " (Copy)");
                foreach (Volume vol in mpvObject.Volumes)
                {
                    GameObject bake = Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;

                    Volume v = bake.GetComponent<Volume>();
                    v.XSize = vol.XSize;
                    v.YSize = vol.YSize;
                    v.ZSize = vol.ZSize;
                    v.MeshingMode = vol.MeshingMode;
                    v.MeshColliderMeshingMode = vol.MeshColliderMeshingMode;
                    v.GenerateMeshColliderSeparately = vol.GenerateMeshColliderSeparately;
                    v.Material = vol.Material;
                    for (int f = 0; f < vol.Frames.Count; f++)
                    {
                        v.AddFrame(f);
                        v.Frames[f].Voxels = vol.Frames[f].Voxels;
                    }
                    v.CreateChunks();
                    v.ChangeCollisionMode(vol.CollisionMode);
                    v.UpdateAllChunks();

                    bake.tag = "Untagged";
                    bake.name = vol.name;
                    DestroyImmediate(bake.GetComponent<Volume>());
                    DestroyImmediate(bake.transform.Find("Hitbox").gameObject);
                    for (int i = 0; i < bake.transform.childCount; i++)
                    {
                        GameObject o = bake.transform.GetChild(i).gameObject;
                        if (o.GetComponent<Frame>() != null)
                        {
                            DestroyImmediate(o.GetComponent<Frame>());
                            for (int c = 0; c < o.transform.Find("Chunks").childCount; c++)
                            {
                                GameObject chunk = o.transform.Find("Chunks").GetChild(c).gameObject;
                                if (chunk.GetComponent<Chunk>() != null)
                                    DestroyImmediate(chunk.GetComponent<Chunk>());
                            }
                        }
                    }

                    bake.transform.parent = newMPV.transform;
                    bake.transform.localPosition = vol.transform.localPosition;
                }

            }

            serializedObject.ApplyModifiedProperties();
        }

        bool AllEditorsEnabled()
        {
            bool enabled = true;
            if(mpvObject.Volumes==null) mpvObject.GetPartReferences();
            foreach(Volume v in mpvObject.Volumes)
                if (v != null && !v.IsEnabledForEditing) enabled = false;

            return enabled;
        }

        bool AllRuntimeOnlyMesh()
        {
            bool rtom = true;
            if (mpvObject.Volumes == null) mpvObject.GetPartReferences();
            foreach (Volume v in mpvObject.Volumes)
                if (v != null && !v.RuntimeOnlyMesh) rtom = false;

            return rtom;
        }
    }
}
#endif
