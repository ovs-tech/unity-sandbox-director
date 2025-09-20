using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Timeline ruler showing time markers, playhead, and frame grid
    /// Handles scrubbing and time display
    /// </summary>
    public class TimelineRuler : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float rulerHeight = 40f;
        [SerializeField] private Color majorTickColor = Color.white;
        [SerializeField] private Color minorTickColor = Color.gray;
        [SerializeField] private Color playheadColor = Color.red;
        [SerializeField] private Color frameGridColor = new Color(1f, 1f, 1f, 0.1f);
        
        [Header("Text Settings")]
        [SerializeField] private Font timeFont;
        [SerializeField] private int timeFontSize = 12;
        [SerializeField] private Color timeTextColor = Color.white;
        
        // Components
        private RectTransform rectTransform;
        private Canvas rulerCanvas;
        private GraphicRaycaster graphicRaycaster;
        
        // Timeline reference
        private TimelineEditorUI timelineEditor;
        
        // Visual elements
        private RectTransform playhead;
        private Image playheadImage;
        private List<RulerTick> ticks = new List<RulerTick>();
        private List<GameObject> frameGridLines = new List<GameObject>();
        
        // Settings
        private float timelineLength = 10f;
        private float frameRate = 30f;
        private float pixelsPerSecond = 100f;
        
        // Tick intervals (in seconds)
        private readonly float[] tickIntervals = { 0.1f, 0.5f, 1f, 5f, 10f, 30f, 60f };
        
        #region Properties
        
        public float TimelineLength => timelineLength;
        public float FrameRate => frameRate;
        public RectTransform PlayheadTransform => playhead;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            // Setup ruler canvas for proper layering
            rulerCanvas = GetComponent<Canvas>();
            if (rulerCanvas == null)
            {
                rulerCanvas = gameObject.AddComponent<Canvas>();
                rulerCanvas.overrideSorting = true;
                rulerCanvas.sortingOrder = 1;
            }
            
            graphicRaycaster = GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Set initial size
            rectTransform.sizeDelta = new Vector2(1000f, rulerHeight);
            
            CreatePlayhead();
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize(TimelineEditorUI editor)
        {
            timelineEditor = editor;
            
            if (timelineEditor != null)
            {
                pixelsPerSecond = timelineEditor.PixelsPerSecond;
            }
        }
        
        public void Rebuild(float length, float fps)
        {
            timelineLength = length;
            frameRate = fps;
            
            if (timelineEditor != null)
            {
                pixelsPerSecond = timelineEditor.PixelsPerSecond;
            }
            
            // Update ruler size
            rectTransform.sizeDelta = new Vector2(timelineLength * pixelsPerSecond, rulerHeight);
            
            // Rebuild ticks and grid
            RebuildTicks();
            RebuildFrameGrid();
        }
        
        #endregion
        
        #region Playhead
        
        private void CreatePlayhead()
        {
            // Create playhead GameObject
            var playheadGO = new GameObject("Playhead", typeof(RectTransform), typeof(Image));
            playheadGO.transform.SetParent(transform, false);
            
            playhead = playheadGO.GetComponent<RectTransform>();
            playheadImage = playheadGO.GetComponent<Image>();
            
            // Setup playhead visual
            playheadImage.color = playheadColor;
            playhead.sizeDelta = new Vector2(2f, rulerHeight + 1000f); // Extend beyond ruler
            playhead.anchorMin = new Vector2(0f, 0f);
            playhead.anchorMax = new Vector2(0f, 1f);
            playhead.pivot = new Vector2(0.5f, 0f);
            playhead.anchoredPosition = Vector2.zero;
            
            // Add playhead handle for dragging
            var handle = playheadGO.AddComponent<PlayheadHandle>();
            handle.Initialize(this);
        }
        
        public void SetPlayheadPosition(float time)
        {
            if (playhead != null)
            {
                float xPos = time * pixelsPerSecond;
                playhead.anchoredPosition = new Vector2(xPos, 0f);
            }
        }
        
        public float GetTimeAtPosition(float xPosition)
        {
            return xPosition / pixelsPerSecond;
        }
        
        public float GetPositionAtTime(float time)
        {
            return time * pixelsPerSecond;
        }
        
        #endregion
        
        #region Ticks and Grid
        
        private void RebuildTicks()
        {
            // Clear existing ticks
            foreach (var tick in ticks)
            {
                if (tick.gameObject != null)
                    DestroyImmediate(tick.gameObject);
            }
            ticks.Clear();
            
            // Determine appropriate tick interval based on zoom
            float tickInterval = DetermineTickInterval();
            
            // Create ticks
            for (float time = 0f; time <= timelineLength; time += tickInterval)
            {
                CreateTick(time, tickInterval);
            }
        }
        
        private float DetermineTickInterval()
        {
            float targetPixelSpacing = 60f; // Desired spacing between major ticks
            float targetTimeSpacing = targetPixelSpacing / pixelsPerSecond;
            
            // Find best interval
            foreach (float interval in tickIntervals)
            {
                if (interval >= targetTimeSpacing)
                    return interval;
            }
            
            return tickIntervals[tickIntervals.Length - 1];
        }
        
        private void CreateTick(float time, float interval)
        {
            bool isMajor = Mathf.Approximately(time % interval, 0f);
            
            // Create tick GameObject
            var tickGO = new GameObject($"Tick_{time:F2}", typeof(RectTransform), typeof(Image));
            tickGO.transform.SetParent(transform, false);
            
            var tickRect = tickGO.GetComponent<RectTransform>();
            var tickImage = tickGO.GetComponent<Image>();
            
            // Setup tick visual
            tickImage.color = isMajor ? majorTickColor : minorTickColor;
            
            float tickHeight = isMajor ? rulerHeight * 0.6f : rulerHeight * 0.3f;
            tickRect.sizeDelta = new Vector2(1f, tickHeight);
            tickRect.anchorMin = new Vector2(0f, 1f);
            tickRect.anchorMax = new Vector2(0f, 1f);
            tickRect.pivot = new Vector2(0.5f, 1f);
            
            float xPos = time * pixelsPerSecond;
            tickRect.anchoredPosition = new Vector2(xPos, 0f);
            
            // Create time label for major ticks
            Text timeLabel = null;
            if (isMajor)
            {
                var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGO.transform.SetParent(tickGO.transform, false);
                
                var labelRect = labelGO.GetComponent<RectTransform>();
                timeLabel = labelGO.GetComponent<Text>();
                
                // Setup label
                timeLabel.text = FormatTime(time);
                timeLabel.font = timeFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                timeLabel.fontSize = timeFontSize;
                timeLabel.color = timeTextColor;
                timeLabel.alignment = TextAnchor.UpperCenter;
                
                labelRect.sizeDelta = new Vector2(60f, 20f);
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = new Vector2(0f, -5f);
            }
            
            // Store tick data
            var tick = new RulerTick
            {
                gameObject = tickGO,
                time = time,
                isMajor = isMajor,
                image = tickImage,
                label = timeLabel
            };
            
            ticks.Add(tick);
        }
        
        private void RebuildFrameGrid()
        {
            // Clear existing grid lines
            foreach (var line in frameGridLines)
            {
                if (line != null)
                    DestroyImmediate(line);
            }
            frameGridLines.Clear();
            
            // Only show frame grid if zoomed in enough
            float framePixelSpacing = pixelsPerSecond / frameRate;
            if (framePixelSpacing < 5f) return; // Too dense
            
            // Create frame grid lines
            int totalFrames = Mathf.FloorToInt(timelineLength * frameRate);
            
            for (int frame = 0; frame <= totalFrames; frame++)
            {
                float time = frame / frameRate;
                if (time > timelineLength) break;
                
                CreateFrameGridLine(time);
            }
        }
        
        private void CreateFrameGridLine(float time)
        {
            var lineGO = new GameObject($"FrameLine_{time:F3}", typeof(RectTransform), typeof(Image));
            lineGO.transform.SetParent(transform, false);
            
            var lineRect = lineGO.GetComponent<RectTransform>();
            var lineImage = lineGO.GetComponent<Image>();
            
            // Setup frame line visual
            lineImage.color = frameGridColor;
            lineRect.sizeDelta = new Vector2(1f, 2000f); // Extend across entire timeline height
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(0f, 1f);
            lineRect.pivot = new Vector2(0.5f, 0f);
            
            float xPos = time * pixelsPerSecond;
            lineRect.anchoredPosition = new Vector2(xPos, 0f);
            
            frameGridLines.Add(lineGO);
        }
        
        #endregion
        
        #region Zoom Handling
        
        public void UpdateZoom(float zoom)
        {
            if (timelineEditor != null)
            {
                pixelsPerSecond = timelineEditor.PixelsPerSecond;
            }
            
            // Update ruler size
            rectTransform.sizeDelta = new Vector2(timelineLength * pixelsPerSecond, rulerHeight);
            
            // Rebuild ticks and grid for new zoom level
            RebuildTicks();
            RebuildFrameGrid();
            
            // Update playhead position
            if (timelineEditor?.Director != null)
            {
                SetPlayheadPosition(timelineEditor.Director.Time);
            }
        }
        
        #endregion
        
        #region Utility
        
        private string FormatTime(float time)
        {
            if (time < 60f)
            {
                return $"{time:F1}s";
            }
            else
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                float seconds = time % 60f;
                return $"{minutes}:{seconds:00.0}";
            }
        }
        
        public bool IsPositionOnRuler(Vector2 localPosition)
        {
            return rectTransform.rect.Contains(localPosition);
        }
        
        public float SnapTimeToFrame(float time)
        {
            if (frameRate <= 0f) return time;
            
            int frame = Mathf.RoundToInt(time * frameRate);
            return frame / frameRate;
        }
        
        #endregion
        
        #region Nested Classes
        
        [System.Serializable]
        private class RulerTick
        {
            public GameObject gameObject;
            public float time;
            public bool isMajor;
            public Image image;
            public Text label;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Handle component for playhead dragging
    /// </summary>
    public class PlayheadHandle : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, 
        UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
    {
        private TimelineRuler ruler;
        private bool isDragging = false;
        
        public void Initialize(TimelineRuler timelineRuler)
        {
            ruler = timelineRuler;
        }
        
        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            isDragging = true;
        }
        
        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!isDragging || ruler == null) return;
            
            // Convert screen position to local ruler position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ruler.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
            
            // Calculate time and scrub
            float time = ruler.GetTimeAtPosition(localPos.x);
            time = Mathf.Clamp(time, 0f, ruler.TimelineLength);
            
            // Apply frame snapping if enabled
            // (This would be controlled by the timeline editor's snap settings)
            
            // Update playhead position and scrub timeline
            ruler.SetPlayheadPosition(time);
            
            // Notify timeline editor about scrubbing
            // TODO: Implement scrubbing notification
        }
        
        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            isDragging = false;
        }
    }
}