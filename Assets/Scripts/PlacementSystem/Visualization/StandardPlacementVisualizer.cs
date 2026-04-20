using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Visualization
{
    /// <summary>
    /// Standard visualizer that changes material colors to indicate placement validity.
    /// Green = valid placement, Red = invalid placement.
    /// Note: As a ScriptableObject, it maintains temporary state for the current ghost.
    /// Assumes only one placement operation occurs at a time.
    /// </summary>
    [CreateAssetMenu(fileName = "StandardPlacementVisualizer", menuName = "Placement System/Visualizers/Standard Visualizer")]
    public class StandardPlacementVisualizer : BasePlacementVisualizer
    {
        [Header("Visual Feedback Colors")]
        [SerializeField, Tooltip("Color when placement is valid")]
        private Color _validColor = new Color(0f, 1f, 0f, 0.5f);

        [SerializeField, Tooltip("Color when placement is invalid")]
        private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Material Settings")]
        [SerializeField, Tooltip("Shader to use for ghost material (should support transparency)")]
        private Shader _ghostShader;

        [SerializeField, Tooltip("Transparency level for ghost objects")]
        [Range(0f, 1f)]
        private float _transparency = 0.5f;

        // Runtime state
        private GameObject _ghostPreview;
        private List<Renderer> _ghostRenderers = new List<Renderer>();
        private List<Material> _originalMaterials = new List<Material>();
        private List<Material> _ghostMaterials = new List<Material>();

        private void OnEnable()
        {
            // Use default shader if none specified
            if (_ghostShader == null)
            {
                _ghostShader = Shader.Find("Standard");
            }
        }

        public override void Initialize(GameObject ghostObject)
        {
            if (ghostObject == null) return;
            
            if (_ghostPreview != null || _ghostMaterials.Count > 0)
                Cleanup();

            _ghostPreview = ghostObject;

            if (_ghostPreview == null)
                return;

            // Get all renderers in the ghost object
            _ghostRenderers.Clear();
            _ghostRenderers.AddRange(_ghostPreview.GetComponentsInChildren<Renderer>());

            // Store original materials and create ghost materials
            _originalMaterials.Clear();
            _ghostMaterials.Clear();

            foreach (var renderer in _ghostRenderers)
            {
                foreach (var originalMaterial in renderer.sharedMaterials)
                {
                    _originalMaterials.Add(originalMaterial);

                    // Create ghost material
                    Material ghostMaterial = new Material(_ghostShader);
                    
                    // Try to preserve texture from original material
                    if (originalMaterial != null && originalMaterial.HasProperty("_MainTex"))
                    {
                        ghostMaterial.mainTexture = originalMaterial.mainTexture;
                    }

                    // Set transparency
                    if (ghostMaterial.HasProperty("_Color"))
                    {
                        Color color = _validColor;
                        color.a = _transparency;
                        ghostMaterial.color = color;
                    }

                    // Enable transparency rendering
                    SetMaterialTransparent(ghostMaterial);

                    _ghostMaterials.Add(ghostMaterial);
                }

                // Apply ghost materials
                renderer.sharedMaterials = _ghostMaterials.ToArray();
            }
        }

        public override void UpdateVisual(bool isValid)
        {
            if (_ghostPreview == null || _ghostMaterials.Count == 0)
                return;

            // Update material colors based on validity
            Color targetColor = isValid ? _validColor : _invalidColor;
            targetColor.a = _transparency;

            foreach (var material in _ghostMaterials)
            {
                if (material != null && material.HasProperty("_Color"))
                {
                    material.color = targetColor;
                }
            }
        }

        public override void Cleanup()
        {
            // Destroy ghost materials to prevent memory leaks
            foreach (var material in _ghostMaterials)
            {
                if (material != null)
                {
                    if (Application.isPlaying)
                        Destroy(material);
                    else
                        DestroyImmediate(material);
                }
            }

            _ghostMaterials.Clear();
            _ghostRenderers.Clear();
            _originalMaterials.Clear();
            _ghostPreview = null;
        }

        /// <summary>
        /// Sets up a material for transparent rendering.
        /// </summary>
        private void SetMaterialTransparent(Material material)
        {
            // Set rendering mode to transparent
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000; // Transparent queue
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
