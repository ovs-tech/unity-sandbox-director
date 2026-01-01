using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.UI.MVVM.Ruler;
using MiniTimeline.UI.MVVM.Track;
using MiniTimeline.Core;
using Unity.Properties;

namespace MiniTimeline.UI.MVVM.Timeline
{
    public class TimelineEditorView : MonoBehaviour
    {
        public VisualElement Root { get; private set; }

        [SerializeField] VisualTreeAsset _uxml;
        [SerializeField] StyleSheet _uss;

        // Track UI assets
        [SerializeField] public VisualTreeAsset TrackUxml;
        [SerializeField] public StyleSheet TrackUss;

        // Clip UI assets
        [SerializeField] public VisualTreeAsset ClipUxml;
        [SerializeField] public StyleSheet ClipUss;

        // Ruler UI assets
        [SerializeField] public VisualTreeAsset RulerUxml;
        [SerializeField] public StyleSheet RulerUss;

        UIDocument _document;

        public IEnumerator InitializeView(TimelineEditorController.ViewModel viewModel)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                _document = gameObject.AddComponent<UIDocument>();
            }
            Root = _document.rootVisualElement;
            Root.Clear();

            if (_uss != null) Root.styleSheets.Add(_uss);
            if (_uxml != null)
            {
                var tree = _uxml.Instantiate();
                Root.Add(tree);
            }
            else
            {
                Debug.LogWarning("UXML not assigned in TimelineEditorView");
            }

            yield return null;
        }

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Slider GetSlider(string name) => Root?.Q<Slider>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);

        public void Bind(TimelineEditorController.ViewModel vm, TimelineRulerModel rulerModel = null)
        {
            // Playback controls
            var playButton = GetButton("play-button");
            if (playButton != null) playButton.clicked += vm.Play;
            var pauseButton = GetButton("pause-button");
            if (pauseButton != null) pauseButton.clicked += vm.Pause;
            var stopButton = GetButton("stop-button");
            if (stopButton != null) stopButton.clicked += vm.Stop;

            // Undo/Redo
            var undoButton = GetButton("undo-button");
            if (undoButton != null)
            {
                undoButton.clicked += vm.Undo;
                undoButton.SetEnabled(vm.CanUndo.Value);
            }

            var redoButton = GetButton("redo-button");
            if (redoButton != null)
            {
                redoButton.clicked += vm.Redo;
                redoButton.SetEnabled(vm.CanRedo.Value);
            }


            vm.OnCommandStacksChanged += (canUndo, canRedo) =>
            {
                redoButton?.SetEnabled(vm.CanRedo.Value);
                undoButton?.SetEnabled(vm.CanUndo.Value);
            };

            // Track/Project management
            var addTrackButton = GetButton("add-track-button");
            if (addTrackButton != null) addTrackButton.clicked += vm.ShowAddTrackForm;
            var bindingManagerButton = GetButton("binding-manager-button");
            if (bindingManagerButton != null) bindingManagerButton.clicked += vm.ShowBindingManager;
            var saveButton = GetButton("save-button");
            if (saveButton != null) saveButton.clicked += vm.ShowSaveProjectForm;
            var loadButton = GetButton("load-button");
            if (loadButton != null) loadButton.clicked += vm.ShowLoadProjectForm;

            // Time slider
            var timeLabel = GetLabel("time-label");
            var timeSlider = GetSlider("time-slider");
            if (timeSlider != null)
            {
                timeSlider.SetBinding(nameof(Slider.value), new DataBinding
                {
                    dataSource = vm.Time,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<float>.Value)),
                    bindingMode = BindingMode.ToTarget
                });
                timeSlider.SetBinding(nameof(Slider.highValue), new DataBinding
                {
                    dataSource = vm.Length,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<float>.Value)),
                    bindingMode = BindingMode.ToTarget
                });
                timeSlider.RegisterValueChangedCallback(evt =>
                {
                    vm.SetTime(evt.newValue);
                });
            }

            // Zoom label
            var zoomLabel = GetLabel("zoom-label");
            if (zoomLabel != null)
            {
                zoomLabel.text = $"{vm.Zoom.Value * 100:F0}%";
            }

            // Zoom slider
            var zoomSlider = GetSlider("zoom-slider");
            if (zoomSlider != null)
            {
                zoomSlider.value = vm.Zoom.Value;
                zoomSlider.RegisterValueChangedCallback(evt =>
                {
                    var newZoom = Mathf.Clamp(evt.newValue, 0.05f, 4f);
                    vm.SetZoom(newZoom);
                    if (rulerModel != null)
                    {
                        rulerModel.SetZoom(newZoom);
                    }

                    if (zoomLabel != null)
                    {
                        zoomLabel.text = $"{newZoom * 100:F0}%";
                    }
                });
            }
        }

        /// <summary>
        /// Initializes the TimelineRulerController and renders it in the ruler container.
        /// </summary>
        public (TimelineRulerController controller, TimelineRulerModel model) InitializeRuler(TimelineEditorController.ViewModel viewModel)
        {
            var rulerElement = GetElement("ruler-container");
            if (rulerElement == null)
            {
                Debug.LogWarning("Cannot find 'ruler-container' element in TimelineEditorView");
                return (null, null);
            }

            try
            {
                var rulerView = new TimelineRulerView(rulerElement, RulerUxml, RulerUss);
                var rulerModel = new TimelineRulerModel();

                float length = viewModel.Director?.Length ?? 0f;
                Debug.Log($"Timeline length for ruler: {length}s");
                int frameRate = (int)Mathf.Round(viewModel.Director?.Project?.frameRate ?? 30f);

                var rulerController = new TimelineRulerController.Builder(rulerView)
                    .WithModel(rulerModel)
                    .WithLength(length)
                    .WithFrameRate(frameRate)
                    .WithDirector(viewModel.Director)
                    .Build();

                Debug.Log("Initialized TimelineRulerController");
                return (rulerController, rulerModel);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize TimelineRulerController: {e.Message}\n{e.StackTrace}");
                return (null, null);
            }
        }

        /// <summary>
        /// Creates and renders a TrackController for the given track.
        /// </summary>
        public TrackController CreateAndRenderTrackController(IMiniTrack track, MiniTimelineDirector director, float pixelsPerSecond, float length, float zoom)
        {
            if (track == null)
            {
                Debug.LogWarning("Cannot create TrackController: Track is null");
                return null;
            }

            try
            {
                // Get the tracks container
                var tracksContainer = GetElement("tracks-container");
                if (tracksContainer == null)
                {
                    Debug.LogWarning("Cannot find 'tracks-container' element in TimelineEditorView");
                    return null;
                }

                // Create a VisualElement for this track and add it to the container
                var trackElement = new VisualElement { name = $"track_{track.Id}" };
                tracksContainer.Add(trackElement);

                // Create TrackView with the VisualElement and UI assets
                var trackView = new TrackView(trackElement, TrackUxml, TrackUss, Root);

                // Create TrackController with builder pattern
                var trackController = new TrackController.Builder(trackView)
                    .WithTrack(track)
                    .WithDirector(director)
                    .WithClipUI(ClipUxml, ClipUss)
                    .Build();

                // Initialize zoom
                trackController.UpdateZoom(zoom, pixelsPerSecond);

                Debug.Log($"Successfully created and rendered TrackController for track: {track.Id}");
                return trackController;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create TrackController for track '{track.Id}': {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Removes the rendered track element from the UI.
        /// </summary>
        public void RemoveTrackElement(string trackId)
        {
            var tracksContainer = GetElement("tracks-container");
            if (tracksContainer == null)
            {
                Debug.LogWarning("Cannot find 'tracks-container' element in TimelineEditorView");
                return;
            }

            var trackElement = tracksContainer.Q<VisualElement>($"track_{trackId}");
            if (trackElement != null)
            {
                tracksContainer.Remove(trackElement);
                Debug.Log($"Removed track element: {trackId}");
            }
        }
    }
}