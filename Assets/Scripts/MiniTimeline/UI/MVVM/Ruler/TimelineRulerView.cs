using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Systems.MiniTimeline.UI.MVVM.Ruler
{
    public class TimelineRulerView
    {
        public VisualElement Root { get; private set; }
        private VisualElement _markersContainer;
        private VisualElement _playheadElement;
        private VisualElement _rulerElement;
        private VisualElement _playheadAreaElement;
        private VisualElement _snapGuideElement;
        private bool _isPlayheadDragging;
        private bool _geometryCallbackRegistered;

        public TimelineRulerView(VisualElement root, VisualTreeAsset uxml = null, StyleSheet uss = null)
        {
            Root = root;
            Initialize(uxml, uss);
        }

        void Initialize(VisualTreeAsset uxml, StyleSheet uss)
        {
            if (Root == null) return;
            if (uss != null) Root.styleSheets.Add(uss);
            if (uxml != null) Root.Add(uxml.Instantiate());
        }

        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);

        public void Bind(TimelineRulerModel model, TimelineRulerController.ViewModel vm)
        {
            _rulerElement = GetElement("timeline-ruler");
            _playheadElement = GetElement("ruler-current-time");
            _playheadAreaElement = GetElement("ruler-playhead-area");
            _snapGuideElement = GetElement("ruler-snap-guide");
            _markersContainer = _rulerElement;

            if(_rulerElement != null)
            {
                var rulerDataBinding = new DataBinding
                {
                    dataSource = vm,
                    dataSourcePath = new PropertyPath(nameof(TimelineRulerController.ViewModel.PixelsPerSecond)),
                    bindingMode = BindingMode.ToTarget
                };
                rulerDataBinding.sourceToUiConverters.AddConverter<float, float>((ref float pixelsPerSecond) =>
                {
                    return vm.Length.Value * pixelsPerSecond;
                });
                _rulerElement.SetBinding(nameof(VisualElement.style.width), rulerDataBinding);
            }

            var inputTarget = _playheadAreaElement ?? _rulerElement;
            if (inputTarget != null)
            {
                inputTarget.RegisterCallback<MouseDownEvent>(evt => OnRulerMouseDown(evt, model));
                inputTarget.RegisterCallback<MouseMoveEvent>(evt => OnRulerMouseMove(evt, model));
                inputTarget.RegisterCallback<MouseUpEvent>(evt => OnRulerMouseUp(evt, model));
            }

            // Subscribe to model changes for live updates
            model.OnTimeChanged += () => UpdatePlayheadPosition(model);
            model.OnMarkersChanged += () => GenerateMarkerDisplay(model, vm);
            model.OnZoomChanged += () => GenerateMarkerDisplay(model, vm);

            // Register a geometry changed callback once so we update playhead after layout
            if (!_geometryCallbackRegistered && _rulerElement != null)
            {
                _rulerElement.RegisterCallback<GeometryChangedEvent>(evt => UpdatePlayheadPosition(model));
                _geometryCallbackRegistered = true;
            }

            Regenerate(model, vm);
        }

        public void Regenerate(TimelineRulerModel model, TimelineRulerController.ViewModel vm)
        {
            GenerateMarkerDisplay(model, vm);
            UpdatePlayheadPosition(model);
        }

        public void UpdatePlayheadPosition(TimelineRulerModel model)
        {
            if (_playheadElement == null) return;
            float x = model.TimeToPosition(model.CurrentTime);

            // Determine maximum X based on ruler element or model fallback
            float maxX = 0f;
            if (_rulerElement != null)
            {
                maxX = _rulerElement.resolvedStyle.width;
            }
            if (maxX <= 0f)
            {
                maxX = model.Length * model.PixelsPerSecond;
            }

            // Clamp to visible bounds
            x = Mathf.Clamp(x, 0f, maxX);

            // If the ruler is inside a ScrollView, account for its horizontal scroll offset
            float scrollOffsetX = 0f;
            VisualElement search = _rulerElement ?? _playheadElement;
            while (search != null)
            {
                if (search is ScrollView sv)
                {
                    scrollOffsetX = sv.scrollOffset.x;
                    break;
                }
                search = search.parent;
            }

            float leftToApply = x - scrollOffsetX;

            // Ensure absolute positioning
            _playheadElement.style.position = Position.Absolute;

            // Apply left as a pixel Length (UIElements expects typed Length values)
            _playheadElement.style.left = new StyleLength(new Length(leftToApply, LengthUnit.Pixel));

            // Ensure top and height so the playhead is visible within the ruler
            _playheadElement.style.top = 0;
            if (_rulerElement != null)
            {
                var rulerH = _rulerElement.resolvedStyle.height;
                if (rulerH > 0)
                    _playheadElement.style.height = new StyleLength(new Length(rulerH, LengthUnit.Pixel));
                else
                    _playheadElement.style.height = new StyleLength(Length.Percent(100));
            }

            // If the playhead is not a child of the ruler, reparent it so absolute left is relative to the ruler
            var parent = _playheadElement.parent;
            if (_rulerElement != null && parent != _rulerElement)
            {
                _playheadElement.RemoveFromHierarchy();
                _rulerElement.Add(_playheadElement);
            }
        }

        private void OnRulerMouseDown(MouseDownEvent evt, TimelineRulerModel model)
        {
            _isPlayheadDragging = true;
            SetTimeFromMouse(evt.localMousePosition.x, model);
        }

        private void OnRulerMouseMove(MouseMoveEvent evt, TimelineRulerModel model)
        {
            if (!_isPlayheadDragging) return;
            SetTimeFromMouse(evt.localMousePosition.x, model);
        }

        private void OnRulerMouseUp(MouseUpEvent evt, TimelineRulerModel model)
        {
            _isPlayheadDragging = false;
        }

        private void SetTimeFromMouse(float localX, TimelineRulerModel model)
        {
            float time = model.PositionToTime(localX);
            model.SetTime(time);
            UpdatePlayheadPosition(model);
        }

        private void GenerateMarkerDisplay(TimelineRulerModel model, TimelineRulerController.ViewModel vm)
        {
            if (_markersContainer == null) return;

            // Set ruler width to match timeline duration
            if (_rulerElement != null)
            {
                // Zoom-based classes for conditional styling
                _rulerElement.RemoveFromClassList("zoom-low");
                _rulerElement.RemoveFromClassList("zoom-high");
                float zoom = model.Zoom;
                if (zoom < 0.5f)
                {
                    _rulerElement.AddToClassList("zoom-low");
                }
                else if (zoom > 2f)
                {
                    _rulerElement.AddToClassList("zoom-high");
                }
            }

            _markersContainer.Clear();

            float pixelsPerSecond = model.PixelsPerSecond;
            float zoomLevel = model.Zoom;
            float duration = model.Length;

            // Determine marker intervals
            float majorInterval = GetMajorInterval(zoomLevel);
            float minorInterval = GetMinorInterval(zoomLevel);

            // Generate major markers
            for (float time = 0f; time <= duration; time += majorInterval)
            {
                CreateMarker(time, pixelsPerSecond, true);
            }

            // Generate minor markers where they don't overlap with major ones
            if (zoomLevel >= 0.5f)
            {
                for (float time = minorInterval; time <= duration; time += minorInterval)
                {
                    if (!Mathf.Approximately(time % majorInterval, 0f))
                    {
                        CreateMarker(time, pixelsPerSecond, false);
                    }
                }
            }
        }

        private void CreateMarker(float time, float pixelsPerSecond, bool isMajor)
        {
            var marker = new VisualElement();
            marker.AddToClassList("ruler-marker");
            marker.AddToClassList(isMajor ? "major" : "minor");
            marker.style.position = Position.Absolute;
            marker.style.left = time * pixelsPerSecond;

            if (isMajor)
            {
                var label = new Label(FormatTime(time));
                label.AddToClassList("ruler-label");
                marker.Add(label);
            }

            _markersContainer.Add(marker);
        }

        private string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 60f)
            {
                return $"{timeInSeconds:F1}s";
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                float seconds = timeInSeconds % 60f;
                return $"{minutes}:{seconds:F1}";
            }
        }

        private float GetMajorInterval(float zoom)
        {
            if (zoom < 0.2f) return 10f;      // 10 second intervals
            if (zoom < 0.5f) return 5f;       // 5 second intervals
            if (zoom < 1f) return 2f;         // 2 second intervals
            if (zoom < 2f) return 1f;         // 1 second intervals
            return 0.5f;                      // 0.5 second intervals
        }

        private float GetMinorInterval(float zoom)
        {
            if (zoom < 0.2f) return 5f;       // 5 second intervals
            if (zoom < 0.5f) return 1f;       // 1 second intervals  
            if (zoom < 1f) return 0.5f;       // 0.5 second intervals
            if (zoom < 2f) return 0.2f;       // 0.2 second intervals
            return 0.1f;                      // 0.1 second intervals
        }
    }
}