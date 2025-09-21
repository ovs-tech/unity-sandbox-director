using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MiniTimeline.Core;
using MiniTimeline.UI.Commands;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Main in-game timeline editor UI using Unity's new Input System
    /// Supports mobile gestures: tap, drag, long-press, pinch zoom
    /// </summary>
    public class TimelineEditorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas editorCanvas;
        [SerializeField] private RectTransform timelineContainer;
        [SerializeField] private RectTransform rulerContainer;
        [SerializeField] private RectTransform tracksContainer;
        [SerializeField] private ScrollRect timelineScrollRect;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private Text timeText;

        [Header("Editor Settings")]
        [SerializeField] private float pixelsPerSecond = 100f;
        [SerializeField] private float minZoom = 0.1f;
        [SerializeField] private float maxZoom = 5f;
        [SerializeField] private float snapThreshold = 0.1f;
        [SerializeField] private bool enableFrameSnap = true;

        [Header("Prefabs")]
        [SerializeField] private GameObject trackUIPrefab;
        [SerializeField] private GameObject clipUIPrefab;
        [SerializeField] private GameObject rulerMarkerPrefab;

        // Input System References
        [Header("Input Actions")]
        [SerializeField] private InputActionReference primaryTouchAction;
        [SerializeField] private InputActionReference secondaryTouchAction;
        [SerializeField] private InputActionReference touchPositionAction;
        [SerializeField] private InputActionReference secondaryTouchPositionAction;
        [SerializeField] private InputActionReference undoAction;
        [SerializeField] private InputActionReference redoAction;

        // Core references
        private MiniTimelineDirector director;
        private TimelineCommandManager commandManager;

        // UI State
        private float currentZoom = 1f;
        private float timelineWidth;
        private bool isDragging = false;
        private bool isPlayheadDragging = false;
        private Vector2 lastTouchPosition;

        // Selection and editing
        private List<ClipUI> selectedClips = new List<ClipUI>();
        private ClipUI clipBeingDragged;
        private float clipDragStartTime;
        private float clipDragCurrentTime; // Track current drag position
        private Vector2 clipDragStartLocalPos; // Track where we started dragging
        private float clipDragOffset; // Offset between click position and clip start
        private bool enableMergeForDragging = true;
        private MoveClipCommand activeDragCommand;
        private TimelineRuler ruler;

        // Resize handling
        private bool isResizing = false;
        private ClipUI clipBeingResized;
        private ClipUI.ResizeHandle activeResizeHandle = ClipUI.ResizeHandle.None;
        private float resizeDragStartTime;
        private float resizeDragStartDuration;

        // Gesture handling
        private float longPressTime = 0.5f;
        private float longPressTimer = 0f;
        private bool isLongPressing = false;

        // Pinch zoom
        private bool isPinching = false;
        private float lastPinchDistance = 0f;

        // Performance optimization
        private readonly List<TrackUI> trackUIs = new List<TrackUI>();
        private readonly Dictionary<string, ClipUI> clipUILookup = new Dictionary<string, ClipUI>();

        #region Events

        public event Action<float> OnTimeChanged;
        public event Action<List<ClipUI>> OnSelectionChanged;
        public event Action<ClipUI> OnClipContextMenu;

        #endregion

        #region Properties

        public float PixelsPerSecond => pixelsPerSecond * currentZoom;
        public float CurrentZoom => currentZoom;
        public MiniTimelineDirector Director => director;
        public IReadOnlyList<ClipUI> SelectedClips => selectedClips;
        public bool EnableFrameSnap => enableFrameSnap;
        public GameObject ClipUIPrefab => clipUIPrefab;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize command manager
            commandManager = new TimelineCommandManager();

            // Subscribe to command manager events
            SetupCommandManagerEvents();

            // Setup input callbacks
            SetupInputCallbacks();

            // Initialize UI components
            InitializeUI();
        }

        private void OnEnable()
        {
            primaryTouchAction?.action?.Enable();
            secondaryTouchAction?.action?.Enable();
            touchPositionAction?.action?.Enable();
            secondaryTouchPositionAction?.action?.Enable();
            undoAction?.action?.Enable();
            redoAction?.action?.Enable();
        }

        private void OnDisable()
        {
            primaryTouchAction?.action?.Disable();
            secondaryTouchAction?.action?.Disable();
            touchPositionAction?.action?.Disable();
            secondaryTouchPositionAction?.action?.Disable();
            undoAction?.action?.Disable();
            redoAction?.action?.Disable();
        }

        private void OnDestroy()
        {
            // Cleanup command manager events
            if (commandManager != null)
            {
                commandManager.OnCommandExecuted -= OnCommandExecuted;
                commandManager.OnUndoPerformed -= OnCommandUndoPerformed;
                commandManager.OnRedoPerformed -= OnCommandRedoPerformed;
                commandManager.OnStacksChanged -= OnCommandStacksChanged;
            }

            // InputActionReference cleanup is handled by Unity
        }

        private void Start()
        {
            // Find director in scene if not assigned
            if (director == null)
                director = FindFirstObjectByType<MiniTimelineDirector>();

            if (director != null)
            {
                BindToDirector();
            }
        }

        private void Update()
        {
            UpdateUI();
            HandleLongPress();
        }

        #endregion

        #region Input System Setup

        private void SetupInputCallbacks()
        {
            // Primary touch for tap, drag, and scrub
            if (primaryTouchAction?.action != null)
            {
                primaryTouchAction.action.started += OnPrimaryTouchStarted;
                primaryTouchAction.action.performed += OnPrimaryTouchPerformed;
                primaryTouchAction.action.canceled += OnPrimaryTouchCanceled;
            }

            // Secondary touch for pinch zoom
            if (secondaryTouchAction?.action != null)
            {
                secondaryTouchAction.action.started += OnSecondaryTouchStarted;
                secondaryTouchAction.action.canceled += OnSecondaryTouchCanceled;
            }

            // Touch position tracking
            if (touchPositionAction?.action != null)
            {
                touchPositionAction.action.performed += OnTouchPositionChanged;
            }

            if (secondaryTouchPositionAction?.action != null)
            {
                secondaryTouchPositionAction.action.performed += OnSecondaryTouchPositionChanged;
            }

            // Undo/Redo actions
            if (undoAction?.action != null)
            {
                undoAction.action.performed += OnUndoPerformed;
            }

            if (redoAction?.action != null)
            {
                redoAction.action.performed += OnRedoPerformed;
            }
        }

        private void OnPrimaryTouchStarted(InputAction.CallbackContext context)
        {
            Vector2 screenPos = touchPositionAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 localPos = ScreenToTimelineLocal(screenPos);

            longPressTimer = 0f;
            isLongPressing = false;
            lastTouchPosition = screenPos;

            // Check what was touched
            var hitResult = GetUIElementAtPosition(localPos);

            Debug.Log($"Touch started - Screen: {screenPos}, Local: {localPos}, Hit clip: {(hitResult.clipUI != null ? hitResult.clipUI.Clip.Id : "none")}, isRuler: {hitResult.isRuler}");

            if (hitResult.clipUI != null)
            {
                if (hitResult.isResizeHandle)
                {
                    OnResizeHandleTouched(hitResult.clipUI, hitResult.resizeHandle, localPos);
                }
                else
                {
                    OnClipTouched(hitResult.clipUI, localPos);
                }
            }
            else if (hitResult.isRuler)
            {
                OnRulerTouched(localPos);
            }
            else
            {
                ClearSelection();
            }
        }

        private void OnPrimaryTouchPerformed(InputAction.CallbackContext context)
        {
            // This event is typically fired once per press/tap, not continuously during drag
            // Continuous drag handling is now done in OnTouchPositionChanged
            Debug.Log($"OnPrimaryTouchPerformed - isDragging: {isDragging}, isPlayheadDragging: {isPlayheadDragging}, isResizing: {isResizing}");
        }

        private void OnPrimaryTouchCanceled(InputAction.CallbackContext context)
        {
            if (clipBeingDragged != null)
            {
                EndClipDrag();
            }

            if (clipBeingResized != null)
            {
                EndResize();
            }

            isDragging = false;
            isPlayheadDragging = false;
            isResizing = false;
            isLongPressing = false;
            activeDragCommand = null;
        }

        private void OnSecondaryTouchStarted(InputAction.CallbackContext context)
        {
            if (primaryTouchAction?.action?.IsPressed() == true)
            {
                // Start pinch zoom
                Vector2 pos1 = touchPositionAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
                Vector2 pos2 = secondaryTouchPositionAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;

                isPinching = true;
                lastPinchDistance = Vector2.Distance(pos1, pos2);
            }
        }

        private void OnSecondaryTouchCanceled(InputAction.CallbackContext context)
        {
            isPinching = false;
        }

        private void OnTouchPositionChanged(InputAction.CallbackContext context)
        {
            if (isPinching)
            {
                HandlePinchZoom();
            }
            else if (isDragging && clipBeingDragged != null && primaryTouchAction?.action?.IsPressed() == true)
            {
                // Handle continuous drag updates through position changes
                Vector2 screenPos = context.ReadValue<Vector2>();
                Vector2 localPos = ScreenToTimelineLocal(screenPos);

                Debug.Log($"OnTouchPositionChanged - Continuous drag for {clipBeingDragged.Clip.Id}");
                DragClip(clipBeingDragged, localPos);
            }
            else if (isPlayheadDragging && primaryTouchAction?.action?.IsPressed() == true)
            {
                // Handle continuous playhead dragging
                Vector2 screenPos = context.ReadValue<Vector2>();
                Vector2 localPos = ScreenToTimelineLocal(screenPos);
                ScrubToPosition(localPos.x);
            }
            else if (isResizing && clipBeingResized != null && primaryTouchAction?.action?.IsPressed() == true)
            {
                // Handle continuous resize updates
                Vector2 screenPos = context.ReadValue<Vector2>();
                Vector2 localPos = ScreenToTimelineLocal(screenPos);
                UpdateResize(clipBeingResized, localPos);
            }
        }

        private void OnSecondaryTouchPositionChanged(InputAction.CallbackContext context)
        {
            if (isPinching)
            {
                HandlePinchZoom();
            }
        }

        private void OnUndoPerformed(InputAction.CallbackContext context)
        {
            commandManager?.Undo();
        }

        private void OnRedoPerformed(InputAction.CallbackContext context)
        {
            commandManager?.Redo();
        }

        #endregion

        #region Command Manager Setup

        private void SetupCommandManagerEvents()
        {
            if (commandManager == null) return;

            commandManager.OnCommandExecuted += OnCommandExecuted;
            commandManager.OnUndoPerformed += OnCommandUndoPerformed;
            commandManager.OnRedoPerformed += OnCommandRedoPerformed;
            commandManager.OnStacksChanged += OnCommandStacksChanged;
        }

        private void OnCommandExecuted()
        {
            // Refresh UI after command execution
            RefreshTimelineUI();
        }

        private void OnCommandUndoPerformed()
        {
            // Refresh UI after undo
            RefreshTimelineUI();
        }

        private void OnCommandRedoPerformed()
        {
            // Refresh UI after redo
            RefreshTimelineUI();
        }

        private void OnCommandStacksChanged()
        {
            // Update undo/redo button states
            UpdateUndoRedoButtonStates();
        }

        private void RefreshTimelineUI()
        {
            // Refresh all track UIs to reflect command changes
            foreach (var trackUI in trackUIs)
            {
                trackUI?.UpdateLayout();
            }
        }

        private void UpdateUndoRedoButtonStates()
        {
            if (commandManager == null) return;

            if (undoButton != null)
                undoButton.interactable = commandManager.CanUndo;

            if (redoButton != null)
                redoButton.interactable = commandManager.CanRedo;
        }

        #endregion

        #region UI Initialization

        private void InitializeUI()
        {
            // Setup playback controls
            if (playButton != null)
                playButton.onClick.AddListener(() => director?.Play());

            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => director?.Pause());

            if (stopButton != null)
                stopButton.onClick.AddListener(() => director?.Stop());

            // Setup undo/redo controls
            if (undoButton != null)
                undoButton.onClick.AddListener(() => commandManager?.Undo());

            if (redoButton != null)
                redoButton.onClick.AddListener(() => commandManager?.Redo());

            if (timeSlider != null)
            {
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            }

            // Initialize ruler
            ruler = GetComponentInChildren<TimelineRuler>();
            if (ruler == null && rulerContainer != null)
            {
                var rulerGO = new GameObject("Ruler", typeof(TimelineRuler));
                rulerGO.transform.SetParent(rulerContainer, false);
                ruler = rulerGO.GetComponent<TimelineRuler>();
            }

            ruler?.Initialize(this);

            // Setup initial zoom and layout
            UpdateTimelineLayout();

            // Initialize button states
            UpdateUndoRedoButtonStates();
        }

        #endregion

        #region Director Integration

        public void SetDirector(MiniTimelineDirector newDirector)
        {
            if (director != null)
            {
                UnbindFromDirector();
            }

            director = newDirector;

            if (director != null)
            {
                BindToDirector();
            }
        }

        private void BindToDirector()
        {
            director.OnTimeChanged += OnDirectorTimeChanged;
            director.OnStateChanged += OnDirectorStateChanged;
            director.OnProjectLoaded += OnProjectLoaded;
            director.OnProjectClosed += OnProjectClosed;

            // Build initial UI if project is already loaded
            if (director.Project != null)
            {
                BuildTimelineUI();
            }
        }

        private void UnbindFromDirector()
        {
            director.OnTimeChanged -= OnDirectorTimeChanged;
            director.OnStateChanged -= OnDirectorStateChanged;
            director.OnProjectLoaded -= OnProjectLoaded;
            director.OnProjectClosed -= OnProjectClosed;
        }

        private void OnDirectorTimeChanged(float time)
        {
            UpdatePlayheadPosition(time);
            UpdateTimeDisplay(time);
            OnTimeChanged?.Invoke(time);
        }

        private void OnDirectorStateChanged(PlaybackState state)
        {
            UpdatePlaybackControls(state);
        }

        private void OnProjectLoaded()
        {
            BuildTimelineUI();
        }

        private void OnProjectClosed()
        {
            ClearTimelineUI();
        }

        #endregion

        #region Timeline UI Building

        private void BuildTimelineUI()
        {
            ClearTimelineUI();

            if (director?.Project == null) return;

            // Update timeline dimensions
            timelineWidth = director.Length * PixelsPerSecond;
            UpdateTimelineLayout();

            // Build track UIs
            foreach (var track in director.Tracks)
            {
                CreateTrackUI(track);
            }

            // Update ruler
            ruler?.Rebuild(director.Length, director.Project.frameRate);
        }

        private void CreateTrackUI(IMiniTrack track)
        {
            if (trackUIPrefab == null) return;

            var trackGO = Instantiate(trackUIPrefab, tracksContainer);
            var trackUI = trackGO.GetComponent<TrackUI>();

            if (trackUI != null)
            {
                trackUI.Initialize(this, track);
                trackUIs.Add(trackUI);
            }
        }

        private void ClearTimelineUI()
        {
            // Clear track UIs
            foreach (var trackUI in trackUIs)
            {
                if (trackUI != null)
                    DestroyImmediate(trackUI.gameObject);
            }
            trackUIs.Clear();
            clipUILookup.Clear();
            selectedClips.Clear();
        }

        #endregion

        #region Gesture Handling

        private void HandleLongPress()
        {
            if (primaryTouchAction?.action?.IsPressed() == true && !isDragging && !isPlayheadDragging)
            {
                longPressTimer += Time.unscaledDeltaTime;

                if (longPressTimer >= longPressTime && !isLongPressing)
                {
                    isLongPressing = true;
                    OnLongPress();
                }
            }
        }

        private void OnLongPress()
        {
            Vector2 screenPos = lastTouchPosition;
            Vector2 localPos = ScreenToTimelineLocal(screenPos);

            var hitResult = GetUIElementAtPosition(localPos);

            if (hitResult.clipUI != null)
            {
                ShowClipContextMenu(hitResult.clipUI, screenPos);
            }
        }

        private void HandlePinchZoom()
        {
            Vector2 pos1 = touchPositionAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 pos2 = secondaryTouchPositionAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;

            float currentDistance = Vector2.Distance(pos1, pos2);

            if (lastPinchDistance > 0)
            {
                float zoomDelta = (currentDistance - lastPinchDistance) / Screen.dpi * 2f;
                SetZoom(currentZoom + zoomDelta);
            }

            lastPinchDistance = currentDistance;
        }

        #endregion

        #region Zoom and Layout

        public void SetZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            UpdateTimelineLayout();
            ruler?.UpdateZoom(currentZoom);

            foreach (var trackUI in trackUIs)
            {
                trackUI.UpdateZoom();
            }
        }

        private void UpdateTimelineLayout()
        {
            if (director?.Project != null)
            {
                timelineWidth = director.Length * PixelsPerSecond;

                // Update timeline container size
                if (timelineContainer != null)
                {
                    var sizeDelta = timelineContainer.sizeDelta;
                    sizeDelta.x = timelineWidth;
                    timelineContainer.sizeDelta = sizeDelta;
                }
            }
        }

        #endregion

        #region Utility Methods

        private Vector2 ScreenToTimelineLocal(Vector2 screenPos)
        {
            // Always convert directly to timeline container's local space
            // This works correctly with ScrollRect because Unity handles the coordinate transformation
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                timelineContainer, screenPos, editorCanvas.worldCamera, out Vector2 localPos);
            return localPos;
        }

        private float PositionToTime(float xPosition)
        {
            return xPosition / PixelsPerSecond;
        }

        private float TimeToPosition(float time)
        {
            return time * PixelsPerSecond;
        }

        private float SnapTime(float time)
        {
            if (!enableFrameSnap || director?.Project == null) return time;

            float frameRate = director.Project.frameRate;
            return Mathf.Round(time * frameRate) / frameRate;
        }

        #endregion

        #region Public Command Manager Interface

        /// <summary>
        /// Execute a command through the command manager
        /// </summary>
        public void ExecuteCommand(ITimelineCommand command, bool allowMerge = false)
        {
            commandManager?.ExecuteCommand(command, allowMerge);
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo()
        {
            commandManager?.Undo();
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo()
        {
            commandManager?.Redo();
        }

        /// <summary>
        /// Clear all command history
        /// </summary>
        public void ClearCommandHistory()
        {
            commandManager?.Clear();
            UpdateUndoRedoButtonStates();
        }

        /// <summary>
        /// Get command manager for direct access if needed
        /// </summary>
        public TimelineCommandManager CommandManager => commandManager;

        #endregion

        #region Placeholder Methods (to be implemented)

        private struct UIHitResult
        {
            public ClipUI clipUI;
            public bool isRuler;
            public bool isResizeHandle;
            public ClipUI.ResizeHandle resizeHandle;
        }

        private UIHitResult GetUIElementAtPosition(Vector2 localPos)
        {
            var result = new UIHitResult();

            // Check if position is within the ruler area first
            if (ruler != null && rulerContainer != null)
            {
                RectTransform rulerRect = rulerContainer;

                // Convert to ruler's local space for hit testing
                Vector2 rulerLocalPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rulerRect,
                    RectTransformUtility.WorldToScreenPoint(editorCanvas.worldCamera, timelineContainer.TransformPoint(localPos)),
                    editorCanvas.worldCamera, out rulerLocalPos))
                {
                    // Check if point is within ruler bounds
                    Rect rulerBounds = rulerRect.rect;
                    if (rulerBounds.Contains(rulerLocalPos))
                    {
                        result.isRuler = true;
                        return result;
                    }
                }
            }

            // Check clips in all tracks - work in timeline container's local space
            foreach (var trackUI in trackUIs)
            {
                if (trackUI == null) continue;

                // Get all clip UIs in this track
                var clipUIs = trackUI.GetComponentsInChildren<ClipUI>();

                foreach (var clipUI in clipUIs)
                {
                    if (clipUI == null) continue;

                    RectTransform clipRect = clipUI.transform as RectTransform;
                    if (clipRect == null) continue;

                    // Convert the hit position to the clip's local coordinate space
                    Vector2 clipLocalPos;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(clipRect,
                        RectTransformUtility.WorldToScreenPoint(editorCanvas.worldCamera, timelineContainer.TransformPoint(localPos)),
                        editorCanvas.worldCamera, out clipLocalPos))
                    {
                        // Check if point is within clip bounds, accounting for anchor
                        Rect clipBounds = clipRect.rect;
                        if (clipBounds.Contains(clipLocalPos))
                        {
                            // Check if we hit a resize handle first
                            var resizeHandle = clipUI.GetResizeHandleAtPosition(clipLocalPos);
                            if (resizeHandle != ClipUI.ResizeHandle.None)
                            {
                                result.clipUI = clipUI;
                                result.resizeHandle = resizeHandle;
                                result.isResizeHandle = true;
                                return result;
                            }

                            // Otherwise, it's a regular clip hit
                            result.clipUI = clipUI;
                            return result;
                        }
                    }
                }
            }

            return result;
        }

        private void OnClipTouched(ClipUI clipUI, Vector2 localPos)
        {
            // Select the clip
            if (!selectedClips.Contains(clipUI))
            {
                ClearSelection();
                selectedClips.Add(clipUI);
                OnSelectionChanged?.Invoke(selectedClips);
            }

            // Start dragging
            clipBeingDragged = clipUI;
            clipDragStartTime = clipUI.Clip.Start;
            clipDragCurrentTime = clipUI.Clip.Start; // Initialize current drag position
            clipDragStartLocalPos = localPos;

            // Calculate drag offset: where did the user click relative to the clip's start?
            // Since ClipUI pivot is (0, 0.5), the clip's left edge is at TimeToPosition(startTime)
            float clipLeftEdgeX = TimeToPosition(clipUI.Clip.Start);
            clipDragOffset = localPos.x - clipLeftEdgeX;

            activeDragCommand = null; // Reset any active drag command
            isDragging = true;

            Debug.Log($"Starting drag - Clip start: {clipUI.Clip.Start}, Left edge X: {clipLeftEdgeX}, Click X: {localPos.x}, Offset: {clipDragOffset}");

            // Disable scroll rect during drag to prevent conflicts
            SetScrollRectEnabled(false);

            // Update visual state
            clipUI.SetDragVisualState(true);
        }

        private void OnRulerTouched(Vector2 localPos)
        {
            isPlayheadDragging = true;
            ScrubToPosition(localPos.x);
        }

        private void ScrubToPosition(float xPosition)
        {
            if (director == null) return;

            float time = PositionToTime(xPosition);
            time = SnapTime(time);
            time = Mathf.Clamp(time, 0f, director.Length);

            director.Seek(time);
        }

        private void DragClip(ClipUI clipUI, Vector2 localPos)
        {
            if (clipUI?.Clip == null) return;

            // Calculate new start time from position, accounting for drag offset
            float targetX = localPos.x - clipDragOffset;
            float newStartTime = PositionToTime(targetX);
            newStartTime = SnapTime(newStartTime);
            newStartTime = Mathf.Max(0f, newStartTime);

            // Calculate expected visual position for verification
            float expectedVisualX = TimeToPosition(newStartTime);

            Debug.Log($"DragClip - MouseX: {localPos.x}, Offset: {clipDragOffset}, TargetX: {targetX}, NewTime: {newStartTime}, ExpectedVisualX: {expectedVisualX}");

            // Store current drag time for EndClipDrag
            clipDragCurrentTime = newStartTime;

            // Update visual position directly using UpdateClipVisualTiming
            // This provides immediate visual feedback without modifying the actual clip data
            clipUI.UpdateClipVisualTiming(newStartTime, clipUI.Clip.Duration);

            // Verify the actual position after update
            var clipRect = clipUI.GetComponent<RectTransform>();
            Debug.Log($"DragClip - Clip actual position after update: {clipRect.anchoredPosition}");
        }

        private void EndClipDrag()
        {
            if (clipBeingDragged == null) return;

            Debug.Log($"EndClipDrag - Clip: {clipBeingDragged.Clip.Id}, StartTime: {clipDragStartTime}, CurrentTime: {clipDragCurrentTime}, Diff: {Mathf.Abs(clipDragCurrentTime - clipDragStartTime)}");

            // Re-enable scroll rect
            SetScrollRectEnabled(true);

            // Reset visual state
            clipBeingDragged.SetDragVisualState(false);

            // Only create a command if the clip actually moved
            var clip = clipBeingDragged.Clip;
            var trackUI = clipBeingDragged.GetComponentInParent<TrackUI>();

            if (clip != null && trackUI != null && Mathf.Abs(clipDragCurrentTime - clipDragStartTime) > 0.001f)
            {
                Debug.Log($"EndClipDrag - Creating MoveClipCommand from {clipDragStartTime} to {clipDragCurrentTime}");
                // Create command with the dragged position (clipDragCurrentTime)
                var command = new MoveClipCommand(clip, clipDragStartTime, clipDragCurrentTime, trackUI);
                commandManager?.ExecuteCommand(command, false); // Don't merge since this is a single drag operation
                Debug.Log($"EndClipDrag - Command executed, clip should now be at {clipDragCurrentTime}");
            }
            else
            {
                Debug.Log($"EndClipDrag - No significant movement detected, restoring original position");
                // If no significant movement, restore original position
                clipBeingDragged.UpdateClipVisualTiming(clipDragStartTime, clip.Duration);
            }

            clipBeingDragged = null;
            clipDragOffset = 0f;
            activeDragCommand = null;
        }

        private void ClearSelection()
        {
            selectedClips.Clear();
            OnSelectionChanged?.Invoke(selectedClips);
        }

        private void ShowClipContextMenu(ClipUI clipUI, Vector2 screenPos)
        {
            OnClipContextMenu?.Invoke(clipUI);
        }

        private void UpdateUI()
        {
            // TODO: Update UI elements that need per-frame updates
        }

        private void UpdatePlayheadPosition(float time)
        {
            // TODO: Update playhead visual position
        }

        private void UpdateTimeDisplay(float time)
        {
            if (timeText != null)
            {
                timeText.text = FormatTime(time);
            }

            if (timeSlider != null && director != null)
            {
                timeSlider.value = time / director.Length;
            }
        }

        private void UpdatePlaybackControls(PlaybackState state)
        {
            // TODO: Update button states based on playback state
        }

        private void OnTimeSliderChanged(float normalizedTime)
        {
            if (director != null && !isPlayheadDragging)
            {
                director.Seek(normalizedTime * director.Length);
            }
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int frames = Mathf.FloorToInt((time % 1f) * 30f); // Assuming 30 FPS for display

            return $"{minutes:00}:{seconds:00}:{frames:00}";
        }

        private void OnResizeHandleTouched(ClipUI clipUI, ClipUI.ResizeHandle handle, Vector2 localPos)
        {
            // Start resize operation
            isResizing = true;
            clipBeingResized = clipUI;
            activeResizeHandle = handle;
            resizeDragStartTime = clipUI.Clip.Start;
            resizeDragStartDuration = clipUI.Clip.Duration;

            // Disable scroll rect during resize to prevent conflicts
            SetScrollRectEnabled(false);

            // Update visual state
            clipUI.SetResizeVisualState(true, handle);

            Debug.Log($"Started resizing clip {clipUI.Clip.Id} with {handle} handle");
        }

        private void UpdateResize(ClipUI clipUI, Vector2 localPos)
        {
            if (clipUI?.Clip == null) return;

            // Calculate current time from position
            float currentTime = PositionToTime(localPos.x);

            if (activeResizeHandle == ClipUI.ResizeHandle.Left)
            {
                // Resize from left - changes start time and duration
                float newStart = Mathf.Max(0f, currentTime);
                float newDuration = Mathf.Max(0.1f, (resizeDragStartTime + resizeDragStartDuration) - newStart);

                if (enableFrameSnap)
                {
                    newStart = SnapTime(newStart);
                    newDuration = (resizeDragStartTime + resizeDragStartDuration) - newStart;
                }

                // Update clip visual timing
                clipUI.UpdateClipVisualTiming(newStart, newDuration);
            }
            else if (activeResizeHandle == ClipUI.ResizeHandle.Right)
            {
                // Resize from right - changes duration only
                // Get current visual start time in case clip was moved during this resize session
                var clipRect = clipUI.GetComponent<RectTransform>();
                float currentStartTime = PositionToTime(clipRect.anchoredPosition.x);
                float newDuration = Mathf.Max(0.1f, currentTime - currentStartTime);

                if (enableFrameSnap)
                {
                    float endTime = SnapTime(currentTime);
                    newDuration = endTime - currentStartTime;
                }

                // Update clip visual timing
                clipUI.UpdateClipVisualTiming(currentStartTime, newDuration);
            }
        }

        private void EndResize()
        {
            if (clipBeingResized == null) return;

            Debug.Log($"Ended resizing clip {clipBeingResized.Clip.Id}");

            // Re-enable scroll rect
            SetScrollRectEnabled(true);

            // Reset visual state
            clipBeingResized.SetResizeVisualState(false, ClipUI.ResizeHandle.None);

            // Get current clip state for command creation
            var clip = clipBeingResized.Clip;
            var trackUI = clipBeingResized.GetComponentInParent<TrackUI>();
            
            if (clip != null && trackUI != null)
            {
                // Get current clip timing from the visual state (what the user sees)
                var clipRect = clipBeingResized.GetComponent<RectTransform>();
                float currentStartTime = PositionToTime(clipRect.anchoredPosition.x);
                float currentDuration = PositionToTime(clipRect.sizeDelta.x);
                
                // Only create command if there was a significant change
                bool hasStartChanged = Mathf.Abs(currentStartTime - resizeDragStartTime) > 0.001f;
                bool hasDurationChanged = Mathf.Abs(currentDuration - resizeDragStartDuration) > 0.001f;
                
                if (hasStartChanged || hasDurationChanged)
                {
                    Debug.Log($"EndResize - Creating ResizeClipCommand: Start {resizeDragStartTime}->{currentStartTime}, Duration {resizeDragStartDuration}->{currentDuration}");
                    
                    // Create and execute resize command
                    var command = new ResizeClipCommand(clip, resizeDragStartTime, resizeDragStartDuration, 
                                                      currentStartTime, currentDuration, trackUI);
                    commandManager?.ExecuteCommand(command, false);
                }
                else
                {
                    Debug.Log("EndResize - No significant change detected, restoring original timing");
                    // If no significant change, restore original timing
                    clipBeingResized.UpdateClipVisualTiming(resizeDragStartTime, resizeDragStartDuration);
                }
            }

            clipBeingResized = null;
            activeResizeHandle = ClipUI.ResizeHandle.None;
        }

        private void SetScrollRectEnabled(bool enabled)
        {
            if (timelineScrollRect != null)
            {
                // Instead of disabling the entire ScrollRect (which can cause issues),
                // just disable its interaction components
                timelineScrollRect.horizontal = enabled;
                timelineScrollRect.vertical = enabled;

                Debug.Log($"ScrollRect interaction {(enabled ? "enabled" : "disabled")}");
            }
        }

        #endregion
    }
}