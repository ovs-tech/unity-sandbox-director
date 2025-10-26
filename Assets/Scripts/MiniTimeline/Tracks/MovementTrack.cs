using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Movement track for controlling Transform position and rotation.
    /// Supports smooth transitions between positions with various animation curves.
    /// </summary>
    public class MovementTrack : MiniTrackBase<MovementClip>
    {
        public override int Order => 20; // Movement tracks run after animation
        
        private Transform targetTransform;
        
        // Animation state
        private MovementClip currentClip;
        private MovementClip nextClip;
        private float blendWeight = 0f;
        
        // Performance optimization
        private readonly List<MovementClip> tempActiveClips = new List<MovementClip>();
        
        #region Track Lifecycle
        
        protected override void OnPrepare()
        {
            Debug.Log($"[MovementTrack] OnPrepare called for track '{Id}', bind key: '{BindKey}', target object: {targetObject}");
            
            // Reset references
            targetTransform = null;
            
            if (targetObject is Transform transform)
            {
                targetTransform = transform;
                Debug.Log($"[MovementTrack] Target is Transform: {transform.name}");
            }
            else if (targetObject is GameObject go)
            {
                targetTransform = go.transform;
                Debug.Log($"[MovementTrack] Target is GameObject: {go.name}");
            }
            else if (targetObject is Component component)
            {
                targetTransform = component.transform;
                Debug.Log($"[MovementTrack] Target is Component: {component.GetType().Name} on {component.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[MovementTrack] Target object for track '{Id}' is not a Transform, GameObject, or Component");
                return;
            }
            
            if (targetTransform == null)
            {
                Debug.LogError($"[MovementTrack] No Transform found for track '{Id}'");
                return;
            }
            
            Debug.Log($"[MovementTrack] Prepared track '{Id}' with {clips.Count} clips, target transform: {targetTransform.name}");
        }
        
        protected override void OnEvaluate(float time, bool scrub)
        {
            if (targetTransform == null) 
            {
                Debug.LogWarning($"[MovementTrack] Cannot evaluate - targetTransform is null for track '{Id}'");
                return;
            }
            
            // Get active clips at current time
            tempActiveClips.Clear();
            tempActiveClips.AddRange(GetActiveClips(time));
            
            Debug.Log($"[MovementTrack] Evaluating at time {time:F3}, found {tempActiveClips.Count} active clips, scrub: {scrub}");
            
            if (tempActiveClips.Count == 0)
            {
                // No active clips - maintain current state
                Debug.Log($"[MovementTrack] No active clips at time {time:F3}");
                return;
            }
            else if (tempActiveClips.Count == 1)
            {
                // Single clip - apply directly
                Debug.Log($"[MovementTrack] Applying single clip: {tempActiveClips[0].Id} at time {time:F3}");
                ApplySingleClip(tempActiveClips[0], time);
            }
            else
            {
                // Multiple clips - blend them
                Debug.Log($"[MovementTrack] Blending {tempActiveClips.Count} clips at time {time:F3}");
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
        
        #region Movement Control
        
        /// <summary>
        /// Apply a single movement clip
        /// </summary>
        private void ApplySingleClip(MovementClip clip, float time)
        {   
            // Calculate local time within the clip
            float localTime = time - clip.Start;
            float normalizedTime = Mathf.Clamp01(localTime / clip.Duration);
            
            Debug.Log($"[MovementTrack] Applying clip '{clip.Id}': localTime={localTime:F3}, normalizedTime={normalizedTime:F3}, " +
                     $"hasPosition={clip.hasPosition}, hasRotation={clip.hasRotation}");
            
            // Apply transform properties
            ApplyTransformProperties(clip, normalizedTime, 1f);
            
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
        /// Apply transform properties with blending
        /// </summary>
        private void ApplyTransformProperties(MovementClip clip, float normalizedTime, float weight)
        {
            // Interpolate position if specified
            if (clip.hasPosition)
            {
                Vector3 targetPos = EvaluatePosition(clip, normalizedTime);
                if (weight < 1f && currentClip != null && currentClip.hasPosition)
                {
                    Vector3 currentPos = targetTransform.position;
                    targetPos = Vector3.Lerp(currentPos, targetPos, weight);
                }
                
                Debug.Log($"[MovementTrack] Setting position: {targetPos} (weight: {weight:F2})");
                targetTransform.position = targetPos;
            }
            
            // Interpolate rotation if specified
            if (clip.hasRotation)
            {
                Quaternion targetRot = EvaluateRotation(clip, normalizedTime);
                if (weight < 1f && currentClip != null && currentClip.hasRotation)
                {
                    Quaternion currentRot = targetTransform.rotation;
                    targetRot = Quaternion.Lerp(currentRot, targetRot, weight);
                }
                
                Debug.Log($"[MovementTrack] Setting rotation: {targetRot.eulerAngles} (weight: {weight:F2})");
                targetTransform.rotation = targetRot;
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
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Add a new position movement clip to this track
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
            Debug.Log($"[MovementTrack] Added position clip '{clip.Id}' to track '{Id}': start={start:F2}, duration={duration:F2}, from={startPos} to={endPos}");
            return clip;
        }
        
        /// <summary>
        /// Add a new rotation movement clip to this track
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
            Debug.Log($"[MovementTrack] Added rotation clip '{clip.Id}' to track '{Id}': start={start:F2}, duration={duration:F2}, from={startRot.eulerAngles} to={endRot.eulerAngles}");
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
                Debug.Log($"[MovementTrack] Removed clip '{clipId}' from track '{Id}'");
                return true;
            }
            
            Debug.LogWarning($"[MovementTrack] Clip '{clipId}' not found in track '{Id}' for removal");
            return false;
        }
        
        #endregion
    }
}