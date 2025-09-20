using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MiniTimeline.Core;

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
        private TimelineRuler ruler;
        
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
        }
        
        private void OnDisable()
        {
            primaryTouchAction?.action?.Disable();
            secondaryTouchAction?.action?.Disable();
            touchPositionAction?.action?.Disable();
            secondaryTouchPositionAction?.action?.Disable();
        }
        
        private void OnDestroy()
        {
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
            
            if (hitResult.clipUI != null)
            {
                OnClipTouched(hitResult.clipUI, localPos);
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
            if (!isDragging && !isPlayheadDragging) return;
            
            Vector2 screenPos = touchPositionAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 localPos = ScreenToTimelineLocal(screenPos);
            
            if (isPlayheadDragging)
            {
                ScrubToPosition(localPos.x);
            }
            else if (clipBeingDragged != null)
            {
                DragClip(clipBeingDragged, localPos);
            }
        }
        
        private void OnPrimaryTouchCanceled(InputAction.CallbackContext context)
        {
            if (clipBeingDragged != null)
            {
                EndClipDrag();
            }
            
            isDragging = false;
            isPlayheadDragging = false;
            isLongPressing = false;
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
        }
        
        private void OnSecondaryTouchPositionChanged(InputAction.CallbackContext context)
        {
            if (isPinching)
            {
                HandlePinchZoom();
            }
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
        
        #region Placeholder Methods (to be implemented)
        
        private struct UIHitResult
        {
            public ClipUI clipUI;
            public bool isRuler;
        }
        
        private UIHitResult GetUIElementAtPosition(Vector2 localPos)
        {
            // TODO: Implement hit testing for clips and ruler
            return new UIHitResult();
        }
        
        private void OnClipTouched(ClipUI clipUI, Vector2 localPos)
        {
            // TODO: Implement clip selection and drag start
        }
        
        private void OnRulerTouched(Vector2 localPos)
        {
            // TODO: Implement playhead scrubbing
        }
        
        private void ScrubToPosition(float xPosition)
        {
            // TODO: Implement scrubbing
        }
        
        private void DragClip(ClipUI clipUI, Vector2 localPos)
        {
            // TODO: Implement clip dragging
        }
        
        private void EndClipDrag()
        {
            // TODO: Implement drag end with command creation
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
        
        #endregion
    }
}