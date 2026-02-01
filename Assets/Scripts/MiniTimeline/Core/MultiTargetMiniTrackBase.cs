using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Base class for tracks that require multiple target bindings in addition to the primary binding.
    /// </summary>
    [Serializable]
    public abstract class MultiTargetMiniTrackBase<TClip> : MiniTrackBase<TClip>
        where TClip : IMiniClip
    {
        [SerializeField]
        protected List<string> targetBindKeys = new List<string>();

        [NonSerialized]
        protected List<Transform> targets = new List<Transform>();

        public List<string> TargetBindKeys { get => targetBindKeys; set => targetBindKeys = value; }

        public override void Bind(BindableObjectManager context)
        {
            // Bind the primary target object using base logic
            base.Bind(context);

            if (context == null) return;

            // Bind secondary targets
            targets.Clear();
            if (targetBindKeys != null)
            {
                foreach (var key in targetBindKeys)
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        targets.Add(null);
                        continue;
                    }

                    var target = context.Resolve<Transform>(key);
                    if (target == null)
                    {
                        // Fallback to GameObject if Transform resolution fails
                        var go = context.Resolve<GameObject>(key);
                        if (go != null) target = go.transform;
                    }
                    
                    targets.Add(target);
                    
                    if (target == null)
                    {
                        Debug.LogWarning($"[MultiTargetMiniTrack] Failed to resolve secondary target key '{key}' for track '{Id}'");
                    }
                }
            }
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();
            // Ensure targets list is at least as long as bind keys if Bind wasn't called (though it should be)
            if (targets.Count == 0 && targetBindKeys != null && targetBindKeys.Count > 0)
            {
                Debug.LogWarning($"[MultiTargetMiniTrack] Targets list is empty for track '{Id}' in OnPrepare. Re-binding might be needed.");
            }
        }
    }
}
