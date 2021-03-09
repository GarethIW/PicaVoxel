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
using System.IO;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections;

namespace PicaVoxel
{
    public enum EditorCursorMode
    {
        Add,
        Subtract,
        Paint,
        BoxAdd,
        BoxSubtract,
        BoxPaint,
        Select,
        PickColor,
        PickValue,
        BrushAdd,
        BrushSubtract,
        BrushPaint
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Volume))]
    public partial class VolumeEditor : Editor
    {
        private Volume voxelObject;

        private Vector3 cursorPosition;

        private SerializedProperty voxelSizeProperty;
        private SerializedProperty overlapAmountProperty;
        private SerializedProperty collisionMode;
        private SerializedProperty meshingMode;
        private SerializedProperty meshCompression;
        private SerializedProperty colliderMeshingMode;
        private SerializedProperty separateColliderMesh;
        private SerializedProperty pivotProperty;
        private SerializedProperty runtimeOnlyMesh;
        private SerializedProperty material;
        private SerializedProperty selfShadeInt;
        private SerializedProperty physicMaterial;
        private SerializedProperty castShadows;
        private SerializedProperty receiveShadows;
        private SerializedProperty chunkLayer;
        private SerializedProperty collisionTrigger;
        private SerializedProperty isEnabledForEditing;

        private float voxelSize;
        private float overlapAmount;
        private Vector3 pivot;
        private bool drawGrid;

        private Color newColor;

        private PicaVoxelPoint boxStart;
        private PicaVoxelBox currentBox;
        private PicaVoxelPoint previousBrushPos = new PicaVoxelPoint(0, 0, 0);

        private EditorPaintMode paintMode = EditorPaintMode.Color;

        private bool propogateAllFrames = false;

        private bool allFrames = true;

        private PicaVoxelBox changedVoxelExtents;

        private bool buttonJustClicked = false;

        private PrefabStage prefabStage;
        
        private void OnEnable()
        {   
            voxelObject = (Volume)target;
            if (voxelObject == null) return;

            prefabStage = PrefabStageUtility.GetPrefabStage(voxelObject.gameObject);
            var isInStage = (prefabStage != null && prefabStage.IsPartOfPrefabContents(voxelObject.gameObject)); 
            
            
            Undo.undoRedoPerformed += () => voxelObject.OnUndoRedo();

            voxelSizeProperty = serializedObject.FindProperty("VoxelSize");
            overlapAmountProperty = serializedObject.FindProperty("OverlapAmount");
            collisionMode = serializedObject.FindProperty("CollisionMode");
            meshingMode = serializedObject.FindProperty("MeshingMode");
            meshCompression = serializedObject.FindProperty("MeshCompression");
            colliderMeshingMode = serializedObject.FindProperty("MeshColliderMeshingMode");
            separateColliderMesh = serializedObject.FindProperty("GenerateMeshColliderSeparately");
            pivotProperty = serializedObject.FindProperty("Pivot");

            voxelSize = voxelSizeProperty.floatValue;
            overlapAmount = overlapAmountProperty.floatValue;
            pivot = pivotProperty.vector3Value;

            selfShadeInt = serializedObject.FindProperty("SelfShadingIntensity");

            drawGrid = voxelObject.DrawGrid;
            runtimeOnlyMesh = serializedObject.FindProperty("RuntimeOnlyMesh");

            material = serializedObject.FindProperty("Material");
            physicMaterial = serializedObject.FindProperty("PhysicMaterial");
            castShadows = serializedObject.FindProperty("CastShadows");
            receiveShadows = serializedObject.FindProperty("ReceiveShadows");
            chunkLayer = serializedObject.FindProperty("ChunkLayer");
            collisionTrigger = serializedObject.FindProperty("CollisionTrigger");

            isEnabledForEditing = serializedObject.FindProperty("IsEnabledForEditing");

            if (voxelObject != null && !Application.isPlaying && !isInStage)
            {
                string path = Path.Combine(Helper.GetMeshStorePath(), voxelObject.AssetGuid);
                if (!Directory.Exists(path))
                    voxelObject.CreateChunks();
            }

            if (!Application.isPlaying && voxelObject.gameObject.activeSelf && !isInStage && !voxelObject.RuntimeOnlyMesh)
                foreach (var frame in voxelObject.Frames)
                {
                    if (frame.HasDeserialized) frame.UpdateChunks(true);
                    frame.HasDeserialized = false;
                }

            paintMode = voxelObject.PaintMode;

            if (EditorPersistence.SelectBox.BottomLeftFront.X > voxelObject.XSize - 1 ||
                EditorPersistence.SelectBox.BottomLeftFront.Y > voxelObject.YSize - 1 ||
                EditorPersistence.SelectBox.BottomLeftFront.Z > voxelObject.ZSize - 1 ||
                EditorPersistence.SelectBox.TopRightBack.X > voxelObject.XSize - 1 ||
                EditorPersistence.SelectBox.TopRightBack.Y > voxelObject.YSize - 1 ||
                EditorPersistence.SelectBox.TopRightBack.Z > voxelObject.ZSize - 1)
                EditorPersistence.SelectBox = new PicaVoxelBox(0, 0, 0, voxelObject.XSize - 1, voxelObject.YSize - 1, voxelObject.ZSize - 1);
        }


        private void OnSceneGUI()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(voxelObject) && voxelObject.IsEnabledForEditing && prefabStage == null)
            {
                voxelObject.IsEnabledForEditing = false;
            }

            voxelObject.GetCurrentFrame().UpdateTransformMatrix();

            if (Selection.Contains(voxelObject.gameObject))
                UnityEditor.Tools.hidden = voxelObject.IsEnabledForEditing;
            else
                UnityEditor.Tools.hidden = false;

            buttonJustClicked = false;

            if (voxelObject.transform.localScale != Vector3.one && !Application.isPlaying)
                voxelObject.transform.localScale = Vector3.one;

            foreach (Frame frame in voxelObject.Frames)
            {
                if (frame.transform.localScale != Vector3.one && !Application.isPlaying)
                    frame.transform.localScale = Vector3.one;

                if (frame.transform.Find("Chunks").localScale != Vector3.one && !Application.isPlaying)
                    frame.transform.Find("Chunks").localScale = Vector3.one;
            }


            if (!voxelObject.IsEnabledForEditing || Selection.objects.Length > 1 || EditorUtility.Buttons.Count == 0)
                return;

            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }


            Event e = Event.current;

            GUILayout.BeginArea(new Rect(10, 10, 120, 500));
            GUILayout.BeginVertical();

            GUISkin skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
            paintMode =
                (EditorPaintMode)
                    EditorGUILayout.EnumPopup(voxelObject.PaintMode,
                        new GUIStyle(skin.GetStyle("DropDown")) { margin = new RectOffset(0, 0, 0, 5) },
                        GUILayout.Width(100));
            if (paintMode != voxelObject.PaintMode)
            {
                voxelObject.PaintMode = paintMode;
                voxelObject.UpdateAllChunks();
            }

            EditorPersistence.SelectedValue =
                (byte)
                    GUILayout.HorizontalSlider(EditorPersistence.SelectedValue, 0f, 255f,
                        new GUIStyle(GUI.skin.horizontalSlider) { margin = new RectOffset(0, 0, 0, 0) },
                        new GUIStyle(GUI.skin.horizontalSliderThumb) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(100)); //.IntSlider((int)selectedValue, 0, 255, GUILayout.Width(120));

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Value:",
                new GUIStyle(GUI.skin.label) { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 5) },
                GUILayout.Width(42));
            string inputSelectedValue = EditorGUILayout.TextField(EditorPersistence.SelectedValue.ToString(),
                new GUIStyle(GUI.skin.textField)
                {
                    padding = new RectOffset(2, 0, 2, 0),
                    margin = new RectOffset(0, 0, 0, 5),
                    fontStyle = FontStyle.Bold
                }, GUILayout.Width(50), GUILayout.Height(20));
            int trySelectedValue;
            int.TryParse(inputSelectedValue, out trySelectedValue);
            if (trySelectedValue >= 0 && trySelectedValue <= 255)
                EditorPersistence.SelectedValue = (byte)trySelectedValue;
            GUILayout.EndHorizontal();

            int colNum = 0;

            for (int y = 0; y < 5; y++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < 5; x++)
                {
                    if (PaletteButton(voxelObject.PaletteColors[colNum], EditorPersistence.SelectedColor == colNum))
                    {

                        EditorPersistence.SelectedColor = colNum;
                        buttonJustClicked = true;
                    }
                    colNum++;
                }
                GUILayout.EndHorizontal();
            }
            newColor = EditorGUILayout.ColorField(voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                GUILayout.Width(90));
            if (newColor != voxelObject.PaletteColors[EditorPersistence.SelectedColor])
                voxelObject.PaletteColors[EditorPersistence.SelectedColor] = newColor;

            GUILayout.EndVertical();

            try
            {
                EditorCursorMode oldMode = EditorPersistence.CursorMode;
                GUILayout.BeginHorizontal();
                if (HighlightButton(EditorUtility.Buttons["pvButton_Add"],
                    EditorPersistence.CursorMode == EditorCursorMode.Add,
                    "Add single voxels"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.Add;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_Subtract"],
                    EditorPersistence.CursorMode == EditorCursorMode.Subtract, "Subtract single voxels"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.Subtract;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_Paint"],
                    EditorPersistence.CursorMode == EditorCursorMode.Paint, "Paint single voxels"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.Paint;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (HighlightButton(EditorUtility.Buttons["pvButton_BoxAdd"],
                    EditorPersistence.CursorMode == EditorCursorMode.BoxAdd, "Draw a box to add voxels"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.BoxAdd;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_BoxSubtract"],
                    EditorPersistence.CursorMode == EditorCursorMode.BoxSubtract, "Draw a box to subtract voxels"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.BoxSubtract;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_BoxPaint"],
                    EditorPersistence.CursorMode == EditorCursorMode.BoxPaint, "Draw a box to paint voxels"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.BoxPaint;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (HighlightButton(EditorUtility.Buttons["pvButton_Select"],
                    EditorPersistence.CursorMode == EditorCursorMode.Select, "Select voxels to create a brush"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.Select;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_PickColor"],
                    EditorPersistence.CursorMode == EditorCursorMode.PickColor,
                    "Change the currently selected color to the color of an existing voxel"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.PickColor;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_PickValue"],
                    EditorPersistence.CursorMode == EditorCursorMode.PickValue,
                    "Change the currently selected value to the value of an existing voxel"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.PickValue;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (HighlightButton(EditorUtility.Buttons["pvButton_BrushAdd"],
                    EditorPersistence.CursorMode == EditorCursorMode.BrushAdd,
                    "Add voxels using stored brush"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.BrushAdd;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_BrushSubtract"],
                    EditorPersistence.CursorMode == EditorCursorMode.BrushSubtract, "Subtract voxels using stored brush"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.BrushSubtract;
                }
                if (HighlightButton(EditorUtility.Buttons["pvButton_BrushPaint"],
                    EditorPersistence.CursorMode == EditorCursorMode.BrushPaint, "Paint voxels using stored brush"))
                {
                    buttonJustClicked = true;
                    EditorPersistence.CursorMode = EditorCursorMode.BrushPaint;
                }
                GUILayout.EndHorizontal();

                if (EditorPersistence.CursorMode == EditorCursorMode.Select)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Selection");
                    if (GUILayout.Button("Create Brush", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 5) },
                        GUILayout.Width(100)))
                    {
                        buttonJustClicked = true;
                        EditorPersistence.Brush =
                            new Voxel[((EditorPersistence.SelectBox.TopRightBack.X + 1) - EditorPersistence.SelectBox.BottomLeftFront.X) *
                                      ((EditorPersistence.SelectBox.TopRightBack.Y + 1) - EditorPersistence.SelectBox.BottomLeftFront.Y) *
                                      ((EditorPersistence.SelectBox.TopRightBack.Z + 1) - EditorPersistence.SelectBox.BottomLeftFront.Z)];
                        EditorPersistence.BrushSize = new PicaVoxelPoint((EditorPersistence.SelectBox.TopRightBack.X + 1) - EditorPersistence.SelectBox.BottomLeftFront.X,
                                                                         (EditorPersistence.SelectBox.TopRightBack.Y + 1) - EditorPersistence.SelectBox.BottomLeftFront.Y,
                                                                         (EditorPersistence.SelectBox.TopRightBack.Z + 1) - EditorPersistence.SelectBox.BottomLeftFront.Z);
                        PicaVoxelBox destBox = new PicaVoxelBox(0, 0, 0, EditorPersistence.BrushSize.X - 1, EditorPersistence.BrushSize.Y - 1, EditorPersistence.BrushSize.Z - 1);
                        Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels, ref EditorPersistence.Brush,
                            EditorPersistence.SelectBox, destBox, new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), EditorPersistence.BrushSize, false);
                        EditorPersistence.CursorMode = EditorCursorMode.BrushAdd;

                        voxelObject.GetCurrentFrame().EditingVoxels =
                                   new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                        Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                            ref voxelObject.GetCurrentFrame().EditingVoxels,
                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), false);
                    }
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fill", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        RegisterUndo();
                        if (propogateAllFrames)
                        {
                            foreach (Frame f in voxelObject.Frames)
                            {
                                f.EditingVoxels = null;
                                for (int x = EditorPersistence.SelectBox.BottomLeftFront.X; x <= EditorPersistence.SelectBox.TopRightBack.X; x++)
                                    for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y; y <= EditorPersistence.SelectBox.TopRightBack.Y; y++)
                                        for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z; z <= EditorPersistence.SelectBox.TopRightBack.Z; z++)
                                            f.Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel()
                                            {
                                                State = VoxelState.Active,
                                                Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                                Value = EditorPersistence.SelectedValue
                                            };
                                f.SaveForSerialize();
                                f.UpdateAllChunks();
                            }
                        }
                        else
                        {
                            voxelObject.Frames[voxelObject.CurrentFrame].EditingVoxels = null;
                            for (int x = EditorPersistence.SelectBox.BottomLeftFront.X; x <= EditorPersistence.SelectBox.TopRightBack.X; x++)
                                for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y; y <= EditorPersistence.SelectBox.TopRightBack.Y; y++)
                                    for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z; z <= EditorPersistence.SelectBox.TopRightBack.Z; z++)
                                        voxelObject.Frames[voxelObject.CurrentFrame].Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel()
                                        {
                                            State = VoxelState.Active,
                                            Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                            Value = EditorPersistence.SelectedValue
                                        };
                            voxelObject.Frames[voxelObject.CurrentFrame].SaveForSerialize();
                            voxelObject.Frames[voxelObject.CurrentFrame].UpdateAllChunks();
                        }
                        buttonJustClicked = true;
                    }
                    if (GUILayout.Button("Clear", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        RegisterUndo();
                        if (propogateAllFrames)
                        {
                            foreach (Frame f in voxelObject.Frames)
                            {
                                f.EditingVoxels = null;
                                for (int x = EditorPersistence.SelectBox.BottomLeftFront.X; x <= EditorPersistence.SelectBox.TopRightBack.X; x++)
                                    for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y; y <= EditorPersistence.SelectBox.TopRightBack.Y; y++)
                                        for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z; z <= EditorPersistence.SelectBox.TopRightBack.Z; z++)
                                            f.Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel()
                                            {
                                                State = VoxelState.Inactive,
                                                Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                                Value = EditorPersistence.SelectedValue
                                            };
                                f.SaveForSerialize();
                                f.UpdateAllChunks();
                            }
                        }
                        else
                        {
                            voxelObject.Frames[voxelObject.CurrentFrame].EditingVoxels = null;
                            for (int x = EditorPersistence.SelectBox.BottomLeftFront.X; x <= EditorPersistence.SelectBox.TopRightBack.X; x++)
                                for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y; y <= EditorPersistence.SelectBox.TopRightBack.Y; y++)
                                    for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z; z <= EditorPersistence.SelectBox.TopRightBack.Z; z++)
                                        voxelObject.Frames[voxelObject.CurrentFrame].Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel()
                                        {
                                            State = VoxelState.Inactive,
                                            Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                            Value = EditorPersistence.SelectedValue
                                        };
                            voxelObject.Frames[voxelObject.CurrentFrame].SaveForSerialize();
                            voxelObject.Frames[voxelObject.CurrentFrame].UpdateAllChunks();
                        }
                        buttonJustClicked = true;
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("Nudge Selection");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("X - 1", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        NudgeSelection(-1, 0, 0);
                        buttonJustClicked = true;
                    }
                    if (GUILayout.Button("X + 1", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        NudgeSelection(1, 0, 0);
                        buttonJustClicked = true;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Y - 1", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        NudgeSelection(0, -1, 0);
                        buttonJustClicked = true;
                    }
                    if (GUILayout.Button("Y + 1", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        NudgeSelection(0, 1, 0);
                        buttonJustClicked = true;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Z - 1", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        NudgeSelection(0, 0, -1);
                        buttonJustClicked = true;
                    }
                    if (GUILayout.Button("Z + 1", new GUIStyle(skin.button) { margin = new RectOffset(0, 0, 0, 0) },
                        GUILayout.Width(50)))
                    {
                        NudgeSelection(0, 0, 1);
                        buttonJustClicked = true;
                    }
                    GUILayout.EndHorizontal();
                }

                if (EditorPersistence.CursorMode == EditorCursorMode.BrushAdd ||
                    EditorPersistence.CursorMode == EditorCursorMode.BrushSubtract ||
                    EditorPersistence.CursorMode == EditorCursorMode.BrushPaint)
                {
                    if (EditorPersistence.Brush == null) EditorPersistence.CursorMode = EditorCursorMode.Select;

                    GUILayout.Space(10);
                    EditorPersistence.BrushReplace = EditorGUILayout.ToggleLeft(" Replace", EditorPersistence.BrushReplace);

                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Brush Anchor");
                    EditorPersistence.BrushAnchorX =
                        (AnchorX)
                            EditorGUILayout.EnumPopup(EditorPersistence.BrushAnchorX,
                                new GUIStyle(skin.GetStyle("DropDown")) { margin = new RectOffset(0, 0, 0, 5) },
                                GUILayout.Width(100));
                    EditorPersistence.BrushAnchorY =
                        (AnchorY)
                            EditorGUILayout.EnumPopup(EditorPersistence.BrushAnchorY,
                                new GUIStyle(skin.GetStyle("DropDown")) { margin = new RectOffset(0, 0, 0, 5) },
                                GUILayout.Width(100));
                    EditorPersistence.BrushAnchorZ =
                        (AnchorZ)
                            EditorGUILayout.EnumPopup(EditorPersistence.BrushAnchorZ,
                                new GUIStyle(skin.GetStyle("DropDown")) { margin = new RectOffset(0, 0, 0, 5) },
                                GUILayout.Width(100));

                    EditorGUILayout.LabelField("Brush Rotate");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("X", "Rotate brush 90 degrees around X axis"),
                        new GUIStyle(GUI.skin.button)
                        {
                            padding = new RectOffset(0, 0, 0, 0),
                            margin = new RectOffset(0, 2, 2, 0)
                        }, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        buttonJustClicked = true;
                        RotateBrush(RotateAxis.X);
                        voxelObject.UpdateAllChunks();
                    }
                    if (GUILayout.Button(new GUIContent("Y", "Rotate brush 90 degrees around Y axis"),
                        new GUIStyle(GUI.skin.button)
                        {
                            padding = new RectOffset(0, 0, 0, 0),
                            margin = new RectOffset(0, 2, 2, 0)
                        }, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        buttonJustClicked = true;
                        RotateBrush(RotateAxis.Y);
                        voxelObject.UpdateAllChunks();
                    }
                    if (GUILayout.Button(new GUIContent("Z", "Rotate brush 90 degrees around Z axis"),
                        new GUIStyle(GUI.skin.button)
                        {
                            padding = new RectOffset(0, 0, 0, 0),
                            margin = new RectOffset(0, 2, 2, 0)
                        }, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        buttonJustClicked = true;
                        RotateBrush(RotateAxis.Z);
                        voxelObject.UpdateAllChunks();
                    }
                    GUILayout.EndHorizontal();

                }

                if ((oldMode == EditorCursorMode.BrushAdd || oldMode == EditorCursorMode.BrushPaint ||
                     oldMode == EditorCursorMode.BrushSubtract) && oldMode != EditorPersistence.CursorMode)
                {
                    voxelObject.GetCurrentFrame().EditingVoxels = null;
                    voxelObject.UpdateAllChunks();
                }
            }
            catch (Exception)
            {
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Animation
            GUI.backgroundColor = (EditorGUIUtility.isProSkin ? Color.white : Color.grey);
            GUILayout.BeginArea(new Rect(Screen.width - 410, Screen.height - 100, 400, 50),
                EditorUtility.Buttons["pvButton_AnimBG"]);
            GUILayout.BeginHorizontal(new GUIStyle() { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(260));
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_AddFramePrev"], "Add frame before"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(3, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.AddFrame(voxelObject.CurrentFrame);
            }
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_DeleteFrame"], "Delete frame"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.DeleteFrame();
            }
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_MoveLeft"], "Move frame left"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.MoveFrameLeft();
            }
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_PrevFrame"], "Previous frame"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.PrevFrame();
            }
            GUILayout.Label(voxelObject.CurrentFrame + 1 + "/" + voxelObject.NumFrames,
                new GUIStyle(GUI.skin.label)
                {
                    normal = new GUIStyleState() { textColor = Color.white },
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(2, 0, 0, 3)
                }, GUILayout.Width(80), GUILayout.Height(50));
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_NextFrame"], "Next frame"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.NextFrame();
            }
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_MoveRight"], "Move frame right"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.MoveFrameRight();
            }
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_AddFrameNext"], "Add frame after"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                voxelObject.AddFrame(voxelObject.CurrentFrame + 1);
            }
            if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_CopyFrame"], "Copy frame to clipboard"),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 0, 9, 0)
                }, GUILayout.Width(32), GUILayout.Height(32)))
            {
                buttonJustClicked = true;
                EditorPersistence.AnimFrameClipboard = new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                Array.Copy(voxelObject.Frames[voxelObject.CurrentFrame].Voxels, EditorPersistence.AnimFrameClipboard,
                    voxelObject.Frames[voxelObject.CurrentFrame].Voxels.Length);
                EditorPersistence.AnimFrameClipboardSize = new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize);
            }

            if (
                GUILayout.Button(
                    new GUIContent(EditorUtility.Buttons["pvButton_PasteFrame"], "Paste frame from clipboard"),
                    new GUIStyle(GUI.skin.button)
                    {
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 0, 9, 0)
                    }, GUILayout.Width(32), GUILayout.Height(32)) && EditorPersistence.AnimFrameClipboard != null)
            {
                buttonJustClicked = true;
                voxelObject.AddFrame(voxelObject.CurrentFrame);
                voxelObject.SetFrame(voxelObject.CurrentFrame + 1);
                for (int x = 0; x < EditorPersistence.AnimFrameClipboardSize.X; x++)
                    for (int y = 0; y < EditorPersistence.AnimFrameClipboardSize.Y; y++)
                        for (int z = 0; z < EditorPersistence.AnimFrameClipboardSize.Z; z++)
                        {
                            if (x < voxelObject.XSize && y < voxelObject.YSize && z < voxelObject.ZSize)
                            {
                                voxelObject.Frames[voxelObject.CurrentFrame].Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] =
                                    EditorPersistence.AnimFrameClipboard[x + EditorPersistence.AnimFrameClipboardSize.X * (y + EditorPersistence.AnimFrameClipboardSize.Y * z)];
                            }
                        }
                voxelObject.SaveForSerialize();
                voxelObject.UpdateAllChunks();
            }
            GUILayout.EndArea();
            GUI.backgroundColor = Color.white;

            Handles.SetCamera(Camera.current);
            DoCursor(e);

            //HandleUtility.Repaint();
        }

        private void OnDrawGizmos()
        {
            voxelObject.OnDrawGizmos();
        }

        private void DoCursor(Event e)
        {
            //if (buttonJustClicked && e.type == EventType.MouseUp && e.button == 0)
            //    buttonJustClicked = false;

            if (PrefabUtility.IsPartOfAnyPrefab(voxelObject) && voxelObject.IsEnabledForEditing && prefabStage == null)
            {
                voxelObject.IsEnabledForEditing = false;
                return;
            }

            var isInStage = (prefabStage != null && prefabStage.IsPartOfPrefabContents(voxelObject.gameObject)); 
            
            if (GUIUtility.hotControl == 0 && EditorGUIUtility.hotControl == 0 && GUI.GetNameOfFocusedControl() == "" && buttonJustClicked == false)
            {

                bool validBrushPos = false;

                if (voxelObject.Undone)
                {
                    //voxelObject.CreateChunks();
                    voxelObject.Undone = false;
                }
                foreach (var frame in voxelObject.Frames)
                {
                    if (frame.HasDeserialized && !isInStage) frame.CreateChunks();
                    frame.HasDeserialized = false;
                }


                Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                //  Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x,
                //   -e.mousePosition.y + Camera.current.pixelHeight));

                //Debug.DrawRay(r.origin, r.direction*200f, Color.red );

                for (float d = 0f; d < voxelObject.VoxelSize * 200f; d += voxelObject.VoxelSize * 0.1f)
                {
                    Voxel? v = voxelObject.GetVoxelAtWorldPosition(r.GetPoint(d));

                    if ((EditorPersistence.CursorMode == EditorCursorMode.Add ||
                         EditorPersistence.CursorMode == EditorCursorMode.BoxAdd ||
                         (EditorPersistence.CursorMode == EditorCursorMode.BrushAdd && !EditorPersistence.BrushReplace)) &&
                        v.HasValue)
                    {
                        if (!v.Value.Active) continue;

                        d -= voxelObject.VoxelSize * 0.1f;
                        v = voxelObject.GetVoxelAtWorldPosition(r.GetPoint(d));
                        if (!v.HasValue) break;
                    }

                    if (v.HasValue &&
                        (v.Value.Active || EditorPersistence.CursorMode == EditorCursorMode.Add ||
                         EditorPersistence.CursorMode == EditorCursorMode.BoxAdd ||
                         (EditorPersistence.CursorMode == EditorCursorMode.BrushAdd && !EditorPersistence.BrushReplace)))
                    {
                        cursorPosition =
                            voxelObject.transform.TransformPoint(((voxelObject.GetVoxelPosition(r.GetPoint(d)) *
                                                                   voxelObject.VoxelSize) - voxelObject.Pivot) +
                                                                 ((Vector3.one * voxelObject.VoxelSize) * 0.5f));

                        if (EditorPersistence.CursorMode != EditorCursorMode.BrushAdd &&
                            EditorPersistence.CursorMode != EditorCursorMode.BrushPaint &&
                            EditorPersistence.CursorMode != EditorCursorMode.BrushSubtract)
                        {
                            Gizmos.matrix =
                                voxelObject.GetCurrentFrame().transform.Find("Chunks").localToWorldMatrix;
                            Handles.color = (EditorPersistence.CursorMode == EditorCursorMode.Subtract ||
                                             EditorPersistence.CursorMode == EditorCursorMode.BoxSubtract ||
                                             EditorPersistence.CursorMode == EditorCursorMode.PickColor ||
                                             EditorPersistence.CursorMode == EditorCursorMode.PickValue)
                                ? Color.red * 0.5f
                                : voxelObject.PaletteColors[EditorPersistence.SelectedColor] * 0.5f;
                            Handles.CubeHandleCap(0, cursorPosition, voxelObject.transform.rotation, voxelObject.VoxelSize, EventType.Repaint);
                            Repaint();
                            SceneView.RepaintAll();
                            Gizmos.matrix = Matrix4x4.identity;
                        }


                        // Mouse down (non-brush)
                        if (e.type == EventType.MouseDown && e.button == 0 &&
                            (EditorPersistence.CursorMode != EditorCursorMode.BrushAdd &&
                             EditorPersistence.CursorMode != EditorCursorMode.BrushPaint &&
                             EditorPersistence.CursorMode != EditorCursorMode.BrushSubtract))
                        {
                            //RegisterUndo();
                            if (EditorPersistence.CursorMode != EditorCursorMode.PickColor &&
                                EditorPersistence.CursorMode != EditorCursorMode.PickValue)
                            {
                                if (propogateAllFrames)
                                {


                                    foreach (Frame f in voxelObject.Frames)
                                    {
                                        f.EditingVoxels =
                                            new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                                        Helper.CopyVoxelsInBox(ref f.Voxels,
                                            ref f.EditingVoxels,
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);

                                    }
                                }
                                else
                                {

                                    voxelObject.GetCurrentFrame().EditingVoxels =
                                        new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                                    Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                                        ref voxelObject.GetCurrentFrame().EditingVoxels,
                                        new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                        new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                        false);

                                }
                            }

                            if (EditorPersistence.CursorMode == EditorCursorMode.BoxAdd ||
                                EditorPersistence.CursorMode == EditorCursorMode.BoxSubtract ||
                                EditorPersistence.CursorMode == EditorCursorMode.BoxPaint)
                            {
                                boxStart = voxelObject.GetVoxelArrayPosition(r.GetPoint(d));
                                currentBox = new PicaVoxelBox(boxStart, boxStart);
                                changedVoxelExtents = new PicaVoxelBox(boxStart, boxStart);
                            }

                            if (EditorPersistence.CursorMode == EditorCursorMode.Add ||
                                EditorPersistence.CursorMode == EditorCursorMode.Subtract ||
                                EditorPersistence.CursorMode == EditorCursorMode.Paint)
                            {
                                changedVoxelExtents = new PicaVoxelBox(voxelObject.GetVoxelArrayPosition(r.GetPoint(d)), voxelObject.GetVoxelArrayPosition(r.GetPoint(d)));
                            }

                            if (EditorPersistence.CursorMode == EditorCursorMode.PickColor)
                            {
                                bool colorFound = false;
                                for (int c = 0; c < 25; c++)
                                    if (((Color32)voxelObject.PaletteColors[c]).r == ((Color32)v.Value.Color).r &&
                                        ((Color32)voxelObject.PaletteColors[c]).g == ((Color32)v.Value.Color).g &&
                                        ((Color32)voxelObject.PaletteColors[c]).b == ((Color32)v.Value.Color).b)
                                    {
                                        EditorPersistence.SelectedColor = c;
                                        colorFound = true;
                                    }

                                if (!colorFound)
                                    voxelObject.PaletteColors[EditorPersistence.SelectedColor] = v.Value.Color;
                            }

                            if (EditorPersistence.CursorMode == EditorCursorMode.PickValue)
                            {
                                EditorPersistence.SelectedValue = v.Value.Value;
                            }
                        }

                        // Mouse drag (non-brush)
                        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                        {
                            if (e.type == EventType.MouseDrag &&
                                (EditorPersistence.CursorMode == EditorCursorMode.BoxAdd ||
                                 EditorPersistence.CursorMode == EditorCursorMode.BoxSubtract ||
                                 EditorPersistence.CursorMode == EditorCursorMode.BoxPaint) && boxStart != null)
                            {
                                if (propogateAllFrames)
                                {
                                    foreach (Frame f in voxelObject.Frames)
                                    {
                                        Helper.CopyVoxelsInBox(ref f.Voxels,
                                            ref f.EditingVoxels, currentBox, currentBox,
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);

                                    }
                                }
                                else
                                {
                                    Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                                        ref voxelObject.GetCurrentFrame().EditingVoxels, currentBox, currentBox,
                                        new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                        new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                        false);
                                }
                                //voxelObject.UpdateAllChunks();

                                PicaVoxelPoint boxEnd = voxelObject.GetVoxelArrayPosition(r.GetPoint(d));
                                currentBox = new PicaVoxelBox(boxStart, boxEnd);

                                if (currentBox.BottomLeftFront.X < changedVoxelExtents.BottomLeftFront.X)
                                    changedVoxelExtents.BottomLeftFront.X = currentBox.BottomLeftFront.X;
                                if (currentBox.BottomLeftFront.Y < changedVoxelExtents.BottomLeftFront.Y)
                                    changedVoxelExtents.BottomLeftFront.Y = currentBox.BottomLeftFront.Y;
                                if (currentBox.BottomLeftFront.Z < changedVoxelExtents.BottomLeftFront.Z)
                                    changedVoxelExtents.BottomLeftFront.Z = currentBox.BottomLeftFront.Z;
                                if (currentBox.TopRightBack.X > changedVoxelExtents.TopRightBack.X)
                                    changedVoxelExtents.TopRightBack.X = currentBox.TopRightBack.X;
                                if (currentBox.TopRightBack.Y > changedVoxelExtents.TopRightBack.Y)
                                    changedVoxelExtents.TopRightBack.Y = currentBox.TopRightBack.Y;
                                if (currentBox.TopRightBack.Z > changedVoxelExtents.TopRightBack.Z)
                                    changedVoxelExtents.TopRightBack.Z = currentBox.TopRightBack.Z;


                                for (int x = currentBox.BottomLeftFront.X; x <= currentBox.TopRightBack.X; x++)
                                    for (int y = currentBox.BottomLeftFront.Y; y <= currentBox.TopRightBack.Y; y++)
                                        for (int z = currentBox.BottomLeftFront.Z; z <= currentBox.TopRightBack.Z; z++)
                                        {
                                            if (propogateAllFrames)
                                            {
                                                foreach (Frame f in voxelObject.Frames)
                                                {
                                                    switch (EditorPersistence.CursorMode)
                                                    {
                                                        case EditorCursorMode.BoxAdd:
                                                            if (
                                                                !f.Voxels[
                                                                    x + voxelObject.XSize * (y + voxelObject.YSize * z)]
                                                                    .Active)
                                                            {
                                                                f.EditingVoxels[
                                                                    x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel
                                                                        ()
                                                                    {
                                                                        Color =
                                                                            voxelObject.PaletteColors[
                                                                                EditorPersistence.SelectedColor],
                                                                        Value = EditorPersistence.SelectedValue,
                                                                        State =
                                                                            (EditorPersistence.CursorMode !=
                                                                             EditorCursorMode.BoxSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                                    };
                                                            }
                                                            break;
                                                        case EditorCursorMode.BoxSubtract:
                                                            f.EditingVoxels[
                                                                x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel
                                                                    ()
                                                                {
                                                                    Color =
                                                                        voxelObject.PaletteColors[
                                                                            EditorPersistence.SelectedColor],
                                                                    Value = EditorPersistence.SelectedValue,
                                                                    State =
                                                                            (EditorPersistence.CursorMode !=
                                                                             EditorCursorMode.BoxSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                                };
                                                            break;
                                                        case EditorCursorMode.BoxPaint:
                                                            if (
                                                                f.Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)
                                                                    ].Active)
                                                            {
                                                                switch (paintMode)
                                                                {

                                                                    case EditorPaintMode.Color:
                                                                        f.EditingVoxels[
                                                                            x +
                                                                            voxelObject.XSize * (y + voxelObject.YSize * z)]
                                                                            = new Voxel()
                                                                            {
                                                                                Color =
                                                                                    voxelObject.PaletteColors[
                                                                                        EditorPersistence.SelectedColor],
                                                                                Value =
                                                                                    f.Voxels[
                                                                                        x +
                                                                                        voxelObject.XSize *
                                                                                        (y + voxelObject.YSize * z)].Value,
                                                                                State =
                                                                                    f.Voxels[
                                                                                        x +
                                                                                        voxelObject.XSize *
                                                                                        (y + voxelObject.YSize * z)]
                                                                                        .State
                                                                            };
                                                                        //GetCurrentFrame().EditingVoxels[x, y, z].Color = voxelObject.PaletteColors[selectedColor];
                                                                        break;
                                                                    case EditorPaintMode.Value:
                                                                        f.EditingVoxels[
                                                                            x +
                                                                            voxelObject.XSize * (y + voxelObject.YSize * z)]
                                                                            = new Voxel()
                                                                            {
                                                                                Color =
                                                                                    f.Voxels[
                                                                                        x +
                                                                                        voxelObject.XSize *
                                                                                        (y + voxelObject.YSize * z)].Color,
                                                                                Value = EditorPersistence.SelectedValue,
                                                                                State =
                                                                                    f.Voxels[
                                                                                        x +
                                                                                        voxelObject.XSize *
                                                                                        (y + voxelObject.YSize * z)]
                                                                                        .State
                                                                            };
                                                                        //voxelObject.GetCurrentFrame().EditingVoxels[x, y, z].Value = selectedValue;
                                                                        break;
                                                                }
                                                            }
                                                            break;
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                switch (EditorPersistence.CursorMode)
                                                {
                                                    case EditorCursorMode.BoxAdd:
                                                        if (
                                                            !voxelObject.GetCurrentFrame().Voxels[
                                                                x + voxelObject.XSize * (y + voxelObject.YSize * z)].Active)
                                                        {
                                                            voxelObject.GetCurrentFrame().EditingVoxels[
                                                                x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel
                                                                    ()
                                                                {
                                                                    Color =
                                                                        voxelObject.PaletteColors[
                                                                            EditorPersistence.SelectedColor],
                                                                    Value = EditorPersistence.SelectedValue,
                                                                    State =
                                                                            (EditorPersistence.CursorMode !=
                                                                             EditorCursorMode.BoxSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                                };
                                                        }
                                                        break;
                                                    case EditorCursorMode.BoxSubtract:
                                                        voxelObject.GetCurrentFrame().EditingVoxels[
                                                            x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel
                                                                ()
                                                            {
                                                                Color =
                                                                    voxelObject.PaletteColors[
                                                                        EditorPersistence.SelectedColor],
                                                                Value = EditorPersistence.SelectedValue,
                                                                State =
                                                                            (EditorPersistence.CursorMode !=
                                                                             EditorCursorMode.BoxSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                            };
                                                        break;
                                                    case EditorCursorMode.BoxPaint:
                                                        if (
                                                            voxelObject.GetCurrentFrame().Voxels[
                                                                x + voxelObject.XSize * (y + voxelObject.YSize * z)].Active)
                                                        {
                                                            switch (paintMode)
                                                            {

                                                                case EditorPaintMode.Color:
                                                                    voxelObject.GetCurrentFrame().EditingVoxels[
                                                                        x + voxelObject.XSize * (y + voxelObject.YSize * z)]
                                                                        = new Voxel()
                                                                        {
                                                                            Color =
                                                                                voxelObject.PaletteColors[
                                                                                    EditorPersistence.SelectedColor],
                                                                            Value =
                                                                                voxelObject.GetCurrentFrame().Voxels[
                                                                                    x +
                                                                                    voxelObject.XSize *
                                                                                    (y + voxelObject.YSize * z)].Value,
                                                                            State =
                                                                                voxelObject.GetCurrentFrame().Voxels[
                                                                                    x +
                                                                                    voxelObject.XSize *
                                                                                    (y + voxelObject.YSize * z)].State
                                                                        };
                                                                    //GetCurrentFrame().EditingVoxels[x, y, z].Color = voxelObject.PaletteColors[selectedColor];
                                                                    break;
                                                                case EditorPaintMode.Value:
                                                                    voxelObject.GetCurrentFrame().EditingVoxels[
                                                                        x + voxelObject.XSize * (y + voxelObject.YSize * z)]
                                                                        = new Voxel()
                                                                        {
                                                                            Color =
                                                                                voxelObject.GetCurrentFrame().Voxels[
                                                                                    x +
                                                                                    voxelObject.XSize *
                                                                                    (y + voxelObject.YSize * z)].Color,
                                                                            Value = EditorPersistence.SelectedValue,
                                                                            State =
                                                                                voxelObject.GetCurrentFrame().Voxels[
                                                                                    x +
                                                                                    voxelObject.XSize *
                                                                                    (y + voxelObject.YSize * z)].State
                                                                        };
                                                                    //voxelObject.GetCurrentFrame().EditingVoxels[x, y, z].Value = selectedValue;
                                                                    break;
                                                            }
                                                        }
                                                        break;
                                                }
                                            }
                                        }


                                for (int x = changedVoxelExtents.BottomLeftFront.X - 1;
                                    x <= changedVoxelExtents.TopRightBack.X + 1;
                                    x++)
                                    for (int y = changedVoxelExtents.BottomLeftFront.Y - 1;
                                        y <= changedVoxelExtents.TopRightBack.Y + 1;
                                        y++)
                                        for (int z = changedVoxelExtents.BottomLeftFront.Z - 1;
                                            z <= changedVoxelExtents.TopRightBack.Z + 1;
                                            z++)
                                            if (propogateAllFrames)
                                                foreach (Frame f in voxelObject.Frames)
                                                    f.SetChunkAtVoxelPositionDirty(x, y, z);
                                            else voxelObject.GetCurrentFrame().SetChunkAtVoxelPositionDirty(x, y, z);


                                if (propogateAllFrames)
                                    foreach (Frame f in voxelObject.Frames) f.UpdateChunks(true);
                                else voxelObject.UpdateChunks(true);

                            }

                            if (EditorPersistence.CursorMode == EditorCursorMode.Add ||
                                EditorPersistence.CursorMode == EditorCursorMode.Subtract ||
                                EditorPersistence.CursorMode == EditorCursorMode.Paint)
                            {
                                PicaVoxelPoint pv = voxelObject.GetVoxelArrayPosition(r.GetPoint(d));

                                if (pv.X < changedVoxelExtents.BottomLeftFront.X)
                                    changedVoxelExtents.BottomLeftFront.X = pv.X;
                                if (pv.Y < changedVoxelExtents.BottomLeftFront.Y)
                                    changedVoxelExtents.BottomLeftFront.Y = pv.Y;
                                if (pv.Z < changedVoxelExtents.BottomLeftFront.Z)
                                    changedVoxelExtents.BottomLeftFront.Z = pv.Z;
                                if (pv.X > changedVoxelExtents.TopRightBack.X)
                                    changedVoxelExtents.TopRightBack.X = pv.X;
                                if (pv.Y > changedVoxelExtents.TopRightBack.Y)
                                    changedVoxelExtents.TopRightBack.Y = pv.Y;
                                if (pv.Z > changedVoxelExtents.TopRightBack.Z)
                                    changedVoxelExtents.TopRightBack.Z = pv.Z;

                                if (propogateAllFrames)
                                {
                                    foreach (Frame f in voxelObject.Frames)
                                    {
                                        f.SetVoxelAtWorldPosition(r.GetPoint(d),
                                            new Voxel()
                                            {
                                                State = (EditorPersistence.CursorMode != EditorCursorMode.Subtract) ? VoxelState.Active : VoxelState.Inactive,
                                                Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                                Value = EditorPersistence.SelectedValue
                                            });
                                        f.UpdateChunks(true);
                                    }
                                }
                                else
                                {
                                    voxelObject.SetVoxelAtWorldPosition(r.GetPoint(d),
                                        new Voxel()
                                        {
                                            State = (EditorPersistence.CursorMode != EditorCursorMode.Subtract) ? VoxelState.Active : VoxelState.Inactive,
                                            Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                            Value = EditorPersistence.SelectedValue
                                        });
                                    voxelObject.UpdateChunks(true);
                                }

                            }
                        }

                        // Mouse move (brush update)
                        if ((EditorPersistence.CursorMode == EditorCursorMode.BrushAdd ||
                             EditorPersistence.CursorMode == EditorCursorMode.BrushPaint ||
                             EditorPersistence.CursorMode == EditorCursorMode.BrushSubtract))
                        {
                            validBrushPos = true;

                            if (e.type == EventType.MouseMove)
                            {
                                if (propogateAllFrames)
                                {
                                    foreach (Frame f in voxelObject.Frames)
                                    {
                                        if (f.EditingVoxels == null)
                                        {
                                            f.EditingVoxels =
                                                new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                                            Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                                                ref f.EditingVoxels,
                                                new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                                new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                                false);
                                        }
                                        //f.UpdateAllChunks();
                                    }
                                }
                                else
                                {
                                    if (voxelObject.GetCurrentFrame().EditingVoxels == null)
                                    {
                                        voxelObject.GetCurrentFrame().EditingVoxels =
                                            new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                                        Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                                            ref voxelObject.GetCurrentFrame().EditingVoxels,
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);
                                    }
                                    //voxelObject.GetCurrentFrame().EditingVoxels = null;
                                    //voxelObject.UpdateAllChunks();


                                }
                                PicaVoxelPoint brushPos = new PicaVoxelPoint(voxelObject.GetVoxelPosition(r.GetPoint(d)));



                                if (propogateAllFrames)
                                {
                                    foreach (Frame f in voxelObject.Frames)
                                    {
                                        //f.EditingVoxels =
                                        //    new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                                        Helper.CopyVoxelsInBox(ref f.Voxels, ref f.EditingVoxels,
                                            new PicaVoxelBox(previousBrushPos.X - voxelObject.XChunkSize, previousBrushPos.Y - voxelObject.YChunkSize, previousBrushPos.Z - voxelObject.ZChunkSize, previousBrushPos.X + EditorPersistence.BrushSize.X + voxelObject.XChunkSize, previousBrushPos.Y + EditorPersistence.BrushSize.Y + voxelObject.YChunkSize, previousBrushPos.Z + EditorPersistence.BrushSize.Z + voxelObject.ZChunkSize),
                                            new PicaVoxelBox(previousBrushPos.X - voxelObject.XChunkSize, previousBrushPos.Y - voxelObject.YChunkSize, previousBrushPos.Z - voxelObject.ZChunkSize, previousBrushPos.X + EditorPersistence.BrushSize.X + voxelObject.XChunkSize, previousBrushPos.Y + EditorPersistence.BrushSize.Y + voxelObject.YChunkSize, previousBrushPos.Z + EditorPersistence.BrushSize.Z + voxelObject.ZChunkSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);


                                    }
                                }
                                else
                                {
                                    //voxelObject.GetCurrentFrame().EditingVoxels =
                                    //    new Voxel[voxelObject.XSize * voxelObject.YSize * voxelObject.ZSize];
                                    //Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                                    //    ref voxelObject.GetCurrentFrame().EditingVoxels,
                                    //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    false);
                                    Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().Voxels,
                                        ref voxelObject.GetCurrentFrame().EditingVoxels,
                                            new PicaVoxelBox(previousBrushPos.X - voxelObject.XChunkSize, previousBrushPos.Y - voxelObject.YChunkSize, previousBrushPos.Z - voxelObject.ZChunkSize, previousBrushPos.X + EditorPersistence.BrushSize.X + voxelObject.XChunkSize, previousBrushPos.Y + EditorPersistence.BrushSize.Y + voxelObject.YChunkSize, previousBrushPos.Z + EditorPersistence.BrushSize.Z + voxelObject.ZChunkSize),
                                            new PicaVoxelBox(previousBrushPos.X - voxelObject.XChunkSize, previousBrushPos.Y - voxelObject.YChunkSize, previousBrushPos.Z - voxelObject.ZChunkSize, previousBrushPos.X + EditorPersistence.BrushSize.X + voxelObject.XChunkSize, previousBrushPos.Y + EditorPersistence.BrushSize.Y + voxelObject.YChunkSize, previousBrushPos.Z + EditorPersistence.BrushSize.Z + voxelObject.ZChunkSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);

                                }

                                SetChunksInBoxDirty(new PicaVoxelBox(previousBrushPos.X - voxelObject.XChunkSize,
                                    previousBrushPos.Y - voxelObject.YChunkSize, previousBrushPos.Z - voxelObject.ZChunkSize,
                                    previousBrushPos.X + EditorPersistence.BrushSize.X + voxelObject.XChunkSize,
                                    previousBrushPos.Y + EditorPersistence.BrushSize.Y + voxelObject.YChunkSize,
                                    previousBrushPos.Z + EditorPersistence.BrushSize.Z + voxelObject.ZChunkSize));

                                int bx = brushPos.X -
                                         (EditorPersistence.BrushAnchorX == AnchorX.Center
                                             ? EditorPersistence.BrushSize.X / 2
                                             : EditorPersistence.BrushAnchorX == AnchorX.Right
                                                 ? EditorPersistence.BrushSize.X - 1
                                                 : 0);
                                int by = brushPos.Y -
                                         (EditorPersistence.BrushAnchorY == AnchorY.Center
                                             ? EditorPersistence.BrushSize.Y / 2
                                             : EditorPersistence.BrushAnchorY == AnchorY.Top
                                                 ? EditorPersistence.BrushSize.Y - 1
                                                 : 0);
                                int bz = brushPos.Z -
                                         (EditorPersistence.BrushAnchorZ == AnchorZ.Center
                                             ? EditorPersistence.BrushSize.Z / 2
                                             : EditorPersistence.BrushAnchorZ == AnchorZ.Back
                                                 ? EditorPersistence.BrushSize.Z - 1
                                                 : 0);

                                previousBrushPos = new PicaVoxelPoint(bx, by, bz);



                                for (int x = 0; x < EditorPersistence.BrushSize.X; x++)
                                {
                                    for (int y = 0; y < EditorPersistence.BrushSize.Y; y++)
                                    {
                                        for (int z = 0; z < EditorPersistence.BrushSize.Z; z++)
                                        {
                                            if (bx < 0 || by < 0 || bz < 0 || bx >= voxelObject.XSize ||
                                                by >= voxelObject.YSize || bz >= voxelObject.ZSize)
                                            {
                                                bz++;
                                                continue;
                                            }

                                            if (
                                                EditorPersistence.Brush[
                                                    x +
                                                    (EditorPersistence.BrushSize.X) *
                                                    (y + (EditorPersistence.BrushSize.Y) * z)].Active)
                                            {
                                                if (propogateAllFrames)
                                                {
                                                    foreach (Frame f in voxelObject.Frames)
                                                    {
                                                        if (EditorPersistence.CursorMode == EditorCursorMode.BrushPaint &&
                                                            !f.Voxels[bx + voxelObject.XSize * (by + voxelObject.YSize * bz)
                                                                ].Active) continue;
                                                        f.SetVoxelAtArrayPosition(bx, by, bz, new Voxel()
                                                        {
                                                            Color =
                                                                EditorPersistence.Brush[
                                                                    x +
                                                                    (EditorPersistence.BrushSize.X) *
                                                                    (y + (EditorPersistence.BrushSize.Y) * z)].Color,
                                                            Value =
                                                                EditorPersistence.Brush[
                                                                    x +
                                                                    (EditorPersistence.BrushSize.X) *
                                                                    (y + (EditorPersistence.BrushSize.Y) * z)].Value,
                                                            State =
                                                                (EditorPersistence.CursorMode !=
                                                                 EditorCursorMode.BrushSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                        });

                                                    }

                                                }
                                                else
                                                {
                                                    if (EditorPersistence.CursorMode == EditorCursorMode.BrushPaint &&
                                                        !voxelObject.GetCurrentFrame().Voxels[
                                                            bx + voxelObject.XSize * (by + voxelObject.YSize * bz)].Active)
                                                        continue;
                                                    voxelObject.SetVoxelAtArrayPosition(bx, by, bz, new Voxel()
                                                    {
                                                        Color =
                                                            EditorPersistence.Brush[
                                                                x +
                                                                (EditorPersistence.BrushSize.X) *
                                                                (y + (EditorPersistence.BrushSize.Y) * z)].Color,
                                                        Value =
                                                            EditorPersistence.Brush[
                                                                x +
                                                                (EditorPersistence.BrushSize.X) *
                                                                (y + (EditorPersistence.BrushSize.Y) * z)].Value,
                                                        State =
                                                                (EditorPersistence.CursorMode !=
                                                                 EditorCursorMode.BrushSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                    });

                                                }

                                            }

                                            bz++;
                                        }
                                        bz = brushPos.Z -
                                             (EditorPersistence.BrushAnchorZ == AnchorZ.Center
                                                 ? EditorPersistence.BrushSize.Z / 2
                                                 : EditorPersistence.BrushAnchorZ == AnchorZ.Back
                                                     ? EditorPersistence.BrushSize.Z - 1
                                                     : 0);
                                        by++;
                                    }
                                    by = brushPos.Y -
                                         (EditorPersistence.BrushAnchorY == AnchorY.Center
                                             ? EditorPersistence.BrushSize.Y / 2
                                             : EditorPersistence.BrushAnchorY == AnchorY.Top
                                                 ? EditorPersistence.BrushSize.Y - 1
                                                 : 0);
                                    bx++;
                                }

                                if (propogateAllFrames)
                                    foreach (Frame f in voxelObject.Frames) f.UpdateChunks(true);
                                else voxelObject.UpdateChunks(true);
                            }
                        }


                        // Mouse Up (Brush mode)
                        if (validBrushPos && e.type == EventType.MouseUp && e.button == 0)// &&
                                                                                          // voxelObject.GetCurrentFrame().EditingVoxels != null)
                        {
                            if ((EditorPersistence.CursorMode == EditorCursorMode.BrushAdd ||
                                 EditorPersistence.CursorMode == EditorCursorMode.BrushPaint ||
                                 EditorPersistence.CursorMode == EditorCursorMode.BrushSubtract))
                            {
                                voxelObject.GetCurrentFrame().EditingVoxels = null;

                                if (propogateAllFrames)
                                {
                                    List<Object> undoObjects = new List<Object>();

                                    foreach (Frame frame in voxelObject.Frames)
                                    {
                                        undoObjects.Add(frame);
                                    }
                                    undoObjects.Add(voxelObject);
                                    Undo.RecordObjects(undoObjects.ToArray(), "Voxel Editing");
                                    //foreach (Frame f in voxelObject.Frames)
                                    //{
                                    //    ////Helper.CopyVoxelsInBox(ref f.EditingVoxels,
                                    //    ////    ref f.Voxels,
                                    //    ////    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    ////    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    ////    false);
                                    //    //Helper.CopyVoxelsInBox(ref f.EditingVoxels,
                                    //    //    ref f.Voxels,
                                    //    //    new PicaVoxelBox(previousBrushPos.X , previousBrushPos.Y , previousBrushPos.Y , previousBrushPos.X + EditorPersistence.BrushSize.X, previousBrushPos.Y + EditorPersistence.BrushSize.Y, previousBrushPos.Z + EditorPersistence.BrushSize.Z),
                                    //    //    new PicaVoxelBox(previousBrushPos.X, previousBrushPos.Y, previousBrushPos.Y, previousBrushPos.X + EditorPersistence.BrushSize.X , previousBrushPos.Y + EditorPersistence.BrushSize.Y, previousBrushPos.Z + EditorPersistence.BrushSize.Z),
                                    //    //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    //    false);
                                    //}
                                }
                                else
                                {
                                    Undo.RecordObjects(new UnityEngine.Object[]
                                    {
                                        voxelObject.GetCurrentFrame(),
                                        voxelObject,
                                    }, "Voxel Editing");
                                    //Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().EditingVoxels,
                                    //    ref voxelObject.GetCurrentFrame().Voxels,
                                    //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //    false);
                                    //Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().EditingVoxels,
                                    //    ref voxelObject.GetCurrentFrame().Voxels,
                                    //       new PicaVoxelBox(previousBrushPos.X, previousBrushPos.Y, previousBrushPos.Y, previousBrushPos.X + EditorPersistence.BrushSize.X, previousBrushPos.Y + EditorPersistence.BrushSize.Y, previousBrushPos.Z + EditorPersistence.BrushSize.Z),
                                    //        new PicaVoxelBox(previousBrushPos.X, previousBrushPos.Y, previousBrushPos.Y, previousBrushPos.X + EditorPersistence.BrushSize.X, previousBrushPos.Y + EditorPersistence.BrushSize.Y, previousBrushPos.Z + EditorPersistence.BrushSize.Z),
                                    //       new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //       new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                    //       false);
                                }

                                PicaVoxelPoint brushPos = new PicaVoxelPoint(voxelObject.GetVoxelPosition(r.GetPoint(d)));
                                int bx = brushPos.X -
                                         (EditorPersistence.BrushAnchorX == AnchorX.Center
                                             ? EditorPersistence.BrushSize.X / 2
                                             : EditorPersistence.BrushAnchorX == AnchorX.Right
                                                 ? EditorPersistence.BrushSize.X - 1
                                                 : 0);
                                int by = brushPos.Y -
                                         (EditorPersistence.BrushAnchorY == AnchorY.Center
                                             ? EditorPersistence.BrushSize.Y / 2
                                             : EditorPersistence.BrushAnchorY == AnchorY.Top
                                                 ? EditorPersistence.BrushSize.Y - 1
                                                 : 0);
                                int bz = brushPos.Z -
                                         (EditorPersistence.BrushAnchorZ == AnchorZ.Center
                                             ? EditorPersistence.BrushSize.Z / 2
                                             : EditorPersistence.BrushAnchorZ == AnchorZ.Back
                                                 ? EditorPersistence.BrushSize.Z - 1
                                                 : 0);

                                previousBrushPos = new PicaVoxelPoint(bx, by, bz);



                                for (int x = 0; x < EditorPersistence.BrushSize.X; x++)
                                {
                                    for (int y = 0; y < EditorPersistence.BrushSize.Y; y++)
                                    {
                                        for (int z = 0; z < EditorPersistence.BrushSize.Z; z++)
                                        {
                                            if (bx < 0 || by < 0 || bz < 0 || bx >= voxelObject.XSize ||
                                                by >= voxelObject.YSize || bz >= voxelObject.ZSize)
                                            {
                                                bz++;
                                                continue;
                                            }

                                            if (
                                                EditorPersistence.Brush[
                                                    x +
                                                    (EditorPersistence.BrushSize.X) *
                                                    (y + (EditorPersistence.BrushSize.Y) * z)].Active)
                                            {
                                                if (propogateAllFrames)
                                                {
                                                    foreach (Frame f in voxelObject.Frames)
                                                    {
                                                        if (EditorPersistence.CursorMode == EditorCursorMode.BrushPaint &&
                                                            !f.Voxels[bx + voxelObject.XSize * (by + voxelObject.YSize * bz)
                                                                ].Active) continue;
                                                        f.SetVoxelAtArrayPosition(bx, by, bz, new Voxel()
                                                        {
                                                            Color =
                                                                EditorPersistence.Brush[
                                                                    x +
                                                                    (EditorPersistence.BrushSize.X) *
                                                                    (y + (EditorPersistence.BrushSize.Y) * z)].Color,
                                                            Value =
                                                                EditorPersistence.Brush[
                                                                    x +
                                                                    (EditorPersistence.BrushSize.X) *
                                                                    (y + (EditorPersistence.BrushSize.Y) * z)].Value,
                                                            State =
                                                                (EditorPersistence.CursorMode !=
                                                                 EditorCursorMode.BrushSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                        });

                                                    }

                                                }
                                                else
                                                {
                                                    if (EditorPersistence.CursorMode == EditorCursorMode.BrushPaint &&
                                                        !voxelObject.GetCurrentFrame().Voxels[
                                                            bx + voxelObject.XSize * (by + voxelObject.YSize * bz)].Active)
                                                        continue;
                                                    voxelObject.SetVoxelAtArrayPosition(bx, by, bz, new Voxel()
                                                    {
                                                        Color =
                                                            EditorPersistence.Brush[
                                                                x +
                                                                (EditorPersistence.BrushSize.X) *
                                                                (y + (EditorPersistence.BrushSize.Y) * z)].Color,
                                                        Value =
                                                            EditorPersistence.Brush[
                                                                x +
                                                                (EditorPersistence.BrushSize.X) *
                                                                (y + (EditorPersistence.BrushSize.Y) * z)].Value,
                                                        State =
                                                                (EditorPersistence.CursorMode !=
                                                                 EditorCursorMode.BrushSubtract) ? VoxelState.Active : VoxelState.Inactive
                                                    });

                                                }

                                            }

                                            bz++;
                                        }
                                        bz = brushPos.Z -
                                             (EditorPersistence.BrushAnchorZ == AnchorZ.Center
                                                 ? EditorPersistence.BrushSize.Z / 2
                                                 : EditorPersistence.BrushAnchorZ == AnchorZ.Back
                                                     ? EditorPersistence.BrushSize.Z - 1
                                                     : 0);
                                        by++;
                                    }
                                    by = brushPos.Y -
                                         (EditorPersistence.BrushAnchorY == AnchorY.Center
                                             ? EditorPersistence.BrushSize.Y / 2
                                             : EditorPersistence.BrushAnchorY == AnchorY.Top
                                                 ? EditorPersistence.BrushSize.Y - 1
                                                 : 0);
                                    bx++;
                                }

                                if (propogateAllFrames)
                                    foreach (Frame f in voxelObject.Frames) f.UpdateChunks(true);
                                else voxelObject.UpdateChunks(true);






                                currentBox = null;
                                boxStart = null;
                                voxelObject.SaveForSerialize();
                            }
                        }


                        break;
                    }

                }

                //if (!validBrushPos && (EditorPersistence.CursorMode == EditorCursorMode.BrushAdd ||
                //                       EditorPersistence.CursorMode == EditorCursorMode.BrushPaint ||
                //                       EditorPersistence.CursorMode == EditorCursorMode.BrushSubtract))
                //{
                //    if (propogateAllFrames)
                //        foreach (Frame f in voxelObject.Frames) f.EditingVoxels = null;
                //    else voxelObject.GetCurrentFrame().EditingVoxels = null;
                //}

                // Mouse Up (non-brush)
                if (e.type == EventType.MouseUp && e.button == 0 && voxelObject.GetCurrentFrame().EditingVoxels != null)
                {
                    if ((EditorPersistence.CursorMode != EditorCursorMode.BrushAdd &&
                         EditorPersistence.CursorMode != EditorCursorMode.BrushPaint &&
                         EditorPersistence.CursorMode != EditorCursorMode.BrushSubtract))
                    {
                        if (propogateAllFrames)
                        {
                            List<Object> undoObjects = new List<Object>();

                            foreach (Frame frame in voxelObject.Frames)
                            {
                                undoObjects.Add(frame);
                            }
                            undoObjects.Add(voxelObject);
                            Undo.RecordObjects(undoObjects.ToArray(), "Voxel Editing");

                            foreach (Frame f in voxelObject.Frames)
                            {
                                //Helper.CopyVoxelsInBox(ref f.EditingVoxels,
                                //    ref f.Voxels,
                                //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                //    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), false);
                                Helper.CopyVoxelsInBox(ref f.EditingVoxels,
                                            ref f.Voxels, changedVoxelExtents, changedVoxelExtents,
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);
                                f.EditingVoxels = null;
                            }
                        }
                        else
                        {
                            Undo.RecordObjects(new UnityEngine.Object[]
                            {
                                voxelObject.GetCurrentFrame(),
                                voxelObject,
                            }, "Voxel Editing");
                            //Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().EditingVoxels,
                            //    ref voxelObject.GetCurrentFrame().Voxels, new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), false);
                            Helper.CopyVoxelsInBox(ref voxelObject.GetCurrentFrame().EditingVoxels,
                                            ref voxelObject.GetCurrentFrame().Voxels, changedVoxelExtents, changedVoxelExtents,
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                            false);
                            voxelObject.GetCurrentFrame().EditingVoxels = null;
                            //voxelObject.SaveForSerialize();
                        }


                        currentBox = null;
                        boxStart = null;

                        voxelObject.SaveForSerialize();

                    }
                }
            }

            if (EditorPersistence.CursorMode == EditorCursorMode.Select)
            {
                Gizmos.matrix = voxelObject.GetCurrentFrame().transform.Find("Chunks").localToWorldMatrix;
                Handles.color = Color.red;

                int i = 0;
                Vector3 newPos = Vector3.zero;

                bool changedBox = false;

                for (int x = EditorPersistence.SelectBox.BottomLeftFront.X;
                    x <= EditorPersistence.SelectBox.TopRightBack.X + 1;
                    x += EditorPersistence.SelectBox.TopRightBack.X + 1 - EditorPersistence.SelectBox.BottomLeftFront.X)
                {
                    for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y;
                        y <= EditorPersistence.SelectBox.TopRightBack.Y + 1;
                        y +=
                            EditorPersistence.SelectBox.TopRightBack.Y + 1 -
                            EditorPersistence.SelectBox.BottomLeftFront.Y)
                    {
                        for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z;
                            z <= EditorPersistence.SelectBox.TopRightBack.Z + 1;
                            z +=
                                EditorPersistence.SelectBox.TopRightBack.Z + 1 -
                                EditorPersistence.SelectBox.BottomLeftFront.Z)
                        {
                            Vector3 handlePos =
                                voxelObject.transform.TransformPoint((new Vector3(x, y, z) * voxelObject.VoxelSize) -
                                                                     voxelObject.Pivot);

                            EditorHandles.DragHandleResult dhResult;
                            newPos = EditorHandles.DragHandle(handlePos, HandleUtility.GetHandleSize(handlePos) * 0.25f, Handles.SphereHandleCap, Color.red, out dhResult);

                            if (dhResult == EditorHandles.DragHandleResult.LMBDrag)
                            {
                                Voxel? handleVox = voxelObject.GetVoxelAtWorldPosition(newPos);
                                if (handleVox.HasValue)
                                {
                                    if (i == 0)
                                    {
                                        EditorPersistence.SelectBox =
                                            new PicaVoxelBox(
                                                new PicaVoxelPoint((int)voxelObject.GetVoxelPosition(newPos).x,
                                                    (int)voxelObject.GetVoxelPosition(newPos).y,
                                                    (int)voxelObject.GetVoxelPosition(newPos).z),
                                                new PicaVoxelPoint(EditorPersistence.SelectBox.TopRightBack.X,
                                                    EditorPersistence.SelectBox.TopRightBack.Y,
                                                    EditorPersistence.SelectBox.TopRightBack.Z));
                                        changedBox = true;
                                    }
                                    if (i == 1)
                                    {
                                        EditorPersistence.SelectBox =
                                            new PicaVoxelBox(
                                                new PicaVoxelPoint((int)voxelObject.GetVoxelPosition(newPos).x,
                                                    (int)voxelObject.GetVoxelPosition(newPos).y,
                                                    EditorPersistence.SelectBox.BottomLeftFront.Z),
                                                new PicaVoxelPoint(EditorPersistence.SelectBox.TopRightBack.X,
                                                    EditorPersistence.SelectBox.TopRightBack.Y,
                                                    (int)voxelObject.GetVoxelPosition(newPos).z));

                                        changedBox = true;
                                    }
                                    if (i == 2)
                                    {
                                        EditorPersistence.SelectBox =
                                            new PicaVoxelBox(
                                                new PicaVoxelPoint((int)voxelObject.GetVoxelPosition(newPos).x,
                                                    EditorPersistence.SelectBox.BottomLeftFront.Y,
                                                    (int)voxelObject.GetVoxelPosition(newPos).z),
                                                new PicaVoxelPoint(EditorPersistence.SelectBox.TopRightBack.X,
                                                    (int)voxelObject.GetVoxelPosition(newPos).y,
                                                    EditorPersistence.SelectBox.TopRightBack.Z));

                                        changedBox = true;
                                    }
                                    if (i == 3)
                                    {
                                        EditorPersistence.SelectBox =
                                           new PicaVoxelBox(
                                               new PicaVoxelPoint((int)voxelObject.GetVoxelPosition(newPos).x,
                                                   EditorPersistence.SelectBox.BottomLeftFront.Y,
                                                   EditorPersistence.SelectBox.BottomLeftFront.Z),
                                               new PicaVoxelPoint(EditorPersistence.SelectBox.TopRightBack.X,
                                                   (int)voxelObject.GetVoxelPosition(newPos).y,
                                                   (int)voxelObject.GetVoxelPosition(newPos).z));

                                        changedBox = true;
                                    }

                                    if (i == 4)
                                    {
                                        EditorPersistence.SelectBox =
                                           new PicaVoxelBox(
                                               new PicaVoxelPoint(EditorPersistence.SelectBox.BottomLeftFront.X,
                                                    (int)voxelObject.GetVoxelPosition(newPos).y,
                                                   (int)voxelObject.GetVoxelPosition(newPos).z),
                                               new PicaVoxelPoint(
                                                   (int)voxelObject.GetVoxelPosition(newPos).x,
                                                   EditorPersistence.SelectBox.TopRightBack.Y,
                                                   EditorPersistence.SelectBox.TopRightBack.Z));

                                        changedBox = true;
                                    }
                                    if (i == 5)
                                    {
                                        EditorPersistence.SelectBox = new PicaVoxelBox(
                                               new PicaVoxelPoint(EditorPersistence.SelectBox.BottomLeftFront.X,
                                                    (int)voxelObject.GetVoxelPosition(newPos).y,
                                                   EditorPersistence.SelectBox.BottomLeftFront.Z),
                                               new PicaVoxelPoint(
                                                   (int)voxelObject.GetVoxelPosition(newPos).x,
                                                   EditorPersistence.SelectBox.TopRightBack.Y,
                                                   (int)voxelObject.GetVoxelPosition(newPos).z));

                                        changedBox = true;
                                    }
                                    if (i == 6)
                                    {
                                        EditorPersistence.SelectBox = new PicaVoxelBox(
                                              new PicaVoxelPoint(EditorPersistence.SelectBox.BottomLeftFront.X,
                                                    EditorPersistence.SelectBox.BottomLeftFront.Y,
                                                    (int)voxelObject.GetVoxelPosition(newPos).z),
                                              new PicaVoxelPoint(
                                                  (int)voxelObject.GetVoxelPosition(newPos).x,
                                                  (int)voxelObject.GetVoxelPosition(newPos).y,
                                                  EditorPersistence.SelectBox.TopRightBack.Z));

                                        changedBox = true;
                                    }
                                    if (i == 7)
                                    {
                                        EditorPersistence.SelectBox = new PicaVoxelBox(
                                              new PicaVoxelPoint(EditorPersistence.SelectBox.BottomLeftFront.X,
                                                    EditorPersistence.SelectBox.BottomLeftFront.Y,
                                                   EditorPersistence.SelectBox.BottomLeftFront.Z),
                                              new PicaVoxelPoint(
                                                  (int)voxelObject.GetVoxelPosition(newPos).x,
                                                  (int)voxelObject.GetVoxelPosition(newPos).y,
                                                   (int)voxelObject.GetVoxelPosition(newPos).z));

                                        changedBox = true;
                                    }
                                }


                            }

                            i++;
                            if (changedBox) break;
                        }
                        if (changedBox) break;
                    }
                    if (changedBox) break;
                }


                Vector3[] corners =
                {
                    voxelObject.transform.TransformPoint((EditorPersistence.SelectBox.BottomLeftFront.ToVector3()*
                                                          voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(
                        (new Vector3(EditorPersistence.SelectBox.BottomLeftFront.X,
                            EditorPersistence.SelectBox.BottomLeftFront.Y,
                            EditorPersistence.SelectBox.TopRightBack.Z + 1)*voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(
                        (new Vector3(EditorPersistence.SelectBox.TopRightBack.X + 1,
                            EditorPersistence.SelectBox.BottomLeftFront.Y,
                            EditorPersistence.SelectBox.TopRightBack.Z + 1)*voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(
                        (new Vector3(EditorPersistence.SelectBox.TopRightBack.X + 1,
                            EditorPersistence.SelectBox.BottomLeftFront.Y, EditorPersistence.SelectBox.BottomLeftFront.Z)*
                         voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(
                        (new Vector3(EditorPersistence.SelectBox.BottomLeftFront.X,
                            EditorPersistence.SelectBox.TopRightBack.Y + 1,
                            EditorPersistence.SelectBox.BottomLeftFront.Z)*voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(
                        (new Vector3(EditorPersistence.SelectBox.BottomLeftFront.X,
                            EditorPersistence.SelectBox.TopRightBack.Y + 1,
                            EditorPersistence.SelectBox.TopRightBack.Z + 1)*voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(((EditorPersistence.SelectBox.TopRightBack.ToVector3() +
                                                           Vector3.one)*voxelSize) - voxelObject.Pivot),
                    voxelObject.transform.TransformPoint(
                        (new Vector3(EditorPersistence.SelectBox.TopRightBack.X + 1,
                            EditorPersistence.SelectBox.TopRightBack.Y + 1,
                            EditorPersistence.SelectBox.BottomLeftFront.Z)*voxelSize) - voxelObject.Pivot)
                };


                Handles.DrawSolidRectangleWithOutline(new[] { corners[0], corners[1], corners[2], corners[3] },
                    Color.white * 0.1f, Color.black);
                Handles.DrawSolidRectangleWithOutline(new[] { corners[0], corners[4], corners[7], corners[3] },
                    Color.white * 0.1f, Color.black);
                Handles.DrawSolidRectangleWithOutline(new[] { corners[1], corners[5], corners[4], corners[0] },
                    Color.white * 0.1f, Color.black);
                Handles.DrawSolidRectangleWithOutline(new[] { corners[2], corners[6], corners[5], corners[1] },
                    Color.white * 0.1f, Color.black);
                Handles.DrawSolidRectangleWithOutline(new[] { corners[3], corners[7], corners[6], corners[2] },
                    Color.white * 0.1f, Color.black);
                Handles.DrawSolidRectangleWithOutline(new[] { corners[4], corners[5], corners[6], corners[7] },
                    Color.white * 0.1f, Color.black);



            }


        }

        private void SetChunksInBoxDirty(PicaVoxelBox picaVoxelBox)
        {

            for (int x = picaVoxelBox.BottomLeftFront.X; x <= picaVoxelBox.TopRightBack.X; x += voxelObject.XChunkSize)
                for (int y = picaVoxelBox.BottomLeftFront.Y; y <= picaVoxelBox.TopRightBack.Y; y += voxelObject.YChunkSize)
                    for (int z = picaVoxelBox.BottomLeftFront.Z; z <= picaVoxelBox.TopRightBack.Z; z += voxelObject.ZChunkSize)
                        if (propogateAllFrames)
                            foreach (Frame f in voxelObject.Frames) f.SetChunkAtVoxelPositionDirty(x, y, z);
                        else
                            voxelObject.GetCurrentFrame().SetChunkAtVoxelPositionDirty(x, y, z);
        }




        private void RegisterUndo()
        {
            if (propogateAllFrames)
            {
                List<Object> undoObjects = new List<Object>();

                foreach (Frame frame in voxelObject.Frames)
                {
                    undoObjects.Add(frame);
                }
                undoObjects.Add(voxelObject);
                Undo.RecordObjects(undoObjects.ToArray(), "Voxel Editing");
            }
            else
            {
                Undo.RecordObjects(new UnityEngine.Object[]
                                    {
                                        voxelObject.GetCurrentFrame(),
                                        voxelObject,
                                    }, "Voxel Editing");
            }
        }

        private void NudgeSelection(int dx, int dy, int dz)
        {
            PicaVoxelBox dest = new PicaVoxelBox(EditorPersistence.SelectBox.BottomLeftFront.X + dx,
                                                 EditorPersistence.SelectBox.BottomLeftFront.Y + dy,
                                                 EditorPersistence.SelectBox.BottomLeftFront.Z + dz,
                                                 EditorPersistence.SelectBox.TopRightBack.X + dx,
                                                 EditorPersistence.SelectBox.TopRightBack.Y + dy,
                                                 EditorPersistence.SelectBox.TopRightBack.Z + dz);

            if (dest.BottomLeftFront.X < 0 || dest.BottomLeftFront.Y < 0 || dest.BottomLeftFront.Z < 0 ||
                dest.TopRightBack.X >= voxelObject.XSize || dest.TopRightBack.Y >= voxelObject.YSize ||
                dest.TopRightBack.Z >= voxelObject.ZSize) return;

            int destWidth = (dest.TopRightBack.X - dest.BottomLeftFront.X) + 1;
            int destHeight = (dest.TopRightBack.Y - dest.BottomLeftFront.Y) + 1;
            int destDepth = (dest.TopRightBack.Z - dest.BottomLeftFront.Z) + 1;

            Voxel[] tempVox = new Voxel[destWidth * destHeight * destDepth];


            RegisterUndo();
            if (propogateAllFrames)
            {
                foreach (Frame f in voxelObject.Frames)
                {
                    Helper.CopyVoxelsInBox(ref f.Voxels, ref tempVox,
                                        EditorPersistence.SelectBox,
                                        new PicaVoxelBox(0, 0, 0, destWidth - 1, destHeight - 1, destDepth - 1),
                                        new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                                        new PicaVoxelPoint(destWidth, destHeight, destDepth), false);
                    f.EditingVoxels = null;
                    for (int x = EditorPersistence.SelectBox.BottomLeftFront.X; x <= EditorPersistence.SelectBox.TopRightBack.X; x++)
                        for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y; y <= EditorPersistence.SelectBox.TopRightBack.Y; y++)
                            for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z; z <= EditorPersistence.SelectBox.TopRightBack.Z; z++)
                                f.Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel()
                                {
                                    State = VoxelState.Inactive,
                                    Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                    Value = EditorPersistence.SelectedValue
                                };
                    Helper.CopyVoxelsInBox(ref tempVox, ref f.Voxels,
                        new PicaVoxelBox(0, 0, 0, destWidth - 1, destHeight - 1, destDepth - 1),
                        dest,
                        new PicaVoxelPoint(destWidth, destHeight, destDepth),
                        new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), false);

                    f.SaveForSerialize();
                    f.UpdateAllChunks();
                }
                EditorPersistence.SelectBox = dest;
            }
            else
            {
                Helper.CopyVoxelsInBox(ref voxelObject.Frames[voxelObject.CurrentFrame].Voxels, ref tempVox,
                    EditorPersistence.SelectBox,
                    new PicaVoxelBox(0, 0, 0, destWidth - 1, destHeight - 1, destDepth - 1),
                    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize),
                    new PicaVoxelPoint(destWidth, destHeight, destDepth), false);
                voxelObject.Frames[voxelObject.CurrentFrame].EditingVoxels = null;
                for (int x = EditorPersistence.SelectBox.BottomLeftFront.X; x <= EditorPersistence.SelectBox.TopRightBack.X; x++)
                    for (int y = EditorPersistence.SelectBox.BottomLeftFront.Y; y <= EditorPersistence.SelectBox.TopRightBack.Y; y++)
                        for (int z = EditorPersistence.SelectBox.BottomLeftFront.Z; z <= EditorPersistence.SelectBox.TopRightBack.Z; z++)
                            voxelObject.Frames[voxelObject.CurrentFrame].Voxels[x + voxelObject.XSize * (y + voxelObject.YSize * z)] = new Voxel()
                            {
                                State = VoxelState.Inactive,
                                Color = voxelObject.PaletteColors[EditorPersistence.SelectedColor],
                                Value = EditorPersistence.SelectedValue
                            };
                Helper.CopyVoxelsInBox(ref tempVox, ref voxelObject.Frames[voxelObject.CurrentFrame].Voxels,
                    new PicaVoxelBox(0, 0, 0, destWidth - 1, destHeight - 1, destDepth - 1),
                    dest,
                    new PicaVoxelPoint(destWidth, destHeight, destDepth),
                    new PicaVoxelPoint(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize), false);
                EditorPersistence.SelectBox = dest;
                voxelObject.Frames[voxelObject.CurrentFrame].SaveForSerialize();
                voxelObject.Frames[voxelObject.CurrentFrame].UpdateAllChunks();
            }
        }

        private void RotateBrush(RotateAxis axis)
        {
            switch (axis)
            {
                case RotateAxis.X:
                    Helper.RotateVoxelArrayX(ref EditorPersistence.Brush, EditorPersistence.BrushSize);
                    int tempY = EditorPersistence.BrushSize.Y;
                    EditorPersistence.BrushSize.Y = EditorPersistence.BrushSize.Z;
                    EditorPersistence.BrushSize.Z = tempY;
                    break;
                case RotateAxis.Y:
                    Helper.RotateVoxelArrayY(ref EditorPersistence.Brush, EditorPersistence.BrushSize);
                    int tempZ = EditorPersistence.BrushSize.Z;
                    EditorPersistence.BrushSize.Z = EditorPersistence.BrushSize.X;
                    EditorPersistence.BrushSize.X = tempZ;
                    break;
                case RotateAxis.Z:
                    Helper.RotateVoxelArrayZ(ref EditorPersistence.Brush, EditorPersistence.BrushSize);
                    int tempX = EditorPersistence.BrushSize.X;
                    EditorPersistence.BrushSize.X = EditorPersistence.BrushSize.Y;
                    EditorPersistence.BrushSize.Y = tempX;
                    break;
            }
        }

        private static bool HighlightButton(Texture2D texture, bool on, string tooltip)
        {
            if (on)
            {
                GUI.contentColor = (Color.white);
                GUI.backgroundColor = Color.black;
            }
            else
            {
                GUI.contentColor = Color.white;
                GUI.backgroundColor = (EditorGUIUtility.isProSkin ? Color.white : Color.grey);
            }
            bool pressed = GUILayout.Button(new GUIContent(texture, tooltip),
                new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 2, 2, 0)
                }, GUILayout.Width(32), GUILayout.Height(32));

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;

            return pressed;
        }

        private static bool PaletteButton(Color col, bool on)
        {
            GUI.contentColor = col;
            if (on) GUI.backgroundColor = Color.black;
            else GUI.backgroundColor = (EditorGUIUtility.isProSkin ? Color.white : Color.grey);

            bool pressed = false;

            try
            {
                pressed = GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_Palette"]),
                    new GUIStyle(GUI.skin.button)
                    {
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    }, GUILayout.Width(20), GUILayout.Height(20));
            }
            catch (Exception)
            {
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;

            return pressed;
        }
    }
#endif
}