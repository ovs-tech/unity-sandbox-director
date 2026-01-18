using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Track for controlling Animator parameters over time
    /// </summary>
    [Serializable]
    public class AnimatorTrack : MiniTrackBase<AnimatorClip>
    {

        [Header("Animator Settings")]
        public string animatorBinding = ""; // Path to GameObject with Animator
        public bool useGameObjectName = true; // If true, search by GameObject name instead of path

        [Header("Blending")]
        public bool supportBlending = true;
        public float defaultBlendWeight = 1f;

        private Animator targetAnimator;
        private Dictionary<string, object> lastParameterValues = new Dictionary<string, object>();

        #region Track Lifecycle

        protected override void OnPrepare()
        {

            if (targetObject is Animator animator)
            {
                targetAnimator = animator;
            }
            else if (targetObject is GameObject go)
            {
                targetAnimator = go.GetComponent<Animator>();
            }
            else
            {
                return;
            }

            if (targetAnimator == null)
            {
                return;
            }
        }

        protected override void OnEvaluate(float time, bool enableScrubbingSound)
        {
            if (targetAnimator == null)
            {
                OnPrepare();
                if (targetAnimator == null)
                {
                    return;
                }
            }

            var activeClips = GetActiveClips(time);

            if (activeClips.Count == 0)
            {
                return;
            }

            if (activeClips.Count == 1)
            {
                // Single clip - direct application
                ApplySingleClip(activeClips[0], time, targetAnimator);
            }
            else if (supportBlending)
            {
                // Multiple clips - blend them
                ApplyBlendedClips(activeClips, time, targetAnimator);
            }
            else
            {
                // No blending - use the last (topmost) clip
                var lastClip = activeClips.OrderBy(c => c.Start).Last();
                ApplySingleClip(lastClip, time, targetAnimator);
            }
        }

        public override void OnProjectClosed()
        {
            targetAnimator = null;
            lastParameterValues.Clear();
            base.OnProjectClosed();
        }

        #endregion

        #region Animator Specific Methods

        /// <summary>
        /// Apply a single clip to the animator
        /// </summary>
        private void ApplySingleClip(AnimatorClip clip, float time, Animator animator)
        {
            var parameterValues = clip.GetAllParameterValuesAtTime(time);

            float weight = clip.GetFadeWeight(time) * defaultBlendWeight;
            ApplyParameterValues(animator, parameterValues, weight, clip.blendMode);
        }

        /// <summary>
        /// Apply multiple blended clips to the animator
        /// </summary>
        private void ApplyBlendedClips(List<AnimatorClip> activeClips, float time, Animator animator)
        {
            // Collect all parameter values with weights
            var blendedParameters = new Dictionary<string, BlendedParameter>();

            foreach (var clip in activeClips)
            {
                var parameterValues = clip.GetAllParameterValuesAtTime(time);
                float weight = clip.GetFadeWeight(time) * defaultBlendWeight;

                foreach (var kvp in parameterValues)
                {
                    string paramName = kvp.Key;
                    object value = kvp.Value;

                    if (!blendedParameters.ContainsKey(paramName))
                    {
                        blendedParameters[paramName] = new BlendedParameter();
                    }

                    blendedParameters[paramName].AddValue(value, weight, clip.blendMode);
                }
            }

            // Apply blended values
            var finalValues = new Dictionary<string, object>();
            foreach (var kvp in blendedParameters)
            {
                finalValues[kvp.Key] = kvp.Value.GetBlendedValue();
            }

            ApplyParameterValues(animator, finalValues, 1f, AnimatorBlendMode.Override);
        }

        /// <summary>
        /// Apply parameter values to the animator
        /// </summary>
        private void ApplyParameterValues(Animator animator, Dictionary<string, object> parameterValues,
            float weight, AnimatorBlendMode blendMode)
        {
            foreach (var kvp in parameterValues)
            {
                string paramName = kvp.Key;
                object value = kvp.Value;

                if (!HasParameter(animator, paramName))
                {
                    continue;
                }

                ApplyParameterValue(animator, paramName, value, weight, blendMode);
                lastParameterValues[paramName] = value;
            }
        }

        /// <summary>
        /// Apply a single parameter value to the animator
        /// </summary>
        private void ApplyParameterValue(Animator animator, string paramName, object value,
            float weight, AnimatorBlendMode blendMode)
        {
            try
            {
                switch (value)
                {
                    case float floatValue:
                        ApplyFloatParameter(animator, paramName, floatValue, weight, blendMode);
                        break;

                    case int intValue:
                        ApplyIntParameter(animator, paramName, intValue, weight, blendMode);
                        break;

                    case bool boolValue:
                        animator.SetBool(paramName, boolValue);
                        break;

                    default:
                        // Handle trigger parameters
                        if (value is bool triggerValue && triggerValue)
                        {
                            animator.SetTrigger(paramName);
                        }
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Apply float parameter with blending
        /// </summary>
        private void ApplyFloatParameter(Animator animator, string paramName, float value,
            float weight, AnimatorBlendMode blendMode)
        {
            switch (blendMode)
            {
                case AnimatorBlendMode.Override:
                    if (weight >= 1f)
                    {
                        animator.SetFloat(paramName, value);
                    }
                    else
                    {
                        float currentValue = animator.GetFloat(paramName);
                        animator.SetFloat(paramName, Mathf.Lerp(currentValue, value, weight));
                    }
                    break;

                case AnimatorBlendMode.Additive:
                    animator.SetFloat(paramName, animator.GetFloat(paramName) + (value * weight));
                    break;

                case AnimatorBlendMode.Multiply:
                    animator.SetFloat(paramName, animator.GetFloat(paramName) * Mathf.Lerp(1f, value, weight));
                    break;
            }
        }

        /// <summary>
        /// Apply int parameter with blending
        /// </summary>
        private void ApplyIntParameter(Animator animator, string paramName, int value,
            float weight, AnimatorBlendMode blendMode)
        {
            switch (blendMode)
            {
                case AnimatorBlendMode.Override:
                    if (weight >= 1f)
                    {
                        animator.SetInteger(paramName, value);
                    }
                    else
                    {
                        int currentValue = animator.GetInteger(paramName);
                        animator.SetInteger(paramName, Mathf.RoundToInt(Mathf.Lerp(currentValue, value, weight)));
                    }
                    break;

                case AnimatorBlendMode.Additive:
                    animator.SetInteger(paramName, animator.GetInteger(paramName) + Mathf.RoundToInt(value * weight));
                    break;

                case AnimatorBlendMode.Multiply:
                    float currentFloat = animator.GetInteger(paramName);
                    float newValue = currentFloat * Mathf.Lerp(1f, value, weight);
                    animator.SetInteger(paramName, Mathf.RoundToInt(newValue));
                    break;
            }
        }

        /// <summary>
        /// Check if animator has a parameter
        /// </summary>
        private bool HasParameter(Animator animator, string paramName)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Clear cached references (call when director changes)
        /// </summary>
        public void ClearCache()
        {
            targetAnimator = null;
            lastParameterValues.Clear();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Create a new animator clip
        /// </summary>
        public AnimatorClip CreateClip(string clipId, float start, float duration)
        {
            var clip = new AnimatorClip
            {
                Id = clipId,
                Start = start,
                Duration = duration
            };

            clips.Add(clip);
            return clip;
        }

        /// <summary>
        /// Get all animator clips
        /// </summary>
        public List<AnimatorClip> GetAnimatorClips()
        {
            return new List<AnimatorClip>(clips);
        }

        #endregion
    }

    /// <summary>
    /// Helper class for blending multiple parameter values
    /// </summary>
    internal class BlendedParameter
    {
        private List<ValueWeight> values = new List<ValueWeight>();

        public void AddValue(object value, float weight, AnimatorBlendMode blendMode)
        {
            values.Add(new ValueWeight { Value = value, Weight = weight, BlendMode = blendMode });
        }

        public object GetBlendedValue()
        {
            if (values.Count == 0) return null;
            if (values.Count == 1) return values[0].Value;

            // For now, we'll use a simple weighted average for float/int
            // and last-wins for bool/trigger
            var firstValue = values[0].Value;

            switch (firstValue)
            {
                case float _:
                    return BlendFloats();
                case int _:
                    return BlendInts();
                case bool _:
                    return values.Last().Value; // Last bool wins
                default:
                    return values.Last().Value; // Last value wins for other types
            }
        }

        private float BlendFloats()
        {
            float totalWeight = 0f;
            float blendedValue = 0f;

            foreach (var vw in values)
            {
                if (vw.Value is float floatVal)
                {
                    switch (vw.BlendMode)
                    {
                        case AnimatorBlendMode.Override:
                            blendedValue += floatVal * vw.Weight;
                            totalWeight += vw.Weight;
                            break;
                        case AnimatorBlendMode.Additive:
                            blendedValue += floatVal * vw.Weight;
                            break;
                        case AnimatorBlendMode.Multiply:
                            // For multiply, we'll apply it as a modifier
                            blendedValue *= Mathf.Lerp(1f, floatVal, vw.Weight);
                            break;
                    }
                }
            }

            return totalWeight > 0f ? blendedValue / totalWeight : blendedValue;
        }

        private int BlendInts()
        {
            float totalWeight = 0f;
            float blendedValue = 0f;

            foreach (var vw in values)
            {
                if (vw.Value is int intVal)
                {
                    blendedValue += intVal * vw.Weight;
                    totalWeight += vw.Weight;
                }
            }

            return totalWeight > 0f ? Mathf.RoundToInt(blendedValue / totalWeight) : 0;
        }

        private struct ValueWeight
        {
            public object Value;
            public float Weight;
            public AnimatorBlendMode BlendMode;
        }
    }
}