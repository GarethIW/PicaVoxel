/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////

using System.Threading;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicaVoxel
{
    [SelectionBase]
    public class QubicleImport : MonoBehaviour
    {
        public string ImportedFile;
        public float ImportedVoxelSize = 0.1f;

    }
}