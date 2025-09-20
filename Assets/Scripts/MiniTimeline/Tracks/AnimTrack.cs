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

        // Store original animator controller to restore later
        private RuntimeAnimatorController originalAnimatorController;

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
            Debug.Log($"[AnimTrack] Target object type: {targetObject?.GetType().Name}, is null: {targetObject == null}");

            if (targetObject is Animator animator)
            {
                targetAnimator = animator;
                Debug.Log($"[AnimTrack] Target is Animator: {animator.name}, instanceID: {animator.GetInstanceID()}");
            }
            else if (targetObject is GameObject go)
            {
                targetAnimator = go.GetComponent<Animator>();
                Debug.Log($"[AnimTrack] Target is GameObject: {go.name}, instanceID: {go.GetInstanceID()}, Animator found: {targetAnimator != null}");
                if (targetAnimator != null)
                {
                    Debug.Log($"[AnimTrack] Found Animator: {targetAnimator.name}, instanceID: {targetAnimator.GetInstanceID()}");
                }
            }
            else
            {
                Debug.LogError($"[AnimTrack] Target object for track '{Id}' is not an Animator or GameObject with Animator. Type: {targetObject?.GetType().Name}");
                return;
            }

            if (targetAnimator == null)
            {
                Debug.LogError($"[AnimTrack] No Animator found for track '{Id}'");
                return;
            }

            Debug.Log($"[AnimTrack] Using Animator: {targetAnimator.name}, enabled: {targetAnimator.enabled}, GameObject active: {targetAnimator.gameObject.activeInHierarchy}");

            // Check if this is a UMA character (they rebuild dynamically)
            bool isUMACharacter = targetAnimator.gameObject.name.Contains("UMA") ||
                                  targetAnimator.GetComponent("UMAData") != null;
            if (isUMACharacter)
            {
                Debug.Log($"[AnimTrack] Detected UMA character: {targetAnimator.gameObject.name}. UMA may rebuild this character dynamically.");
            }

            // ALTERNATIVE FIX: Temporarily disable the Animator to prevent conflicts during setup
            bool wasEnabled = targetAnimator.enabled;
            if (targetAnimator.runtimeAnimatorController != null)
            {
                Debug.Log($"[AnimTrack] Found AnimatorController '{targetAnimator.runtimeAnimatorController.name}' - temporarily disabling Animator during setup");
                originalAnimatorController = targetAnimator.runtimeAnimatorController;
                targetAnimator.enabled = false;
                Debug.Log($"[AnimTrack] Animator temporarily disabled. Will re-enable after PlayableGraph setup.");
            }

            // Create playable graph for this track
            try
            {
                Debug.Log($"[AnimTrack] Creating MiniPlayableGraph for track '{Id}'...");
                playableGraph = new MiniPlayableGraph($"AnimTrack_{Id}");
                Debug.Log($"[AnimTrack] MiniPlayableGraph created successfully");

                Debug.Log($"[AnimTrack] Initializing playable graph with animator '{targetAnimator.name}'...");
                playableGraph.Initialize(targetAnimator);
                Debug.Log($"[AnimTrack] PlayableGraph initialized successfully");

                Debug.Log($"[AnimTrack] Starting playable graph...");
                playableGraph.Play();
                Debug.Log($"[AnimTrack] PlayableGraph play command sent");

                Debug.Log($"[AnimTrack] Created and started playable graph. IsValid: {playableGraph.IsValid}, IsPlaying: {playableGraph.IsPlaying}");
                Debug.Log($"[AnimTrack] Target animator: {targetAnimator.name}, has controller: {targetAnimator.runtimeAnimatorController != null}");

                // Re-enable the Animator now that PlayableGraph is set up and has taken control
                if (originalAnimatorController != null && !targetAnimator.enabled)
                {
                    targetAnimator.enabled = true;
                    Debug.Log($"[AnimTrack] Re-enabled Animator. PlayableGraph should now have control over AnimatorController.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnimTrack] Failed to create or initialize PlayableGraph: {e.Message}");
                Debug.LogError($"[AnimTrack] Stack trace: {e.StackTrace}");

                // Make sure to re-enable the animator even if there was an error
                if (originalAnimatorController != null && !targetAnimator.enabled)
                {
                    targetAnimator.enabled = true;
                    Debug.Log($"[AnimTrack] Re-enabled Animator after PlayableGraph setup error.");
                }
                return;
            }

            // Load all animation clips
            LoadAllClips();

            Debug.Log($"[AnimTrack] Prepared track '{Id}' with {clips.Count} clips");
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            if (playableGraph == null || !playableGraph.IsValid)
            {
                return;
            }

            // Check if the target animator is still valid
            if (targetAnimator == null) return;

            // Update crossfade if active (do this before evaluating clips)
            if (isBlending && !scrub)
            {
                playableGraph.UpdateCrossfade(UnityEngine.Time.deltaTime);
            }

            // Get active clips at current time
            tempActiveClips.Clear();
            tempActiveClips.AddRange(GetActiveClips(time));

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

            // Clear stored reference (we didn't modify the original)
            originalAnimatorController = null;

            Debug.Log($"[AnimTrack] Cleaned up track '{Id}'");
        }

        #endregion

        #region Animation Playback

        /// <summary>
        /// Play a single animation clip
        /// </summary>
        private void PlaySingleClip(AnimClip clip, float time, bool scrub)
        {
            // Debug.Log($"[AnimTrack] Playing clip '{clip.Id}' at time {time} (scrub={scrub})");
            if (clip.cachedClip == null)
            {
                return;
            }

            // Double-check animator is still valid (for UMA dynamic character rebuilds)
            if (targetAnimator == null) return;

            // Check if we need to start a new clip or continue current one
            bool isNewClip = currentPrimaryClip != clip;

            if (isNewClip || !currentClipStartedInGraph)
            {
                // Start crossfade to new clip
                float fadeTime = scrub ? 0f : clip.fadeIn;

                playableGraph.CrossfadeToClip(clip.cachedClip, fadeTime, clip.Id);
                currentPrimaryClip = clip;
                isBlending = fadeTime > 0f;
                currentClipStartedInGraph = true;

                // Set the initial time for the new clip
                float normalizedTime = clip.GetNormalizedAnimationTime(time);
                playableGraph.SetClipNormalizedTime(clip.Id, normalizedTime);

            }
            else
            {
                // Check if the clip is actually active - if not, force restart
                bool isActive = playableGraph.IsClipActive(clip.Id);
                if (!isActive)
                {
                    float fadeTime = scrub ? 0f : clip.fadeIn;
                    playableGraph.CrossfadeToClip(clip.cachedClip, fadeTime, clip.Id);

                    // Set the initial time for the restarted clip
                    float normalizedTime = clip.GetNormalizedAnimationTime(time);
                    playableGraph.SetClipNormalizedTime(clip.Id, normalizedTime);
                }
            }

            // Verify the playable graph is valid before evaluating
            if (!playableGraph.IsValid)
            {
                return;
            }

            if (!playableGraph.IsPlaying)
            {
                playableGraph.Play(); // Try to start it again
            }

            if (scrub)
            {
                // For scrubbing, manually set the exact time and evaluate without time advance
                float normalizedTime = clip.GetNormalizedAnimationTime(time);
                playableGraph.SetClipNormalizedTime(clip.Id, normalizedTime);
                playableGraph.Evaluate(0f); // Force evaluation without time advance
            }
            else
            {
                // For continuous playback, just evaluate the graph with deltaTime
                // Don't constantly reset the time - let the graph advance naturally
                float deltaTime = UnityEngine.Time.deltaTime;
                playableGraph.Evaluate(deltaTime);

                // Check if the clip is actually active in the graph
                bool isActive = playableGraph.IsClipActive(clip.Id);
            }

            // Update fade weight
            float fadeWeight = clip.GetFadeWeight(time);
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

        /// <summary>
        /// Rebind to a new animator (useful for UMA character rebuilds)
        /// </summary>
        /// <param name="newAnimator">New animator to bind to</param>
        public void RebindAnimator(Animator newAnimator)
        {
            if (newAnimator == null)
            {
                Debug.LogError("[AnimTrack] Cannot rebind to null animator");
                return;
            }

            Debug.Log($"[AnimTrack] Rebinding from '{targetAnimator?.name ?? "null"}' to '{newAnimator.name}'");

            // Dispose old playable graph
            if (playableGraph != null)
            {
                playableGraph.Dispose();
                playableGraph = null;
            }

            // Set new animator
            targetAnimator = newAnimator;

            // Create new playable graph
            playableGraph = new MiniPlayableGraph($"AnimTrack_{Id}_Rebound");
            playableGraph.Initialize(targetAnimator);
            playableGraph.Play();

            // Reset clip state
            currentClipStartedInGraph = false;
            currentPrimaryClip = null;

            Debug.Log($"[AnimTrack] Successfully rebound to animator '{newAnimator.name}'");
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