using System.Collections.Generic;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Base implementation of IMiniTrack with common functionality
    /// </summary>
    public abstract class MiniTrackBase<TClip> : IMiniTrack 
        where TClip : IMiniClip
    {
        public string Id { get; set; }
        public string BindKey { get; set; }
        public bool Enabled { get; set; } = true;
        public virtual int Order => 0;
        
        protected List<TClip> clips = new List<TClip>();
        protected UnityEngine.Object targetObject;
        protected bool isBound;
        protected bool isPrepared;
        
        public virtual void Bind(BindingContext context)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogWarning($"[MiniTrack] Cannot bind track '{Id}': BindingContext is null");
                isBound = false;
                return;
            }
            
            if (!string.IsNullOrEmpty(BindKey))
            {
                targetObject = context.Resolve<UnityEngine.Object>(BindKey);
                isBound = targetObject != null;
                
                if (!isBound)
                {
                    UnityEngine.Debug.LogWarning($"[MiniTrack] Failed to bind track '{Id}' with key '{BindKey}'");
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
                UnityEngine.Debug.LogWarning($"[MiniTrack] Cannot prepare unbound track '{Id}'");
                return;
            }
            
            OnPrepare();
            isPrepared = true;
        }
        
        public virtual void Evaluate(float time, bool scrub)
        {
            if (!Enabled || !isPrepared) return;
            
            OnEvaluate(time, scrub);
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
                UnityEngine.Debug.LogError($"[MiniTrack] Cannot add clip of type {clip?.GetType()} to track that expects {typeof(TClip)}");
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
        /// Override this to implement track-specific preparation
        /// </summary>
        protected virtual void OnPrepare() { }
        
        /// <summary>
        /// Override this to implement track-specific evaluation
        /// </summary>
        protected abstract void OnEvaluate(float time, bool scrub);
        
        /// <summary>
        /// Override this to implement track-specific cleanup
        /// </summary>
        protected virtual void OnCleanup() { }
    }
}