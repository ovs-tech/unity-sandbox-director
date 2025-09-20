using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Camera track for controlling camera position, rotation, and properties
    /// Supports smooth transitions between camera positions and settings
    /// </summary>
    public class MovementTrack : MiniTrackBase<MovementClip>
    {
        public override int Order => 20; // Camera tracks run after animation
        
        private Camera targetCamera;
        private Transform cameraTransform;
        
        // Animation state
        private MovementClip currentClip;
        private MovementClip nextClip;
        private float blendWeight = 0f;
        
        // Performance optimization
        private readonly List<MovementClip> tempActiveClips = new List<MovementClip>();
        
        #region Track Lifecycle
        
        protected override void OnPrepare()
        {
            Debug.Log($"[MovementTrack] OnPrepare called for track '{Id}', target object: {targetObject}");
            
            if(targetObject is Camera camera)
            {
                targetCamera = camera;
                cameraTransform = camera.transform;
                Debug.Log($"[MovementTrack] Target is Camera: {camera.name}");
            }
            else if (targetObject is GameObject go)
            {
                targetCamera = go.GetComponent<Camera>();
                if (targetCamera != null)
                {
                    cameraTransform = targetCamera.transform;
                    Debug.Log($"[MovementTrack] Target is GameObject: {go.name}, Camera found: {targetCamera != null}");
                }
            }
            else
            {
                Debug.LogError($"[MovementTrack] Target object for track '{Id}' is not a Camera or GameObject with Camera");
                return;
            }
            
            if (targetCamera == null)
            {
                Debug.LogError($"[MovementTrack] No Camera found for track '{Id}'");
                return;
            }
            
            Debug.Log($"[MovementTrack] Prepared track '{Id}' with {clips.Count} clips");
        }
        
        protected override void OnEvaluate(float time, bool scrub)
        {
            if (targetCamera == null || cameraTransform == null) 
            {
                return;
            }
            
            // Get active clips at current time
            tempActiveClips.Clear();
            tempActiveClips.AddRange(GetActiveClips(time));
            
            if (tempActiveClips.Count == 0)
            {
                // No active clips - maintain current state
                return;
            }
            else if (tempActiveClips.Count == 1)
            {
                // Single clip - apply directly
                ApplySingleClip(tempActiveClips[0], time);
            }
            else
            {
                // Multiple clips - blend them
                BlendMultipleClips(tempActiveClips, time);
            }
            
            tempActiveClips.Clear();
        }
        
        protected override void OnCleanup()
        {
            currentClip = null;
            nextClip = null;
            Debug.Log($"[MovementTrack] Cleaned up track '{Id}'");
        }
        
        #endregion
        
        #region Camera Control
        
        /// <summary>
        /// Apply a single camera clip
        /// </summary>
        private void ApplySingleClip(MovementClip clip, float time)
        {   
            // Calculate local time within the clip
            float localTime = time - clip.Start;
            float normalizedTime = Mathf.Clamp01(localTime / clip.Duration);
            
            // Apply camera properties
            ApplyCameraProperties(clip, normalizedTime, 1f);
            
            currentClip = clip;
        }
        
        /// <summary>
        /// Blend multiple overlapping clips
        /// </summary>
        private void BlendMultipleClips(List<MovementClip> activeClips, float time)
        {
            // Sort clips by start time
            activeClips.Sort((a, b) => a.Start.CompareTo(b.Start));
            
            // For now, use simple priority-based selection (latest clip wins)
            // TODO: Implement proper multi-clip blending with weights
            var primaryClip = activeClips[activeClips.Count - 1];
            ApplySingleClip(primaryClip, time);
        }
        
        /// <summary>
        /// Apply camera properties with blending
        /// </summary>
        private void ApplyCameraProperties(MovementClip clip, float normalizedTime, float weight)
        {
            // Interpolate position if specified
            if (clip.hasPosition)
            {
                Vector3 targetPos = EvaluatePosition(clip, normalizedTime);
                if (weight < 1f && currentClip != null && currentClip.hasPosition)
                {
                    Vector3 currentPos = cameraTransform.position;
                    targetPos = Vector3.Lerp(currentPos, targetPos, weight);
                }
                cameraTransform.position = targetPos;
            }
            
            // Interpolate rotation if specified
            if (clip.hasRotation)
            {
                Quaternion targetRot = EvaluateRotation(clip, normalizedTime);
                if (weight < 1f && currentClip != null && currentClip.hasRotation)
                {
                    Quaternion currentRot = cameraTransform.rotation;
                    targetRot = Quaternion.Lerp(currentRot, targetRot, weight);
                }
                cameraTransform.rotation = targetRot;
            }
            
            // Apply camera settings
            if (clip.hasFieldOfView)
            {
                float targetFOV = EvaluateFieldOfView(clip, normalizedTime);
                if (weight < 1f && currentClip != null && currentClip.hasFieldOfView)
                {
                    float currentFOV = targetCamera.fieldOfView;
                    targetFOV = Mathf.Lerp(currentFOV, targetFOV, weight);
                }
                targetCamera.fieldOfView = targetFOV;
            }
        }
        
        /// <summary>
        /// Evaluate position at normalized time
        /// </summary>
        private Vector3 EvaluatePosition(MovementClip clip, float normalizedTime)
        {
            if (clip.animationCurve == CameraAnimationCurve.Linear)
            {
                return Vector3.Lerp(clip.startPosition, clip.endPosition, normalizedTime);
            }
            else if (clip.animationCurve == CameraAnimationCurve.EaseInOut)
            {
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                return Vector3.Lerp(clip.startPosition, clip.endPosition, easedTime);
            }
            else
            {
                // Custom curve or constant
                return normalizedTime < 0.5f ? clip.startPosition : clip.endPosition;
            }
        }
        
        /// <summary>
        /// Evaluate rotation at normalized time
        /// </summary>
        private Quaternion EvaluateRotation(MovementClip clip, float normalizedTime)
        {
            if (clip.animationCurve == CameraAnimationCurve.Linear)
            {
                return Quaternion.Lerp(clip.startRotation, clip.endRotation, normalizedTime);
            }
            else if (clip.animationCurve == CameraAnimationCurve.EaseInOut)
            {
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                return Quaternion.Lerp(clip.startRotation, clip.endRotation, easedTime);
            }
            else
            {
                // Custom curve or constant
                return normalizedTime < 0.5f ? clip.startRotation : clip.endRotation;
            }
        }
        
        /// <summary>
        /// Evaluate field of view at normalized time
        /// </summary>
        private float EvaluateFieldOfView(MovementClip clip, float normalizedTime)
        {
            if (clip.animationCurve == CameraAnimationCurve.Linear)
            {
                return Mathf.Lerp(clip.startFieldOfView, clip.endFieldOfView, normalizedTime);
            }
            else if (clip.animationCurve == CameraAnimationCurve.EaseInOut)
            {
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                return Mathf.Lerp(clip.startFieldOfView, clip.endFieldOfView, easedTime);
            }
            else
            {
                // Custom curve or constant
                return normalizedTime < 0.5f ? clip.startFieldOfView : clip.endFieldOfView;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Add a new camera clip to this track
        /// </summary>
        /// <param name="start">Start time</param>
        /// <param name="duration">Duration</param>
        /// <param name="startPos">Starting position</param>
        /// <param name="endPos">Ending position</param>
        /// <returns>Created clip</returns>
        public MovementClip AddPositionClip(float start, float duration, Vector3 startPos, Vector3 endPos)
        {
            var clip = new MovementClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                hasPosition = true,
                startPosition = startPos,
                endPosition = endPos,
                animationCurve = CameraAnimationCurve.Linear
            };
            
            clips.Add(clip);
            return clip;
        }
        
        /// <summary>
        /// Add a new camera rotation clip to this track
        /// </summary>
        /// <param name="start">Start time</param>
        /// <param name="duration">Duration</param>
        /// <param name="startRot">Starting rotation</param>
        /// <param name="endRot">Ending rotation</param>
        /// <returns>Created clip</returns>
        public MovementClip AddRotationClip(float start, float duration, Quaternion startRot, Quaternion endRot)
        {
            var clip = new MovementClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                hasRotation = true,
                startRotation = startRot,
                endRotation = endRot,
                animationCurve = CameraAnimationCurve.Linear
            };
            
            clips.Add(clip);
            return clip;
        }
        
        /// <summary>
        /// Add a new field of view clip to this track
        /// </summary>
        /// <param name="start">Start time</param>
        /// <param name="duration">Duration</param>
        /// <param name="startFOV">Starting field of view</param>
        /// <param name="endFOV">Ending field of view</param>
        /// <returns>Created clip</returns>
        public MovementClip AddFieldOfViewClip(float start, float duration, float startFOV, float endFOV)
        {
            var clip = new MovementClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                hasFieldOfView = true,
                startFieldOfView = startFOV,
                endFieldOfView = endFOV,
                animationCurve = CameraAnimationCurve.Linear
            };
            
            clips.Add(clip);
            return clip;
        }
        
        /// <summary>
        /// Remove a clip from this track
        /// </summary>
        /// <param name="clipId">Clip ID to remove</param>
        public bool RemoveClip(string clipId)
        {
            var clip = clips.FirstOrDefault(c => c.Id == clipId);
            if (clip != null)
            {
                clips.Remove(clip);
                return true;
            }
            return false;
        }
        
        #endregion
    }
}