using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MiniTimeline.Core;
using MiniTimeline.Serialization;
using MiniTimeline.UI.Commands;
using MiniTimeline.UI.FormDefinitions;

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
        [SerializeField] private Button addTrackButton;
        [SerializeField] private Button bindingManagerButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
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
        [SerializeField] private GameObject contextMenuPrefab;
        [SerializeField] private GameObject formSubmitPanelPrefab;

        [Header("Debug")]
        [SerializeField] bool isDebug;

        // Core references
        [SerializeField] private MiniTimelineDirector director;
        private TimelineCommandManager commandManager;
        private TimelineContextMenu contextMenu;
        private FormSubmitPanel formSubmitPanel;

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
        public GameObject FormSubmitPanelPrefab => formSubmitPanelPrefab;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize UI creation helper to prevent TextMeshPro threading issues
            UICreationHelper.Initialize(this);
            
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

            // Setup add track button
            if (addTrackButton != null)
                addTrackButton.onClick.AddListener(ShowAddTrackForm);

            // Setup binding manager button
            if (bindingManagerButton != null)
                bindingManagerButton.onClick.AddListener(ShowBindingManagerForm);

            // Setup save/load buttons
            if (saveButton != null)
                saveButton.onClick.AddListener(ShowSaveProjectForm);

            if (loadButton != null)
                loadButton.onClick.AddListener(ShowLoadProjectForm);

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

            if (formSubmitPanelPrefab != null)
            {
                var formSubmitGO = Instantiate(formSubmitPanelPrefab, editorCanvas.transform);
                formSubmitPanel = formSubmitGO.GetComponent<FormSubmitPanel>();
                formSubmitPanel.CloseForm();
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

        #region Add Track Methods

        /// <summary>
        /// Show form for adding a new track to the timeline
        /// </summary>
        private void ShowAddTrackForm()
        {
            if (director?.Project == null)
            {
                Debug.LogError("Cannot add track: No project loaded");
                return;
            }

            // Get form field definitions from TrackFormDefinitions
            var fieldDefinitions = TrackFormDefinitions.GetCreateTrackFields();

            // Show the form using FormSubmitPanel singleton
            FormSubmitPanel.Instance.Show(
                "Add New Track",
                fieldDefinitions,
                OnAddTrackFormSubmitted,
                OnAddTrackFormCancelled
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
                string trackId = System.Guid.NewGuid().ToString();

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

                Debug.Log($"Added new {TrackUIHelper.GetTrackDisplayName(trackType)} track: {trackName}");
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
            Debug.Log("Add track cancelled");
        }

        #endregion
        
        #region Binding Manager Methods
        
        /// <summary>
        /// Show the main binding manager form
        /// </summary>
        private void ShowBindingManagerForm()
        {
            if (director?.BindingContext == null)
            {
                Debug.LogError("Cannot show binding manager: No binding context available");
                return;
            }

            Debug.Log("Opening main binding manager form");
            
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
                    type = "text",  // Using text for now since objectfield might not be implemented
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
            
            // Show the binding manager form
            FormSubmitPanel.Instance.Show(
                "Scene Binding Manager",
                fieldDefinitions,
                OnBindingManagerFormSubmitted,
                OnBindingManagerFormCancelled
            );
        }
        
        /// <summary>
        /// Format the current bindings for display in the info field
        /// </summary>
        private string FormatBindingsForDisplay(BindingContext bindingContext)
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
                Debug.Log("Main binding manager form submitted with data:");
                foreach (var kvp in formData)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }
                
                // Check if this is an auto-detect action
                if (formData.ContainsKey("action") && formData["action"].ToString() == "autoDetectBindings")
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
                            Debug.Log($"Added new binding: {newKey} → {targetObject.name}");
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
                        Debug.Log($"Removed binding: {removeKey}");
                    }
                }
                
                if (bindingsChanged)
                {
                    Debug.Log("Scene bindings updated successfully");
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
            Debug.Log("Binding manager cancelled");
        }
        
        /// <summary>
        /// Auto-detect common GameObjects in the scene and bind them using BindableObject components
        /// </summary>
        private void AutoDetectSceneBindings()
        {
            if (director?.BindingContext == null) return;
            
            var bindingContext = director.BindingContext;
            int bindingsAdded = 0;
            int bindableObjectsFound = 0;
            int fallbackBindingsAdded = 0;
            
            // Track binding counts for each type to handle multiple objects of same type
            var typeBindingCounts = new Dictionary<string, int>();
            
            // First pass: Find objects with BindableObject components
            var bindableObjects = FindObjectsByType<BindableObject>(FindObjectsSortMode.None);
            
            foreach (var bindableObj in bindableObjects)
            {
                if (!bindableObj.IsValid) continue;
                
                bindableObjectsFound++;
                
                // Get binding key from the object's type
                string baseBindingKey = DetermineBindingKeyFromType(bindableObj);
                
                if (string.IsNullOrEmpty(baseBindingKey))
                {
                    // Fallback to component-based detection
                    baseBindingKey = DetermineBindingKeyFromComponents(bindableObj.gameObject);
                }
                
                if (string.IsNullOrEmpty(baseBindingKey))
                {
                    // Last resort: use sanitized object name
                    baseBindingKey = SanitizeBindingKey(bindableObj.gameObject.name.ToLower());
                }
                
                // Handle multiple objects of the same type by incrementing
                string finalBindingKey = baseBindingKey;
                if (typeBindingCounts.ContainsKey(baseBindingKey))
                {
                    typeBindingCounts[baseBindingKey]++;
                    finalBindingKey = $"{baseBindingKey}_{typeBindingCounts[baseBindingKey]}";
                }
                else
                {
                    typeBindingCounts[baseBindingKey] = 0;
                    // Check if base key already exists in binding context
                    if (bindingContext.HasKey(baseBindingKey))
                    {
                        typeBindingCounts[baseBindingKey] = 1;
                        finalBindingKey = $"{baseBindingKey}_1";
                    }
                }
                
                // Ensure the final key is truly unique
                finalBindingKey = EnsureUniqueBindingKey(bindingContext, finalBindingKey);
                
                if (!string.IsNullOrEmpty(finalBindingKey))
                {
                    bindingContext.Bind(finalBindingKey, bindableObj.gameObject);
                    bindingsAdded++;
                    
                    string typeInfo = bindableObj.ObjectType == BindableObjectType.Custom 
                        ? $"Custom({bindableObj.CustomTypeName})" 
                        : bindableObj.ObjectType.ToString();
                    string tagsInfo = bindableObj.Tags.Count > 0 ? $" [Tags: {string.Join(", ", bindableObj.Tags)}]" : "";
                    Debug.Log($"Auto-detected binding: {finalBindingKey} → {bindableObj.gameObject.name} (Type: {typeInfo}){tagsInfo}");
                }
            }
            
            // Second pass: Fallback detection for objects without BindableObject components
            if (bindableObjectsFound == 0)
            {
                Debug.Log("No BindableObject components found, falling back to name-based detection");
                fallbackBindingsAdded = AutoDetectSceneBindingsFallback();
            }
            
            string summary = $"Auto-detection complete. Found {bindableObjectsFound} BindableObject components, added {bindingsAdded} bindings";
            if (fallbackBindingsAdded > 0)
            {
                summary += $", plus {fallbackBindingsAdded} fallback bindings";
            }
            Debug.Log(summary);
        }
        
        /// <summary>
        /// Determine binding key from BindableObject type
        /// </summary>
        private string DetermineBindingKeyFromType(BindableObject bindableObj)
        {
            // Use the object's TypeName property which handles both enum types and custom types
            return bindableObj.TypeName;
        }
        
        /// <summary>
        /// Determine binding key from GameObject components
        /// </summary>
        private string DetermineBindingKeyFromComponents(GameObject go)
        {
            if (go.GetComponent<Camera>())
                return "camera";
            else if (go.GetComponent<Canvas>())
                return "ui";
            else if (go.GetComponent<Light>())
                return "light";
            else if (go.GetComponent<AudioSource>())
                return "audio";
            else if (go.GetComponent<Animator>() || go.GetComponent<Animation>())
            {
                string name = go.name.ToLower();
                return name.Contains("character") || name.Contains("player") ? "character" : "animated_object";
            }
            
            return null;
        }
        
        /// <summary>
        /// Sanitize a string to be a valid binding key
        /// </summary>
        private string SanitizeBindingKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "object";
            
            // Remove invalid characters and convert to lowercase
            key = key.ToLower();
            key = System.Text.RegularExpressions.Regex.Replace(key, @"[^a-z0-9_]", "_");
            key = System.Text.RegularExpressions.Regex.Replace(key, @"_+", "_");
            key = key.Trim('_');
            
            return string.IsNullOrEmpty(key) ? "object" : key;
        }
        
        /// <summary>
        /// Ensure the binding key is unique by appending numbers if needed
        /// </summary>
        private string EnsureUniqueBindingKey(BindingContext bindingContext, string baseKey)
        {
            if (string.IsNullOrEmpty(baseKey)) return null;
            
            if (!bindingContext.HasKey(baseKey))
            {
                return baseKey;
            }
            
            // Generate unique key by appending numbers
            for (int i = 1; i < 100; i++)
            {
                string uniqueKey = $"{baseKey}_{i}";
                if (!bindingContext.HasKey(uniqueKey))
                {
                    return uniqueKey;
                }
            }
            
            Debug.LogWarning($"Could not generate unique binding key for base key: {baseKey}");
            return null;
        }
        
        /// <summary>
        /// Fallback auto-detection using name patterns when no BindingableObject components are found
        /// </summary>
        private int AutoDetectSceneBindingsFallback()
        {
            if (director?.BindingContext == null) return 0;
            
            var bindingContext = director.BindingContext;
            int bindingsAdded = 0;
            
            // Find common objects in the scene using the old method
            var gameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var go in gameObjects)
            {
                // Skip objects that already have BindableObject components
                if (go.GetComponent<BindableObject>() != null) continue;
                
                string name = go.name.ToLower();
                string bindingKey = null;
                
                // Determine binding key based on object name patterns
                if (name.Contains("character") || name.Contains("player"))
                    bindingKey = "character";
                else if (name.Contains("camera") && go.GetComponent<Camera>() != null)
                    bindingKey = "camera";
                else if (name.Contains("environment") || name.Contains("level"))
                    bindingKey = "environment";
                else if (name.Contains("ui") && go.GetComponent<Canvas>() != null)
                    bindingKey = "ui";
                else if (name.Contains("light") && go.GetComponent<Light>() != null)
                    bindingKey = "light";
                else if (name.Contains("audio") && go.GetComponent<AudioSource>() != null)
                    bindingKey = "audio";
                
                // Add binding if we found a key and it doesn't already exist
                if (!string.IsNullOrEmpty(bindingKey) && !bindingContext.HasKey(bindingKey))
                {
                    bindingContext.Bind(bindingKey, go);
                    bindingsAdded++;
                    Debug.Log($"Fallback auto-detected binding: {bindingKey} → {go.name}");
                }
            }
            
            return bindingsAdded;
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
                Debug.Log($"Removed binding: {key}");
            }
            else
            {
                Debug.LogWarning($"Cannot remove binding: BindingContext not available");
            }
        }

        #endregion
        
        #region Save/Load Project Methods
        
        /// <summary>
        /// Show the save project form
        /// </summary>
        private void ShowSaveProjectForm()
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
            
            // Show the save form
            FormSubmitPanel.Instance.Show(
                "Save Timeline Project",
                fieldDefinitions,
                OnSaveProjectFormSubmitted,
                OnSaveProjectFormCancelled
            );
        }
        
        /// <summary>
        /// Handle save project form submission
        /// </summary>
        private void OnSaveProjectFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                Debug.Log("Save project form submitted with data:");
                foreach (var kvp in formData)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }
                
                string filename = formData.ContainsKey("filename") ? formData["filename"].ToString() : "timeline_project";
                bool prettyPrint = formData.ContainsKey("prettyPrint") ? Convert.ToBoolean(formData["prettyPrint"]) : true;
                
                // Ensure filename has .json extension
                if (!filename.EndsWith(".json"))
                {
                    filename += ".json";
                }
                
                // For now, save to persistent data path (in a real game you'd want file browser)
                string filePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, filename);
                
                // Save the project (automatically updates from runtime tracks)
                bool success = ProjectSerializer.SaveToFile(director.Project, director, filePath);
                
                if (success)
                {
                    Debug.Log($"Project saved successfully to: {filePath}");
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
            Debug.Log("Save project cancelled");
        }
        
        /// <summary>
        /// Show the load project form
        /// </summary>
        private void ShowLoadProjectForm()
        {
            Debug.Log("Opening load project form");
            
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
            
            // Show the load form
            FormSubmitPanel.Instance.Show(
                "Load Timeline Project",
                fieldDefinitions,
                OnLoadProjectFormSubmitted,
                OnLoadProjectFormCancelled
            );
        }
        
        /// <summary>
        /// Handle load project form submission
        /// </summary>
        private void OnLoadProjectFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                Debug.Log("Load project form submitted with data:");
                foreach (var kvp in formData)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }
                
                // Try to get filename from dropdown first, then manual entry
                string filename = "";
                
                if (formData.ContainsKey("availableFiles") && !string.IsNullOrEmpty(formData["availableFiles"]?.ToString()))
                {
                    filename = formData["availableFiles"].ToString();
                    Debug.Log($"Using selected file from dropdown: {filename}");
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
                string filePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, filename);
                
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
                string persistentPath = UnityEngine.Application.persistentDataPath;
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
                return $"No project files found in:\n{UnityEngine.Application.persistentDataPath}\n\nTip: Save a project first, or manually place .json files in this directory.";
            }
            
            var display = $"Found {files.Length} project file(s) in:\n{UnityEngine.Application.persistentDataPath}\n\n";
            display += "Use the dropdown above to select a file, or enter a filename manually.";
            
            return display;
        }

        #endregion
    }
}