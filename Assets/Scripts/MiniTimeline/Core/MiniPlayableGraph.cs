using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Wrapper for Unity Playables system to manage animation and audio mixing
    /// Provides high-level API for crossfading, mixing, and playback control
    /// </summary>
    public class MiniPlayableGraph : IDisposable
    {
        private PlayableGraph graph;
        private bool isValid;
        private string graphName;
        
        // Animation components
        private AnimationPlayableOutput animOutput;
        private AnimationMixerPlayable animMixer;
        private readonly Dictionary<string, AnimationClipPlayable> clipPlayables = new Dictionary<string, AnimationClipPlayable>();
        private readonly List<AnimationClipPlayable> activeClipPlayables = new List<AnimationClipPlayable>();
        
        // Audio components  
        private AudioPlayableOutput audioOutput;
        private AudioMixerPlayable audioMixer;
        private readonly Dictionary<string, AudioClipPlayable> audioClipPlayables = new Dictionary<string, AudioClipPlayable>();
        
        // Crossfade state
        private float crossfadeTime;
        private float crossfadeDuration;
        private AnimationClipPlayable fadeOutClip;
        private AnimationClipPlayable fadeInClip;
        private bool isCrossfading;
        
        #region Properties
        
        /// <summary>
        /// Whether the graph is valid and ready to use
        /// </summary>
        public bool IsValid => isValid && graph.IsValid();
        
        /// <summary>
        /// Current playback time of the graph
        /// </summary>
        public double Time => IsValid ? graph.GetRootPlayable(0).GetTime() : 0.0;
        
        /// <summary>
        /// Whether graph is currently playing
        /// </summary>
        public bool IsPlaying => IsValid && graph.IsPlaying();
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Create a new playable graph
        /// </summary>
        /// <param name="name">Graph name for debugging</param>
        /// <param name="frameData">Frame rate for evaluation</param>
        public MiniPlayableGraph(string name = "MiniTimelineGraph", FrameData frameData = default)
        {
            graphName = name;
            CreateGraph(frameData);
        }
        
        /// <summary>
        /// Initialize the playable graph with animation and audio outputs
        /// </summary>
        /// <param name="animator">Target animator for animation output</param>
        /// <param name="audioSource">Target audio source for audio output (optional)</param>
        public void Initialize(Animator animator, AudioSource audioSource = null)
        {
            Debug.Log($"[MiniPlayableGraph] Initialize called - IsValid: {IsValid}");
            
            if (!IsValid)
            {
                Debug.LogError("[MiniPlayableGraph] Cannot initialize invalid graph");
                return;
            }
            
            // Setup animation output
            if (animator != null)
            {
                Debug.Log($"[MiniPlayableGraph] Setting up animation output for animator: {animator.name}");
                SetupAnimationOutput(animator);
            }
            else
            {
                Debug.LogWarning("[MiniPlayableGraph] No animator provided for initialization");
            }
            
            // Setup audio output
            if (audioSource != null)
            {
                Debug.Log($"[MiniPlayableGraph] Setting up audio output for audio source: {audioSource.name}");
                SetupAudioOutput(audioSource);
            }
            
            Debug.Log("[MiniPlayableGraph] Initialize completed");
        }
        
        private void CreateGraph(FrameData frameData)
        {
            try
            {
                graph = PlayableGraph.Create(graphName);
                isValid = true;
                
                if (frameData.frameId > 0)
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                }
                
                Debug.Log($"[MiniPlayableGraph] Created graph: {graphName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniPlayableGraph] Failed to create graph: {e.Message}");
                isValid = false;
            }
        }
        
        private void SetupAnimationOutput(Animator animator)
        {
            if (!IsValid) 
            {
                Debug.LogError("[MiniPlayableGraph] Cannot setup animation output - graph is invalid");
                return;
            }
            
            try
            {
                Debug.Log($"[MiniPlayableGraph] Starting animation output setup for {animator.name}");
                
                // CRITICAL: Check if the animator has a controller
                if (animator.runtimeAnimatorController != null)
                {
                    Debug.LogWarning($"[MiniPlayableGraph] Animator {animator.name} still has RuntimeAnimatorController '{animator.runtimeAnimatorController.name}' - this will conflict!");
                }
                else
                {
                    Debug.Log($"[MiniPlayableGraph] Animator {animator.name} has no RuntimeAnimatorController - good for PlayableGraph control");
                }
                
                // Create animation mixer with 4 inputs for crossfading
                Debug.Log("[MiniPlayableGraph] Creating AnimationMixerPlayable...");
                animMixer = AnimationMixerPlayable.Create(graph, 4);
                Debug.Log($"[MiniPlayableGraph] AnimationMixerPlayable created. Valid: {animMixer.IsValid()}");
                
                // Create animation output
                Debug.Log("[MiniPlayableGraph] Creating AnimationPlayableOutput...");
                animOutput = AnimationPlayableOutput.Create(graph, "AnimationOutput", animator);
                Debug.Log($"[MiniPlayableGraph] AnimationPlayableOutput created. Valid: {animOutput.IsOutputValid()}");
                
                Debug.Log("[MiniPlayableGraph] Connecting mixer to output...");
                animOutput.SetSourcePlayable(animMixer);
                Debug.Log("[MiniPlayableGraph] Mixer connected to output successfully");
                
                // Ensure the animator is enabled and ready
                if (!animator.enabled)
                {
                    animator.enabled = true;
                    Debug.Log($"[MiniPlayableGraph] Enabled animator {animator.name}");
                }
                
                Debug.Log($"[MiniPlayableGraph] Setup animation output for {animator.name}");
                Debug.Log($"[MiniPlayableGraph] Final state - Mixer valid: {animMixer.IsValid()}, Output valid: {animOutput.IsOutputValid()}");
                Debug.Log($"[MiniPlayableGraph] Animator culling mode: {animator.cullingMode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniPlayableGraph] Failed to setup animation output: {e.Message}");
                Debug.LogError($"[MiniPlayableGraph] Stack trace: {e.StackTrace}");
            }
        }
        
        private void SetupAudioOutput(AudioSource audioSource)
        {
            if (!IsValid) return;
            
            try
            {
                // Create audio mixer with 4 inputs
                audioMixer = AudioMixerPlayable.Create(graph, 4);
                
                // Create audio output
                audioOutput = AudioPlayableOutput.Create(graph, "AudioOutput", audioSource);
                audioOutput.SetSourcePlayable(audioMixer);
                
                Debug.Log($"[MiniPlayableGraph] Setup audio output for {audioSource.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniPlayableGraph] Failed to setup audio output: {e.Message}");
            }
        }
        
        #endregion
        
        #region Playback Control
        
        /// <summary>
        /// Play the graph
        /// </summary>
        public void Play()
        {
            if (IsValid)
            {
                graph.Play();
                Debug.Log($"[MiniPlayableGraph] Graph started playing. IsPlaying: {graph.IsPlaying()}");
            }
            else
            {
                Debug.LogError("[MiniPlayableGraph] Cannot play invalid graph");
            }
        }
        
        /// <summary>
        /// Stop the graph
        /// </summary>
        public void Stop()
        {
            if (IsValid)
            {
                graph.Stop();
            }
        }
        
        /// <summary>
        /// Evaluate the graph manually
        /// </summary>
        /// <param name="deltaTime">Time delta to advance</param>
        public void Evaluate(float deltaTime = 0f)
        {
            if (IsValid)
            {
                if (deltaTime > 0f)
                {
                    graph.Evaluate(deltaTime);
                    Debug.Log($"[MiniPlayableGraph] Evaluated graph with deltaTime: {deltaTime:F4}");
                }
                else
                {
                    graph.Evaluate();
                    Debug.Log("[MiniPlayableGraph] Evaluated graph (no time advance)");
                }
            }
            else
            {
                Debug.LogWarning("[MiniPlayableGraph] Cannot evaluate invalid graph");
            }
        }
        
        /// <summary>
        /// Set the graph time directly
        /// </summary>
        /// <param name="time">Target time</param>
        public void SetTime(double time)
        {
            if (IsValid && graph.GetRootPlayableCount() > 0)
            {
                for (int i = 0; i < graph.GetRootPlayableCount(); i++)
                {
                    var rootPlayable = graph.GetRootPlayable(i);
                    if (rootPlayable.IsValid())
                    {
                        rootPlayable.SetTime(time);
                    }
                }
            }
        }
        
        /// <summary>
        /// Clear all active clips and stop animation
        /// </summary>
        public void ClearActiveClips()
        {
            if (!animMixer.IsValid()) return;
            
            // Set all weights to 0 and disconnect
            for (int i = 0; i < animMixer.GetInputCount(); i++)
            {
                animMixer.SetInputWeight(i, 0f);
                animMixer.DisconnectInput(i);
            }
            
            // Clear tracking variables
            activeClipPlayables.Clear();
            isCrossfading = false;
        }
        
        /// <summary>
        /// Check if a clip is currently active in the graph
        /// </summary>
        /// <param name="clipId">Clip ID to check</param>
        /// <returns>True if the clip is active with weight > 0</returns>
        public bool IsClipActive(string clipId)
        {
            if (!animMixer.IsValid() || !clipPlayables.TryGetValue(clipId, out var clipPlayable))
                return false;
            
            // Check if the clip is connected to any input with weight > 0
            for (int i = 0; i < animMixer.GetInputCount(); i++)
            {
                var connectedPlayable = animMixer.GetInput(i);
                if (connectedPlayable.IsValid() && connectedPlayable.Equals(clipPlayable))
                {
                    float weight = animMixer.GetInputWeight(i);
                    if (weight > 0f)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Animation Clip Management
        
        /// <summary>
        /// Play an animation clip with optional crossfade
        /// </summary>
        /// <param name="clip">Animation clip to play</param>
        /// <param name="fadeTime">Fade in duration</param>
        /// <param name="clipId">Unique ID for the clip</param>
        /// <returns>Created clip playable</returns>
        public AnimationClipPlayable PlayAnimationClip(AnimationClip clip, float fadeTime = 0f, string clipId = null)
        {
            if (!IsValid || clip == null) 
            {
                Debug.LogWarning($"[MiniPlayableGraph] Cannot play clip - graph valid: {IsValid}, clip null: {clip == null}");
                return default;
            }
            
            clipId = clipId ?? clip.name;
            Debug.Log($"[MiniPlayableGraph] PlayAnimationClip: '{clipId}' (fadeTime: {fadeTime:F3})");
            
            // Get or create clip playable
            if (!clipPlayables.TryGetValue(clipId, out var clipPlayable))
            {
                clipPlayable = AnimationClipPlayable.Create(graph, clip);
                clipPlayables[clipId] = clipPlayable;
                Debug.Log($"[MiniPlayableGraph] Created new AnimationClipPlayable for '{clipId}', valid: {clipPlayable.IsValid()}");
                Debug.Log($"[MiniPlayableGraph] Clip '{clipId}' length: {clip.length:F3}s, looped: {clip.isLooping}");
                
                // Verify the clip playable was created correctly
                if (!clipPlayable.IsValid())
                {
                    Debug.LogError($"[MiniPlayableGraph] Failed to create valid AnimationClipPlayable for '{clipId}'!");
                    return default;
                }
            }
            else
            {
                Debug.Log($"[MiniPlayableGraph] Reusing existing AnimationClipPlayable for '{clipId}', valid: {clipPlayable.IsValid()}");
                
                // Verify existing playable is still valid
                if (!clipPlayable.IsValid())
                {
                    Debug.LogWarning($"[MiniPlayableGraph] Existing AnimationClipPlayable for '{clipId}' is invalid, recreating...");
                    clipPlayable = AnimationClipPlayable.Create(graph, clip);
                    clipPlayables[clipId] = clipPlayable;
                }
            }
            
            // Check if we're already playing this clip or crossfading to it
            if (activeClipPlayables.Count > 0 && activeClipPlayables[0].Equals(clipPlayable))
            {
                Debug.Log($"[MiniPlayableGraph] Clip '{clipId}' is already active");
                return clipPlayable;
            }
            
            if (isCrossfading && fadeInClip.Equals(clipPlayable))
            {
                Debug.Log($"[MiniPlayableGraph] Clip '{clipId}' is already being faded in");
                return clipPlayable;
            }
            
            // Start crossfade if needed
            if (fadeTime > 0f && activeClipPlayables.Count > 0)
            {
                Debug.Log($"[MiniPlayableGraph] Starting crossfade to '{clipId}' over {fadeTime:F3}s");
                StartCrossfade(activeClipPlayables[0], clipPlayable, fadeTime);
            }
            else
            {
                Debug.Log($"[MiniPlayableGraph] Setting '{clipId}' as active clip with weight 1.0");
                SetActiveClipPlayable(clipPlayable, 1f);
            }
            
            return clipPlayable;
        }
        
        /// <summary>
        /// Crossfade to a new animation clip
        /// </summary>
        /// <param name="clip">Target animation clip</param>
        /// <param name="duration">Crossfade duration</param>
        /// <param name="clipId">Unique ID for the clip</param>
        public void CrossfadeToClip(AnimationClip clip, float duration, string clipId = null)
        {
            Debug.Log($"[MiniPlayableGraph] CrossfadeToClip: '{clipId ?? clip.name}' with duration {duration:F3}s");
            var result = PlayAnimationClip(clip, duration, clipId);
            Debug.Log($"[MiniPlayableGraph] PlayAnimationClip result valid: {result.IsValid()}");
        }
        
        /// <summary>
        /// Set normalized time for an animation clip
        /// </summary>
        /// <param name="clipId">Clip ID</param>
        /// <param name="normalizedTime">Normalized time (0-1)</param>
        public void SetClipNormalizedTime(string clipId, float normalizedTime)
        {
            if (clipPlayables.TryGetValue(clipId, out var clipPlayable))
            {
                var clip = clipPlayable.GetAnimationClip();
                if (clip != null)
                {
                    double time = normalizedTime * clip.length;
                    clipPlayable.SetTime(time);
                    Debug.Log($"[MiniPlayableGraph] Set clip '{clipId}' time to {time:F3}s (normalized: {normalizedTime:F3})");
                }
                else
                {
                    Debug.LogWarning($"[MiniPlayableGraph] Clip playable for '{clipId}' has no AnimationClip");
                }
            }
            else
            {
                Debug.LogWarning($"[MiniPlayableGraph] No clip playable found for '{clipId}'");
            }
        }
        
        /// <summary>
        /// Set the active clip playable with weight
        /// </summary>
        private void SetActiveClipPlayable(AnimationClipPlayable clipPlayable, float weight)
        {
            if (!animMixer.IsValid()) 
            {
                Debug.LogError("[MiniPlayableGraph] Cannot set active clip - animation mixer is invalid");
                return;
            }
            
            // Clear weights first
            for (int i = 0; i < animMixer.GetInputCount(); i++)
            {
                animMixer.SetInputWeight(i, 0f);
            }
            
            // Set new active clip
            activeClipPlayables.Clear();
            activeClipPlayables.Add(clipPlayable);
            
            // Disconnect input 0 if it's connected to a different playable
            var currentInput = animMixer.GetInput(0);
            if (currentInput.IsValid() && !currentInput.Equals(clipPlayable))
            {
                animMixer.DisconnectInput(0);
                Debug.Log("[MiniPlayableGraph] Disconnected previous clip from input 0");
            }
            
            // Connect to mixer if not already connected
            if (!animMixer.GetInput(0).IsValid() || !animMixer.GetInput(0).Equals(clipPlayable))
            {
                animMixer.ConnectInput(0, clipPlayable, 0);
                Debug.Log("[MiniPlayableGraph] Connected new clip to input 0");
            }
            animMixer.SetInputWeight(0, weight);
            Debug.Log($"[MiniPlayableGraph] Set active clip with weight {weight:F3}");
        }
        
        /// <summary>
        /// Start crossfade between two clips
        /// </summary>
        private void StartCrossfade(AnimationClipPlayable fromClip, AnimationClipPlayable toClip, float duration)
        {
            fadeOutClip = fromClip;
            fadeInClip = toClip;
            crossfadeDuration = duration;
            crossfadeTime = 0f;
            isCrossfading = true;
            
            // Set all weights to 0 first
            for (int i = 0; i < animMixer.GetInputCount(); i++)
            {
                animMixer.SetInputWeight(i, 0f);
            }
            
            // Safely connect fromClip to input 0
            var input0 = animMixer.GetInput(0);
            if (input0.IsValid() && !input0.Equals(fromClip))
            {
                animMixer.DisconnectInput(0);
            }
            if (!animMixer.GetInput(0).IsValid() || !animMixer.GetInput(0).Equals(fromClip))
            {
                animMixer.ConnectInput(0, fromClip, 0);
            }
            
            // Safely connect toClip to input 1
            var input1 = animMixer.GetInput(1);
            if (input1.IsValid() && !input1.Equals(toClip))
            {
                animMixer.DisconnectInput(1);
            }
            if (!animMixer.GetInput(1).IsValid() || !animMixer.GetInput(1).Equals(toClip))
            {
                animMixer.ConnectInput(1, toClip, 0);
            }
            
            // Start with fade out clip at full weight
            animMixer.SetInputWeight(0, 1f);
            animMixer.SetInputWeight(1, 0f);
        }
        
        /// <summary>
        /// Update crossfade progress
        /// </summary>
        /// <param name="deltaTime">Time delta</param>
        public void UpdateCrossfade(float deltaTime)
        {
            if (!isCrossfading) return;
            
            crossfadeTime += deltaTime;
            float progress = Mathf.Clamp01(crossfadeTime / crossfadeDuration);
            
            // Update weights
            animMixer.SetInputWeight(0, 1f - progress);
            animMixer.SetInputWeight(1, progress);
            
            // Finish crossfade
            if (progress >= 1f)
            {
                isCrossfading = false;
                SetActiveClipPlayable(fadeInClip, 1f);
            }
        }
        
        #endregion
        
        #region Audio Clip Management
        
        /// <summary>
        /// Play an audio clip
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        /// <param name="clipId">Unique ID for the clip</param>
        /// <returns>Created audio clip playable</returns>
        public AudioClipPlayable PlayAudioClip(AudioClip clip, string clipId = null)
        {
            if (!IsValid || clip == null || !audioMixer.IsValid()) return default;
            
            clipId = clipId ?? clip.name;
            
            // Get or create audio clip playable
            if (!audioClipPlayables.TryGetValue(clipId, out var clipPlayable))
            {
                clipPlayable = AudioClipPlayable.Create(graph, clip, false);
                audioClipPlayables[clipId] = clipPlayable;
            }
            
            // Connect to audio mixer
            audioMixer.ConnectInput(0, clipPlayable, 0);
            audioMixer.SetInputWeight(0, 1f);
            
            return clipPlayable;
        }
        
        /// <summary>
        /// Set audio clip time
        /// </summary>
        /// <param name="clipId">Clip ID</param>
        /// <param name="time">Time in seconds</param>
        public void SetAudioClipTime(string clipId, double time)
        {
            if (audioClipPlayables.TryGetValue(clipId, out var clipPlayable))
            {
                clipPlayable.SetTime(time);
            }
        }
        
        #endregion
        

        
        #region Cleanup
        
        /// <summary>
        /// Clear all cached playables
        /// </summary>
        public void ClearCache()
        {
            foreach (var clipPlayable in clipPlayables.Values)
            {
                if (clipPlayable.IsValid())
                {
                    clipPlayable.Destroy();
                }
            }
            clipPlayables.Clear();
            
            foreach (var audioClipPlayable in audioClipPlayables.Values)
            {
                if (audioClipPlayable.IsValid())
                {
                    audioClipPlayable.Destroy();
                }
            }
            audioClipPlayables.Clear();
            
            activeClipPlayables.Clear();
        }
        
        /// <summary>
        /// Dispose the graph and cleanup resources
        /// </summary>
        public void Dispose()
        {
            if (isValid)
            {
                ClearCache();
                
                if (graph.IsValid())
                {
                    graph.Destroy();
                }
                
                isValid = false;
                Debug.Log($"[MiniPlayableGraph] Disposed graph: {graphName}");
            }
        }
        
        #endregion
    }
}