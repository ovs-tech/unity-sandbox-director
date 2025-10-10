using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MiniTimeline.Core;
using MiniTimeline.UI.Commands;
using MiniTimeline.Tracks;
using Core.UI.FormSubmit;
using Core.UI.ContextMenu;
using MiniTimeline.UI.FormDefinitions;

namespace MiniTimeline.UI
{
    /// <summary>
    /// UI representation of a timeline clip
    /// Handles visual display, selection, and drag operations
    /// Implements dynamic context menu registration
    /// </summary>
    public class ClipUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IContextMenuRegisterable
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
        private float clipDragOffset;
        
        // Resize data
        private float resizeDragStartTime;
        private float resizeDragStartDuration;
        private float resizeStartMouseX;
        
        // Layout
        private float minClipWidth = 10f;
        
        // Long press detection
        [Header("Long Press Settings")]
        [SerializeField] private float longPressDuration = 0.8f; // Time to trigger long press in seconds
        
        private bool isLongPressing = false;
        private bool longPressTriggered = false;
        private Coroutine longPressCoroutine = null;
        private Vector2 longPressStartPosition;
        private float longPressMoveThreshold = 20f; // Pixels
        
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
        public event Action<ClipUI, Vector2> OnClipLongPressed; // Screen position where long press occurred
        
        // Input blocking events
        public event Action<ClipUI> OnStartInteraction;  // When clip starts being interacted with (drag/resize)
        public event Action<ClipUI> OnEndInteraction;    // When clip interaction ends
        
        #endregion
        
        #region Properties
        
        public IMiniClip Clip => clip;
        public TrackUI ParentTrack => parentTrack;
        public bool IsSelected => isSelected;
        public bool IsDragging => isDragging;
        public bool IsLongPressing => isLongPressing;
        public RectTransform RectTransform => rectTransform;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            
            // Set the pivot to center for more predictable behavior during resize
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // Create UI structure if not set up
            if (clipBackground == null)
            {
                SetupClipStructure();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up long press coroutine
            StopLongPressDetection();
            
            // Unregister from context menu
            UnregisterContextMenuItems();
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
            
            // Register for context menu
            RegisterContextMenuItems();
            
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
            // Left resize handle - positioned inside the clip for better hit detection
            var leftHandleGO = new GameObject("ResizeLeft", typeof(RectTransform), typeof(Image));
            leftHandleGO.transform.SetParent(transform, false);
            
            resizeHandleLeft = leftHandleGO.GetComponent<RectTransform>();
            var leftImage = leftHandleGO.GetComponent<Image>();
            
            // Position handle at the left edge but with some width inside the clip
            resizeHandleLeft.anchorMin = new Vector2(0f, 0f);
            resizeHandleLeft.anchorMax = new Vector2(0f, 1f);
            resizeHandleLeft.sizeDelta = new Vector2(12f, 0f); // Wider for easier touch
            resizeHandleLeft.anchoredPosition = new Vector2(6f, 0f); // Half width inside clip
            
            leftImage.color = new Color(0.8f, 0.8f, 1f, 0.7f); // More visible color
            
            // Right resize handle - positioned inside the clip for better hit detection
            var rightHandleGO = new GameObject("ResizeRight", typeof(RectTransform), typeof(Image));
            rightHandleGO.transform.SetParent(transform, false);
            
            resizeHandleRight = rightHandleGO.GetComponent<RectTransform>();
            var rightImage = rightHandleGO.GetComponent<Image>();
            
            // Position handle at the right edge but with some width inside the clip
            resizeHandleRight.anchorMin = new Vector2(1f, 0f);
            resizeHandleRight.anchorMax = new Vector2(1f, 1f);
            resizeHandleRight.sizeDelta = new Vector2(12f, 0f); // Wider for easier touch
            resizeHandleRight.anchoredPosition = new Vector2(-6f, 0f); // Half width inside clip
            
            rightImage.color = new Color(0.8f, 0.8f, 1f, 0.7f); // More visible color
            
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
        
        public void UpdateClipAppearance()
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
            
            // Update rect transform - use center pivot for predictable resize behavior
            rectTransform.anchorMin = new Vector2(0f, 0.1f);
            rectTransform.anchorMax = new Vector2(0f, 0.9f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot for predictable behavior
            rectTransform.sizeDelta = new Vector2(width, 0f);
            // With center pivot, position needs to be at the center of the clip
            rectTransform.anchoredPosition = new Vector2(xPos + width / 2f, 0f);
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
            bool wasInteracting = this.isResizing || this.isDragging;
            
            this.isResizing = isResizing;
            this.activeResizeHandle = handle;
            
            bool isInteracting = this.isResizing || this.isDragging;
            
            // Trigger events when interaction state changes
            if (!wasInteracting && isInteracting)
            {
                OnStartInteraction?.Invoke(this);
            }
            else if (wasInteracting && !isInteracting)
            {
                OnEndInteraction?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Get the resize handle at a specific local position (in clip's local space)
        /// </summary>
        public ResizeHandle GetResizeHandleAtPosition(Vector2 localPosition)
        {
            if (!isSelected) 
            {
                return ResizeHandle.None;
            }
            
            // Get the clip's local rect for bounds checking
            Rect clipRect = rectTransform.rect;
            
            
            // Check left resize handle - positioned with center at left edge + 6 pixels inside
            if (resizeHandleLeft != null && resizeHandleLeft.gameObject.activeInHierarchy)
            {
                // Create hit area for left handle - positioned inside the clip
                float handleWidth = 20f; // Generous hit area for touch
                float handleCenterX = clipRect.xMin + 6f; // Center of handle is 6 pixels inside clip
                Rect leftHandleRect = new Rect(handleCenterX - handleWidth/2, 
                                               clipRect.yMin,
                                               handleWidth, 
                                               clipRect.height);
                
                if (leftHandleRect.Contains(localPosition))
                {
                    return ResizeHandle.Left;
                }
            }
            
            // Check right resize handle - positioned with center at right edge - 6 pixels inside
            if (resizeHandleRight != null && resizeHandleRight.gameObject.activeInHierarchy)
            {
                // Create hit area for right handle - positioned inside the clip
                float handleWidth = 20f; // Generous hit area for touch
                float handleCenterX = clipRect.xMax - 6f; // Center of handle is 6 pixels inside clip
                Rect rightHandleRect = new Rect(handleCenterX - handleWidth/2, 
                                                clipRect.yMin,
                                                handleWidth, 
                                                clipRect.height);
                
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
                float leftEdgePos = newStart * pixelsPerSecond;
                float width = Mathf.Max(newDuration * pixelsPerSecond, minClipWidth);
                
                // With center pivot (0.5, 0.5), position needs to be at the center of the clip
                float centerPos = leftEdgePos + width / 2f;
                Vector2 currentPos = rectTransform.anchoredPosition;
                
                // Update both position and size
                rectTransform.anchoredPosition = new Vector2(centerPos, currentPos.y);
                rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
                
            }
        }
        
        /// <summary>
        /// Update only the left edge (start time) - used during left handle resize
        /// </summary>
        public void UpdateClipVisualStart(float newStart, float duration)
        {
            if (parentTrack?.TimelineEditor != null)
            {
                float pixelsPerSecond = parentTrack.TimelineEditor.PixelsPerSecond;
                float leftEdgePos = newStart * pixelsPerSecond;
                float width = Mathf.Max(duration * pixelsPerSecond, minClipWidth);
                
                // With center pivot, position is at center of clip
                float centerPos = leftEdgePos + width / 2f;
                Vector2 currentPos = rectTransform.anchoredPosition;
                
                rectTransform.anchoredPosition = new Vector2(centerPos, currentPos.y);
                rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
                
            }
        }
        
        /// <summary>
        /// Update only the right edge (duration) - used during right handle resize
        /// </summary>
        public void UpdateClipVisualDuration(float startTime, float newDuration)
        {
            if (parentTrack?.TimelineEditor != null)
            {
                float pixelsPerSecond = parentTrack.TimelineEditor.PixelsPerSecond;
                float leftEdgePos = startTime * pixelsPerSecond;
                float width = Mathf.Max(newDuration * pixelsPerSecond, minClipWidth);
                
                // With center pivot, position is at center of clip
                float centerPos = leftEdgePos + width / 2f;
                Vector2 currentPos = rectTransform.anchoredPosition;
                
                rectTransform.anchoredPosition = new Vector2(centerPos, currentPos.y);
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
            bool wasInteracting = this.isResizing || this.isDragging;
            
            this.isDragging = isDragging;
            
            bool isInteracting = this.isResizing || this.isDragging;
            
            // Trigger events when interaction state changes
            if (!wasInteracting && isInteracting)
            {
                OnStartInteraction?.Invoke(this);
            }
            else if (wasInteracting && !isInteracting)
            {
                OnEndInteraction?.Invoke(this);
            }
            
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
        
        #region Long Press Detection
        
        /// <summary>
        /// Start long press detection
        /// </summary>
        private void StartLongPressDetection(Vector2 screenPosition)
        {
            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
            }
            
            isLongPressing = true;
            longPressTriggered = false;
            longPressStartPosition = screenPosition;
            longPressCoroutine = StartCoroutine(LongPressCoroutine(screenPosition));
        }
        
        /// <summary>
        /// Stop long press detection
        /// </summary>
        private void StopLongPressDetection()
        {
            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
                longPressCoroutine = null;
            }
            
            isLongPressing = false;
        }
        
        /// <summary>
        /// Check if current pointer position is still within long press threshold
        /// </summary>
        private bool IsWithinLongPressThreshold(Vector2 currentScreenPosition)
        {
            float distance = Vector2.Distance(longPressStartPosition, currentScreenPosition);
            return distance <= longPressMoveThreshold;
        }
        
        /// <summary>
        /// Coroutine that handles long press timing
        /// </summary>
        private IEnumerator LongPressCoroutine(Vector2 screenPosition)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < longPressDuration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                
                // Check if we're still pressing and within threshold
                if (!isLongPressing)
                {
                    yield break;
                }
            }
            
            // Long press completed
            if (isLongPressing && !longPressTriggered)
            {
                longPressTriggered = true;
                HandleLongPress(screenPosition);
            }
        }
        
        /// <summary>
        /// Handle long press event
        /// </summary>
        private void HandleLongPress(Vector2 screenPosition)
        {
            Debug.Log($"Long press detected on clip: {clip?.Id}");
            
            // Trigger the long press event
            OnClipLongPressed?.Invoke(this, screenPosition);
            
            // Show context menu directly using dynamic registration
            ShowClipContextMenu(screenPosition, true);
        }
        
        #endregion
        
        #region Public Context Menu Interface
        
        /// <summary>
        /// Public method to show context menu for this clip
        /// Can be called by external systems when needed
        /// </summary>
        /// <param name="screenPosition">Screen position for the menu</param>
        /// <param name="fromLongPress">Whether this was triggered by a long press</param>
        public void ShowContextMenu(Vector2 screenPosition, bool fromLongPress = false)
        {
            ShowClipContextMenu(screenPosition, fromLongPress);
        }
        
        #endregion
        
        #region UI Event System Handlers
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Don't handle click if long press was triggered
                if (!longPressTriggered)
                {
                    // Handle left click - selection
                    HandleClipClick();
                }
                
                // Reset long press triggered flag
                longPressTriggered = false;
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Handle right click - context menu
                HandleClipRightClick(eventData);
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Start long press detection
                StartLongPressDetection(eventData.position);
                
                // Check if we're clicking on a resize handle
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, eventData.position, eventData.pressEventCamera, out localPos);
                
                var resizeHandle = GetResizeHandleAtPosition(localPos);
                if (resizeHandle != ResizeHandle.None && isSelected)
                {
                    // Stop long press detection for resize handles
                    StopLongPressDetection();
                    // Don't start drag if we're on a resize handle
                    return;
                }
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            // If long press was triggered, add a delay to prevent immediate backdrop clicks
            if (longPressTriggered)
            {
                // Don't allow this pointer up event to propagate immediately
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
            }
            
            // Stop long press detection when pointer is released
            StopLongPressDetection();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            // Stop long press detection when drag begins
            StopLongPressDetection();
            
            // Check if we're on a resize handle first using local position
            Vector2 localPosClip;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out localPosClip);
            
            var resizeHandle = GetResizeHandleAtPosition(localPosClip);
            if (resizeHandle != ResizeHandle.None && isSelected)
            {
                // Start resize - use track container position for consistency
                Vector2 localPosTrack;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentTrack.ClipContainer, eventData.position, eventData.pressEventCamera, out localPosTrack);
                HandleResizeStart(resizeHandle, localPosTrack);
            }
            else
            {
                // Start drag - use track container position for consistency
                Vector2 localPosTrack;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentTrack.ClipContainer, eventData.position, eventData.pressEventCamera, out localPosTrack);
                HandleDragStart(localPosTrack);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            // Check if we should cancel long press due to movement
            if (isLongPressing && !IsWithinLongPressThreshold(eventData.position))
            {
                StopLongPressDetection();
            }
            
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentTrack.ClipContainer, eventData.position, eventData.pressEventCamera, out localPos);
            
            if (isResizing)
            {
                HandleResize(localPos);
            }
            else if (isDragging)
            {
                HandleDrag(localPos);
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            if (isResizing)
            {
                HandleResizeEnd();
            }
            else if (isDragging)
            {
                HandleDragEnd();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleClipClick()
        {
            // Notify parent track or timeline editor about selection
            OnClipSelected?.Invoke(this);
            
            // Select this clip
            if (!isSelected)
            {
                SetSelected(true);
            }
        }
        
        /// <summary>
        /// Handle right click - context menu
        /// </summary>
        private void HandleClipRightClick(PointerEventData eventData)
        {
            // Show context menu directly
            ShowClipContextMenu(eventData.position, false);
        }
        
        /// <summary>
        /// Show context menu for this clip at the specified screen position
        /// </summary>
        /// <param name="screenPosition">Screen position for the menu</param>
        /// <param name="fromLongPress">Whether this was triggered by a long press</param>
        private void ShowClipContextMenu(Vector2 screenPosition, bool fromLongPress = false)
        {
            Debug.Log($"Showing context menu for clip: {clip?.Id} (long press: {fromLongPress})");
            
            // Use the dynamic context menu system
            if (TimelineContextMenu.HasInstance)
            {
                // Disable timeline interaction through parent track if possible
                DisableTimelineInteraction();
                
                // Show dynamic menu with this clip as the target
                TimelineContextMenu.Instance.ShowDynamicMenu(screenPosition, this, "clip");
                
                // Subscribe to menu closed event to re-enable interaction
                TimelineContextMenu.Instance.OnMenuClosed -= EnableTimelineInteraction;
                TimelineContextMenu.Instance.OnMenuClosed += EnableTimelineInteraction;
            }
            else
            {
                Debug.LogWarning("TimelineContextMenu instance not available");
            }
        }
        
        /// <summary>
        /// Disable timeline interaction while context menu is open
        /// </summary>
        private void DisableTimelineInteraction()
        {
            // Try to disable through parent track's timeline editor if available
            if (parentTrack?.TimelineEditor != null)
            {
                // We can't directly access private methods, so we'll rely on the context menu system
                // The timeline editor should handle this through event subscription
            }
        }
        
        /// <summary>
        /// Re-enable timeline interaction when context menu closes
        /// </summary>
        private void EnableTimelineInteraction()
        {
            // Unsubscribe from the event to avoid memory leaks
            if (TimelineContextMenu.HasInstance)
            {
                TimelineContextMenu.Instance.OnMenuClosed -= EnableTimelineInteraction;
            }
            
            // The timeline editor should handle re-enabling interaction through its own event system
        }
        
        private void HandleDragStart(Vector2 localPos)
        {
            if (!isSelected) HandleClipClick();
            
            isDragging = true;
            dragStartPosition = localPos;
            dragStartTime = clip.Start;
            
            // Calculate drag offset based on where we clicked relative to the clip's left edge
            // localPos is now in track container space, so we need to find the clip's left position
            float clipLeftEdge = parentTrack.TimelineEditor.TimeToPositionPublic(clip.Start);
            clipDragOffset = localPos.x - clipLeftEdge;
            
            SetDragVisualState(true);
        }
        
        private void HandleDrag(Vector2 localPos)
        {
            if (!isDragging || parentTrack?.TimelineEditor == null) return;
            
            // Calculate new position
            float targetX = localPos.x - clipDragOffset;
            float newStartTime = parentTrack.TimelineEditor.PositionToTimePublic(targetX);
            newStartTime = parentTrack.TimelineEditor.SnapTimePublic(newStartTime);
            newStartTime = Mathf.Max(0f, newStartTime);
            
            // Update visual position
            UpdateClipVisualTiming(newStartTime, clip.Duration);
            
            // Notify drag event
            OnClipDragged?.Invoke(this, localPos);
        }
        
        private void HandleDragEnd()
        {
            if (!isDragging) return;
            
            isDragging = false;
            SetDragVisualState(false);
            
            // Calculate final position and create command
            var timelineEditor = parentTrack?.TimelineEditor;
            if (timelineEditor != null)
            {
                var clipRect = GetComponent<RectTransform>();
                float centerPos = clipRect.anchoredPosition.x;
                float width = clipRect.sizeDelta.x;
                float leftEdge = centerPos - width / 2f;
                float finalStartTime = timelineEditor.PositionToTimePublic(leftEdge);
                
                // Only create command if position actually changed
                if (Mathf.Abs(finalStartTime - dragStartTime) > 0.001f)
                {
                    var command = new MoveClipCommand(clip, dragStartTime, finalStartTime, parentTrack);
                    timelineEditor.ExecuteCommand(command);
                }
                else
                {
                    // Restore original position
                    UpdateClipVisualTiming(dragStartTime, clip.Duration);
                }
            }
        }
        
        private void HandleResizeStart(ResizeHandle handle, Vector2 localPos)
        {
            if (!isSelected) return;
            
            isResizing = true;
            activeResizeHandle = handle;
            resizeDragStartTime = clip.Start;
            resizeDragStartDuration = clip.Duration;
            resizeStartMouseX = localPos.x;
            
            SetResizeVisualState(true, handle);
        }
        
        private void HandleResize(Vector2 localPos)
        {
            if (!isResizing || parentTrack?.TimelineEditor == null) return;
            
            float mouseDelta = localPos.x - resizeStartMouseX;
            float timeDelta = parentTrack.TimelineEditor.PositionToTimePublic(mouseDelta);
            
            if (activeResizeHandle == ResizeHandle.Left)
            {
                float newStart = resizeDragStartTime + timeDelta;
                newStart = Mathf.Max(0f, newStart);
                float originalEnd = resizeDragStartTime + resizeDragStartDuration;
                float newDuration = Mathf.Max(0.1f, originalEnd - newStart);
                
                if (parentTrack.TimelineEditor.EnableFrameSnap)
                {
                    newStart = parentTrack.TimelineEditor.SnapTimePublic(newStart);
                    newDuration = Mathf.Max(0.1f, originalEnd - newStart);
                }
                
                UpdateClipVisualStart(newStart, newDuration);
            }
            else if (activeResizeHandle == ResizeHandle.Right)
            {
                float newDuration = resizeDragStartDuration + timeDelta;
                newDuration = Mathf.Max(0.1f, newDuration);
                
                if (parentTrack.TimelineEditor.EnableFrameSnap)
                {
                    float endTime = resizeDragStartTime + newDuration;
                    endTime = parentTrack.TimelineEditor.SnapTimePublic(endTime);
                    newDuration = Mathf.Max(0.1f, endTime - resizeDragStartTime);
                }
                
                UpdateClipVisualDuration(resizeDragStartTime, newDuration);
            }
            
            OnClipResized?.Invoke(this, 0, 0); // Parameters can be adjusted as needed
        }
        
        private void HandleResizeEnd()
        {
            if (!isResizing) return;
            
            isResizing = false;
            SetResizeVisualState(false, ResizeHandle.None);
            
            var timelineEditor = parentTrack?.TimelineEditor;
            if (timelineEditor != null)
            {
                // Get current visual timing
                var clipRect = GetComponent<RectTransform>();
                float centerPos = clipRect.anchoredPosition.x;
                float width = clipRect.sizeDelta.x;
                float leftEdge = centerPos - width / 2f;
                
                float currentStartTime = timelineEditor.PositionToTimePublic(leftEdge);
                float currentDuration = timelineEditor.PositionToTimePublic(width);
                
                // Create resize command if there was significant change
                bool startChanged = Mathf.Abs(currentStartTime - resizeDragStartTime) > 0.001f;
                bool durationChanged = Mathf.Abs(currentDuration - resizeDragStartDuration) > 0.001f;
                
                if (startChanged || durationChanged)
                {
                    var command = new ResizeClipCommand(clip, resizeDragStartTime, resizeDragStartDuration,
                                                      currentStartTime, currentDuration, parentTrack);
                    timelineEditor.ExecuteCommand(command);
                }
                else
                {
                    // Restore original timing
                    UpdateClipVisualTiming(resizeDragStartTime, resizeDragStartDuration);
                }
            }
            
            activeResizeHandle = ResizeHandle.None;
        }
        
        #endregion
        
        #region IContextMenuRegisterable Implementation
        
        public string ComponentId => $"ClipUI_{clip?.Id ?? GetInstanceID().ToString()}";
        
        public IEnumerable<ContextMenuItem> GetContextMenuItems(MenuContext menuContext)
        {
            // Only provide items if this is a clip context menu for this specific clip
            if (menuContext.MenuType != "clip" || menuContext.GetTarget<ClipUI>() != this)
                yield break;

            // Edit operations
            yield return new ContextMenuItem("Cut", () => CutClip(), MenuCategory.Edit, MenuPriority.Cut, "✂️")
                .WithTooltip("Cut this clip to clipboard");
                
            yield return new ContextMenuItem("Copy", () => CopyClip(), MenuCategory.Edit, MenuPriority.Copy, "📋")
                .WithTooltip("Copy this clip to clipboard");
                
            yield return new ContextMenuItem("Delete", () => DeleteClip(), MenuCategory.Edit, MenuPriority.Delete, "🗑️")
                .WithTooltip("Delete this clip")
                .WithTextColor(Color.red);
                
            yield return new ContextMenuItem("Duplicate", () => DuplicateClip(), MenuCategory.Edit, MenuPriority.Duplicate, "📄")
                .WithTooltip("Create a copy of this clip");

            // Transform operations
            yield return new ContextMenuItem("Split at Playhead", () => SplitClipAtPlayhead(), MenuCategory.Transform, MenuPriority.Normal, "🔪")
                .WithVisibility(() => CanSplitAtPlayhead())
                .WithTooltip("Split this clip at the current playhead position");

            // Properties
            yield return new ContextMenuItem("Properties", () => ShowProperties(), MenuCategory.Properties, MenuPriority.Properties, "⚙️")
                .WithTooltip("Show clip properties and settings");

            // Debug operations (only in development)
            #if UNITY_EDITOR
            yield return new ContextMenuItem("Debug Info", () => LogDebugInfo(), MenuCategory.Debug, MenuPriority.Normal, "🐛")
                .WithTooltip("Log debug information about this clip");
            #endif
        }
        
        public void RegisterContextMenuItems()
        {
            TimelineContextMenu.RegisterMenuProvider(this);
        }
        
        public void UnregisterContextMenuItems()
        {
            TimelineContextMenu.UnregisterMenuProvider(this);
        }
        
        public bool CanProvideMenuItems(MenuContext menuContext)
        {
            return menuContext.MenuType == "clip" && menuContext.GetTarget<ClipUI>() == this;
        }

        #endregion
        
        #region Context Menu Actions
        
        private void CutClip()
        {
            Debug.Log($"Cut clip: {clip.Id}");
            // TODO: Implement cut functionality
            // Could integrate with TimelineEditorUI clipboard system
        }
        
        private void CopyClip()
        {
            Debug.Log($"Copy clip: {clip.Id}");
            // TODO: Implement copy functionality
        }
        
        private void DeleteClip()
        {
            Debug.Log($"Delete clip: {clip.Id}");
            var timelineEditor = parentTrack?.TimelineEditor;
            if (timelineEditor != null)
            {
                var command = new DeleteClipCommand(parentTrack.Track, clip, parentTrack);
                timelineEditor.ExecuteCommand(command);
            }
        }
        
        private void DuplicateClip()
        {
            Debug.Log($"Duplicate clip: {clip.Id}");
            // TODO: Implement duplicate functionality
        }
        
        private void SplitClipAtPlayhead()
        {
            Debug.Log($"Split clip at playhead: {clip.Id}");
            // TODO: Implement split functionality
        }
        
        private bool CanSplitAtPlayhead()
        {
            // TODO: Check if playhead is within clip bounds
            return true;
        }
        
        private void ShowProperties()
        {
            Debug.Log($"Show properties for clip: {clip.Id}");
            
            if (clip == null || parentTrack == null)
            {
                Debug.LogWarning("Cannot show properties: clip or parent track is null");
                return;
            }
            
            // Get track type to determine form fields
            string trackType = GetClipTrackType();
            
            // Get field definitions for this track type (same as creation form)
            var fieldDefinitions = ClipFormDefinitions.GetFieldsForTrackType(trackType);
            
            // Populate form fields with current clip data
            PopulateFormFieldsWithClipData(fieldDefinitions);
            
            string formTitle = $"Edit {ClipFormDefinitions.GetTrackTypeDisplayName(trackType)}";
            
            // Show the form for editing
            FormSubmitPanel.Instance.Show(
                formTitle,
                fieldDefinitions,
                OnClipEditFormSubmitted,
                OnClipEditFormCancelled,
                parentTrack.TimelineEditor.transform
            );
        }
        
        /// <summary>
        /// Get the track type string for this clip's parent track
        /// </summary>
        private string GetClipTrackType()
        {
            if (parentTrack?.Track == null) return "generic";
            
            // Use the same logic as TrackUI.GetTrackType()
            switch (parentTrack.Track)
            {
                case AnimTrack _:
                    return MiniTimelineConstants.TRACK_ANIM;
                case AnimatorTrack _:
                    return MiniTimelineConstants.TRACK_ANIMATOR;
                case MorphTrack _:
                    return MiniTimelineConstants.TRACK_MORPH;
                case MovementTrack _:
                    return MiniTimelineConstants.TRACK_MOVEMENT;
                case SignalTrack _:
                    return MiniTimelineConstants.TRACK_SIGNAL;
                case UmaWardrobeTrack _:
                    return MiniTimelineConstants.TRACK_UMA_WARDROBE;
                case UMAExpressionTrack _:
                    return MiniTimelineConstants.TRACK_UMA_EXPRESSION;
                default:
                    return "generic";
            }
        }
        
        /// <summary>
        /// Populate form field definitions with current clip data
        /// </summary>
        private void PopulateFormFieldsWithClipData(List<FormFieldDefinition> fieldDefinitions)
        {
            foreach (var fieldDef in fieldDefinitions)
            {
                switch (fieldDef.name)
                {
                    case "trackType":
                        // Keep the hidden track type field as is
                        break;
                        
                    case "name":
                        fieldDef.defaultValue = clip.Id;
                        break;
                        
                    case "start":
                        fieldDef.defaultValue = clip.Start;
                        break;
                        
                    case "duration":
                        fieldDef.defaultValue = clip.Duration;
                        break;
                        
                    default:
                        // Handle clip-specific properties using reflection
                        PopulateClipSpecificProperty(fieldDef);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Populate clip-specific properties using reflection
        /// </summary>
        private void PopulateClipSpecificProperty(FormFieldDefinition fieldDef)
        {
            try
            {
                var clipType = clip.GetType();
                var property = clipType.GetProperty(fieldDef.name);
                var field = clipType.GetField(fieldDef.name);
                
                if (property != null && property.CanRead)
                {
                    fieldDef.defaultValue = property.GetValue(clip);
                }
                else if (field != null)
                {
                    fieldDef.defaultValue = field.GetValue(clip);
                }
                // If property/field not found, keep the original default value
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not populate field '{fieldDef.name}': {ex.Message}");
                // Keep the original default value
            }
        }
        
        /// <summary>
        /// Handle form submission for clip editing
        /// </summary>
        private void OnClipEditFormSubmitted(Dictionary<string, object> formData)
        {
            Debug.Log($"Clip edit form submitted with {formData.Count} fields to clip {clip.Id}");
            
            try
            {
                // Create a command to update the clip properties
                var command = new EditClipCommand(clip, formData, parentTrack);
                
                if (parentTrack?.TimelineEditor != null)
                {
                    parentTrack.TimelineEditor.ExecuteCommand(command);
                }
                else
                {
                    // Fallback: apply changes directly if no command system
                    ApplyClipChangesDirectly(formData);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to update clip properties: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle form cancellation for clip editing
        /// </summary>
        private void OnClipEditFormCancelled()
        {
            Debug.Log("Clip edit cancelled");
        }
        
        /// <summary>
        /// Apply clip changes directly (fallback when no command system available)
        /// </summary>
        private void ApplyClipChangesDirectly(Dictionary<string, object> formData)
        {
            // Cast to MiniClipBase to access setters
            var clipBase = clip as MiniClipBase;
            if (clipBase == null)
            {
                Debug.LogError($"Cannot edit clip: clip {clip.GetType().Name} is not derived from MiniClipBase");
                return;
            }
            
            // Store original values for basic properties
            float originalStart = clipBase.Start;
            float originalDuration = clipBase.Duration;
            string originalId = clipBase.Id;
            
            // Apply basic properties
            if (formData.ContainsKey("name"))
            {
                clipBase.Id = formData["name"].ToString();
            }
            
            if (formData.ContainsKey("start") && float.TryParse(formData["start"].ToString(), out float newStart))
            {
                clipBase.Start = newStart;
            }
            
            if (formData.ContainsKey("duration") && float.TryParse(formData["duration"].ToString(), out float newDuration))
            {
                clipBase.Duration = Mathf.Max(0.1f, newDuration); // Ensure minimum duration
            }
            
            // Apply clip-specific properties using reflection
            ApplyClipSpecificProperties(formData);
            
            // Update visual representation
            UpdateClipAppearance();
            UpdateLayout();
            
            // If position or duration changed, rebuild track layout
            if (Math.Abs(originalStart - clipBase.Start) > 0.001f || 
                Math.Abs(originalDuration - clipBase.Duration) > 0.001f)
            {
                parentTrack?.RebuildClipUIs();
            }
            
            Debug.Log($"Applied clip changes directly: {originalId} -> {clipBase.Id}");
        }
        
        /// <summary>
        /// Apply clip-specific properties using reflection
        /// </summary>
        private void ApplyClipSpecificProperties(Dictionary<string, object> formData)
        {
            var clipType = clip.GetType();
            
            foreach (var kvp in formData)
            {
                // Skip basic properties already handled
                if (kvp.Key == "trackType" || kvp.Key == "name" || kvp.Key == "start" || kvp.Key == "duration")
                    continue;
                
                try
                {
                    var property = clipType.GetProperty(kvp.Key);
                    var field = clipType.GetField(kvp.Key);
                    
                    if (property != null && property.CanWrite)
                    {
                        // Convert value to appropriate type
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, property.PropertyType);
                        property.SetValue(clip, convertedValue);
                    }
                    else if (field != null)
                    {
                        // Convert value to appropriate type
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, field.FieldType);
                        field.SetValue(clip, convertedValue);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not set property '{kvp.Key}': {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Convert form value to the target property type
        /// </summary>
        private object ConvertValueToPropertyType(object value, System.Type targetType)
        {
            if (value == null) return null;
            
            // If already correct type, return as-is
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;
            
            // Handle common type conversions
            if (targetType == typeof(string))
                return value.ToString();
            
            if (targetType == typeof(float))
                return System.Convert.ToSingle(value);
            
            if (targetType == typeof(int))
                return System.Convert.ToInt32(value);
            
            if (targetType == typeof(bool))
                return System.Convert.ToBoolean(value);
            
            // For other types, try direct conversion
            return System.Convert.ChangeType(value, targetType);
        }
        
        #if UNITY_EDITOR
        private void LogDebugInfo()
        {
            Debug.Log($"Clip Debug Info - ID: {clip.Id}, Start: {clip.Start}, Duration: {clip.Duration}, Selected: {isSelected}");
        }
        #endif

        #endregion
    }
}