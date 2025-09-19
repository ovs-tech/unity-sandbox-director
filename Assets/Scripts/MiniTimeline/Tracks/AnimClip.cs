using System;
using UnityEngine;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Wrap mode for animation clips
    /// </summary>
    public enum AnimWrapMode
    {
        Loop,
        Clamp,
        PingPong
    }
    
    /// <summary>
    /// Animation clip data for timeline
    /// Contains reference to animation and playback settings
    /// </summary>
    [Serializable]
    public class AnimClip : MiniTimeline.Core.MiniClipBase
    {
        /// <summary>
        /// Animation clip asset reference (Addressables key or scene path)
        /// </summary>
        public string animationAsset;
        
        /// <summary>
        /// Playback speed multiplier
        /// </summary>
        public float speed = 1f;
        
        /// <summary>
        /// Animation wrap mode
        /// </summary>
        public AnimWrapMode wrapMode = AnimWrapMode.Clamp;
        
        /// <summary>
        /// Animation layer index (for multi-layer animation)
        /// </summary>
        public int layer = 0;
        
        /// <summary>
        /// Fade in duration when clip starts
        /// </summary>
        public float fadeIn = 0.1f;
        
        /// <summary>
        /// Fade out duration when clip ends
        /// </summary>
        public float fadeOut = 0.1f;
        
        /// <summary>
        /// Clip weight (for blending multiple clips)
        /// </summary>
        public float weight = 1f;
        
        /// <summary>
        /// Start offset within the animation clip
        /// </summary>
        public float clipOffset = 0f;
        
        // Cached animation clip (loaded at runtime)
        [NonSerialized]
        public AnimationClip cachedClip;
        
        /// <summary>
        /// Get local time within the animation clip accounting for speed and offset
        /// </summary>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Local animation time</returns>
        public float GetLocalAnimationTime(float globalTime)
        {
            if (!Contains(globalTime)) return 0f;
            
            float localTime = (globalTime - Start) * speed + clipOffset;
            
            if (cachedClip != null)
            {
                switch (wrapMode)
                {
                    case AnimWrapMode.Loop:
                        localTime = localTime % cachedClip.length;
                        break;
                    case AnimWrapMode.Clamp:
                        localTime = Mathf.Clamp(localTime, 0f, cachedClip.length);
                        break;
                    case AnimWrapMode.PingPong:
                        float pingPongTime = localTime % (cachedClip.length * 2f);
                        localTime = pingPongTime > cachedClip.length 
                            ? (cachedClip.length * 2f) - pingPongTime 
                            : pingPongTime;
                        break;
                }
            }
            
            return localTime;
        }
        
        /// <summary>
        /// Get normalized animation time (0-1)
        /// </summary>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Normalized animation time</returns>
        public float GetNormalizedAnimationTime(float globalTime)
        {
            if (cachedClip == null || cachedClip.length <= 0f) return 0f;
            
            return GetLocalAnimationTime(globalTime) / cachedClip.length;
        }
        
        /// <summary>
        /// Get current fade weight based on fade in/out settings
        /// </summary>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Fade weight (0-1)</returns>
        public float GetFadeWeight(float globalTime)
        {
            if (!Contains(globalTime)) return 0f;
            
            float localTime = globalTime - Start;
            float fadeWeight = 1f;
            
            // Fade in
            if (fadeIn > 0f && localTime < fadeIn)
            {
                fadeWeight *= localTime / fadeIn;
            }
            
            // Fade out
            if (fadeOut > 0f && localTime > (Duration - fadeOut))
            {
                float fadeOutTime = Duration - localTime;
                fadeWeight *= fadeOutTime / fadeOut;
            }
            
            return Mathf.Clamp01(fadeWeight * weight);
        }
    }
}