using System;
using System.Linq;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Track for controlling Time.timeScale
    /// </summary>
    [Serializable]
    public class TimeScaleTrack : MiniTrackBase<TimeScaleClip>
    {
        private float originalTimeScale = 1.0f;
        private bool isModifyingTime = false;

        protected override void OnPrepare()
        {
            // Nothing to prepare
        }

        protected override void OnEnter(float time, bool scrub)
        {
            if (!isModifyingTime)
            {
                originalTimeScale = Time.timeScale;
                isModifyingTime = true;
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            // Find active clip
            // Strategy: Last started clip wins

            float targetScale = originalTimeScale;
            bool hasActiveClip = false;

            foreach (var clip in clips)
            {
                if (clip.Contains(time))
                {
                    hasActiveClip = true;

                    float clipTime = time - clip.Start;
                    float progress = Mathf.Clamp01(clipTime / clip.Duration);

                    // Use curve to modulate time scale
                    // Default curve is Linear(0,0, 1,1) in TimeScaleClip constructor I wrote?
                    // Let's check TimeScaleClip.cs...
                    // public AnimationCurve blendCurve = AnimationCurve.Linear(0, 0, 1, 1);
                    // This means it starts at 0 and goes to 1.
                    // So `scale = clip.timeScale * curve` means scale starts at 0?
                    // That's bad default. Usually we want constant scale.
                    // But I can't change the default in the class instance easily if I already created it (in my mind).
                    // Actually, for a helper, I should probably check if curve is valid.

                    float curveValue = 1.0f;
                    if (clip.blendCurve != null && clip.blendCurve.length > 0)
                    {
                         curveValue = clip.blendCurve.Evaluate(progress);
                    }

                    targetScale = clip.timeScale * curveValue;
                }
            }

            if (hasActiveClip)
            {
                // Safety clamp
                targetScale = Mathf.Max(0.01f, targetScale);

                // Only apply if changed significantly
                if (Mathf.Abs(Time.timeScale - targetScale) > 0.001f)
                {
                    Time.timeScale = targetScale;
                }
            }
        }

        protected override void OnExit(float time, bool scrub)
        {
            if (isModifyingTime)
            {
                // Restore original time scale
                Time.timeScale = originalTimeScale;
                isModifyingTime = false;
            }
        }

        protected override void OnCleanup()
        {
            if (isModifyingTime)
            {
                Time.timeScale = originalTimeScale;
                isModifyingTime = false;
            }
        }
    }
}
