using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiniTimeline.Core;
using MiniTimeline.UI.Commands;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.UI.FormDefinitions;
using MiniTimeline.Serialization;

namespace MiniTimeline.UI
{
    /// <summary>
    /// UI Toolkit version of TrackUI - represents a timeline track
    /// Displays track header and contains clip UI elements
    /// Implements long press detection and context menu support
    /// </summary>
    public class TrackUIToolkit : ITrackUI
    {
        [Header("Visual Settings")]

        [Header("Long Press Settings")]
        private float longPressDuration = 0.8f; // Time to trigger long press in seconds
        private float longPressMoveThreshold = 20f; // Pixels

        // Mouse interaction fields
        private bool isMouseDown;
        private bool isLongPress;
        private float mousePressStartTime;
        private IVisualElementScheduledItem longPressTimer;

        // Templates and styling
        private VisualTreeAsset trackTemplate;
        private StyleSheet trackStyleSheet;

        // UI Elements
        private VisualElement trackElement;
        private TimelineEditorUIToolkit editorUI;
        private IMiniTrack track;

        // Track UI Elements
        private VisualElement trackHeader;
        private VisualElement trackContent;
        private VisualElement tracksContainer;
        private VisualElement clipsContainer;
        private VisualElement trackInfo;
        private VisualElement trackControls;
        private VisualElement trackMenu;

        // Track info elements
        private Label trackTitle;
        private Label trackBindKey;
        private Label trackType;

        // Control elements
        private Toggle trackEnabled;
        private Button trackMuteButton;
        private Button trackSoloButton;
        private Button trackMenuButton;

        // Long press detection
        private bool isLongPressing = false;
        private bool longPressTriggered = false;
        private Vector2 longPressStartPosition;
        private IVisualElementScheduledItem longPressScheduledItem;

        // UI State
        private bool isMuted = false;
        private bool isSolo = false;
        private readonly List<ClipUIToolkit> clipUIs = new List<ClipUIToolkit>();
        private readonly Dictionary<string, ClipUIToolkit> clipUILookup = new Dictionary<string, ClipUIToolkit>();

        #region Properties

        public IMiniTrack Track => track;
        public IReadOnlyList<ClipUIToolkit> ClipUIs => clipUIs;
        public VisualElement ClipContainer => clipsContainer;
        public TimelineEditorUIToolkit TimelineEditor => editorUI;
        public bool IsLongPressing => isLongPressing;
        public VisualElement TrackElement => trackElement;

        #endregion

        #region Events

        public event Action<TrackUIToolkit, Vector2> OnTrackLongPressed; // Screen position where long press occurred
        public event Action<TrackUIToolkit> OnTrackSelected;
        public event Action<ClipUIToolkit> OnClipStartInteraction;
        public event Action<ClipUIToolkit> OnClipEndInteraction;

        #endregion

        public void Initialize(TimelineEditorUIToolkit editorUI, IMiniTrack track, VisualTreeAsset trackTemplate = null, StyleSheet trackStyleSheet = null)
        {
            this.editorUI = editorUI;
            this.track = track;
            this.trackTemplate = trackTemplate;
            this.trackStyleSheet = trackStyleSheet;

            CreateTrackElement();
            SetupTrackData();
            SetupEventHandlers();
            CreateClipElements();

            // Assign track color after all elements are set up
            AssignTrackColor();

            // Ensure TimelineContextMenu is initialized for context menu support
            EnsureContextMenuInitialized();
        }

        /// <summary>
        /// Ensures TimelineContextMenu is properly initialized for this track
        /// </summary>
        private void EnsureContextMenuInitialized()
        {
            if (!TimelineContextMenu.HasInstance)
            {
                Debug.LogWarning("TimelineContextMenu instance not found. Context menus may not work properly.");
                return;
            }

            // Get the TimelineEditorUI component for context menu initialization
            var timelineEditorUI = editorUI?.GetComponent<TimelineEditorUI>();
            if (timelineEditorUI != null)
            {
                // Initialize the context menu with the timeline editor
                TimelineContextMenu.Instance.Initialize(timelineEditorUI);
            }
        }

        private void CreateTrackElement()
        {
            if (trackTemplate != null)
            {
                // Clone from template
                trackElement = trackTemplate.CloneTree();
            }
            else
            {
                // Fallback: create manually
                trackElement = CreateTrackElementManually();
            }

            // Apply styling
            if (trackStyleSheet != null)
            {
                trackElement.styleSheets.Add(trackStyleSheet);
            }

            // Set track identification
            trackElement.name = $"track-{track.Id}";
            trackElement.AddToClassList("track");

            // Track height is now content-driven; don't set an explicit height here.

            // Set initial track width to match timeline width
            if (editorUI != null)
            {
                var timelineWidth = editorUI.TimelineWidth;
                if (timelineWidth > 0)
                {
                    trackElement.style.width = timelineWidth;
                }
            }

            // Query UI elements
            QueryTrackElements();
        }

        private void AssignTrackColor()
        {
            if (track == null || trackContent == null) return;

            // Use the helper to get track-specific color
            Color bgColor = TrackUIHelper.GetTrackColor(track);
            bgColor.a = 0.3f; // Make it semi-transparent for background

            trackContent.style.backgroundColor = bgColor;
        }

        private VisualElement CreateTrackElementManually()
        {
            var root = new VisualElement();
            root.AddToClassList("track");

            // Create track header
            trackHeader = new VisualElement();
            trackHeader.AddToClassList("track-header");
            trackHeader.style.width = 200f;

            // Track info
            trackInfo = new VisualElement();
            trackInfo.AddToClassList("track-info");

            trackTitle = new Label("Track Name");
            trackTitle.AddToClassList("track-title");
            trackInfo.Add(trackTitle);

            trackBindKey = new Label("binding-key");
            trackBindKey.AddToClassList("track-bind-key");
            trackInfo.Add(trackBindKey);

            trackType = new Label("anim");
            trackType.AddToClassList("track-type");
            trackInfo.Add(trackType);

            trackHeader.Add(trackInfo);

            // Track controls
            trackControls = new VisualElement();
            trackControls.AddToClassList("track-controls");

            trackEnabled = new Toggle();
            trackEnabled.AddToClassList("track-enabled-toggle");
            trackEnabled.value = true;
            trackControls.Add(trackEnabled);

            trackMuteButton = new Button { text = "M" };
            trackMuteButton.AddToClassList("track-button");
            trackMuteButton.AddToClassList("track-mute-button");
            trackControls.Add(trackMuteButton);

            trackSoloButton = new Button { text = "S" };
            trackSoloButton.AddToClassList("track-button");
            trackSoloButton.AddToClassList("track-solo-button");
            trackControls.Add(trackSoloButton);

            trackHeader.Add(trackControls);

            // Track menu
            trackMenu = new VisualElement();
            trackMenu.AddToClassList("track-menu");

            trackMenuButton = new Button { text = "⋮" };
            trackMenuButton.AddToClassList("track-button");
            trackMenuButton.AddToClassList("track-menu-button");
            trackMenu.Add(trackMenuButton);

            trackHeader.Add(trackMenu);
            root.Add(trackHeader);

            // Track content
            trackContent = new VisualElement();
            trackContent.AddToClassList("track-content");

            var trackTimeline = new VisualElement();
            trackTimeline.AddToClassList("track-timeline");

            // Track grid (for visual alignment)
            var trackGrid = new VisualElement();
            trackGrid.AddToClassList("track-grid");
            trackTimeline.Add(trackGrid);

            clipsContainer = new VisualElement();
            clipsContainer.AddToClassList("clips-container");
            trackTimeline.Add(clipsContainer);

            trackContent.Add(trackTimeline);
            root.Add(trackContent);

            // Track resize handle
            var resizeHandle = new VisualElement();
            resizeHandle.AddToClassList("track-resize-handle");
            root.Add(resizeHandle);

            return root;
        }

        private void QueryTrackElements()
        {
            // Query track elements from the created structure
            trackHeader = trackElement.Q("track-header");
            trackContent = trackElement.Q("track-content");
            clipsContainer = trackElement.Q("clips-container");
            trackInfo = trackElement.Q("track-info");
            trackControls = trackElement.Q("track-controls");
            trackMenu = trackElement.Q("track-menu");

            trackTitle = trackElement.Q<Label>("track-title");
            trackBindKey = trackElement.Q<Label>("track-bind-key");
            trackType = trackElement.Q<Label>("track-type");

            trackEnabled = trackElement.Q<Toggle>("track-enabled");
            trackMuteButton = trackElement.Q<Button>("track-mute");
            trackSoloButton = trackElement.Q<Button>("track-solo");
            trackMenuButton = trackElement.Q<Button>("track-menu-button");

            // CRITICAL FIX: Disable mouse picking on track-lanes so they don't block clip interaction
            var trackLanes = trackElement.Q("track-lanes");
            if (trackLanes != null)
            {
                trackLanes.pickingMode = PickingMode.Ignore;

                // Also set picking mode on all lane children
                foreach (var lane in trackLanes.Children())
                {
                    lane.pickingMode = PickingMode.Ignore;
                }
            }
        }

        private void SetupTrackData()
        {
            if (track == null) return;

            // Set track information
            if (trackTitle != null)
                trackTitle.text = GetTrackDisplayName();

            if (trackBindKey != null)
                trackBindKey.text = track.BindKey ?? "unbound";

            if (trackType != null)
                trackType.text = GetTrackTypeName();

            // Apply track type styling
            trackElement?.AddToClassList(GetTrackTypeClass());

            // Set enabled state
            if (trackEnabled != null)
                trackEnabled.value = track.Enabled;
        }

        private string GetTrackDisplayName()
        {
            // Use helper to get friendly track name or fallback to basic naming
            string typeName = TrackUIHelper.GetFriendlyTrackName(track.GetType().Name);
            return $"{typeName}";
        }

        private void SetupEventHandlers()
        {
            // Track enable/disable
            if (trackEnabled != null)
            {
                trackEnabled.RegisterValueChangedCallback(OnTrackEnabledChanged);
            }

            // Mute button
            if (trackMuteButton != null)
            {
                trackMuteButton.clicked += OnMuteClicked;
            }

            // Solo button
            if (trackSoloButton != null)
            {
                trackSoloButton.clicked += OnSoloClicked;
            }

            // Menu button
            if (trackMenuButton != null)
            {
                trackMenuButton.clicked += OnMenuClicked;
            }

            // Track interaction events for long press and selection
            if (trackElement != null)
            {
                trackElement.RegisterCallback<MouseDownEvent>(OnTrackMouseDown);
                trackElement.RegisterCallback<MouseUpEvent>(OnTrackMouseUp);
                trackElement.RegisterCallback<MouseMoveEvent>(OnTrackMouseMove);
            }
        }

        private void CreateClipElements()
        {
            if (clipsContainer == null)
            {
                Debug.LogWarning($"[TrackUI] ClipsContainer is null for track {track?.Id}");
                return;
            }


            var clips = track.GetClips().ToList();

            foreach (var clip in clips)
            {
                var clipUI = new ClipUIToolkit();
                clipUI.Initialize(editorUI, clip, clipsContainer, editorUI.ClipTemplate, editorUI.ClipStyleSheet);
                clipUIs.Add(clipUI);
                clipUILookup[clip.Id] = clipUI;


                // Subscribe to clip interaction events
                // Note: ClipUIToolkit events would need to be wired here if they exist
            }

        }

        private string GetTrackTypeName()
        {
            return track.GetType().Name.Replace("Track", "").ToLower();
        }

        private string GetTrackTypeClass()
        {
            string typeName = GetTrackTypeName();
            return $"track-{typeName}";
        }

        #region Event Handlers

        private void OnTrackEnabledChanged(ChangeEvent<bool> evt)
        {
            if (track == null || editorUI == null) return;

            bool oldEnabledState = track.Enabled;
            bool newEnabledState = evt.newValue;

            // Only create and execute command if the state actually changed
            if (oldEnabledState != newEnabledState)
            {
                // Create command without the TrackUI parameter since TrackUIToolkit is different
                track.Enabled = newEnabledState;
                // TODO: Create proper command for TrackUIToolkit
            }

            // Update visual state
            if (evt.newValue)
            {
                trackElement?.RemoveFromClassList("disabled");
            }
            else
            {
                trackElement?.AddToClassList("disabled");
            }
        }

        private void OnTrackMouseDown(MouseDownEvent evt)
        {

            // Don't handle mouse events if they're on clips or clips container
            if (IsMouseOverClipsArea(evt.target as VisualElement))
            {

                // Try to find a clip element at this position and forward the event
                var clip = FindClipAtPosition(evt.localMousePosition);
                if (clip != null)
                {
                    // The clip should handle its own events, just don't interfere
                }
                else
                {
                }

                return; // Let the event propagate to clips
            }


            if (evt.button != 0) return; // Only left mouse button

            isMouseDown = true;
            mousePressStartTime = Time.realtimeSinceStartup;

            // Start long press detection
            longPressTimer = trackElement?.schedule.Execute(() =>
            {
                if (isMouseDown)
                {
                    OnLongPress();
                }
            }).StartingIn((long)(longPressDuration * 1000)); // Convert to milliseconds
        }

        private void OnTrackMouseUp(MouseUpEvent evt)
        {
            // Don't handle mouse events if they're on clips or clips container
            if (IsMouseOverClipsArea(evt.target as VisualElement))
            {
                return; // Let the event propagate to clips
            }

            if (evt.button != 0) return; // Only left mouse button

            bool wasLongPress = isLongPress;

            isMouseDown = false;
            isLongPress = false;
            longPressTimer?.Pause();
            longPressTimer = null;

            if (!wasLongPress)
            {
                OnTrackClick();
            }
        }

        private void OnTrackMouseMove(MouseMoveEvent evt)
        {
            if (isMouseDown && !isLongPress)
            {
                // Cancel long press if mouse moves too much during press
                float timeSincePress = Time.realtimeSinceStartup - mousePressStartTime;
                if (timeSincePress < longPressDuration)
                {
                    longPressTimer?.Pause();
                    longPressTimer = null;
                }
            }
        }

        private void OnTrackClick()
        {
            // Handle normal click - select track
            editorUI?.SelectTrack(track);
        }

        private void OnLongPress()
        {
            isLongPress = true;
            // Show context menu for track using FormSubmitPanelUIToolkit
            if (trackElement != null)
            {
                ShowTrackContextMenuDynamic();
            }
        }

        /// <summary>
        /// Check if the mouse event target is over the clips area (clips container or clip elements)
        /// </summary>
        private bool IsMouseOverClipsArea(VisualElement target)
        {
            if (target == null)
            {
                return false;
            }


            // Track lanes are visual overlays that should not block clip interaction
            if (target.ClassListContains("track-lane") || target.ClassListContains("track-lanes"))
            {
                return true; // Treat lane clicks as if they're on clips area (pass through)
            }

            // Check if target has "clip" class (it's a clip element)
            if (target.ClassListContains("clip"))
            {
                return true;
            }

            // Check if the target is the clips container
            if (target == clipsContainer)
            {
                return true;
            }

            // Check if the target is a child of the clips container (i.e., a clip or clip child element)
            VisualElement current = target;
            while (current != null)
            {

                if (current == clipsContainer)
                {
                    return true;
                }
                if (current == trackElement) break; // Stop at track boundary
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Find a clip element at the given local position (relative to track)
        /// </summary>
        private VisualElement FindClipAtPosition(Vector2 localPosition)
        {
            if (clipsContainer == null) return null;

            // Convert position to clips container space
            Vector2 containerLocalPos = clipsContainer.WorldToLocal(trackElement.LocalToWorld(localPosition));


            // Check each clip to see if it contains this position
            foreach (var child in clipsContainer.Children())
            {
                if (child.ClassListContains("clip"))
                {
                    var clipRect = child.contentRect;
                    var clipTranslate = child.resolvedStyle.translate;

                    // Check if position is within clip bounds
                    if (clipRect.Contains(containerLocalPos))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private void OnMenuClicked()
        {
            // Show track context menu using FormSubmitPanelUIToolkit
            if (trackElement != null)
            {
                ShowTrackContextMenuDynamic();
            }
        }

        /// <summary>
        /// Shows track context menu using FormSubmitPanelUIToolkit directly
        /// </summary>
        private void ShowTrackContextMenuDynamic()
        {
            // Create form fields for track actions
            var formFields = CreateTrackActionFields();

            // Show form panel directly
            if (FormSubmitPanelUIToolkit.HasInstance)
            {
                FormSubmitPanelUIToolkit.Instance.Show(
                    "Track Actions",
                    formFields,
                    onSubmit: OnTrackActionSubmitted,
                    onCancel: OnTrackActionCancelled,
                    editorUI?.transform
                );
            }
            else
            {
                Debug.LogWarning("FormSubmitPanelUIToolkit not available");
            }
        }

        /// <summary>
        /// Creates form fields for track context menu actions
        /// </summary>
        private List<FormFieldDefinition> CreateTrackActionFields()
        {
            var fields = new List<FormFieldDefinition>();

            // Add Clip button
            var addClipField = new FormFieldDefinition("addClip", "Add Clip", "button");
            addClipField.options = new Dictionary<string, object> { ["action"] = "addClip" };
            fields.Add(addClipField);

            // Mute Track button
            var muteField = new FormFieldDefinition("mute", track.Enabled ? "Mute Track" : "Unmute Track", "button");
            muteField.options = new Dictionary<string, object> { ["action"] = "mute" };
            fields.Add(muteField);

            // Solo Track button
            var soloField = new FormFieldDefinition("solo", "Solo Track", "button");
            soloField.options = new Dictionary<string, object> { ["action"] = "solo" };
            fields.Add(soloField);

            // Delete Track button
            var deleteField = new FormFieldDefinition("delete", "Delete Track", "button");
            deleteField.options = new Dictionary<string, object> { ["action"] = "delete" };
            fields.Add(deleteField);

            // Track Settings button
            var settingsField = new FormFieldDefinition("settings", "Track Settings", "button");
            settingsField.options = new Dictionary<string, object> { ["action"] = "settings" };
            fields.Add(settingsField);

            return fields;
        }

        /// <summary>
        /// Handle track action form submission
        /// </summary>
        private void OnTrackActionSubmitted(Dictionary<string, object> formData)
        {
            if (formData.FirstOrDefault().Value is string action)
            {
                HandleTrackAction(action);
            }
        }

        /// <summary>
        /// Handle track action form cancellation
        /// </summary>
        private void OnTrackActionCancelled()
        {
        }

        /// <summary>
        /// Execute the selected track action
        /// </summary>
        private void HandleTrackAction(string action)
        {
            switch (action)
            {
                case "addClip":
                    ShowCreateClipForm();
                    break;
                case "mute":
                    ToggleTrackMute();
                    break;
                case "solo":
                    break;
                case "delete":
                    break;
                case "settings":
                    break;
                default:
                    break;
            }
        }

        private void ShowCreateClipForm()
        {
            if (track == null)
            {
                return;
            }

            // Get the track type
            string trackType = TrackUIHelper.GetTrackTypeString(track);

            // Get form field definitions using ClipFormDefinitions
            var fieldDefinitions = ClipFormDefinitions.GetFieldsForTrackType(trackType);

            if (fieldDefinitions != null)
            {
                foreach (var field in fieldDefinitions)
                {
                }
            }
            else
            {
            }

            // Get display name for the form title
            string formTitle = ClipFormDefinitions.GetTrackTypeDisplayName(trackType);

            // Show the form
            FormSubmitPanelUIToolkit.Instance.Show(
                formTitle,
                fieldDefinitions,
                (data) => OnClipFormSubmitted(data),
                () => OnClipFormCancelled(),
                TimelineEditor?.transform
            );
        }

        private void OnClipFormSubmitted(Dictionary<string, object> formData)
        {

            // Get track type from form data (it's included as a hidden field)
            string trackType = formData.ContainsKey("trackType") ? formData["trackType"].ToString() : GetTrackTypeName();

            CreateClipFromFormData(trackType, formData);
        }

        private void CreateClipFromFormData(string trackType, Dictionary<string, object> formData)
        {
            try
            {
                // Use TrackFactory to create the clip instance properly
                IMiniClip clipInstance = TrackFactory.CreateClipFromFormData(trackType, formData);

                if (clipInstance != null)
                {
                    // Try to add clip directly since we don't have access to commands
                    editorUI.ExecuteCommand(new CreateClipCommand(track, clipInstance, this));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create clip: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnClipFormCancelled()
        {
        }

        /// <summary>
        /// Toggle track mute state
        /// </summary>
        private void ToggleTrackMute()
        {
            if (track != null)
            {
                track.Enabled = !track.Enabled;
            }
        }

        /// <summary>
        /// Public property to expose track for TimelineContextMenu compatibility
        /// This allows the context menu to access the track when using dynamic menus
        /// </summary>
        public IMiniTrack TrackForContextMenu => track;

        /// <summary>
        /// Public property to expose timeline editor for context menu actions
        /// </summary>
        public TimelineEditorUIToolkit TimelineEditorForContextMenu => editorUI;

        /// <summary>
        /// Provides a TrackUI-compatible interface for context menu operations
        /// This method allows TimelineContextMenu to work with TrackUIToolkit
        /// </summary>
        public void RebuildClipUIs()
        {
            // Clear existing clips
            foreach (var clipUI in clipUIs)
            {
                clipUI.Dispose();
            }
            clipUIs.Clear();
            clipUILookup.Clear();

            // Rebuild clips
            CreateClipElements();
        }

        /// <summary>
        /// Initialize the TrackUI wrapper with minimal required data
        /// </summary>
        private void InitializeTrackUIWrapper(TrackUI wrapper)
        {
            if (wrapper == null || track == null) return;

            // Set the track reference using reflection if needed
            var trackField = typeof(TrackUI).GetField("track",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (trackField != null)
            {
                trackField.SetValue(wrapper, track);
            }

            // Set the timeline editor reference
            var editorField = typeof(TrackUI).GetField("timelineEditor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (editorField != null && editorUI != null)
            {
                var timelineEditorUI = editorUI.GetComponent<TimelineEditorUI>();
                if (timelineEditorUI != null)
                {
                    editorField.SetValue(wrapper, timelineEditorUI);
                }
            }

            // Set the wrapper name for debugging
            wrapper.name = $"TrackUIWrapper_{track.Id}";

            // Hide the wrapper GameObject since it's just for context menu compatibility
            wrapper.gameObject.SetActive(false);
        }

        #endregion

        #region UI Creation Methods

        private void OnMuteClicked()
        {
            isMuted = !isMuted;

            if (isMuted)
            {
                trackElement?.AddToClassList("muted");
                trackMuteButton?.AddToClassList("active");
            }
            else
            {
                trackElement?.RemoveFromClassList("muted");
                trackMuteButton?.RemoveFromClassList("active");
            }
        }

        private void OnSoloClicked()
        {
            isSolo = !isSolo;

            if (isSolo)
            {
                trackElement?.AddToClassList("solo");
                trackSoloButton?.AddToClassList("active");
            }
            else
            {
                trackElement?.RemoveFromClassList("solo");
                trackSoloButton?.RemoveFromClassList("active");
            }
        }

        private void OnTrackClicked(MouseDownEvent evt)
        {
            // TODO: Handle track selection
        }

        public void UpdateLayout()
        {
            // Update clip layouts
            foreach (var clipUI in clipUIs)
            {
                clipUI.UpdatePosition();
            }
        }

        public void UpdateZoom()
        {
            // Update track width to match timeline width
            if (trackElement != null && editorUI != null)
            {
                // Set track width to match the timeline width for proper horizontal scrolling
                var timelineWidth = editorUI.TimelineWidth;
                if (timelineWidth > 0)
                {
                    trackElement.style.width = timelineWidth;
                }
            }

            // Update clip positions and sizes based on new zoom
            foreach (var clipUI in clipUIs)
            {
                clipUI.UpdatePosition();
            }
        }

        public void Dispose()
        {
            // Dispose clips
            foreach (var clipUI in clipUIs)
            {
                clipUI.Dispose();
            }
            clipUIs.Clear();

            // Remove event handlers
            if (trackEnabled != null)
                trackEnabled.UnregisterValueChangedCallback(OnTrackEnabledChanged);

            if (trackElement != null)
                trackElement.UnregisterCallback<MouseDownEvent>(OnTrackClicked);

            // Remove from hierarchy
            trackElement?.RemoveFromHierarchy();
        }

        #endregion
    }

}