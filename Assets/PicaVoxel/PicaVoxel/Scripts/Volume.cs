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
using System.IO;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace PicaVoxel
{
    /// <summary>
    /// Which type of mesh collider to use when generating meshes
    /// </summary>
    public enum CollisionMode
    {
        None,
        MeshColliderConvex,
        MeshColliderConcave
    }

    public enum MeshingMode
    {
        Greedy,
        Culled,
        Marching
    }

#if UNITY_EDITOR
    /// <summary>
    /// Toggles between Color and Value rendering when generating meshes - editor only
    /// </summary>
    public enum EditorPaintMode
    {
        Color,
        Value
    }

    public enum Importer
    {
        None,
        Magica,
        Image
    }
#endif

    /// <summary>
    /// The main parent script for a PicaVoxel volume
    /// </summary>
    [AddComponentMenu("PicaVoxel/PicaVoxel Volume")]
    [Serializable]
    [ExecuteInEditMode]
    [SelectionBase]
    public class Volume : MonoBehaviour, ISerializationCallbackReceiver
    {
        public GameObject FramePrefab;

        public int XChunkSize = 16;
        public int YChunkSize = 16;
        public int ZChunkSize = 16;

        public int XSize = 32;
        public int YSize = 32;
        public int ZSize = 32;
        public float VoxelSize = 0.1f;
        public float OverlapAmount = 0f;

        public Vector3 Pivot = Vector3.zero;

        public int NumFrames
        {
            get { return Frames.Count; }
        }

        public int CurrentFrame = 0;
        public List<Frame> Frames;

        // Chunk generation settings
        public BoxCollider Hitbox;
        public MeshingMode MeshingMode;
        public MeshingMode MeshColliderMeshingMode;
        public bool GenerateMeshColliderSeparately = false;
        public Material Material;
        public PhysicMaterial PhysicMaterial;
        public bool CollisionTrigger;
        public CollisionMode CollisionMode;
        public float SelfShadingIntensity = 0.2f;
        public ShadowCastingMode CastShadows = ShadowCastingMode.On;
        public bool ReceiveShadows = true;
        public int ChunkLayer; 

        public Color[] PaletteColors = new Color[25];

        [FormerlySerializedAs("RuntimOnlyMesh")]
        public bool RuntimeOnlyMesh = false;

#if UNITY_EDITOR
        public bool IsEnabledForEditing = false;
        public bool DrawGrid = false;
        public bool DrawMesh = false;
        public string AssetGuid;
        public EditorPaintMode PaintMode = EditorPaintMode.Color;
        public ModelImporterMeshCompression MeshCompression = ModelImporterMeshCompression.Off;
        public Importer ImportedFrom;
        public string ImportedFile;
        public Color ImportedCutoutColor;
        [SerializeField] private int thisInstanceId;
#endif

        private Batch destructBatch;

        private void Awake()
        {
            Hitbox = transform.Find("Hitbox").GetComponent<BoxCollider>();
            destructBatch = new Batch(this, XSize*YSize*ZSize);

            int startFrame = CurrentFrame;
            if (Application.isPlaying)
            {
                // Pre-enable all animation frames to avoid mesh generation when frame is changed for the first time
                for (int i = 0; i < NumFrames; i++)
                    NextFrame();
            }
            CurrentFrame = startFrame;

#if UNITY_EDITOR
            // This used to perform a check to see if the object had been duplicated in the editor
            // However it is unreliable at best, so I've removed it until Unity provide a proper way to check for copy+paste/duplicate
            // Instead, there's a button on the Volume inspector to create new mesh asset instances
            if (!Application.isPlaying)//if in the editor
            {
                if (thisInstanceId != GetInstanceID())
                {
                    if (thisInstanceId == 0)
                    {
                        thisInstanceId = GetInstanceID();
                        //Debug.Log("Not Copied");
                    }
                    else
                    {
                        // This used to perform a check to see if the object had been duplicated in the editor
                        // However it is unreliable at best, so I've removed it until Unity provide a proper way to check for copy+paste/duplicate
                        // Instead, there's a button on the Volume inspector to create new mesh asset instances
                        thisInstanceId = GetInstanceID();
                        //if (thisInstanceId < 0)
                        //{
                        //    SaveChunkMeshes(true);
                        //    //Debug.Log("DUPLICATE/COPY");
                        //}
                    }
                }

            }
#endif
        }

        /// <summary>
        /// Returns a voxel contained in this volume's current frame, at a given world position
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <returns>A voxel if position is within this volume, otherwise null</returns>
        public Voxel? GetVoxelAtWorldPosition(Vector3 pos)
        {
            return Frames[CurrentFrame].GetVoxelAtWorldPosition(pos);
        }

        /// <summary>
        /// Returns a voxel contained in this volume's current frame,, at a given array position
        /// </summary>
        /// <param name="x">X array position</param>
        /// <param name="y">Y array position</param>
        /// <param name="z">Z array position</param>
        /// <returns>A voxel if position is within the array, otherwise null</returns>
        public Voxel? GetVoxelAtArrayPosition(int x, int y, int z)
        {
            return Frames[CurrentFrame].GetVoxelAtArrayPosition(x, y, z);
        }

        /// <summary>
        /// Attempts to set a voxel within this volume's current frame, at a given world position, to the supplied voxel value
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <param name="vox">The new voxel to set to</param>
        /// <returns>The array position of the voxel</returns>
        public Vector3 SetVoxelAtWorldPosition(Vector3 pos, Voxel vox)
        {
            return Frames[CurrentFrame].SetVoxelAtWorldPosition(pos, vox);
        }

        /// <summary>
        /// Attempts to set a voxel's state within this volume's current frame, at a given world position, to the supplied value
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <param name="state">The new voxel state to set to</param>
        /// <returns>The array position of the voxel</returns>
        public Vector3 SetVoxelStateAtWorldPosition(Vector3 pos, VoxelState state)
        {
            return Frames[CurrentFrame].SetVoxelStateAtWorldPosition(pos, state);
        }

        /// <summary>
        /// Attempts to set a voxel within this volume's current frame, at a specified array position
        /// </summary>
        /// <param name="pos">A PicaVoxelPoint location within the 3D array of voxels</param>
        /// <param name="vox">The new voxel to set to</param>
        public void SetVoxelAtArrayPosition(PicaVoxelPoint pos, Voxel vox)
        {
            Frames[CurrentFrame].SetVoxelAtArrayPosition(pos, vox);
        }

        /// <summary>
        /// Attempts to set a voxel's state within this volume's current frame, at a specified array position
        /// </summary>
        /// <param name="pos">A PicaVoxelPoint location within the 3D array of voxels</param>
        /// <param name="state">The new state to set to</param>
        public void SetVoxelStateAtArrayPosition(PicaVoxelPoint pos, VoxelState state)
        {
            Frames[CurrentFrame].SetVoxelStateAtArrayPosition(pos, state);
        }

        /// <summary>
        /// Attempts to set a voxel within this volume's current frame, at a specified x,y,z array position
        /// </summary>
        /// <param name="x">X array position</param>
        /// <param name="y">Y array position</param>
        /// <param name="z">Z array position</param>
        /// <param name="vox">The new voxel to set to</param>
        public void SetVoxelAtArrayPosition(int x, int y, int z, Voxel vox)
        {
            Frames[CurrentFrame].SetVoxelAtArrayPosition(x, y, z, vox);
        }

        /// <summary>
        /// Attempts to set a voxel's state within this volume's current frame, at a specified x,y,z array position
        /// </summary>
        /// <param name="x">X array position</param>
        /// <param name="y">Y array position</param>
        /// <param name="z">Z array position</param>
        /// <param name="state">The new state to set to</param>
        public void SetVoxelStateAtArrayPosition(int x, int y, int z, VoxelState state)
        {
            Frames[CurrentFrame].SetVoxelStateAtArrayPosition(x, y, z, state);
        }

        /// <summary>
        /// Returns the local position of a voxel within this volume's current frame, at a specified world position
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <returns>The local voxel position</returns>
        public Vector3 GetVoxelPosition(Vector3 pos)
        {
            return Frames[CurrentFrame].GetVoxelPosition(pos);
        }

        /// <summary>
        /// Returns the array position of a voxel within this volume's current frame, at a specified world position
        /// </summary>
        /// <param name="pos">The world position in the scene</param>
        /// <returns>The array position of the voxel</returns>
        public PicaVoxelPoint GetVoxelArrayPosition(Vector3 pos)
        {
            return Frames[CurrentFrame].GetVoxelArrayPosition(pos);
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
            return Frames[CurrentFrame].GetVoxelWorldPosition(x,y,z);
        }

        /// <summary>
        /// Get the current animation frame
        /// </summary>
        /// <returns>The current animation frame</returns>
        public Frame GetCurrentFrame()
        {
            return Frames[CurrentFrame];
        }

        /// <summary>
        /// Update the pivot position. Use this after setting Pivot.
        /// </summary>
        public void UpdatePivot()
        {
            foreach (Frame frame in Frames)
                frame.transform.localPosition = -Pivot;

            UpdateBoxCollider();
        }

        /// <summary>
        /// Generates a 32x32x32 frame, filled with white voxels of value 128
        /// </summary>
        public void GenerateBasic(FillMode fillMode)
        {
            //Debug.Log("Object GenerateBasic");
            AddFrame(0);
            Frames[0].GenerateBasic(fillMode);
            UpdateBoxCollider();
        }

        /// <summary>
        /// Insert a new animation frame before the supplied frame number
        /// </summary>
        /// <param name="where">The frame number to insert the new frame before</param>
        public void AddFrame(int where)
        {
            //Debug.Log("Object AddFrame");
            var newFrame = Instantiate(FramePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            Frame newPVFrame = newFrame.GetComponent<Frame>();
            newPVFrame.XSize = XSize;
            newPVFrame.YSize = YSize;
            newPVFrame.ZSize = ZSize;
            newFrame.name = "Frame";
            newFrame.layer = gameObject.layer;
            newFrame.transform.parent = transform;
            newFrame.transform.localPosition = -Pivot;
            newFrame.transform.rotation = transform.rotation;
            newFrame.transform.localScale = transform.localScale;
            if (Frames.Count > 0)
                newPVFrame.GenerateNewFrame(Frames[CurrentFrame]);
            else
                newPVFrame.GenerateNewFrame();
            Frames.Insert(where, newPVFrame);
            SetFrame(where);

            UpdateFrameNames();
        }

        /// <summary>
        /// Delete the current frame
        /// </summary>
        public void DeleteFrame()
        {
            if (Frames.Count <= 1) return;

            DestroyImmediate(Frames[CurrentFrame].gameObject);
            Frames.RemoveAt(CurrentFrame);
            if (CurrentFrame >= Frames.Count) CurrentFrame = Frames.Count - 1;
            SetFrame(CurrentFrame);

            UpdateFrameNames();
        }

        public void MoveFrameRight()
        {
            if (CurrentFrame >= NumFrames-1) return;
            Frame tempFrame = Frames[CurrentFrame];
            Frames[CurrentFrame] = Frames[CurrentFrame + 1];
            Frames[CurrentFrame + 1] = tempFrame;
            CurrentFrame++;

            UpdateFrameNames();
        }

        public void MoveFrameLeft()
        {
            if (CurrentFrame <= 0) return;
            Frame tempFrame = Frames[CurrentFrame];
            Frames[CurrentFrame] = Frames[CurrentFrame - 1];
            Frames[CurrentFrame - 1] = tempFrame;
            CurrentFrame--;

            UpdateFrameNames();
        }

        /// <summary>
        /// Move to the next animation frame (wraps around)
        /// </summary>
        public void NextFrame()
        {
            if (NumFrames <= 1) return;

            CurrentFrame++;
            if (CurrentFrame >= NumFrames) CurrentFrame = 0;
            ChangeFrame();
        }


        /// <summary>
        /// Move to the previous animation frame (wraps around)
        /// </summary>
        public void PrevFrame()
        {
            if (NumFrames <= 1) return;

            CurrentFrame--;
            if (CurrentFrame < 0) CurrentFrame = NumFrames - 1;
            ChangeFrame();
        }

        /// <summary>
        /// Sets the current animation frame
        /// </summary>
        /// <param name="frame">The frame number to set to</param>
        /// Will not set the frame if supplied frame is invalid
        public void SetFrame(int frame)
        {
            if (frame < 0 || frame >= NumFrames) return;

            CurrentFrame = frame;
            ChangeFrame();
        }

        private void ChangeFrame()
        {
            foreach (var frame in Frames)
            {
                frame.gameObject.SetActive(false);
            }
            Frames[CurrentFrame].gameObject.SetActive(true);
        }

        private void UpdateFrameNames()
        {
            foreach (Frame f in Frames)
            {
                f.gameObject.name = "Frame " + (Frames.IndexOf(f)+1);
            }
        }

        /// <summary>
        /// Update only the chunks which have changed voxels on the current frame
        /// </summary>
        /// <param name="immediate">If true, don't use threading to perform this update</param>
        public void UpdateChunks(bool immediate)
        {
            //Debug.Log("Object UpdateChunks");
            Frames[CurrentFrame].UpdateChunks(immediate);
        }

        /// <summary>
        /// Immediately update all chunks on the current frame
        /// </summary>
        public void UpdateAllChunks()
        {
            Frames[CurrentFrame].UpdateAllChunks();
        }

        /// <summary>
        /// Updates all chunks on the current animation frame next game frame (threaded)
        /// </summary>
        public void UpdateAllChunksNextFrame()
        {
            Frames[CurrentFrame].UpdateAllChunksNextFrame();
        }

        /// <summary>
        /// Re-create all chunks on all animation frames
        /// </summary>
        public void CreateChunks()
        {
            foreach (Frame frame in Frames) 
                if(frame!=null) frame.CreateChunks();

            UpdateBoxCollider();
        }

        public void SaveChunkMeshes(bool forceNew)
        {
#if UNITY_EDITOR
            if (RuntimeOnlyMesh) return;

            if (string.IsNullOrEmpty(AssetGuid) || forceNew) AssetGuid = Guid.NewGuid().ToString();

            string path = Path.Combine(Helper.GetMeshStorePath(), AssetGuid.ToString());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

#if !UNITY_WEBPLAYER
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo f in di.GetFiles())
                f.Delete();
#endif

            foreach (Frame frame in Frames)
                if (frame != null) frame.SaveChunkMeshes(true);
#endif
        }

        /// <summary>
        /// Deactivates all particles in the current frame, within a supplied radius of a world position
        /// </summary>
        /// <param name="position">The world position of the centre of the explosion</param>
        /// <param name="explosionRadius">The radius of the explosion</param>
        /// <returns>A Batch of voxels that were destroyed by the explosion</returns>
        public Batch Explode(Vector3 position, float explosionRadius, int valueFilter, Exploder.ExplodeValueFilterOperation valueFilterOperation)
        {
            Batch batch = new Batch(this);

            Color tint = Material.GetColor("_Tint");

            Matrix4x4 transformMatrix = transform.worldToLocalMatrix;

            position += (transform.rotation * (Pivot));
            position = transformMatrix.MultiplyPoint3x4(position);

            for (float x = position.x - explosionRadius; x <= position.x + explosionRadius; x += VoxelSize * 0.5f)
                for (float y = position.y - explosionRadius; y <= position.y + explosionRadius; y += VoxelSize * 0.5f)
                    for (float z = position.z - explosionRadius; z <= position.z + explosionRadius; z += VoxelSize * 0.5f)
                    {
                        Vector3 checkPos = new Vector3(x, y, z);
                        if ((checkPos-  position).magnitude <= explosionRadius)
                        {
                            //Vector3 localPos =; //transform.InverseTransformPoint(pos);
                            //if (!Frames[CurrentFrame].IsLocalPositionInBounds(localPos)) continue;

                            int testX = (int)(checkPos.x / VoxelSize);
                            int testY = (int)(checkPos.y / VoxelSize);
                            int testZ = (int)(checkPos.z / VoxelSize);
                            if (testX < 0 || testY < 0 || testZ < 0 || testX >= XSize || testY >= YSize || testZ >= ZSize) continue;

                            if (Frames[CurrentFrame].Voxels[testX + XSize * (testY + YSize * testZ)].Active &&
                            FilterExplosion(Frames[CurrentFrame].Voxels[testX + XSize * (testY + YSize * testZ)].Value, valueFilter, valueFilterOperation))
                            {
                                Voxel v = Frames[CurrentFrame].Voxels[testX + XSize * (testY + YSize * testZ)];
                                v.Color *= tint;
                                batch.Add(v, testX, testY, testZ, transform.localToWorldMatrix.MultiplyPoint3x4(checkPos - Pivot));// );
                                SetVoxelStateAtArrayPosition(testX, testY, testZ, VoxelState.Hidden);
                            }
                        }
                    }


            return batch;
        }

        /// <summary>
        /// Adds particles to the PicaVoxel Particle System (if available) representing the shape of this volume
        /// Use it before destroying/deactivating the volume to leave particles behind
        /// </summary>
        /// <param name="particleVelocity">Initial velocity of the created particles (outward from center of volume)</param>
        /// <param name="actuallyDestroyVoxels">If true, will set all the voxels to inactive</param>
        public void Destruct(float particleVelocity, bool actuallyDestroyVoxels)
        {
            Vector3 posZero = transform.position + (transform.rotation * (-Pivot + (Vector3.one * (VoxelSize * 0.5f))));
            Vector3 oneX = transform.rotation * (new Vector3(VoxelSize, 0, 0));
            Vector3 oneY = transform.rotation * (new Vector3(0f, VoxelSize, 0));
            Vector3 oneZ = transform.rotation * (new Vector3(0, 0, VoxelSize));

            Vector3 partPos = posZero;
            Color matColor = Material.GetColor("_Tint");
            for (int x = 0; x < XSize; x++)
            {
                Vector3 xmult = oneX * x;
                for (int y = 0; y < YSize; y++)
                {
                    Vector3 ymult = oneY * y;
                    for (int z = 0; z < ZSize; z++)
                    {
                        Vector3 zmult = oneZ * z;
                        partPos.x = posZero.x + xmult.x + ymult.x + zmult.x;
                        partPos.y = posZero.y + xmult.y + ymult.y + zmult.y;
                        partPos.z = posZero.z + xmult.z + ymult.z + zmult.z;

                        if (Frames[CurrentFrame].Voxels[x + XSize * (y + YSize * z)].Active)
                        {
                            Voxel v = Frames[CurrentFrame].Voxels[x + XSize * (y + YSize * z)];
                            v.Color *= matColor;
                            destructBatch.Add(v, x, y, z, partPos);
                            v.State = VoxelState.Hidden;
                            if (actuallyDestroyVoxels) SetVoxelAtArrayPosition(x, y, z, v);
                        }
                    }
                }
            }

            if (destructBatch.Voxels.Count > 0 && VoxelParticleSystem.Instance != null)
                VoxelParticleSystem.Instance.SpawnBatch(destructBatch,
                    pos => (pos - transform.position).normalized * Random.Range(0f, particleVelocity * 2f));

            destructBatch.Clear();
        }

        /// <summary>
        /// Restore all voxels that have been destroyed (Voxel.State = VoxelState.Hidden) on all frames
        /// </summary>
        public void Rebuild()
        {
            foreach (Frame frame in Frames)
                frame.Rebuild();

        }

        private bool FilterExplosion(byte value, int valueFilter, Exploder.ExplodeValueFilterOperation valueFilterOperation)
        {
            switch (valueFilterOperation)
            {
                case Exploder.ExplodeValueFilterOperation.LessThan:
                    return value < valueFilter;
                case Exploder.ExplodeValueFilterOperation.LessThanOrEqualTo:
                    return value <= valueFilter;
                case Exploder.ExplodeValueFilterOperation.EqualTo:
                    return value == valueFilter;
                case Exploder.ExplodeValueFilterOperation.GreaterThanOrEqualTo:
                    return value >= valueFilter;
                case Exploder.ExplodeValueFilterOperation.GreaterThan:
                    return value > valueFilter;
            }

            return false;
        }

        /// <summary>
        /// Rotate the entire volume around the X axis
        /// </summary>
        public void RotateX()
        {
            int tempSize = YSize;
            YSize = ZSize;
            ZSize = tempSize;
            foreach (Frame frame in Frames)
            {
                Helper.RotateVoxelArrayX(ref frame.Voxels,new PicaVoxelPoint(frame.XSize, frame.YSize, frame.ZSize));
                frame.XSize = XSize;
                frame.YSize = YSize;
                frame.ZSize = ZSize;
            }

            CreateChunks();
            SaveForSerialize();
        }

        /// <summary>
        /// Rotate the entire volume around the Y axis
        /// </summary>
        public void RotateY()
        {
            int tempSize = XSize;
            XSize = ZSize;
            ZSize = tempSize;
            foreach (Frame frame in Frames)
            {
                Helper.RotateVoxelArrayY(ref frame.Voxels, new PicaVoxelPoint(frame.XSize, frame.YSize, frame.ZSize));
                frame.XSize = XSize;
                frame.YSize = YSize;
                frame.ZSize = ZSize;
            }

            CreateChunks();
            SaveForSerialize();
        }

        /// <summary>
        /// Rotate the entire volume around the Z axis
        /// </summary>
        public void RotateZ()
        {
            int tempSize = YSize;
            YSize = XSize;
            XSize = tempSize;

            foreach (Frame frame in Frames)
            {
                Helper.RotateVoxelArrayZ(ref frame.Voxels, new PicaVoxelPoint(frame.XSize, frame.YSize, frame.ZSize));
                frame.XSize = XSize;
                frame.YSize = YSize;
                frame.ZSize = ZSize;
            }
           
            CreateChunks();
            SaveForSerialize();
        }

        public void ScrollX(int amount, bool allFrames)
        {
            if (allFrames)
                foreach (Frame frame in Frames)
                    frame.ScrollX(amount);
            else
                GetCurrentFrame().ScrollX(amount);
        }

        public void ScrollY(int amount, bool allFrames)
        {
            if (allFrames)
                foreach (Frame frame in Frames)
                    frame.ScrollY(amount);
            else
                GetCurrentFrame().ScrollY(amount);
        }

        public void ScrollZ(int amount, bool allFrames)
        {
            if (allFrames)
                foreach (Frame frame in Frames)
                    frame.ScrollZ(amount);
            else
                GetCurrentFrame().ScrollZ(amount);
        }

        private void UpdateBoxCollider()
        {
            Hitbox = transform.Find("Hitbox").GetComponent<BoxCollider>();
            Hitbox.size = new Vector3(XSize*VoxelSize, YSize*VoxelSize, ZSize*VoxelSize);
            Hitbox.center = new Vector3(XSize*VoxelSize, YSize*VoxelSize, ZSize*VoxelSize)/2f;
            transform.Find("Hitbox").transform.localPosition = -Pivot;

        }

        /// <summary>
        /// Serialise all frames to byte array ready for Unity serialisation
        /// </summary>
        public void SaveForSerialize()
        {
            //Debug.Log("Object SaveForSerialize");
            foreach (Frame frame in Frames) frame.SaveForSerialize();
        }

        /// <summary>
        /// Change the mesh collision mode of all frames
        /// </summary>
        /// <param name="collisionMode">The CollisonMode to change to</param>
        public void ChangeCollisionMode(CollisionMode collisionMode)
        {
            CollisionMode = collisionMode;
            foreach (Frame frame in Frames) frame.GenerateMeshColliders();
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {

        }

        // Editor-specific functionality
#if UNITY_EDITOR
        public bool Undone = false;

        public void OnUndoRedo()
        {
            Undone = true;

            if (this == null) return;
            //CreateChunks();
        }

        public void OnDrawGizmos()
        {
            foreach (Frame frame in Frames)
            {
                for (int i = 0; i < frame.transform.Find("Chunks").childCount; i++)
                {
                    var o = frame.transform.Find("Chunks").GetChild(i).gameObject;
                    if (o.GetComponent<Renderer>() && IsEnabledForEditing)
                        EditorUtility.SetSelectedRenderState(o.GetComponent<Renderer>(), EditorSelectedRenderState.Hidden);
                    else
                        EditorUtility.SetSelectedRenderState(o.GetComponent<Renderer>(), EditorSelectedRenderState.Highlight);
                }
            }

            if (!IsEnabledForEditing) return;

            if (DrawGrid)
            {
                Gizmos.matrix = Frames[CurrentFrame].transform.Find("Chunks").localToWorldMatrix;
                Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
                for (int x = 0; x <= XSize; x++)
                    for (int y = 0; y <= YSize; y++)
                        for (int z = 0; z <= ZSize; z++)
                        {
                            if (x == 0)
                                Gizmos.DrawLine(new Vector3(x, y, z)*VoxelSize, new Vector3(x + XSize, y, z)*VoxelSize);
                            if (y == 0)
                                Gizmos.DrawLine(new Vector3(x, y, z)*VoxelSize, new Vector3(x, y + YSize, z)*VoxelSize);
                            if (z == 0)
                                Gizmos.DrawLine(new Vector3(x, y, z)*VoxelSize, new Vector3(x, y, z + ZSize)*VoxelSize);
                        }
            }


        }
#endif


       
    }
}