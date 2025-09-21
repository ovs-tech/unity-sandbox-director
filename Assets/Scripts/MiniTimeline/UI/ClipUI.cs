using System;
using UnityEngine;
using UnityEngine.UI;
using MiniTimeline.Core;

namespace MiniTimeline.UI
{
    /// <summary>
    /// UI representation of a timeline clip
    /// Handles visual display, selection, and drag operations
    /// </summary>
    public class ClipUI : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Image clipBackground;
        [SerializeField] private Image clipBorder;
        [SerializeField] private Text clipLabel;
        [SerializeField] private RectTransform resizeHandleLeft;
        [SerializeField] private RectTransform resizeHandleRight;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.4f, 0.6f, 0.8f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.5f, 0.7f, 0.9f, 0.9f);
        [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.8f, 0.2f, 1f);
        
        // References
        private TrackUI parentTrack;
        private IMiniClip clip;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        
        // State
        private bool isSelected = false;
        private bool isHovered = false;
        private bool isDragging = false;
        private bool isResizing = false;
        private ResizeHandle activeResizeHandle = ResizeHandle.None;
        
        // Drag data
        private Vector2 dragStartPosition;
        private float dragStartTime;
        private float dragStartDuration;
        private Vector2 initialClipPosition;
        
        // Layout
        private float minClipWidth = 10f;
        
        #region Enums
        
        public enum ResizeHandle
        {
            None,
            Left,
            Right
        }
        
        #endregion
        
        #region Events
        
        public event Action<ClipUI> OnClipSelected;
        public event Action<ClipUI> OnClipDeselected;
        public event Action<ClipUI, Vector2> OnClipDragged;
        public event Action<ClipUI, float, float> OnClipResized;
        public event Action<ClipUI> OnClipDoubleClicked;
        
        #endregion
        
        #region Properties
        
        public IMiniClip Clip => clip;
        public TrackUI ParentTrack => parentTrack;
        public bool IsSelected => isSelected;
        public bool IsDragging => isDragging;
        public RectTransform RectTransform => rectTransform;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            
            // Create UI structure if not set up
            if (clipBackground == null)
            {
                SetupClipStructure();
            }
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize(TrackUI track, IMiniClip clipData)
        {
            parentTrack = track;
            clip = clipData;
            
            // Update visual appearance
            UpdateClipAppearance();
            UpdateLayout();
        }
        
        private void SetupClipStructure()
        {
            // Background
            if (clipBackground == null)
            {
                var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGO.transform.SetParent(transform, false);
                
                var bgRect = bgGO.GetComponent<RectTransform>();
                clipBackground = bgGO.GetComponent<Image>();
                
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                
                clipBackground.color = normalColor;
            }
            
            // Border
            if (clipBorder == null)
            {
                var borderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
                borderGO.transform.SetParent(transform, false);
                
                var borderRect = borderGO.GetComponent<RectTransform>();
                clipBorder = borderGO.GetComponent<Image>();
                
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = Vector2.zero;
                borderRect.offsetMax = Vector2.zero;
                
                clipBorder.color = borderColor;
                clipBorder.sprite = CreateBorderSprite();
                clipBorder.type = Image.Type.Sliced;
            }
            
            // Label
            if (clipLabel == null)
            {
                var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGO.transform.SetParent(transform, false);
                
                var labelRect = labelGO.GetComponent<RectTransform>();
                clipLabel = labelGO.GetComponent<Text>();
                
                labelRect.anchorMin = new Vector2(0.05f, 0.2f);
                labelRect.anchorMax = new Vector2(0.95f, 0.8f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                
                clipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                clipLabel.fontSize = 10;
                clipLabel.color = Color.white;
                clipLabel.alignment = TextAnchor.MiddleLeft;
            }
            
            // Resize handles
            CreateResizeHandles();
        }
        
        private void CreateResizeHandles()
        {
            // Left resize handle - simple visual indicator, no event handling
            var leftHandleGO = new GameObject("ResizeLeft", typeof(RectTransform), typeof(Image));
            leftHandleGO.transform.SetParent(transform, false);
            
            resizeHandleLeft = leftHandleGO.GetComponent<RectTransform>();
            var leftImage = leftHandleGO.GetComponent<Image>();
            
            resizeHandleLeft.anchorMin = new Vector2(0f, 0f);
            resizeHandleLeft.anchorMax = new Vector2(0f, 1f);
            resizeHandleLeft.sizeDelta = new Vector2(8f, 0f);
            resizeHandleLeft.anchoredPosition = Vector2.zero;
            
            leftImage.color = new Color(1f, 1f, 1f, 0.5f);
            
            // Right resize handle - simple visual indicator, no event handling
            var rightHandleGO = new GameObject("ResizeRight", typeof(RectTransform), typeof(Image));
            rightHandleGO.transform.SetParent(transform, false);
            
            resizeHandleRight = rightHandleGO.GetComponent<RectTransform>();
            var rightImage = rightHandleGO.GetComponent<Image>();
            
            resizeHandleRight.anchorMin = new Vector2(1f, 0f);
            resizeHandleRight.anchorMax = new Vector2(1f, 1f);
            resizeHandleRight.sizeDelta = new Vector2(8f, 0f);
            resizeHandleRight.anchoredPosition = Vector2.zero;
            
            rightImage.color = new Color(1f, 1f, 1f, 0.5f);
            
            // Initially hide resize handles
            SetResizeHandlesVisible(false);
        }
        
        private Sprite CreateBorderSprite()
        {
            // Create a simple 1-pixel border sprite
            var texture = new Texture2D(3, 3);
            var colors = new Color[9];
            
            // Border pixels
            colors[0] = colors[1] = colors[2] = Color.white; // Top
            colors[3] = colors[5] = Color.white; // Sides
            colors[6] = colors[7] = colors[8] = Color.white; // Bottom
            colors[4] = Color.clear; // Center
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateClipAppearance()
        {
            if (clip == null) return;
            
            // Update label
            if (clipLabel != null)
            {
                clipLabel.text = GetClipDisplayName();
            }
            
            // Update colors based on state
            UpdateColors();
        }
        
        private void UpdateColors()
        {
            Color bgColor = normalColor;
            Color brColor = borderColor;
            
            if (isSelected)
            {
                bgColor = selectedColor;
                brColor = selectedBorderColor;
            }
            else if (isHovered)
            {
                bgColor = hoverColor;
            }
            
            if (clipBackground != null)
                clipBackground.color = bgColor;
            
            if (clipBorder != null)
                clipBorder.color = brColor;
        }
        
        private string GetClipDisplayName()
        {
            if (clip == null) return "Clip";
            
            // Try to get a meaningful name from the clip
            string name = clip.Id;
            
            // Shorten very long names
            if (name.Length > 15)
            {
                name = name.Substring(0, 12) + "...";
            }
            
            // For marker clips (duration 0), show as event
            if (clip.Duration <= 0.001f)
            {
                return $"● {name}";
            }
            
            return name;
        }
        
        public void UpdateLayout()
        {
            if (clip == null || parentTrack?.TimelineEditor == null) return;
            
            float pixelsPerSecond = parentTrack.TimelineEditor.PixelsPerSecond;
            
            // Calculate position and size
            float xPos = clip.Start * pixelsPerSecond;
            float width = Mathf.Max(clip.Duration * pixelsPerSecond, minClipWidth);
            
            // For marker clips, use a fixed small width
            if (clip.Duration <= 0.001f)
            {
                width = 20f;
            }
            
            // Update rect transform
            rectTransform.anchorMin = new Vector2(0f, 0.1f);
            rectTransform.anchorMax = new Vector2(0f, 0.9f);
            rectTransform.sizeDelta = new Vector2(width, 0f);
            rectTransform.anchoredPosition = new Vector2(xPos, 0f);
        }
        
        #endregion
        
        #region Selection
        
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            
            isSelected = selected;
            UpdateColors();
            SetResizeHandlesVisible(selected);
            
            if (selected)
                OnClipSelected?.Invoke(this);
            else
                OnClipDeselected?.Invoke(this);
        }
        
        private void SetResizeHandlesVisible(bool visible)
        {
            if (resizeHandleLeft != null)
                resizeHandleLeft.gameObject.SetActive(visible);
            
            if (resizeHandleRight != null)
                resizeHandleRight.gameObject.SetActive(visible);
        }
        
        #endregion
        
        #region Input System Integration
        
        /// <summary>
        /// Called by TimelineEditorUI when this clip is clicked/touched
        /// </summary>
        public void OnClipTouched()
        {
            // Toggle selection
            SetSelected(!isSelected);
        }
        
        /// <summary>
        /// Called by TimelineEditorUI when this clip is double-clicked/touched
        /// </summary>
        public void HandleDoubleClick()
        {
            OnClipDoubleClicked?.Invoke(this);
        }
        
        /// <summary>
        /// Called by TimelineEditorUI when hover state changes
        /// </summary>
        public void SetHoverState(bool hovered)
        {
            isHovered = hovered;
            UpdateColors();
        }
        
        // All input handling is now managed by TimelineEditorUI through the Input System
        
        #endregion
        
        #region Resizing - Now handled by TimelineEditorUI
        
        /// <summary>
        /// Called by TimelineEditorUI to set resize visual state
        /// </summary>
        public void SetResizeVisualState(bool isResizing, ResizeHandle handle)
        {
            this.isResizing = isResizing;
            this.activeResizeHandle = handle;
        }
        
        /// <summary>
        /// Get the resize handle at a specific local position (in clip's local space)
        /// </summary>
        public ResizeHandle GetResizeHandleAtPosition(Vector2 localPosition)
        {
            if (!isSelected) return ResizeHandle.None;
            
            // Check left resize handle (positioned at left edge)
            if (resizeHandleLeft != null && resizeHandleLeft.gameObject.activeInHierarchy)
            {
                Rect leftHandleRect = new Rect(resizeHandleLeft.anchoredPosition.x - resizeHandleLeft.sizeDelta.x/2, 
                                               resizeHandleLeft.anchoredPosition.y - rectTransform.sizeDelta.y/2,
                                               resizeHandleLeft.sizeDelta.x, 
                                               rectTransform.sizeDelta.y);
                
                if (leftHandleRect.Contains(localPosition))
                {
                    return ResizeHandle.Left;
                }
            }
            
            // Check right resize handle (positioned at right edge)
            if (resizeHandleRight != null && resizeHandleRight.gameObject.activeInHierarchy)
            {
                Rect rightHandleRect = new Rect(rectTransform.sizeDelta.x - resizeHandleRight.sizeDelta.x/2, 
                                                resizeHandleRight.anchoredPosition.y - rectTransform.sizeDelta.y/2,
                                                resizeHandleRight.sizeDelta.x, 
                                                rectTransform.sizeDelta.y);
                
                if (rightHandleRect.Contains(localPosition))
                {
                    return ResizeHandle.Right;
                }
            }
            
            return ResizeHandle.None;
        }
        
        #endregion
        
        #region Utility
        
        private void UpdateClipTiming(float newStart, float newDuration)
        {
            // TODO: This should be done through the command system
            // For now, we'll need to update the clip data directly
            // In a real implementation, this would create a command
            
            UpdateClipVisualTiming(newStart, newDuration);
        }
        
        public void UpdateClipVisualTiming(float newStart, float newDuration)
        {
            // Update visual position and size
            if (parentTrack?.TimelineEditor != null)
            {
                float pixelsPerSecond = parentTrack.TimelineEditor.PixelsPerSecond;
                float xPos = newStart * pixelsPerSecond;
                float width = Mathf.Max(newDuration * pixelsPerSecond, minClipWidth);
                
                rectTransform.anchoredPosition = new Vector2(xPos, rectTransform.anchoredPosition.y);
                rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
            }
        }
        
        private float SnapTimeToFrame(float time)
        {
            if (parentTrack?.TimelineEditor?.Director?.Project == null) return time;
            
            float frameRate = parentTrack.TimelineEditor.Director.Project.frameRate;
            if (frameRate <= 0f) return time;
            
            int frame = Mathf.RoundToInt(time * frameRate);
            return frame / frameRate;
        }
        
        public bool ContainsPosition(Vector2 localPosition)
        {
            return rectTransform.rect.Contains(localPosition);
        }
        
        /// <summary>
        /// Called by TimelineEditorUI during drag operations to update visual state
        /// </summary>
        public void SetDragVisualState(bool isDragging)
        {
            this.isDragging = isDragging;
            
            if (isDragging)
            {
                // Bring to front during drag
                transform.SetAsLastSibling();
                canvasGroup.alpha = 0.8f;
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }
        
        #endregion
    }
}