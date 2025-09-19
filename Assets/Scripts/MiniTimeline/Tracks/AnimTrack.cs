using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Animation track for playing character animations
    /// Supports crossfading, blending, and multi-layer animation
    /// </summary>
    public class AnimTrack : MiniTrackBase<AnimClip>
    {
        public override int Order => 10; // Animation tracks run early
        
        private Animator targetAnimator;
        private MiniPlayableGraph playableGraph;
        private readonly Dictionary<string, AnimationClip> loadedClips = new Dictionary<string, AnimationClip>();
        private readonly List<AsyncOperationHandle<AnimationClip>> loadingHandles = new List<AsyncOperationHandle<AnimationClip>>();
        
        // Blending state
        private AnimClip currentPrimaryClip;
        private AnimClip currentSecondaryClip;
        private float blendWeight = 0f;
        private bool isBlending = false;
        private bool currentClipStartedInGraph = false;
        
        // Performance optimization
        private readonly List<AnimClip> tempActiveClips = new List<AnimClip>();
        
        #region Track Lifecycle
        
        protected override void OnPrepare()
        {
            Debug.Log($"[AnimTrack] OnPrepare called for track '{Id}', target object: {targetObject}");
            
            if(targetObject is Animator animator)
            {
                targetAnimator = animator;
                Debug.Log($"[AnimTrack] Target is Animator: {animator.name}");
            }
            else if (targetObject is GameObject go)
            {
                targetAnimator = go.GetComponent<Animator>();
                Debug.Log($"[AnimTrack] Target is GameObject: {go.name}, Animator found: {targetAnimator != null}");
            }
            else
            {
                Debug.LogError($"[AnimTrack] Target object for track '{Id}' is not an Animator or GameObject with Animator");
                return;
            }
            
            if (targetAnimator == null)
            {
                Debug.LogError($"[AnimTrack] No Animator found for track '{Id}'");
                return;
            }
            
            // Create playable graph for this track
            playableGraph = new MiniPlayableGraph($"AnimTrack_{Id}");
            playableGraph.Initialize(targetAnimator);
            playableGraph.Play();
            
            Debug.Log($"[AnimTrack] Created and started playable graph. IsValid: {playableGraph.IsValid}, IsPlaying: {playableGraph.IsPlaying}");
            
            // Load all animation clips
            LoadAllClips();
            
            Debug.Log($"[AnimTrack] Prepared track '{Id}' with {clips.Count} clips");
        }
        
        protected override void OnEvaluate(float time, bool scrub)
        {
            if (playableGraph == null || !playableGraph.IsValid) 
            {
                Debug.LogWarning($"[AnimTrack] Playable graph is invalid during evaluation");
                return;
            }
            
            // Get active clips at current time
            tempActiveClips.Clear();
            tempActiveClips.AddRange(GetActiveClips(time));
            
            Debug.Log($"[AnimTrack] Evaluating at time {time:F2}, found {tempActiveClips.Count} active clips");
            
            if (tempActiveClips.Count == 0)
            {
                // No active clips - stop animation
                StopAnimation();
            }
            else if (tempActiveClips.Count == 1)
            {
                // Single clip - play directly
                PlaySingleClip(tempActiveClips[0], time, scrub);
            }
            else
            {
                // Multiple clips - blend them
                BlendMultipleClips(tempActiveClips, time, scrub);
            }
            
            // Update crossfade if active
            if (isBlending && !scrub)
            {
                playableGraph.UpdateCrossfade(Time.deltaTime);
            }
            
            tempActiveClips.Clear();
        }
        
        protected override void OnCleanup()
        {
            // Cancel any loading operations
            foreach (var handle in loadingHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            loadingHandles.Clear();
            
            // Clear loaded clips
            loadedClips.Clear();
            
            // Dispose playable graph
            if (playableGraph != null)
            {
                playableGraph.Dispose();
                playableGraph = null;
            }
            
            Debug.Log($"[AnimTrack] Cleaned up track '{Id}'");
        }
        
        #endregion
        
        #region Animation Playback
        
        /// <summary>
        /// Play a single animation clip
        /// </summary>
        private void PlaySingleClip(AnimClip clip, float time, bool scrub)
        {
            Debug.Log($"[AnimTrack] PlaySingleClip called - Clip ID: {clip.Id}, Asset: {clip.animationAsset}, Time: {time:F2}");
            
            if (clip.cachedClip == null) 
            {
                Debug.LogWarning($"[AnimTrack] Clip {clip.Id} has no cached animation clip - still loading?");
                return;
            }
            
            Debug.Log($"[AnimTrack] Clip has cached animation: {clip.cachedClip.name}, Length: {clip.cachedClip.length:F2}s");
            
            // Check if we need to start a new clip or continue current one
            bool isNewClip = currentPrimaryClip != clip;
            
            Debug.Log($"[AnimTrack] Is new clip: {isNewClip}, Current primary: {(currentPrimaryClip?.Id ?? "null")}, Started in graph: {currentClipStartedInGraph}");
            
            if (isNewClip || !currentClipStartedInGraph)
            {
                // Start crossfade to new clip
                float fadeTime = scrub ? 0f : clip.fadeIn;
                Debug.Log($"[AnimTrack] Playing clip '{clip.cachedClip.name}' with fade time {fadeTime} (new: {isNewClip}, started: {currentClipStartedInGraph})");
                playableGraph.CrossfadeToClip(clip.cachedClip, fadeTime, clip.Id);
                currentPrimaryClip = clip;
                isBlending = fadeTime > 0f;
                currentClipStartedInGraph = true;
            }
            else
            {
                Debug.Log($"[AnimTrack] Continuing current clip '{clip.cachedClip.name}' (should be playing)");
                // Double-check: if the clip should be playing but might not be, restart it
                // This is a safety measure for cases where the graph state got corrupted
                Debug.Log($"[AnimTrack] Safety check: restarting clip to ensure playback");
                playableGraph.CrossfadeToClip(clip.cachedClip, 0f, clip.Id);
            }
            
            // Update clip time
            float animTime = clip.GetLocalAnimationTime(time);
            float normalizedTime = clip.GetNormalizedAnimationTime(time);
            
            Debug.Log($"[AnimTrack] Setting clip time - Local: {animTime:F2}, Normalized: {normalizedTime:F2}");
            playableGraph.SetClipNormalizedTime(clip.Id, normalizedTime);
            
            // Update fade weight
            float fadeWeight = clip.GetFadeWeight(time);
            Debug.Log($"[AnimTrack] Fade weight: {fadeWeight:F2}");
            // Note: Weight is handled by the playable graph during crossfade
        }
        
        /// <summary>
        /// Blend multiple overlapping clips
        /// </summary>
        private void BlendMultipleClips(List<AnimClip> activeClips, float time, bool scrub)
        {
            // Sort clips by priority (later clips have higher priority)
            activeClips.Sort((a, b) => a.Start.CompareTo(b.Start));
            
            // For now, use simple priority-based selection
            // TODO: Implement proper multi-clip blending
            var primaryClip = activeClips[activeClips.Count - 1];
            PlaySingleClip(primaryClip, time, scrub);
        }
        
        /// <summary>
        /// Stop animation playback
        /// </summary>
        private void StopAnimation()
        {
            Debug.Log($"[AnimTrack] Stopping animation");
            if (currentPrimaryClip != null)
            {
                currentPrimaryClip = null;
                isBlending = false;
                currentClipStartedInGraph = false;
            }
            
            // Clear the playable graph inputs to stop animation
            if (playableGraph != null && playableGraph.IsValid)
            {
                playableGraph.ClearActiveClips();
            }
        }
        
        #endregion
        
        #region Asset Loading
        
        /// <summary>
        /// Load all animation clips referenced by this track
        /// </summary>
        private void LoadAllClips()
        {
            foreach (var clip in clips)
            {
                LoadClip(clip);
            }
        }
        
        /// <summary>
        /// Load a single animation clip
        /// </summary>
        private void LoadClip(AnimClip clip)
        {
            if (string.IsNullOrEmpty(clip.animationAsset)) return;
            
            // Check if already loaded
            if (loadedClips.ContainsKey(clip.animationAsset))
            {
                clip.cachedClip = loadedClips[clip.animationAsset];
                return;
            }
            
            // Load via Addressables or Resources
            if (clip.animationAsset.StartsWith(MiniTimelineConstants.ASSET_ADDRESSABLE))
            {
                LoadClipFromAddressables(clip);
            }
            else if (clip.animationAsset.StartsWith(MiniTimelineConstants.ASSET_SCENE))
            {
                LoadClipFromScene(clip);
            }
            else
            {
                // Try Resources as fallback
                LoadClipFromResources(clip);
            }
        }
        
        /// <summary>
        /// Load clip from Addressables
        /// </summary>
        private void LoadClipFromAddressables(AnimClip clip)
        {
            string address = clip.animationAsset.Substring(MiniTimelineConstants.ASSET_ADDRESSABLE.Length);
            
            var handle = Addressables.LoadAssetAsync<AnimationClip>(address);
            loadingHandles.Add(handle);
            
            handle.Completed += (operation) =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    var animClip = operation.Result;
                    loadedClips[clip.animationAsset] = animClip;
                    clip.cachedClip = animClip;
                    
                    Debug.Log($"[AnimTrack] Loaded animation clip: {address}");
                }
                else
                {
                    Debug.LogError($"[AnimTrack] Failed to load animation clip: {address}");
                }
            };
        }
        
        /// <summary>
        /// Load clip from scene reference
        /// </summary>
        private void LoadClipFromScene(AnimClip clip)
        {
            string scenePath = clip.animationAsset.Substring(MiniTimelineConstants.ASSET_SCENE.Length);
            
            // Try to find the animation clip in the scene
            var foundClip = GameObject.Find(scenePath)?.GetComponent<Animation>()?.clip;
            
            if (foundClip != null)
            {
                loadedClips[clip.animationAsset] = foundClip;
                clip.cachedClip = foundClip;
                Debug.Log($"[AnimTrack] Found animation clip in scene: {scenePath}");
            }
            else
            {
                Debug.LogWarning($"[AnimTrack] Could not find animation clip in scene: {scenePath}");
            }
        }
        
        /// <summary>
        /// Load clip from Resources folder
        /// </summary>
        private void LoadClipFromResources(AnimClip clip)
        {
            var animClip = Resources.Load<AnimationClip>(clip.animationAsset);
            
            if (animClip != null)
            {
                loadedClips[clip.animationAsset] = animClip;
                clip.cachedClip = animClip;
                Debug.Log($"[AnimTrack] Loaded animation clip from Resources: {clip.animationAsset}");
            }
            else
            {
                Debug.LogWarning($"[AnimTrack] Could not load animation clip from Resources: {clip.animationAsset}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Add a new animation clip to this track
        /// </summary>
        /// <param name="animationAsset">Animation asset reference</param>
        /// <param name="start">Start time</param>
        /// <param name="duration">Duration</param>
        /// <returns>Created clip</returns>
        public AnimClip AddClip(string animationAsset, float start, float duration)
        {
            var clip = new AnimClip
            {
                Id = Guid.NewGuid().ToString(),
                animationAsset = animationAsset,
                Start = start,
                Duration = duration
            };
            
            clips.Add(clip);
            LoadClip(clip);
            
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
        
        /// <summary>
        /// Get current animation state info
        /// </summary>
        /// <returns>Animation state info</returns>
        public AnimationInfo GetCurrentAnimationInfo()
        {
            return new AnimationInfo
            {
                primaryClip = currentPrimaryClip,
                isBlending = isBlending,
                blendWeight = blendWeight
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Information about current animation state
    /// </summary>
    public struct AnimationInfo
    {
        public AnimClip primaryClip;
        public bool isBlending;
        public float blendWeight;
    }
}