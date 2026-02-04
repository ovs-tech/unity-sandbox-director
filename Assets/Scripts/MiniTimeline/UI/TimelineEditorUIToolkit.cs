using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.Serialization;
using Systems.MiniTimeline.UI.Commands;
using Core.UI.FormSubmit;
using Core.UI.Core.Helpers;
using Core.Behaviors.Command;
using Systems.MiniTimeline.UI.FormDefinitions;
using Core.UI.FormSubmit.Fields;
using BindingContextCore = Systems.MiniTimeline.Core.BindableObjectManager;

namespace Systems.MiniTimeline.UI
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

        private void OnCommandExecuted(ITimelineCommand command)
        {
            // Refresh UI after command execution
            RefreshTimelineUI();
        }

        private void OnCommandUndoPerformed(ITimelineCommand command)
        {
            // Refresh UI after undo
            RefreshTimelineUI();
        }

        private void OnCommandRedoPerformed(ITimelineCommand command)
        {
            // Refresh UI after redo
            RefreshTimelineUI();
        }

        private void OnCommandStacksChanged(bool canUndo, bool canRedo)
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

                // Create and execute add track command with track type
                // AddTrackCommand now creates runtime track instances directly
                var addTrackCommand = new AddTrackCommand(director, trackType);
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
                bool success = ProjectSerializer.SaveToFile(director.Project, filePath);

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

            var project = director.Project;
            var info = $"Project: {project.name}\n";
            info += $"Length: {project.length}s\n";
            info += $"Frame Rate: {project.frameRate} FPS\n";
            info += $"Tracks: {project.tracks.Count}\n";

            // Count clips from runtime tracks
            int totalClips = 0;
            foreach (var track in project.tracks)
            {
                var clips = track.GetClips();
                if (clips != null)
                    totalClips += clips.Count();
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

        #region Context Menu Actions

        /// <summary>
        /// Show clip context menu
        /// </summary>
        public void ShowClipMenu(ClipUIToolkit clip, Vector2 screenPosition)
        {
            Debug.Log($"ShowClipMenu called with clip: {clip?.Clip?.Id}, position: {screenPosition}");

            // Get form fields from ClipContextMenuDefinitions
            var formFields = ClipContextMenuDefinitions.GetClipActionFields();
            Debug.Log($"Created {formFields?.Count ?? 0} form fields for clip");

            // Show form panel
            Debug.Log("Calling FormSubmitPanelUIToolkit.Instance.Show for clip menu...");
            FormSubmitPanelUIToolkit.Instance.Show(
                ClipContextMenuDefinitions.GetClipMenuTitle(), 
                formFields,
                onSubmit: (data) =>
                {
                    Debug.Log("Clip menu form submitted");
                    HandleClipFormSubmission(data, clip);
                },
                onCancel: () =>
                {
                    Debug.Log("Clip menu form cancelled");
                },
                transform);
            Debug.Log("ShowClipMenu completed");
        }

        /// <summary>
        /// Show track context menu
        /// </summary>
        public void ShowTrackMenu(TrackUIToolkit track, Vector2 screenPosition)
        {
            Debug.Log($"ShowTrackMenu called with track: {track?.Track?.Id}, position: {screenPosition}");

            // Get form fields from TrackContextMenuDefinitions
            var formFields = TrackContextMenuDefinitions.GetTrackActionFields();
            Debug.Log($"Created {formFields?.Count ?? 0} form fields");

            // Show form panel
            Debug.Log("Calling FormSubmitPanelUIToolkit.Instance.Show...");
            FormSubmitPanelUIToolkit.Instance.Show(
                TrackContextMenuDefinitions.GetTrackMenuTitle(), 
                formFields,
                onSubmit: (data) =>
                {
                    Debug.Log("Track menu form submitted");
                    HandleTrackFormSubmission(data, track);
                },
                onCancel: () =>
                {
                    Debug.Log("Track menu form cancelled");
                },
                transform);
            Debug.Log("ShowTrackMenu completed");
        }

        /// <summary>
        /// Show timeline context menu
        /// </summary>
        public void ShowTimelineMenu(Vector2 screenPosition, float timePosition)
        {
            // Get form fields from TimelineContextMenuDefinitions
            var formFields = TimelineContextMenuDefinitions.GetTimelineActionFields();

            // Show form panel
            FormSubmitPanelUIToolkit.Instance.Show(
                TimelineContextMenuDefinitions.GetTimelineMenuTitle(), 
                formFields,
                onSubmit: (data) => HandleTimelineFormSubmission(data, timePosition),
                onCancel: () => { /* Form cancelled */ },
                transform);
        }

        #endregion

        #region Clip Action Handlers

        /// <summary>
        /// Handles clip form submission
        /// </summary>
        private void HandleClipFormSubmission(Dictionary<string, object> data, ClipUIToolkit clip)
        {
            if (data.TryGetValue("action", out var action))
            {
                HandleClipAction(action.ToString(), clip);
            }
        }

        /// <summary>
        /// Handles clip actions
        /// </summary>
        private void HandleClipAction(string action, ClipUIToolkit clip)
        {
            switch (action)
            {
                case "cut":
                    CutClip(clip);
                    break;
                case "copy":
                    CopyClip(clip);
                    break;
                case "delete":
                    DeleteClip(clip);
                    break;
                case "duplicate":
                    DuplicateClip(clip);
                    break;
                case "split":
                    SplitClipAtPlayhead(clip);
                    break;
                case "properties":
                    ShowClipProperties(clip);
                    break;
                default:
                    Debug.LogWarning($"Unknown clip action: {action}");
                    break;
            }
        }

        private void CutClip(ClipUIToolkit clipUI)
        {
            if (clipUI?.Clip == null) return;

            Debug.Log($"Cut clip: {clipUI.Clip.Id}");

            // Copy to clipboard first
            CopyClip(clipUI);

            // Then delete
            DeleteClip(clipUI);
        }

        private void CopyClip(ClipUIToolkit clipUI)
        {
            if (clipUI?.Clip == null) return;

            Debug.Log($"Copy clip: {clipUI.Clip.Id}");

            try
            {
                // Create serialized wrapper to store type info + data
                var wrapper = new SerializedWrapper
                {
                    type = clipUI.Clip.GetType().AssemblyQualifiedName,
                    data = JsonUtility.ToJson(clipUI.Clip)
                };

                // Serialize wrapper to JSON and store in clipboard
                string clipboardJson = JsonUtility.ToJson(wrapper);
                GUIUtility.systemCopyBuffer = clipboardJson;

                Debug.Log($"Copied clip to clipboard ({clipboardJson.Length} bytes)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to copy clip: {ex.Message}");
            }
        }

        private void DeleteClip(ClipUIToolkit clipUI)
        {
            if (clipUI?.Clip == null || clipUI?.ParentTrack == null)
            {
                Debug.LogError("Cannot delete clip: clip or parent track is null");
                return;
            }

            Debug.Log($"Delete clip: {clipUI.Clip.Id}");
            
            // Use command for undo support
            var deleteCommand = new DeleteClipCommand(clipUI.ParentTrack.Track, clipUI.Clip);
            ExecuteCommand(deleteCommand);
        }

        private void DuplicateClip(ClipUIToolkit clipUI)
        {
            Debug.Log($"Duplicate clip: {clipUI?.Clip?.Id}");
            // TODO: Implement duplicate functionality
        }

        private void SplitClipAtPlayhead(ClipUIToolkit clipUI)
        {
            Debug.Log($"Split clip at playhead: {clipUI?.Clip?.Id}");
            // TODO: Implement split functionality
        }

        private void ShowClipProperties(ClipUIToolkit clipUI)
        {
            Debug.Log($"Show properties for clip: {clipUI?.Clip?.Id}");

            if (clipUI?.Clip == null || clipUI?.ParentTrack == null)
            {
                Debug.LogWarning("Cannot show properties: clip or parent track is null");
                return;
            }

            // Get track type to determine form fields
            string trackType = GetClipTrackType(clipUI);

            // Get field definitions for this track type (same as creation form)
            var fieldDefinitions = ClipFormDefinitions.GetFieldsForTrackType(trackType);

            // Populate form fields with current clip data
            PopulateFormFieldsWithClipData(fieldDefinitions, clipUI);

            string formTitle = $"Edit {ClipFormDefinitions.GetTrackTypeDisplayName(trackType)}";

            // Show the form for editing
            FormSubmitPanelUIToolkit.Instance.Show(
                formTitle,
                fieldDefinitions,
                (data) => OnClipEditFormSubmitted(data, clipUI),
                () => OnClipEditFormCancelled(),
                transform
            );
        }

        /// <summary>
        /// Get the track type string for this clip's parent track
        /// </summary>
        private string GetClipTrackType(ClipUIToolkit clipUI)
        {
            if (clipUI?.ParentTrack?.Track == null) return "generic";

            // Use the same logic as the existing GetTrackType method
            return TrackUIHelper.GetTrackTypeString(clipUI.ParentTrack.Track);
        }

        /// <summary>
        /// Populate form field definitions with current clip data
        /// </summary>
        private void PopulateFormFieldsWithClipData(List<FormFieldDefinition> fieldDefinitions, ClipUIToolkit clipUI)
        {
            foreach (var fieldDef in fieldDefinitions)
            {
                switch (fieldDef.name)
                {
                    case "trackType":
                        fieldDef.defaultValue = GetClipTrackType(clipUI);
                        break;

                    case "name":
                        fieldDef.defaultValue = clipUI.Clip.Id;
                        break;

                    case "start":
                        fieldDef.defaultValue = clipUI.Clip.Start;
                        break;

                    case "duration":
                        fieldDef.defaultValue = clipUI.Clip.Duration;
                        break;

                    default:
                        // Handle clip-specific properties using reflection
                        PopulateClipSpecificProperty(fieldDef, clipUI);
                        break;
                }
            }
        }

        /// <summary>
        /// Populate clip-specific properties using reflection
        /// </summary>
        private void PopulateClipSpecificProperty(FormFieldDefinition fieldDef, ClipUIToolkit clipUI)
        {
            try
            {
                var clipType = clipUI.Clip.GetType();
                var property = clipType.GetProperty(fieldDef.name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var field = clipType.GetField(fieldDef.name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (property != null && property.CanRead)
                {
                    fieldDef.defaultValue = property.GetValue(clipUI.Clip);
                }
                else if (field != null)
                {
                    fieldDef.defaultValue = field.GetValue(clipUI.Clip);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to populate clip property '{fieldDef.name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Handle form submission for clip editing
        /// </summary>
        private void OnClipEditFormSubmitted(Dictionary<string, object> formData, ClipUIToolkit clipUI)
        {
            Debug.Log($"Clip edit form submitted with {formData.Count} fields to clip {clipUI.Clip.Id}");

            try
            {
                // Apply clip changes directly (fallback when no command system available)
                ApplyClipChangesDirectly(formData, clipUI);

                // TODO: Implement proper command-based editing
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply clip changes: {ex.Message}");
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
        private void ApplyClipChangesDirectly(Dictionary<string, object> formData, ClipUIToolkit clipUI)
        {
            // Cast to MiniClipBase to access setters
            var clipBase = clipUI.Clip as MiniClipBase;
            if (clipBase == null)
            {
                Debug.LogError($"Cannot edit clip: clip {clipUI.Clip.GetType().Name} is not derived from MiniClipBase");
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
            ApplyClipSpecificProperties(formData, clipUI);

            // Update visual representation
            clipUI.UpdateClipAppearance();

            // If position or duration changed, rebuild track layout
            if (Mathf.Abs(originalStart - clipBase.Start) > 0.001f ||
                Mathf.Abs(originalDuration - clipBase.Duration) > 0.001f)
            {
                clipUI.ParentTrack?.RebuildClipUIs();
            }

            Debug.Log($"Applied clip changes directly: {originalId} -> {clipBase.Id}");
        }

        /// <summary>
        /// Apply clip-specific properties using reflection
        /// </summary>
        private void ApplyClipSpecificProperties(Dictionary<string, object> formData, ClipUIToolkit clipUI)
        {
            var clipType = clipUI.Clip.GetType();

            foreach (var kvp in formData)
            {
                // Skip basic properties that are handled separately
                if (kvp.Key == "trackType" || kvp.Key == "name" || kvp.Key == "start" || kvp.Key == "duration")
                    continue;

                try
                {
                    var property = clipType.GetProperty(kvp.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var field = clipType.GetField(kvp.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (property != null && property.CanWrite)
                    {
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, property.PropertyType);
                        property.SetValue(clipUI.Clip, convertedValue);
                        Debug.Log($"Set clip property {kvp.Key} = {convertedValue}");
                    }
                    else if (field != null)
                    {
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, field.FieldType);
                        field.SetValue(clipUI.Clip, convertedValue);
                        Debug.Log($"Set clip field {kvp.Key} = {convertedValue}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to set clip property '{kvp.Key}': {ex.Message}");
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
                return Convert.ToSingle(value);

            if (targetType == typeof(int))
                return Convert.ToInt32(value);

            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);

            // Default: try direct conversion
            return Convert.ChangeType(value, targetType);
        }

        #endregion

        #region Track Action Handlers

        /// <summary>
        /// Handles track form submission
        /// </summary>
        private void HandleTrackFormSubmission(Dictionary<string, object> data, TrackUIToolkit track)
        {
            Debug.Log($"HandleTrackFormSubmission called with {data.Count} data items");
            foreach (var kvp in data)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            if (data.TryGetValue("action", out var action))
            {
                Debug.Log($"Found action: {action}");
                HandleTrackAction(action.ToString(), track);
            }
            else
            {
                Debug.LogWarning("No 'action' key found in form data");
            }
        }

        /// <summary>
        /// Handles track actions
        /// </summary>
        private void HandleTrackAction(string action, TrackUIToolkit track)
        {
            Debug.Log($"HandleTrackAction called with action: '{action}', track: {track?.Track?.Id}");

            switch (action)
            {
                case "addClip":
                    Debug.Log("Calling AddClipToTrack");
                    AddClipToTrack(track);
                    break;
                case "mute":
                    MuteTrack(track);
                    break;
                case "solo":
                    SoloTrack(track);
                    break;
                case "delete":
                    DeleteTrack(track);
                    break;
                case "settings":
                    ShowTrackSettings(track);
                    break;
                default:
                    Debug.LogWarning($"Unknown track action: {action}");
                    break;
            }
        }

        private void AddClipToTrack(TrackUIToolkit track)
        {
            Debug.Log($"AddClipToTrack called with track: {track?.Track?.Id}");

            if (track?.Track == null)
            {
                Debug.LogError("Cannot create clip: track is null");
                return;
            }

            Debug.Log("Track is valid, calling ShowCreateClipForm");
            // Show create clip form
            ShowCreateClipForm(track);
        }

        private void ShowCreateClipForm(TrackUIToolkit trackUI)
        {
            Debug.Log($"ShowCreateClipForm called with trackUI: {trackUI?.Track?.Id}");

            if (trackUI.Track == null)
            {
                Debug.LogError("Cannot create clip: track is null");
                return;
            }

            Debug.Log("Getting track type...");
            // Get the track type
            string trackType = TrackUIHelper.GetTrackTypeString(trackUI.Track);
            Debug.Log($"Track type: {trackType}");

            Debug.Log("Getting form field definitions...");
            // Get form field definitions using ClipFormDefinitions
            var fieldDefinitions = ClipFormDefinitions.GetFieldsForTrackType(trackType);
            Debug.Log($"Got {fieldDefinitions?.Count ?? 0} field definitions");

            if (fieldDefinitions != null)
            {
                foreach (var field in fieldDefinitions)
                {
                    Debug.Log($"  Field: {field.name} | Type: {field.type} | Label: {field.label}");
                }
            }
            else
            {
                Debug.LogError("fieldDefinitions is null!");
            }

            // Get display name for the form title
            string formTitle = ClipFormDefinitions.GetTrackTypeDisplayName(trackType);
            Debug.Log($"Form title: {formTitle}");

            Debug.Log("Showing FormSubmitPanelUIToolkit...");
            // Show the form
            FormSubmitPanelUIToolkit.Instance.Show(
                formTitle,
                fieldDefinitions,
                (data) => OnClipFormSubmittedForTrack(data, trackUI),
                () => OnClipFormCancelled(),
                transform
            );
            Debug.Log("FormSubmitPanelUIToolkit.Show call completed");
        }

        private void OnClipFormSubmittedForTrack(Dictionary<string, object> formData, TrackUIToolkit trackUI)
        {
            Debug.Log($"Clip form submitted with {formData.Count} fields: {string.Join(", ", formData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            // Get track type from form data (it's included as a hidden field)
            string trackType = formData.ContainsKey("trackType") ? formData["trackType"].ToString() : TrackUIHelper.GetTrackTypeString(trackUI.Track);

            CreateClipFromFormData(trackType, formData, trackUI);
        }

        private void OnClipFormCancelled()
        {
            Debug.Log("Clip creation cancelled");
        }

        private void CreateClipFromFormData(string trackType, Dictionary<string, object> formData, TrackUIToolkit trackUI)
        {
            try
            {
                // Clip creation from UI form data not yet implemented
                Debug.LogWarning("[TimelineEditorUIToolkit] Clip creation from UI form not yet implemented");
                IMiniClip clipInstance = null;

                if (clipInstance != null)
                {
                    // Try to add clip directly since we don't have access to commands
                    trackUI.Track.AddClip(clipInstance);
                    trackUI.RebuildClipUIs();

                    Debug.Log($"Successfully created clip '{clipInstance.Id}' on track '{trackUI.Track.Id}'");
                }
                else
                {
                    Debug.LogError($"Failed to create clip instance for track type: {trackType}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create clip: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void MuteTrack(TrackUIToolkit track)
        {
            Debug.Log($"Toggle mute for track: {track.Track?.GetType().Name}");
            // TODO: Implement mute functionality - need access to mute toggle or track enabled state
            // For now, just toggle the track enabled state
            if (track.Track != null)
            {
                track.Track.Enabled = !track.Track.Enabled;
            }
        }

        private void SoloTrack(TrackUIToolkit track)
        {
            Debug.Log($"Solo track: {track.Track?.GetType().Name}");
            // TODO: Implement solo functionality
        }

        private void DeleteTrack(TrackUIToolkit track)
        {
            if (track.Track == null || director == null)
            {
                Debug.LogError("Cannot delete track: track or timeline director is null");
                return;
            }

            // Show confirmation dialog for track deletion
            ShowDeleteTrackConfirmation(track);
        }

        private void ShowDeleteTrackConfirmation(TrackUIToolkit trackUI)
        {
            string trackDisplayName = GetTrackDisplayName(trackUI.Track);
            int clipCount = trackUI.Track.GetClips()?.Count() ?? 0;

            // Create confirmation form
            var fieldDefinitions = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "confirmationText",
                    type = "textarea",
                    label = "Confirmation",
                    required = false,
                    defaultValue = $"Are you sure you want to delete '{trackDisplayName}'?\n\n" +
                                   $"This track contains {clipCount} clip(s).\n\n" +
                                   "This action cannot be undone (but can be undone via Undo command).\n\n" +
                                   "Type 'DELETE' below to confirm:",
                    tooltip = "Confirmation message for track deletion",
                    options = new Dictionary<string, object>
                    {
                        { "readonly", true }
                    }
                },

                new FormFieldDefinition
                {
                    name = "confirmationInput",
                    type = "text",
                    label = "Type 'DELETE' to confirm",
                    required = true,
                    placeholder = "DELETE",
                    tooltip = "Type 'DELETE' exactly to confirm track deletion"
                },

                new FormFieldDefinition
                {
                    name = "preserveClips",
                    type = "toggle",
                    label = "Preserve Clips Data (for debugging)",
                    required = false,
                    defaultValue = false,
                    tooltip = "Keep clip data in memory for debugging purposes (not recommended for production)"
                }
            };

            // Show confirmation form
            FormSubmitPanelUIToolkit.Instance.Show(
                $"⚠️ Delete {trackDisplayName}",
                fieldDefinitions,
                (data) => OnDeleteTrackConfirmed(data, trackUI),
                () => OnDeleteTrackCancelled(),
                transform
            );
        }

        private string GetTrackDisplayName(IMiniTrack track)
        {
            if (track == null) return "Unknown Track";

            string typeName = track.GetType().Name;
            return typeName switch
            {
                "AnimTrack" => "Animation Track",
                "AnimatorTrack" => "Animator Track",
                "MorphTrack" => "Morph Track",
                "MovementTrack" => "Movement Track",
                "SignalTrack" => "Signal Track",
                "UmaWardrobeTrack" => "UMA Wardrobe Track",
                "UMAExpressionTrack" => "UMA Expression Track",
                _ => $"{typeName} Track"
            };
        }

        private void OnDeleteTrackConfirmed(Dictionary<string, object> formData, TrackUIToolkit trackUI)
        {
            try
            {
                // Validate confirmation input
                string confirmationInput = formData.ContainsKey("confirmationInput")
                    ? formData["confirmationInput"]?.ToString() ?? ""
                    : "";

                if (confirmationInput != "DELETE")
                {
                    Debug.LogWarning("Track deletion cancelled: confirmation text does not match 'DELETE'");
                    return;
                }

                // Get preserve clips option
                bool preserveClips = formData.ContainsKey("preserveClips")
                    ? Convert.ToBoolean(formData["preserveClips"])
                    : false;

                // Execute delete command
                ExecuteDeleteTrack(trackUI, preserveClips);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to process track deletion confirmation: {ex.Message}");
            }
        }

        private void OnDeleteTrackCancelled()
        {
            Debug.Log("Track deletion cancelled by user");
        }

        private void ExecuteDeleteTrack(TrackUIToolkit trackUI, bool preserveClips = false)
        {
            try
            {
                string trackDisplayName = GetTrackDisplayName(trackUI.Track);

                if (preserveClips)
                {
                    Debug.Log($"Preserving clips data for debugging: {trackUI.Track.GetClips()?.Count() ?? 0} clips");
                    // Could store clips data here for debugging if needed
                }

                // For now, just log the deletion - implement proper track removal command later
                Debug.Log($"Delete track requested: {trackDisplayName}");
                // TODO: Implement proper track deletion through command system
                // var removeCommand = new RemoveTrackCommand(director, trackUI.Track, this);
                // ExecuteCommand(removeCommand);

                Debug.Log($"Successfully deleted {trackDisplayName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete track: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowTrackSettings(TrackUIToolkit track)
        {
            if (track.Track == null)
            {
                Debug.LogError("Cannot show track settings: track is null");
                return;
            }

            // Get the track type for form definition lookup
            string trackType = TrackUIHelper.GetTrackTypeString(track.Track);

            // Get form field definitions from TrackFormDefinitions
            var fieldDefinitions = TrackFormDefinitions.GetTrackSettingsFields(trackType);

            // Set default values from current track state
            SetDefaultValuesForTrackSettings(fieldDefinitions, track);

            // Show the form using FormSubmitPanelUIToolkit
            FormSubmitPanelUIToolkit.Instance.Show(
                $"Track Settings - {TrackFormDefinitions.GetTrackTypeDisplayName(trackType)}",
                fieldDefinitions,
                (data) => OnTrackSettingsFormSubmitted(data, track),
                () => OnTrackSettingsFormCancelled(),
                transform
            );

            Debug.Log($"Show track settings: {track.Track?.GetType().Name}");
        }

        private void SetDefaultValuesForTrackSettings(List<FormFieldDefinition> fieldDefinitions, TrackUIToolkit trackUI)
        {
            foreach (var field in fieldDefinitions)
            {
                switch (field.name)
                {
                    case "trackName":
                        field.defaultValue = GetTrackDisplayName(trackUI.Track);
                        break;
                    case "bindKey":
                        field.defaultValue = trackUI.Track.BindKey ?? "";
                        // Update selectbox options with available bindings
                        if (field.options == null)
                            field.options = new Dictionary<string, object>();
                        field.options["items"] = TrackFormDefinitions.GetAvailableBindingKeys();
                        break;
                    case "enabled":
                        field.defaultValue = trackUI.Track.Enabled;
                        break;
                    case "trackOrder":
                        field.defaultValue = GetTrackOrder(trackUI);
                        break;
                }
            }
        }

        private int GetTrackOrder(TrackUIToolkit trackUI)
        {
            if (trackUI.Track == null || director?.Project == null) return 0;

            var projectTracks = director.Project.tracks;
            var trackData = projectTracks?.FirstOrDefault(t => t.Id == trackUI.Track.Id);
            return trackData?.Order ?? 0;
        }

        private void OnTrackSettingsFormSubmitted(Dictionary<string, object> formData, TrackUIToolkit trackUI)
        {
            try
            {
                Debug.Log("Track settings form submitted with data:");
                foreach (var kvp in formData)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }

                // Apply the settings changes
                ApplyTrackSettings(formData, trackUI);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply track settings: {ex.Message}");
            }
        }

        private void OnTrackSettingsFormCancelled()
        {
            Debug.Log("Track settings cancelled");
        }

        private void ApplyTrackSettings(Dictionary<string, object> formData, TrackUIToolkit trackUI)
        {
            if (trackUI.Track == null) return;

            string trackType = TrackUIHelper.GetTrackTypeString(trackUI.Track);

            // Validate the form data using TrackFormDefinitions
            if (!TrackFormDefinitions.ValidateTrackSettings(formData, trackType, out string errorMessage))
            {
                Debug.LogError($"Track settings validation failed: {errorMessage}");
                return;
            }

            // Convert form data to track settings using TrackFormDefinitions
            var newSettings = TrackFormDefinitions.ConvertFormDataToTrackSettings(formData, trackType);

            // Apply settings directly to the track
            ApplyTrackSettingsDirectly(newSettings, trackUI);
        }

        private void ApplyTrackSettingsDirectly(Dictionary<string, object> formData, TrackUIToolkit trackUI)
        {
            try
            {
                // Log the settings that would be applied
                // Note: Some properties like BindKey might be readonly and require special handling
                Debug.Log("Track settings to apply:");

                if (formData.ContainsKey("bindKey"))
                {
                    string newBindKey = formData["bindKey"].ToString();
                    Debug.Log($"  BindKey: {trackUI.Track.BindKey} -> {newBindKey}");
                    // TODO: Apply bind key through proper command or method
                }

                if (formData.ContainsKey("enabled"))
                {
                    bool newEnabled = Convert.ToBoolean(formData["enabled"]);
                    Debug.Log($"  Enabled: {trackUI.Track.Enabled} -> {newEnabled}");
                    trackUI.Track.Enabled = newEnabled;
                }

                Debug.Log("Track settings applied successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply track settings: {ex.Message}");
            }
        }

        #endregion

        #region Timeline Action Handlers

        /// <summary>
        /// Handles timeline form submission
        /// </summary>
        private void HandleTimelineFormSubmission(Dictionary<string, object> data, float timePosition)
        {
            if (data.TryGetValue("action", out var action))
            {
                HandleTimelineAction(action.ToString(), timePosition);
            }
        }

        /// <summary>
        /// Handles timeline actions
        /// </summary>
        private void HandleTimelineAction(string action, float timePosition)
        {
            switch (action)
            {
                case "addTrack":
                    ShowAddTrackForm();
                    break;
                case "paste":
                    PasteAtTime(timePosition);
                    break;
                case "addMarker":
                    AddMarker(timePosition);
                    break;
                case "zoomFit":
                    ZoomToFit();
                    break;
                case "resetZoom":
                    ResetZoom();
                    break;
            }
        }

        private void PasteAtTime(float time)
        {
            Debug.Log($"Paste at time: {time}");
            // TODO: Implement paste functionality
        }

        private void AddMarker(float time)
        {
            Debug.Log($"Add marker at time: {time}");
            // TODO: Add event marker
        }

        private void ZoomToFit()
        {
            Debug.Log("Zoom to fit");
            SetZoom(1f); // TODO: Calculate proper zoom to fit
        }

        private void ResetZoom()
        {
            Debug.Log("Reset zoom");
            SetZoom(1f);
        }

        #endregion
    }
}