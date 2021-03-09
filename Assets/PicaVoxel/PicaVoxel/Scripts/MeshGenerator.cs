/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////
//
// PORTIONS OF THIS CODE:
//
// The MIT License (MIT)
//
// Copyright (c) 2012-2013 Mikola Lysenko
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace PicaVoxel
{
    public static class MeshGenerator
    {
        private const int SOUTH = 0;
        private const int NORTH = 1;
        private const int EAST = 2;
        private const int WEST = 3;
        private const int TOP = 4;
        private const int BOTTOM = 5;

        [Flags]
        enum Corners
        {
            TopLeft = 1,
            TopRight = 2,
            BottomRight = 4,
            BottomLeft = 8
        }
    
        class VoxelFace
        {
            public int Color;
            public Corners VertShade;
            public bool Active;
            public int Side;

            public bool Equals(VoxelFace face) { return face.Active==Active && face.Color == Color && face.VertShade == VertShade; }
        }

#if UNITY_EDITOR
        public static void GenerateGreedy(List<Vector3> vertices, List<Vector2> uvs, List<Color32> colors, List<int> indexes, ref Voxel[] invoxels, float voxelSize, float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, EditorPaintMode paintMode)
#else
        public static void GenerateGreedy(List<Vector3> vertices, List<Vector2> uvs, List<Color32> colors, List<int> indexes, ref Voxel[] invoxels, float voxelSize,float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity)
#endif
        {
            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            indexes.Clear();

            int i, j, k, l, w, h, u, v, n, side = 0;

            int[] x = new int[] { 0, 0, 0 };
            int[] q = new int[] { 0, 0, 0 };
            int[] du = new int[] { 0, 0, 0 };
            int[] dv = new int[] { 0, 0, 0 };

            int[] size = { xSize, ySize, zSize };

            VoxelFace[] mask = new VoxelFace[(xSize * ySize * zSize) * 2];
            VoxelFace voxelFace, voxelFace1;

            for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
            {
                for (int d = 0; d < 3; d++)
                {

                    u = (d + 1) % 3;
                    v = (d + 2) % 3;

                    x[0] = 0;
                    x[1] = 0;
                    x[2] = 0;

                    q[0] = 0;
                    q[1] = 0;
                    q[2] = 0;
                    q[d] = 1;

                    if (d == 0)
                    {
                        side = backFace ? WEST : EAST;
                    }
                    else if (d == 1)
                    {
                        side = backFace ? BOTTOM : TOP;
                    }
                    else if (d == 2)
                    {
                        side = backFace ? SOUTH : NORTH;
                    }

                    for (x[d] = -1; x[d] < size[d];)
                    {
                        n = 0;

                        for (x[v] = 0; x[v] < size[v]; x[v]++)
                        {

                            for (x[u] = 0; x[u] < size[u]; x[u]++)
                            {
                                if (x[d] >= 0)
                                {
                                    voxelFace = new VoxelFace();
#if UNITY_EDITOR
                                    GetVoxelFace(voxelFace, x[0], x[1], x[2], side, ref invoxels, voxelSize, xOffset,
                                        yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2, selfShadeIntensity, paintMode);
#else
                                    GetVoxelFace(voxelFace, x[0], x[1], x[2], side, ref invoxels, voxelSize, xOffset,
                                        yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2, selfShadeIntensity);
#endif
                                }
                                else voxelFace = null;

                                if (x[d] < size[d] - 1)
                                {
                                    voxelFace1 = new VoxelFace();
#if UNITY_EDITOR
                                    GetVoxelFace(voxelFace1, x[0] + q[0], x[1] + q[1], x[2] + q[2], side, ref invoxels,
                                        voxelSize, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                                        selfShadeIntensity, paintMode);
#else
                                    GetVoxelFace(voxelFace1, x[0] + q[0], x[1] + q[1], x[2] + q[2], side, ref invoxels,
                                        voxelSize, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2,
                                        selfShadeIntensity);
#endif
                                }
                                else voxelFace1 = null;

                                if (voxelFace != null && voxelFace1 != null && voxelFace.Equals(voxelFace1)) mask[n++] = null;
                                else
                                {
                                    if(!backFace && voxelFace!=null) mask[n++] = new VoxelFace()
                                    {
                                        Active = voxelFace.Active,
                                        Color = voxelFace.Color,
                                        Side = voxelFace.Side,
                                        VertShade =  voxelFace.VertShade
                                    };
                                    if(backFace && voxelFace1!=null) mask[n++] = new VoxelFace()
                                    {
                                        Active = voxelFace1.Active,
                                        Color = voxelFace1.Color,
                                        Side = voxelFace1.Side,
                                        VertShade = voxelFace1.VertShade
                                    };
                                }
                            }
                        }

                        x[d]++;
                        n = 0;

                        for (j = 0; j < size[v]; j++)
                        {

                            for (i = 0; i < size[u];)
                            {

                                if (mask[n] != null)
                                {
                                    for (w = 1;
                                        i + w < size[u] && mask[n + w] != null && mask[n + w].Equals(mask[n]);
                                        w++)
                                    {
                                    }

                                    bool done = false;

                                    for (h = 1; j + h < size[v]; h++)
                                    {

                                        for (k = 0; k < w; k++)
                                        {

                                            if (mask[n + k + h*size[u]] == null ||
                                                !mask[n + k + h*size[u]].Equals(mask[n]))
                                            {
                                                done = true;
                                                break;
                                            }
                                        }

                                        if (done)
                                        {
                                            break;
                                        }
                                    }

                                    if (mask[n].Active)
                                    {
                                        x[u] = i;
                                        x[v] = j;

                                        du[0] = 0;
                                        du[1] = 0;
                                        du[2] = 0;
                                        du[u] = w;

                                        dv[0] = 0;
                                        dv[1] = 0;
                                        dv[2] = 0;
                                        dv[v] = h;

                                        Quad(new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]),
                                            new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1],
                                                x[2] + du[2] + dv[2]),
                                            new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]),
                                            new Vector3(x[0], x[1], x[2]), 
                                            side,
                                            voxelSize,
                                            overlapAmount,
                                            mask[n],
                                            backFace,
                                            selfShadeIntensity,
                                            vertices, indexes, colors, uvs);
                                    }

                                    for (l = 0; l < h; ++l)
                                    {

                                        for (k = 0; k < w; ++k)
                                        {
                                            mask[n + k + l*size[u]] = null;
                                        }
                                    }

                                    i += w;
                                    n += w;
                                }
                                else
                                {

                                    i++;
                                    n++;
                                }
                            }
                        }
                        
                    }
                }
            }

          
        }

#if UNITY_EDITOR
        public static void GenerateCulled(List<Vector3> vertices, List<Vector2> uvs, List<Color32> colors, List<int> indexes, ref Voxel[] invoxels, float voxelSize, float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, EditorPaintMode paintMode)
#else
        public static void GenerateCulled(List<Vector3> vertices, List<Vector2> uvs, List<Color32> colors, List<int> indexes, ref Voxel[] invoxels, float voxelSize,float overlapAmount, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity)
#endif
        {
            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            indexes.Clear();

            VoxelFace vf = new VoxelFace();

            for (int z = 0; z < zSize; z++)
                for (int y = 0; y < ySize; y++)
                    for (int x = 0; x < xSize; x++)
                    {
                        if (invoxels[xOffset + x + (ub0 + 1) * (yOffset + y + (ub1 + 1) * (zOffset + z))].Active == false) continue;

                        Vector3 worldOffset = (new Vector3(x, y, z));

                        for (int f = 0; f < 6; f++)
                        {
#if UNITY_EDITOR
                            GetVoxelFace(vf, x, y, z, f, ref invoxels, voxelSize, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2, selfShadeIntensity, paintMode);
#else
                            GetVoxelFace(vf, x, y, z, f, ref invoxels, voxelSize, xOffset, yOffset, zOffset, xSize, ySize, zSize, ub0, ub1, ub2, selfShadeIntensity);
#endif

                            if (vf.Active)
                            {
                                switch (f)
                                {
                                    case 0: Quad(worldOffset + new Vector3(1, 0, 0), worldOffset + new Vector3(1, 1, 0), worldOffset + new Vector3(0, 1, 0), worldOffset + new Vector3(0, 0, 0), f, voxelSize, overlapAmount, vf, true, selfShadeIntensity, vertices, indexes, colors, uvs); break;
                                    case 1: Quad(worldOffset + new Vector3(1, 0, 1), worldOffset + new Vector3(1, 1, 1), worldOffset + new Vector3(0, 1, 1), worldOffset + new Vector3(0, 0, 1), f, voxelSize, overlapAmount, vf, false, selfShadeIntensity, vertices, indexes, colors, uvs); break;
                                    case 2: Quad(worldOffset + new Vector3(1, 1, 0), worldOffset + new Vector3(1, 1, 1), worldOffset + new Vector3(1, 0, 1), worldOffset + new Vector3(1, 0, 0), f, voxelSize, overlapAmount, vf, false, selfShadeIntensity, vertices, indexes, colors, uvs); break;
                                    case 3: Quad(worldOffset + new Vector3(0, 1, 0), worldOffset + new Vector3(0, 1, 1), worldOffset + new Vector3(0, 0, 1), worldOffset + new Vector3(0, 0, 0), f, voxelSize, overlapAmount, vf, true, selfShadeIntensity, vertices, indexes, colors, uvs); break;
                                    case 4: Quad(worldOffset + new Vector3(0, 1, 1), worldOffset + new Vector3(1, 1, 1), worldOffset + new Vector3(1, 1, 0), worldOffset + new Vector3(0, 1, 0), f, voxelSize, overlapAmount, vf, false, selfShadeIntensity, vertices, indexes, colors, uvs); break;
                                    case 5: Quad(worldOffset + new Vector3(0, 0, 1), worldOffset + new Vector3(1, 0, 1), worldOffset + new Vector3(1, 0, 0), worldOffset + new Vector3(0, 0, 0), f, voxelSize, overlapAmount, vf, true, selfShadeIntensity, vertices, indexes, colors, uvs); break;
                                }
                            }
                        }
                    }


          
        }

#if UNITY_EDITOR
        static void GetVoxelFace(VoxelFace voxelFace, int x, int y, int z, int side, ref Voxel[] invoxels, float voxelSize, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, EditorPaintMode paintMode)
#else
        static void GetVoxelFace(VoxelFace voxelFace, int x, int y, int z, int side, ref Voxel[] invoxels, float voxelSize, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity)
#endif
        {
            Voxel v = invoxels[(x + xOffset) + (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + zOffset))];

            voxelFace.Active = v.Active;
#if UNITY_EDITOR
            if(paintMode == EditorPaintMode.Color) voxelFace.Color = ColorToInt(v.Color);
            else voxelFace.Color = ColorToInt(new Color32(v.Value,v.Value,v.Value,255));
#else
            voxelFace.Color = ColorToInt(v.Color);
#endif
            voxelFace.VertShade = 0;
            voxelFace.Side = side;

            if (!voxelFace.Active) return;

            if (selfShadeIntensity > 0f)
            {
                switch (side)
                {
                    case 0:
                    case 1:
                        if (IsVoxelAt(x + xOffset -1, y + yOffset, z + zOffset + (side==0?-1:1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset - 1, y + yOffset - 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset, y + yOffset - 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.TopLeft;
                        if (IsVoxelAt(x + xOffset + 1, y + yOffset, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + 1, y + yOffset - 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset, y + yOffset - 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.TopRight;
                        if (IsVoxelAt(x + xOffset - 1, y + yOffset, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset - 1, y + yOffset + 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset, y + yOffset + 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.BottomLeft;
                        if (IsVoxelAt(x + xOffset + 1, y + yOffset, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + 1, y + yOffset + 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset, y + yOffset + 1, z + zOffset + (side == 0 ? -1 : 1), ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.BottomRight;
                        break;
                    case 2:
                    case 3:
                        if (IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset, z + zOffset-1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset - 1, z + zOffset-1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset - 1, z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.TopLeft;
                        if (IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset, z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset - 1, z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset - 1, z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.BottomLeft;
                        if (IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset, z + zOffset - 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset + 1, z + zOffset - 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset + 1, z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.TopRight;
                        if (IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset, z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset + 1, z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset + (side == 3 ? -1 : 1), y + yOffset + 1, z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.BottomRight;
                        break;
                    case 4:
                    case 5:
                        if (IsVoxelAt(x + xOffset, y + yOffset + (side == 5 ? -1 : 1), z + zOffset - 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset-1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset - 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset-1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.TopLeft;
                        if (IsVoxelAt(x + xOffset, y + yOffset + (side == 5 ? -1 : 1), z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset-1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset-1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.TopRight;
                        if (IsVoxelAt(x + xOffset, y + yOffset + (side == 5 ? -1 : 1), z + zOffset - 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset+1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset - 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset+1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.BottomLeft;
                        if (IsVoxelAt(x + xOffset, y + yOffset + (side == 5 ? -1 : 1), z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset+1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset + 1, ref invoxels, ub0, ub1, ub2) ||
                            IsVoxelAt(x + xOffset+1, y + yOffset + (side == 5 ? -1 : 1), z + zOffset, ref invoxels, ub0, ub1, ub2)) voxelFace.VertShade |= Corners.BottomRight;
                        break;
                }
            }

            switch (side)
            {
                case 0: voxelFace.Active = !IsVoxelAt(x + xOffset, y + yOffset, z+zOffset - 1, ref invoxels, ub0, ub1, ub2); break;
                case 1: voxelFace.Active = !IsVoxelAt(x + xOffset, y + yOffset, z + zOffset + 1, ref invoxels, ub0, ub1, ub2); break;
                case 2: voxelFace.Active = !IsVoxelAt(x + xOffset + 1, y + yOffset, z + zOffset, ref invoxels, ub0, ub1, ub2); break;
                case 3: voxelFace.Active = !IsVoxelAt(x + xOffset - 1, y + yOffset, z + zOffset, ref invoxels, ub0, ub1, ub2); break;
                case 4: voxelFace.Active = !IsVoxelAt(x + xOffset, y + yOffset + 1, z + zOffset, ref invoxels, ub0, ub1, ub2); break;
                case 5: voxelFace.Active = !IsVoxelAt(x + xOffset, y + yOffset - 1, z + zOffset, ref invoxels, ub0, ub1, ub2); break;
            }

        }

        static bool IsVoxelAt(int x, int y, int z, ref Voxel[] invoxels, int ub0, int ub1, int ub2)
        {
            if (x < 0 || x > ub0 || y < 0 || y > ub1 || z < 0 || z > ub2) return false;
            return invoxels[x + (ub0 + 1)*(y + (ub1 + 1)*z)].Active;
        }
    
        static void Quad(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight,Vector3 bottomLeft,int dir,float voxelSize,float overlapAmount, VoxelFace voxel, bool backFace,float selfShadeIntensity,List<Vector3> vertices, List<int> indexes, List<Color32> colors, List<Vector2> uvs)
        {
            int index = vertices.Count;

            Vector3 overlapBL = Vector3.zero;
            Vector3 overlapBR = Vector3.zero;
            Vector3 overlapTL = Vector3.zero;
            Vector3 overlapTR = Vector3.zero;

            if (overlapAmount > 0f)
            {
                switch (dir)
                {
                    case 0:
                    case 1:
                        overlapBL = new Vector3(-1, -1, 0)*overlapAmount;
                        overlapBR = new Vector3(-1, 1, 0)*overlapAmount;
                        overlapTR = new Vector3(1, 1, 0)*overlapAmount;
                        overlapTL = new Vector3(1, -1, 0)*overlapAmount;
                        break;
                    case 2:
                    case 3:
                        overlapBL = new Vector3(0, -1, -1)*overlapAmount;
                        overlapBR = new Vector3(0, -1, 1)*overlapAmount;
                        overlapTR = new Vector3(0, 1, 1)*overlapAmount;
                        overlapTL = new Vector3(0, 1, -1)*overlapAmount;
                        break;
                    case 4:
                    case 5:
                        overlapBL = new Vector3(-1, 0, -1)*overlapAmount;
                        overlapBR = new Vector3(1, 0, -1)*overlapAmount;
                        overlapTR = new Vector3(1, 0, 1)*overlapAmount;
                        overlapTL = new Vector3(-1, 0, 1)*overlapAmount;
                        break;


                }
            }

            vertices.Add((bottomLeft * voxelSize) + overlapBL);
            vertices.Add((bottomRight * voxelSize) + overlapBR);
            vertices.Add((topLeft * voxelSize) + overlapTL);
            vertices.Add((topRight * voxelSize) + overlapTR);

            Color temp = IntToColor(voxel.Color);
            int test1 = (((voxel.VertShade & Corners.TopRight) == Corners.TopRight) ? 1 : 0) + (((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft) ? 1 : 0) + (((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft) ? 1 : 0) + (((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight) ? 1 : 0);
            int test2 = (((voxel.VertShade & Corners.TopRight) == Corners.TopRight) ? 1 : 0) + (((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft) ? 1 : 0);
            int test3 = (((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft) ? 1 : 0) + (((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight) ? 1 : 0);
            if (((test2 > test3) && (test1>2)) || ((test2<=test3) && (test1==1)))
            {
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft ? (1f - selfShadeIntensity) : 1f), temp.a));
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft ? (1f - selfShadeIntensity) : 1f), temp.a));
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.TopRight) == Corners.TopRight ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.TopRight) == Corners.TopRight ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.TopRight) == Corners.TopRight ? (1f - selfShadeIntensity) : 1f), temp.a));
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight ? (1f - selfShadeIntensity) : 1f), temp.a));

                if (backFace)
                {
                    indexes.Add(index + 2);
                    indexes.Add(index);
                    indexes.Add(index + 1);
                    indexes.Add(index + 1);
                    indexes.Add(index + 3);
                    indexes.Add(index + 2);
                }
                else
                {
                    indexes.Add(index + 2);
                    indexes.Add(index + 3);
                    indexes.Add(index + 1);
                    indexes.Add(index + 1);
                    indexes.Add(index);
                    indexes.Add(index + 2);
                }
            }
            else
            {
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.TopLeft) == Corners.TopLeft ? (1f - selfShadeIntensity) : 1f), temp.a));
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.BottomLeft) == Corners.BottomLeft ? (1f - selfShadeIntensity) : 1f), temp.a));
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.TopRight) == Corners.TopRight ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.TopRight) == Corners.TopRight ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.TopRight) == Corners.TopRight ? (1f - selfShadeIntensity) : 1f), temp.a));
                colors.Add(new Color(temp.r * ((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight ? (1f - selfShadeIntensity) : 1f), temp.g * ((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight ? (1f - selfShadeIntensity) : 1f), temp.b * ((voxel.VertShade & Corners.BottomRight) == Corners.BottomRight ? (1f - selfShadeIntensity) : 1f), temp.a));

                if (backFace)
                {
                    indexes.Add(index + 3);
                    indexes.Add(index + 2);
                    indexes.Add(index);
                    indexes.Add(index);
                    indexes.Add(index + 1);
                    indexes.Add(index + 3);
                }
                else
                {
                    indexes.Add(index + 3);
                    indexes.Add(index + 1);
                    indexes.Add(index);
                    indexes.Add(index);
                    indexes.Add(index + 2);
                    indexes.Add(index + 3);
                }
            }

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
    }

#if UNITY_EDITOR
        public static void GenerateMarching(List<Vector3> vertices, List<Vector2> uvs, List<Color32> colors, List<int> indexes, ref Voxel[] invoxels, float voxelSize, int xOffset, int yOffset, int zOffset, int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity, EditorPaintMode paintMode)
#else
        public static void GenerateMarching(List<Vector3> vertices, List<Vector2> uvs, List<Color32> colors,
            List<int> indexes, ref Voxel[] invoxels, float voxelSize, int xOffset, int yOffset, int zOffset,
            int xSize, int ySize, int zSize, int ub0, int ub1, int ub2, float selfShadeIntensity)
#endif
        {
            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            indexes.Clear();

            bool[] corners = new bool[8];
            Vector3[] positions = new Vector3[8];

            int doubleXsize = xSize * 2;
            int doubleYsize = ySize * 2;
            int doubleZsize = zSize * 2;

            for (int subz = 0; subz < doubleZsize; subz++)
                for (int suby = 0; suby < doubleYsize; suby++)
                    for (int subx = 0; subx < doubleXsize; subx++)
                    {
                        int cubeIndex = 0;

                        corners[0] = IsVoxelAtMarching(ref invoxels, subx, suby, subz + 1, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[1] = IsVoxelAtMarching(ref invoxels, subx + 1, suby, subz + 1, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[2] = IsVoxelAtMarching(ref invoxels, subx + 1, suby, subz, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[3] = IsVoxelAtMarching(ref invoxels, subx, suby, subz, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[4] = IsVoxelAtMarching(ref invoxels, subx, suby + 1, subz + 1, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[5] = IsVoxelAtMarching(ref invoxels, subx + 1, suby + 1, subz + 1, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[6] = IsVoxelAtMarching(ref invoxels, subx + 1, suby + 1, subz, xOffset, yOffset, zOffset, ub0, ub1, ub2);
                        corners[7] = IsVoxelAtMarching(ref invoxels, subx, suby + 1, subz, xOffset, yOffset, zOffset, ub0, ub1, ub2);

                        if (corners[0])
                            cubeIndex += 1;
                        if (corners[1])
                            cubeIndex += 2;
                        if (corners[2])
                            cubeIndex += 4;
                        if (corners[3])
                            cubeIndex += 8;
                        if (corners[4])
                            cubeIndex += 16;
                        if (corners[5])
                            cubeIndex += 32;
                        if (corners[6])
                            cubeIndex += 64;
                        if (corners[7])
                            cubeIndex += 128;

                        if (cubeIndex == 0 || cubeIndex == 255)
                            continue;

                        Vector3 worldOffset = (new Vector3(subx, suby, subz));// + (Vector3.one * hs);

                        positions[0] = worldOffset + new Vector3(0, 0, 1);
                        positions[1] = worldOffset + new Vector3(1, 0, 1);
                        positions[2] = worldOffset + new Vector3(1, 0, 0);
                        positions[3] = worldOffset + new Vector3(0, 0, 0);
                        positions[4] = worldOffset + new Vector3(0, 1, 1);
                        positions[5] = worldOffset + new Vector3(1, 1, 1);
                        positions[6] = worldOffset + new Vector3(1, 1, 0);
                        positions[7] = worldOffset + new Vector3(0, 1, 0);

                        Vector3[] vertlist = new Vector3[12];
                        if (IsBitSet(edgeTable[cubeIndex], 1))
                            vertlist[0] = VertexInterp(positions[0], positions[1], corners[0], corners[1]);
                        if (IsBitSet(edgeTable[cubeIndex], 2))
                            vertlist[1] = VertexInterp(positions[1], positions[2], corners[1], corners[2]);
                        if (IsBitSet(edgeTable[cubeIndex], 4))
                            vertlist[2] = VertexInterp(positions[2], positions[3], corners[2], corners[3]);
                        if (IsBitSet(edgeTable[cubeIndex], 8))
                            vertlist[3] = VertexInterp(positions[3], positions[0], corners[3], corners[0]);
                        if (IsBitSet(edgeTable[cubeIndex], 16))
                            vertlist[4] = VertexInterp(positions[4], positions[5], corners[4], corners[5]);
                        if (IsBitSet(edgeTable[cubeIndex], 32))
                            vertlist[5] = VertexInterp(positions[5], positions[6], corners[5], corners[6]);
                        if (IsBitSet(edgeTable[cubeIndex], 64))
                            vertlist[6] = VertexInterp(positions[6], positions[7], corners[6], corners[7]);
                        if (IsBitSet(edgeTable[cubeIndex], 128))
                            vertlist[7] = VertexInterp(positions[7], positions[4], corners[7], corners[4]);
                        if (IsBitSet(edgeTable[cubeIndex], 256))
                            vertlist[8] = VertexInterp(positions[0], positions[4], corners[0], corners[4]);
                        if (IsBitSet(edgeTable[cubeIndex], 512))
                            vertlist[9] = VertexInterp(positions[1], positions[5], corners[1], corners[5]);
                        if (IsBitSet(edgeTable[cubeIndex], 1024))
                            vertlist[10] = VertexInterp(positions[2], positions[6], corners[2], corners[6]);
                        if (IsBitSet(edgeTable[cubeIndex], 2048))
                            vertlist[11] = VertexInterp(positions[3], positions[7], corners[3], corners[7]);

                        for (int i = 0; triTable[cubeIndex][i] != -1; i += 3)
                        {
                            int index = vertices.Count;

                            vertices.Add(vertlist[triTable[cubeIndex][i]] * voxelSize * 0.5f);
                            vertices.Add(vertlist[triTable[cubeIndex][i + 1]] * voxelSize * 0.5f);
                            vertices.Add(vertlist[triTable[cubeIndex][i + 2]] * voxelSize * 0.5f);

                            indexes.Add(index + 2);
                            indexes.Add(index + 1);
                            indexes.Add(index + 0);

                            // Convert to x,y,z position
                            int x = Mathf.FloorToInt(((float)subx) / 2.0f);
                            int y = Mathf.FloorToInt(((float)suby) / 2.0f);
                            int z = Mathf.FloorToInt(((float)subz) / 2.0f);

                            x = Mathf.Max(x, 0);
                            y = Mathf.Max(y, 0);
                            z = Mathf.Max(z, 0);
                            
                            Color useColor = Color.red;
                            try
                            {
                                if (invoxels[(x + xOffset) + (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + zOffset))].Active)
                                    useColor =
                                        invoxels[(x + xOffset) + (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + zOffset))]
                                            .Color;
                                else if (x + xOffset < ub0 &&
                                         invoxels[
                                             (x + 1 + xOffset) + (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + zOffset))
                                             ].Active)
                                    useColor =
                                        invoxels[
                                            (x + 1 + xOffset) + (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + zOffset))]
                                            .Color;
                                else if (y + yOffset < ub1 &&
                                         invoxels[
                                             (x + xOffset) + (ub0 + 1)*((y + 1 + yOffset) + (ub1 + 1)*(z + zOffset))]
                                             .Active)
                                    useColor =
                                        invoxels[
                                            (x + xOffset) +
                                            (ub0 + 1)*((y + 1 + yOffset) + (ub1 + 1)*(z + zOffset))].Color;
                                else if (z + zOffset < ub2 &&
                                         invoxels[
                                             (x + xOffset) +
                                             (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + 1 + zOffset))].Active)
                                    useColor =
                                        invoxels[
                                            (x + xOffset) +
                                            (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + 1 + zOffset))].Color;

                                //else if (x + xOffset >0 && invoxels[((x - 1) + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
                                //    useColor = invoxels[((x - 1) + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * (z + zOffset))].Color;
                                //else if (y + yOffset >0 && invoxels[(x + xOffset) + (ub0 + 1) * (((y - 1) + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
                                //    useColor = invoxels[(x + xOffset) + (ub0 + 1) * (((y - 1) + yOffset) + (ub1 + 1) * (z + zOffset))].Color;
                                //else if (z + zOffset >0 && invoxels[(x + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * ((z - 1) + zOffset))].Active)
                                //    useColor = invoxels[(x + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * ((z - 1) + zOffset))].Color;

                                else if (x + xOffset < ub0 && y + yOffset < ub1 &&
                                         invoxels[
                                             (x + 1 + xOffset) +
                                             (ub0 + 1)*((y + 1 + yOffset) + (ub1 + 1)*(z + zOffset))].Active)
                                    useColor =
                                        invoxels[
                                            (x + 1 + xOffset) +
                                            (ub0 + 1)*((y + 1 + yOffset) + (ub1 + 1)*(z + zOffset))]
                                            .Color;
                                else if (x + xOffset < ub0 && z + zOffset < ub2 &&
                                         invoxels[
                                             (x + 1 + xOffset) +
                                             (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + 1 + zOffset))]
                                             .Active)
                                    useColor =
                                        invoxels[
                                            (x + 1 + xOffset) +
                                            (ub0 + 1)*((y + yOffset) + (ub1 + 1)*(z + 1 + zOffset))]
                                            .Color;
                                else if (y + yOffset < ub1 && z + zOffset < ub2 &&
                                         invoxels[
                                             (x + xOffset) +
                                             (ub0 + 1)*
                                             ((y + 1 + yOffset) + (ub1 + 1)*(z + 1 + zOffset))]
                                             .Active)
                                    useColor =
                                        invoxels[
                                            (x + xOffset) +
                                            (ub0 + 1)*
                                            ((y + 1 + yOffset) + (ub1 + 1)*(z + 1 + zOffset))]
                                            .Color;

                                //else if (x + xOffset > 0 && y + yOffset > 0 && invoxels[((x - 1) + xOffset) + (ub0 + 1) * (((y - 1) + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
                                //    useColor = invoxels[((x - 1) + xOffset) + (ub0 + 1) * (((y - 1) + yOffset) + (ub1 + 1) * (z + zOffset))].Color;

                                //else if (x + xOffset > 0 && z + zOffset > 0 && invoxels[((x - 1) + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * ((z - 1) + zOffset))].Active)
                                //    useColor = invoxels[((x - 1) + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * ((z - 1) + zOffset))].Color;

                                //else if (y + yOffset >0 && z + zOffset >0 && invoxels[(x + xOffset) + (ub0 + 1) * (((y - 1) + yOffset) + (ub1 + 1) * ((z - 1) + zOffset))].Active)
                                //    useColor = invoxels[(x + xOffset) + (ub0 + 1) * (((y - 1) + yOffset) + (ub1 + 1) * ((z - 1) + zOffset))].Color;


                                else if (x + xOffset < ub0 && y + yOffset < ub1 &&
                                         z + zOffset < ub2 &&
                                         invoxels[
                                             (x + 1 + xOffset) +
                                             (ub0 + 1)*
                                             ((y + 1 + yOffset) + (ub1 + 1)*(z + 1 + zOffset))]
                                             .Active)
                                    useColor =
                                        invoxels[
                                            (x + 1 + xOffset) +
                                            (ub0 + 1)*
                                            ((y + 1 + yOffset) + (ub1 + 1)*(z + 1 + zOffset))
                                            ].Color;
                            }
                            catch (Exception)
                            {
                                Debug.Log("x: " + x + " y: " + y + " z: " + z + " ub0: " + ub0 + " ub1: " + ub1 + " ub2: " +ub2);
                            }

                            colors.Add(useColor);
                            colors.Add(useColor);
                            colors.Add(useColor);
                            //colors.Add(Color.white);
                            //colors.Add(Color.white);
                            //colors.Add(Color.white);

                            uvs.Add(Vector2.zero);
                            uvs.Add(Vector2.zero);
                            uvs.Add(Vector2.zero);
                        }

                    }

           
        }

        private static bool IsBitSet(int b, int pos)
        {
            return ((b & pos) == pos);
        }

        private static Vector3 VertexInterp(Vector3 p1, Vector3 p2, bool valp1, bool valp2)
        {
            //0 < 0.9 = true (0 = true) // 1 < 0.9 = false (1)
            // 0.9 - 0 = 0.9 = false // 0.9 - 1 = true

            if (!valp1)
                return (p1);
            if (!valp2)
                return (p2);

            Vector3 p;
            p.x = p1.x + (p2.x - p1.x);
            p.y = p1.y + (p2.y - p1.y);
            p.z = p1.z + (p2.z - p1.z);
            return (p);
        }

        static bool IsVoxelAtMarching(ref Voxel[] voxels, int subx, int suby, int subz, int xOffset, int yOffset, int zOffset, int ub0, int ub1, int ub2)
        {
            // Convert to x,y,z position
            int x = Mathf.FloorToInt(((float)subx) / 2.0f);
            int y = Mathf.FloorToInt(((float)suby) / 2.0f);
            int z = Mathf.FloorToInt(((float)subz) / 2.0f);

            if (x + xOffset < 0 || y + yOffset < 0 || z + zOffset < 0 || x + xOffset > ub0 || y + yOffset > ub1 || z + zOffset > ub2)
                return false;
            if (x + xOffset == 0 && voxels[(x + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
                return false;
            if (y + yOffset == 0 && voxels[(x + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
                return false;
            if (z + zOffset == 0 && voxels[(x + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
                return false;
            // if (x == 0 || y == 0 || z == 0) return false;
            if (voxels[(x + xOffset) + (ub0 + 1) * ((y + yOffset) + (ub1 + 1) * (z + zOffset))].Active)
            {
                return true;
            }
            return false;
        }

        private static int ColorToInt(Color32 color)
        {
            return ((color.a << 24) | (color.r << 16) |
                    (color.g << 8) | (color.b << 0));
        }

        private static Color32 IntToColor(int color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return new Color32(r, g, b, a);
        }

        #region Marching Cubes Data
        private static int[] edgeTable = new int[256] {
                0x0, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
                0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
                0x190, 0x99, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
                0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
                0x230, 0x339, 0x33, 0x13a, 0x636, 0x73f, 0x435, 0x53c,
                0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
                0x3a0, 0x2a9, 0x1a3, 0xaa, 0x7a6, 0x6af, 0x5a5, 0x4ac,
                0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
                0x460, 0x569, 0x663, 0x76a, 0x66, 0x16f, 0x265, 0x36c,
                0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
                0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff, 0x3f5, 0x2fc,
                0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
                0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55, 0x15c,
                0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
                0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc,
                0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
                0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
                0xcc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
                0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
                0x15c, 0x55, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
                0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
                0x2fc, 0x3f5, 0xff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
                0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
                0x36c, 0x265, 0x16f, 0x66, 0x76a, 0x663, 0x569, 0x460,
                0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
                0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa, 0x1a3, 0x2a9, 0x3a0,
                0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
                0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33, 0x339, 0x230,
                0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
                0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99, 0x190,
                0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
                0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0
        };
        private static int[][] triTable = new int[256][] {
                new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
                new int[] { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 },
                new int[] { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 },
                new int[] { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
                new int[] { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 },
                new int[] { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 },
                new int[] { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 },
                new int[] { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
                new int[] { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 },
                new int[] { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 },
                new int[] { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
                new int[] { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 },
                new int[] { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 },
                new int[] { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 },
                new int[] { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 },
                new int[] { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 },
                new int[] { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 },
                new int[] { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
                new int[] { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 },
                new int[] { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
                new int[] { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 },
                new int[] { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 },
                new int[] { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 },
                new int[] { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
                new int[] { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 },
                new int[] { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 },
                new int[] { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
                new int[] { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 },
                new int[] { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 },
                new int[] { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 },
                new int[] { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
                new int[] { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 },
                new int[] { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 },
                new int[] { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
                new int[] { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 },
                new int[] { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 },
                new int[] { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 },
                new int[] { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 },
                new int[] { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 },
                new int[] { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
                new int[] { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 },
                new int[] { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
                new int[] { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 },
                new int[] { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 },
                new int[] { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 },
                new int[] { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 },
                new int[] { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 },
                new int[] { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 },
                new int[] { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 },
                new int[] { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 },
                new int[] { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 },
                new int[] { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 },
                new int[] { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 },
                new int[] { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 },
                new int[] { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 },
                new int[] { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 },
                new int[] { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 },
                new int[] { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 },
                new int[] { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 },
                new int[] { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 },
                new int[] { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 },
                new int[] { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 },
                new int[] { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 },
                new int[] { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 },
                new int[] { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 },
                new int[] { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 },
                new int[] { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 },
                new int[] { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 },
                new int[] { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 },
                new int[] { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 },
                new int[] { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 },
                new int[] { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 },
                new int[] { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 },
                new int[] { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 },
                new int[] { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 },
                new int[] { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 },
                new int[] { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 },
                new int[] { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 },
                new int[] { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 },
                new int[] { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 },
                new int[] { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 },
                new int[] { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 },
                new int[] { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 },
                new int[] { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 },
                new int[] { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 },
                new int[] { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 },
                new int[] { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 },
                new int[] { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 },
                new int[] { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 },
                new int[] { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 },
                new int[] { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 },
                new int[] { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 },
                new int[] { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 },
                new int[] { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 },
                new int[] { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 },
                new int[] { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 },
                new int[] { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 },
                new int[] { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 },
                new int[] { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 },
                new int[] { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 },
                new int[] { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 },
                new int[] { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 },
                new int[] { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 },
                new int[] { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 },
                new int[] { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 },
                new int[] { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 },
                new int[] { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 },
                new int[] { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 },
                new int[] { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
        };
        #endregion
    }

}