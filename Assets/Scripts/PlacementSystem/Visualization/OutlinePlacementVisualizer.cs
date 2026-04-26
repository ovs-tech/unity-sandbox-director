using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Visualization
{
    /// <summary>
    /// Advanced visualizer with outline effects and additional visual feedback.
    /// Can be used as an alternative to StandardPlacementVisualizer.
    /// </summary>
    [CreateAssetMenu(fileName = "OutlinePlacementVisualizer", menuName = "Placement System/Visualizers/Outline Visualizer")]
    public class OutlinePlacementVisualizer : BasePlacementVisualizer
    {
        [Header("Outline Settings")]
        [SerializeField, Tooltip("Outline width")]
        [Range(0f, 0.1f)]
        private float _outlineWidth = 0.03f;

        [SerializeField, Tooltip("Valid placement outline color")]
        private Color _validOutlineColor = Color.green;

        [SerializeField, Tooltip("Invalid placement outline color")]
        private Color _invalidOutlineColor = Color.red;

        [Header("Pulse Effect")]
        [SerializeField, Tooltip("Enable pulsing effect")]
        private bool _enablePulse = true;

        [SerializeField, Tooltip("Pulse speed")]
        private float _pulseSpeed = 2f;

        [SerializeField, Tooltip("Pulse intensity")]
        [Range(0f, 1f)]
        private float _pulseIntensity = 0.3f;

        [Header("Shader")]
        [SerializeField, Tooltip("Outline shader to use (optional)")]
        private Shader _outlineShader;

        /// <summary>
        /// Public accessor to set or get the outline shader at runtime.
        /// </summary>
        public Shader OutlineShader
        {
            get => _outlineShader;
            set => _outlineShader = value;
        }

        // Runtime state
        private GameObject _ghostPreview;
        private Material _outlineMaterial;
        private bool _isValid = true;
        private float _pulseTime = 0f;

        // Renderers and original materials for restoration
        private Renderer[] _renderers;
        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

        public override void Initialize(GameObject ghostObject)
        {
            if (ghostObject == null)
                return;

            if (_ghostPreview != null || _outlineMaterial != null)
                Cleanup();

            _ghostPreview = ghostObject;

            // Create outline material
            // Note: This requires a custom outline shader or post-processing effect
            // For production use, you would implement a proper outline shader

            // Prefer the serialized/assigned shader, otherwise try to find the default "Outline" shader
            var shader = _outlineShader != null ? _outlineShader : Shader.Find("Shade Graphs/Outline");

            _outlineMaterial = new Material(shader ?? Shader.Find("Standard"));

            // Assign outline material to ghost renderers (append as extra material)
            _renderers = _ghostPreview.GetComponentsInChildren<Renderer>();
            _originalMaterials.Clear();
            foreach (var r in _renderers)
            {
                if (r == null)
                    continue;

                var mats = r.sharedMaterials ?? new Material[0];
                var copy = new Material[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                    copy[i] = mats[i];

                _originalMaterials[r] = copy;

                var newMats = new Material[mats.Length + 1];
                for (int i = 0; i < mats.Length; i++)
                    newMats[i] = mats[i];
                newMats[mats.Length] = _outlineMaterial;
                r.sharedMaterials = newMats;
            }

            // Apply initial state
            UpdateVisual(true);
        }

        public override void UpdateVisual(bool isValid)
        {
            _isValid = isValid;

            if (_ghostPreview == null)
                return;


            // Update outline color based on validity
            Color outlineColor = isValid ? _validOutlineColor : _invalidOutlineColor;

            // Apply pulse effect if enabled
            if (_enablePulse)
            {
                _pulseTime += Time.deltaTime * _pulseSpeed;
                float pulse = Mathf.Sin(_pulseTime) * _pulseIntensity;
                outlineColor = Color.Lerp(outlineColor, Color.white, pulse);
            }

            // Apply to material (this is a simplified example)
            if (_outlineMaterial != null)
            {
                // Try common color properties for different shaders
                if (_outlineMaterial.HasProperty("_Color"))
                {
                    _outlineMaterial.SetColor("_Color", outlineColor);
                }
                else if (_outlineMaterial.HasProperty("_OutlineColor"))
                {
                    _outlineMaterial.SetColor("_OutlineColor", outlineColor);
                }
                else if (_outlineMaterial.HasProperty("_TintColor"))
                {
                    _outlineMaterial.SetColor("_TintColor", outlineColor);
                }
                else if (_outlineMaterial.HasProperty("_BaseColor"))
                {
                    _outlineMaterial.SetColor("_BaseColor", outlineColor);
                }
                else
                {
                    try { _outlineMaterial.SetColor("_Color", outlineColor); } catch { }
                }

            }
        }

        public override void Cleanup()
        {
            // Restore original materials
            if (_renderers != null && _originalMaterials != null)
            {
                foreach (var r in _renderers)
                {
                    if (r == null)
                        continue;

                    if (_originalMaterials.TryGetValue(r, out var mats))
                    {
                        r.sharedMaterials = mats;
                    }
                }
            }

            if (_outlineMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_outlineMaterial);
                else
                    DestroyImmediate(_outlineMaterial);
                _outlineMaterial = null;
            }

            _ghostPreview = null;
        }
    }
}
