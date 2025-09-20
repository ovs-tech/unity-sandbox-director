using System;
using System.Collections.Generic;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Clip for controlling Animator parameters over time
    /// </summary>
    [Serializable]
    public class AnimatorClip : IMiniClip
    {
        [SerializeField] private string id;
        [SerializeField] private float start;
        [SerializeField] private float duration;
        
        [Header("Parameter Settings")]
        public List<AnimatorParameterKey> parameterKeys = new List<AnimatorParameterKey>();
        
        [Header("Blending")]
        public float fadeIn = 0f;
        public float fadeOut = 0f;
        public AnimatorBlendMode blendMode = AnimatorBlendMode.Override;
        
        #region IMiniClip Implementation
        
        public string Id 
        { 
            get => id; 
            set => id = value; 
        }
        
        public float Start 
        { 
            get => start; 
            set => start = value; 
        }
        
        public float Duration 
        { 
            get => duration; 
            set => duration = Mathf.Max(0.01f, value); 
        }
        
        public float End => Start + Duration;
        
        public bool IsActive(float time)
        {
            return time >= Start && time <= End;
        }
        
        public bool Contains(float time)
        {
            return time >= Start && time <= End;
        }
        
        public float GetLocalTime(float globalTime)
        {
            return globalTime - Start;
        }
        
        public float GetNormalizedTime(float globalTime)
        {
            if (Duration <= 0f) return 0f;
            float localTime = GetLocalTime(globalTime);
            return Mathf.Clamp01(localTime / Duration);
        }
        
        #endregion
        
        #region Animator Specific Methods
        
        /// <summary>
        /// Get parameter value at global time
        /// </summary>
        public object GetParameterValueAtTime(string parameterName, float globalTime)
        {
            var key = parameterKeys.Find(k => k.parameterName == parameterName);
            if (key == null) return null;
            
            float normalizedTime = GetNormalizedTime(globalTime);
            return key.GetValueAtTime(normalizedTime);
        }
        
        /// <summary>
        /// Get all parameter values at global time
        /// </summary>
        public Dictionary<string, object> GetAllParameterValuesAtTime(float globalTime)
        {
            var values = new Dictionary<string, object>();
            float normalizedTime = GetNormalizedTime(globalTime);
            
            foreach (var key in parameterKeys)
            {
                values[key.parameterName] = key.GetValueAtTime(normalizedTime);
            }
            
            return values;
        }
        
        /// <summary>
        /// Get fade weight at global time (for blending)
        /// </summary>
        public float GetFadeWeight(float globalTime)
        {
            float localTime = GetLocalTime(globalTime);
            
            // Fade in
            if (localTime < fadeIn)
            {
                return fadeIn > 0f ? localTime / fadeIn : 1f;
            }
            
            // Fade out
            float fadeOutStartTime = Duration - fadeOut;
            if (localTime > fadeOutStartTime)
            {
                return fadeOut > 0f ? (Duration - localTime) / fadeOut : 1f;
            }
            
            // Full weight
            return 1f;
        }
        
        /// <summary>
        /// Add a parameter key to this clip
        /// </summary>
        public void AddParameterKey(AnimatorParameterKey key)
        {
            // Remove existing key with same parameter name
            parameterKeys.RemoveAll(k => k.parameterName == key.parameterName);
            parameterKeys.Add(key);
        }
        
        /// <summary>
        /// Remove a parameter key by name
        /// </summary>
        public bool RemoveParameterKey(string parameterName)
        {
            return parameterKeys.RemoveAll(k => k.parameterName == parameterName) > 0;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a keyframe for an Animator parameter
    /// </summary>
    [Serializable]
    public class AnimatorParameterKey
    {
        [Header("Parameter Info")]
        public string parameterName;
        public AnimatorControllerParameterType parameterType;
        
        [Header("Values")]
        public float startFloatValue;
        public float endFloatValue;
        public int startIntValue;
        public int endIntValue;
        public bool startBoolValue;
        public bool endBoolValue;
        public bool triggerValue; // For trigger parameters
        
        [Header("Animation")]
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimatorParameterCurve curveType = AnimatorParameterCurve.Linear;
        
        /// <summary>
        /// Get the interpolated value at normalized time
        /// </summary>
        public object GetValueAtTime(float normalizedTime)
        {
            switch (parameterType)
            {
                case AnimatorControllerParameterType.Float:
                    return GetFloatValue(normalizedTime);
                    
                case AnimatorControllerParameterType.Int:
                    return GetIntValue(normalizedTime);
                    
                case AnimatorControllerParameterType.Bool:
                    return GetBoolValue(normalizedTime);
                    
                case AnimatorControllerParameterType.Trigger:
                    return triggerValue;
                    
                default:
                    return null;
            }
        }
        
        private float GetFloatValue(float normalizedTime)
        {
            float t = EvaluateCurve(normalizedTime);
            return Mathf.Lerp(startFloatValue, endFloatValue, t);
        }
        
        private int GetIntValue(float normalizedTime)
        {
            float t = EvaluateCurve(normalizedTime);
            return Mathf.RoundToInt(Mathf.Lerp(startIntValue, endIntValue, t));
        }
        
        private bool GetBoolValue(float normalizedTime)
        {
            // For boolean values, we can either:
            // 1. Switch at 50% through the clip
            // 2. Use start value for first half, end value for second half
            // 3. Or just use the end value when normalizedTime > 0
            
            if (normalizedTime < 0.5f)
                return startBoolValue;
            else
                return endBoolValue;
        }
        
        private float EvaluateCurve(float normalizedTime)
        {
            switch (curveType)
            {
                case AnimatorParameterCurve.Linear:
                    return normalizedTime;
                    
                case AnimatorParameterCurve.EaseIn:
                    return normalizedTime * normalizedTime;
                    
                case AnimatorParameterCurve.EaseOut:
                    return 1f - (1f - normalizedTime) * (1f - normalizedTime);
                    
                case AnimatorParameterCurve.EaseInOut:
                    return Mathf.SmoothStep(0f, 1f, normalizedTime);
                    
                case AnimatorParameterCurve.Custom:
                    return curve.Evaluate(normalizedTime);
                    
                default:
                    return normalizedTime;
            }
        }
    }
    
    /// <summary>
    /// Animation curve types for animator parameters
    /// </summary>
    public enum AnimatorParameterCurve
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Custom
    }
    
    /// <summary>
    /// Blend modes for animator parameter clips
    /// </summary>
    public enum AnimatorBlendMode
    {
        Override,   // Replace existing values
        Additive,   // Add to existing values (for float/int)
        Multiply    // Multiply existing values (for float)
    }
}