using System;
using UnityEngine;

using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Camera clip for controlling camera movement and properties
    /// </summary>
    [Serializable]
    public class MovementClip : MiniClipBase
    {
        // Camera transform properties
        [Header("Position")]

        public MovementMode mode = MovementMode.Direct;
        
        public bool hasPosition = false;
        
        public Vector3 startPosition = Vector3.zero;
        
        public Vector3 endPosition = Vector3.zero;
        
        [Header("Rotation")]
        
        public bool hasRotation = false;
        
        public Quaternion startRotation = Quaternion.identity;
        
        public Quaternion endRotation = Quaternion.identity;
        
        [Header("Camera Properties")]
        
        public bool hasFieldOfView = false;
        
        public float startFieldOfView = 60f;
        
        public float endFieldOfView = 60f;
        
        [Header("Animation")]
        
        public CameraAnimationCurve animationCurve = CameraAnimationCurve.Linear;
        
        public AnimationCurve customCurve;
        
        [Header("Blending")]
        
        public float fadeIn = 0f;
        
        public float fadeOut = 0f;
        
        #region Camera Specific Methods
        
        /// <summary>
        /// Get local time within this clip
        /// </summary>
        public float GetLocalTime(float globalTime)
        {
            return globalTime - Start;
        }
        
        /// <summary>
        /// Get interpolated position at global time
        /// </summary>
        public Vector3 GetPositionAtTime(float globalTime)
        {
            if (!hasPosition) return Vector3.zero;
            
            float normalizedTime = GetNormalizedTime(globalTime);
            return EvaluateVector3(startPosition, endPosition, normalizedTime);
        }
        
        /// <summary>
        /// Get interpolated rotation at global time
        /// </summary>
        public Quaternion GetRotationAtTime(float globalTime)
        {
            if (!hasRotation) return Quaternion.identity;
            
            float normalizedTime = GetNormalizedTime(globalTime);
            return EvaluateQuaternion(startRotation, endRotation, normalizedTime);
        }
        
        /// <summary>
        /// Get interpolated field of view at global time
        /// </summary>
        public float GetFieldOfViewAtTime(float globalTime)
        {
            if (!hasFieldOfView) return 60f;
            
            float normalizedTime = GetNormalizedTime(globalTime);
            return EvaluateFloat(startFieldOfView, endFieldOfView, normalizedTime);
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
        
        #endregion
        
        #region Private Interpolation Methods
        
        private Vector3 EvaluateVector3(Vector3 start, Vector3 end, float t)
        {
            switch (animationCurve)
            {
                case CameraAnimationCurve.Linear:
                    return Vector3.Lerp(start, end, t);
                    
                case CameraAnimationCurve.EaseInOut:
                    float easedT = Mathf.SmoothStep(0f, 1f, t);
                    return Vector3.Lerp(start, end, easedT);
                    
                case CameraAnimationCurve.EaseIn:
                    float easeInT = t * t;
                    return Vector3.Lerp(start, end, easeInT);
                    
                case CameraAnimationCurve.EaseOut:
                    float easeOutT = 1f - (1f - t) * (1f - t);
                    return Vector3.Lerp(start, end, easeOutT);
                    
                case CameraAnimationCurve.Custom:
                    if (customCurve != null)
                    {
                        float curveT = customCurve.Evaluate(t);
                        return Vector3.Lerp(start, end, curveT);
                    }
                    return Vector3.Lerp(start, end, t);
                    
                default:
                    return Vector3.Lerp(start, end, t);
            }
        }
        
        private Quaternion EvaluateQuaternion(Quaternion start, Quaternion end, float t)
        {
            switch (animationCurve)
            {
                case CameraAnimationCurve.Linear:
                    return Quaternion.Lerp(start, end, t);
                    
                case CameraAnimationCurve.EaseInOut:
                    float easedT = Mathf.SmoothStep(0f, 1f, t);
                    return Quaternion.Lerp(start, end, easedT);
                    
                case CameraAnimationCurve.EaseIn:
                    float easeInT = t * t;
                    return Quaternion.Lerp(start, end, easeInT);
                    
                case CameraAnimationCurve.EaseOut:
                    float easeOutT = 1f - (1f - t) * (1f - t);
                    return Quaternion.Lerp(start, end, easeOutT);
                    
                case CameraAnimationCurve.Custom:
                    if (customCurve != null)
                    {
                        float curveT = customCurve.Evaluate(t);
                        return Quaternion.Lerp(start, end, curveT);
                    }
                    return Quaternion.Lerp(start, end, t);
                    
                default:
                    return Quaternion.Lerp(start, end, t);
            }
        }
        
        private float EvaluateFloat(float start, float end, float t)
        {
            switch (animationCurve)
            {
                case CameraAnimationCurve.Linear:
                    return Mathf.Lerp(start, end, t);
                    
                case CameraAnimationCurve.EaseInOut:
                    float easedT = Mathf.SmoothStep(0f, 1f, t);
                    return Mathf.Lerp(start, end, easedT);
                    
                case CameraAnimationCurve.EaseIn:
                    float easeInT = t * t;
                    return Mathf.Lerp(start, end, easeInT);
                    
                case CameraAnimationCurve.EaseOut:
                    float easeOutT = 1f - (1f - t) * (1f - t);
                    return Mathf.Lerp(start, end, easeOutT);
                    
                case CameraAnimationCurve.Custom:
                    if (customCurve != null)
                    {
                        float curveT = customCurve.Evaluate(t);
                        return Mathf.Lerp(start, end, curveT);
                    }
                    return Mathf.Lerp(start, end, t);
                    
                default:
                    return Mathf.Lerp(start, end, t);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Animation curve types for camera movement
    /// </summary>
    public enum CameraAnimationCurve
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Custom
    }

    /// <summary>
    /// Movement mode types
    /// </summary>
    public enum MovementMode
    {
        Direct,
        NavMesh
    }
}