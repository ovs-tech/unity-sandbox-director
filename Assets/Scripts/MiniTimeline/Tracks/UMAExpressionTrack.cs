using UnityEngine;
using MiniTimeline.Core;
using UMA.CharacterSystem;

namespace MiniTimeline.Tracks
{
    public class UMAExpressionTrack : MiniTrackBase<UMAExpressionClip>
    {
        private UMAExpressionPlayer expressionPlayer;

        protected override void OnPrepare()
        {
            if (targetObject is GameObject go)
            {
                expressionPlayer = go.GetComponent<UMAExpressionPlayer>();
            }

            if (expressionPlayer == null)
            {
                Debug.LogError("[UMAExpressionTrack] UMAExpressionPlayer component not found on target object.");
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            if (expressionPlayer == null) return;

            // Reset all expression weights
            foreach (var expression in expressionPlayer.Values)
            {
                expression.SetValue(0);
            }

            var activeClips = GetActiveClips(time);
            foreach (var clip in activeClips)
            {
                var clipTime = time - clip.Start;
                var blendWeight = clip.blendCurve.Evaluate(clipTime / clip.Duration);
                expressionPlayer.SetValue(clip.expression, blendWeight);
            }
        }

        protected override void OnCleanup()
        {
            if (expressionPlayer != null)
            {
                foreach (var expression in expressionPlayer.Values)
                {
                    expression.SetValue(0);
                }
            }
        }
    }
}