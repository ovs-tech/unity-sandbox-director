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
    /// Track for playing Unity Animation clips on timeline
    /// Supports multiple animation layers, blending, and timing control
    /// Directly samples AnimationClip without requiring Animator component
    /// 
    /// Asset Loading:
    /// - Supports Addressable assets with "addr:" prefix (e.g., "addr:Animations/WalkCycle")
    /// - Supports Resources folder assets (e.g., "Animations/WalkCycle")  
    /// - Automatic async loading with proper cleanup
    /// - Loading progress tracking and status queries
    /// 
    /// EvaluateMode:
    /// - Default: Continuous - samples animation every frame
    /// - OnEnter: Only triggers when animation starts
    /// - OnExit: Only triggers when animation ends
    /// </summary>
    public class AnimTrack : MiniTrackBase<AnimClip>
    {
        public override int Order => 10; // Animation tracks run early, after events but before IK/morph
        
        private GameObject targetGameObject;
        private Transform targetTransform;
        
        // Animation state management
        private readonly Dictionary<string, AnimationClip> loadedClips = new Dictionary<string, AnimationClip>();
        private readonly Dictionary<string, AnimationState> lastClipStates = new Dictionary<string, AnimationState>();
        private readonly List<AnimClip> activeClips = new List<AnimClip>();
        
        // Addressables asset management
        private readonly Dictionary<string, AsyncOperationHandle<AnimationClip>> loadingOperations = new Dictionary<string, AsyncOperationHandle<AnimationClip>>();
        private readonly Dictionary<string, AsyncOperationHandle<AnimationClip>> loadedHandles = new Dictionary<string, AsyncOperationHandle<AnimationClip>>();
        
        // Performance optimization
        private readonly List<AnimationState> tempStates = new List<AnimationState>();
        private bool hasLoadedAssets = false;
        
        #region Track Lifecycle
        
        protected override void OnPrepare()
        {
            // Resolve target GameObject
            if (targetObject is GameObject go)
            {
                targetGameObject = go;
                targetTransform = go.transform;
            }
            else if (targetObject is Transform transform)
            {
                targetTransform = transform;
                targetGameObject = transform.gameObject;
            }
            else if (targetObject is Animator animator)
            {
                targetGameObject = animator.gameObject;
                targetTransform = animator.transform;
            }
            else if (targetObject is Animation animation)
            {
                targetGameObject = animation.gameObject;
                targetTransform = animation.transform;
            }
            
            if (targetGameObject == null)
            {
                Debug.LogError($"[AnimTrack] Could not resolve target GameObject for track '{Id}' with bind key '{BindKey}'");
                return;
            }
            
            // Load animation assets
            LoadAnimationAssets();
        }
        
        protected override void OnEnter(float time, bool scrub)
        {
            // Called when track enters active state
            Debug.Log($"[AnimTrack] OnEnter at time {time:F2}, scrub={scrub}");
        }
        
        protected override void OnEvaluate(float time, bool scrub)
        {
            if (targetGameObject == null) return;
            
            // Get all active clips at current time
            GetActiveClipsAtTime(time, activeClips);
            
            if (activeClips.Count == 0) return;
            
            // Sort clips by layer (higher layers have priority)
            activeClips.Sort((a, b) => a.layer.CompareTo(b.layer));
            
            // Sample animations directly
            foreach (var clip in activeClips)
            {
                var state = clip.GetAnimationState(time);
                if (!state.isActive) continue;
                
                if (loadedClips.TryGetValue(clip.animationAsset, out var animClip))
                {
                    // Apply weight for blending
                    float effectiveWeight = state.weight * clip.weight;
                    
                    // Sample animation at specific time
                    if (effectiveWeight > 0.01f) // Only sample if weight is significant
                    {
                        animClip.SampleAnimation(targetGameObject, state.animationTime);
                    }
                    
                    // Store state
                    lastClipStates[state.clipId] = state;
                }
            }
        }
        
        protected override void OnExit(float time, bool scrub)
        {
            // Called when track exits active state
            Debug.Log($"[AnimTrack] OnExit at time {time:F2}, scrub={scrub}");
            
            // Clear animation states
            lastClipStates.Clear();
        }
        
        protected override void OnCleanup()
        {
            UnloadAnimationAssets();
            
            activeClips.Clear();
            lastClipStates.Clear();
            hasLoadedAssets = false;
        }
        
        #endregion
        
        #region Asset Management
        
        private async void LoadAnimationAssets()
        {
            if (hasLoadedAssets) return;
            
            var loadTasks = new List<System.Threading.Tasks.Task>();
            
            foreach (var clip in clips)
            {
                if (string.IsNullOrEmpty(clip.animationAsset)) continue;
                
                var task = LoadAnimationClipAsync(clip.animationAsset);
                loadTasks.Add(task);
            }
            
            // Wait for all assets to load
            await System.Threading.Tasks.Task.WhenAll(loadTasks);
            
            hasLoadedAssets = true;
            // Debug.Log($"[AnimTrack] Finished loading {loadedClips.Count} animation clips");
        }
        
        private async System.Threading.Tasks.Task LoadAnimationClipAsync(string assetPath)
        {
            if (loadedClips.ContainsKey(assetPath) || loadingOperations.ContainsKey(assetPath)) 
                return;
            
            try
            {
                AnimationClip animClip = null;
                
                // Handle Addressable assets
                if (assetPath.StartsWith(MiniTimelineConstants.ASSET_ADDRESSABLE))
                {
                    string addressableKey = assetPath.Substring(MiniTimelineConstants.ASSET_ADDRESSABLE.Length);
                    await LoadFromAddressables(assetPath, addressableKey);
                    return;
                }
                // Handle scene assets or Resources
                else
                {
                    animClip = Resources.Load<AnimationClip>(assetPath);
                    if (animClip == null)
                    {
                        // Debug.LogWarning($"[AnimTrack] Could not load animation clip: {assetPath}");
                        return;
                    }
                }
                
                if (animClip != null)
                {
                    loadedClips[assetPath] = animClip;
                    // Debug.Log($"[AnimTrack] Loaded animation clip: {assetPath}");
                }
            }
            catch (Exception)
            {
                // Debug.LogError($"[AnimTrack] Error loading animation clip '{assetPath}': {e.Message}");
            }
        }
        
        private async System.Threading.Tasks.Task LoadFromAddressables(string assetPath, string addressableKey)
        {
            try
            {
                // Debug.Log($"[AnimTrack] Loading Addressable animation clip: {addressableKey}");
                
                // Start the addressable load operation
                var handle = Addressables.LoadAssetAsync<AnimationClip>(addressableKey);
                loadingOperations[assetPath] = handle;
                
                // Wait for the operation to complete
                var animClip = await handle.Task;
                
                // Remove from loading operations and add to loaded clips
                loadingOperations.Remove(assetPath);
                
                if (handle.Status == AsyncOperationStatus.Succeeded && animClip != null)
                {
                    loadedClips[assetPath] = animClip;
                    loadedHandles[assetPath] = handle;
                    // Debug.Log($"[AnimTrack] Successfully loaded Addressable animation clip: {addressableKey}");
                }
                else
                {
                    // Debug.LogError($"[AnimTrack] Failed to load Addressable animation clip: {addressableKey}");
                    Addressables.Release(handle);
                }
            }
            catch (Exception)
            {
                // Debug.LogError($"[AnimTrack] Exception loading Addressable animation clip '{addressableKey}': {e.Message}");
                
                // Clean up failed operation
                if (loadingOperations.TryGetValue(assetPath, out var failedHandle))
                {
                    loadingOperations.Remove(assetPath);
                    Addressables.Release(failedHandle);
                }
            }
        }
        
        /// <summary>
        /// Synchronous fallback method for immediate loading needs
        /// </summary>
        private void LoadAnimationClip(string assetPath)
        {
            if (loadedClips.ContainsKey(assetPath)) return;
            
            try
            {
                AnimationClip animClip = null;
                
                // Handle Addressable assets - use synchronous load for immediate needs
                if (assetPath.StartsWith(MiniTimelineConstants.ASSET_ADDRESSABLE))
                {
                    string addressableKey = assetPath.Substring(MiniTimelineConstants.ASSET_ADDRESSABLE.Length);
                    
                    // Check if we have a completed async operation
                    if (loadedHandles.TryGetValue(assetPath, out var existingHandle))
                    {
                        if (existingHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            animClip = existingHandle.Result;
                        }
                    }
                    else
                    {
                        // Synchronous Addressable load (not recommended but sometimes necessary)
                        // Debug.LogWarning($"[AnimTrack] Using synchronous Addressable load for: {addressableKey}");
                        var handle = Addressables.LoadAssetAsync<AnimationClip>(addressableKey);
                        animClip = handle.WaitForCompletion();
                        
                        if (handle.Status == AsyncOperationStatus.Succeeded && animClip != null)
                        {
                            loadedHandles[assetPath] = handle;
                        }
                        else
                        {
                            Addressables.Release(handle);
                        }
                    }
                }
                // Handle scene assets or Resources
                else
                {
                    animClip = Resources.Load<AnimationClip>(assetPath);
                    if (animClip == null)
                    {
                        // Debug.LogWarning($"[AnimTrack] Could not load animation clip: {assetPath}");
                    }
                }
                
                if (animClip != null)
                {
                    loadedClips[assetPath] = animClip;
                    // Debug.Log($"[AnimTrack] Loaded animation clip: {assetPath}");
                }
            }
            catch (Exception)
            {
                // Debug.LogError($"[AnimTrack] Error loading animation clip '{assetPath}': {e.Message}");
            }
        }
        
        private void UnloadAnimationAssets()
        {
            // Release any ongoing loading operations
            foreach (var handle in loadingOperations.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            loadingOperations.Clear();
            
            // Release loaded Addressable assets
            foreach (var handle in loadedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            loadedHandles.Clear();
            
            // Clear loaded clips
            loadedClips.Clear();
            
            // Debug.Log($"[AnimTrack] Unloaded all animation assets for track '{Id}'");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void GetActiveClipsAtTime(float time, List<AnimClip> result)
        {
            result.Clear();
            
            foreach (var clip in clips)
            {
                if (clip.ShouldPlay(time))
                {
                    result.Add(clip);
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Add a new animation clip to this track
        /// </summary>
        /// <param name="clip">Animation clip to add</param>
        public void AddClip(AnimClip clip)
        {
            if (clip == null) return;
            
            clip.ValidateSettings();
            clips.Add(clip);
            
            // Load the asset if we're already prepared
            if (hasLoadedAssets && !string.IsNullOrEmpty(clip.animationAsset))
            {
                // Use async loading for new clips
                _ = LoadAnimationClipAsync(clip.animationAsset);
            }
        }
        
        /// <summary>
        /// Add a new animation clip to this track with async loading
        /// </summary>
        /// <param name="clip">Animation clip to add</param>
        /// <returns>Task that completes when the clip and its assets are loaded</returns>
        public async System.Threading.Tasks.Task AddClipAsync(AnimClip clip)
        {
            if (clip == null) return;
            
            clip.ValidateSettings();
            clips.Add(clip);
            
            // Load the asset if we're already prepared
            if (hasLoadedAssets && !string.IsNullOrEmpty(clip.animationAsset))
            {
                await LoadAnimationClipAsync(clip.animationAsset);
            }
        }
        
        /// <summary>
        /// Remove an animation clip from this track
        /// </summary>
        /// <param name="clipId">ID of clip to remove</param>
        public bool RemoveClip(string clipId)
        {
            var clip = clips.FirstOrDefault(c => c.Id == clipId);
            if (clip != null)
            {
                clips.Remove(clip);
                lastClipStates.Remove(clipId);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get animation clip by ID
        /// </summary>
        /// <param name="clipId">Clip ID</param>
        /// <returns>Animation clip or null</returns>
        public AnimClip GetClip(string clipId)
        {
            return clips.FirstOrDefault(c => c.Id == clipId);
        }
        
        /// <summary>
        /// Get all clips on this track
        /// </summary>
        /// <returns>Collection of animation clips</returns>
        public IReadOnlyList<AnimClip> GetAllClips()
        {
            return clips.AsReadOnly();
        }
        
        /// <summary>
        /// Get current animation state for a clip
        /// </summary>
        /// <param name="clipId">Clip ID</param>
        /// <returns>Animation state or null</returns>
        public AnimationState? GetClipState(string clipId)
        {
            return lastClipStates.TryGetValue(clipId, out var state) ? state : null;
        }
        
        /// <summary>
        /// Check if track has any clips active at given time
        /// </summary>
        /// <param name="time">Timeline time</param>
        /// <returns>True if any clips are active</returns>
        public bool HasActiveClipsAt(float time)
        {
            return clips.Any(clip => clip.ShouldPlay(time));
        }
        
        /// <summary>
        /// Validate all clips on this track
        /// </summary>
        public void ValidateAllClips()
        {
            foreach (var clip in clips)
            {
                clip.ValidateSettings();
            }
        }
        
        /// <summary>
        /// Check if a specific animation asset is loaded
        /// </summary>
        /// <param name="assetPath">Asset path to check</param>
        /// <returns>True if the asset is loaded and ready</returns>
        public bool IsAssetLoaded(string assetPath)
        {
            return loadedClips.ContainsKey(assetPath);
        }
        
        /// <summary>
        /// Check if a specific animation asset is currently loading
        /// </summary>
        /// <param name="assetPath">Asset path to check</param>
        /// <returns>True if the asset is currently being loaded</returns>
        public bool IsAssetLoading(string assetPath)
        {
            return loadingOperations.ContainsKey(assetPath);
        }
        
        /// <summary>
        /// Get the loading progress for all assets (0-1)
        /// </summary>
        /// <returns>Loading progress where 1.0 means all assets are loaded</returns>
        public float GetLoadingProgress()
        {
            if (clips.Count == 0) return 1.0f;
            
            int totalAssets = clips.Count(c => !string.IsNullOrEmpty(c.animationAsset));
            if (totalAssets == 0) return 1.0f;
            
            int loadedAssets = clips.Count(c => !string.IsNullOrEmpty(c.animationAsset) && loadedClips.ContainsKey(c.animationAsset));
            
            return (float)loadedAssets / totalAssets;
        }
        
        /// <summary>
        /// Wait for all assets to finish loading
        /// </summary>
        /// <returns>Task that completes when all assets are loaded</returns>
        public async System.Threading.Tasks.Task WaitForAllAssetsLoaded()
        {
            // Wait for any ongoing loading operations
            var loadingTasks = new List<System.Threading.Tasks.Task>();
            
            foreach (var handle in loadingOperations.Values)
            {
                if (handle.IsValid())
                {
                    loadingTasks.Add(handle.Task);
                }
            }
            
            if (loadingTasks.Count > 0)
            {
                await System.Threading.Tasks.Task.WhenAll(loadingTasks);
            }
        }
        
        #endregion
        
        #region Debug and Diagnostics
        
        /// <summary>
        /// Get debug information about this track
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"AnimTrack '{Id}' (BindKey: '{BindKey}')\n";
            info += $"  Target: {targetObject?.name ?? "None"}\n";
            info += $"  GameObject: {targetGameObject != null}\n";
            info += $"  Transform: {targetTransform != null}\n";
            info += $"  Clips: {clips.Count}\n";
            info += $"  Loaded Assets: {loadedClips.Count}\n";
            info += $"  Loading Assets: {loadingOperations.Count}\n";
            info += $"  Addressable Handles: {loadedHandles.Count}\n";
            info += $"  Loading Progress: {GetLoadingProgress():P1}\n";
            info += $"  Active States: {lastClipStates.Count}\n";
            info += $"  EvaluateMode: {EvaluateMode}";
            
            return info;
        }
        
        #endregion
    }
}