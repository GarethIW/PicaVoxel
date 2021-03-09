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
using System.Linq;
using System.Net.Mail;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicaVoxel
{
    public class EditorPersistence
    {
        public static int SelectedColor = 0;
        public static byte SelectedValue = 128;

        public static bool BrushReplace = false;

        public static PicaVoxelBox SelectBox = new PicaVoxelBox(0, 0, 0, 31, 31, 31);

        public static EditorCursorMode CursorMode = EditorCursorMode.Subtract;

        public static Voxel[] Brush;
        public static PicaVoxelPoint BrushSize;
        public static AnchorX BrushAnchorX = AnchorX.Left;
        public static AnchorY BrushAnchorY = AnchorY.Bottom;
        public static AnchorZ BrushAnchorZ = AnchorZ.Front;

        public static Voxel[] AnimFrameClipboard;
        public static PicaVoxelPoint AnimFrameClipboardSize;
    }

}