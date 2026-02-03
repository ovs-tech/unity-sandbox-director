using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    /// <summary>
    /// Track for controlling blendshapes/morph targets
    /// Supports additive and override blend modes with priority
    /// </summary>
    [Serializable]
    public class MorphTrack : MiniTrackBase<IMorphClip>
    {
        public override int Order => 30; // Morph tracks run after animation
        
        private SkinnedMeshRenderer targetRenderer;
        private Mesh targetMesh;
        
        // Blendshape caching
        private readonly Dictionary<string, int> blendshapeIndices = new Dictionary<string, int>();
        private readonly Dictionary<string, float> currentValues = new Dictionary<string, float>();
        private readonly Dictionary<string, float> accumulatedValues = new Dictionary<string, float>();
        
        // Performance optimization
        private readonly List<IMorphClip> tempActiveClips = new List<IMorphClip>();
        private readonly Dictionary<string, float> tempMorphValues = new Dictionary<string, float>();
        private readonly List<string> tempMorphIds = new List<string>();
        private readonly List<IMorphClip> tempOverrideClips = new List<IMorphClip>();
        private readonly HashSet<string> processedMorphs = new HashSet<string>();
        
        #region Track Lifecycle
        
        protected override void OnPrepare()
        {
            // Try to get SkinnedMeshRenderer from target
            if (targetObject is SkinnedMeshRenderer renderer)
            {
                targetRenderer = renderer;
            }
            else if (targetObject is GameObject go)
            {
                targetRenderer = go.GetComponent<SkinnedMeshRenderer>();
            }
            
            if (targetRenderer == null)
            {
                // Debug.LogError($"[MorphTrack] Target object for track '{Id}' does not have a SkinnedMeshRenderer");
                return;
            }
            
            targetMesh = targetRenderer.sharedMesh;
            if (targetMesh == null)
            {
                // Debug.LogError($"[MorphTrack] SkinnedMeshRenderer on '{targetRenderer.name}' has no mesh");
                return;
            }
            
            // Cache blendshape indices
            CacheBlendshapeIndices();
            
            // Initialize current values
            InitializeCurrentValues();
            
            // Debug.Log($"[MorphTrack] Prepared track '{Id}' with {blendshapeIndices.Count} blendshapes");
        }
        
        protected override void OnEvaluate(float time, bool scrub)
        {
            if (targetRenderer == null || targetMesh == null) return;
            
            // Clear accumulated values
            accumulatedValues.Clear();
            tempActiveClips.Clear();
            
            // Get all active clips
            tempActiveClips.AddRange(GetActiveClips(time));
            
            if (tempActiveClips.Count == 0)
            {
                // No active clips - reset to defaults
                ResetToDefaults();
                return;
            }
            
            // Process clips by blend mode
            ProcessAdditiveClips(tempActiveClips, time);
            ProcessOverrideClips(tempActiveClips, time);
            
            // Apply final values to mesh
            ApplyMorphValues();
            
            tempActiveClips.Clear();
        }
        
        protected override void OnCleanup()
        {
            // Reset all blendshapes to 0
            if (targetRenderer != null && targetMesh != null)
            {
                ResetAllBlendshapes();
            }
            
            blendshapeIndices.Clear();
            currentValues.Clear();
            accumulatedValues.Clear();
            
            // Debug.Log($"[MorphTrack] Cleaned up track '{Id}'");
        }
        
        #endregion
        
        #region Blendshape Management
        
        /// <summary>
        /// Cache blendshape name to index mapping
        /// </summary>
        private void CacheBlendshapeIndices()
        {
            blendshapeIndices.Clear();
            
            for (int i = 0; i < targetMesh.blendShapeCount; i++)
            {
                string name = targetMesh.GetBlendShapeName(i);
                blendshapeIndices[name] = i;
            }
            
            // Debug.Log($"[MorphTrack] Cached {blendshapeIndices.Count} blendshape indices");
        }
        
        /// <summary>
        /// Initialize current values from renderer
        /// </summary>
        private void InitializeCurrentValues()
        {
            currentValues.Clear();
            
            foreach (var kvp in blendshapeIndices)
            {
                float currentWeight = targetRenderer.GetBlendShapeWeight(kvp.Value);
                currentValues[kvp.Key] = currentWeight;
            }
        }
        
        /// <summary>
        /// Reset all blendshapes to 0
        /// </summary>
        private void ResetAllBlendshapes()
        {
            foreach (var kvp in blendshapeIndices)
            {
                targetRenderer.SetBlendShapeWeight(kvp.Value, 0f);
                currentValues[kvp.Key] = 0f;
            }
        }
        
        /// <summary>
        /// Reset to default values (no active clips)
        /// </summary>
        private void ResetToDefaults()
        {
            // For now, reset to 0. Could store initial values if needed.
            foreach (var kvp in blendshapeIndices)
            {
                if (!Mathf.Approximately(currentValues[kvp.Key], 0f))
                {
                    targetRenderer.SetBlendShapeWeight(kvp.Value, 0f);
                    currentValues[kvp.Key] = 0f;
                }
            }
        }
        
        #endregion
        
        #region Clip Processing
        
        /// <summary>
        /// Process additive clips - sum all values
        /// </summary>
        private void ProcessAdditiveClips(List<IMorphClip> activeClips, float time)
        {
            foreach (var clip in activeClips)
            {
                if (clip.BlendMode != MorphBlendMode.Additive) continue;
                
                tempMorphValues.Clear();
                clip.GetAllMorphValues(time, tempMorphValues);
                
                foreach (var kvp in tempMorphValues)
                {
                    if (!accumulatedValues.ContainsKey(kvp.Key))
                        accumulatedValues[kvp.Key] = 0f;
                    
                    if (clip.BlendMode == MorphBlendMode.Multiply)
                    {
                        accumulatedValues[kvp.Key] *= kvp.Value;
                    }
                    else
                    {
                        accumulatedValues[kvp.Key] += kvp.Value;
                    }
                }
            }
            
            // Clamp additive values to 0-100 range
            tempMorphIds.Clear();
            tempMorphIds.AddRange(accumulatedValues.Keys);
            
            foreach (var morphId in tempMorphIds)
            {
                accumulatedValues[morphId] = Mathf.Clamp(accumulatedValues[morphId], 0f, 100f);
            }
            
            tempMorphIds.Clear();
        }
        
        /// <summary>
        /// Process override clips - highest priority wins
        /// </summary>
        private void ProcessOverrideClips(List<IMorphClip> activeClips, float time)
        {
            // Group override clips by morph ID and find highest priority for each
            tempOverrideClips.Clear();
            for (int i = 0; i < activeClips.Count; i++)
            {
                var clip = activeClips[i];
                if (clip.BlendMode == MorphBlendMode.Override)
                {
                    tempOverrideClips.Add(clip);
                }
            }

            if (tempOverrideClips.Count == 0) return;
            
            // Sort by priority (highest first)
            tempOverrideClips.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            processedMorphs.Clear();
            
            foreach (var clip in tempOverrideClips)
            {
                tempMorphValues.Clear();
                clip.GetAllMorphValues(time, tempMorphValues);
                
                foreach (var kvp in tempMorphValues)
                {
                    // Only apply if this morph hasn't been overridden by higher priority clip
                    if (!processedMorphs.Contains(kvp.Key))
                    {
                        accumulatedValues[kvp.Key] = Mathf.Clamp(kvp.Value, 0f, 100f);
                        processedMorphs.Add(kvp.Key);
                    }
                }
            }

            tempOverrideClips.Clear();
        }
        
        /// <summary>
        /// Apply accumulated values to the mesh renderer
        /// </summary>
        private void ApplyMorphValues()
        {
            foreach (var kvp in accumulatedValues)
            {
                if (blendshapeIndices.TryGetValue(kvp.Key, out int index))
                {
                    float newValue = kvp.Value;
                    
                    // Only update if value changed (performance optimization)
                    if (!Mathf.Approximately(currentValues[kvp.Key], newValue))
                    {
                        targetRenderer.SetBlendShapeWeight(index, newValue);
                        currentValues[kvp.Key] = newValue;
                    }
                }
                else
                {
                    // Debug.LogWarning($"[MorphTrack] Blendshape '{kvp.Key}' not found on mesh '{targetMesh.name}'");
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Add a morph key clip to this track
        /// </summary>
        /// <param name="start">Start time</param>
        /// <param name="duration">Duration</param>
        /// <param name="morphKeys">Morph keys</param>
        /// <returns>Created clip</returns>
        public MorphKeyClip AddKeyClip(float start, float duration, List<MorphKey> morphKeys)
        {
            var clip = new MorphKeyClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                keys = morphKeys ?? new List<MorphKey>()
            };
            
            clips.Add(clip);
            return clip;
        }
        
        /// <summary>
        /// Add a morph curve clip to this track
        /// </summary>
        /// <param name="start">Start time</param>
        /// <param name="duration">Duration</param>
        /// <param name="channels">Curve channels</param>
        /// <returns>Created clip</returns>
        public MorphCurveClip AddCurveClip(float start, float duration, List<MorphCurveClip.CurveChannel> channels)
        {
            var clip = new MorphCurveClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                channels = channels ?? new List<MorphCurveClip.CurveChannel>()
            };
            
            clips.Add(clip);
            return clip;
        }
        
        /// <summary>
        /// Get current morph value for a blendshape
        /// </summary>
        /// <param name="morphId">Blendshape name</param>
        /// <returns>Current weight (0-100) or null if not found</returns>
        public float? GetCurrentMorphValue(string morphId)
        {
            return currentValues.TryGetValue(morphId, out float value) ? value : (float?)null;
        }
        
        /// <summary>
        /// Get all available blendshape names
        /// </summary>
        /// <returns>Collection of blendshape names</returns>
        public IReadOnlyCollection<string> GetAvailableBlendshapes()
        {
            return blendshapeIndices.Keys;
        }
        
        /// <summary>
        /// Manually set a morph value (useful for testing)
        /// </summary>
        /// <param name="morphId">Blendshape name</param>
        /// <param name="weight">Weight (0-100)</param>
        public void SetMorphValue(string morphId, float weight)
        {
            if (blendshapeIndices.TryGetValue(morphId, out int index))
            {
                targetRenderer.SetBlendShapeWeight(index, weight);
                currentValues[morphId] = weight;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Interface for morph clips (both key and curve types)
    /// </summary>
    public interface IMorphClip : IMiniClip
    {
        MorphBlendMode BlendMode { get; }
        int Priority { get; }
        float Weight { get; }
        
        float? GetMorphValue(string morphId, float globalTime);
        void GetAllMorphValues(float globalTime, Dictionary<string, float> result);
    }
    
    // Implement interface for both clip types
    public partial class MorphKeyClip : IMorphClip
    {
        public MorphBlendMode BlendMode => blendMode;
        public int Priority => priority;
        public float Weight => weight;
        
        public void GetAllMorphValues(float globalTime, Dictionary<string, float> result)
        {
            var values = GetAllMorphValues(globalTime);
            foreach (var kvp in values)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
    }
    
    public partial class MorphCurveClip : IMorphClip
    {
        public MorphBlendMode BlendMode => blendMode;
        public int Priority => priority;
        public float Weight => weight;
        
        public void GetAllMorphValues(float globalTime, Dictionary<string, float> result)
        {
            var values = GetAllMorphValues(globalTime);
            foreach (var kvp in values)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
    }
}