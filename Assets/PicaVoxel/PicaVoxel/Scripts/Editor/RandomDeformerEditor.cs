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
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicaVoxel
{
    [CustomEditor(typeof (RandomDeformer))]
    public class RandomDeformerEditor : Editor
    {
        private PicaVoxelBox constrainBox;
        private bool constrainToBox;
        private bool add;
        private int num;
        private float interval;

        private RandomDeformer voxelDeformer;

        private void OnEnable()
        {
            voxelDeformer = (RandomDeformer) target;

            if (voxelDeformer.ConstrainBox == null)
                voxelDeformer.ConstrainBox = new PicaVoxelBox(0, 0, 0, voxelDeformer.GetComponent<Volume>().XSize,
                    voxelDeformer.GetComponent<Volume>().YSize, voxelDeformer.GetComponent<Volume>().ZSize);

            constrainBox = voxelDeformer.ConstrainBox;
            constrainToBox = voxelDeformer.ConstrainToBox;
            add = voxelDeformer.AddVoxels;
            num = voxelDeformer.NumVoxels;
            interval = voxelDeformer.Interval;

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            //EditorGUILayout.LabelField("Voxel Size:", new[] { GUILayout.Width(75) });
            constrainToBox = EditorGUILayout.ToggleLeft(new GUIContent(" Constrain to Box"), constrainToBox);
            if (constrainToBox != voxelDeformer.ConstrainToBox) voxelDeformer.ConstrainToBox = constrainToBox;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min X:", new[] {GUILayout.Width(50)});
            constrainBox.BottomLeftFront.X = EditorGUILayout.IntField(constrainBox.BottomLeftFront.X);
            EditorGUILayout.LabelField("Min Y:", new[] {GUILayout.Width(50)});
            constrainBox.BottomLeftFront.Y = EditorGUILayout.IntField(constrainBox.BottomLeftFront.Y);
            EditorGUILayout.LabelField("Min Z:", new[] {GUILayout.Width(50)});
            constrainBox.BottomLeftFront.Z = EditorGUILayout.IntField(constrainBox.BottomLeftFront.Z);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max X:", new[] {GUILayout.Width(50)});
            constrainBox.TopRightBack.X = EditorGUILayout.IntField(constrainBox.TopRightBack.X);
            EditorGUILayout.LabelField("Max Y:", new[] {GUILayout.Width(50)});
            constrainBox.TopRightBack.Y = EditorGUILayout.IntField(constrainBox.TopRightBack.Y);
            EditorGUILayout.LabelField("Max Z:", new[] {GUILayout.Width(50)});
            constrainBox.TopRightBack.Z = EditorGUILayout.IntField(constrainBox.TopRightBack.Z);
            EditorGUILayout.EndHorizontal();

            if (constrainBox != voxelDeformer.ConstrainBox) voxelDeformer.ConstrainBox = constrainBox;

            EditorGUILayout.Space();
            interval = EditorGUILayout.FloatField("Deform interval:", interval);
            if (interval != voxelDeformer.Interval)
            {
                if (interval < 0.1) voxelDeformer.Interval = 0.1f;
                voxelDeformer.Interval = interval;
            }
            num = EditorGUILayout.IntField("Voxels per interval:", num);
            if (num != voxelDeformer.NumVoxels)
            {
                if (num < 0) num = 0;
                voxelDeformer.NumVoxels = num;
            }

            EditorGUILayout.Space();
            add = EditorGUILayout.ToggleLeft(new GUIContent(" Add voxels instead of removing"), add);
            if (add != voxelDeformer.AddVoxels) voxelDeformer.AddVoxels = add;
        }
    }
}