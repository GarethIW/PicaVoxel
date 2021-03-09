/////////////////////////////////////////////////////////////////////////
// 
// PicaVoxel - The tiny voxel engine for Unity - http://picavoxel.com
// By Gareth Williams - @garethiw - http://gareth.pw
// 
// Source code distributed under standard Asset Store licence:
// http://unity3d.com/legal/as_terms
//
/////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;

namespace PicaVoxel
{
    [CustomEditor(typeof (Exploder))]
    public class ExploderEditor : Editor
    {
        private string tag;
        private float explosionRadius;
        private float particleVelocity;
        private Exploder.ExplodeTargets explodeTarget;
        private Exploder.ExplodeValueFilterOperation valueFilterOperation;
        private int valueFilter;

        private Exploder voxelExploder;

        private void OnEnable()
        {
            voxelExploder = (Exploder) target;

            tag = voxelExploder.Tag;
            explosionRadius = voxelExploder.ExplosionRadius;
            particleVelocity = voxelExploder.ParticleVelocity;
            explodeTarget = voxelExploder.ExplodeTarget;
            valueFilterOperation = voxelExploder.ValueFilterOperation;
            valueFilter = voxelExploder.ValueFilter;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            tag = EditorGUILayout.TextField("Tag:", tag);
            if (tag != voxelExploder.Tag) voxelExploder.Tag = tag;
            explosionRadius = EditorGUILayout.FloatField("Explosion Radius:", explosionRadius);
            if (explosionRadius != voxelExploder.ExplosionRadius) voxelExploder.ExplosionRadius = explosionRadius;
            particleVelocity = EditorGUILayout.FloatField("Particle Velocity:", particleVelocity);
            if (particleVelocity != voxelExploder.ParticleVelocity) voxelExploder.ParticleVelocity = particleVelocity;
            explodeTarget = (Exploder.ExplodeTargets) EditorGUILayout.EnumPopup("Targets:", explodeTarget);
            if (explodeTarget != voxelExploder.ExplodeTarget) voxelExploder.ExplodeTarget = explodeTarget;
            EditorGUILayout.LabelField("Explode when voxel Value is");
            EditorGUILayout.BeginHorizontal();
            valueFilterOperation = (Exploder.ExplodeValueFilterOperation)EditorGUILayout.EnumPopup(valueFilterOperation);
            if (valueFilterOperation != voxelExploder.ValueFilterOperation) voxelExploder.ValueFilterOperation = valueFilterOperation;
            valueFilter = EditorGUILayout.IntField(valueFilter);
            if (valueFilter != voxelExploder.ValueFilter)
            {
                if (valueFilter < 0) valueFilter = 0;
                if (valueFilter >255) valueFilter = 255;
                voxelExploder.ValueFilter = valueFilter;
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Explode!"))
            {
                voxelExploder.Explode();
                foreach (GameObject o in GameObject.FindGameObjectsWithTag("PicaVoxelVolume"))
                    o.GetComponent<Volume>().UpdateChunks(true);
            }

        }
    }
}