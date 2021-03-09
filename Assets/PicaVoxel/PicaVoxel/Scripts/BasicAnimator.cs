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
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

namespace PicaVoxel
{
    [AddComponentMenu("PicaVoxel/Utilities/Basic Animator")]
    [Serializable]
    public class BasicAnimator : MonoBehaviour
    {
        public float Interval = 0.1f;
        public bool PingPong = false;
        public bool Loop = true;
        public bool RandomStartFrame = false;
        public bool PlayOnAwake = true;
        public bool IsPlaying = false;
        public int CurrentFrame = 0;
        public int NumFrames = 0;

        private Volume voxelObject;
        private float currentFrameTime = 0f;
        private int dir = 1;

        private bool isBakedVolume = false;

        private void Awake()
        {
            voxelObject = GetComponent<Volume>();

            // If we can't find a Volume component, check to see if this is a baked Volume
            if (voxelObject == null)
            {
                for(int i=0;i<transform.childCount;i++)
                    if (transform.GetChild(i).name.ToLower().StartsWith("frame")) NumFrames++;

                // If we found some Frame objects, we're on a baked Volume
                if (NumFrames > 0) isBakedVolume = true;
            }
            else
            {
                NumFrames = voxelObject.NumFrames;
            }

            Reset();
            if (PlayOnAwake) Play();
        }

        // Update is called once per frame
        private void Update()
        {
            if ((voxelObject == null && !isBakedVolume) || (NumFrames<=0) || !IsPlaying) return;

            currentFrameTime += Time.deltaTime;
            if (currentFrameTime >= Interval)
            {
                currentFrameTime = 0;

                CurrentFrame += dir;
                if (dir == 1 && CurrentFrame == NumFrames)
                {
                    if (PingPong)
                    {
                        CurrentFrame--;
                        dir = -1;
                    }
                    else CurrentFrame = 0;

                    if (!Loop) Reset();
                }
                else if (dir == -1 && CurrentFrame == 0)
                {
                    dir = 1;
                    if (!Loop) Reset();
                }

                if(!isBakedVolume)
                    voxelObject.SetFrame(CurrentFrame);
                else
                {
                    int thisFrame = 0;
                    for(int i=0;i<transform.childCount;i++)
                        if (transform.GetChild(i).name.ToLower().StartsWith("frame"))
                        {
                            transform.GetChild(i).gameObject.SetActive(false);
                            if (CurrentFrame == thisFrame)
                            {
                                transform.GetChild(i).gameObject.SetActive(true);
                            }

                            thisFrame++;
                        }
                }
            }
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Reset()
        {
            IsPlaying = false;
            if (RandomStartFrame) CurrentFrame = Random.Range(0, NumFrames-1);
            else CurrentFrame = 0;
            currentFrameTime = 0;
        }
    }
}