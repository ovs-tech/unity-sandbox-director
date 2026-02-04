using System;
using UnityEngine;
using Systems.MiniTimeline.Core;
using UMA.PoseTools;

namespace Systems.MiniTimeline.Tracks
{
    [Serializable]
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

            // Reset all expression weights (use reflection to avoid hard UMA dependency)
            var valuesObj = expressionPlayer.GetType().GetProperty("Values")?.GetValue(expressionPlayer)
                ?? expressionPlayer.GetType().GetField("Values")?.GetValue(expressionPlayer);
            float[] values = valuesObj as float[] ?? new float[0];
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
            
                // Apply the updated values back to the expression player (property or field)
            var prop = expressionPlayer.GetType().GetProperty("Values");
            var field = expressionPlayer.GetType().GetField("Values");
            if (prop != null) prop.SetValue(expressionPlayer, values);
            else if (field != null) field.SetValue(expressionPlayer, values);
        }

        protected override void OnCleanup()
        {
            if (expressionPlayer != null)
            {
                var valuesObj2 = expressionPlayer.GetType().GetProperty("Values")?.GetValue(expressionPlayer)
                    ?? expressionPlayer.GetType().GetField("Values")?.GetValue(expressionPlayer);
                float[] values2 = valuesObj2 as float[] ?? new float[0];
                for (int i = 0; i < values2.Length; i++)
                {
                    values2[i] = 0f;
                }
                var prop2 = expressionPlayer.GetType().GetProperty("Values");
                var field2 = expressionPlayer.GetType().GetField("Values");
                if (prop2 != null) prop2.SetValue(expressionPlayer, values2);
                else if (field2 != null) field2.SetValue(expressionPlayer, values2);
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
            var field = expressionPlayer.GetType().GetField(expressionName);
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