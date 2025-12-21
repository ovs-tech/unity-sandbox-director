#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.UI.MVVM.Ruler;
using UnityEditor.UIElements;
using Unity.Properties;
using Systems.Inventory;

namespace MiniTimeline.UI.MVVM.Timeline {
    public class TimelineEditorView : MonoBehaviour {
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

        public IEnumerator InitializeView(TimelineEditorController.ViewModel viewModel) {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null) {
                _document = gameObject.AddComponent<UIDocument>();
            }
            Root = _document.rootVisualElement;
            Root.Clear();

            if (_uss != null) Root.styleSheets.Add(_uss);
            if (_uxml != null) {
                var tree = _uxml.Instantiate();
                Root.Add(tree);
            } else {
                Debug.LogWarning("UXML not assigned in TimelineEditorView");
            }

            yield return null;
        }

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Slider GetSlider(string name) => Root?.Q<Slider>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);

        public void Bind(TimelineEditorController.ViewModel vm, TimelineEditorModel model, TimelineRulerModel rulerModel = null) {
            // Playback controls
            var playButton = GetButton("play-button");
            if (playButton != null) playButton.clicked += vm.Play;
            var pauseButton = GetButton("pause-button");
            if (pauseButton != null) pauseButton.clicked += vm.Pause;
            var stopButton = GetButton("stop-button");
            if (stopButton != null) stopButton.clicked += vm.Stop;

            // Undo/Redo
            var undoButton = GetButton("undo-button");
            if (undoButton != null) undoButton.clicked += vm.Undo;
            var redoButton = GetButton("redo-button");
            if (redoButton != null) redoButton.clicked += vm.Redo;

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
            if (timeSlider != null) {
                timeSlider.SetBinding(nameof(Slider.value), new DataBinding {
                    dataSource = model,
                    dataSourcePath = new PropertyPath(nameof(TimelineEditorModel.Time)),
                    bindingMode = BindingMode.TwoWay
                });
            }

            // Zoom label
            var zoomLabel = GetLabel("zoom-label");
            if (zoomLabel != null) {
                zoomLabel.text = $"{model.Zoom * 100:F0}%";
                model.OnCommandExecuted += () => {
                    zoomLabel.text = $"{model.Zoom * 100:F0}%";
                };
            }

            // Zoom slider
            var zoomSlider = GetSlider("zoom-slider");
            if (zoomSlider != null) {
                zoomSlider.value = model.Zoom;
                zoomSlider.RegisterValueChangedCallback(evt => {
                    var newZoom = Mathf.Clamp(evt.newValue, 0.05f, 4f);
                    vm.SetZoom(newZoom);
                    if (rulerModel != null) {
                        rulerModel.SetZoom(newZoom);
                    }
                    if (zoomLabel != null) {
                        zoomLabel.text = $"{newZoom * 100:F0}%";
                    }
                });
            }
        }
    }
}
#endif