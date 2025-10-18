using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI
{
    
    /// <summary>
    /// UI Toolkit version of TimelineRuler
    /// </summary>
    public class TimelineRulerToolkit
    {
        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset rulerTemplate;
        [SerializeField] private StyleSheet rulerStyleSheet;

        private VisualElement rulerElement;
        private TimelineEditorUIToolkit editorUI;

        public void Initialize(TimelineEditorUIToolkit editorUI, VisualElement rulerContainer, VisualTreeAsset template = null, StyleSheet styleSheet = null)
        {
            this.editorUI = editorUI;
            
            // Use provided templates or fallback to loading assets
            rulerTemplate = template;
            rulerStyleSheet = styleSheet;
            
            // Load assets if not provided
            if (rulerTemplate == null || rulerStyleSheet == null)
            {
                LoadAssets();
            }
            
            // Create ruler from template or fallback to programmatic creation
            if (rulerTemplate != null)
            {
                rulerElement = rulerTemplate.CloneTree().Q("timeline-ruler");
            }
            else
            {
                // Fallback to programmatic creation
                rulerElement = new VisualElement();
                rulerElement.AddToClassList("ruler");
                rulerElement.name = "timeline-ruler";
            }
            
            // Apply stylesheet if available
            if (rulerStyleSheet != null && rulerElement.styleSheets.Contains(rulerStyleSheet) == false)
            {
                rulerElement.styleSheets.Add(rulerStyleSheet);
            }
            
            rulerContainer.Add(rulerElement);
        }

        private void LoadAssets()
        {
            if (rulerTemplate == null)
            {
                rulerTemplate = Resources.Load<VisualTreeAsset>("UI/TimelineRulerToolkit");
            }
            
            if (rulerStyleSheet == null)
            {
                rulerStyleSheet = Resources.Load<StyleSheet>("UI/TimelineRulerToolkit");
            }
        }

        public void Rebuild(float duration, float frameRate)
        {
            if (rulerElement == null) return;
            
            rulerElement.Clear();
            
            // Create ruler markers based on duration and frame rate
            float pixelsPerSecond = editorUI.PixelsPerSecond;
            float zoom = editorUI.CurrentZoom;
            
            // Set the ruler width to match the timeline duration
            float rulerWidth = duration * pixelsPerSecond;
            rulerElement.style.width = rulerWidth;
            
            // Determine marker intervals based on zoom level
            float majorInterval = GetMajorInterval(zoom);
            float minorInterval = GetMinorInterval(zoom);
            
            // Add zoom class for conditional styling
            rulerElement.RemoveFromClassList("zoom-low");
            rulerElement.RemoveFromClassList("zoom-high");
            if (zoom < 0.5f)
            {
                rulerElement.AddToClassList("zoom-low");
            }
            else if (zoom > 2f)
            {
                rulerElement.AddToClassList("zoom-high");
            }
            
            // Generate major markers
            for (float time = 0; time <= duration; time += majorInterval)
            {
                CreateMarker(time, pixelsPerSecond, true);
            }
            
            // Generate minor markers only if zoom allows
            if (zoom >= 0.5f)
            {
                for (float time = minorInterval; time <= duration; time += minorInterval)
                {
                    // Skip if this would overlap with a major marker
                    if (time % majorInterval != 0)
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
            marker.style.left = time * pixelsPerSecond;
            
            // Only add labels to major markers to avoid clutter
            if (isMajor)
            {
                var label = new Label(FormatTime(time));
                label.AddToClassList("ruler-label");
                marker.Add(label);
            }
            
            rulerElement.Add(marker);
        }

        private string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 60)
            {
                return $"{timeInSeconds:F1}s";
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60);
                float seconds = timeInSeconds % 60;
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

        public void UpdateZoom(float zoom)
        {
            // Rebuild the ruler with new zoom level
            if (editorUI?.Director?.Project != null)
            {
                float duration = editorUI.Director.Length;
                float frameRate = editorUI.Director.Project.frameRate;
                Rebuild(duration, frameRate);
            }
        }

        public void SetCurrentTime(float time)
        {
            // Update current time indicator
            var currentTimeIndicator = rulerElement?.Q("ruler-current-time");
            if (currentTimeIndicator == null)
            {
                // Create current time indicator if it doesn't exist
                currentTimeIndicator = new VisualElement();
                currentTimeIndicator.name = "ruler-current-time";
                currentTimeIndicator.AddToClassList("ruler-current-time");
                rulerElement?.Add(currentTimeIndicator);
            }

            if (currentTimeIndicator != null)
            {
                float pixelsPerSecond = editorUI.PixelsPerSecond;
                currentTimeIndicator.style.left = time * pixelsPerSecond;
            }
        }

        public void ShowSnapGuide(float time, bool visible)
        {
            var snapGuide = rulerElement?.Q("ruler-snap-guide");
            if (snapGuide == null && visible)
            {
                snapGuide = new VisualElement();
                snapGuide.name = "ruler-snap-guide";
                snapGuide.AddToClassList("ruler-snap-guide");
                rulerElement.Add(snapGuide);
            }

            if (snapGuide != null)
            {
                snapGuide.EnableInClassList("visible", visible);
                if (visible)
                {
                    float pixelsPerSecond = editorUI.PixelsPerSecond;
                    snapGuide.style.left = time * pixelsPerSecond;
                }
            }
        }

        public void Cleanup()
        {
            rulerElement?.Clear();
            rulerElement = null;
        }

        /// <summary>
        /// Get the main ruler element for external access
        /// </summary>
        public VisualElement RulerElement => rulerElement;

        /// <summary>
        /// Check if the ruler has been properly initialized
        /// </summary>
        public bool IsInitialized => rulerElement != null && editorUI != null;
    }

}