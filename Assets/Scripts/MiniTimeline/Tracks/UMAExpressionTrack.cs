using UnityEngine;
using MiniTimeline.Core;
using UMA.CharacterSystem;
using UMA.PoseTools;

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
            var values = expressionPlayer.Values;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = 0f;
            }

            var activeClips = GetActiveClips(time);
            foreach (var clip in activeClips)
            {
                var clipTime = time - clip.Start;
                var normalizedTime = clipTime / clip.Duration;
                var blendWeight = clip.blendCurve.Evaluate(normalizedTime);
                
                // Interpolate between from and to values using the blend weight
                var expressionValue = Mathf.Lerp(clip.from, clip.to, blendWeight);
                // Find the expression index using the PoseNames array
                SetExpressionValue(clip.expression, expressionValue, values);
            }
            
            // Apply the updated values back to the expression player
            expressionPlayer.Values = values;
        }

        protected override void OnCleanup()
        {
            if (expressionPlayer != null)
            {
                var values = expressionPlayer.Values;
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = 0f;
                }
                expressionPlayer.Values = values;
            }
        }

        /// <summary>
        /// Helper method to set expression value by name
        /// </summary>
        private void SetExpressionValue(string expressionName, float value, float[] values)
        {
            if (string.IsNullOrEmpty(expressionName) || values == null) return;

            // Find the index of the expression in the PoseNames array
            var poseNames = ExpressionPlayer.PoseNames;
            for (int i = 0; i < poseNames.Length && i < values.Length; i++)
            {
                if (poseNames[i] == expressionName)
                {
                    values[i] = value;
                    return;
                }
            }

            // If not found in standard poses, try to set using reflection as fallback
            var field = typeof(UMAExpressionPlayer).GetField(expressionName);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(expressionPlayer, value);
            }
            else
            {
                Debug.LogWarning($"[UMAExpressionTrack] Expression '{expressionName}' not found in UMAExpressionPlayer");
            }
        }
    }
}