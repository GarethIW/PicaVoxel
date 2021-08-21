using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PicaVoxel;
using System;

public class VolumeSplitter : MonoBehaviour {

    struct I3
    {
        public int X;
        public int Y;
        public int Z;

        public I3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public void Split()
    {
        var splits = new List<bool[]>();

        var vol = GetComponent<Volume>();

        bool finished = false;

        int lastx = 0, lasty = 0, lastz = 0;

        while (!finished)
        {
            bool startingsplit = false;
            //Debug.Log("Split counter: " + splits.Count + " | Last: " + lastx + "," + lasty + "," + lastz);
            for (var x = lastx; x < vol.XSize; x++)
            {
               
                for (var y = lasty; y < vol.YSize; y++)
                {
                    
                    for (var z = lastz; z < vol.ZSize; z++)
                    {
                        lastx = x;
                        lasty = y;
                        lastz = z;

                        //vol.Frames[0].Voxels[x + vol.XSize * (y + vol.YSize * z)].Color = Color.red;
                        if (vol.Frames[0].Voxels[x + vol.XSize * (y + vol.YSize * z)].State != VoxelState.Active) continue;
                        var found = false;
                        foreach (var a in splits)
                            if (a[x + vol.XSize * (y + vol.YSize * z)])
                            {
                                found = true;
                                break;
                            }

                        if (found) continue;

                       // Debug.Log("Found a possible split at " + lastx + "," + lasty + "," + lastz);
                        startingsplit = true;

                        break;
                    }
                    if (startingsplit) break;
                    lastz = 0;
                }
                if (startingsplit) break;
                lasty = 0;
            }


            if (startingsplit)
            {
                var checklist = new bool[vol.XSize * vol.YSize * vol.ZSize];
                splits.Add(checklist);

                //Debug.Log("Started a new split");

                var queue = new Queue<I3>();
                queue.Enqueue(new I3(lastx, lasty, lastz));
                checklist[lastx + vol.XSize * (lasty + vol.YSize * lastz)] = true;

                var safetybadger = 0;
                while (queue.Count!=0 && safetybadger< vol.XSize * vol.YSize * vol.ZSize)
                {
                    safetybadger++;
                    var p = queue.Dequeue();
                    TryAdd(vol, queue, checklist, p.X - 1, p.Y, p.Z);
                    TryAdd(vol, queue, checklist, p.X + 1, p.Y, p.Z);
                    TryAdd(vol, queue, checklist, p.X, p.Y - 1, p.Z);
                    TryAdd(vol, queue, checklist, p.X, p.Y + 1, p.Z);
                    TryAdd(vol, queue, checklist, p.X, p.Y, p.Z - 1);
                    TryAdd(vol, queue, checklist, p.X, p.Y, p.Z + 1);
                }
            }

            if (!startingsplit) finished = true;

        }

        if (splits.Count > 1)
        {
            foreach (var s in splits)
            {
                var newvol = Instantiate(vol);

                for (int i = 0; i < s.Length; i++)
                    newvol.Frames[0].Voxels[i].State = (s[i]) ? VoxelState.Active : VoxelState.Inactive;

                newvol.UpdateAllChunksNextFrame();
                newvol.UpdateChunks(true);
            }

            Destroy(vol.gameObject);
        }
    }

    private void TryAdd(Volume vol, Queue<I3> queue, bool[] checklist, int x, int y, int z)
    {
        if (x < 0 || x > vol.XSize - 1 || y < 0 || y > vol.YSize - 1 || z < 0 || z > vol.ZSize - 1) return;
        if (checklist[x + vol.XSize * (y + vol.YSize * z)]) return;
        if (vol.Frames[0].Voxels[x + vol.XSize * (y + vol.YSize * z)].State != VoxelState.Active) return;

        checklist[x + vol.XSize * (y + vol.YSize * z)] = true;
        queue.Enqueue(new I3(x, y, z));
    }
}
