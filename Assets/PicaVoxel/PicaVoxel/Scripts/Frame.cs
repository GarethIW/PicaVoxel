/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// /////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Collections;
using System.IO.Compression;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace PicaVoxel
{
    /// <summary>
    /// A frame of animation
    /// </summary>
    /// This is the real heart of PicaVoxel - one frame of a parent Volume. This is where the voxel array is kept for a frame, and Chunks are created
    /// below the Frame in the heirarchy
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class Frame : MonoBehaviour, ISerializationCallbackReceiver
    {
        public GameObject ChunkPrefab;

        public Volume ParentVolume;

        [NonSerialized]
        public Voxel[] Voxels;
        [NonSerialized]
        public Voxel[] EditingVoxels;

        public int XSize = 32;
        public int YSize = 32;
        public int ZSize = 32;

#if UNITY_EDITOR
        public string AssetGuid;
#endif

        [SerializeField]
        [HideInInspector]
        private byte[] bserializedVoxels;

        private Chunk[,,] chunks;
        private List<PicaVoxelPoint> chunksToUpdate = new List<PicaVoxelPoint>();

        private Quaternion lastRotation;
        private Vector3 lastPosition;
        private Matrix4x4 transformMatrix;

        private void Start()
        {
            //Debug.Log("Frame start");
            if (!Application.isPlaying)
            {
                GetChunkReferences();
                if (chunks == null && (ParentVolume != null && !ParentVolume.RuntimeOnlyMesh))
                    CreateChunks();
            }
        }

        private void Awake()
        {
            //Debug.Log("Frame awake");
            if (transform.parent != null) ParentVolume = transform.parent.GetComponent<Volume>();
            if (ParentVolume != null)
            {
                if (ParentVolume.RuntimeOnlyMesh)// || !Application.isPlaying)
                    UpdateAllChunks();
            }
            transformMatrix = transform.worldToLocalMatrix;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (chunks != null && chunks[0, 0, 0] != null)
                ParentVolume.ChunkLayer = chunks[0, 0, 0].gameObject.layer;

            if (!Application.isPlaying)
                return;
#endif
            if (chunksToUpdate.Count > 0) UpdateChunks(false);

            // We have to keep track of the volume's world-local matrix for when we do any voxel position calculations
            // But worldToLocalMatrix is expensive, so let's check to see if the transform has changed before we do the math
            if (transform.rotation != lastRotation || transform.position != lastPosition)
            {
                transformMatrix = transform.worldToLocalMatrix;

                lastPosition = transform.position;
                lastRotation = transform.rotation;
            }
        }


        /// <summary>
        /// Returns a voxel contained in this frame, at a given world position
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <returns>A voxel if position is within this volume, otherwise null</returns>
        public Voxel? GetVoxelAtWorldPosition(Vector3 pos)
        {

            Vector3 localPos = transformMatrix.MultiplyPoint3x4(pos); //transform.InverseTransformPoint(pos);

            if (!IsLocalPositionInBounds(localPos)) return null;

            int testX = (int)(localPos.x / ParentVolume.VoxelSize);
            int testY = (int)(localPos.y / ParentVolume.VoxelSize);
            int testZ = (int)(localPos.z / ParentVolume.VoxelSize);
            if (testX < 0 || testY < 0 || testZ < 0 || testX >= XSize || testY >= YSize || testZ >= ZSize) return null;

            return Voxels[testX + XSize * (testY + YSize * testZ)];
        }

        /// <summary>
        /// Returns a voxel contained in this frame, at a given array position
        /// </summary>
        /// <param name="x">X array position</param>
        /// <param name="y">Y array position</param>
        /// <param name="z">Z array position</param>
        /// <returns>A voxel if position is within the array, otherwise null</returns>
        public Voxel? GetVoxelAtArrayPosition(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= XSize || y >= YSize || z >= ZSize) return null;

            return Voxels[x + XSize * (y + YSize * z)];
        }

        /// <summary>
        /// Attempts to set a voxel within this frame, at a given world position, to the supplied voxel value
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <param name="vox">The new voxel to set to</param>
        public Vector3 SetVoxelAtWorldPosition(Vector3 pos, Voxel vox)
        {
            Vector3 localPos = transform.InverseTransformPoint(pos);
            Vector3 arrayPos = new Vector3((int)(localPos.x / ParentVolume.VoxelSize),
                (int)(localPos.y / ParentVolume.VoxelSize),
                (int)(localPos.z / ParentVolume.VoxelSize));

            SetVoxelAtArrayPosition((int)arrayPos.x, (int)arrayPos.y, (int)arrayPos.z, vox);

            return arrayPos;
        }

        /// <summary>
        /// Attempts to set a voxel's state within this frame, at a given world position, to the supplied value
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <param name="state">The new voxel state to set to</param>
        public Vector3 SetVoxelStateAtWorldPosition(Vector3 pos, VoxelState state)
        {
            Vector3 localPos = transform.InverseTransformPoint(pos);
            Vector3 arrayPos = new Vector3((int)(localPos.x / ParentVolume.VoxelSize),
                (int)(localPos.y / ParentVolume.VoxelSize),
                (int)(localPos.z / ParentVolume.VoxelSize));

            SetVoxelStateAtArrayPosition((int)arrayPos.x, (int)arrayPos.y, (int)arrayPos.z, state);

            return arrayPos;
        }

        /// <summary>
        /// Attempts to set a voxel within this frame, at a specified array position
        /// </summary>
        /// <param name="pos">A PicaVoxelPoint location within the 3D array of voxels</param>
        /// <param name="vox">The new voxel to set to</param>
        public void SetVoxelAtArrayPosition(PicaVoxelPoint pos, Voxel vox)
        {
            SetVoxelAtArrayPosition(pos.X, pos.Y, pos.Z, vox);
        }

        /// <summary>
        /// Attempts to set a voxel within this frame, at a specified x,y,z array position
        /// </summary>
        /// <param name="x">X array position</param>
        /// <param name="y">Y array position</param>
        /// <param name="z">Z array position</param>
        /// <param name="vox">The new voxel to set to</param>
        public void SetVoxelAtArrayPosition(int x, int y, int z, Voxel vox)
        {
            if (x < 0 || y < 0 || z < 0 || x >= XSize || y >= YSize || z >= ZSize) return;

            bool updateVox = false;
            int index = x + XSize * (y + YSize * z);

#if UNITY_EDITOR
            if (EditingVoxels != null)
            {
                EditingVoxels[index] = vox;
                updateVox = true;
            }
            else
            {
#endif
                if (vox.Active != Voxels[index].Active) updateVox = true;
                if (Voxels[index].Active && (vox.Color.r != Voxels[index].Color.r || vox.Color.g != Voxels[index].Color.g || vox.Color.b != Voxels[index].Color.b || vox.Value != Voxels[index].Value))
                    updateVox = true;

                Voxels[index] = vox;
#if UNITY_EDITOR
            }
#endif

            if (!updateVox) return;
            if (chunks == null) GetChunkReferences();
            AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);

            // If we're at the edge of a chunk, we should update the next voxel as well
            if (x % ParentVolume.XChunkSize == 0 && (x - 1) >= 0) AddChunkToUpdateList((x - 1) / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (x % ParentVolume.XChunkSize == ParentVolume.XChunkSize - 1 && (x + 1) < XSize) AddChunkToUpdateList((x + 1) / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (y % ParentVolume.YChunkSize == 0 && (y - 1) >= 0) AddChunkToUpdateList(x / ParentVolume.XChunkSize, (y - 1) / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (y % ParentVolume.YChunkSize == ParentVolume.YChunkSize - 1 && (y + 1) < YSize) AddChunkToUpdateList(x / ParentVolume.XChunkSize, (y + 1) / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (z % ParentVolume.ZChunkSize == 0 && (z - 1) >= 0) AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, (z - 1) / ParentVolume.ZChunkSize);
            if (z % ParentVolume.ZChunkSize == ParentVolume.ZChunkSize - 1 && (z + 1) < ZSize) AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, (z + 1) / ParentVolume.ZChunkSize);

        }

        /// <summary>
        /// Attempts to set a voxel's state within this frame, at a specified array position
        /// </summary>
        /// <param name="pos">A PicaVoxelPoint location within the 3D array of voxels</param>
        /// <param name="state">The new state to set to</param>
        public void SetVoxelStateAtArrayPosition(PicaVoxelPoint pos, VoxelState state)
        {
            SetVoxelStateAtArrayPosition(pos.X, pos.Y, pos.Z, state);
        }

        /// <summary>
        /// Attempts to set a voxel's state within this frame, at a specified x,y,z array position
        /// </summary>
        /// <param name="x">X array position</param>
        /// <param name="y">Y array position</param>
        /// <param name="z">Z array position</param>
        /// <param name="state">The new voxel state to set to</param>
        public void SetVoxelStateAtArrayPosition(int x, int y, int z, VoxelState state)
        {
            if (x < 0 || y < 0 || z < 0 || x >= XSize || y >= YSize || z >= ZSize) return;

            bool updateVox = false;
            int index = x + XSize * (y + YSize * z);


            if (state != Voxels[index].State) updateVox = true;

            Voxels[index].State = state;

            if (!updateVox) return;
            if (chunks == null) GetChunkReferences();
            AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);

            // If we're at the edge of a chunk, we should update the next voxel as well
            if (x % ParentVolume.XChunkSize == 0 && (x - 1) >= 0) AddChunkToUpdateList((x - 1) / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (x % ParentVolume.XChunkSize == ParentVolume.XChunkSize - 1 && (x + 1) < XSize) AddChunkToUpdateList((x + 1) / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (y % ParentVolume.YChunkSize == 0 && (y - 1) >= 0) AddChunkToUpdateList(x / ParentVolume.XChunkSize, (y - 1) / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (y % ParentVolume.YChunkSize == ParentVolume.YChunkSize - 1 && (y + 1) < YSize) AddChunkToUpdateList(x / ParentVolume.XChunkSize, (y + 1) / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
            if (z % ParentVolume.ZChunkSize == 0 && (z - 1) >= 0) AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, (z - 1) / ParentVolume.ZChunkSize);
            if (z % ParentVolume.ZChunkSize == ParentVolume.ZChunkSize - 1 && (z + 1) < ZSize) AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, (z + 1) / ParentVolume.ZChunkSize);

        }

        /// <summary>
        /// Returns the local position of a voxel within this frame, at a specified world position
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <returns>The local voxel position</returns>
        public Vector3 GetVoxelPosition(Vector3 scenePos)
        {
            Vector3 localPos = transform.InverseTransformPoint(scenePos);

            return new Vector3((int)(localPos.x / ParentVolume.VoxelSize), (int)(localPos.y / ParentVolume.VoxelSize),
                (int)(localPos.z / ParentVolume.VoxelSize));
        }

        /// <summary>
        /// Returns the array position of a voxel within this frame, at a specified world position
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <returns>The array position of the voxel</returns>
        public PicaVoxelPoint GetVoxelArrayPosition(Vector3 scenePos)
        {
            Vector3 localPos = transform.InverseTransformPoint(scenePos);

            return new PicaVoxelPoint((int)(localPos.x / ParentVolume.VoxelSize),
                (int)(localPos.y / ParentVolume.VoxelSize), (int)(localPos.z / ParentVolume.VoxelSize));
        }

        /// <summary>
        /// Returns the world position of a voxel given its array positions
        /// </summary>
        /// <param name="x">The X position of the voxel in the array</param>
        /// <param name="y">The Y position of the voxel in the array</param>
        /// <param name="z">The Z position of the voxel in the array</param>
        /// <returns>The world position of the center of the voxel</returns>
        public Vector3 GetVoxelWorldPosition(int x, int y, int z)
        {
            Vector3 localPos = (new Vector3(x * ParentVolume.VoxelSize, y * ParentVolume.VoxelSize, z * ParentVolume.VoxelSize) + (Vector3.one * (ParentVolume.VoxelSize / 2f)));
            return transform.TransformPoint(localPos);
        }

        /// <summary>
        /// Update the pivot position. Use this after setting Pivot.
        /// </summary>
        public void UpdatePivot()
        {
            transform.Find("Chunks").localPosition = -ParentVolume.Pivot;
        }

        public void UpdateTransformMatrix()
        {
            transformMatrix = transform.worldToLocalMatrix;
        }

        /// <summary>
        /// Generates a 32x32x32 frame, filled with white voxels of value 128
        /// </summary>
        public void GenerateBasic(FillMode fillMode)
        {
            ParentVolume = ParentVolume.GetComponent<Volume>();
            Voxels = new Voxel[XSize * YSize * ZSize];
            for (int x = 0; x < XSize; x++)
                for (int y = 0; y < YSize; y++)
                    for (int z = 0; z < ZSize; z++)
                    {
                        int index = x + XSize * (y + YSize * z);
                        if (fillMode == FillMode.AllVoxels || (fillMode == FillMode.BaseOnly && y == 0))
                            Voxels[index].State = VoxelState.Active;
                        Voxels[index].Value = 128;
                        Voxels[index].Color = ParentVolume.PaletteColors[0];
                    }

            SaveForSerialize();
        }

        /// <summary>
        /// Initialise this frame with an array of empty voxels
        /// </summary>
        public void GenerateNewFrame()
        {
            ParentVolume = transform.parent.GetComponent<Volume>();
            Voxels = new Voxel[XSize * YSize * ZSize];
            for (int i = 0; i < Voxels.Length; i++) Voxels[i].Value = 128;
            SaveForSerialize();
        }

        /// <summary>
        /// Initialise this frame and copy the voxels from a source frame
        /// </summary>
        /// <param name="sourceFrame"></param>
        public void GenerateNewFrame(Frame sourceFrame)
        {
            ParentVolume = transform.parent.GetComponent<Volume>();
            Voxels = new Voxel[XSize * YSize * ZSize];
            Helper.CopyVoxelsInBox(ref sourceFrame.Voxels, ref Voxels, new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            CreateChunks();
            SaveForSerialize();
        }

        /// <summary>
        /// Generate the colliders for all of the chunks
        /// </summary>
        public void GenerateMeshColliders()
        {
            if (ParentVolume.CollisionMode == CollisionMode.None)
            {
                DestroyMeshColliders();
                return;
            }


            for (int i = transform.Find("Chunks").childCount - 1; i >= 0; i--)
            {
                var chunk = transform.Find("Chunks").GetChild(i);

                if (chunk.GetComponent<MeshCollider>() == null)
                    chunk.gameObject.AddComponent(typeof(MeshCollider));

                //Mesh m = chunk.GetComponent<MeshFilter>().sharedMesh;
                //m.RecalculateBounds();
                //chunk.GetComponent<MeshCollider>().sharedMesh = m;
                chunk.GetComponent<MeshCollider>().convex = (ParentVolume.CollisionMode ==
                                                             CollisionMode.MeshColliderConvex);
                chunk.GetComponent<MeshCollider>().sharedMaterial = ParentVolume.PhysicMaterial;
                chunk.GetComponent<MeshCollider>().isTrigger = ParentVolume.CollisionTrigger;
            }

            UpdateAllChunks();

        }

        /// <summary>
        /// Destroy the mesh colliders on all of the chunks
        /// </summary>
        public void DestroyMeshColliders()
        {
            //Debug.Log("Frame DestroyMeshColliders");
            for (int i = transform.Find("Chunks").childCount - 1; i >= 0; i--)
            {
                var chunk = transform.Find("Chunks").GetChild(i);

                if (chunk.GetComponent<MeshCollider>() != null)
                    DestroyImmediate(chunk.GetComponent<MeshCollider>());
            }
        }


        /// <summary>
        /// Update only the chunks which have changed voxels
        /// </summary>
        /// <param name="immediate">If true, don't use threading to perform this update</param>
        public void UpdateChunks(bool immediate)
        {
            if (chunksToUpdate.Count == 0) return;

            if (ParentVolume == null) return;
            if (ParentVolume.RuntimeOnlyMesh && !Application.isPlaying) return;

            if (Voxels == null) return;

            if (chunks == null) GetChunkReferences();

            while (chunksToUpdate.Count > 0)
            {
                int x = chunksToUpdate[0].X;
                int y = chunksToUpdate[0].Y;
                int z = chunksToUpdate[0].Z;
                chunks[x, y, z].IsUpdated = false;
#if UNITY_EDITOR
                chunks[x, y, z].GenerateMesh(EditingVoxels == null ? Voxels : EditingVoxels.Length > 0 ? EditingVoxels : Voxels,
                    ParentVolume.VoxelSize, ParentVolume.OverlapAmount,
                    x * ParentVolume.XChunkSize, y * ParentVolume.YChunkSize, z * ParentVolume.ZChunkSize,
                    (XSize - (x * ParentVolume.XChunkSize) < ParentVolume.XChunkSize
                        ? XSize - (x * ParentVolume.XChunkSize)
                        : ParentVolume.XChunkSize),
                    (YSize - (y * ParentVolume.YChunkSize) < ParentVolume.YChunkSize
                        ? YSize - (y * ParentVolume.YChunkSize)
                        : ParentVolume.YChunkSize),
                    (ZSize - (z * ParentVolume.ZChunkSize) < ParentVolume.ZChunkSize
                        ? ZSize - (z * ParentVolume.ZChunkSize)
                        : ParentVolume.ZChunkSize), XSize - 1, YSize - 1, ZSize - 1,
                    ParentVolume.SelfShadingIntensity, ParentVolume.MeshingMode, ParentVolume.MeshColliderMeshingMode, immediate,
                    ParentVolume.PaintMode);
#else
                chunks[x, y, z].GenerateMesh(EditingVoxels==null?Voxels:EditingVoxels.Length>0?EditingVoxels:Voxels,
                    ParentVolume.VoxelSize, ParentVolume.OverlapAmount,
                        x* ParentVolume.XChunkSize, y* ParentVolume.YChunkSize, z* ParentVolume.ZChunkSize,
                    (XSize - (x* ParentVolume.XChunkSize) < ParentVolume.XChunkSize
                        ? XSize - (x* ParentVolume.XChunkSize)
                        : ParentVolume.XChunkSize),
                    (YSize - (y* ParentVolume.YChunkSize) < ParentVolume.YChunkSize
                        ? YSize - (y* ParentVolume.YChunkSize)
                        : ParentVolume.YChunkSize),
                    (ZSize - (z* ParentVolume.ZChunkSize) < ParentVolume.ZChunkSize
                        ? ZSize - (z* ParentVolume.ZChunkSize)
                        : ParentVolume.ZChunkSize), XSize-1, YSize-1, ZSize-1,
                    ParentVolume.SelfShadingIntensity,ParentVolume.MeshingMode,ParentVolume.MeshColliderMeshingMode, immediate);
#endif
                chunksToUpdate.RemoveAt(0);
            }
        }

        /// <summary>
        /// Immediately update all chunks
        /// </summary>
        public void UpdateAllChunks()
        {
            // Debug.Log("UpdateAllChunks " + ParentVolume.transform.name);
            if (ParentVolume == null) return;
            if (ParentVolume.RuntimeOnlyMesh && !Application.isPlaying)
            {
                DestroyMeshColliders();

                Transform chunkContainer = transform.Find("Chunks");
                for (int i = chunkContainer.childCount - 1; i >= 0; i--)
                    if (chunkContainer.GetChild(i).GetComponent<Chunk>() != null)
                        DestroyImmediate(chunkContainer.GetChild(i).gameObject);
                return;
            }
            if (Voxels == null) return;

            if (chunks == null)
            {
                GetChunkReferences();
            }



            int progress = 0;

            for (int x = 0; x < (int)Mathf.Ceil((float)XSize / ParentVolume.XChunkSize); x++)
            {
                for (int y = 0; y < (int)Mathf.Ceil((float)YSize / ParentVolume.YChunkSize); y++)
                {
                    for (int z = 0; z < (int)Mathf.Ceil((float)ZSize / ParentVolume.ZChunkSize); z++)
                    {
                        
                        
#if UNITY_EDITOR
                        chunks[x, y, z].GenerateMesh(
                            EditingVoxels == null ? Voxels : EditingVoxels.Length > 0 ? EditingVoxels : Voxels,
                            ParentVolume.VoxelSize, ParentVolume.OverlapAmount,
                            x * ParentVolume.XChunkSize, y * ParentVolume.YChunkSize, z * ParentVolume.ZChunkSize,
                            (XSize - (x * ParentVolume.XChunkSize) < ParentVolume.XChunkSize
                                ? XSize - (x * ParentVolume.XChunkSize)
                                : ParentVolume.XChunkSize),
                            (YSize - (y * ParentVolume.YChunkSize) < ParentVolume.YChunkSize
                                ? YSize - (y * ParentVolume.YChunkSize)
                                : ParentVolume.YChunkSize),
                            (ZSize - (z * ParentVolume.ZChunkSize) < ParentVolume.ZChunkSize
                                ? ZSize - (z * ParentVolume.ZChunkSize)
                                : ParentVolume.ZChunkSize), XSize - 1, YSize - 1, ZSize - 1,
                            ParentVolume.SelfShadingIntensity, ParentVolume.MeshingMode,
                            ParentVolume.MeshColliderMeshingMode, true,
                            ParentVolume.PaintMode);
#else
                         ParentVolume.GenerateMeshColliderSeparately = EditingVoxels != null;
                         chunks[x, y, z].GenerateMesh(EditingVoxels==null?Voxels:EditingVoxels.Length>0?EditingVoxels:Voxels,
                                ParentVolume.VoxelSize, ParentVolume.OverlapAmount,
                                 x* ParentVolume.XChunkSize, y* ParentVolume.YChunkSize, z* ParentVolume.ZChunkSize,
                            (XSize - (x* ParentVolume.XChunkSize) < ParentVolume.XChunkSize
                                ? XSize - (x* ParentVolume.XChunkSize)
                                : ParentVolume.XChunkSize),
                            (YSize - (y* ParentVolume.YChunkSize) < ParentVolume.YChunkSize
                                ? YSize - (y* ParentVolume.YChunkSize)
                                : ParentVolume.YChunkSize),
                            (ZSize - (z* ParentVolume.ZChunkSize) < ParentVolume.ZChunkSize
                                ? ZSize - (z* ParentVolume.ZChunkSize)
                                : ParentVolume.ZChunkSize), XSize-1, YSize-1, ZSize-1,
                                ParentVolume.SelfShadingIntensity,ParentVolume.MeshingMode,ParentVolume.MeshColliderMeshingMode, true);
#endif

                        progress++;
                    }
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.DisplayProgressBar("PicaVoxel", "Updating Chunks", (1f / (float)(XSize * YSize * ZSize)) * (float)(progress));
#endif
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.ClearProgressBar();
            
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) { EditorSceneManager.MarkSceneDirty(prefabStage.scene); }
#endif
            transformMatrix = transform.worldToLocalMatrix;
        }

        /// <summary>
        /// Updates all chunks next frame (threaded)
        /// </summary>
        public void UpdateAllChunksNextFrame()
        {
            if (chunks == null) GetChunkReferences();

            for (int x = 0; x < chunks.GetLength(0); x++)
                for (int y = 0; y < chunks.GetLength(1); y++)
                    for (int z = 0; z < chunks.GetLength(2); z++)
                        AddChunkToUpdateList(x, y, z);
        }

        /// <summary>
        /// Re-create all chunks
        /// </summary>
        public void CreateChunks()
        {
            //Debug.Log("CreateChunks " + ParentVolume.transform.name);
            if (ParentVolume.RuntimeOnlyMesh && !Application.isPlaying) return;
            if (Voxels == null) return;

            DestroyMeshColliders();

            Transform chunkContainer = transform.Find("Chunks");

            for (int i = chunkContainer.childCount - 1; i >= 0; i--)
                if (chunkContainer.GetChild(i).GetComponent<Chunk>() != null)
                    DestroyImmediate(chunkContainer.GetChild(i).gameObject);

            chunks =
                new Chunk[(int)Mathf.Ceil((float)XSize / ParentVolume.XChunkSize),
                    (int)Mathf.Ceil((float)YSize / ParentVolume.YChunkSize), (int)Mathf.Ceil((float)ZSize / ParentVolume.YChunkSize)];

            int x = 0;
            int y = 0;
            int z = 0;
            int chunkX = 0;
            int chunkY = 0;
            int chunkZ = 0;
            while (x < XSize)
            {
                while (y < YSize)
                {
                    while (z < ZSize)
                    {
                        var newChunk =
                            Instantiate(ChunkPrefab, new Vector3(x, y, z) * ParentVolume.VoxelSize, Quaternion.identity)
                                as GameObject;
                        newChunk.name = "Chunk (" + chunkX + "," + chunkY + "," + chunkZ + ")";
                        newChunk.layer = ParentVolume.ChunkLayer;
                        newChunk.transform.parent = chunkContainer;
                        newChunk.transform.localPosition = new Vector3(x, y, z) * ParentVolume.VoxelSize;
                        newChunk.transform.rotation = transform.rotation;
                        newChunk.transform.localScale = transform.localScale;
                        newChunk.GetComponent<MeshRenderer>().shadowCastingMode = ParentVolume.CastShadows;
                        newChunk.GetComponent<MeshRenderer>().receiveShadows = ParentVolume.ReceiveShadows;
			newChunk.isStatic = ParentVolume.gameObject.isStatic;
                        chunks[chunkX, chunkY, chunkZ] = newChunk.GetComponent<Chunk>();

                        #if UNITY_EDITOR
                        if (PrefabUtility.IsPartOfAnyPrefab(ParentVolume))
                        {
                            PrefabUtility.ApplyAddedGameObject(newChunk, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(ParentVolume), InteractionMode.AutomatedAction);
                        }
                        #endif
                        
                        z += ParentVolume.ZChunkSize;
                        chunkZ++;
                    }

                    z = 0;
                    chunkZ = 0;
                    y += ParentVolume.YChunkSize;
                    chunkY++;
                }

                y = 0;
                chunkY = 0;
                x += ParentVolume.XChunkSize;
                chunkX++;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.DisplayProgressBar("PicaVoxel", "Creating Chunks", (1f / (float)(XSize * YSize * ZSize)) * (float)(x * y * z));
#endif
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.ClearProgressBar();
#endif

            GenerateMeshColliders();
            UpdateAllChunks();
            SaveChunkMeshes(false);
        }

        public void SaveChunkMeshes(bool forceNew)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(ParentVolume.AssetGuid)) ParentVolume.AssetGuid = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(AssetGuid) || forceNew) AssetGuid = Guid.NewGuid().ToString();

            string path = Path.Combine(Helper.GetMeshStorePath(), ParentVolume.AssetGuid.ToString());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (forceNew)
            {
                UpdateAllChunks();
                //foreach (Chunk c in chunks) c.SaveMeshes();
            }

            GetChunkReferences();
            int progress = 0;
            if (chunks != null)
            {
                foreach (Chunk c in chunks)
                {
                    if (!Application.isPlaying)
                        UnityEditor.EditorUtility.DisplayProgressBar("PicaVoxel", "Saving Chunks",
                            (1f / (float)(chunks.Length)) * (float)(progress));
                    c.SaveMeshes();
                    progress++;
                }

                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.ClearProgressBar();
            }
#endif
        }

        public void SetChunkAtVoxelPositionDirty(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= ParentVolume.XSize || y >= ParentVolume.YSize || z >= ParentVolume.ZSize)
                return;

            PicaVoxelPoint chunkPos = new PicaVoxelPoint((int)(x / ParentVolume.XChunkSize), (int)(y / ParentVolume.YChunkSize),
                    (int)(z / ParentVolume.ZChunkSize));

            if (chunks[chunkPos.X, chunkPos.Y, chunkPos.Z] != null) AddChunkToUpdateList(chunkPos.X, chunkPos.Y, chunkPos.Z);

        }

        private void AddChunkToUpdateList(int x, int y, int z)
        {
            var pvp = new PicaVoxelPoint(x, y, z);
            if (chunksToUpdate.Contains(pvp)) return;

            chunks[x, y, z].IsUpdated = true;
            chunksToUpdate.Add(pvp);
        }

        private void GetChunkReferences()
        {
            if (ParentVolume.RuntimeOnlyMesh && !Application.isPlaying) return;
            //Debug.Log("Frame GetChunkReferences");
            chunks =
                new Chunk[(int)Mathf.Ceil((float)XSize / ParentVolume.XChunkSize),
                    (int)Mathf.Ceil((float)YSize / ParentVolume.YChunkSize), (int)Mathf.Ceil((float)ZSize / ParentVolume.ZChunkSize)];

            for (int x = 0; x < (int)Mathf.Ceil((float)XSize / ParentVolume.XChunkSize); x++)
                for (int y = 0; y < (int)Mathf.Ceil((float)YSize / ParentVolume.YChunkSize); y++)
                    for (int z = 0; z < (int)Mathf.Ceil((float)ZSize / ParentVolume.ZChunkSize); z++)
                    {
                        Transform c = transform.Find("Chunks/Chunk (" + x + "," + y + "," + z + ")");
                        if (c == null)
                        {
                            //Debug.LogWarning("Couldn't get chunk refs - should probably work out why!");
                            CreateChunks();
                            return;
                        }
                        chunks[x, y, z] = c.GetComponent<Chunk>();
                    }
        }

        public bool IsLocalPositionInBounds(Vector3 pos)
        {
            Vector3 size = new Vector3(XSize * ParentVolume.VoxelSize, YSize * ParentVolume.VoxelSize,
                ZSize * ParentVolume.VoxelSize);
            Vector3 v1 = -size;
            Vector3 v2 = size;

            if (pos.x >= v1.x && pos.y >= v1.y && pos.z >= v1.z && pos.x <= v2.x && pos.y <= v2.y && pos.z <= v2.z)
                return true;

            return false;
        }

        /// <summary>
        /// Scroll voxels along X axis
        /// </summary>
        /// <param name="amount">The amount of voxels to scroll by</param>
        /// This shouldn't really be used at runtime
        public void ScrollX(int amount)
        {
            Voxel[] tempVoxels = new Voxel[XSize * YSize * ZSize];

            int reverse = -amount;
            if (amount < 0)
            {
                Helper.CopyVoxelsInBox(ref Voxels, ref tempVoxels, new PicaVoxelBox(0, 0, 0, reverse - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, reverse - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref Voxels, ref Voxels, new PicaVoxelBox(reverse, 0, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1 - reverse - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref tempVoxels, ref Voxels, new PicaVoxelBox(0, 0, 0, reverse - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(XSize - reverse, 0, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            }
            else
            {
                Helper.CopyVoxelsInBox(ref Voxels, ref tempVoxels, new PicaVoxelBox(0, 0, 0, XSize - 1 - amount, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1 - amount, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref Voxels, ref Voxels, new PicaVoxelBox(XSize - amount, 0, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, amount - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref tempVoxels, ref Voxels, new PicaVoxelBox(0, 0, 0, XSize - 1 - amount, YSize - 1, ZSize - 1), new PicaVoxelBox(amount, 0, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            }

            UpdateAllChunks();
            SaveForSerialize();
        }

        /// <summary>
        /// Scroll voxels along Y axis
        /// </summary>
        /// <param name="amount">The amount of voxels to scroll by</param>
        /// This shouldn't really be used at runtime
        public void ScrollY(int amount)
        {
            Voxel[] tempVoxels = new Voxel[XSize * YSize * ZSize];

            int reverse = -amount;
            if (amount < 0)
            {
                Helper.CopyVoxelsInBox(ref Voxels, ref tempVoxels, new PicaVoxelBox(0, 0, 0, XSize - 1, reverse - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, reverse - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref Voxels, ref Voxels, new PicaVoxelBox(0, reverse, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - reverse - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref tempVoxels, ref Voxels, new PicaVoxelBox(0, 0, 0, XSize - 1, reverse - 1, ZSize - 1), new PicaVoxelBox(0, YSize - reverse, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            }
            else
            {
                Helper.CopyVoxelsInBox(ref Voxels, ref tempVoxels, new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1 - amount, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1 - amount, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref Voxels, ref Voxels, new PicaVoxelBox(0, YSize - amount, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, amount - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref tempVoxels, ref Voxels, new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1 - amount, ZSize - 1), new PicaVoxelBox(0, amount, 0, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            }

            UpdateAllChunks();
            SaveForSerialize();
        }

        /// <summary>
        /// Scroll voxels along Z axis
        /// </summary>
        /// <param name="amount">The amount of voxels to scroll by</param>
        /// This shouldn't really be used at runtime
        public void ScrollZ(int amount)
        {
            Voxel[] tempVoxels = new Voxel[XSize * YSize * ZSize];

            int reverse = -amount;
            if (amount < 0)
            {
                Helper.CopyVoxelsInBox(ref Voxels, ref tempVoxels, new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, reverse - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, reverse - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref Voxels, ref Voxels, new PicaVoxelBox(0, 0, reverse, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, ZSize - reverse - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref tempVoxels, ref Voxels, new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, reverse - 1), new PicaVoxelBox(0, 0, ZSize - reverse, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            }
            else
            {
                Helper.CopyVoxelsInBox(ref Voxels, ref tempVoxels, new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, ZSize - 1 - amount), new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, ZSize - 1 - amount), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref Voxels, ref Voxels, new PicaVoxelBox(0, 0, ZSize - amount, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, amount - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
                Helper.CopyVoxelsInBox(ref tempVoxels, ref Voxels, new PicaVoxelBox(0, 0, 0, XSize - 1, YSize - 1, ZSize - 1 - amount), new PicaVoxelBox(0, 0, amount, XSize - 1, YSize - 1, ZSize - 1), new PicaVoxelPoint(XSize, YSize, ZSize), new PicaVoxelPoint(XSize, YSize, ZSize), false);
            }

            UpdateAllChunks();
            SaveForSerialize();
        }

        /// <summary>
        /// Restore all voxels that have been destroyed (Voxel.State = VoxelState.Hidden)
        /// </summary>
        public void Rebuild()
        {
            if (chunks == null) GetChunkReferences();

            for (int x = 0; x < XSize; x++)
                for (int y = 0; y < YSize; y++)
                    for (int z = 0; z < ZSize; z++)
                    {
                        if (Voxels[x + XSize * (y + YSize * z)].State == VoxelState.Hidden)
                        {
                            Voxels[x + XSize * (y + YSize * z)].State = VoxelState.Active;
                        }

                        AddChunkToUpdateList(x / ParentVolume.XChunkSize, y / ParentVolume.YChunkSize, z / ParentVolume.ZChunkSize);
                    }
        }

        /// <summary>
        /// Serialise voxel array to byte array ready for Unity serialisation
        /// </summary>
        public void SaveForSerialize()
        {
            // We don't need to be saving for serialization at runtime
            if (Application.isPlaying) return;

            //Debug.Log("Frame SaveForSerialize");
            if (Voxels == null)
            {
                Debug.LogError("Voxels are null upon saving!");
                return;
            }
            try
            {
                bserializedVoxels = ToCompressedByteArray();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        public byte[] ToCompressedByteArray()
        {
            byte[] returnArray;

            byte[] streambuff = new byte[(XSize * YSize * ZSize) * Voxel.BYTE_SIZE];
            byte[] buffer = new byte[Voxel.BYTE_SIZE];
            int o = 0;
            for (int x = 0; x < XSize; x++)
                for (int y = 0; y < YSize; y++)
                    for (int z = 0; z < ZSize; z++)
                    {
                        buffer = Voxels[x + XSize * (y + YSize * z)].ToBytes();
                        for (int i = 0; i < Voxel.BYTE_SIZE; i++)
                        {
                            streambuff[o] = buffer[i];
                            o++;
                        }
                    }

            using (var ms = new MemoryStream())
            {

                using (var gzs = new GZipStream(ms, CompressionMode.Compress)) // GZipOutputStream(ms))
                {
                    //gzs.SetLevel(1);
                    gzs.Write(streambuff, 0, streambuff.Length);
                }

                returnArray = ms.ToArray();
            }

            return returnArray;

            //return streambuff;
        }


        public void OnBeforeSerialize()
        {

        }

#if UNITY_EDITOR
        public bool HasDeserialized = false;
#endif

        public void OnAfterDeserialize()
        {
            // Debug.Log("Frame OnAfterSerialize");

            try
            {
                if (bserializedVoxels == null) return;
                if (bserializedVoxels.Length == 0) return;

                FromCompressedByteArray(bserializedVoxels);

#if UNITY_EDITOR
                HasDeserialized = true;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
                Debug.LogError("Couldn't deserialise something");

            }
        }

        public void FromCompressedByteArray(byte[] compressed)
        {
            //var streambuff = compressed;
            byte[] streambuff = new byte[XSize * YSize * ZSize * Voxel.BYTE_SIZE];

            using (var ms = new MemoryStream(compressed))
            {

                using (var gzs = new GZipStream(ms, CompressionMode.Decompress))
                {
                    gzs.Read(streambuff, 0, streambuff.Length);
                }
            }


            Voxels = new Voxel[XSize * YSize * ZSize];

            int o = 0;
            byte[] buffer = new byte[Voxel.BYTE_SIZE];
            for (int x = 0; x < XSize; x++)
                for (int y = 0; y < YSize; y++)
                    for (int z = 0; z < ZSize; z++)
                    {
                        for (int i = 0; i < Voxel.BYTE_SIZE; i++) buffer[i] = streambuff[o + i];
                            Voxels[x + XSize * (y + YSize * z)] = new Voxel(buffer);
                            o += Voxel.BYTE_SIZE;
                      
                    }

        }

    }
}