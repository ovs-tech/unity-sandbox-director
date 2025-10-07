using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.Tracks
{
    public class UmaWardrobeTrack : MiniTrackBase<UmaWardrobeClip>
    {
        private UmaAvatar umaAvatar;
        private UmaWardrobeClip lastAppliedClip;

        protected override void OnPrepare()
        {
            lastAppliedClip = null;
            if (targetObject is GameObject go)
            {
                umaAvatar = go.GetComponent<UmaAvatar>();
            }

            if (umaAvatar == null)
            {
                Debug.LogError("[UmaWardrobeTrack] UmaAvatar component not found on target object.");
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            if (umaAvatar == null) return;

            var activeClips = GetActiveClips(time);
            UmaWardrobeClip currentClip = null;
            if (activeClips.Count > 0)
            {
                // Apply the last active clip.
                currentClip = activeClips[activeClips.Count - 1];
            }

            if (currentClip != lastAppliedClip)
            {
                lastAppliedClip = currentClip;
                if (currentClip != null && !string.IsNullOrEmpty(currentClip.wardrobeJson))
                {
                    umaAvatar.LoadWardrobeFromJson(currentClip.wardrobeJson);
                }
            }
        }

        protected override void OnCleanup()
        {
            lastAppliedClip = null;
        }
    }
}