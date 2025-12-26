using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinSerializer;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Blend mode for morph clips
    /// </summary>
    public enum MorphBlendMode
    {
        Additive,    // Add values together (clamped)
        Override,    // Higher priority clips override lower ones
        Multiply     // Multiply values
    }
    
    /// <summary>
    /// Single morph key data
    /// </summary>
    [Serializable]
    public struct MorphKey
    {
        /// <summary>
        /// Blendshape name or ID
        /// </summary>
        [OdinSerialize]
        public string id;
        
        /// <summary>
        /// Start value (0-100)
        /// </summary>
        [OdinSerialize]
        public float startValue;
        
        /// <summary>
        /// End value (0-100)
        /// </summary>
        [OdinSerialize]
        public float endValue;
        
        /// <summary>
        /// Interpolation curve
        /// </summary>
        public AnimationCurve curve;
        
        /// <summary>
        /// Get interpolated value at normalized time
        /// </summary>
        /// <param name="normalizedTime">Time from 0-1</param>
        /// <returns>Interpolated morph value</returns>
        public float GetValue(float normalizedTime)
        {
            float curveValue = curve?.Evaluate(normalizedTime) ?? normalizedTime;
            return Mathf.Lerp(startValue, endValue, curveValue);
        }
    }
    
    /// <summary>
    /// Morph clip with keyframe animation
    /// Can animate multiple blendshapes with curves
    /// </summary>
    [Serializable]
    public partial class MorphKeyClip : MiniTimeline.Core.MiniClipBase
    {
        /// <summary>
        /// Blend mode for this clip
        /// </summary>
        [OdinSerialize]
        public MorphBlendMode blendMode = MorphBlendMode.Additive;
        
        /// <summary>
        /// Priority for Override blend mode (higher = more priority)
        /// </summary>
        [OdinSerialize]
        public int priority = 0;
        
        /// <summary>
        /// Overall clip weight multiplier
        /// </summary>
        [OdinSerialize]
        public float weight = 1f;
        
        /// <summary>
        /// Morph keys in this clip
        /// </summary>
        [OdinSerialize]
        public List<MorphKey> keys = new List<MorphKey>();
        
        /// <summary>
        /// Get morph value for a specific blendshape at given time
        /// </summary>
        /// <param name="morphId">Blendshape ID</param>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Morph value or null if not found</returns>
        public float? GetMorphValue(string morphId, float globalTime)
        {
            if (!Contains(globalTime)) return null;
            
            var key = keys.Find(k => k.id == morphId);
            if (key.id == null) return null; // Key not found
            
            float normalizedTime = GetNormalizedTime(globalTime);
            return key.GetValue(normalizedTime) * weight;
        }
        
        /// <summary>
        /// Get all morph values at given time
        /// </summary>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Dictionary of morph ID to value</returns>
        public Dictionary<string, float> GetAllMorphValues(float globalTime)
        {
            var result = new Dictionary<string, float>();
            
            if (!Contains(globalTime)) return result;
            
            float normalizedTime = GetNormalizedTime(globalTime);
            
            foreach (var key in keys)
            {
                result[key.id] = key.GetValue(normalizedTime) * weight;
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Curve-based morph clip for complex animations
    /// Each blendshape has its own animation curve
    /// </summary>
    [Serializable]
    public partial class MorphCurveClip : MiniTimeline.Core.MiniClipBase
    {
        /// <summary>
        /// Curve channel for a single blendshape
        /// </summary>
        [Serializable]
        public struct CurveChannel
        {
            [OdinSerialize]
            public string id;
            [OdinSerialize]
            public AnimationCurve curve;
            [OdinSerialize]
            public float multiplier;
        }
        
        /// <summary>
        /// Blend mode for this clip
        /// </summary>
        [OdinSerialize]
        public MorphBlendMode blendMode = MorphBlendMode.Additive;
        
        /// <summary>
        /// Priority for Override blend mode
        /// </summary>
        [OdinSerialize]
        public int priority = 0;
        
        /// <summary>
        /// Overall clip weight
        /// </summary>
        [OdinSerialize]
        public float weight = 1f;
        
        /// <summary>
        /// Curve channels in this clip
        /// </summary>
        [OdinSerialize]
        public List<CurveChannel> channels = new List<CurveChannel>();
        
        /// <summary>
        /// Get morph value for a specific blendshape at given time
        /// </summary>
        /// <param name="morphId">Blendshape ID</param>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Morph value or null if not found</returns>
        public float? GetMorphValue(string morphId, float globalTime)
        {
            if (!Contains(globalTime)) return null;
            
            var channel = channels.Find(c => c.id == morphId);
            if (channel.id == null) return null;
            
            float localTime = globalTime - Start;
            float curveValue = channel.curve?.Evaluate(localTime) ?? 0f;
            
            return curveValue * channel.multiplier * weight;
        }
        
        /// <summary>
        /// Get all morph values at given time
        /// </summary>
        /// <param name="globalTime">Global timeline time</param>
        /// <returns>Dictionary of morph ID to value</returns>
        public Dictionary<string, float> GetAllMorphValues(float globalTime)
        {
            var result = new Dictionary<string, float>();
            
            if (!Contains(globalTime)) return result;
            
            float localTime = globalTime - Start;
            
            foreach (var channel in channels)
            {
                float curveValue = channel.curve?.Evaluate(localTime) ?? 0f;
                result[channel.id] = curveValue * channel.multiplier * weight;
            }
            
            return result;
        }
    }
}