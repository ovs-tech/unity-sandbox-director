using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Track for playing Unity Animation clips on timeline.
    /// Supports multiple animation layers, blending, and timing control using Unity's PlayableGraph API.
    /// </summary>
    [Serializable]
    public class AnimTrack : MiniTrackBase<AnimClip>
    {   
        // Target references
        private Animator _animator;
        
        // PlayableGraph state
        private PlayableGraph _playableGraph;
        private AnimationMixerPlayable _mixerPlayable;
        private readonly Dictionary<string, AnimationClipPlayable> _clipPlayables = new();
        private bool _isGraphInitialized;
        
        // Asset loading state
        private readonly AnimationAssetLoader _assetLoader = new();
        private bool _hasLoadedAssets;
        private bool _isLoadingAssets;
        
        // Clip tracking
        private readonly List<AnimClip> _activeClips = new();
        
        #region Track Lifecycle
        
        protected override void OnPrepare()
        {
            Debug.Log($"[AnimTrack] OnPrepare called for track '{Id}' with bind key '{BindKey}', target type: {targetObject?.GetType().Name}");
            
            _animator = ResolveAnimator();
            if (_animator == null)
            {
                Debug.LogError($"[AnimTrack] Failed to resolve Animator for track '{Id}' with bind key '{BindKey}'");
                return;
            }
            
            Debug.Log($"[AnimTrack] Resolved Animator on GameObject '{_animator.gameObject.name}' for track '{Id}'");
            
            // Start loading assets but don't block - will complete async
            StartLoadingAssetsIfNeeded();
        }
        
        protected override void OnEnter(float time, bool scrub)
        {
            Debug.Log($"[AnimTrack] OnEnter at time {time:F2}s, scrub={scrub}, hasLoadedAssets={_hasLoadedAssets}, graphInitialized={_isGraphInitialized}, animator={_animator != null}");
            
            // Re-resolve animator if it's null
            if (_animator == null)
            {
                Debug.LogWarning($"[AnimTrack] Animator is null in OnEnter, attempting to re-resolve for track '{Id}'");
                _animator = ResolveAnimator();
                
                if (_animator == null)
                {
                    Debug.LogError($"[AnimTrack] Still cannot resolve Animator in OnEnter for track '{Id}' with bind key '{BindKey}', target={targetObject}");
                    return;
                }
                
                Debug.Log($"[AnimTrack] Successfully re-resolved Animator in OnEnter: '{_animator.gameObject.name}'");
            }
            
            // Try to initialize graph (will check if assets are ready)
            TryInitializePlayableGraph();
        }
        
        protected override void OnEvaluate(float time, bool scrub)
        {
            if (_animator == null)
            {
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"[AnimTrack] OnEvaluate called but Animator is null for track '{Id}'");
                }
                return;
            }
            
            // Try to initialize graph if not ready yet (assets may still be loading)
            if (!_isGraphInitialized)
            {
                TryInitializePlayableGraph();
                
                if (!_isGraphInitialized)
                {
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[AnimTrack] Graph not yet initialized for track '{Id}', waiting for assets...");
                    }
                    return;
                }
            }
            
            GetActiveClipsAtTime(time, _activeClips);
            ResetMixerWeights();
            
            if (_activeClips.Count == 0)
            {
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[AnimTrack] No active clips at time {time:F2}s for track '{Id}'");
                }
                return;
            }
            
            _activeClips.Sort((a, b) => a.layer.CompareTo(b.layer));
            
            foreach (var clip in _activeClips)
            {
                UpdateClipPlayable(clip, time);
            }
        }
        
        protected override void OnExit(float time, bool scrub)
        {
            Debug.Log($"[AnimTrack] OnExit at time {time:F2}s, scrub={scrub}, graphInitialized={_isGraphInitialized}");
            
            if (_isGraphInitialized)
            {
                DestroyPlayableGraph();
            }
        }
        
        protected override void OnCleanup()
        {
            DestroyPlayableGraph();
            _assetLoader.UnloadAll();
            _activeClips.Clear();
            _hasLoadedAssets = false;
        }
        
        #endregion
        
        #region Initialization and Setup
        
        private Animator ResolveAnimator()
        {
            if (targetObject == null)
            {
                Debug.LogError($"[AnimTrack] ResolveAnimator called but targetObject is null for track '{Id}' with bind key '{BindKey}'");
                return null;
            }
            
            Debug.Log($"[AnimTrack] ResolveAnimator for track '{Id}', targetObject type: {targetObject.GetType().Name}");
            
            Animator result = targetObject switch
            {
                Animator animator => animator,
                GameObject go => go.GetComponentInChildren<Animator>() ?? go.AddComponent<Animator>(),
                Transform transform => transform.GetComponentInChildren<Animator>() ?? transform.gameObject.AddComponent<Animator>(),
                Animation animation => animation.GetComponentInChildren<Animator>() ?? animation.gameObject.AddComponent<Animator>(),
                _ => null
            };
            
            if (result == null)
            {
                Debug.LogError($"[AnimTrack] ResolveAnimator returned null for track '{Id}', targetObject type: {targetObject.GetType().Name}");
            }
            else
            {
                Debug.Log($"[AnimTrack] ResolveAnimator found Animator on '{result.gameObject.name}' for track '{Id}'");
            }
            
            return result;
        }
        
        private void StartLoadingAssetsIfNeeded()
        {
            if (_hasLoadedAssets || _isLoadingAssets) return;
            
            var assetPaths = clips
                .Select(c => c.animationAsset)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();
            
            if (assetPaths.Count == 0)
            {
                Debug.Log($"[AnimTrack] No assets to load for track '{Id}'");
                _hasLoadedAssets = true;
                return;
            }
            
            Debug.Log($"[AnimTrack] Starting to load {assetPaths.Count} assets for track '{Id}'");
            _isLoadingAssets = true;
            
            // Fire and forget - don't block
            _ = LoadAnimationAssetsAsync(assetPaths);
        }
        
        private async Task LoadAnimationAssetsAsync(IEnumerable<string> assetPaths)
        {
            try
            {
                await _assetLoader.LoadAssetsAsync(assetPaths);
                _hasLoadedAssets = true;
                _isLoadingAssets = false;
                
                Debug.Log($"[AnimTrack] Finished loading assets for track '{Id}', loaded count: {_assetLoader.LoadedCount}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimTrack] Failed to load assets for track '{Id}': {e.Message}");
                _isLoadingAssets = false;
            }
        }
        
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
        
        private void ResetMixerWeights()
        {
            for (int i = 0; i < _mixerPlayable.GetInputCount(); i++)
            {
                _mixerPlayable.SetInputWeight(i, 0f);
            }
        }
        
        private void UpdateClipPlayable(AnimClip clip, float time)
        {
            var state = clip.GetAnimationState(time);
            if (!state.isActive || !_clipPlayables.TryGetValue(clip.Id, out var playable))
                return;
            
            float effectiveWeight = state.weight * clip.weight;
            if (effectiveWeight <= 0.01f) return;
            
            playable.SetTime(state.animationTime);
            
            int inputIndex = GetMixerInputIndex(clip.Id);
            if (inputIndex >= 0)
            {
                _mixerPlayable.SetInputWeight(inputIndex, effectiveWeight);
            }
        }
        
        private int GetMixerInputIndex(string clipId)
        {
            int index = 0;
            foreach (var clip in clips)
            {
                if (clip.Id == clipId) return index;
                if (_assetLoader.IsLoaded(clip.animationAsset)) index++;
            }
            return -1;
        }
        
        private void TryInitializePlayableGraph()
        {
            if (_isGraphInitialized) return;
            if (_animator == null) return;
            if (!_hasLoadedAssets)
            {
                // Assets not ready yet
                return;
            }
            
            InitializePlayableGraph();
        }
        
        #endregion
        
        #region Public API
        
        public void AddClip(AnimClip clip)
        {
            if (clip == null) return;
            
            clip.ValidateSettings();
            clips.Add(clip);
            
            if (_hasLoadedAssets && !string.IsNullOrEmpty(clip.animationAsset))
            {
                LoadAndRebuildAsync(clip.animationAsset);
            }
        }
        
        public async Task AddClipAsync(AnimClip clip)
        {
            if (clip == null) return;
            
            clip.ValidateSettings();
            clips.Add(clip);
            
            if (_hasLoadedAssets && !string.IsNullOrEmpty(clip.animationAsset))
            {
                await _assetLoader.LoadAssetAsync(clip.animationAsset);
                RebuildPlayableGraph();
            }
        }
        
        public bool RemoveClip(string clipId)
        {
            var clip = clips.FirstOrDefault(c => c.Id == clipId);
            if (clip == null) return false;
            
            clips.Remove(clip);
            RebuildPlayableGraph();
            return true;
        }
        
        public AnimClip GetClip(string clipId) => clips.FirstOrDefault(c => c.Id == clipId);
        
        public IReadOnlyList<AnimClip> GetAllClips() => clips.AsReadOnly();
        
        public bool HasActiveClipsAt(float time) => clips.Any(clip => clip.ShouldPlay(time));
        
        public void ValidateAllClips()
        {
            foreach (var clip in clips)
            {
                clip.ValidateSettings();
            }
        }
        
        public bool IsAssetLoaded(string assetPath) => _assetLoader.IsLoaded(assetPath);
        
        public bool IsAssetLoading(string assetPath) => _assetLoader.IsLoading(assetPath);
        
        public float GetLoadingProgress() => _assetLoader.GetProgress(clips.Select(c => c.animationAsset));
        
        public Task WaitForAllAssetsLoaded() => _assetLoader.WaitForAllLoaded();
        
        private async void LoadAndRebuildAsync(string assetPath)
        {
            await _assetLoader.LoadAssetAsync(assetPath);
            RebuildPlayableGraph();
        }
        
        #endregion
        
        #region PlayableGraph Management
        
        private void InitializePlayableGraph()
        {
            if (_isGraphInitialized)
            {
                Debug.LogWarning($"[AnimTrack] PlayableGraph already initialized for track '{Id}'");
                return;
            }
            
            if (_animator == null)
            {
                Debug.LogError($"[AnimTrack] Cannot initialize PlayableGraph - Animator is null for track '{Id}'");
                return;
            }
            
            Debug.Log($"[AnimTrack] Initializing PlayableGraph for track '{Id}' with {clips.Count} clips");
            
            try
            {
                _playableGraph = PlayableGraph.Create($"AnimTrack_{Id}");
                _mixerPlayable = AnimationMixerPlayable.Create(_playableGraph, clips.Count);
                
                int inputIndex = 0;
                foreach (var clip in clips)
                {
                    if (!_assetLoader.TryGetClip(clip.animationAsset, out var animClip))
                    {
                        Debug.LogWarning($"[AnimTrack] Asset '{clip.animationAsset}' not found for clip '{clip.Id}'");
                        continue;
                    }
                    
                    Debug.Log($"[AnimTrack] Creating playable for clip '{clip.Id}' with asset '{clip.animationAsset}' at input {inputIndex}");
                    
                    var clipPlayable = AnimationClipPlayable.Create(_playableGraph, animClip);
                    clipPlayable.SetApplyFootIK(false);
                    clipPlayable.SetApplyPlayableIK(false);
                    
                    _playableGraph.Connect(clipPlayable, 0, _mixerPlayable, inputIndex);
                    _mixerPlayable.SetInputWeight(inputIndex, 0f);
                    
                    _clipPlayables[clip.Id] = clipPlayable;
                    inputIndex++;
                }
                
                var output = AnimationPlayableOutput.Create(_playableGraph, $"AnimOutput_{Id}", _animator);
                output.SetSourcePlayable(_mixerPlayable);
                
                _playableGraph.Play();
                _isGraphInitialized = true;
                
                Debug.Log($"[AnimTrack] PlayableGraph initialized successfully with {_clipPlayables.Count} clip playables");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimTrack] Failed to initialize PlayableGraph: {e.Message}\n{e.StackTrace}");
                DestroyPlayableGraph();
            }
        }
        
        private void DestroyPlayableGraph()
        {
            if (!_isGraphInitialized)
            {
                Debug.Log($"[AnimTrack] DestroyPlayableGraph called but graph not initialized for track '{Id}'");
                return;
            }
            
            Debug.Log($"[AnimTrack] Destroying PlayableGraph for track '{Id}'");
            
            if (_playableGraph.IsValid())
            {
                _playableGraph.Destroy();
            }
            
            _clipPlayables.Clear();
            _isGraphInitialized = false;
        }
        
        private void RebuildPlayableGraph()
        {
            if (_isGraphInitialized)
            {
                DestroyPlayableGraph();
                InitializePlayableGraph();
            }
        }
        
        #endregion
        
        #region Debug
        
        public string GetDebugInfo()
        {
            return $"AnimTrack '{Id}' (BindKey: '{BindKey}')\n" +
                   $"  Animator: {_animator != null}\n" +
                   $"  Clips: {clips.Count}\n" +
                   $"  Loaded Assets: {_assetLoader.LoadedCount}\n" +
                   $"  Loading Progress: {GetLoadingProgress():P1}\n" +
                   $"  Graph Initialized: {_isGraphInitialized}\n" +
                   $"  Graph Valid: {_playableGraph.IsValid()}\n" +
                   $"  Clip Playables: {_clipPlayables.Count}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Manages loading and caching of animation clip assets.
    /// Supports both Addressable assets and Resources folder assets.
    /// </summary>
    internal class AnimationAssetLoader
    {
        private readonly Dictionary<string, AnimationClip> _loadedClips = new();
        private readonly Dictionary<string, AsyncOperationHandle<AnimationClip>> _loadingHandles = new();
        private readonly Dictionary<string, AsyncOperationHandle<AnimationClip>> _addressableHandles = new();
        
        public int LoadedCount => _loadedClips.Count;
        
        public async Task LoadAssetsAsync(IEnumerable<string> assetPaths)
        {
            var tasks = assetPaths
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(LoadAssetAsync)
                .ToList();
            
            await Task.WhenAll(tasks);
        }
        
        public async Task LoadAssetAsync(string assetPath)
        {
            if (_loadedClips.ContainsKey(assetPath) || _loadingHandles.ContainsKey(assetPath))
            {
                Debug.Log($"[AnimationAssetLoader] Asset '{assetPath}' already loaded or loading");
                return;
            }
            
            Debug.Log($"[AnimationAssetLoader] Starting load for asset '{assetPath}'");
            
            try
            {
                if (assetPath.StartsWith(MiniTimelineConstants.ASSET_ADDRESSABLE))
                {
                    await LoadFromAddressables(assetPath);
                }
                else
                {
                    var clip = Resources.Load<AnimationClip>(assetPath);
                    if (clip != null)
                    {
                        _loadedClips[assetPath] = clip;
                        Debug.Log($"[AnimationAssetLoader] Successfully loaded '{assetPath}' from Resources");
                    }
                    else
                    {
                        Debug.LogWarning($"[AnimationAssetLoader] Failed to load '{assetPath}' from Resources");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimationAssetLoader] Failed to load '{assetPath}': {e.Message}\n{e.StackTrace}");
            }
        }
        
        private async Task LoadFromAddressables(string assetPath)
        {
            string addressableKey = assetPath.Substring(MiniTimelineConstants.ASSET_ADDRESSABLE.Length);
            Debug.Log($"[AnimationAssetLoader] Loading Addressable: '{addressableKey}'");
            
            var handle = Addressables.LoadAssetAsync<AnimationClip>(addressableKey);
            _loadingHandles[assetPath] = handle;
            
            try
            {
                var clip = await handle.Task;
                _loadingHandles.Remove(assetPath);
                
                if (handle.Status == AsyncOperationStatus.Succeeded && clip != null)
                {
                    _loadedClips[assetPath] = clip;
                    _addressableHandles[assetPath] = handle;
                    Debug.Log($"[AnimationAssetLoader] Successfully loaded Addressable '{addressableKey}'");
                }
                else
                {
                    Debug.LogError($"[AnimationAssetLoader] Failed to load Addressable '{addressableKey}', status: {handle.Status}");
                    Addressables.Release(handle);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimationAssetLoader] Exception loading Addressable '{addressableKey}': {e.Message}\n{e.StackTrace}");
                _loadingHandles.Remove(assetPath);
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }
        
        public void UnloadAll()
        {
            foreach (var handle in _loadingHandles.Values.Concat(_addressableHandles.Values))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            
            _loadingHandles.Clear();
            _addressableHandles.Clear();
            _loadedClips.Clear();
        }
        
        public bool TryGetClip(string assetPath, out AnimationClip clip) =>
            _loadedClips.TryGetValue(assetPath, out clip);
        
        public bool IsLoaded(string assetPath) => _loadedClips.ContainsKey(assetPath);
        
        public bool IsLoading(string assetPath) => _loadingHandles.ContainsKey(assetPath);
        
        public float GetProgress(IEnumerable<string> assetPaths)
        {
            if (assetPaths == null) return 1.0f;

            int totalCount = 0;
            int loadedCount = 0;

            foreach (var path in assetPaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    totalCount++;
                    if (_loadedClips.ContainsKey(path))
                    {
                        loadedCount++;
                    }
                }
            }

            if (totalCount == 0) return 1.0f;
            
            return (float)loadedCount / totalCount;
        }
        
        public async Task WaitForAllLoaded()
        {
            var tasks = _loadingHandles.Values
                .Where(h => h.IsValid())
                .Select(h => h.Task)
                .ToList();
            
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }
}