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
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicaVoxel
{
    public static class MeshScanner
    {
       
        public static void Scan(GameObject meshObject, float voxelSize)
        {
            Transform meshTransform = meshObject.transform;

            Mesh mesh = null;
            MeshFilter mF = meshObject.GetComponent<MeshFilter>();
            if (mF != null)
                mesh = mF.sharedMesh;
            else
            {
                SkinnedMeshRenderer sMR = meshObject.GetComponent<SkinnedMeshRenderer>();
                if (sMR != null)
                    mesh = sMR.sharedMesh;
            }
            if (mesh == null) return;

           // bool addedCollider = false;
            GameObject mcContainer = new GameObject("PVMCC");
            mcContainer.transform.SetParent(meshObject.transform, false);
            MeshCollider mc = mcContainer.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            GameObject mcContainerRev = new GameObject("PVMCCR");
            mcContainerRev.transform.SetParent(meshObject.transform, false);
            Mesh reverseMesh = (Mesh)UnityEngine.Object.Instantiate(mesh);
            reverseMesh.triangles = mesh.triangles.Reverse().ToArray();
            MeshCollider mcr = mcContainerRev.AddComponent<MeshCollider>();
            mcr.sharedMesh = reverseMesh;

            Vector3[] vertices = mesh.vertices;
            if (vertices.Length <= 0) return;

            Texture2D renderTex =null;
            Color materialColor = Color.white;
            Renderer rend = meshObject.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.mainTexture != null)
            {
                renderTex = (Texture2D)rend.sharedMaterial.mainTexture;
                try
                {
                    renderTex.GetPixel(0, 0);
                }
                catch (Exception)
                {
                    string path = AssetDatabase.GetAssetPath(rend.sharedMaterial.mainTexture);
                    TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(path);
                    ti.isReadable = true;
                    AssetDatabase.ImportAsset(path);
                    ti.isReadable = false;

                    try
                    {
                        renderTex.GetPixel(0, 0);
                    }
                    catch (Exception)
                    {
                        renderTex = null;
                    }
                }
            }
            else if (rend != null && rend.sharedMaterial != null)
            {
                materialColor = rend.sharedMaterial.color;
            }

            Vector3 min, max;
            min = max = meshTransform.TransformPoint(vertices[0]);
            for (int i = 1; i < vertices.Length; i++)
            {
                Vector3 V = meshTransform.TransformPoint(vertices[i]);
                for (int n = 0; n < 3; n++)
                {
                    if (V[n] > max[n])
                        max[n] = V[n];
                    if (V[n] < min[n])
                        min[n] = V[n];
                }

                vertices[i] = V;
            }
            vertices[0] = meshTransform.TransformPoint(vertices[0]);
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f) return;

            var newObject = Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            newObject.name = meshObject.name+" (Voxels)";
            newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
            newObject.GetComponent<Volume>().GenerateBasic(FillMode.None);
            Volume voxelVolume = newObject.GetComponent<Volume>();

            voxelVolume.XSize = (int)(bounds.size.x/voxelSize)+1;
            voxelVolume.YSize = (int)(bounds.size.y / voxelSize)+1;
            voxelVolume.ZSize = (int)(bounds.size.z / voxelSize)+1;
            voxelVolume.Frames[0].XSize = voxelVolume.XSize;
            voxelVolume.Frames[0].YSize = voxelVolume.YSize;
            voxelVolume.Frames[0].ZSize = voxelVolume.ZSize;
            voxelVolume.Frames[0].Voxels = new Voxel[voxelVolume.XSize * voxelVolume.YSize * voxelVolume.ZSize];
            for (int i = 0; i < voxelVolume.Frames[0].Voxels.Length; i++) voxelVolume.Frames[0].Voxels[i].Value = 128;
            voxelVolume.VoxelSize = voxelSize;


            Vector3[] testDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };

            float stepScale = 0.5f;
            int size = (int)((bounds.size.x) / (voxelSize * stepScale)) * (int)((bounds.size.y) / (voxelSize * stepScale)) *
                       (int)((bounds.size.z) / (voxelSize * stepScale));
            int progress = 0;

            // Outside
            for (float x = bounds.min.x; x < bounds.max.x + voxelSize; x += voxelSize * stepScale)
            {
                for (float y = bounds.min.y; y < bounds.max.y + voxelSize; y += voxelSize * stepScale)
                {
                    for (float z = bounds.min.z; z < bounds.max.z + voxelSize; z += voxelSize * stepScale)
                    {

                        Vector3 test = new Vector3(x, y, z);

                        for (int d = 0; d < 6; d++)
                        {
                            Vector3 hit;
                            Color col;
                            if (TestRay(mc, mcContainer, renderTex, materialColor, mesh, test, testDirs[d].normalized, out hit, out col))
                            {
                                int vX = (voxelVolume.XSize-1) - (int)((bounds.max.x - hit.x) / voxelSize);
                                int vY = (voxelVolume.YSize - 1) - (int)((bounds.max.y - hit.y) / voxelSize);
                                int vZ = (voxelVolume.ZSize - 1) - (int)((bounds.max.z - hit.z) / voxelSize);
                                if (!(vX < voxelVolume.XSize && vY < voxelVolume.YSize && vZ < voxelVolume.ZSize)) continue;
                                voxelVolume.Frames[0].Voxels[vX + voxelVolume.XSize * (vY + voxelVolume.YSize * vZ)] = new Voxel() { State = VoxelState.Active, Color = col, Value = 128 };
                            }
                        }

                        progress++;

                    }
                }


                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Scanning Mesh", "Scanning Outside", (1f / (float)size) * (float)progress))
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                    GameObject.DestroyImmediate(voxelVolume.gameObject);
                    UnityEngine.Object.DestroyImmediate(mcContainer);
                    UnityEngine.Object.DestroyImmediate(mcContainerRev);
                    return;
                }
            }

            progress = 0;

            //Inside
            for (float x = bounds.min.x; x < bounds.max.x + voxelSize; x += voxelSize * stepScale)
            {
                for (float y = bounds.min.y; y < bounds.max.y + voxelSize; y += voxelSize * stepScale)
                {
                    for (float z = bounds.min.z; z < bounds.max.z + voxelSize; z += voxelSize * stepScale)
                    {
                        Vector3 test = new Vector3(x, y, z);

                        Vector3 hit;
                        Color col;

                        float closestHit = Mathf.Infinity;
                        Color closestCol = Color.white;

                        int hitCount = 0;
                        for (int d = 0; d < 6; d++)
                        {
                            
                            if (TestRay(mcr, mcContainerRev, renderTex, materialColor, reverseMesh, test, testDirs[d].normalized, out hit, out col))
                            {
                                if (Vector3.Distance(hit, test) < closestHit)
                                {
                                    closestHit = Vector3.Distance(hit, test);
                                    closestCol = col;
                                }
                                hitCount++;

                                
                            }
                        }

                        if (hitCount ==6)
                        {
                            int vX = (voxelVolume.XSize - 1) - (int)((bounds.max.x - test.x) / voxelSize);
                            int vY = (voxelVolume.YSize - 1) - (int)((bounds.max.y - test.y) / voxelSize);
                            int vZ = (voxelVolume.ZSize - 1) - (int)((bounds.max.z - test.z) / voxelSize);
                            if (!(vX < voxelVolume.XSize && vY < voxelVolume.YSize && vZ < voxelVolume.ZSize)) continue;
                            voxelVolume.Frames[0].Voxels[vX + voxelVolume.XSize * (vY + voxelVolume.YSize * vZ)] = new Voxel() { State = VoxelState.Active, Color = closestCol, Value = 128 };
                        }

                        progress++;

                        
                    }
                }

                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Scanning Mesh", "Attempting to fill inside", (1f / (float)size) * (float)progress))
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                    GameObject.DestroyImmediate(voxelVolume.gameObject);
                    UnityEngine.Object.DestroyImmediate(mcContainer);
                    UnityEngine.Object.DestroyImmediate(mcContainerRev);
                    return;
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();

           

            voxelVolume.CreateChunks();
            voxelVolume.SaveForSerialize();
            voxelVolume.Frames[0].OnAfterDeserialize();
            voxelVolume.transform.position = meshObject.transform.position;

            voxelVolume.Pivot = (new Vector3(voxelVolume.XSize, voxelVolume.YSize, voxelVolume.ZSize) * voxelVolume.VoxelSize) / 2f;
            voxelVolume.UpdatePivot();

            UnityEngine.Object.DestroyImmediate(mcContainer);
            UnityEngine.Object.DestroyImmediate(mcContainerRev);

            voxelVolume.UpdateAllChunks();
        }

        static bool TestRay(MeshCollider mc, GameObject mcContainer, Texture2D tex, Color matCol, Mesh mesh, Vector3 point, Vector3 dir, out Vector3 hitpoint, out Color col)
        {
            RaycastHit hitInfo;

            Ray ray = new Ray(point, dir);
            if (mc.Raycast(ray, out hitInfo, Mathf.Infinity))
            {
                if (hitInfo.collider.gameObject == mcContainer)
                {
                    hitpoint = hitInfo.point;
                    if (tex != null)
                    {
                        Vector3 uv = hitInfo.textureCoord;
                        col = tex.GetPixel((int) (uv.x*tex.width), (int) (uv.y*tex.height));
                    }
                    else
                    {
                        col = matCol;
                    }
                    return true;
                }
            }

            hitpoint = Vector3.zero;
            col = Color.white;
            return false;
        }


    }
}
