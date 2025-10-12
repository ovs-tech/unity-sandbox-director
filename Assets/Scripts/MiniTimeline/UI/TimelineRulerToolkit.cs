using UnityEngine.UIElements;

namespace MiniTimeline.UI
{
    
    /// <summary>
    /// UI Toolkit version of TimelineRuler
    /// </summary>
    public class TimelineRulerToolkit
    {
        private VisualElement rulerElement;
        private TimelineEditorUIToolkit editorUI;

        public void Initialize(TimelineEditorUIToolkit editorUI, VisualElement rulerContainer)
        {
            this.editorUI = editorUI;
            
            rulerElement = new VisualElement();
            rulerElement.AddToClassList("ruler");
            rulerElement.name = "timeline-ruler";
            
            rulerContainer.Add(rulerElement);
        }

        public void Rebuild(float duration, float frameRate)
        {
            rulerElement.Clear();
            
            // Create ruler markers based on duration and frame rate
            float timeStep = 1f; // 1 second intervals
            float pixelsPerSecond = editorUI.PixelsPerSecond;
            
            for (float time = 0; time <= duration; time += timeStep)
            {
                var marker = new VisualElement();
                marker.AddToClassList("ruler-marker");
                marker.style.left = time * pixelsPerSecond;
                
                var label = new Label($"{time:F1}s");
                label.AddToClassList("ruler-label");
                marker.Add(label);
                
                rulerElement.Add(marker);
            }
        }

        public void UpdateZoom(float zoom)
        {
            // Update ruler based on new zoom level
        }
    }

}