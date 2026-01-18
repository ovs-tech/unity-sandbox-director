#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.MVVM.Clip;
using Unity.Properties;

namespace Systems.MiniTimeline.UI.MVVM.Track
{
    public class TrackView
    {
        public VisualElement Root { get; private set; }
        public VisualElement PanelRoot { get; private set; }

        public TrackView(VisualElement root, VisualTreeAsset uxml = null, StyleSheet uss = null, VisualElement panelRoot = null)
        {
            Root = root;
            PanelRoot = panelRoot;
            Initialize(uxml, uss);
        }

        void Initialize(VisualTreeAsset uxml, StyleSheet uss)
        {
            if (Root == null) return;

            // Add stylesheet if provided
            if (uss != null)
            {
                Root.styleSheets.Add(uss);
            }

            // Instantiate and add UXML if provided
            if (uxml != null)
            {
                var tree = uxml.Instantiate();
                Root.Add(tree);
            }
        }

        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public Toggle GetToggle(string name) => Root?.Q<Toggle>(name);
        public Switch GetSwitch(string name) => Root?.Q<Switch>(name);
        public Button GetButton(string name) => Root?.Q<Button>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);

        VisualElement _clipsContainer;

        /// <summary>
        /// Renders the track view by binding UI elements and updating displays.
        /// </summary>
        public void Render(TrackController.ViewModel vm, TrackModel model)
        {
            var trackRoot = GetElement("track-root");
            if (trackRoot != null)
            {
                trackRoot.AddToClassList($"track-{vm.TrackType.Value.ToLower()}");
            }

            // State toggles
            var enabled = GetSwitch("track-enabled");
            var mute = GetButton("track-mute");
            var solo = GetButton("track-solo");

            // Display elements
            var title = GetLabel("track-title");
            var bindInfo = GetLabel("track-bind-key");

            // Action buttons
            var menuButton = GetButton("track-menu-button");

            // Wire state toggles
            if (enabled != null)
            {
                Debug.Log("[TrackView] Binding enabled switch: " + vm.Title.Value);
                enabled.SetBinding(nameof(Switch.value), new DataBinding
                {
                    dataSource = vm,
                    dataSourcePath = PropertyPath.FromName(nameof(TrackController.ViewModel.Enabled)),
                    bindingMode = BindingMode.TwoWay
                });
            }
            if (mute != null)
            {
                mute.clicked += vm.Mute;
            }
            if (solo != null)
            {
                solo.clicked += vm.ToggleSolo;
            }

            // Update display labels
            if (title != null)
                title.SetBinding(nameof(Label.text), new DataBinding
                {
                    dataSource = vm.Title,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
                    bindingMode = BindingMode.TwoWay
                });
            title.SetBinding(nameof(Label.tooltip), new DataBinding
            {
                dataSource = vm.Title,
                dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
                bindingMode = BindingMode.ToTarget
            });
            if (bindInfo != null)
                bindInfo.SetBinding(nameof(Label.text), new DataBinding
                {
                    dataSource = vm.BindKey,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
                    bindingMode = BindingMode.TwoWay
                });

            // Wire action buttons
            if (menuButton != null) menuButton.clicked += vm.ShowSettings;

            model.OnClipsChanged += () =>
            {
                vm.RefreshClips();
            };
            model.OnZoomChanged += () => UpdateZoom(model);
        }

        /// <summary>
        /// Initialize ClipControllers for all existing clips in the track.
        /// </summary>
        public void InitializeClipControllers(TrackModel model, Dictionary<string, ClipController> clipControllers, VisualTreeAsset clipUxml, StyleSheet clipUss)
        {
            if (model.Track == null) return;

            // Get clips container from track view (place clips under first lane)
            _clipsContainer = GetElement("lane-1");
            if (_clipsContainer == null)
            {
                Debug.LogWarning("Cannot find 'lane-1' element in TrackView");
                return;
            }

            _clipsContainer.Clear();

            // Create controllers for all current clips
            foreach (var clip in model.Clips)
            {
                CreateAndRegisterClipController(clip.Id, clip, model, _clipsContainer, clipControllers, clipUxml, clipUss);
            }

            Debug.Log($"Initialized {clipControllers.Count} ClipControllers for track");
        }

        /// <summary>
        /// Syncs clip controllers based on the current clip list in the model.
        /// </summary>
        public void SyncClipControllers(TrackModel model, Dictionary<string, ClipController> clipControllers, VisualTreeAsset clipUxml, StyleSheet clipUss)
        {
            if (_clipsContainer == null)
            {
                Debug.LogWarning("Cannot find 'lane-1' element in TrackView");
                return;
            }

            // Get the set of clip IDs currently in the model
            var clipIdsInModel = new HashSet<string>();
            foreach (var clip in model.Clips)
            {
                if (clip != null)
                {
                    clipIdsInModel.Add(clip.Id);

                    // Create controller if it doesn't exist
                    if (!clipControllers.ContainsKey(clip.Id))
                    {
                        CreateAndRegisterClipController(clip.Id, clip, model, _clipsContainer, clipControllers, clipUxml, clipUss);
                    }
                }
            }

            // Remove controllers for clips no longer in the model
            var clipsToRemove = new List<string>();
            foreach (var clipId in clipControllers.Keys)
            {
                if (!clipIdsInModel.Contains(clipId))
                {
                    clipsToRemove.Add(clipId);
                }
            }

            foreach (var clipId in clipsToRemove)
            {
                UnregisterClipController(clipId, clipControllers);
            }
        }

        /// <summary>
        /// Creates and registers a ClipController for a given clip.
        /// </summary>
        private ClipController CreateAndRegisterClipController(string clipId, IMiniClip clip, TrackModel model, VisualElement clipsContainer, Dictionary<string, ClipController> clipControllers, VisualTreeAsset clipUxml, StyleSheet clipUss)
        {
            if (string.IsNullOrEmpty(clipId) || clip == null)
            {
                Debug.LogWarning("Cannot create ClipController: Invalid clipId or clip");
                return null;
            }

            if (clipControllers.ContainsKey(clipId))
            {
                Debug.LogWarning($"ClipController already exists for clip {clipId}");
                return clipControllers[clipId];
            }

            try
            {
                // Create a VisualElement for this clip and add it to the container
                var clipElement = new VisualElement { name = $"clip_{clipId}" };
                clipsContainer.Add(clipElement);

                // Create ClipView with the VisualElement and optional UI assets
                var clipView = new ClipView(clipElement, clipUxml, clipUss, PanelRoot);

                // Create ClipModel and initialize with clip
                var clipModel = new ClipModel();
                clipModel.Initialize(clip, model.Track, model.Director);

                // Create ClipController with builder pattern
                var clipController = new ClipController.Builder(clipView)
                    .WithModel(clipModel)
                    .WithClip(clip)
                    .WithDirector(model.Director)
                    .Build();

                // Register the controller
                clipControllers[clipId] = clipController;

                Debug.Log($"Successfully created and registered ClipController for clip: {clipId}");
                return clipController;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create ClipController for clip '{clipId}': {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Unregisters a ClipController for a given clip ID.
        /// </summary>
        private void UnregisterClipController(string clipId, Dictionary<string, ClipController> clipControllers)
        {
            if (clipControllers.ContainsKey(clipId))
            {
                clipControllers.Remove(clipId);
                Debug.Log($"Unregistered ClipController for clip: {clipId}");
            }
        }

        /// <summary>
        /// Updates the zoom level and timeline width, refreshing clip layouts.
        /// </summary>
        public void UpdateZoom(TrackModel model)
        {
            // Update track width based on timeline width
            if (_clipsContainer != null)
            {
                _clipsContainer.style.width = model.TimelineWidth * model.PixelsPerSecond;
            }
        }
    }
}
#endif