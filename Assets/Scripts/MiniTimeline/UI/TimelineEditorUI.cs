using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] InputActionAsset uiActions;
        
        private InputAction _point;
        private InputAction _click;
        private InputAction _scroll;
        private InputAction _submit;
        private InputAction _cancel;

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
        private float resizeStartMouseX; // Track where the mouse was when resize started
        
        // Prevent drag when resize was just started
        private bool justStartedResize = false;
        private float lastResizeEndTime = 0f;

        // Gesture handling
        private float longPressTime = 0.5f;
        private float longPressTimer = 0f;
        private bool isLongPressing = false;
        
        // Click state tracking
        private bool wasClickPressed = false;
        private float clickPressStartTime = 0f;

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

            // Initialize input actions
            SetupInputActions();

            // Setup input callbacks
            SetupInputCallbacks();

            // Initialize UI components
            InitializeUI();
        }

        private void SetupInputActions()
        {
            if (uiActions == null)
            {
                Debug.LogError("TimelineEditorUI: uiActions is null! Make sure to assign the InputActionAsset in the inspector.");
                return;
            }

            _point  = uiActions.FindAction("UI/Point");   
            _click  = uiActions.FindAction("UI/Click");   
            _scroll = uiActions.FindAction("UI/ScrollWheel");  
            _submit = uiActions.FindAction("UI/Submit");  
            _cancel = uiActions.FindAction("UI/Cancel");

            if (_point != null) _point.Enable();
            if (_click != null) _click.Enable();
            if (_scroll != null) _scroll.Enable();
            if (_submit != null) _submit.Enable();
            if (_cancel != null) _cancel.Enable();

            Debug.Log($"TimelineEditorUI: Input actions setup - Point: {(_point != null ? "OK" : "NULL")}, Click: {(_click != null ? "OK" : "NULL")}, Scroll: {(_scroll != null ? "OK" : "NULL")}, Submit: {(_submit != null ? "OK" : "NULL")}, Cancel: {(_cancel != null ? "OK" : "NULL")}");
        }

        private void OnEnable()
        {
            _point?.Enable();
            _click?.Enable();
            _scroll?.Enable();
            _submit?.Enable();
            _cancel?.Enable();
        }

        private void OnDisable()
        {
            _point?.Disable();
            _click?.Disable();
            _scroll?.Disable();
            _submit?.Disable();
            _cancel?.Disable();
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
            // Note: Long press handling and button release detection now handled 
            // by the new click detection system using OnClickPerformed
        }

        // Note: HandleButtonReleaseDetection and HandleLongPress methods removed
        // Button release detection now handled in OnClickPerformed using float value changes
        // Long press handling now uses coroutine-based timer instead of Update loop

        #endregion

        #region Input System Setup

        private void SetupInputCallbacks()
        {
            // Primary touch for tap, drag, and scrub
            if (_click != null)
            {
                _click.performed += OnClickPerformed;
            }

            // Scroll wheel for zoom
            if (_scroll != null)
            {
                _scroll.performed += OnScrollWheelPerformed;
            }

            // Touch position tracking
            if (_point != null)
            {
                _point.performed += OnTouchPositionChanged;
            }

            // Undo/Redo actions
            if (_submit != null)
            {
                _submit.performed += OnUndoPerformed;
            }

            if (_cancel != null)
            {
                _cancel.performed += OnRedoPerformed;
            }
        }

        private void OnClickPerformed(InputAction.CallbackContext context)
        {
            float clickValue = context.ReadValue<float>();
            bool isCurrentlyPressed = clickValue > 0.5f;
            
            Debug.Log($"*** OnClickPerformed - clickValue: {clickValue}, isCurrentlyPressed: {isCurrentlyPressed}, wasClickPressed: {wasClickPressed} ***");
            
            // Detect press (transition from not pressed to pressed)
            if (isCurrentlyPressed && !wasClickPressed)
            {
                Debug.Log("*** Click PRESS detected ***");
                OnClickPress();
                wasClickPressed = true;
                clickPressStartTime = Time.unscaledTime;
            }
            // Detect release (transition from pressed to not pressed)
            else if (!isCurrentlyPressed && wasClickPressed)
            {
                Debug.Log("*** Click RELEASE detected ***");
                OnClickRelease();
                wasClickPressed = false;
            }
        }

        private void OnClickPress()
        {
            Debug.Log("OnClickPress: Processing click press interaction");
            
            // Get screen position from the Point action
            Vector2 screenPos = Vector2.zero;
            if (_point != null)
            {
                screenPos = _point.ReadValue<Vector2>();
                Debug.Log($"OnClickPress: Got screen position from Point action: {screenPos}");
            }
            else
            {
                Debug.LogWarning("OnClickPress: Point action is null, cannot get screen position");
                return;
            }
            
            // Fallback: try to get position from mouse if touch isn't available
            if (screenPos == Vector2.zero)
            {
                screenPos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
                Debug.Log($"OnClickPress: Fallback to mouse position: {screenPos}");
            }
            
            // If still zero, something is wrong with the input setup
            if (screenPos == Vector2.zero)
            {
                Debug.LogError("OnClickPress: Could not get valid screen position from input system");
                return;
            }
            
            Vector2 localPos = ScreenToTimelineLocal(screenPos);

            Debug.Log($"OnClickPress: Touch at screen position {screenPos}, local position {localPos}");
            Debug.Log($"OnClickPress: Director state - hasDirector: {director != null}, hasProject: {director?.Project != null}, trackCount: {(director != null ? director.Tracks.Count : 0)}");

            // Debug timeline UI state
            Debug.Log($"OnClickPress: UI state - trackUIs: {trackUIs.Count}, timelineContainer: {timelineContainer != null}");

            longPressTimer = 0f;
            isLongPressing = false;
            lastTouchPosition = screenPos;
            
            // Reset flags
            justStartedResize = false;

            // Check what was touched
            var hitResult = GetUIElementAtPosition(localPos);

            Debug.Log($"OnClickPress: Touch started - Screen: {screenPos}, Local: {localPos}, Hit clip: {(hitResult.clipUI != null ? hitResult.clipUI.Clip.Id : "none")}, isRuler: {hitResult.isRuler}, hit resize handle: {hitResult.isResizeHandle}");

            if (hitResult.clipUI != null)
            {
                if (hitResult.isResizeHandle)
                {
                    Debug.Log($"OnClickPress: Hit resize handle {hitResult.resizeHandle} on clip {hitResult.clipUI.Clip.Id} - STARTING RESIZE ONLY");
                    OnResizeHandleTouched(hitResult.clipUI, hitResult.resizeHandle, localPos);
                    return;
                }
                else
                {
                    Debug.Log($"OnClickPress: Hit clip body on clip {hitResult.clipUI.Clip.Id} - STARTING DRAG ONLY");
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
            
            // Start long press timer using coroutine instead of Update
            StartCoroutine(HandleLongPressTimer());
        }

        private void OnClickRelease()
        {
            Debug.Log("*** OnClickRelease - ending interactions ***");
            
            float pressDuration = Time.unscaledTime - clickPressStartTime;
            Debug.Log($"OnClickRelease: Press duration: {pressDuration}s");
            
            // Handle long press if it was detected during press
            if (isLongPressing)
            {
                Debug.Log("OnClickRelease: Was long pressing, handling long press end");
                HandleLongPressEnd();
            }
            // Handle regular click if it was a short press and no operations were active
            else if (pressDuration < longPressTime && !isDragging && !isResizing && !isPlayheadDragging)
            {
                Debug.Log("OnClickRelease: Short press without drag/resize - treating as simple click");
                // This was just a simple click, not a drag or resize
            }
            
            // End any active operations
            if (clipBeingDragged != null && isDragging)
            {
                Debug.Log($"OnClickRelease - Ending drag for clip {clipBeingDragged.Clip.Id}");
                EndClipDrag();
            }

            if (clipBeingResized != null && isResizing)
            {
                Debug.Log($"OnClickRelease - Ending resize for clip {clipBeingResized.Clip.Id}");
                EndResize();
            }

            if (isPlayheadDragging)
            {
                Debug.Log("OnClickRelease - Ending playhead drag");
                isPlayheadDragging = false;
            }

            // Clear all operation states
            isDragging = false;
            isPlayheadDragging = false;
            isResizing = false;
            isLongPressing = false;
            longPressTimer = 0f;
            justStartedResize = false;
            activeDragCommand = null;
            clipBeingDragged = null;
            clipBeingResized = null;
            activeResizeHandle = ClipUI.ResizeHandle.None;
            resizeStartMouseX = 0f;
        }

        private System.Collections.IEnumerator HandleLongPressTimer()
        {
            longPressTimer = 0f;
            
            while (wasClickPressed && longPressTimer < longPressTime)
            {
                longPressTimer += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // If we reached the long press threshold and button is still pressed
            if (wasClickPressed && longPressTimer >= longPressTime && !isLongPressing)
            {
                isLongPressing = true;
                OnLongPress();
            }
        }

        private void HandleLongPressEnd()
        {
            // Handle the end of a long press
            Debug.Log("HandleLongPressEnd called");
            isLongPressing = false;
            longPressTimer = 0f;
        }

        private void OnPrimaryTouchStarted(InputAction.CallbackContext context)
        {
            Debug.Log("*** OnPrimaryTouchStarted called - this should appear for regular clicks ***");
            Debug.Log("TimelineEditorUI: OnPrimaryTouchStarted called");
            
            // Debug input action status
            Debug.Log($"TimelineEditorUI: Input action status - Point: {(_point != null ? "exists" : "null")}, enabled: {(_point?.enabled ?? false)}, phase: {(_point?.phase ?? InputActionPhase.Disabled)}");
            
            // Get screen position from the Point action, not from the context
            Vector2 screenPos = Vector2.zero;
            if (_point != null)
            {
                screenPos = _point.ReadValue<Vector2>();
                Debug.Log($"TimelineEditorUI: Got screen position from Point action: {screenPos}");
            }
            else
            {
                Debug.LogWarning("TimelineEditorUI: Point action is null, cannot get screen position");
                return;
            }
            
            // Fallback: try to get position from mouse if touch isn't available
            if (screenPos == Vector2.zero)
            {
                screenPos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
                Debug.Log($"TimelineEditorUI: Fallback to mouse position: {screenPos}");
            }
            
            // If still zero, something is wrong with the input setup
            if (screenPos == Vector2.zero)
            {
                Debug.LogError("TimelineEditorUI: Could not get valid screen position from input system");
                
                // Final fallback: use Unity's legacy Input system
                screenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                Debug.Log($"TimelineEditorUI: Final fallback to Unity Input.mousePosition: {screenPos}");
            }
            
            Vector2 localPos = ScreenToTimelineLocal(screenPos);

            Debug.Log($"TimelineEditorUI: Touch at screen position {screenPos}, local position {localPos}");
            Debug.Log($"TimelineEditorUI: Director state - hasDirector: {director != null}, hasProject: {director?.Project != null}, trackCount: {(director != null ? director.Tracks.Count : 0)}");

            // Debug timeline UI state
            Debug.Log($"TimelineEditorUI: UI state - trackUIs: {trackUIs.Count}, timelineContainer: {timelineContainer != null}");
            if (trackUIs.Count > 0)
            {
                for (int i = 0; i < trackUIs.Count; i++)
                {
                    var trackUI = trackUIs[i];
                    if (trackUI != null)
                    {
                        var clipUIs = trackUI.GetComponentsInChildren<ClipUI>();
                        Debug.Log($"TimelineEditorUI: Track {i} has {clipUIs.Length} clip UIs");
                        foreach (var clipUI in clipUIs)
                        {
                            if (clipUI != null && clipUI.Clip != null)
                            {
                                var rect = clipUI.GetComponent<RectTransform>();
                                Debug.Log($"TimelineEditorUI: Clip {clipUI.Clip.Id} - position: {rect.anchoredPosition}, size: {rect.sizeDelta}, active: {clipUI.gameObject.activeInHierarchy}");
                            }
                        }
                    }
                }
            }

            longPressTimer = 0f;
            isLongPressing = false;
            lastTouchPosition = screenPos;
            
            // Reset flags
            justStartedResize = false;

            // Check what was touched
            var hitResult = GetUIElementAtPosition(localPos);

            Debug.Log($"Touch started - Screen: {screenPos}, Local: {localPos}, Hit clip: {(hitResult.clipUI != null ? hitResult.clipUI.Clip.Id : "none")}, isRuler: {hitResult.isRuler}, hit resize handle: {hitResult.isResizeHandle}");

            if (hitResult.clipUI != null)
            {
                if (hitResult.isResizeHandle)
                {
                    Debug.Log($"Hit resize handle {hitResult.resizeHandle} on clip {hitResult.clipUI.Clip.Id} - STARTING RESIZE ONLY");
                    OnResizeHandleTouched(hitResult.clipUI, hitResult.resizeHandle, localPos);
                    // Do NOT call OnClipTouched when we hit a resize handle
                    return;
                }
                else
                {
                    Debug.Log($"Hit clip body on clip {hitResult.clipUI.Clip.Id} - STARTING DRAG ONLY");
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
            Debug.Log("*** OnPrimaryTouchPerformed called - handling as click ***");
            Debug.Log($"OnPrimaryTouchPerformed - isDragging: {isDragging}, isPlayheadDragging: {isPlayheadDragging}, isResizing: {isResizing}");
            
            // Handle this as a click since OnPrimaryTouchStarted is not being called for PassThrough actions
            HandlePrimaryTouchLogic();
        }

        private void HandlePrimaryTouchLogic()
        {
            Debug.Log("HandlePrimaryTouchLogic: Processing click interaction");
            
            // Debug input action status
            Debug.Log($"HandlePrimaryTouchLogic: Input action status - Point: {(_point != null ? "exists" : "null")}, enabled: {(_point?.enabled ?? false)}, phase: {(_point?.phase ?? InputActionPhase.Disabled)}");
            
            // Get screen position from the Point action, not from the context
            Vector2 screenPos = Vector2.zero;
            if (_point != null)
            {
                screenPos = _point.ReadValue<Vector2>();
                Debug.Log($"HandlePrimaryTouchLogic: Got screen position from Point action: {screenPos}");
            }
            else
            {
                Debug.LogWarning("HandlePrimaryTouchLogic: Point action is null, cannot get screen position");
                return;
            }
            
            // Fallback: try to get position from mouse if touch isn't available
            if (screenPos == Vector2.zero)
            {
                screenPos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
                Debug.Log($"HandlePrimaryTouchLogic: Fallback to mouse position: {screenPos}");
            }
            
            // If still zero, something is wrong with the input setup
            if (screenPos == Vector2.zero)
            {
                Debug.LogError("HandlePrimaryTouchLogic: Could not get valid screen position from input system");
                return;
            }
            
            Vector2 localPos = ScreenToTimelineLocal(screenPos);

            Debug.Log($"HandlePrimaryTouchLogic: Touch at screen position {screenPos}, local position {localPos}");
            Debug.Log($"HandlePrimaryTouchLogic: Director state - hasDirector: {director != null}, hasProject: {director?.Project != null}, trackCount: {(director != null ? director.Tracks.Count : 0)}");

            // Debug timeline UI state
            Debug.Log($"HandlePrimaryTouchLogic: UI state - trackUIs: {trackUIs.Count}, timelineContainer: {timelineContainer != null}");
            if (trackUIs.Count > 0)
            {
                for (int i = 0; i < trackUIs.Count; i++)
                {
                    var trackUI = trackUIs[i];
                    if (trackUI != null)
                    {
                        var clipUIs = trackUI.GetComponentsInChildren<ClipUI>();
                        Debug.Log($"HandlePrimaryTouchLogic: Track {i} has {clipUIs.Length} clip UIs");
                        foreach (var clipUI in clipUIs)
                        {
                            if (clipUI != null && clipUI.Clip != null)
                            {
                                var rect = clipUI.GetComponent<RectTransform>();
                                Debug.Log($"HandlePrimaryTouchLogic: Clip {clipUI.Clip.Id} - position: {rect.anchoredPosition}, size: {rect.sizeDelta}, active: {clipUI.gameObject.activeInHierarchy}");
                            }
                        }
                    }
                }
            }

            longPressTimer = 0f;
            isLongPressing = false;
            lastTouchPosition = screenPos;
            
            // Reset flags
            justStartedResize = false;

            // Check what was touched
            var hitResult = GetUIElementAtPosition(localPos);

            Debug.Log($"HandlePrimaryTouchLogic: Touch started - Screen: {screenPos}, Local: {localPos}, Hit clip: {(hitResult.clipUI != null ? hitResult.clipUI.Clip.Id : "none")}, isRuler: {hitResult.isRuler}, hit resize handle: {hitResult.isResizeHandle}");

            if (hitResult.clipUI != null)
            {
                if (hitResult.isResizeHandle)
                {
                    Debug.Log($"HandlePrimaryTouchLogic: Hit resize handle {hitResult.resizeHandle} on clip {hitResult.clipUI.Clip.Id} - STARTING RESIZE ONLY");
                    OnResizeHandleTouched(hitResult.clipUI, hitResult.resizeHandle, localPos);
                    // Do NOT call OnClipTouched when we hit a resize handle
                    return;
                }
                else
                {
                    Debug.Log($"HandlePrimaryTouchLogic: Hit clip body on clip {hitResult.clipUI.Clip.Id} - STARTING DRAG ONLY");
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

        private void OnPrimaryTouchCanceled(InputAction.CallbackContext context)
        {
            // NOTE: OnPrimaryTouchCanceled does NOT trigger reliably with PassThrough actions in Unity's Input System
            // This is expected behavior - PassThrough actions have different callback patterns than Button actions
            // Button release detection is handled in OnTouchPositionChanged using _click.IsPressed() instead
            
            Debug.Log($"*** OnPrimaryTouchCanceled called (unexpected for PassThrough actions) ***");
            Debug.Log($"OnPrimaryTouchCanceled - isDragging: {isDragging}, isResizing: {isResizing}, longPressTimer: {longPressTimer}");

            // This method is kept for completeness but should not be relied upon for PassThrough actions
            // The actual button release detection happens in OnTouchPositionChanged using _click.IsPressed()
        }

        private void OnScrollWheelPerformed(InputAction.CallbackContext context)
        {
            Vector2 scrollDelta = context.ReadValue<Vector2>();
            
            // Use scroll wheel for zooming
            float zoomDelta = scrollDelta.y * 0.1f;
            SetZoom(currentZoom + zoomDelta);
        }

        private void OnTouchPositionChanged(InputAction.CallbackContext context)
        {
            // Handle continuous position updates during drag/resize operations
            // Button release is now handled in OnClickRelease via float value detection
            
            // Clear the just started resize flag once we start moving
            if (justStartedResize)
            {
                justStartedResize = false;
                Debug.Log("OnTouchPositionChanged - Cleared justStartedResize flag");
            }
            
            if (isResizing && clipBeingResized != null && wasClickPressed)
            {
                // Handle continuous resize updates - prioritize resize over drag
                Vector2 screenPos = context.ReadValue<Vector2>();
                Vector2 localPos = ScreenToTimelineLocal(screenPos);
                UpdateResize(clipBeingResized, localPos);
            }
            else if (isDragging && clipBeingDragged != null && wasClickPressed)
            {
                // Handle continuous drag updates through position changes
                Vector2 screenPos = context.ReadValue<Vector2>();
                Vector2 localPos = ScreenToTimelineLocal(screenPos);

                Debug.Log($"OnTouchPositionChanged - Continuous drag for {clipBeingDragged.Clip.Id}, screenPos: {screenPos}, localPos: {localPos}");
                DragClip(clipBeingDragged, localPos);
            }
            else if (isPlayheadDragging && wasClickPressed)
            {
                // Handle continuous playhead dragging
                Vector2 screenPos = context.ReadValue<Vector2>();
                Vector2 localPos = ScreenToTimelineLocal(screenPos);
                ScrubToPosition(localPos.x);
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

            if (director?.Project == null) 
            {
                Debug.LogWarning("TimelineEditorUI: BuildTimelineUI called but director or project is null");
                return;
            }

            Debug.Log($"TimelineEditorUI: Building timeline UI - Project: {director.Project.name}, Length: {director.Length}, Tracks: {director.Tracks.Count}");

            // Update timeline dimensions
            timelineWidth = director.Length * PixelsPerSecond;
            UpdateTimelineLayout();

            // Build track UIs
            foreach (var track in director.Tracks)
            {
                var clips = track.GetClips().ToList();
                Debug.Log($"TimelineEditorUI: Creating track UI for track {track.Id} with {clips.Count} clips");
                CreateTrackUI(track);
            }

            // Update ruler
            ruler?.Rebuild(director.Length, director.Project.frameRate);
            
            Debug.Log($"TimelineEditorUI: Built {trackUIs.Count} track UIs total");
        }

        private void CreateTrackUI(IMiniTrack track)
        {
            if (trackUIPrefab == null) 
            {
                Debug.LogWarning("TimelineEditorUI: trackUIPrefab is null, cannot create track UI");
                return;
            }

            if (tracksContainer == null)
            {
                Debug.LogWarning("TimelineEditorUI: tracksContainer is null, cannot create track UI");
                return;
            }

            var trackGO = Instantiate(trackUIPrefab, tracksContainer);
            var trackUI = trackGO.GetComponent<TrackUI>();

            if (trackUI != null)
            {
                Debug.Log($"TimelineEditorUI: Created TrackUI for track {track.Id}, initializing...");
                trackUI.Initialize(this, track);
                trackUIs.Add(trackUI);
                
                // Check if clips were created
                var clipUIs = trackUI.GetComponentsInChildren<ClipUI>();
                Debug.Log($"TimelineEditorUI: TrackUI for {track.Id} now has {clipUIs.Length} clip UIs");
            }
            else
            {
                Debug.LogError("TimelineEditorUI: Instantiated trackUIPrefab does not have TrackUI component");
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

        // Note: HandleLongPress() method removed - now using coroutine-based timer in HandleLongPressTimer()

        private void OnLongPress()
        {
            Debug.Log("OnLongPress triggered");
            
            // Get current screen position from the Point action
            Vector2 screenPos = Vector2.zero;
            if (_point != null)
            {
                screenPos = _point.ReadValue<Vector2>();
                Debug.Log($"OnLongPress: Got screen position from Point action: {screenPos}");
            }
            else
            {
                // Fallback to last known touch position
                screenPos = lastTouchPosition;
                Debug.Log($"OnLongPress: Using lastTouchPosition: {screenPos}");
            }
            
            // If still zero, try mouse position as fallback
            if (screenPos == Vector2.zero)
            {
                screenPos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
                Debug.Log($"OnLongPress: Fallback to mouse position: {screenPos}");
            }
            
            Vector2 localPos = ScreenToTimelineLocal(screenPos);
            
            // Check for clip at position
            var hitResult = GetUIElementAtPosition(localPos);
            
            if (hitResult.clipUI != null)
            {
                // If it's a quick release (not a true long press), treat it as a regular click
                if (longPressTimer < longPressTime * 0.8f) // 80% of long press threshold
                {
                    Debug.Log($"OnLongPress: Quick release detected (timer: {longPressTimer}), treating as regular click");
                    OnClipTouched(hitResult.clipUI, localPos);
                }
                else
                {
                    Debug.Log($"OnLongPress: True long press detected (timer: {longPressTimer}), showing context menu");
                    ShowClipContextMenu(hitResult.clipUI, screenPos);
                }
            }
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
            // Convert screen position to timeline container's local space
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                timelineContainer, screenPos, editorCanvas.worldCamera, out localPos))
            {
                Debug.Log($"ScreenToTimelineLocal: screenPos={screenPos} -> localPos={localPos}");
                return localPos;
            }
            
            Debug.LogWarning($"ScreenToTimelineLocal: Failed to convert screen position {screenPos}");
            return Vector2.zero;
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

        #region Clip Interaction Methods

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
            
            Debug.Log($"GetUIElementAtPosition - Input localPos: {localPos}, trackUIs count: {trackUIs.Count}");

            // Check if position is within the ruler area first
            if (ruler != null && rulerContainer != null)
            {
                RectTransform rulerRect = rulerContainer;
                Vector2 rulerLocalPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rulerRect,
                    RectTransformUtility.WorldToScreenPoint(editorCanvas.worldCamera, timelineContainer.TransformPoint(localPos)),
                    editorCanvas.worldCamera, out rulerLocalPos))
                {
                    Rect rulerBounds = rulerRect.rect;
                    if (rulerBounds.Contains(rulerLocalPos))
                    {
                        Debug.Log($"Hit ruler at {rulerLocalPos}");
                        result.isRuler = true;
                        return result;
                    }
                }
            }

            // Check clips in all tracks - use simple rect containment test
            foreach (var trackUI in trackUIs)
            {
                if (trackUI == null) continue;
                
                Debug.Log($"Checking track (index {trackUIs.IndexOf(trackUI)})");

                // Get all clip UIs in this track
                var clipUIs = trackUI.GetComponentsInChildren<ClipUI>();
                Debug.Log($"Found {clipUIs.Length} clip UIs in track");

                foreach (var clipUI in clipUIs)
                {
                    if (clipUI == null || clipUI.Clip == null) continue;

                    RectTransform clipRect = clipUI.transform as RectTransform;
                    if (clipRect == null) continue;

                    // Simple approach: convert the timeline local position to clip's local space
                    Vector2 clipLocalPos;
                    Vector3 timelineWorldPoint = timelineContainer.TransformPoint(localPos);
                    
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(clipRect,
                        RectTransformUtility.WorldToScreenPoint(editorCanvas.worldCamera, timelineWorldPoint),
                        editorCanvas.worldCamera, out clipLocalPos))
                    {
                        Rect clipBounds = clipRect.rect;
                        Debug.Log($"Hit test - Clip {clipUI.Clip.Id}: localPos={localPos}, clipLocalPos={clipLocalPos}, clipBounds={clipBounds}");
                        
                        // Check if point is within clip bounds
                        if (clipBounds.Contains(clipLocalPos))
                        {
                            Debug.Log($"Clip {clipUI.Clip.Id} contains position - checking resize handles");
                            
                            // Check if we hit a resize handle first (only if clip is selected)
                            if (clipUI.IsSelected)
                            {
                                var resizeHandle = clipUI.GetResizeHandleAtPosition(clipLocalPos);
                                if (resizeHandle != ClipUI.ResizeHandle.None)
                                {
                                    Debug.Log($"Hit resize handle {resizeHandle} on clip {clipUI.Clip.Id}");
                                    result.clipUI = clipUI;
                                    result.resizeHandle = resizeHandle;
                                    result.isResizeHandle = true;
                                    return result;
                                }
                            }

                            // Otherwise, it's a regular clip hit
                            Debug.Log($"Regular clip hit on {clipUI.Clip.Id}");
                            result.clipUI = clipUI;
                            return result;
                        }
                        else
                        {
                            Debug.Log($"Clip {clipUI.Clip.Id} does NOT contain position {clipLocalPos} (bounds: {clipBounds})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to convert timeline position to clip local space for {clipUI.Clip.Id}");
                    }
                }
            }

            Debug.Log("No UI element hit at position");
            return result;
        }

        private void OnClipTouched(ClipUI clipUI, Vector2 localPos)
        {
            float timeSinceLastResize = Time.unscaledTime - lastResizeEndTime;
            Debug.Log($"OnClipTouched - Clip: {clipUI.Clip.Id}, isResizing: {isResizing}, justStartedResize: {justStartedResize}, timeSinceLastResize: {timeSinceLastResize}");
            
            // Don't start dragging if we're already resizing, just started resize, or just finished resize
            if (isResizing || justStartedResize || timeSinceLastResize < 0.1f)
            {
                Debug.Log("OnClipTouched - Ignoring clip touch because we're resizing, just started resize, or just ended resize");
                return;
            }

            // Select the clip first
            Debug.Log($"OnClipTouched - Current selection contains clip: {selectedClips.Contains(clipUI)}, selectedClips.Count: {selectedClips.Count}");
            if (!selectedClips.Contains(clipUI))
            {
                Debug.Log($"OnClipTouched - Clearing selection and adding clip {clipUI.Clip.Id}");
                ClearSelection();
                selectedClips.Add(clipUI);
                clipUI.SetSelected(true); // Ensure the clip knows it's selected
                Debug.Log($"OnClipTouched - After selection: selectedClips.Count: {selectedClips.Count}, clipUI.IsSelected: {clipUI.IsSelected}");
                OnSelectionChanged?.Invoke(selectedClips);
            }
            else
            {
                Debug.Log($"OnClipTouched - Clip {clipUI.Clip.Id} is already selected, clipUI.IsSelected: {clipUI.IsSelected}");
            }

            // Double-check: if we somehow became resizing after selection, don't start drag
            float timeSinceLastResize2 = Time.unscaledTime - lastResizeEndTime;
            if (isResizing || justStartedResize || timeSinceLastResize2 < 0.1f)
            {
                Debug.Log("OnClipTouched - Became resizing after selection or too soon after resize, not starting drag");
                return;
            }

            // Start dragging
            Debug.Log($"OnClipTouched - Starting drag for clip {clipUI.Clip.Id}");
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

            Debug.Log($"OnClipTouched - Drag setup: startTime={clipDragStartTime}, localPos={localPos}, clipLeftEdgeX={clipLeftEdgeX}, dragOffset={clipDragOffset}, clipBeingDragged: {(clipBeingDragged != null ? clipBeingDragged.Clip.Id : "null")}, isDragging: {isDragging}");

            // Disable scroll rect during drag to prevent conflicts
            SetScrollRectEnabled(false);

            // Update visual state
            clipUI.SetDragVisualState(true);
            Debug.Log($"OnClipTouched - Completed touch handling for clip {clipUI.Clip.Id}");
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
            
            // Don't drag if we're currently resizing
            if (isResizing)
            {
                Debug.Log("DragClip - Blocked because we're resizing");
                return;
            }

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
            // Properly deselect all clips
            foreach (var clipUI in selectedClips)
            {
                if (clipUI != null)
                {
                    clipUI.SetSelected(false);
                }
            }
            
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
            Debug.Log($"OnResizeHandleTouched - Starting resize for clip {clipUI.Clip.Id} with {handle} handle at localPos {localPos}");
            
            // Clear any existing drag state to prevent conflicts
            if (isDragging)
            {
                Debug.Log("OnResizeHandleTouched - Clearing drag state to start resize");
                isDragging = false;
                clipBeingDragged = null;
                activeDragCommand = null;
            }

            // Set flag to prevent drag from starting
            justStartedResize = true;

            // Start resize operation
            isResizing = true;
            clipBeingResized = clipUI;
            activeResizeHandle = handle;
            resizeDragStartTime = clipUI.Clip.Start;
            resizeDragStartDuration = clipUI.Clip.Duration;
            resizeStartMouseX = localPos.x; // Record where the mouse was when resize started

            // Disable scroll rect during resize to prevent conflicts
            SetScrollRectEnabled(false);

            // Update visual state
            clipUI.SetResizeVisualState(true, handle);

            Debug.Log($"Started resizing clip {clipUI.Clip.Id} with {handle} handle - startTime: {resizeDragStartTime}, startDuration: {resizeDragStartDuration}, startMouseX: {resizeStartMouseX}");
        }

        private void UpdateResize(ClipUI clipUI, Vector2 localPos)
        {
            if (clipUI?.Clip == null) return;

            // Calculate mouse movement from start position
            float mouseDeltaX = localPos.x - resizeStartMouseX;
            float timeDelta = PositionToTime(mouseDeltaX);

            Debug.Log($"UpdateResize - MouseX: {localPos.x}, StartMouseX: {resizeStartMouseX}, DeltaX: {mouseDeltaX}, TimeDelta: {timeDelta}");

            if (activeResizeHandle == ClipUI.ResizeHandle.Left)
            {
                // Resize from left - changes start time and duration
                // Apply the mouse delta to the original start time
                float newStart = resizeDragStartTime + timeDelta;
                newStart = Mathf.Max(0f, newStart);
                
                // The end time remains fixed at the original end position
                float originalEndTime = resizeDragStartTime + resizeDragStartDuration;
                float newDuration = Mathf.Max(0.1f, originalEndTime - newStart);

                if (enableFrameSnap)
                {
                    newStart = SnapTime(newStart);
                    newDuration = Mathf.Max(0.1f, originalEndTime - newStart);
                }

                Debug.Log($"Left resize: originalStart={resizeDragStartTime}, newStart={newStart}, originalEnd={originalEndTime}, newDuration={newDuration}");

                // Use specialized method for left resize to avoid pivot issues
                clipUI.UpdateClipVisualStart(newStart, newDuration);
            }
            else if (activeResizeHandle == ClipUI.ResizeHandle.Right)
            {
                // Resize from right - changes duration only, start time stays the same
                // Apply the mouse delta to the original duration
                float newDuration = resizeDragStartDuration + timeDelta;
                newDuration = Mathf.Max(0.1f, newDuration);

                if (enableFrameSnap)
                {
                    // For right resize, snap the end time
                    float endTime = resizeDragStartTime + newDuration;
                    endTime = SnapTime(endTime);
                    newDuration = Mathf.Max(0.1f, endTime - resizeDragStartTime);
                }

                Debug.Log($"Right resize: startTime={resizeDragStartTime}, originalDuration={resizeDragStartDuration}, newDuration={newDuration}");

                // Use specialized method for right resize to avoid pivot issues
                clipUI.UpdateClipVisualDuration(resizeDragStartTime, newDuration);
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
                
                // Since we use center pivot (0.5, 0.5), anchoredPosition.x is the center of the clip
                // We need to calculate the start time from the center position and width
                float centerPos = clipRect.anchoredPosition.x;
                float width = clipRect.sizeDelta.x;
                float leftEdgePos = centerPos - width / 2f;  // Calculate left edge from center
                
                float currentStartTime = PositionToTime(leftEdgePos);
                float currentDuration = PositionToTime(width);
                
                Debug.Log($"EndResize - Visual state: centerPos={centerPos}, width={width}, leftEdge={leftEdgePos}, startTime={currentStartTime}, duration={currentDuration}");
                
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

            // Clear all resize state
            clipBeingResized = null;
            activeResizeHandle = ClipUI.ResizeHandle.None;
            isResizing = false;
            justStartedResize = false;
            lastResizeEndTime = Time.unscaledTime;
            resizeStartMouseX = 0f;

            // Ensure drag state is completely cleared
            isDragging = false;
            clipBeingDragged = null;
            activeDragCommand = null;

            // Re-enable scroll rect
            SetScrollRectEnabled(true);

            Debug.Log("EndResize - All resize and drag state cleared");
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