using System;
using UnityEngine;
using Sirenix.OdinSerializer;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Wrap mode for animation clips
    /// </summary>
    public enum AnimWrapMode
    {
        /// <summary>
        /// Animation plays once and stops
        /// </summary>
        Once,
        
        /// <summary>
        /// Animation loops continuously
        /// </summary>
        Loop,
        
        /// <summary>
        /// Animation plays back and forth
        /// </summary>
        PingPong,
        
        /// <summary>
        /// Animation clamps to the last frame
        /// </summary>
        ClampForever,
        
        /// <summary>
        /// Animation uses default wrap mode from the asset
        /// </summary>
        Default
    }
    
    /// <summary>
    /// Animation clip for playing Unity animations on timeline
    /// Supports Unity Animation system with blending and timing control
    /// </summary>
    [Serializable]
    public class AnimClip : MiniClipBase
    {
        [Header("Animation Asset")]
        [Tooltip("Path to the animation asset (supports Addressables with 'addr:' prefix)")]
        [OdinSerialize]
        public string animationAsset;
        
        [Header("Playback Settings")]
        [Tooltip("Playback speed multiplier (1.0 = normal speed)")]
        [OdinSerialize]
        public float speed = 1.0f;
        
        [Tooltip("How the animation should wrap when it reaches the end")]
        [OdinSerialize]
        public AnimWrapMode wrapMode = AnimWrapMode.Once;
        
        [Tooltip("Animation layer for blending (higher layers override lower ones)")]
        [OdinSerialize]
        public int layer = 0;
        
        [Header("Blending")]
        [Tooltip("Fade-in duration in seconds")]
        [Range(0f, 5f)]
        [OdinSerialize]
        public float fadeIn = 0f;
        
        [Tooltip("Fade-out duration in seconds")]
        [Range(0f, 5f)]
        [OdinSerialize]
        public float fadeOut = 0f;
        
        [Tooltip("Weight of this animation clip (0-1)")]
        [Range(0f, 1f)]
        [OdinSerialize]
        public float weight = 1f;
        
        [Header("Timing")]
        [Tooltip("Offset into the animation clip (in seconds)")]
        [OdinSerialize]
        public float clipOffset = 0f;
        
        #region Animation Specific Methods
        
        /// <summary>
        /// Get the effective animation time considering speed and offset
        /// </summary>
        /// <param name="globalTime">Timeline time</param>
        /// <returns>Animation time to sample</returns>
        public float GetAnimationTime(float globalTime)
        {
            float localTime = globalTime - Start;
            float scaledTime = localTime * speed;
            return clipOffset + scaledTime;
        }
        
        /// <summary>
        /// Get the blend weight at the given time considering fade in/out
        /// </summary>
        /// <param name="globalTime">Timeline time</param>
        /// <returns>Effective weight (0-1)</returns>
        public float GetBlendWeight(float globalTime)
        {
            if (!Contains(globalTime))
                return 0f;
                
            float localTime = globalTime - Start;
            float effectiveWeight = weight;
            
            // Apply fade in
            if (fadeIn > 0f && localTime < fadeIn)
            {
                effectiveWeight *= Mathf.Clamp01(localTime / fadeIn);
            }
            
            // Apply fade out
            if (fadeOut > 0f && localTime > (Duration - fadeOut))
            {
                float fadeOutProgress = (Duration - localTime) / fadeOut;
                effectiveWeight *= Mathf.Clamp01(fadeOutProgress);
            }
            
            return effectiveWeight;
        }
        
        /// <summary>
        /// Check if this clip should be playing at the given time
        /// </summary>
        /// <param name="globalTime">Timeline time</param>
        /// <returns>True if clip should be active</returns>
        public bool ShouldPlay(float globalTime)
        {
            return Contains(globalTime) && GetBlendWeight(globalTime) > 0f;
        }
        
        /// <summary>
        /// Get animation state info for this clip at the given time
        /// </summary>
        /// <param name="globalTime">Timeline time</param>
        /// <returns>Animation state data</returns>
        public AnimationState GetAnimationState(float globalTime)
        {
            return new AnimationState
            {
                clipId = Id,
                animationTime = GetAnimationTime(globalTime),
                weight = GetBlendWeight(globalTime),
                speed = speed,
                layer = layer,
                wrapMode = wrapMode,
                isActive = ShouldPlay(globalTime)
            };
        }
        
        /// <summary>
        /// Validate clip settings and fix common issues
        /// </summary>
        public void ValidateSettings()
        {
            // Ensure minimum duration
            if (Duration < MiniTimelineConstants.MIN_CLIP_DURATION)
            {
                Duration = MiniTimelineConstants.MIN_CLIP_DURATION;
            }
            
            // Clamp values to valid ranges
            speed = Mathf.Max(0.01f, speed);
            weight = Mathf.Clamp01(weight);
            fadeIn = Mathf.Max(0f, fadeIn);
            fadeOut = Mathf.Max(0f, fadeOut);
            clipOffset = Mathf.Max(0f, clipOffset);
            
            // Ensure fade times don't exceed clip duration
            float maxFadeTime = Duration * 0.5f;
            fadeIn = Mathf.Min(fadeIn, maxFadeTime);
            fadeOut = Mathf.Min(fadeOut, maxFadeTime);
        }
        
        #endregion
        
        #region Unity Editor Support
        
        /// <summary>
        /// Get friendly display name for the clip
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(animationAsset))
            {
                // Extract filename from path
                string fileName = System.IO.Path.GetFileNameWithoutExtension(animationAsset);
                return string.IsNullOrEmpty(fileName) ? Id : fileName;
            }
            
            return string.IsNullOrEmpty(Id) ? "Animation Clip" : Id;
        }
        
        /// <summary>
        /// Get color for UI display based on layer
        /// </summary>
        public Color GetDisplayColor()
        {
            // Generate color based on layer for easy visual identification
            float hue = (layer * 0.3f) % 1f;
            return Color.HSVToRGB(hue, 0.6f, 0.8f);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Animation state data for a clip at a specific time
    /// </summary>
    [Serializable]
    public struct AnimationState
    {
        public string clipId;
        public float animationTime;
        public float weight;
        public float speed;
        public int layer;
        public AnimWrapMode wrapMode;
        public bool isActive;
        
        /// <summary>
        /// Convert to Unity WrapMode enum
        /// </summary>
        public WrapMode ToUnityWrapMode()
        {
            return wrapMode switch
            {
                AnimWrapMode.Once => WrapMode.Once,
                AnimWrapMode.Loop => WrapMode.Loop,
                AnimWrapMode.PingPong => WrapMode.PingPong,
                AnimWrapMode.ClampForever => WrapMode.ClampForever,
                AnimWrapMode.Default => WrapMode.Default,
                _ => WrapMode.Once
            };
        }
    }
}