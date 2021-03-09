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
    public static class TerrainGenerator
    {
       
        public static void Generate(int xSize, int ySize, int zSize, float smoothness, float voxelSize)
        {
            

            var newObject = Editor.Instantiate(EditorUtility.VoxelVolumePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            newObject.name = "Terrain";
            newObject.GetComponent<Volume>().Material = EditorUtility.PicaVoxelDiffuseMaterial;
            newObject.GetComponent<Volume>().GenerateBasic(FillMode.None);
            Volume voxelVolume = newObject.GetComponent<Volume>();

            voxelVolume.XSize = xSize;
            voxelVolume.YSize = ySize;
            voxelVolume.ZSize = zSize;
            voxelVolume.Frames[0].XSize = voxelVolume.XSize;
            voxelVolume.Frames[0].YSize = voxelVolume.YSize;
            voxelVolume.Frames[0].ZSize = voxelVolume.ZSize;
            voxelVolume.Frames[0].Voxels = new Voxel[voxelVolume.XSize * voxelVolume.YSize * voxelVolume.ZSize];

            // Could remove this if we use Value as block type
            for (int i = 0; i < voxelVolume.Frames[0].Voxels.Length; i++) voxelVolume.Frames[0].Voxels[i].Value = 128;
            voxelVolume.VoxelSize = voxelSize;


      
            int progress = 0;
           // float progressSize = xSize*zSize;

            float[][] noise = PerlinNoise.GeneratePerlinNoise(xSize, zSize, 2 + (int)(smoothness * 6f));


            for(int x=0;x<xSize;x++)
                for (int z = 0; z < zSize; z++)
                {
                    //if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Terrain Generator", "Generating Terrain",
                    //    (1f/(float) progressSize)*(float) progress))
                    //{
                    //    UnityEditor.EditorUtility.ClearProgressBar();
                    //    GameObject.DestroyImmediate(voxelVolume.gameObject);
                    //    return;
                    //}

                    for (int y = 0; y < ySize; y++)
                    {
                        voxelVolume.Frames[0].Voxels[x + xSize * (y + ySize * z)] = new Voxel()
                        {
                            State = (noise[x][z] >= (1f/(float)ySize) * (float)y)?VoxelState.Active : VoxelState.Inactive,
                            Color = (y==ySize-1 || noise[x][z] < (1f / (float)ySize) * (float)(y+1))? new Color32(0, 128, 0,255) : new Color32(128,64,0,255),
                        };
                    }

                    progress++;
                }

            voxelVolume.CreateChunks();
            voxelVolume.SaveForSerialize();
            voxelVolume.Frames[0].OnAfterDeserialize();

            voxelVolume.Pivot = (new Vector3(voxelVolume.XSize, 0, voxelVolume.ZSize) * voxelVolume.VoxelSize) / 2f;
            voxelVolume.UpdatePivot();

            voxelVolume.UpdateAllChunks();
        }

        


    }

#region Perlin Noise Gen

    public class PerlinNoise
    {
        static System.Random random = new System.Random();

        public static float[][] GenerateWhiteNoise(int width, int height)
        {
            float[][] noise = GetEmptyArray<float>(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    noise[i][j] = (float) random.NextDouble()%1;
                }
            }

            return noise;
        }

        public static float Interpolate(float x0, float x1, float alpha)
        {
            return x0*(1 - alpha) + alpha*x1;
        }

        public static T[][] GetEmptyArray<T>(int width, int height)
        {
            T[][] image = new T[width][];

            for (int i = 0; i < width; i++)
            {
                image[i] = new T[height];
            }

            return image;
        }

        public static float[][] GenerateSmoothNoise(float[][] baseNoise, int octave)
        {
            int width = baseNoise.Length;
            int height = baseNoise[0].Length;

            float[][] smoothNoise = GetEmptyArray<float>(width, height);

            int samplePeriod = 1 << octave; // calculates 2 ^ k
            float sampleFrequency = 1.0f/samplePeriod;

            for (int i = 0; i < width; i++)
            {
                //calculate the horizontal sampling indices
                int sample_i0 = (i/samplePeriod)*samplePeriod;
                int sample_i1 = (sample_i0 + samplePeriod)%width; //wrap around
                float horizontal_blend = (i - sample_i0)*sampleFrequency;

                for (int j = 0; j < height; j++)
                {
                    //calculate the vertical sampling indices
                    int sample_j0 = (j/samplePeriod)*samplePeriod;
                    int sample_j1 = (sample_j0 + samplePeriod)%height; //wrap around
                    float vertical_blend = (j - sample_j0)*sampleFrequency;

                    //blend the top two corners
                    float top = Interpolate(baseNoise[sample_i0][sample_j0],
                        baseNoise[sample_i1][sample_j0], horizontal_blend);

                    //blend the bottom two corners
                    float bottom = Interpolate(baseNoise[sample_i0][sample_j1],
                        baseNoise[sample_i1][sample_j1], horizontal_blend);

                    //final blend
                    smoothNoise[i][j] = Interpolate(top, bottom, vertical_blend);
                }
            }

            return smoothNoise;
        }

        public static float[][] GeneratePerlinNoise(float[][] baseNoise, int octaveCount)
        {
            int width = baseNoise.Length;
            int height = baseNoise[0].Length;

            float[][][] smoothNoise = new float[octaveCount][][]; //an array of 2D arrays containing

            float persistance = 0.1f;

            //generate smooth noise
            for (int i = 0; i < octaveCount; i++)
            {
                smoothNoise[i] = GenerateSmoothNoise(baseNoise, i);
            }

            float[][] perlinNoise = GetEmptyArray<float>(width, height); //an array of floats initialised to 0

            float amplitude = 1.0f;
            float totalAmplitude = 0.0f;

            //blend noise together
            for (int octave = octaveCount - 1; octave >= 0; octave--)
            {
                amplitude *= persistance;
                totalAmplitude += amplitude;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        perlinNoise[i][j] += smoothNoise[octave][i][j]*amplitude;
                    }
                }
            }

            //normalisation
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    perlinNoise[i][j] /= totalAmplitude;
                }
            }

            return perlinNoise;
        }

        public static float[][] GeneratePerlinNoise(int width, int height, int octaveCount)
        {
            float[][] baseNoise = GenerateWhiteNoise(width, height);

            return GeneratePerlinNoise(baseNoise, octaveCount);
        }



        public static float[][] AdjustLevels(float[][] image, float low, float high)
        {
            int width = image.Length;
            int height = image[0].Length;

            float[][] newImage = GetEmptyArray<float>(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float col = image[i][j];

                    if (col <= low)
                    {
                        newImage[i][j] = 0;
                    }
                    else if (col >= high)
                    {
                        newImage[i][j] = 1;
                    }
                    else
                    {
                        newImage[i][j] = (col - low)/(high - low);
                    }
                }
            }

            return newImage;
        }
    }

    #endregion
}
