using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PicaVoxel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

namespace PicaVoxel
{
    public partial class VolumeEditor
    {
        static bool scrollFoldout = false;
        static bool chunkFoldout = true;

        public override void OnInspectorGUI()
        {
            var isInStage = (prefabStage != null && prefabStage.IsPartOfPrefabContents(voxelObject.gameObject)); 

            serializedObject.Update();

            if (PrefabUtility.IsPartOfAnyPrefab(voxelObject) && !isInStage)
            {
                EditorGUILayout.HelpBox("Open the prefab scene to edit this PicaVoxel volume.", MessageType.Info);
                return;
            }
            
            GUIStyle foldoutStyle = EditorStyles.foldout;
            foldoutStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.Space();
            EditorUtility.SkinnedLabel("Editor");

            if (!runtimeOnlyMesh.boolValue)
            {
                if (serializedObject.targetObjects.Length < 2)
                {
                    if (GUILayout.Button(voxelObject.IsEnabledForEditing ? "Stop Editing" : "Start Editing"))
                    {
                        if (voxelObject.IsEnabledForEditing)
                            voxelObject.SaveForSerialize();

                        voxelObject.IsEnabledForEditing = !voxelObject.IsEnabledForEditing;
                        voxelObject.PaintMode = EditorPaintMode.Color;

                        voxelObject.UpdateChunks(true);

                        if (isInStage)
                        {
                            EditorSceneManager.MarkSceneDirty(prefabStage.scene); 
                        }    

                        SceneView.RepaintAll();
                    }
                    if (voxelObject.IsEnabledForEditing)
                    {
                        var oldbgcol = GUI.backgroundColor;
                        GUI.backgroundColor = (EditorGUIUtility.isProSkin ? Color.white : Color.grey);
                        GUILayout.BeginHorizontal(new GUIStyle() { stretchWidth = true, alignment = TextAnchor.MiddleCenter });
                        GUILayout.FlexibleSpace();

                        GUILayout.BeginHorizontal(EditorUtility.Buttons["pvButton_AnimBG"], new GUIStyle() { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(260));
                        if (GUILayout.Button(new GUIContent(EditorUtility.Buttons["pvButton_AddFramePrev"], "Add frame before"),
                            new GUIStyle(GUI.skin.button)
                            {
                                padding = new RectOffset(0, 0, 0, 0),
                                margin = new RectOffset(3, 0, 9, 0)
                            }, GUILayout.Width(32), GUILayout.Height(32)))
                        {
                            buttonJustClicked = true;
                            voxelObject.AddFrame(voxelObject.CurrentFrame);
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
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
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                        }

                        GUI.backgroundColor = oldbgcol;
                        GUILayout.EndHorizontal();
                        GUILayout.FlexibleSpace();

                        GUILayout.EndHorizontal();


                        propogateAllFrames = EditorGUILayout.ToggleLeft(" Propagate edits to all frames",
                            propogateAllFrames);

                        EditorGUILayout.BeginHorizontal();
                        drawGrid = EditorGUILayout.ToggleLeft(new GUIContent(" Draw Grid"), drawGrid);
                        if (drawGrid != voxelObject.DrawGrid)
                        {
                            voxelObject.DrawGrid = drawGrid;
                            SceneView.RepaintAll();
                        }
                        //drawMesh = EditorGUILayout.ToggleLeft(new GUIContent(" Draw Wireframe"), drawMesh);
                        //if (drawMesh != voxelObject.DrawMesh)
                        //{
                        //    voxelObject.DrawMesh = drawMesh;
                        //    SceneView.RepaintAll();
                        //}
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            runtimeOnlyMesh.boolValue = EditorGUILayout.ToggleLeft(new GUIContent(" Runtime-Only Mesh"),
                runtimeOnlyMesh.boolValue);
            if (runtimeOnlyMesh.boolValue != voxelObject.RuntimeOnlyMesh)
            {
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume) o).IsEnabledForEditing = false;
                    ((Volume) o).RuntimeOnlyMesh = runtimeOnlyMesh.boolValue;
                    ((Volume) o).CreateChunks();
                    ((Volume) o).UpdateAllChunks();
                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }   
            }

            EditorGUILayout.Space();

            EditorUtility.SkinnedLabel(
                "Size (Volume size: " + voxelObject.XSize + "," + voxelObject.YSize + "," + voxelObject.ZSize +
                " Chunk Size: " + voxelObject.XChunkSize + "," + voxelObject.YChunkSize + "," + voxelObject.ZChunkSize +
                ")");
            float size = EditorGUILayout.FloatField("Voxel Size:", voxelSizeProperty.floatValue);
            if (size != voxelSizeProperty.floatValue && size > 0f)
            {
                voxelSizeProperty.floatValue = size;
                voxelSize = voxelSizeProperty.floatValue;
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume) o).VoxelSize = voxelSize;
                    ((Volume) o).CreateChunks();
                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
            }

            float overlap = EditorGUILayout.FloatField("Face Overlap:", overlapAmountProperty.floatValue);
            if (overlap != overlapAmountProperty.floatValue)
            {
                overlapAmountProperty.floatValue = overlap;
                overlapAmount = overlapAmountProperty.floatValue;
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume) o).OverlapAmount = overlapAmount;
                    ((Volume) o).CreateChunks();
                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
            }

            if (serializedObject.targetObjects.Length < 2 && !voxelObject.IsEnabledForEditing)
            {
                if (GUILayout.Button("Resize"))
                {
                    EditorResizeWindow window =
                        (EditorResizeWindow)
                            EditorWindow.GetWindowWithRect((typeof (EditorResizeWindow)), new Rect(100, 100, 400, 240),
                                true);
                    window.Init(voxelObject);
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Rotate X"))
                {
                    voxelObject.RotateX();
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }
                if (GUILayout.Button("Rotate Y"))
                {
                    voxelObject.RotateY();
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }
                if (GUILayout.Button("Rotate Z"))
                {
                    voxelObject.RotateZ();
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorUtility.SkinnedLabel("Pivot");
            pivotProperty.vector3Value = EditorGUILayout.Vector3Field("", pivotProperty.vector3Value, null);
            if (pivotProperty.vector3Value != voxelObject.Pivot)
            {
                pivot = pivotProperty.vector3Value;
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume) o).Pivot = pivot;
                    ((Volume) o).UpdatePivot();
                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
            }
            if (GUILayout.Button("Set to Center"))
            {
                pivot = (new Vector3(voxelObject.XSize, voxelObject.YSize, voxelObject.ZSize)*voxelObject.VoxelSize)/2f;
                pivotProperty.vector3Value = pivot;
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume) o).Pivot = pivot;
                    ((Volume) o).UpdatePivot();
                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
            }

            EditorGUILayout.Space();
            scrollFoldout = EditorGUILayout.Foldout(scrollFoldout, "Scroll Voxels", foldoutStyle);
            if (scrollFoldout)
            {
                //EditorUtility.SkinnedLabel("Scroll Voxels");
                allFrames = EditorGUILayout.ToggleLeft(" Scroll all frames", allFrames);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("X-1"))
                {
                    voxelObject.ScrollX(-1, allFrames);
                }
                if (GUILayout.Button("X-10"))
                {
                    voxelObject.ScrollX(-10, allFrames);
                }
                if (GUILayout.Button("X+1"))
                {
                    voxelObject.ScrollX(1, allFrames);
                }
                if (GUILayout.Button("X+10"))
                {
                    voxelObject.ScrollX(10, allFrames);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Y-1"))
                {
                    voxelObject.ScrollY(-1, allFrames);
                }
                if (GUILayout.Button("Y-10"))
                {
                    voxelObject.ScrollY(-10, allFrames);
                }
                if (GUILayout.Button("Y+1"))
                {
                    voxelObject.ScrollY(1, allFrames);
                }
                if (GUILayout.Button("Y+10"))
                {
                    voxelObject.ScrollY(10, allFrames);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Z-1"))
                {
                    voxelObject.ScrollZ(-1, allFrames);
                }
                if (GUILayout.Button("Z-10"))
                {
                    voxelObject.ScrollZ(-10, allFrames);
                }
                if (GUILayout.Button("Z+1"))
                {
                    voxelObject.ScrollZ(1, allFrames);
                }
                if (GUILayout.Button("Z+10"))
                {
                    voxelObject.ScrollZ(10, allFrames);
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            chunkFoldout = EditorGUILayout.Foldout(chunkFoldout, "Chunk Generation", foldoutStyle);
            if (chunkFoldout)
            {

                chunkLayer.intValue = EditorGUILayout.LayerField("Chunk Layer: ", chunkLayer.intValue);
                if (chunkLayer.intValue != voxelObject.ChunkLayer)
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).ChunkLayer = chunkLayer.intValue;
                        ((Volume) o).CreateChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }

                EditorUtility.SkinnedLabel("Mesh Collider");
                collisionMode.enumValueIndex =
                    Convert.ToInt16(EditorGUILayout.EnumPopup((CollisionMode) collisionMode.enumValueIndex));
                if (collisionMode.enumValueIndex != Convert.ToInt16(voxelObject.CollisionMode))
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).ChangeCollisionMode((CollisionMode) collisionMode.enumValueIndex);
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }
                if (collisionMode.enumValueIndex > 0)
                {
                    collisionTrigger.boolValue = EditorGUILayout.ToggleLeft(new GUIContent(" Is Trigger"),
                        collisionTrigger.boolValue);
                    if (collisionTrigger.boolValue != voxelObject.CollisionTrigger)
                    {
                        foreach (var o in serializedObject.targetObjects)
                        {
                            ((Volume) o).CollisionTrigger = collisionTrigger.boolValue;
                            ((Volume) o).CreateChunks();
                        }
                        if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                    }

                    physicMaterial.objectReferenceValue = EditorGUILayout.ObjectField("Physic Material: ",
                        physicMaterial.objectReferenceValue,
                        typeof (PhysicMaterial),
                        false);
                    if (physicMaterial.objectReferenceValue != voxelObject.PhysicMaterial)
                    {
                        foreach (var o in serializedObject.targetObjects)
                        {
                            ((Volume) o).PhysicMaterial = (PhysicMaterial) physicMaterial.objectReferenceValue;
                            ((Volume) o).CreateChunks();
                        }
                        if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                    }

                    separateColliderMesh.boolValue =
                        EditorGUILayout.ToggleLeft(
                            new GUIContent(" Generate collider mesh separately (Edit-time only)"),
                            separateColliderMesh.boolValue);
                    if (separateColliderMesh.boolValue != voxelObject.GenerateMeshColliderSeparately)
                    {
                        foreach (var o in serializedObject.targetObjects)
                        {
                            ((Volume) o).GenerateMeshColliderSeparately = separateColliderMesh.boolValue;
                            ((Volume) o).CreateChunks();
                        }
                        if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                    }

                    if (separateColliderMesh.boolValue)
                    {
                        EditorUtility.SkinnedLabel("Collider Meshing Mode");
                        colliderMeshingMode.enumValueIndex =
                            Convert.ToInt16(EditorGUILayout.EnumPopup((MeshingMode) colliderMeshingMode.enumValueIndex));
                        if (colliderMeshingMode.enumValueIndex != Convert.ToInt16(voxelObject.MeshColliderMeshingMode))
                        {
                            foreach (var o in serializedObject.targetObjects)
                            {
                                ((Volume) o).MeshColliderMeshingMode = (MeshingMode) colliderMeshingMode.enumValueIndex;
                                ((Volume) o).CreateChunks();
                            }
                            if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                        }
                    }
                }

                EditorGUILayout.Space();

                EditorUtility.SkinnedLabel("Meshing Mode");
                meshingMode.enumValueIndex =
                    Convert.ToInt16(EditorGUILayout.EnumPopup((MeshingMode) meshingMode.enumValueIndex));
                if (meshingMode.enumValueIndex != Convert.ToInt16(voxelObject.MeshingMode))
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).MeshingMode = (MeshingMode) meshingMode.enumValueIndex;
                        ((Volume) o).CreateChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }

                EditorUtility.SkinnedLabel("Mesh Compression");
                meshCompression.enumValueIndex =
                    Convert.ToInt16(
                        EditorGUILayout.EnumPopup((ModelImporterMeshCompression) meshCompression.enumValueIndex));
                if (meshCompression.enumValueIndex != Convert.ToInt16(voxelObject.MeshCompression))
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).MeshCompression = (ModelImporterMeshCompression) meshCompression.enumValueIndex;
                        ((Volume) o).CreateChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }

                selfShadeInt.floatValue = EditorGUILayout.Slider("Self-Shading Intensity", selfShadeInt.floatValue, 0, 1);
                if (selfShadeInt.floatValue != voxelObject.SelfShadingIntensity)
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).SelfShadingIntensity = selfShadeInt.floatValue;
                        ((Volume) o).UpdateAllChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }

                material.objectReferenceValue = EditorGUILayout.ObjectField("Material: ", material.objectReferenceValue,
                    typeof (Material),
                    false);
                if (material.objectReferenceValue != voxelObject.Material)
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).Material = (Material) material.objectReferenceValue;
                        ((Volume) o).CreateChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }

                castShadows.enumValueIndex =
                    Convert.ToInt16(EditorGUILayout.EnumPopup("Cast Shadows",
                        (ShadowCastingMode) castShadows.enumValueIndex));
                if (castShadows.enumValueIndex != Convert.ToInt16(voxelObject.CastShadows))
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).CastShadows = (ShadowCastingMode) castShadows.enumValueIndex;
                        ((Volume) o).CreateChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }

                receiveShadows.boolValue = EditorGUILayout.ToggleLeft(new GUIContent(" Receive Shadows"),
                    receiveShadows.boolValue);
                if (receiveShadows.boolValue != voxelObject.ReceiveShadows)
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).ReceiveShadows = receiveShadows.boolValue;
                        ((Volume) o).CreateChunks();
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
                }
            }




            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Mesh-Only Copy", "Create a copy of this Volume with mesh(es) only")))
            {
                foreach (var target in serializedObject.targetObjects)
                {

                    GameObject bake = ((Volume) Instantiate(target)).gameObject;
                    bake.GetComponent<Volume>().AssetGuid = Guid.NewGuid().ToString();
                    foreach (Frame f in bake.GetComponent<Volume>().Frames)
                        f.AssetGuid = Guid.NewGuid().ToString();
                    bake.GetComponent<Volume>().CreateChunks();
                    bake.tag = "Untagged";
                    bake.name = target.name + " (Copy)";
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

                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Refresh Chunks", "Regenerates all chunk meshes for all frames")))
            {
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume) o).CreateChunks();
                    ((Volume)o).SaveForSerialize();
                }
                if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }    
            }

            if (voxelObject.ImportedFrom != Importer.None && !string.IsNullOrEmpty(voxelObject.ImportedFile) &&
                serializedObject.targetObjects.Length < 2 && !voxelObject.IsEnabledForEditing)
            {
                EditorGUILayout.Space();
                if (
                    GUILayout.Button(
                        new GUIContent(
                            voxelObject.ImportedFrom == Importer.Magica
                                ? "Re-import from MagicaVoxel"
                                : "Re-import from Image",
                            voxelObject.ImportedFrom == Importer.Magica
                                ? "Re-import from original .VOX file"
                                : "Re-import from original image file")))
                {
                    if (UnityEditor.EditorUtility.DisplayDialog("Warning!",
                        "Re-importing will overwrite any changes made since original import. This cannot be undone!",
                        "OK", "Cancel"))
                    {
                        foreach (var o in serializedObject.targetObjects)
                        {
                            switch (voxelObject.ImportedFrom)
                            {
                                case Importer.Magica:
                                    MagicaVoxelImporter.MagicaVoxelImport((Volume) o);
                                    break;
                                case Importer.Image:
                                    ImageImporter.ImageImport((Volume) o);
                                    break;
                            }
                        }
                    }
                    if (isInStage) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }  
                }
                  
            }

            if (serializedObject.targetObjects.Length < 2 && !voxelObject.IsEnabledForEditing)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Export QB"))
                {
                    QubicleExporter.QubicleExport(voxelObject);
                }
                //if (GUILayout.Button("Export VOX"))
                //{

                //}
                GUILayout.EndHorizontal();
            }

            if (serializedObject.targetObjects.Length < 2 && !voxelObject.IsEnabledForEditing)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Export VOX"))
                {
                    MagicaExporter.MagicaExport(voxelObject);
                }
                //if (GUILayout.Button("Export VOX"))
                //{

                //}
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("New Mesh Asset Instance", "Creates a new set of mesh assets for this volume. Use if you have copy+pasted or duplicated the volume and want the mesh to be separate from the original at edit-time.")))
            {
                foreach (var o in serializedObject.targetObjects)
                {
                    ((Volume)o).SaveChunkMeshes(true);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Rebuild Volume", "Reset any voxels that have been destroyed")))
                {
                    foreach (var o in serializedObject.targetObjects)
                    {
                        ((Volume) o).Rebuild();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
