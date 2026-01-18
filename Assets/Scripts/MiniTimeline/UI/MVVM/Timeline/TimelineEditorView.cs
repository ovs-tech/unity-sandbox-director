using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.MiniTimeline.UI.MVVM.Ruler;
using Systems.MiniTimeline.UI.MVVM.Track;
using Systems.MiniTimeline.Core;
using Unity.Properties;

namespace Systems.MiniTimeline.UI.MVVM.Timeline
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

        private VisualElement _tracksContainer;

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

            _tracksContainer = Root?.Q<VisualElement>("tracks-container");
            if (_tracksContainer != null)
            {
                _tracksContainer.Clear();
            }
            else
            {
                Debug.LogWarning("Cannot find 'tracks-container' element in TimelineEditorView during initialization");
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
            if (playButton != null) {
                playButton.clicked += () => {
                    if( vm.IsPlaying.Value ) {
                        vm.Pause();
                        return;
                    }
                    vm.Play();
                };
                var playButtonDataBinding = new DataBinding
                {
                    dataSource = vm.IsPlaying,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<bool>.Value)),
                    bindingMode = BindingMode.ToTarget
                };
                playButtonDataBinding.sourceToUiConverters.AddConverter<bool, string>((ref bool val) => val ? "\uf04c" : "\uf04b");
                playButton.SetBinding(nameof(Button.text), playButtonDataBinding);
            }
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
            if( timeLabel != null )
            {
                var timeDataBinding = new DataBinding
                {
                    dataSource = vm.Time,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<float>.Value)),
                    bindingMode = BindingMode.ToTarget
                };
                timeDataBinding.sourceToUiConverters.AddConverter<float, string>((ref float val) =>
                {
                    TimeSpan time = TimeSpan.FromSeconds(val);
                    return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
                });
                timeLabel.SetBinding(nameof(Label.text), timeDataBinding);
            }
            var durationLabel = GetLabel("duration-label");
            if( durationLabel != null )
            {
                var durationDataBinding = new DataBinding
                {
                    dataSource = vm.Length,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<float>.Value)),
                    bindingMode = BindingMode.ToTarget
                };
                durationDataBinding.sourceToUiConverters.AddConverter<float, string>((ref float val) =>
                {
                    TimeSpan time = TimeSpan.FromSeconds(val);
                    return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
                });
                durationLabel.SetBinding(nameof(Label.text), durationDataBinding);
            }
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
                var zoomDataBinding = new DataBinding
                {
                    dataSource = vm,
                    dataSourcePath = new PropertyPath(nameof(TimelineEditorController.ViewModel.Zoom)),
                    bindingMode = BindingMode.ToTarget
                };
                zoomDataBinding.sourceToUiConverters.AddConverter<float, string>((ref float val) =>
                {
                    return $"{val * 100:F0}%";
                });
                zoomLabel.SetBinding(nameof(Label.text), zoomDataBinding);
            }

            // Zoom slider
            var zoomSlider = GetSlider("zoom-slider");
            if (zoomSlider != null)
            {
                var zoomDataBinding = new DataBinding
                {
                    dataSource = vm,
                    dataSourcePath = new PropertyPath(nameof(TimelineEditorController.ViewModel.Zoom)),
                    bindingMode = BindingMode.TwoWay
                };
                zoomDataBinding.uiToSourceConverters.AddConverter<float, float>((ref float val) =>
                {
                    return Mathf.Clamp(val, 0.05f, 4f);
                });
                zoomSlider.SetBinding(nameof(Slider.value), zoomDataBinding);
                zoomSlider.RegisterValueChangedCallback(evt =>
                {
                    var newValue = Mathf.Clamp(evt.newValue, 0.05f, 4f);
                    rulerModel.SetZoom(newValue);
                });
            }

            if( _tracksContainer == null )
            {
                var trackContainerDataBinding = new DataBinding
                {
                    dataSource = vm,
                    dataSourcePath = new PropertyPath(nameof(TimelineEditorController.ViewModel.PixelsPerSecond)),
                    bindingMode = BindingMode.ToTarget
                };
                trackContainerDataBinding.sourceToUiConverters.AddConverter<float, float>((ref float pixelsPerSecond) =>
                {
                    return vm.Length.Value * pixelsPerSecond;
                });
                _tracksContainer.SetBinding(nameof(VisualElement.style.width), trackContainerDataBinding);
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
                // Create a VisualElement for this track and add it to the container
                var trackElement = new VisualElement { name = $"track_{track.Id}" };
                _tracksContainer.Add(trackElement);

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
            var tracksContainer = _tracksContainer ?? GetElement("tracks-container");
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