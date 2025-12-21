#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Ruler
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

        public void Bind(TimelineRulerModel model)
        {
            _rulerElement = GetElement("timeline-ruler");
            _playheadElement = GetElement("ruler-current-time");
            _playheadAreaElement = GetElement("ruler-playhead-area");
            _snapGuideElement = GetElement("ruler-snap-guide");
            _markersContainer = _rulerElement;

            var inputTarget = _playheadAreaElement ?? _rulerElement;
            if (inputTarget != null)
            {
                inputTarget.RegisterCallback<MouseDownEvent>(evt => OnRulerMouseDown(evt, model));
                inputTarget.RegisterCallback<MouseMoveEvent>(evt => OnRulerMouseMove(evt, model));
                inputTarget.RegisterCallback<MouseUpEvent>(evt => OnRulerMouseUp(evt, model));
            }

            // Subscribe to model changes for live updates
            model.OnTimeChanged += () => UpdatePlayheadPosition(model);
            model.OnMarkersChanged += () => GenerateMarkerDisplay(model);
            model.OnZoomChanged += () => GenerateMarkerDisplay(model);

            Regenerate(model);
        }

        public void Regenerate(TimelineRulerModel model)
        {
            GenerateMarkerDisplay(model);
            UpdatePlayheadPosition(model);
        }

        public void UpdatePlayheadPosition(TimelineRulerModel model)
        {
            if (_playheadElement == null) return;
            float x = model.TimeToPosition(model.CurrentTime);
            _playheadElement.style.left = x;
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

        private void GenerateMarkerDisplay(TimelineRulerModel model)
        {
            if (_markersContainer == null) return;

            // Set ruler width to match timeline duration
            if (_rulerElement != null)
            {
                float rulerWidth = model.Length * model.PixelsPerSecond;
                _rulerElement.style.width = rulerWidth;

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
#endif