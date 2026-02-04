using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Systems.MiniTimeline.Core;

namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Manages preview mode for the Scene Sandbox Builder system
    /// </summary>
    public class PreviewController : MonoBehaviour
    {
        [Header("Preview Settings")]
        [SerializeField] private bool _autoPreview = false;
        [SerializeField] private float _previewDuration = 10f;

        [Header("Events")]
        [SerializeField] private BoolEvent _onPreviewStateChanged = new BoolEvent();

        // Dependencies
        private MiniTimelineDirector _timelineDirector;
        private PlacementSystem _placementSystem;

        // Runtime state
        private List<GameObject> _previewObjects = new List<GameObject>();

        // Events
        public event Action<bool> OnPreviewStateChanged;

        // Properties
        public bool IsInPreviewMode => _previewObjects?.Count > 0;
        public bool AutoPreview => _autoPreview;
        public float PreviewDuration => _previewDuration;
        public BoolEvent OnPreviewStateChangedEvent => _onPreviewStateChanged;

        /// <summary>
        /// Initialize the preview controller with required dependencies
        /// </summary>
        public void Initialize(MiniTimelineDirector timelineDirector, PlacementSystem placementSystem)
        {
            _timelineDirector = timelineDirector;
            _placementSystem = placementSystem;
        }

        /// <summary>
        /// Start preview mode
        /// </summary>
        public void StartPreview()
        {
            if (_timelineDirector == null || IsInPreviewMode) return;

            StopPreview(); // Ensure clean state

            // Create preview objects (snapshots of current scene)
            CreatePreviewObjects();

            // Set up timeline director with current scene
            SetupTimelineForPreview();

            // Start playback
            _timelineDirector.Play();

            _onPreviewStateChanged?.Invoke(true);
            OnPreviewStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Stop preview mode
        /// </summary>
        public void StopPreview()
        {
            if (_timelineDirector != null)
            {
                _timelineDirector.Stop();
            }

            // Clean up preview objects
            CleanupPreviewObjects();

            _onPreviewStateChanged?.Invoke(false);
            OnPreviewStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Toggle preview mode on/off
        /// </summary>
        public void TogglePreview()
        {
            if (IsInPreviewMode)
            {
                StopPreview();
            }
            else
            {
                StartPreview();
            }
        }

        #region Private Methods

        private void CreatePreviewObjects()
        {
            _previewObjects.Clear();

            if (_placementSystem == null) return;

            // Get all placed objects from PlacementSystem
            var placedObjects = _placementSystem.PlacedObjects;

            foreach (var original in placedObjects.Values)
            {
                if (original == null) continue;

                var preview = Instantiate(original);

                // Disable draggable component during preview
                var draggable = preview.GetComponent<TransformableItem>();
                if (draggable != null)
                {
                    draggable.enabled = false;
                }

                _previewObjects.Add(preview);
            }
        }

        private void CleanupPreviewObjects()
        {
            foreach (var previewObj in _previewObjects)
            {
                if (previewObj != null)
                {
                    Destroy(previewObj);
                }
            }
            _previewObjects.Clear();
        }

        private void SetupTimelineForPreview()
        {
            if (_timelineDirector?.Project == null) return;

            // TODO: Set up timeline tracks for preview objects
            // This would integrate with the existing MiniTimeline system
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Set auto-preview enabled/disabled
        /// </summary>
        public void SetAutoPreview(bool enabled)
        {
            _autoPreview = enabled;
        }

        /// <summary>
        /// Set preview duration
        /// </summary>
        public void SetPreviewDuration(float duration)
        {
            _previewDuration = Mathf.Max(0f, duration);
        }

        #endregion
    }
}
