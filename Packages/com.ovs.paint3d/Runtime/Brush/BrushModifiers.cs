using System;
using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Optional modifiers that vary a brush per dab: pen pressure, random jitter, stroke spacing and gradual (flow based) painting.
    /// </summary>
    [Serializable]
    public class BrushModifiers
    {
        [Header("Pressure")]
        [Tooltip("Scale the brush radius by pen / touch pressure.")]
        [SerializeField] private bool _pressureAffectsRadius;

        [Tooltip("Scale the brush opacity by pen / touch pressure.")]
        [SerializeField] private bool _pressureAffectsOpacity = true;

        [Tooltip("Maps raw pressure (0..1) to the applied pressure factor (0..1).")]
        [SerializeField] private AnimationCurve _pressureCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Jitter")]
        [Tooltip("Random radius variation per dab, as a fraction of the radius.")]
        [SerializeField, Range(0f, 1f)] private float _radiusJitter;

        [Tooltip("Random opacity variation per dab, as a fraction of the opacity.")]
        [SerializeField, Range(0f, 1f)] private float _opacityJitter;

        [Tooltip("Random hue variation per dab (0 = none, 1 = full color wheel).")]
        [SerializeField, Range(0f, 1f)] private float _hueJitter;

        [Header("Stroke")]
        [Tooltip("Distance between dabs along a stroke, as a fraction of the brush radius.")]
        [SerializeField, Range(0.05f, 4f)] private float _spacing = 0.25f;

        [Header("Gradual Painting")]
        [Tooltip("When enabled, paint accumulates over time while the brush is held instead of per dab.")]
        [SerializeField] private bool _gradual;

        [Tooltip("How fast opacity accumulates per second when gradual painting is enabled.")]
        [SerializeField, Min(0.01f)] private float _flowRate = 2f;

        public bool PressureAffectsRadius { get => _pressureAffectsRadius; set => _pressureAffectsRadius = value; }
        public bool PressureAffectsOpacity { get => _pressureAffectsOpacity; set => _pressureAffectsOpacity = value; }
        public AnimationCurve PressureCurve { get => _pressureCurve; set => _pressureCurve = value; }
        public float RadiusJitter { get => _radiusJitter; set => _radiusJitter = Mathf.Clamp01(value); }
        public float OpacityJitter { get => _opacityJitter; set => _opacityJitter = Mathf.Clamp01(value); }
        public float HueJitter { get => _hueJitter; set => _hueJitter = Mathf.Clamp01(value); }
        public float Spacing { get => _spacing; set => _spacing = Mathf.Max(0.05f, value); }
        public bool Gradual { get => _gradual; set => _gradual = value; }
        public float FlowRate { get => _flowRate; set => _flowRate = Mathf.Max(0.01f, value); }

        /// <summary>Applies the pressure curve to a raw pressure value.</summary>
        public float EvaluatePressure(float pressure)
        {
            pressure = Mathf.Clamp01(pressure);
            return _pressureCurve != null && _pressureCurve.length > 0
                ? Mathf.Clamp01(_pressureCurve.Evaluate(pressure))
                : pressure;
        }

        public BrushModifiers Clone()
        {
            var clone = (BrushModifiers)MemberwiseClone();
            clone._pressureCurve = _pressureCurve != null ? new AnimationCurve(_pressureCurve.keys) : null;
            return clone;
        }
    }
}
