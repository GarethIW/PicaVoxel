using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PicaVoxel;

public class PlanetGenerator : MonoBehaviour {
    public int Size = 55;
    public int CrustDepth = 2;
    public float Bumpiness = 5;
    public float BumpFrequency = 0.1f;
    public int VeinNoiseOctaves = 3;
    public float VeinThreshold = 0.5f;
    public Color MainColor = new Color32(0,180,0,255);
    public Color VeinColor = new Color32(0, 60, 0, 255);
    public Color CrustColor = new Color32(170, 100,0 ,255);

    public void Generate()
    {
        var starttime = System.DateTime.Now;

        var vol = GetComponent<Volume>();

        // Only resize volume if necessary
        var resized = false;
        if (vol.XSize != Size * 2 || vol.YSize != Size * 2)
        {
            resized = true;
            vol.XSize = Size * 2;
            vol.YSize = Size * 2;
            vol.ZSize = 1;

            vol.XChunkSize = 64;
            vol.YChunkSize = 64;
            vol.ZChunkSize = 1;

            vol.Frames[0].XSize = vol.XSize;
            vol.Frames[0].YSize = vol.YSize;
            vol.Frames[0].ZSize = vol.ZSize;
        }

        var vox = new Voxel[vol.XSize * vol.YSize];

        // We're using perlin noise to generate the secondary color / rock veins
        float[][] noise = PerlinNoise.GeneratePerlinNoise(vol.XSize, vol.YSize, VeinNoiseOctaves);

        Vector2 centre = new Vector2(Size, Size) + (Vector2.one*0.5f);

        // First pass, generate the surface
        for (int x = 0; x < vol.XSize; x++)
            for (int y = 0; y < vol.YSize; y++)
            {
                var dist = Vector2.Distance(new Vector2(x, y) + (Vector2.one * 0.5f), centre);
                if (dist<Size)
                {
                    float colorBlend = 0f;
                    if (noise[x][y] > VeinThreshold)
                        colorBlend = (1f / (1f - VeinThreshold)) * (noise[x][y] - VeinThreshold);

                    vox[x + vol.XSize * (y+vol.YSize*0)] = new Voxel(){ State = VoxelState.Active, Color = Color.Lerp(MainColor,VeinColor,colorBlend), Value = 128 };
                }
            }

        // Second pass, generate the edge/crust
        var currentDepth = 0f;
        var targetDepth = Random.Range(0f,Bumpiness);
        for(float a =0;a<Mathf.PI*2;a+=0.01f)
        {
            float crust = Random.Range(CrustDepth, (CrustDepth*2));
            if (CrustDepth < 1) crust = 0;

            for (float d = (Size - currentDepth) - crust; d <= Size+1; d += 0.5f)
            {
                int vx = (int)centre.x + (int)(d * Mathf.Cos(a));
                int vy = (int)centre.y + (int)(d * Mathf.Sin(a));

                if (vx < 0) vx = 0;
                if (vx >= vol.XSize) vx = vol.XSize-1;
                if (vy < 0) vy = 0;
                if (vy >= vol.YSize) vy = vol.YSize-1;

                if (d >= (Size - currentDepth) - crust)
                    vox[vx + vol.XSize * (vy + vol.YSize * 0)] = new Voxel() { State = VoxelState.Active, Color = CrustColor, Value = 128 };

                if (d >= Size - currentDepth)
                    vox[vx + vol.XSize * (vy + vol.YSize * 0)] = new Voxel() { State = VoxelState.Inactive };

                if (d >= Size)
                    vox[vx + vol.XSize * (vy + vol.YSize * 0)] = new Voxel() { State = VoxelState.Inactive };
            }

            currentDepth = Mathf.Lerp(currentDepth, targetDepth, BumpFrequency);
            if(Mathf.Abs(currentDepth-targetDepth)<0.1f)
                targetDepth = Random.Range(0f, Bumpiness);

            if(a>=(Mathf.PI*2)-0.1f)
                targetDepth = 0f;
        }

        vol.Frames[0].Voxels = vox;

        var pivot = (new Vector3(vol.XSize, vol.YSize, vol.ZSize) * vol.VoxelSize) / 2f;
        vol.Pivot = pivot;
        vol.UpdatePivot();

        Debug.Log("Generated in (ms): " + System.TimeSpan.FromTicks(System.DateTime.Now.Ticks - starttime.Ticks).TotalMilliseconds);

        starttime = System.DateTime.Now;

        vol.SaveForSerialize();
        if (resized)
            vol.CreateChunks();
        else
            if(!Application.isPlaying)
            vol.UpdateAllChunks();
        else
            vol.UpdateAllChunksNextFrame();

        Debug.Log("Chunked in (ms): " + System.TimeSpan.FromTicks(System.DateTime.Now.Ticks - starttime.Ticks).TotalMilliseconds);

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
                noise[i][j] = (float)random.NextDouble() % 1;
            }
        }

        return noise;
    }

    public static float Interpolate(float x0, float x1, float alpha)
    {
        return x0 * (1 - alpha) + alpha * x1;
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
        float sampleFrequency = 1.0f / samplePeriod;

        for (int i = 0; i < width; i++)
        {
            //calculate the horizontal sampling indices
            int sample_i0 = (i / samplePeriod) * samplePeriod;
            int sample_i1 = (sample_i0 + samplePeriod) % width; //wrap around
            float horizontal_blend = (i - sample_i0) * sampleFrequency;

            for (int j = 0; j < height; j++)
            {
                //calculate the vertical sampling indices
                int sample_j0 = (j / samplePeriod) * samplePeriod;
                int sample_j1 = (sample_j0 + samplePeriod) % height; //wrap around
                float vertical_blend = (j - sample_j0) * sampleFrequency;

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
                    perlinNoise[i][j] += smoothNoise[octave][i][j] * amplitude;
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
   
}

#endregion
