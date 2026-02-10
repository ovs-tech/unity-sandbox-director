using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.MiniTimeline.Serialization;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Base implementation of IMiniTrack with common functionality
    /// </summary>
    [Serializable]
    public abstract class MiniTrackBase<TClip> : IMiniTrack, ISerializationCallbackReceiver
        where TClip : IMiniClip
    {
        [SerializeField] private string id;
        [SerializeField] private string name;
        [SerializeField] private string bindKey;
        [SerializeField] private bool enabled = true;
        [SerializeField] private int order = 0;
        [SerializeField] private EvaluateMode evaluateMode = EvaluateMode.Continuous;

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string BindKey { get => bindKey; set => bindKey = value; }
        public bool Enabled { get => enabled; set => enabled = value; }
        public int Order { get => order; set => order = value; }
        public EvaluateMode EvaluateMode { get => evaluateMode; set => evaluateMode = value; }

        public bool IsBound => isBound;
        public bool IsReady => Enabled && isPrepared;
        
        [NonSerialized] protected List<TClip> clips = new List<TClip>();
        [SerializeField] private List<SerializedWrapper> _serializedClips = new List<SerializedWrapper>();

        [NonSerialized] protected UnityEngine.Object targetObject;
        [NonSerialized] protected bool isBound;
        [NonSerialized] protected bool isPrepared;
        [NonSerialized] protected bool wasActive; // Track previous active state for OnEnter/OnExit detection

        public void OnBeforeSerialize()
        {
            Debug.Log($"MiniTrackBase.OnBeforeSerialize: trackId={Id}, clips={(clips == null ? 0 : clips.Count)}");
            _serializedClips.Clear();
            if (clips == null) return;

            int serialized = 0;
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                _serializedClips.Add(new SerializedWrapper
                {
                    type = clip.GetType().AssemblyQualifiedName,
                    data = JsonUtility.ToJson(clip)
                });
                serialized++;
            }

            Debug.Log($"MiniTrackBase.OnBeforeSerialize: trackId={Id}, serializedClips={serialized}");
        }

        public void OnAfterDeserialize()
        {
            Debug.Log($"MiniTrackBase.OnAfterDeserialize: trackId={Id}, serializedClips={( _serializedClips == null ? 0 : _serializedClips.Count)}");
            if (clips == null) clips = new List<TClip>();
            clips.Clear();

            if (_serializedClips == null) return;

            int added = 0;
            foreach (var wrapped in _serializedClips)
            {
                Type type = TypeResolver.ResolveType(wrapped.type);
                if (type != null)
                {
                    try
                    {
                        // normalize stored type name
                        wrapped.type = type.AssemblyQualifiedName;
                    }
                    catch { }

                    TClip clip = (TClip)JsonUtility.FromJson(wrapped.data, type);
                    clips.Add(clip);
                    added++;
                }
                else
                {
                    Debug.LogWarning($"MiniTrackBase.OnAfterDeserialize: type not found: {wrapped.type}");
                }
            }

            Debug.Log($"MiniTrackBase.OnAfterDeserialize: trackId={Id}, clips_added={added}, total_clips={clips.Count}");
        }
        
        public virtual void Bind(BindableObjectManager context)
        {
            if (context == null)
            {
                Debug.LogWarning($"[MiniTrack] Cannot bind track '{Id}': BindingContext is null");
                isBound = false;
                isPrepared = false;
                return;
            }
            
            // Store previous target to detect changes
            var previousTarget = targetObject;
            
            if (!string.IsNullOrEmpty(BindKey))
            {
                targetObject = context.Resolve<UnityEngine.Object>(BindKey);
                isBound = targetObject != null;
                
                if (!isBound)
                {
                    Debug.LogWarning($"[MiniTrack] Failed to bind track '{Id}' with key '{BindKey}'");
                    isPrepared = false;
                }
                else if (previousTarget != targetObject)
                {
                    // Target changed, need to re-prepare
                    isPrepared = false;
                }
            }
            else
            {
                // Track doesn't require binding (e.g., global tracks)
                isBound = true;
            }
        }
        
        public virtual void Prepare()
        {
            if (!isBound)
            {
                Debug.LogWarning($"[MiniTrack] Cannot prepare unbound track '{Id}'");
                return;
            }
            
            OnPrepare();
            isPrepared = true;
            wasActive = false; // Reset state when preparing
        }
        
        public virtual void Evaluate(float time, bool scrub)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"[MiniTrack] Track '{Id}' is not ready (Enabled={Enabled}, IsPrepared={isPrepared})");
                return;
            }
            
            // Determine if track is active (has active clips at this time)
            bool isActive = IsActiveAtTime(time);
            
            // State transition detection
            bool justEntered = isActive && !wasActive;
            bool justExited = !isActive && wasActive;
            
            // Debug state transitions
            if (justEntered)
            {
                Debug.Log($"[MiniTrack] Track '{Id}' entered active state at time {time:F2}s (mode: {EvaluateMode})");
            }
            else if (justExited)
            {
                Debug.Log($"[MiniTrack] Track '{Id}' exited active state at time {time:F2}s (mode: {EvaluateMode})");
            }
            
            // Trigger callbacks based on EvaluateMode
            switch (EvaluateMode)
            {
                case EvaluateMode.OnEnter:
                    if (justEntered)
                    {
                        Debug.Log($"[MiniTrack] OnEnter triggered for track '{Id}' at {time:F2}s");
                        OnEnter(time, scrub);
                    }
                    break;
                    
                case EvaluateMode.OnExit:
                    if (justExited)
                    {
                        Debug.Log($"[MiniTrack] OnExit triggered for track '{Id}' at {time:F2}s");
                        OnExit(time, scrub);
                    }
                    break;
                    
                case EvaluateMode.OnEnterAndExit:
                    if (justEntered)
                    {
                        Debug.Log($"[MiniTrack] OnEnter triggered for track '{Id}' at {time:F2}s");
                        OnEnter(time, scrub);
                    }
                    else if (justExited)
                    {
                        Debug.Log($"[MiniTrack] OnExit triggered for track '{Id}' at {time:F2}s");
                        OnExit(time, scrub);
                    }
                    break;
                    
                case EvaluateMode.Continuous:
                default:
                    if (justEntered)
                    {
                        Debug.Log($"[MiniTrack] OnEnter triggered for track '{Id}' at {time:F2}s");
                        OnEnter(time, scrub);
                    }
                    
                    if (isActive)
                    {
                        // Only log every 60th frame to avoid spam
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"[MiniTrack] OnEvaluate for track '{Id}' at {time:F2}s (continuous)");
                        }
                        OnEvaluate(time, scrub);
                    }
                    
                    if (justExited)
                    {
                        Debug.Log($"[MiniTrack] OnExit triggered for track '{Id}' at {time:F2}s");
                        OnExit(time, scrub);
                    }
                    break;
            }
            
            // Update state for next frame
            wasActive = isActive;
        }
        
        public virtual IEnumerable<IMiniClip> GetClips()
        {
            foreach (var clip in clips)
                yield return clip;
        }
        
        public virtual void AddClip(IMiniClip clip)
        {
            if (clip is TClip typedClip)
            {
                clips.Add(typedClip);
                // Sort clips by start time to maintain order
                clips.Sort((a, b) => a.Start.CompareTo(b.Start));
            }
            else
            {
                Debug.LogError($"[MiniTrack] Cannot add clip of type {clip?.GetType()} to track that expects {typeof(TClip)}");
            }
        }
        
        public virtual bool RemoveClip(IMiniClip clip)
        {
            if (clip is TClip typedClip)
            {
                return clips.Remove(typedClip);
            }
            return false;
        }
        
        public virtual void OnProjectClosed()
        {
            OnCleanup();
            isPrepared = false;
            isBound = false;
            targetObject = null;
            wasActive = false; // Reset active state
        }
        
        /// <summary>
        /// Set clips for this track
        /// </summary>
        public virtual void SetClips(List<TClip> newClips)
        {
            clips = newClips ?? new List<TClip>();
        }
        
        /// <summary>
        /// Get active clips at given time
        /// </summary>
        protected virtual List<TClip> GetActiveClips(float time)
        {
            var activeClips = new List<TClip>();
            
            foreach (var clip in clips)
            {
                if (clip.Contains(time))
                {
                    activeClips.Add(clip);
                }
            }
            
            return activeClips;
        }
        
        /// <summary>
        /// Check if track is active at given time (has active clips)
        /// </summary>
        protected virtual bool IsActiveAtTime(float time)
        {
            foreach (var clip in clips)
            {
                if (clip.Contains(time))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Override this to implement track-specific preparation
        /// </summary>
        protected virtual void OnPrepare() { }
        
        /// <summary>
        /// Called when track enters active state (first active clip starts)
        /// </summary>
        protected virtual void OnEnter(float time, bool scrub) { }
        
        /// <summary>
        /// Called when track exits active state (all clips finished)
        /// </summary>
        protected virtual void OnExit(float time, bool scrub) { }
        
        /// <summary>
        /// Override this to implement track-specific evaluation
        /// Called continuously during Continuous mode when track is active
        /// </summary>
        protected abstract void OnEvaluate(float time, bool scrub);
        
        /// <summary>
        /// Override this to implement track-specific cleanup
        /// </summary>
        protected virtual void OnCleanup() { }
    }
}