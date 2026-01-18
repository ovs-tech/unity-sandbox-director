using System;
using System.Collections;
using System.Collections.Generic;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.Commands;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;
using UnityEngine;
using UnityEngine.UIElements;

namespace Systems.MiniTimeline.UI
{
    
    /// <summary>
    /// UI Toolkit version of ClipUI - represents a timeline clip
    /// Handles visual display, selection, drag operations, and context menus
    /// Implements long press detection and resize functionality
    /// Compatible with TimelineContextMenu for comprehensive clip actions
    /// </summary>
    public class ClipUIToolkit
    {
        #region Visual Settings
        
        [Header("Visual Settings")]
        private readonly Color normalColor = new Color(0.4f, 0.6f, 0.8f, 0.8f);
        private readonly Color selectedColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
        private readonly Color hoverColor = new Color(0.5f, 0.7f, 0.9f, 0.9f);
        private readonly Color borderColor = new Color(1f, 1f, 1f, 0.8f);
        private readonly Color selectedBorderColor = new Color(1f, 0.8f, 0.2f, 1f);
        
        [Header("Long Press Settings")]
        private readonly float longPressDuration = 0.8f; // Time to trigger long press in seconds
        private readonly float longPressMoveThreshold = 20f; // Pixels
        
        #endregion
        
        #region Private Fields
        
        // Templates
        private VisualTreeAsset clipTemplate;
        private StyleSheet clipStyleSheet;
        
        // Core references
        private VisualElement clipElement;
        private TimelineEditorUIToolkit editorUI;
        private IMiniClip clip;
        private TrackUIToolkit parentTrack;
        
        // State
        private bool isSelected;
        private bool isHovered;
        private bool isDragging;
        private bool isResizing;
        private bool isMouseDown;
        private ResizeHandle activeResizeHandle = ResizeHandle.None;
        
        // Long press detection
        private bool isLongPressing = false;
        private bool longPressTriggered = false;
        private Vector2 longPressStartPosition;
        private IVisualElementScheduledItem longPressTimer;
        
        // Drag and resize data
        private Vector2 dragStartPosition;
        private float dragStartTime;
        private float dragStartDuration;
        private float clipDragOffset;
        private float currentDragNewStartTime; // Track current drag position
        private float resizeDragStartTime;
        private float resizeDragStartDuration;
        private float resizeStartMouseX;
        private float currentResizeNewStart;
        private float currentResizeNewDuration;
        
        // Layout
        private readonly float minClipWidth = 10f;
        
        #endregion
        
        #region Enums
        
        public enum ResizeHandle
        {
            None,
            Left,
            Right
        }
        
        #endregion
        
        #region UI Elements
        
        // Clip UI Elements
        private VisualElement clipHeader;
        private VisualElement clipContent;
        private VisualElement clipPreview;
        private Label clipTitle;
        private Label clipDuration;
        private Label clipDetails;
        private Label clipTypeIcon;
        private VisualElement clipResizeLeft;
        private VisualElement clipResizeRight;
        private VisualElement clipSelectionBorder;
        
        #endregion
        
        #region Events
        
        public event Action<ClipUIToolkit> OnClipSelected;
        public event Action<ClipUIToolkit> OnClipDeselected;
        public event Action<ClipUIToolkit, Vector2> OnClipDragged;
        public event Action<ClipUIToolkit, float, float> OnClipResized;
        public event Action<ClipUIToolkit> OnClipDoubleClicked;
        public event Action<ClipUIToolkit, Vector2> OnClipLongPressed;
        
        // Input blocking events
        public event Action<ClipUIToolkit> OnStartInteraction;
        public event Action<ClipUIToolkit> OnEndInteraction;
        
        #endregion
        
        #region Properties
        
        public IMiniClip Clip => clip;
        public TrackUIToolkit ParentTrack => parentTrack;
        public bool IsSelected => isSelected;
        public bool IsDragging => isDragging;
        public bool IsLongPressing => isLongPressing;
        public VisualElement ClipElement => clipElement;
        
        #endregion

        public void Initialize(TimelineEditorUIToolkit editorUI, IMiniClip clip, VisualElement parentContainer, 
                              VisualTreeAsset clipTemplate = null, StyleSheet clipStyleSheet = null, 
                              TrackUIToolkit parentTrack = null)
        {
            this.editorUI = editorUI;
            this.clip = clip;
            this.clipTemplate = clipTemplate;
            this.clipStyleSheet = clipStyleSheet;
            this.parentTrack = parentTrack;
            
            LoadClipTemplateIfNeeded();
            CreateClipElement();
            SetupClipData();
            SetupEventHandlers();
            
            parentContainer.Add(clipElement);
            UpdatePosition();
        }
        
        private void LoadClipTemplateIfNeeded()
        {
            // Try to load clip template from Resources if not provided
            if (clipTemplate == null)
            {
                clipTemplate = Resources.Load<VisualTreeAsset>("UI/ClipUIToolkit");
            }
            
            if (clipStyleSheet == null)
            {
                clipStyleSheet = Resources.Load<StyleSheet>("UI/ClipUIToolkit");
            }
        }

        private void CreateClipElement()
        {
            if (clipTemplate != null)
            {
                // Clone from template
                clipElement = clipTemplate.CloneTree();
            }
            else
            {
                // Fallback: create manually
                clipElement = CreateClipElementManually();
            }
            
            // Apply styling
            if (clipStyleSheet != null)
            {
                clipElement.styleSheets.Add(clipStyleSheet);
            }
            
            // Set clip identification
            clipElement.name = $"clip-{clip.Id}";
            clipElement.AddToClassList("clip");
            
            // Query UI elements
            QueryClipElements();
        }

        private VisualElement CreateClipElementManually()
        {
            var root = new VisualElement();
            root.AddToClassList("clip");
            
            // Clip header
            clipHeader = new VisualElement();
            clipHeader.AddToClassList("clip-header");
            
            // Clip icon
            var clipIcon = new VisualElement();
            clipIcon.AddToClassList("clip-icon");
            
            clipTypeIcon = new Label("🎭");
            clipTypeIcon.AddToClassList("clip-type-icon");
            clipIcon.Add(clipTypeIcon);
            clipHeader.Add(clipIcon);
            
            // Clip title
            clipTitle = new Label("Clip Name");
            clipTitle.AddToClassList("clip-title");
            clipHeader.Add(clipTitle);
            
            root.Add(clipHeader);
            
            // Clip content
            clipContent = new VisualElement();
            clipContent.AddToClassList("clip-content");
            
            // Clip preview
            clipPreview = new VisualElement();
            clipPreview.AddToClassList("clip-preview");
            clipContent.Add(clipPreview);
            
            // Clip info
            var clipInfo = new VisualElement();
            clipInfo.AddToClassList("clip-info");
            
            clipDuration = new Label("2.5s");
            clipDuration.AddToClassList("clip-duration");
            clipInfo.Add(clipDuration);
            
            clipDetails = new Label("Details");
            clipDetails.AddToClassList("clip-details");
            clipInfo.Add(clipDetails);
            
            clipContent.Add(clipInfo);
            root.Add(clipContent);
            
            // Resize handles
            var resizeHandles = new VisualElement();
            resizeHandles.AddToClassList("clip-resize-handles");
            
            clipResizeLeft = new VisualElement();
            clipResizeLeft.AddToClassList("clip-resize-handle");
            clipResizeLeft.AddToClassList("clip-resize-left");
            resizeHandles.Add(clipResizeLeft);
            
            clipResizeRight = new VisualElement();
            clipResizeRight.AddToClassList("clip-resize-handle");
            clipResizeRight.AddToClassList("clip-resize-right");
            resizeHandles.Add(clipResizeRight);
            
            root.Add(resizeHandles);
            
            // Selection border
            clipSelectionBorder = new VisualElement();
            clipSelectionBorder.AddToClassList("clip-selection-border");
            root.Add(clipSelectionBorder);
            
            return root;
        }

        private void QueryClipElements()
        {
            // Query clip elements from the created structure
            clipHeader = clipElement.Q("clip-header");
            clipContent = clipElement.Q("clip-content");
            clipPreview = clipElement.Q("clip-preview");
            
            clipTitle = clipElement.Q<Label>("clip-title");
            clipDuration = clipElement.Q<Label>("clip-duration");
            clipDetails = clipElement.Q<Label>("clip-details");
            clipTypeIcon = clipElement.Q<Label>("clip-type-icon");
            
            clipResizeLeft = clipElement.Q("clip-resize-left");
            clipResizeRight = clipElement.Q("clip-resize-right");
            clipSelectionBorder = clipElement.Q("clip-selection-border");
        }

        private void SetupClipData()
        {
            if (clip == null) return;
            
            // Set clip information
            if (clipTitle != null)
                clipTitle.text = GetClipDisplayName();
            
            if (clipDuration != null)
                clipDuration.text = $"{clip.Duration:F1}s";
            
            if (clipDetails != null)
                clipDetails.text = GetClipDetails();
            
            // Set clip type icon and styling
            string clipType = GetClipTypeName();
            clipElement?.AddToClassList(clipType);
            
            if (clipTypeIcon != null)
                clipTypeIcon.text = GetClipTypeIcon(clipType);
            
            // Update initial colors
            UpdateColors();
        }

        /// <summary>
        /// Update clip appearance including colors and text
        /// </summary>
        public void UpdateClipAppearance()
        {
            if (clip == null) return;
            
            // Update label
            if (clipTitle != null)
            {
                clipTitle.text = GetClipDisplayName();
            }
            
            if (clipDuration != null)
            {
                clipDuration.text = $"{clip.Duration:F1}s";
            }
            
            if (clipDetails != null)
            {
                clipDetails.text = GetClipDetails();
            }
            
            // Update colors based on state
            UpdateColors();
        }
        
        /// <summary>
        /// Update clip colors based on current state (selected, hovered, etc.)
        /// </summary>
        private void UpdateColors()
        {
            if (clipElement == null) return;
            
            // Remove existing state classes
            clipElement.RemoveFromClassList("selected");
            clipElement.RemoveFromClassList("hovered");
            clipElement.RemoveFromClassList("normal");
            
            // Add appropriate state class
            if (isSelected)
            {
                clipElement.AddToClassList("selected");
            }
            else if (isHovered)
            {
                clipElement.AddToClassList("hovered");
            }
            else
            {
                clipElement.AddToClassList("normal");
            }
        }

        private void SetupEventHandlers()
        {
            if (clipElement == null) return;
            
            // Main clip interaction with long press support
            clipElement.RegisterCallback<MouseDownEvent>(OnClipMouseDown);
            clipElement.RegisterCallback<MouseMoveEvent>(OnClipMouseMove);
            clipElement.RegisterCallback<MouseUpEvent>(OnClipMouseUp);
            clipElement.RegisterCallback<MouseEnterEvent>(OnClipMouseEnter);
            clipElement.RegisterCallback<MouseLeaveEvent>(OnClipMouseLeave);
            
            // Resize handles
            if (clipResizeLeft != null)
            {
                clipResizeLeft.RegisterCallback<MouseDownEvent>(OnResizeLeftMouseDown);
            }
            
            if (clipResizeRight != null)
            {
                clipResizeRight.RegisterCallback<MouseDownEvent>(OnResizeRightMouseDown);
            }
        }

        private void OnClipMouseEnter(MouseEnterEvent evt)
        {
            SetHoverState(true);
        }

        private void OnClipMouseLeave(MouseLeaveEvent evt)
        {
            SetHoverState(false);
        }

        private void SetHoverState(bool hovered)
        {
            isHovered = hovered;
            UpdateColors();
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

        private string GetClipDetails()
        {
            return $"Start: {clip.Start:F1}s";
        }

        private string GetClipTypeName()
        {
            // Determine clip type based on clip class
            string typeName = clip.GetType().Name.ToLower();
            if (typeName.Contains("anim")) return "anim";
            if (typeName.Contains("audio")) return "audio";
            if (typeName.Contains("camera")) return "camera";
            return "custom";
        }

        private string GetClipTypeIcon(string clipType)
        {
            switch (clipType)
            {
                case "anim": return "🎭";
                case "audio": return "🔊";
                case "camera": return "📷";
                case "custom": return "⚙️";
                default: return "📄";
            }
        }

        private void OnClipMouseDown(MouseDownEvent evt)
        {
            // Debug.Log($"[ClipUI] MouseDown on clip {clip?.Id}, button: {evt.button}");
            
            if (evt.button == 0) // Left mouse button
            {
                // Start long press detection
                StartLongPressDetection(evt.mousePosition);
                
                // Check if we're clicking on a resize handle
                var resizeHandle = GetResizeHandleAtPosition(evt.localMousePosition);
                if (resizeHandle != ResizeHandle.None && isSelected)
                {
                    // Debug.Log($"[ClipUI] Starting resize with handle: {resizeHandle}");
                    // Stop long press detection for resize handles
                    StopLongPressDetection();
                    // Start resize operation
                    HandleResizeStart(resizeHandle, evt.localMousePosition);
                    
                    // Capture mouse to continue receiving events during resize
                    clipElement?.CaptureMouse();
                    evt.StopPropagation();
                    return;
                }
                
                // Debug.Log($"[ClipUI] Preparing for drag, selected: {isSelected}");
                
                // Handle clip selection
                HandleClipClick();
                
                dragStartPosition = evt.localMousePosition;
                isDragging = false; // Will be set to true if mouse moves
                isMouseDown = true;
                
                // Capture mouse to continue receiving events during drag
                clipElement?.CaptureMouse();
                // Debug.Log($"[ClipUI] Mouse captured: {(clipElement?.panel != null)}");
                
                // Notify interaction start
                OnStartInteraction?.Invoke(this);
                evt.StopPropagation();
            }
            else if (evt.button == 1) // Right mouse button
            {
                // Show context menu immediately for right click
                ShowClipContextMenu(evt.mousePosition, false);
                evt.StopPropagation();
            }
        }

        private void OnClipMouseMove(MouseMoveEvent evt)
        {
            // Debug.Log($"[ClipUI] MouseMove: isMouseDown={isMouseDown}, isDragging={isDragging}, isResizing={isResizing}");
            
            // Check if we should cancel long press due to movement
            if (isLongPressing && !IsWithinLongPressThreshold(evt.mousePosition))
            {
                StopLongPressDetection();
            }
            
            // Only check for drag start if mouse is down and not already dragging/resizing
            if (isMouseDown && !isDragging && !isResizing && Vector2.Distance(evt.localMousePosition, dragStartPosition) > 5f)
            {
                Debug.Log($"[ClipUI] Starting drag, distance: {Vector2.Distance(evt.localMousePosition, dragStartPosition)}");
                // Stop long press when drag begins
                StopLongPressDetection();
                
                isDragging = true;
                clipElement?.AddToClassList("dragging");
                HandleDragStart(evt.localMousePosition);
            }
            
            if (isDragging)
            {
                HandleDrag(evt.mousePosition);
            }
            else if (isResizing)
            {
                HandleResize(evt.mousePosition);
            }
        }

        private void OnClipMouseUp(MouseUpEvent evt)
        {
            Debug.Log($"[ClipUI] MouseUp: isDragging={isDragging}, isResizing={isResizing}");
            
            // Release mouse capture
            if (clipElement?.panel != null)
            {
                clipElement.ReleaseMouse();
            }
            
            // Reset mouse down state
            isMouseDown = false;
            
            // If long press was triggered, prevent normal click handling
            if (longPressTriggered)
            {
                longPressTriggered = false;
            }
            
            // Stop long press detection
            StopLongPressDetection();
            
            if (isDragging)
            {
                HandleDragEnd();
                isDragging = false;
                clipElement?.RemoveFromClassList("dragging");
                if (clipElement != null)
                {
                    clipElement.style.opacity = 1f;
                }
                OnEndInteraction?.Invoke(this);
            }
            else if (isResizing)
            {
                HandleResizeEnd();
                OnEndInteraction?.Invoke(this);
            }
        }

        #region Long Press Detection
        
        /// <summary>
        /// Start long press detection
        /// </summary>
        private void StartLongPressDetection(Vector2 screenPosition)
        {
            // Stop any existing long press detection
            StopLongPressDetection();
            
            isLongPressing = true;
            longPressTriggered = false;
            longPressStartPosition = screenPosition;
            
            // Start long press timer
            longPressTimer = clipElement?.schedule.Execute(() =>
            {
                if (isLongPressing)
                {
                    HandleLongPress();
                }
            }).StartingIn((long)(longPressDuration * 1000)); // Convert to milliseconds
        }
        
        /// <summary>
        /// Stop long press detection
        /// </summary>
        private void StopLongPressDetection()
        {
            longPressTimer?.Pause();
            longPressTimer = null;
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
        /// Handle long press event
        /// </summary>
        private void HandleLongPress()
        {
            longPressTriggered = true;
            isLongPressing = false;
            
            // Debug.Log($"Long press detected on clip: {clip?.Id}");
            
            // Trigger the long press event
            OnClipLongPressed?.Invoke(this, longPressStartPosition);
            
            // Show context menu
            ShowClipContextMenu(longPressStartPosition, true);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleClipClick()
        {
            // Handle selection
            SetSelected(!isSelected);
            
            if (isSelected)
            {
                OnClipSelected?.Invoke(this);
            }
            else
            {
                OnClipDeselected?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Show context menu for this clip using TimelineEditorUIToolkit
        /// </summary>
        private void ShowClipContextMenu(Vector2 screenPosition, bool fromLongPress = false)
        {
            if (editorUI != null)
            {
                editorUI.ShowClipMenu(this, screenPosition);
            }
            else
            {
                Debug.LogWarning("TimelineEditorUIToolkit not available");
            }
        }
        
        /// <summary>
        /// Creates form fields for clip context menu actions
        /// </summary>
        private List<FormFieldDefinition> CreateClipActionFields()
        {
            var fields = new List<FormFieldDefinition>();
            
            // Delete Clip button
            var deleteField = new FormFieldDefinition("delete", "Delete Clip", "button");
            deleteField.options = new Dictionary<string, object> { ["action"] = "delete" };
            fields.Add(deleteField);
            
            // Duplicate Clip button
            var duplicateField = new FormFieldDefinition("duplicate", "Duplicate Clip", "button");
            duplicateField.options = new Dictionary<string, object> { ["action"] = "duplicate" };
            fields.Add(duplicateField);
            
            // Split Clip button
            var splitField = new FormFieldDefinition("split", "Split at Playhead", "button");
            splitField.options = new Dictionary<string, object> { ["action"] = "split" };
            fields.Add(splitField);
            
            // Cut Clip button
            var cutField = new FormFieldDefinition("cut", "Cut Clip", "button");
            cutField.options = new Dictionary<string, object> { ["action"] = "cut" };
            fields.Add(cutField);
            
            // Copy Clip button
            var copyField = new FormFieldDefinition("copy", "Copy Clip", "button");
            copyField.options = new Dictionary<string, object> { ["action"] = "copy" };
            fields.Add(copyField);
            
            // Clip Properties button
            var propertiesField = new FormFieldDefinition("properties", "Properties", "button");
            propertiesField.options = new Dictionary<string, object> { ["action"] = "properties" };
            fields.Add(propertiesField);
            
            return fields;
        }
        
        /// <summary>
        /// Handle clip action form submission
        /// </summary>
        private void OnClipActionSubmitted(Dictionary<string, object> formData)
        {
            if (formData.TryGetValue("action", out var action))
            {
                HandleClipAction(action.ToString());
            }
        }
        
        /// <summary>
        /// Handle clip action form cancellation
        /// </summary>
        private void OnClipActionCancelled()
        {
            // Debug.Log("Clip action cancelled");
        }
        
        /// <summary>
        /// Execute the selected clip action
        /// </summary>
        private void HandleClipAction(string action)
        {
            switch (action)
            {
                case "delete":
                    ShowDeleteClipConfirmation();
                    break;
                case "duplicate":
                    DuplicateClip();
                    break;
                case "split":
                    SplitClipAtPlayhead();
                    break;
                case "cut":
                    CutClip();
                    break;
                case "copy":
                    CopyClip();
                    break;
                case "properties":
                    ShowClipProperties();
                    break;
                default:
                    Debug.LogWarning($"Unknown clip action: {action}");
                    break;
            }
        }
        
        #region Clip Action Implementations
        
        private void ShowDeleteClipConfirmation()
        {
            var fields = new List<FormFieldDefinition>();
            
            var messageField = new FormFieldDefinition("message", "Confirmation", "textarea");
            messageField.defaultValue = $"Are you sure you want to delete '{GetClipDisplayName()}'?\n\nThis action cannot be undone.";
            messageField.options = new Dictionary<string, object> { ["readonly"] = true };
            fields.Add(messageField);
            
            var confirmField = new FormFieldDefinition("confirm", "Type 'DELETE' to confirm", "text");
            confirmField.required = true;
            confirmField.placeholder = "DELETE";
            fields.Add(confirmField);
            
            if (FormSubmitPanelUIToolkit.HasInstance)
            {
                FormSubmitPanelUIToolkit.Instance.Show(
                    "⚠️ Delete Clip",
                    fields,
                    onSubmit: OnDeleteClipSubmitted,
                    onCancel: () => { /* Debug.Log("Delete clip cancelled"); */ },
                    editorUI?.transform
                );
            }
        }
        
        private void OnDeleteClipSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                string confirmation = formData["confirm"].ToString();
                if (confirmation == "DELETE")
                {
                    // Debug.Log("Clip deletion confirmed");
                    // TODO: Execute clip deletion command
                }
                else
                {
                    Debug.LogWarning("Clip deletion cancelled: confirmation text does not match");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to process clip deletion: {ex.Message}");
            }
        }
        
        private void DuplicateClip()
        {
            // Debug.Log($"Duplicating clip: {clip?.Id}");
            // TODO: Implement clip duplication
        }
        
        private void SplitClipAtPlayhead()
        {
            // Debug.Log($"Splitting clip at playhead: {clip?.Id}");
            // TODO: Implement clip splitting
        }
        
        private void CutClip()
        {
            // Debug.Log($"Cutting clip: {clip?.Id}");
            // TODO: Implement cut to clipboard
        }
        
        private void CopyClip()
        {
            // Debug.Log($"Copying clip: {clip?.Id}");
            // TODO: Implement copy to clipboard
        }
        
        private void ShowClipProperties()
        {
            var fields = new List<FormFieldDefinition>();
            
            // Clip name
            var nameField = new FormFieldDefinition("name", "Clip Name", "text");
            nameField.defaultValue = clip?.Id ?? "";
            nameField.required = true;
            fields.Add(nameField);
            
            // Start time
            var startField = new FormFieldDefinition("start", "Start Time", "number");
            startField.defaultValue = clip?.Start ?? 0f;
            startField.required = true;
            fields.Add(startField);
            
            // Duration
            var durationField = new FormFieldDefinition("duration", "Duration", "number");
            durationField.defaultValue = clip?.Duration ?? 1f;
            durationField.required = true;
            fields.Add(durationField);
            
            if (FormSubmitPanelUIToolkit.HasInstance)
            {
                FormSubmitPanelUIToolkit.Instance.Show(
                    "Clip Properties",
                    fields,
                    onSubmit: OnClipPropertiesSubmitted,
                    onCancel: () => { /* Debug.Log("Clip properties cancelled"); */ },
                    editorUI?.transform
                );
            }
        }
        
        private void OnClipPropertiesSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                if (clip != null)
                {
                    string newName = formData["name"].ToString();
                    float newStart = Convert.ToSingle(formData["start"]);
                    float newDuration = Convert.ToSingle(formData["duration"]);
                    
                    // Debug.Log($"Updating clip properties: {newName}, Start: {newStart}, Duration: {newDuration}");
                    // TODO: Apply clip property changes through command system
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply clip properties: {ex.Message}");
            }
        }
        
        #endregion
        
        #endregion
        
        #region Drag and Resize Handling
        
        private void HandleDragStart(Vector2 localPos)
        {
            if (!isSelected) HandleClipClick();
            
            dragStartTime = clip.Start;
            dragStartDuration = clip.Duration;
            
            // Initialize current drag position to original position
            currentDragNewStartTime = dragStartTime;
            
            Debug.Log($"[ClipUI] HandleDragStart: dragStartTime={dragStartTime}, localPos={localPos}");
            
            // Calculate drag offset based on where we clicked relative to the clip's left edge
            clipDragOffset = localPos.x;
            
            // Visual feedback
            clipElement?.AddToClassList("dragging");
            if (clipElement != null)
            {
                clipElement.style.opacity = 0.8f;
            }
        }
        
        private void HandleDrag(Vector2 mousePosition)
        {
            if (!isDragging || editorUI == null || clipElement == null) return;
            
            // Convert mouse position to clips container local space
            var clipsContainer = clipElement.parent;
            if (clipsContainer == null)
            {
                Debug.LogWarning("[ClipUI] Clips container is null, cannot drag");
                return;
            }
            
            Vector2 localPos = clipsContainer.WorldToLocal(mousePosition);
            
            // Calculate new position based on drag
            float targetX = localPos.x - clipDragOffset;
            float newStartTime = editorUI.PositionToTimePublic(targetX);
            newStartTime = editorUI.SnapTimePublic(newStartTime);
            newStartTime = Mathf.Max(0f, newStartTime);
            
            // Store current drag position for command execution
            currentDragNewStartTime = newStartTime;
            
            // Update visual position
            UpdateClipVisualTiming(newStartTime, clip.Duration);
            
            // Notify drag event
            OnClipDragged?.Invoke(this, mousePosition);
        }
        
        private void HandleDragEnd()
        {
            if (!isDragging) return;
            
            isDragging = false;
            
            // Use the stored current drag position
            if (editorUI != null && parentTrack != null)
            {
                float finalStartTime = currentDragNewStartTime;
                
                Debug.Log($"[ClipUI] HandleDragEnd: editorUI={editorUI != null}, parentTrack={parentTrack != null}, dragStartTime={dragStartTime}, finalStartTime={finalStartTime}, delta={Mathf.Abs(finalStartTime - dragStartTime)}");
                
                // Only create command if position actually changed
                if (Mathf.Abs(finalStartTime - dragStartTime) > 0.001f)
                {
                    Debug.Log($"[ClipUI] Creating MoveClipCommand: {dragStartTime} → {finalStartTime}");
                    
                    // Create and execute move command for undo/redo support
                    var moveCommand = new MoveClipCommand(
                        clip,
                        dragStartTime,
                        finalStartTime
                    );
                    
                    editorUI.ExecuteCommand(moveCommand);
                    
                    Debug.Log($"[ClipUI] Move command executed successfully");
                }
                else
                {
                    Debug.Log($"[ClipUI] No significant movement detected, restoring original position");
                    // Restore original position if no significant change
                    UpdateClipVisualTiming(dragStartTime, dragStartDuration);
                }
            }
            else
            {
                Debug.Log($"[ClipUI] Command not created - editorUI={editorUI != null}, parentTrack={parentTrack != null}");
            }
        }
        
        /// <summary>
        /// Get the resize handle at a specific local position (in clip's local space)
        /// </summary>
        private ResizeHandle GetResizeHandleAtPosition(Vector2 localPosition)
        {
            if (!isSelected || clipElement == null) 
            {
                // Debug.Log($"[ClipUI] GetResizeHandleAtPosition: not selected or no element");
                return ResizeHandle.None;
            }
            
            // Get the clip's content rect for bounds checking
            Rect clipRect = clipElement.contentRect;
            float clipWidth = clipRect.width;
            
            // Debug.Log($"[ClipUI] GetResizeHandleAtPosition: localPos={localPosition}, clipRect={clipRect}, clipWidth={clipWidth}");
            
            // Define edge zones - larger hit area for better UX
            float edgeZoneWidth = 15f; // Pixels from edge
            
            // Check left edge
            if (localPosition.x >= clipRect.xMin && localPosition.x <= clipRect.xMin + edgeZoneWidth)
            {
                // Debug.Log($"[ClipUI] Detected LEFT resize handle at x={localPosition.x}");
                return ResizeHandle.Left;
            }
            
            // Check right edge
            if (localPosition.x >= clipRect.xMax - edgeZoneWidth && localPosition.x <= clipRect.xMax)
            {
                // Debug.Log($"[ClipUI] Detected RIGHT resize handle at x={localPosition.x}");
                return ResizeHandle.Right;
            }
            
            // Debug.Log($"[ClipUI] No resize handle detected at position");
            return ResizeHandle.None;
        }
        
        private void HandleResizeStart(ResizeHandle handle, Vector2 localPos)
        {
            if (!isSelected || clipElement == null) return;
            
            try
            {
                // Convert local position to clips container space
                var clipsContainer = clipElement.parent;
                if (clipsContainer == null)
                {
                    Debug.LogWarning("[ClipUI] Cannot start resize: clips container is null");
                    return;
                }
                
                Vector2 containerLocalPos = clipsContainer.WorldToLocal(clipElement.LocalToWorld(localPos));
                
                isResizing = true;
                activeResizeHandle = handle;
                resizeDragStartTime = clip.Start;
                resizeDragStartDuration = clip.Duration;
                resizeStartMouseX = containerLocalPos.x;
                
                // Initialize current resize values to original clip values
                currentResizeNewStart = resizeDragStartTime;
                currentResizeNewDuration = resizeDragStartDuration;
                
                // Debug.Log($"[ClipUI] Resize started: handle={handle}, startTime={resizeDragStartTime}, duration={resizeDragStartDuration}, mouseX={resizeStartMouseX}");
                
                // Visual feedback
                clipElement?.AddToClassList("resizing");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ClipUI] Error in HandleResizeStart: {ex.Message}\n{ex.StackTrace}");
                isResizing = false;
                activeResizeHandle = ResizeHandle.None;
            }
        }
        
        private void HandleResize(Vector2 mousePosition)
        {
            if (!isResizing || editorUI == null || clipElement == null) return;
            
            // Debug.Log($"[ClipUI] HandleResize: handle={activeResizeHandle}, mousePos={mousePosition}");
            
            // Convert mouse position to clips container local space
            var clipsContainer = clipElement.parent;
            if (clipsContainer == null) return;
            
            Vector2 localPos = clipsContainer.WorldToLocal(mousePosition);
            // Debug.Log($"[ClipUI] HandleResize localPos: {localPos}, resizeStartMouseX: {resizeStartMouseX}");
            
            float mouseDelta = localPos.x - resizeStartMouseX;
            float timeDelta = editorUI.PositionToTimePublic(mouseDelta);
            
            // Debug.Log($"[ClipUI] HandleResize mouseDelta: {mouseDelta}, timeDelta: {timeDelta}");
            
            if (activeResizeHandle == ResizeHandle.Left)
            {
                float newStart = resizeDragStartTime + timeDelta;
                newStart = Mathf.Max(0f, newStart);
                float originalEnd = resizeDragStartTime + resizeDragStartDuration;
                float newDuration = Mathf.Max(0.1f, originalEnd - newStart);
                
                newStart = editorUI.SnapTimePublic(newStart);
                newDuration = Mathf.Max(0.1f, originalEnd - newStart);
                
                Debug.Log($"[ClipUI] Resizing LEFT: newStart={newStart}, newDuration={newDuration}");
                
                // Store for command execution on mouse up
                currentResizeNewStart = newStart;
                currentResizeNewDuration = newDuration;
                
                // Update visual only (don't modify clip data yet)
                UpdateClipVisualStart(newStart, newDuration);
            }
            else if (activeResizeHandle == ResizeHandle.Right)
            {
                float newDuration = resizeDragStartDuration + timeDelta;
                newDuration = Mathf.Max(0.1f, newDuration);
                
                float endTime = resizeDragStartTime + newDuration;
                endTime = editorUI.SnapTimePublic(endTime);
                newDuration = Mathf.Max(0.1f, endTime - resizeDragStartTime);
                
                Debug.Log($"[ClipUI] Resizing RIGHT: newDuration={newDuration}");
                
                // Store for command execution on mouse up
                currentResizeNewStart = resizeDragStartTime;
                currentResizeNewDuration = newDuration;
                
                // Update visual only (don't modify clip data yet)
                UpdateClipVisualDuration(resizeDragStartTime, newDuration);
            }
            
                OnClipResized?.Invoke(this, 0, 0);
        }
        
        private void HandleResizeEnd()
        {
            if (!isResizing) return;
            
            isResizing = false;
            ResizeHandle endedHandle = activeResizeHandle;
            activeResizeHandle = ResizeHandle.None;
            
            // Remove visual feedback
            clipElement?.RemoveFromClassList("resizing");
            
            // Check if there was a significant change
            bool hasChanged = Mathf.Abs(currentResizeNewStart - resizeDragStartTime) > 0.01f ||
                            Mathf.Abs(currentResizeNewDuration - resizeDragStartDuration) > 0.01f;
            
            Debug.Log($"[ClipUI] HandleResizeEnd: hasChanged={hasChanged}, editorUI={editorUI != null}, parentTrack={parentTrack != null}, startDelta={Mathf.Abs(currentResizeNewStart - resizeDragStartTime)}, durationDelta={Mathf.Abs(currentResizeNewDuration - resizeDragStartDuration)}");
            
            if (hasChanged && editorUI != null && parentTrack != null)
            {
                Debug.Log($"[ClipUI] Creating ResizeClipCommand: ({resizeDragStartTime}, {resizeDragStartDuration}) → ({currentResizeNewStart}, {currentResizeNewDuration})");
                
                // Create and execute resize command for undo/redo support
                var resizeCommand = new ResizeClipCommand(
                    clip,
                    resizeDragStartTime,
                    resizeDragStartDuration,
                    currentResizeNewStart,
                    currentResizeNewDuration
                );
                
                editorUI.ExecuteCommand(resizeCommand);
                
                Debug.Log($"[ClipUI] Resize command executed successfully");
            }
            else
            {
                Debug.Log($"[ClipUI] Command not created - hasChanged={hasChanged}, editorUI={editorUI != null}, parentTrack={parentTrack != null}");
            }
        }
        
        
        #endregion
        
        #region Visual Updates
        
        /// <summary>
        /// Update clip visual timing (position and size)
        /// </summary>
        private void UpdateClipVisualTiming(float newStart, float newDuration)
        {
            if (editorUI == null) return;
            
            float startPos = editorUI.TimeToPositionPublic(newStart);
            float width = editorUI.TimeToPositionPublic(newDuration);
            
            // Ensure minimum width for usability
            width = Mathf.Max(width, minClipWidth);
            
            // For marker clips, use a fixed small width
            if (newDuration <= 0.001f)
            {
                width = 20f;
            }
            
            clipElement.style.left = startPos;
            clipElement.style.width = width;
        }
        
        /// <summary>
        /// Update only the left edge (start time) - used during left handle resize
        /// </summary>
        private void UpdateClipVisualStart(float newStart, float duration)
        {
            UpdateClipVisualTiming(newStart, duration);
        }
        
        /// <summary>
        /// Update only the right edge (duration) - used during right handle resize
        /// </summary>
        private void UpdateClipVisualDuration(float startTime, float newDuration)
        {
            UpdateClipVisualTiming(startTime, newDuration);
        }
        
        #endregion

        private void OnResizeLeftMouseDown(MouseDownEvent evt)
        {
            // Debug.Log($"[ClipUI] OnResizeLeftMouseDown called on {clip?.Id}");
            
            if (evt.button != 0) return; // Only left mouse button
            
            // Capture mouse on clipElement (where MouseMove/MouseUp are registered)
            clipElement.CaptureMouse();
            // Debug.Log($"[ClipUI] Mouse captured for LEFT resize: {clipElement.HasMouseCapture()}");
            
            // Start resize operation using the proper handler
            HandleResizeStart(ResizeHandle.Left, evt.localMousePosition);
            
            // Stop propagation so clip doesn't also receive the event
            evt.StopPropagation();
        }

        private void OnResizeRightMouseDown(MouseDownEvent evt)
        {
            // Debug.Log($"[ClipUI] OnResizeRightMouseDown called on {clip?.Id}");
            
            if (evt.button != 0) return; // Only left mouse button
            
            // Capture mouse on clipElement (where MouseMove/MouseUp are registered)
            clipElement.CaptureMouse();
            // Debug.Log($"[ClipUI] Mouse captured for RIGHT resize: {clipElement.HasMouseCapture()}");
            
            // Start resize operation using the proper handler
            HandleResizeStart(ResizeHandle.Right, evt.localMousePosition);
            
            // Stop propagation so clip doesn't also receive the event
            evt.StopPropagation();
        }

        public void UpdatePosition()
        {
            if (clipElement == null || clip == null) return;
            
            float startPos = editorUI.TimeToPositionPublic(clip.Start);
            float width = editorUI.TimeToPositionPublic(clip.Duration);
            
            // Ensure minimum width for usability
            width = Mathf.Max(width, 20f);
            
            clipElement.style.left = startPos;
            clipElement.style.width = width;
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            
            isSelected = selected;
            UpdateColors();
            
            // Show/hide resize handles based on selection
            SetResizeHandlesVisible(selected);
            
            if (selected)
            {
                OnClipSelected?.Invoke(this);
            }
            else
            {
                OnClipDeselected?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Show or hide resize handles
        /// </summary>
        private void SetResizeHandlesVisible(bool visible)
        {
            if (clipResizeLeft != null)
            {
                clipResizeLeft.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (clipResizeRight != null)
            {
                clipResizeRight.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Update layout and rebuild clip appearance
        /// Called when clip data changes
        /// </summary>
        public void RebuildClip()
        {
            UpdateClipAppearance();
            UpdatePosition();
        }
        
        /// <summary>
        /// Properties for TimelineContextMenu compatibility
        /// </summary>
        public IMiniClip ClipForContextMenu => clip;
        public TimelineEditorUIToolkit EditorForContextMenu => editorUI;
        public TrackUIToolkit ParentTrackForContextMenu => parentTrack;

        public void Dispose()
        {
            // Remove event handlers
            if (clipElement != null)
            {
                clipElement.UnregisterCallback<MouseDownEvent>(OnClipMouseDown);
                clipElement.UnregisterCallback<MouseMoveEvent>(OnClipMouseMove);
                clipElement.UnregisterCallback<MouseUpEvent>(OnClipMouseUp);
            }
            
            if (clipResizeLeft != null)
                clipResizeLeft.UnregisterCallback<MouseDownEvent>(OnResizeLeftMouseDown);
            
            if (clipResizeRight != null)
                clipResizeRight.UnregisterCallback<MouseDownEvent>(OnResizeRightMouseDown);
            
            // Remove from hierarchy
            clipElement?.RemoveFromHierarchy();
        }
    }

}