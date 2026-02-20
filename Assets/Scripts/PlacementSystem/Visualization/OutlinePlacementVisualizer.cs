using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Visualization
{
    /// <summary>
    /// Advanced visualizer with outline effects and additional visual feedback.
    /// Can be used as an alternative to StandardPlacementVisualizer.
    /// </summary>
    public class OutlinePlacementVisualizer : MonoBehaviour, IPlacementVisualizer
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

        // Runtime state
        private GameObject _ghostObject;
        private Material _outlineMaterial;
        private bool _isValid = true;
        private float _pulseTime = 0f;

        public void Initialize(GameObject ghostObject)
        {
            _ghostObject = ghostObject;

            if (_ghostObject == null)
                return;

            // Create outline material
            // Note: This requires a custom outline shader or post-processing effect
            // For production use, you would implement a proper outline shader
            _outlineMaterial = new Material(Shader.Find("Outline"));
            
            // Apply initial state
            UpdateVisual(true);
        }

        public void UpdateVisual(bool isValid)
        {
            _isValid = isValid;

            if (_ghostObject == null)
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
            if (_outlineMaterial != null && _outlineMaterial.HasProperty("_Color"))
            {
                _outlineMaterial.SetColor("_Color", outlineColor);
            }
        }

        public void Cleanup()
        {
            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
                _outlineMaterial = null;
            }

            _ghostObject = null;
        }

        private void Update()
        {
            // Continuously update visual for pulse effect
            if (_enablePulse && _ghostObject != null)
            {
                UpdateVisual(_isValid);
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
