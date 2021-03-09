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
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Threading;
#if UNITY_WINRT && !UNITY_EDITOR
using System.Threading.Tasks;
#endif
using UnityEngine.UI;

namespace PicaVoxel
{
    [AddComponentMenu("")]
    public class Chunk : MonoBehaviour
    {
        private class RecalculateToken
        {
            public Voxel[] voxels;
            public float voxelSize;
            public float overlapAmount;
            public int xOffset;
            public int yOffset;
            public int zOffset;
            public int xSize;
            public int ySize;
            public int zSize;
            public int ub0;
            public int ub1;
            public int ub2;
            public float selfShadeIntensity;
            public MeshingMode mode;
            public MeshingMode colliderMode;
            public bool immediate;
#if UNITY_EDITOR
            public EditorPaintMode paintMode;
#endif
        }
        
        private enum ChunkStatus
        {
            NoChange,
            CalculatingMesh,
            Ready
        }

        //public const int CHUNK_SIZE = 16;

        public bool IsUpdated;

        private ChunkStatus status = ChunkStatus.NoChange;

        private List<Vector3> vertices = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<Color32> colors = new List<Color32>();
        private List<int> indexes = new List<int>();

        private MeshFilter mf;
        private MeshCollider mc;

        private bool hasCreatedRuntimeMesh = false;
        private bool hasCreatedRuntimeColMesh = false;
        private bool updateColliderNextFrame = false;

        private bool needsRecalculating;
        private RecalculateToken recalcToken;
        
        private void Update()
        {
            if (status == ChunkStatus.Ready)
            {
                status = ChunkStatus.NoChange;
                if (!needsRecalculating || recalcToken==null)
                {
                    SetMesh();
                }
                else
                {
                    //Debug.Log("Recalculating");
                    needsRecalculating = false;
#if UNITY_EDITOR
                    GenerateMesh(recalcToken.voxels, recalcToken.voxelSize, recalcToken.overlapAmount, recalcToken.xOffset, recalcToken.yOffset, recalcToken.zOffset,recalcToken.xSize,recalcToken.ySize,recalcToken.zSize,recalcToken.ub0,recalcToken.ub1,recalcToken.ub2,recalcToken.selfShadeIntensity,recalcToken.mode,recalcToken.colliderMode,false,recalcToken.paintMode);
#else
                    GenerateMesh(recalcToken.voxels, recalcToken.voxelSize, recalcToken.overlapAmount, recalcToken.xOffset, recalcToken.yOffset, recalcToken.zOffset,recalcToken.xSize,recalcToken.ySize,recalcToken.zSize,recalcToken.ub0,recalcToken.ub1,recalcToken.ub2,recalcToken.selfShadeIntensity,recalcToken.mode,recalcToken.colliderMode,false);
#endif
                }
            }

            if (updateColliderNextFrame)
            {
                UpdateCollider();
            }
        }

#if UNITY_EDITOR
        public void GenerateMesh(Voxel[] voxels, float voxelSize, float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize,
            int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, MeshingMode mode, MeshingMode colliderMode, bool immediate,
            EditorPaintMode paintMode)
#else
        public void GenerateMesh(Voxel[] voxels, float voxelSize,float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, MeshingMode mode, MeshingMode colliderMode, bool immediate)
#endif
        {
            if (mf == null) mf = GetComponent<MeshFilter>();
            if (mc == null) mc = GetComponent<MeshCollider>();
            if (immediate)
            {
#if UNITY_EDITOR
                Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                            selfShadeIntensity, mode, paintMode);
#else
                Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                            selfShadeIntensity, mode);
#endif
                SetMesh();

                if (colliderMode != mode &&transform.parent.parent.parent.GetComponent<Volume>().CollisionMode != CollisionMode.None)
                {
#if UNITY_EDITOR
                    Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                                selfShadeIntensity, mode, paintMode);
#else
                Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                            selfShadeIntensity, mode);
#endif

                }

                if (transform.parent.parent.parent.GetComponent<Volume>().CollisionMode != CollisionMode.None)
                    SetColliderMesh();
            }
            else
            {
               
#if UNITY_WINRT && !UNITY_EDITOR
                System.Threading.Tasks.Task.Run(() =>
                {
                    GenerateThreaded(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize,ub0, ub1, ub2,
                        selfShadeIntensity, mode);
                });
#else
#if UNITY_WEBGL && !UNITY_EDITOR

                // WEBGL platform does not support threading (yet)
                Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                            selfShadeIntensity, mode);
                SetMesh();

                if (colliderMode != mode &&transform.parent.parent.parent.GetComponent<Volume>().CollisionMode != CollisionMode.None)
                {
                    Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                             selfShadeIntensity, mode);
                }

                if (transform.parent.parent.parent.GetComponent<Volume>().CollisionMode != CollisionMode.None)
                    SetColliderMesh();


#else
               
#if UNITY_EDITOR
                if (status != ChunkStatus.NoChange)
                {
                    needsRecalculating = true;
                    //Debug.Log("Setting recalc");
                    recalcToken = new RecalculateToken()
                    {
                        voxels = voxels,
                        voxelSize = voxelSize,
                        overlapAmount = overlapAmount,
                        xOffset = xOffset,
                        yOffset = yOffset,
                        zOffset = zOffset,
                        xSize = xSize,
                        ySize = ySize,
                        zSize = zSize,
                        ub0 = ub0,
                        ub1 = ub1,
                        ub2 = ub2,
                        selfShadeIntensity = selfShadeIntensity,
                        mode = mode,
                        colliderMode = colliderMode,
                        immediate = immediate,
                        paintMode = paintMode,    
                    };
                    return;
                }

                if(!ThreadPool.QueueUserWorkItem(delegate
                {
                    GenerateThreaded(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity, mode, paintMode);
                })) Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                                selfShadeIntensity, mode, paintMode);
#else
                if (status != ChunkStatus.NoChange)
                                {
                    needsRecalculating = true;
                    recalcToken = new RecalculateToken()
                    {
                        voxels = voxels,
                        voxelSize = voxelSize,
                        overlapAmount = overlapAmount,
                        xOffset = xOffset,
                        yOffset = yOffset,
                        zOffset = zOffset,
                        xSize = xSize,
                        ySize = ySize,
                        zSize = zSize,
                        ub0 = ub0,
                        ub1 = ub1,
                        ub2 = ub2,
                        selfShadeIntensity = selfShadeIntensity,
                        mode = mode,
                        colliderMode = colliderMode,
                        immediate = immediate, 
                    };
                    return;
                }
    
                if(!ThreadPool.QueueUserWorkItem(delegate
                {
                    GenerateThreaded(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize,ub0, ub1, ub2,
                        selfShadeIntensity, mode);
                })) Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                                selfShadeIntensity, mode);
#endif
#endif
#endif
            }
        }

#if UNITY_EDITOR
        private void GenerateThreaded(ref Voxel[] voxels, float voxelSize, float overlapAmount, int xOffset, int yOffset, int zOffset,
            int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, MeshingMode meshMode, EditorPaintMode paintMode)
#else
        private void GenerateThreaded(ref Voxel[] voxels, float voxelSize,float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, MeshingMode meshMode)
#endif
        {
            
            status = ChunkStatus.CalculatingMesh;
#if UNITY_EDITOR
            Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity, meshMode, paintMode);
          
           
#else
            Generate(ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity, meshMode);
#endif
         
            status = ChunkStatus.Ready;
        }

#if UNITY_EDITOR
        private void Generate(ref Voxel[] voxels, float voxelSize, float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, MeshingMode meshMode, EditorPaintMode paintMode)
#else
        private void Generate(ref Voxel[] voxels, float voxelSize,float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, MeshingMode meshMode)
#endif
        {
#if UNITY_EDITOR
            switch (meshMode)
            {
                case MeshingMode.Culled:
                    MeshGenerator.GenerateCulled(vertices, uvs, colors, indexes, ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity, paintMode);
                    break;
                case MeshingMode.Greedy:
                    MeshGenerator.GenerateGreedy(vertices, uvs, colors, indexes, ref voxels, voxelSize, overlapAmount, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity, paintMode);
                    break;
                case MeshingMode.Marching:
                    MeshGenerator.GenerateMarching(vertices, uvs, colors, indexes, ref voxels, voxelSize, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity, paintMode);
                    break;
            }
#else
           switch (meshMode)
            {
                case MeshingMode.Culled:
                    MeshGenerator.GenerateCulled(vertices, uvs, colors, indexes, ref voxels, voxelSize, overlapAmount,xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity);
                    break;
                case MeshingMode.Greedy:
                    MeshGenerator.GenerateGreedy(vertices, uvs, colors, indexes, ref voxels, voxelSize, overlapAmount,xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity);
                    break;
                case MeshingMode.Marching:
                    MeshGenerator.GenerateMarching(vertices, uvs, colors, indexes, ref voxels, voxelSize, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                        selfShadeIntensity);
                    break;
            }    
#endif
        }

        private void SetMesh()
        {
            Volume vol = transform.parent.parent.parent.GetComponent<Volume>();
            if (vol == null) return;

            if (vertices.Count == 0)
            {
                if (Application.isPlaying && !hasCreatedRuntimeMesh)
                {
                    if(mf.sharedMesh!=null)
                        mf.sharedMesh = (Mesh)Instantiate(mf.sharedMesh);
                    hasCreatedRuntimeMesh = true;
                }

                if (mf.sharedMesh != null)
                {
                    mf.sharedMesh.Clear();
                    mf.sharedMesh = null;
                }
                if (mc != null && vol.CollisionMode != CollisionMode.None && !vol.GenerateMeshColliderSeparately)
                {
                    mc.sharedMesh = null;
                }
                return;
            }

            if (mf.sharedMesh == null)
            {
                mf.sharedMesh = new Mesh();
            }

            if (Application.isPlaying && !hasCreatedRuntimeMesh)
            {
                mf.sharedMesh = (Mesh)Instantiate(mf.sharedMesh);
                hasCreatedRuntimeMesh = true;
            }

            mf.sharedMesh.Clear();
            mf.sharedMesh.SetVertices(vertices);
            mf.sharedMesh.SetColors(colors);
            mf.sharedMesh.SetUVs(0, uvs);
            mf.sharedMesh.SetTriangles(indexes, 0);
            
            mf.sharedMesh.RecalculateNormals();

            mf.GetComponent<Renderer>().sharedMaterial = vol.Material;

            if (mc != null && vol.CollisionMode != CollisionMode.None && !vol.GenerateMeshColliderSeparately)
            {
                updateColliderNextFrame = true;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && uvs.Count>0)
            {
                if (gameObject.isStatic) Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh);
                MeshUtility.SetMeshCompression(mf.sharedMesh, vol.MeshCompression);
            }
#endif
        }

        private void UpdateCollider()
        {
            mc.sharedMesh = null;
            mf.sharedMesh.RecalculateBounds();
            mc.sharedMesh = mf.sharedMesh;
            updateColliderNextFrame = false;
        }

        private void SetColliderMesh()
        {
            Volume vol = transform.parent.parent.parent.GetComponent<Volume>();
            if (vol == null) return;

            if (vertices.Count == 0)
            {
                if (mc.sharedMesh != null)
                {
                    mc.sharedMesh.Clear();
                    mc.sharedMesh = null;
                }
                return;
            }

            if (mc.sharedMesh == null || (Application.isPlaying && !hasCreatedRuntimeColMesh))
            {
                mc.sharedMesh = new Mesh();
                if (Application.isPlaying) hasCreatedRuntimeColMesh = true;
            }
            mc.sharedMesh.Clear();
            mc.sharedMesh.SetVertices(vertices);
            mc.sharedMesh.SetColors(colors);
            mc.sharedMesh.SetUVs(0, uvs);
            mc.sharedMesh.SetTriangles(indexes, 0);

            mc.sharedMesh.RecalculateNormals();
            mc.sharedMesh.RecalculateBounds();

#if UNITY_EDITOR
            if (!Application.isPlaying && uvs.Count > 0)
            {
                MeshUtility.SetMeshCompression(mc.sharedMesh, vol.MeshCompression);
            }
#endif
        }

#if UNITY_EDITOR
        public void SaveMeshes()
        {
            if (Application.isPlaying) return;

            Volume vol = transform.parent.parent.parent.GetComponent<Volume>();
            if (vol == null) return;

            Frame frame = transform.parent.parent.GetComponent<Frame>();
            if (frame == null) return;

            if (!PrefabUtility.IsPartOfAnyPrefab(vol))
            {
                try
                {
                    //if (vol.AssetGuid == Guid.Empty) vol.g = Guid.NewGuid();
                    string path = Path.Combine(Helper.GetMeshStorePath(), vol.AssetGuid);

                    if (mf.sharedMesh != null)
                    {
                        Mesh tempMesh = (Mesh) Instantiate(mf.sharedMesh);
                        AssetDatabase.CreateAsset(tempMesh, Path.Combine(path, transform.name + "_Frame" + frame.AssetGuid + ".asset"));

                        mf.sharedMesh = tempMesh;
                    }

                    if (mc != null && mc.sharedMesh != null)
                    {
                        Mesh tempMesh = (Mesh) Instantiate(mc.sharedMesh);
                        AssetDatabase.CreateAsset(tempMesh, Path.Combine(path, transform.name + "_Frame" + frame.AssetGuid + "_mc" + ".asset"));
                        mc.sharedMesh = tempMesh;
                    }
                }
                catch (Exception)
                {
                }
            }
//            if (PrefabUtility.IsPartOfAnyPrefab(vol))
//            {
//                PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
//                PrefabUtility.RecordPrefabInstancePropertyModifications(mc);
//            }
//            if (PrefabUtility.IsPartOfAnyPrefab(vol))
//            {
//                if (mf.sharedMesh != null)
//                {
//                    Mesh tempMesh = (Mesh) Instantiate(mf.sharedMesh);
//                    //AssetDatabase.CreateAsset(tempMesh, Path.Combine(path, transform.name + "_Frame" + frame.AssetGuid + ".asset"));
//                    PrefabUtility.SavePrefabAsset()
//                    //PrefabUtility.SaveAsPrefabAsset(mf, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(vol));
//                   // mf.sharedMesh = tempMesh;
//                   PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
//                }
//
//                if (mc != null && mc.sharedMesh != null)
//                {
//                   // Mesh tempMesh = (Mesh) Instantiate(mc.sharedMesh);
//                    //AssetDatabase.CreateAsset(tempMesh, Path.Combine(path, transform.name + "_Frame" + frame.AssetGuid + "_mc" + ".asset"));
//                   // mc.sharedMesh = tempMesh;
//                   PrefabUtility.RecordPrefabInstancePropertyModifications(mc);
//                }
                
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();
            
            
            
        }
#endif
    }
}