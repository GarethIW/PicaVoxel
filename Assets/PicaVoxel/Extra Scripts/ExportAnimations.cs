#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using PicaVoxel;

/// <summary>
/// Editor script for generating AnimationClips from PicaVoxel Volumes.
/// by Evan Kawa (timmypowergamer) - Nov 2015. Feel free to use and modify.
/// </summary>
[RequireComponent(typeof(Volume))]
[ExecuteInEditMode]
public class ExportAnimations : MonoBehaviour {

#if UNITY_EDITOR
    [System.Serializable]
    public class ClipInfo
    {
        /// <summary>
        /// The Name of the generated AnimationClip
        /// </summary>
        public string ClipName;

        /// <summary>
        /// An ordered list of Frames to play in this animation. Comma-separated, can use '-' to define a range. (ex. "1-5,10-12,1,2")
        /// </summary>
        public string Frames;

        /// <summary>
        /// Should this clip loop?
        /// </summary>
        public bool loopTime = true;

        /// <summary>
        /// Frames Per Second at which this clip should play.
        /// </summary>
        public float FrameRate = 10f;
    }

    public ClipInfo[] Clips;

    //This will use AnimationEvents to set frames through Volume.SetFrame(frameNum). Results in smaller, more manageable AnimationClips,
    // but they can't be previewed in-editor.
    private bool useAnimEvents = false;

    //until I make a nice inspector for this with a button, checking this in the editor will cause the clips to be exported.
    public bool exportNow = false;

    void Update()
    {
        if (exportNow)
        {
            exportNow = false;
            ExportClips();
        }
    }

    /// <summary>
    /// Creates an AnimationClip for each ClipInfo defined on this script, in a directory chosen by the user.
    /// </summary>
    public void ExportClips()
    {
        //Start by enumerating the total number of PicaVoxel Frames in this volume;
        Volume volume = GetComponent<Volume>();
        int frameCount = 0;
        if (volume == null)
        {
            //if there is no Volume component, it is a mesh-only, and thus we need to manually check for frames.
            for (int child = 0; child < transform.childCount; child++)
            {
                if (transform.GetChild(child).name.ToLower().StartsWith("frame"))
                {
                    frameCount++;
                }
            }
        }
        else
        {
            frameCount = volume.Frames.Count;
        }

        //brings up the SaveToFolder dialog, so we can pick where to save the clips
        string assetPath = EditorUtility.SaveFolderPanel("Save Animation Clips", Application.dataPath, "Animations");
        if (string.IsNullOrEmpty(assetPath)) return;    //this would mean they cancelled

        //we need the assetPath to be relative to the Assets folder
        assetPath = assetPath.Substring(assetPath.LastIndexOf("Assets/"));
        assetPath += "/";

        //sanity check
        if (!assetPath.StartsWith("Assets/"))
        {
            Debug.LogWarning("Can not save Assets outside of Assets folder. No clips exported.");
            return;
        }

        AssetDatabase.StartAssetEditing();  //This tells unity to batch all the imports that would normally occur when creating/modifying asset files.
                                            // Speeds things up considerably.

        //Now let's loop through all the ClipInfo's we have defined on the export script...
        for (int i = 0; i < Clips.Length; i++)
        {
            ClipInfo clipInfo = Clips[i];
            if (clipInfo != null)
            {
                //generate a list of all the frame numbers to include in this clip;
                List<int> frameOrder = new List<int>();
                string[] frameList = clipInfo.Frames.Split(',');
                List<int> tempList = null;
                foreach (string s in frameList)
                {
                    tempList = getFrameRange(s);
                    if (tempList != null)
                    {
                        frameOrder.AddRange(tempList);
                    }
                }

                bool createNew = false;

                //check if the clip already exists in the directory. If so, we can just update it instead of creating an new one.
                AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath + clipInfo.ClipName + ".anim");
                if (newClip == null)
                {
                    newClip = new AnimationClip();
                    createNew = true;
                }
                else
                {
                    newClip.ClearCurves();
                    AnimationUtility.SetAnimationEvents(newClip, null);
                }


                if (useAnimEvents) //generate a list of AnimationEvents to set the frames
                {
                    List<AnimationEvent> eventList = createEvents(frameOrder, clipInfo.FrameRate);
                    AnimationUtility.SetAnimationEvents(newClip, eventList.ToArray());
                }
                else
                {
                    //Go through every PicaVoxel Frame in the volume and bind an AnimationCurve to it. Even if the frame is not actually included in this clip,
                    // we need to turn them off on the first frame.
                    for (int frameNumber = 1; frameNumber <= frameCount; frameNumber++)
                    {
                        AnimationCurve newCurve = createMeshOnlyCurve(frameNumber, frameOrder, clipInfo.FrameRate);
                        newClip.SetCurve("Frame " + frameNumber.ToString(), typeof(GameObject), "m_IsActive", newCurve);
                    }
                }

                
                
                //Set some settings on the clip...
                newClip.frameRate = clipInfo.FrameRate;
                AnimationClipSettings settings = new AnimationClipSettings();
                settings.loopTime = clipInfo.loopTime;
                settings.stopTime = newClip.length;
                AnimationUtility.SetAnimationClipSettings(newClip, settings);

                //if this is a new asset, create it in the asset database
                if (createNew)
                {
                    AssetDatabase.CreateAsset(newClip, assetPath + clipInfo.ClipName + ".anim");
                }
            }
        }

        AssetDatabase.SaveAssets();
        //tells Unity to do all of the imports it needs to now.
        AssetDatabase.StopAssetEditing();
    }

    /// <summary>
    /// This creates AnimationEvents to control the frame switching via Volume.SetFrame(frameNumber).
    ///  However, AnimationEvents do not get triggered when previewing in-editor, so this will only work at runtime.
    /// </summary>
    /// <param name="frameOrder"></param>
    /// <param name="frameRate"></param>
    /// <returns></returns>
    List<AnimationEvent> createEvents(List<int> frameOrder, float frameRate)
    {
        float frameTime = 1f / frameRate;

        List<AnimationEvent> events = new List<AnimationEvent>();

        for (int i = 0; i < frameOrder.Count; i++)
        {
            if (frameOrder[i] > 0)
            {
                AnimationEvent newEvent = new AnimationEvent();
                newEvent.functionName = "SetFrame";
                newEvent.intParameter = frameOrder[i] - 1;
                newEvent.time = (float)i * frameTime;
                newEvent.messageOptions = SendMessageOptions.DontRequireReceiver;
                events.Add(newEvent);
            }
        }

        //basically, just duplicate the final frame so that it has time to play for a full frame length before looping back to the beginning.
        AnimationEvent finalEvent = new AnimationEvent();
        finalEvent.functionName = "SetFrame";
        finalEvent.intParameter = frameOrder[frameOrder.Count - 1] - 1;
        finalEvent.time = (float)frameOrder.Count * frameTime;
        events.Add(finalEvent);
        return events;
    }

    /// <summary>
    /// Creates an AnimationCurve that turns on and off a specific frame at intervals defined in a list of frame numbers
    /// </summary>
    /// <param name="frameNumber">The number of the PicaVoxel Frame to be controlled by this curve</param>
    /// <param name="frameOrder">An ordered list of frames in this animation</param>
    /// <param name="frameRate">The frames-per-second at which the animation should play</param>
    /// <returns>An AnimationCurve object</returns>
    AnimationCurve createMeshOnlyCurve(int frameNumber, List<int> frameOrder, float frameRate)
    {
        AnimationCurve curve = new AnimationCurve();
        float frameTime = 1f / frameRate;  //the time in seconds between each frame

        Keyframe key = new Keyframe(0, 0);
        key.tangentMode = 31;   //this doesn't seem to really effect much. 31 = constant tangent mode, but the behavior is really controlled by the in and out tangents...
        key.inTangent = float.NegativeInfinity; //infinity causes an instant jump to the next frame
        key.outTangent = float.PositiveInfinity;

        //Make the first keyframe set all frames off, unless the frame is supposed to be on.
        if (frameOrder[0] != frameNumber)
        {
            curve.AddKey(key);
        }

        bool isOn = false;

        //loop through all the frames in the orderedList
        for (int i = 0; i < frameOrder.Count; i++)
        {
            //if the frame in the list matches this one, turn it on
            if (frameOrder[i] == frameNumber)
            {
                key.time = (float)i * frameTime;
                key.value = 1f;
                curve.AddKey(key);
                isOn = true;
            }
            else if(isOn)   //if this frame was turned on last keyframe, turn it off now.
            {
                key.time = (float)i * frameTime;
                key.value = 0;
                curve.AddKey(key);
                isOn = false;
            }
        }

        //final key, a duplicate of the last one in the orderedList, so that the full length of the frame plays out before looping back to the beginning.
        if (isOn)
        {
            key.time = (float)frameOrder.Count * frameTime;
            key.value = 1;
            curve.AddKey(key);
        }

        return curve;
    }

    /// <summary>
    /// Gets a list of frame id's from a properly formatted string.
    /// </summary>
    /// <param name="input">the string to parse</param>
    /// <returns>List of frame numbers</returns>
    List<int> getFrameRange(string input)
    {
        input = input.Replace(" ", ""); //remove spaces
        List<int> returnRange = new List<int>();

        //first check if we have defined a frame range
        if (input.Contains("-"))    
        {
            string[] range = input.Split('-');
            if (range.Length == 2)      //make sure it is only 2 numbers separated by a '-'
            {
                int beginRange = 0;
                if (int.TryParse(range[0], out beginRange))   //make sure both are actually valid numbers
                {
                    int endRange = 0;
                    if (int.TryParse(range[1], out endRange))
                    {
                        for(int i = beginRange; i <= endRange; i++)
                        {
                            //add all frame numbers in the range
                            returnRange.Add(i);
                        }
                        return returnRange;
                    }
                }
            }
            Debug.LogError("Invalid input format for frame range: '" + input + "'");
        }
        else
        {
            //check if it is a single frame
            int num = 0;
            if (int.TryParse(input, out num))
            {
                returnRange.Add(num);
                return returnRange;
            }

            Debug.LogError("Invalid input for frame definition: '" + input + "'");
        }

        return null;
    }

#endif
}
#endif
