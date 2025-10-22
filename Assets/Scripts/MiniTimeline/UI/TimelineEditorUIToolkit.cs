using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using MiniTimeline.Core;
using MiniTimeline.Serialization;
using MiniTimeline.UI.Commands;
using Core.UI.FormSubmit;
using Core.UI.Core.Helpers;
using Core.Behaviors.Command;
using MiniTimeline.UI.FormDefinitions;
using Core.UI.FormSubmit.Fields;
using BindingContextCore = MiniTimeline.Core.BindingContext;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Main in-game timeline editor UI using UI Toolkit
    /// </summary>
    public class TimelineEditorUIToolkit : MonoBehaviour, ITimelineEditorUI
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string rootElementName = "timeline-editor";

        [Header("Editor Settings")]
        [SerializeField] private float pixelsPerSecond = 100f;
        [SerializeField] private float minZoom = 0.1f;
        [SerializeField] private float maxZoom = 5f;
        [SerializeField] private float snapThreshold = 0.1f;
        [SerializeField] private bool enableFrameSnap = true;

        [Header("USS Classes")]
        [SerializeField] private string timelineContainerClass = "timeline-container";
        [SerializeField] private string rulerContainerClass = "ruler-container";
        [SerializeField] private string tracksContainerClass = "tracks-container";
        [SerializeField] private string controlsContainerClass = "controls-container";

        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset trackTemplate;
        [SerializeField] private StyleSheet trackStyleSheet;
        [SerializeField] private VisualTreeAsset clipTemplate;
        [SerializeField] private StyleSheet clipStyleSheet;
        [SerializeField] private VisualTreeAsset rulerTemplate;
        [SerializeField] private StyleSheet rulerStyleSheet;

        [Header("Debug")]
        [SerializeField] bool isDebug;

        // Core references
        [SerializeField] private MiniTimelineDirector director;
        private TimelineCommandManager commandManager;

        // UI Toolkit Elements
        private VisualElement rootElement;
        private VisualElement timelineContainer;
        private VisualElement rulerContainer;
        private VisualElement tracksContainer;
        private ScrollView timelineScrollView;
        private VisualElement playheadElement;
        
        // Controls
        private Button addTrackButton;
        private Button bindingManagerButton;
        private Button saveButton;
        private Button loadButton;
        private Button playButton;
        private Button pauseButton;
        private Button stopButton;
        private Button undoButton;
        private Button redoButton;
        
        private Slider timeSlider;
        private Label timeLabel;
        private Slider zoomSlider;
        private Label zoomLabel;
        private Label statusLabel;

        // UI State
        private float currentZoom = 1f;
        private float timelineWidth;
        private bool isPlayheadDragging = false;

        // Selection and editing
        private List<ClipUIToolkit> selectedClips = new List<ClipUIToolkit>();
        private TimelineRulerToolkit ruler;

        // Performance optimization
        private readonly List<TrackUIToolkit> trackUIs = new List<TrackUIToolkit>();
        private readonly Dictionary<string, ClipUIToolkit> clipUILookup = new Dictionary<string, ClipUIToolkit>();

        #region Events

        public event Action<float> OnTimeChanged;
        public event Action<List<ClipUIToolkit>> OnSelectionChanged;
        public event Action<ClipUIToolkit> OnClipContextMenu;

        #endregion

        #region Properties

        public float PixelsPerSecond => pixelsPerSecond * currentZoom;
        public float CurrentZoom => currentZoom;
        public float TimelineWidth => timelineWidth;
        public MiniTimelineDirector Director => director;
        public IReadOnlyList<ClipUIToolkit> SelectedClips => selectedClips;
        public bool EnableFrameSnap => enableFrameSnap;
        public VisualElement TimelineContainer => timelineContainer;
        public VisualElement TracksContainer => tracksContainer;
        
        // Template access for child components
        public VisualTreeAsset ClipTemplate => clipTemplate;
        public StyleSheet ClipStyleSheet => clipStyleSheet;
        public VisualTreeAsset RulerTemplate => rulerTemplate;
        public StyleSheet RulerStyleSheet => rulerStyleSheet;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize UI creation helper to prevent threading issues
            UICreationHelper.Initialize(this);

            // Initialize command manager
            commandManager = new TimelineCommandManager();

            // Subscribe to command manager events
            SetupCommandManagerEvents();

            // Initialize UI components
            InitializeUIToolkit();
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

            // Cleanup UI Toolkit event registrations
            CleanupUIEvents();
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
                undoButton.SetEnabled(commandManager.CanUndo);

            if (redoButton != null)
                redoButton.SetEnabled(commandManager.CanRedo);
        }

        #endregion

        #region UI Toolkit Initialization

        private void InitializeUIToolkit()
        {
            // Load templates if not assigned
            // LoadTemplatesIfNeeded();
            
            // Get or create UI Document
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found. Please add a UIDocument component to this GameObject.");
                return;
            }

            // Get root element
            rootElement = uiDocument.rootVisualElement;
            if (!string.IsNullOrEmpty(rootElementName))
            {
                rootElement = rootElement.Q(rootElementName);
                if (rootElement == null)
                {
                    Debug.LogError($"Root element '{rootElementName}' not found in UI Document.");
                    return;
                }
            }

            // Setup UI element hierarchy
            SetupUIHierarchy();

            // Query UI elements
            QueryUIElements();

            // Setup event handlers
            SetupUIEvents();

            // Initialize ruler
            InitializeRuler();

            // Setup initial zoom and layout
            UpdateTimelineLayout();

            // Initialize button states
            UpdateUndoRedoButtonStates();

            // Initialize context menu
            InitializeContextMenu();
        }

        // private void LoadTemplatesIfNeeded()
        // {
        //     // Load track template if not assigned
        //     if (trackTemplate == null)
        //     {
        //         trackTemplate = Resources.Load<VisualTreeAsset>("UI/TrackUIToolkit");
        //         if (trackTemplate == null && isDebug)
        //         {
        //             Debug.LogWarning("TrackUIToolkit template not found in Resources/UI/. Using fallback creation.");
        //         }
        //     }
            
        //     if (trackStyleSheet == null)
        //     {
        //         trackStyleSheet = Resources.Load<StyleSheet>("UI/TrackUIToolkit");
        //         if (trackStyleSheet == null && isDebug)
        //         {
        //             Debug.LogWarning("TrackUIToolkit stylesheet not found in Resources/UI/. Using default styling.");
        //         }
        //     }
            
        //     // Load clip template if not assigned
        //     if (clipTemplate == null)
        //     {
        //         clipTemplate = Resources.Load<VisualTreeAsset>("UI/ClipUIToolkit");
        //         if (clipTemplate == null && isDebug)
        //         {
        //             Debug.LogWarning("ClipUIToolkit template not found in Resources/UI/. Using fallback creation.");
        //         }
        //     }
            
        //     if (clipStyleSheet == null)
        //     {
        //         clipStyleSheet = Resources.Load<StyleSheet>("UI/ClipUIToolkit");
        //         if (clipStyleSheet == null && isDebug)
        //         {
        //             Debug.LogWarning("ClipUIToolkit stylesheet not found in Resources/UI/. Using default styling.");
        //         }
        //     }
        // }

        private void SetupUIHierarchy()
        {
            // Create main containers if they don't exist
            if (timelineContainer == null)
            {
                timelineContainer = rootElement.Q(className: timelineContainerClass);
                if (timelineContainer == null)
                {
                    timelineContainer = new VisualElement();
                    timelineContainer.AddToClassList(timelineContainerClass);
                    timelineContainer.name = "timeline-container";
                    rootElement.Add(timelineContainer);
                }
            }

            if (rulerContainer == null)
            {
                rulerContainer = rootElement.Q(className: rulerContainerClass);
                if (rulerContainer == null)
                {
                    rulerContainer = new VisualElement();
                    rulerContainer.AddToClassList(rulerContainerClass);
                    rulerContainer.name = "ruler-container";
                    timelineContainer.Add(rulerContainer);
                }
            }

            if (tracksContainer == null)
            {
                tracksContainer = rootElement.Q(className: tracksContainerClass);
                if (tracksContainer == null)
                {
                    tracksContainer = new VisualElement();
                    tracksContainer.AddToClassList(tracksContainerClass);
                    tracksContainer.name = "tracks-container";
                    timelineContainer.Add(tracksContainer);
                }
            }

            // Create scroll view for timeline
            if (timelineScrollView == null)
            {
                timelineScrollView = rootElement.Q<ScrollView>("timeline-scroll");
                if (timelineScrollView == null)
                {
                    timelineScrollView = new ScrollView();
                    timelineScrollView.name = "timeline-scroll";
                    timelineScrollView.mode = ScrollViewMode.Horizontal;
                    timelineScrollView.Add(timelineContainer);
                    rootElement.Add(timelineScrollView);
                }
            }
        }

        private void QueryUIElements()
        {
            // Query control buttons
            addTrackButton = rootElement.Q<Button>("add-track-button");
            bindingManagerButton = rootElement.Q<Button>("binding-manager-button");
            saveButton = rootElement.Q<Button>("save-button");
            loadButton = rootElement.Q<Button>("load-button");
            playButton = rootElement.Q<Button>("play-button");
            pauseButton = rootElement.Q<Button>("pause-button");
            stopButton = rootElement.Q<Button>("stop-button");
            undoButton = rootElement.Q<Button>("undo-button");
            redoButton = rootElement.Q<Button>("redo-button");

            // Query time controls
            timeSlider = rootElement.Q<Slider>("time-slider");
            timeLabel = rootElement.Q<Label>("time-label");
            
            // Query zoom controls
            zoomSlider = rootElement.Q<Slider>("zoom-slider");
            zoomLabel = rootElement.Q<Label>("zoom-label");
            
            // Query status label
            statusLabel = rootElement.Q<Label>("status-text");
            
            // Query playhead
            playheadElement = rootElement.Q("playhead");
        }

        private void SetupUIEvents()
        {
            // Setup playback controls
            if (playButton != null)
                playButton.clicked += () => director?.Play();

            if (pauseButton != null)
                pauseButton.clicked += () => director?.Pause();

            if (stopButton != null)
                stopButton.clicked += () => director?.Stop();

            // Setup undo/redo controls
            if (undoButton != null)
                undoButton.clicked += () => commandManager?.Undo();

            if (redoButton != null)
                redoButton.clicked += () => commandManager?.Redo();

            // Setup add track button
            if (addTrackButton != null)
                addTrackButton.clicked += ShowAddTrackForm;

            // Setup binding manager button
            if (bindingManagerButton != null)
                bindingManagerButton.clicked += ShowBindingManagerForm;

            // Setup save/load buttons
            if (saveButton != null)
                saveButton.clicked += ShowSaveProjectForm;

            if (loadButton != null)
                loadButton.clicked += ShowLoadProjectForm;

            // Setup time slider
            if (timeSlider != null)
            {
                timeSlider.RegisterValueChangedCallback(OnTimeSliderChanged);
            }

            // Setup zoom slider
            if (zoomSlider != null)
            {
                zoomSlider.value = currentZoom;
                zoomSlider.RegisterValueChangedCallback(OnZoomSliderChanged);
            }

            // Setup timeline interaction events
            SetupTimelineEvents();
        }

        private void SetupTimelineEvents()
        {
            if (timelineContainer != null)
            {
                // Handle mouse events for timeline interaction
                timelineContainer.RegisterCallback<MouseDownEvent>(OnTimelineMouseDown);
                timelineContainer.RegisterCallback<MouseMoveEvent>(OnTimelineMouseMove);
                timelineContainer.RegisterCallback<MouseUpEvent>(OnTimelineMouseUp);
                timelineContainer.RegisterCallback<WheelEvent>(OnTimelineWheel);
            }
        }

        private void CleanupUIEvents()
        {
            // Cleanup UI Toolkit event registrations
            if (timelineContainer != null)
            {
                timelineContainer.UnregisterCallback<MouseDownEvent>(OnTimelineMouseDown);
                timelineContainer.UnregisterCallback<MouseMoveEvent>(OnTimelineMouseMove);
                timelineContainer.UnregisterCallback<MouseUpEvent>(OnTimelineMouseUp);
                timelineContainer.UnregisterCallback<WheelEvent>(OnTimelineWheel);
            }

            if (timeSlider != null)
            {
                timeSlider.UnregisterValueChangedCallback(OnTimeSliderChanged);
            }

            if (zoomSlider != null)
            {
                zoomSlider.UnregisterValueChangedCallback(OnZoomSliderChanged);
            }
        }

        private void InitializeRuler()
        {
            // Create ruler if it doesn't exist
            if (ruler == null && rulerContainer != null)
            {
                ruler = new TimelineRulerToolkit();
                ruler.Initialize(this, rulerContainer, rulerTemplate, rulerStyleSheet);
            }
        }

        private void InitializeContextMenu()
        {
            // TODO: Implement context menu for UI Toolkit
            // This would be a VisualElement-based context menu instead of a prefab
        }

        #endregion

        #region Timeline Event Handlers

        private void OnTimelineMouseDown(MouseDownEvent evt)
        {
            Vector2 localPos = evt.localMousePosition;

            if (IsRulerHit(localPos))
            {
                OnRulerTouched(localPos);
                evt.StopPropagation();
            }
        }

        private void OnTimelineMouseMove(MouseMoveEvent evt)
        {
            if (isPlayheadDragging)
            {
                Vector2 localPos = evt.localMousePosition;
                ScrubToPosition(localPos.x);
            }
        }

        private void OnTimelineMouseUp(MouseUpEvent evt)
        {
            if (isPlayheadDragging)
            {
                isPlayheadDragging = false;
                EnableTimelineInteraction();
            }
        }

        private void OnTimelineWheel(WheelEvent evt)
        {
            // Handle zoom with wheel
            float zoomDelta = -evt.delta.y * 0.001f;
            float newZoom = currentZoom + zoomDelta;
            SetZoom(newZoom);
            evt.StopPropagation();
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

        [ContextMenu("Build Timeline UI")]
        public void BuildTimelineUI()
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
                CreateTrackUI(track);
            }

            // Update ruler
            ruler?.Rebuild(director.Length, director.Project.frameRate);
        }

        private void CreateTrackUI(IMiniTrack track)
        {
            if (tracksContainer == null)
            {
                return;
            }

            var trackUI = new TrackUIToolkit();
            trackUI.Initialize(this, track, trackTemplate, trackStyleSheet);
            trackUIs.Add(trackUI);
            
            // Add the track element to the tracks container
            tracksContainer.Add(trackUI.TrackElement);
        }

        private void ClearTimelineUI()
        {
            // Clear track UIs
            foreach (var trackUI in trackUIs)
            {
                trackUI?.Dispose();
            }
            trackUIs.Clear();
            clipUILookup.Clear();
            selectedClips.Clear();

            // Clear tracks container
            tracksContainer?.Clear();
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
                
                // Ensure minimum width for the timeline
                timelineWidth = Mathf.Max(timelineWidth, 1000f);

                // Update timeline container size using UI Toolkit layout
                if (timelineContainer != null)
                {
                    timelineContainer.style.width = timelineWidth;
                }
            }
            else
            {
                // Set a default width when no project is loaded
                timelineWidth = 1000f;
                if (timelineContainer != null)
                {
                    timelineContainer.style.width = timelineWidth;
                }
            }
        }

        #endregion

        #region Utility Methods

        private Vector2 ScreenToTimelineLocal(Vector2 screenPos)
        {
            // Convert screen position to timeline container's local space
            if (timelineContainer != null)
            {
                return timelineContainer.WorldToLocal(screenPos);
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

            Rect rulerBounds = rulerContainer.contentRect;
            return rulerBounds.Contains(localPos);
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
        /// Undo the last 
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

        public void HandleClipSelection(ClipUIToolkit clipUI)
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

        public void HandleClipContextMenu(ClipUIToolkit clipUI)
        {
            // TODO: Implement clip context menu
            Debug.Log($"Clip context menu requested for clip: {clipUI}");
            OnClipContextMenu?.Invoke(clipUI);
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
            DisableTimelineInteraction();
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
            // Disable scroll view
            SetScrollViewEnabled(false);
        }

        private void EnableTimelineInteraction()
        {
            // Enable scroll view
            SetScrollViewEnabled(true);
        }

        /// <summary>
        /// Called when any clip starts being interacted with (drag/resize)
        /// </summary>
        public void OnClipStartInteraction(ClipUIToolkit clipUI)
        {
            DisableTimelineInteraction();
        }

        /// <summary>
        /// Called when any clip ends being interacted with (drag/resize)
        /// </summary>
        public void OnClipEndInteraction(ClipUIToolkit clipUI)
        {
            EnableTimelineInteraction();
        }

        private void UpdateTimeDisplay(float time)
        {
            if (timeLabel != null)
            {
                timeLabel.text = FormatTime(time);
            }

            if (timeSlider != null && director != null)
            {
                timeSlider.value = time / director.Length;
            }

            // Update playhead position
            UpdatePlayheadPosition(time);
        }

        private void UpdatePlayheadPosition(float time)
        {
            if (playheadElement != null)
            {
                float position = TimeToPosition(time);
                playheadElement.style.left = position;
            }
        }

        private void UpdatePlaybackControls(PlaybackState state)
        {
            // Update button states based on playback state
            if (playButton != null)
                playButton.SetEnabled(state != PlaybackState.Playing);
            
            if (pauseButton != null)
                pauseButton.SetEnabled(state == PlaybackState.Playing);
            
            if (stopButton != null)
                stopButton.SetEnabled(state != PlaybackState.Stopped);
        }

        private void OnTimeSliderChanged(ChangeEvent<float> evt)
        {
            if (director != null && !isPlayheadDragging)
            {
                director.Seek(evt.newValue * director.Length);
            }
        }

        private void OnZoomSliderChanged(ChangeEvent<float> evt)
        {
            SetZoom(evt.newValue);
            
            // Update zoom label
            if (zoomLabel != null)
            {
                zoomLabel.text = $"{evt.newValue * 100:F0}%";
            }
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int frames = Mathf.FloorToInt((time % 1f) * 30f); // Assuming 30 FPS for display

            return $"{minutes:00}:{seconds:00}:{frames:00}";
        }

        private void SetScrollViewEnabled(bool enabled)
        {
            if (timelineScrollView != null)
            {
                timelineScrollView.SetEnabled(enabled);
            }
        }

        #endregion

        #region Form Methods - Reusing from original implementation

        /// <summary>
        /// Show form for adding a new track to the timeline
        /// </summary>
        private async void ShowAddTrackForm()
        {
            if (director?.Project == null)
            {
                Debug.LogError("Cannot add track: No project loaded");
                return;
            }

            // Get form field definitions from TrackFormDefinitions
            var fieldDefinitions = TrackFormDefinitions.GetCreateTrackFields();

            // Get localized title
            var titleHandle = new LocalizedString("UI", "form.addTrack.title").GetLocalizedStringAsync();
            await titleHandle.Task;

            // Show the form using FormSubmitPanelUIToolkit singleton
            FormSubmitPanelUIToolkit.Instance.Show(
                titleHandle.Result,
                fieldDefinitions,
                OnAddTrackFormSubmitted,
                OnAddTrackFormCancelled,
                transform
            );
        }

        /// <summary>
        /// Handle track creation form submission
        /// </summary>
        private void OnAddTrackFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                // Extract form data
                string trackType = formData.ContainsKey("trackType") ? formData["trackType"].ToString() : MiniTimelineConstants.TRACK_ANIM;
                string trackName = formData.ContainsKey("trackName") ? formData["trackName"].ToString() : "New Track";
                string bindKey = formData.ContainsKey("bindKey") ? formData["bindKey"].ToString() : "";
                bool enabled = formData.ContainsKey("enabled") ? Convert.ToBoolean(formData["enabled"]) : true;

                // Generate unique ID for the track
                string trackId = Guid.NewGuid().ToString();

                // Determine track order (place at the end)
                int trackOrder = director.Project.tracks.Count > 0 ? director.Project.tracks.Max(t => t.order) + 1 : 0;

                // Create track data
                var trackData = new TrackData
                {
                    id = trackId,
                    type = trackType,
                    bindKey = bindKey,
                    enabled = enabled,
                    order = trackOrder,
                    clips = new List<ClipData>()
                };

                // Create and execute add track command
                var addTrackCommand = new AddTrackCommand(director, trackData, this);
                ExecuteCommand(addTrackCommand);

                // Debug.Log($"Added new {TrackUIHelper.GetTrackDisplayName(trackType)} track: {trackName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create track: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle track creation form cancellation
        /// </summary>
        private void OnAddTrackFormCancelled()
        {
            // Debug.Log("Add track cancelled");
        }

        /// <summary>
        /// Show the main binding manager form
        /// </summary>
        private async void ShowBindingManagerForm()
        {
            if (director?.BindingContext == null)
            {
                Debug.LogError("Cannot show binding manager: No binding context available");
                return;
            }

            // Debug.Log("Opening main binding manager form");

            // Get current BindingContext
            var bindingContext = director.BindingContext;

            // Create form fields for binding management
            var fieldDefinitions = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "bindingList",
                    type = "textarea",
                    label = "Current Scene Bindings",
                    required = false,
                    defaultValue = FormatBindingsForDisplay(bindingContext),
                    tooltip = "List of currently registered scene object bindings",
                    options = new Dictionary<string, object>
                    {
                        { "readonly", true }
                    }
                },
                new FormFieldDefinition
                {
                    name = "newBindingKey",
                    type = "text",
                    label = "New Binding Key",
                    required = false,
                    placeholder = "Enter binding key (e.g., 'character', 'camera')...",
                    tooltip = "Unique key name for the new binding"
                },
                new FormFieldDefinition
                {
                    name = "newBindingObject",
                    type = "text",
                    label = "Target Object Name",
                    required = false,
                    placeholder = "Enter GameObject name in scene...",
                    tooltip = "Name of the GameObject to bind to this key"
                },
                new FormFieldDefinition
                {
                    name = "removeBinding",
                    type = "selectbox",
                    label = "Remove Binding",
                    required = false,
                    tooltip = "Select a binding to remove from the scene",
                    options = new Dictionary<string, object>
                    {
                        { "items", bindingContext?.GetKeys().ToList() ?? new List<string>() },
                        { "placeholder", "Select binding to remove..." }
                    }
                },
                new FormFieldDefinition
                {
                    name = "autoDetectObjects",
                    type = "button",
                    label = "Auto-Detect Scene Objects",
                    required = false,
                    defaultValue = "Scan Scene for Common Objects",
                    tooltip = "Automatically detect and bind common GameObjects in the scene",
                    options = new Dictionary<string, object>
                    {
                        { "action", "autoDetectBindings" }
                    }
                }
            };

            // Get localized title
            var titleHandle = new LocalizedString("UI", "form.bindingManager.title").GetLocalizedStringAsync();
            await titleHandle.Task;

            // Show the binding manager form
            FormSubmitPanelUIToolkit.Instance.Show(
                titleHandle.Result,
                fieldDefinitions,
                OnBindingManagerFormSubmitted,
                OnBindingManagerFormCancelled,
                transform
            );
        }

        /// <summary>
        /// Format the current bindings for display in the info field
        /// </summary>
        private string FormatBindingsForDisplay(BindingContextCore bindingContext)
        {
            if (bindingContext == null)
            {
                return "No binding context available.";
            }

            var keys = bindingContext.GetKeys().ToList();
            if (keys.Count == 0)
            {
                return "No bindings currently registered.\n\nTip: Use 'Auto-Detect Scene Objects' to automatically find common GameObjects, or manually add bindings below.";
            }

            var displayText = "Active Scene Bindings:\n\n";
            foreach (var key in keys)
            {
                if (bindingContext.TryResolve(key, out UnityEngine.Object obj))
                {
                    string objectName = obj != null ? obj.name : "<null>";
                    string objectType = obj != null ? obj.GetType().Name : "missing";
                    string status = obj != null ? "✓" : "✗";
                    displayText += $"{status} {key} → {objectName} ({objectType})\n";
                }
                else
                {
                    displayText += $"✗ {key} → <not resolved>\n";
                }
            }

            displayText += "\nTip: Tracks use these binding keys to reference GameObjects in the scene.";
            return displayText;
        }

        /// <summary>
        /// Handle binding manager form submission
        /// </summary>
        private void OnBindingManagerFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                // Debug.Log("Main binding manager form submitted with data:");
                //foreach (var kvp in formData)
                //{
                //    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                //}

                // Check if this is an auto-detect action
                if (formData.ContainsKey("autoDetectObjects") && formData["autoDetectObjects"].ToString() == "autoDetectBindings")
                {
                    AutoDetectSceneBindings();
                    ShowBindingManagerForm(); // Refresh the form
                    return;
                }

                bool bindingsChanged = false;

                // Handle adding new binding
                if (formData.ContainsKey("newBindingKey") && formData.ContainsKey("newBindingObject"))
                {
                    string newKey = formData["newBindingKey"]?.ToString();
                    string objectName = formData["newBindingObject"]?.ToString();

                    if (!string.IsNullOrEmpty(newKey) && !string.IsNullOrEmpty(objectName))
                    {
                        // Find the GameObject by name
                        GameObject targetObject = GameObject.Find(objectName);
                        if (targetObject != null)
                        {
                            AddBinding(newKey, targetObject);
                            bindingsChanged = true;
                            // Debug.Log($"Added new binding: {newKey} → {targetObject.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find GameObject with name '{objectName}' in scene");
                        }
                    }
                }

                // Handle removing binding
                if (formData.ContainsKey("removeBinding"))
                {
                    string removeKey = formData["removeBinding"]?.ToString();
                    if (!string.IsNullOrEmpty(removeKey))
                    {
                        RemoveBinding(removeKey);
                        bindingsChanged = true;
                        // Debug.Log($"Removed binding: {removeKey}");
                    }
                }

                if (bindingsChanged)
                {
                    // Debug.Log("Scene bindings updated successfully");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to process binding manager changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle binding manager form cancellation
        /// </summary>
        private void OnBindingManagerFormCancelled()
        {
            // Debug.Log("Binding manager cancelled");
        }

        /// <summary>
        /// Auto-detect common GameObjects in the scene and bind them using BindableObject components
        /// </summary>
        private void AutoDetectSceneBindings()
        {
            if (director?.BindingContext == null) return;

            director.BindingContext.AutoBind();
        }

        /// <summary>
        /// Add a new binding to the BindingContext
        /// </summary>
        private void AddBinding(string key, UnityEngine.Object targetObject)
        {
            var bindingContext = director?.BindingContext;
            if (bindingContext != null)
            {
                bindingContext.Bind(key, targetObject);
                Debug.Log($"Added binding: {key} → {targetObject?.name ?? "<null>"}");
            }
            else
            {
                Debug.LogWarning($"Cannot add binding: BindingContext not available");
            }
        }

        /// <summary>
        /// Remove a binding from the BindingContext
        /// </summary>
        private void RemoveBinding(string key)
        {
            var bindingContext = director?.BindingContext;
            if (bindingContext != null)
            {
                bindingContext.Unbind(key);
                // Debug.Log($"Removed binding: {key}");
            }
            else
            {
                Debug.LogWarning($"Cannot remove binding: BindingContext not available");
            }
        }

        /// <summary>
        /// Show the save project form
        /// </summary>
        private async void ShowSaveProjectForm()
        {
            if (director?.Project == null)
            {
                Debug.LogError("Cannot save project: No project loaded");
                return;
            }

            Debug.Log("Opening save project form");

            // Create form fields for saving
            var fieldDefinitions = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "filename",
                    type = "text",
                    label = "Filename",
                    required = true,
                    defaultValue = director.Project.name ?? "timeline_project",
                    placeholder = "Enter project filename...",
                    tooltip = "Name of the file to save (without extension)"
                },
                new FormFieldDefinition
                {
                    name = "prettyPrint",
                    type = "checkbox",
                    label = "Pretty Print JSON",
                    required = false,
                    defaultValue = "true",
                    tooltip = "Format JSON for better readability"
                },
                new FormFieldDefinition
                {
                    name = "projectInfo",
                    type = "textarea",
                    label = "Project Information",
                    required = false,
                    defaultValue = GetProjectInfoForDisplay(),
                    tooltip = "Current project details (read-only)",
                    options = new Dictionary<string, object>
                    {
                        { "readonly", true }
                    }
                }
            };

            // Get localized title
            var titleHandle = new LocalizedString("UI", "form.saveProject.title").GetLocalizedStringAsync();
            await titleHandle.Task;

            // Show the save form
            FormSubmitPanelUIToolkit.Instance.Show(
                titleHandle.Result,
                fieldDefinitions,
                OnSaveProjectFormSubmitted,
                OnSaveProjectFormCancelled,
                transform
            );
        }

        /// <summary>
        /// Handle save project form submission
        /// </summary>
        private void OnSaveProjectFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                // Debug.Log("Save project form submitted with data:");
                //foreach (var kvp in formData)
                //{
                //    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                //}

                string filename = formData.ContainsKey("filename") ? formData["filename"].ToString() : "timeline_project";
                bool prettyPrint = formData.ContainsKey("prettyPrint") ? Convert.ToBoolean(formData["prettyPrint"]) : true;

                // Ensure filename has .json extension
                if (!filename.EndsWith(".json"))
                {
                    filename += ".json";
                }

                // For now, save to persistent data path (in a real game you'd want file browser)
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

                // Save the project (automatically updates from runtime tracks)
                bool success = ProjectSerializer.SaveToFile(director.Project, director, filePath);

                if (success)
                {
                    // Debug.Log($"Project saved successfully to: {filePath}");
                }
                else
                {
                    Debug.LogError("Failed to save project. Check console for details.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save project: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle save project form cancellation
        /// </summary>
        private void OnSaveProjectFormCancelled()
        {
            // Debug.Log("Save project cancelled");
        }

        /// <summary>
        /// Show the load project form
        /// </summary>
        private async void ShowLoadProjectForm()
        {
            // Debug.Log("Opening load project form");

            // Get list of available project files in persistent data path
            string[] projectFiles = GetAvailableProjectFiles();

            // Create form fields for loading
            var fieldDefinitions = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "availableFiles",
                    type = "selectbox",
                    label = "Select Project File",
                    required = false,
                    placeholder = "Choose from available files...",
                    tooltip = "Select a project file from the list of available files",
                    options = new Dictionary<string, object>
                    {
                        { "items", projectFiles.ToList() },
                        { "placeholder", "Select a project file..." }
                    }
                },
                new FormFieldDefinition
                {
                    name = "filename",
                    type = "text",
                    label = "Or Enter Filename Manually",
                    required = false,
                    placeholder = "Enter project filename...",
                    tooltip = "Alternatively, manually enter the filename (with or without .json extension)"
                },
                new FormFieldDefinition
                {
                    name = "filesInfo",
                    type = "textarea",
                    label = "Available Files Info",
                    required = false,
                    defaultValue = GetAvailableFilesDisplay(projectFiles),
                    tooltip = "Information about available project files",
                    options = new Dictionary<string, object>
                    {
                        { "readonly", true }
                    }
                },
                new FormFieldDefinition
                {
                    name = "replaceBindings",
                    type = "checkbox",
                    label = "Auto-Update Scene Bindings",
                    required = false,
                    defaultValue = "true",
                    tooltip = "Automatically update scene bindings after loading the project"
                }
            };

            // Get localized title
            var titleHandle = new LocalizedString("UI", "form.loadProject.title").GetLocalizedStringAsync();
            await titleHandle.Task;

            // Show the load form
            FormSubmitPanelUIToolkit.Instance.Show(
                titleHandle.Result,
                fieldDefinitions,
                OnLoadProjectFormSubmitted,
                OnLoadProjectFormCancelled,
                transform
            );
        }

        /// <summary>
        /// Handle load project form submission
        /// </summary>
        private void OnLoadProjectFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                // Debug.Log("Load project form submitted with data:");
                //foreach (var kvp in formData)
                //{
                //    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                //}

                // Try to get filename from dropdown first, then manual entry
                string filename = "";

                if (formData.ContainsKey("availableFiles") && !string.IsNullOrEmpty(formData["availableFiles"]?.ToString()))
                {
                    filename = formData["availableFiles"].ToString();
                    // Debug.Log($"Using selected file from dropdown: {filename}");
                }
                else if (formData.ContainsKey("filename") && !string.IsNullOrEmpty(formData["filename"]?.ToString()))
                {
                    filename = formData["filename"].ToString();
                    Debug.Log($"Using manually entered filename: {filename}");
                }

                bool replaceBindings = formData.ContainsKey("replaceBindings") ? Convert.ToBoolean(formData["replaceBindings"]) : true;

                if (string.IsNullOrEmpty(filename))
                {
                    Debug.LogError("No filename provided for loading. Please select a file from the dropdown or enter a filename manually.");
                    return;
                }

                // Ensure filename has .json extension
                if (!filename.EndsWith(".json"))
                {
                    filename += ".json";
                }

                // For now, load from persistent data path
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

                // Load the project
                var loadedProject = ProjectSerializer.LoadFromFile(filePath);

                if (loadedProject != null)
                {
                    director.SetProject(loadedProject);

                    // Auto-update bindings if requested
                    if (replaceBindings)
                    {
                        AutoDetectSceneBindings();
                    }

                    Debug.Log($"Project '{loadedProject.name}' loaded successfully from: {filePath}");
                }
                else
                {
                    Debug.LogError("Failed to load project. Check console for details.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load project: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle load project form cancellation
        /// </summary>
        private void OnLoadProjectFormCancelled()
        {
            Debug.Log("Load project cancelled");
        }

        /// <summary>
        /// Get project information for display in save form
        /// </summary>
        private string GetProjectInfoForDisplay()
        {
            if (director?.Project == null)
            {
                return "No project loaded.";
            }

            // Update project data from runtime tracks to ensure accuracy
            ProjectSerializer.UpdateProjectFromRuntimeTracks(director.Project, director);

            var project = director.Project;
            var info = $"Project: {project.name}\n";
            info += $"Length: {project.length}s\n";
            info += $"Frame Rate: {project.frameRate} FPS\n";
            info += $"Tracks: {project.tracks.Count}\n";

            // Now we can get accurate clip count from the updated project data
            int totalClips = 0;
            foreach (var track in project.tracks)
            {
                totalClips += track.clips.Count;
            }

            info += $"Total Clips: {totalClips}\n";

            return info;
        }

        /// <summary>
        /// Get available project files in persistent data path
        /// </summary>
        private string[] GetAvailableProjectFiles()
        {
            try
            {
                string persistentPath = Application.persistentDataPath;
                if (System.IO.Directory.Exists(persistentPath))
                {
                    return System.IO.Directory.GetFiles(persistentPath, "*.json")
                        .Select(System.IO.Path.GetFileName)
                        .ToArray();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to get available project files: {ex.Message}");
            }

            return new string[0];
        }

        /// <summary>
        /// Format available files for display in load form
        /// </summary>
        private string GetAvailableFilesDisplay(string[] files)
        {
            if (files.Length == 0)
            {
                return $"No project files found in:\n{Application.persistentDataPath}\n\nTip: Save a project first, or manually place .json files in this directory.";
            }

            var display = $"Found {files.Length} project file(s) in:\n{Application.persistentDataPath}\n\n";
            display += "Use the dropdown above to select a file, or enter a filename manually.";

            return display;
        }

        /// <summary>
        /// Select a track in the timeline
        /// </summary>
        public void SelectTrack(IMiniTrack track)
        {
            // TODO: Implement track selection logic
            Debug.Log($"Track selected: {track.Id}");
        }

        /// <summary>
        /// Show context menu for a track at the specified screen position
        /// </summary>
        public void ShowTrackContextMenu(IMiniTrack track, Vector2 screenPosition)
        {
            // TODO: Implement track context menu
            Debug.Log($"Show context menu for track: {track.Id} at position: {screenPosition}");
        }

        #endregion
    }
}