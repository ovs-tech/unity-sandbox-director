using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MiniTimeline.Core;
using MiniTimeline.UI.Commands;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Main in-game timeline editor UI
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
        private TimelineContextMenu contextMenu;

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
        [SerializeField] private GameObject contextMenuPrefab;

        [Header("Debug")]
        [SerializeField] bool isDebug;

        // Core references
        private MiniTimelineDirector director;
        private TimelineCommandManager commandManager;

        // UI State
        private float currentZoom = 1f;
        private float timelineWidth;
        private bool isPlayheadDragging = false;

        // Selection and editing
        private List<ClipUI> selectedClips = new List<ClipUI>();
        private TimelineRuler ruler;

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
        public GameObject ContextMenuPrefab => contextMenuPrefab;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize command manager
            commandManager = new TimelineCommandManager();

            // Subscribe to command manager events
            SetupCommandManagerEvents();

            // Initialize UI components
            InitializeUI();
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

        // Note: HandleButtonReleaseDetection and HandleLongPress methods removed
        // Button release detection now handled in OnClickPerformed using float value changes
        // Long press handling now uses coroutine-based timer instead of Update loop

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

            // Initialize context menu as prefab
            if (contextMenu == null && contextMenuPrefab != null)
            {
                var contextMenuGO = Instantiate(contextMenuPrefab, editorCanvas.transform);
                contextMenu = contextMenuGO.GetComponent<TimelineContextMenu>();
            }

            contextMenu?.Initialize(this);
            // Listen to context menu open/close to disable/enable timeline interaction
            if (contextMenu != null)
            {
                contextMenu.OnMenuClosed += EnableTimelineInteraction;
            }
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
                return;
            }



            // Update timeline dimensions
            timelineWidth = director.Length * PixelsPerSecond;
            UpdateTimelineLayout();

            // Build track UIs
            foreach (var track in director.Tracks)
            {
                var clips = track.GetClips().ToList();

                CreateTrackUI(track);
            }

            // Update ruler
            ruler?.Rebuild(director.Length, director.Project.frameRate);


        }

        private void CreateTrackUI(IMiniTrack track)
        {
            if (trackUIPrefab == null)
            {
                return;
            }

            if (tracksContainer == null)
            {
                return;
            }

            var trackGO = Instantiate(trackUIPrefab, tracksContainer);
            var trackUI = trackGO.GetComponent<TrackUI>();

            if (trackUI != null)
            {
                trackUI.Initialize(this, track);
                trackUIs.Add(trackUI);
                // Check if clips were created
                var clipUIs = trackUI.GetComponentsInChildren<ClipUI>();
            }
            else
            {
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

                return localPos;
            }


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
        
        // Public versions for ClipUI access
        public float PositionToTimePublic(float xPosition) => PositionToTime(xPosition);
        public float TimeToPositionPublic(float time) => TimeToPosition(time);
        public float SnapTimePublic(float time) => SnapTime(time);
        
        /// <summary>
        /// Check if a local position hits the ruler area
        /// </summary>
        private bool IsRulerHit(Vector2 localPos)
        {
            if (ruler == null || rulerContainer == null) return false;
            
            RectTransform rulerRect = rulerContainer;
            Vector2 rulerLocalPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rulerRect,
                RectTransformUtility.WorldToScreenPoint(editorCanvas.worldCamera, timelineContainer.TransformPoint(localPos)),
                editorCanvas.worldCamera, out rulerLocalPos))
            {
                Rect rulerBounds = rulerRect.rect;
                return rulerBounds.Contains(rulerLocalPos);
            }
            
            return false;
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

        #region Clip Interaction Methods - Now handled by ClipUI directly

        // Most clip interaction methods moved to ClipUI itself
        // Only keep methods that TimelineEditorUI needs to manage selection
        
        public void HandleClipSelection(ClipUI clipUI)
        {
            if (!selectedClips.Contains(clipUI))
            {
                // Multi-select logic can be added here later (Ctrl+click, etc.)
                ClearSelection();
                selectedClips.Add(clipUI);
                clipUI.SetSelected(true);
                OnSelectionChanged?.Invoke(selectedClips);
            }
        }

        private void ClearSelection()
        {
            foreach (var clip in selectedClips)
            {
                clip.SetSelected(false);
            }
            selectedClips.Clear();
            OnSelectionChanged?.Invoke(selectedClips);
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

        private void DisableTimelineInteraction()
        {
            // Disable scroll rect
            SetScrollRectEnabled(false);
        }

        private void EnableTimelineInteraction()
        {
            // Enable scroll rect
            SetScrollRectEnabled(true);
        }
        
        /// <summary>
        /// Called when any clip starts being interacted with (drag/resize)
        /// </summary>
        public void OnClipStartInteraction(ClipUI clipUI)
        {
            DisableTimelineInteraction();
        }
        
        /// <summary>
        /// Called when any clip ends being interacted with (drag/resize)
        /// </summary>
        public void OnClipEndInteraction(ClipUI clipUI)
        {
            EnableTimelineInteraction();
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
        
        private void SetScrollRectEnabled(bool enabled)
        {
            if (timelineScrollRect != null)
            {
                // Instead of disabling the entire ScrollRect (which can cause issues),
                // just disable its interaction components
                timelineScrollRect.horizontal = enabled;
                timelineScrollRect.vertical = enabled;

            }
        }

        #endregion
    }
}