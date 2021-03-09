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
    public class MultiPartWindow : EditorWindow
    {
        private GameObject parentObject;

        private int xSize = 1;
        private int ySize = 1;
        private int zSize = 1;

        private int volumeXSize = 32;
        private int volumeYSize = 32;
        private int volumeZSize = 32;

        private bool centerPivot = false;

        private float voxelSize = 0.1f;

        private string objectName = "Multi-part Volume";

        private FillMode fillMode = FillMode.AllVoxels;

        public void Init()
        {
            titleContent = new GUIContent("Create Multi-part Volume");
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Create Multi-part Volume", new GUIStyle() { fontStyle = FontStyle.Bold });
            EditorGUILayout.Space();
            objectName = EditorGUILayout.TextField("Volume name: ", objectName);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Number of volumes:");
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X:", new[] {GUILayout.Width(30)});
            xSize = EditorGUILayout.IntField(xSize);
            EditorGUILayout.LabelField("Y:", new[] {GUILayout.Width(30)});
            ySize = EditorGUILayout.IntField(ySize);
            EditorGUILayout.LabelField("Z:", new[] {GUILayout.Width(30)});
            zSize = EditorGUILayout.IntField(zSize);
            EditorGUILayout.EndHorizontal();

            if (xSize < 1) xSize = 1;
            if (ySize < 1) ySize = 1;
            if (zSize < 1) zSize = 1;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Size of each volume:");
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X:", new[] { GUILayout.Width(30) });
            volumeXSize = EditorGUILayout.IntField(volumeXSize);
            EditorGUILayout.LabelField("Y:", new[] { GUILayout.Width(30) });
            volumeYSize = EditorGUILayout.IntField(volumeYSize);
            EditorGUILayout.LabelField("Z:", new[] { GUILayout.Width(30) });
            volumeZSize = EditorGUILayout.IntField(volumeZSize);
            EditorGUILayout.EndHorizontal();

            if (volumeXSize < 1) volumeXSize = 1;
            if (volumeYSize < 1) volumeYSize = 1;
            if (volumeZSize < 1) volumeZSize = 1;
            if (volumeXSize > 64) volumeXSize = 64;
            if (volumeYSize > 64) volumeYSize = 64;
            if (volumeZSize > 64) volumeZSize = 64;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total size (voxels): " + xSize * volumeXSize + "x" + ySize * volumeYSize + "x" + zSize * volumeZSize);

            EditorGUILayout.Space();
            voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);
            fillMode = (FillMode)EditorGUILayout.EnumPopup("Fill each volume: ", fillMode);
            centerPivot = EditorGUILayout.ToggleLeft(" Center Pivot", centerPivot);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create"))
            {
                if (xSize < 1) xSize = 1;
                if (ySize < 1) ySize = 1;
                if (zSize < 1) zSize = 1;

                parentObject = new GameObject();
                parentObject.name = objectName;
                MultiPartVolume mpv = parentObject.AddComponent<MultiPartVolume>();
                mpv.XSize = xSize;
                mpv.YSize = ySize;
                mpv.ZSize = zSize;

                for (int x = 0; x < xSize; x++)
                    for (int y = 0; y < ySize; y++)
                        for (int z = 0; z < zSize; z++)
                        {
                            var newObject = Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
                            if (newObject != null)
                            {
                                newObject.name = objectName + " ("+x+","+y+"," + z +")";
                                newObject.GetComponent<Volume>().XSize = volumeXSize;
                                newObject.GetComponent<Volume>().YSize = volumeYSize;
                                newObject.GetComponent<Volume>().ZSize = volumeZSize;
                                newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
                                newObject.GetComponent<Volume>().VoxelSize = voxelSize;
                                newObject.GetComponent<Volume>().GenerateBasic(fillMode);
                                newObject.transform.parent = parentObject.transform;
                                //if(!centerPivot)
                                    newObject.transform.localPosition = new Vector3(x* volumeXSize * voxelSize, y*volumeYSize*voxelSize, z*volumeZSize*voxelSize);
                                //else
                                //{
                                //    newObject.GetComponent<Volume>().Pivot = (new Vector3(volumeXSize, volumeYSize, volumeZSize) * voxelSize) / 2f;
                                //    newObject.GetComponent<Volume>().UpdatePivot();
                                //    newObject.transform.localPosition = -(new Vector3(xSize * volumeXSize * (voxelSize/2f), ySize * volumeYSize * (voxelSize/2f), zSize * volumeZSize * (voxelSize/2f)))
                                //        + new Vector3(volumeXSize * (voxelSize / 2f), volumeYSize * (voxelSize / 2f), volumeZSize * (voxelSize / 2f))
                                //        + new Vector3(x * volumeXSize * voxelSize, y * volumeYSize * voxelSize, z * volumeZSize * voxelSize);
                                //}
                            }
                        }

                mpv.Material = EditorUtility.PicaVoxelDiffuseMaterial;
                mpv.GetPartReferences();
                if(centerPivot) mpv.SetPivotToCenter();
                Close();
            }
            if (GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }

}