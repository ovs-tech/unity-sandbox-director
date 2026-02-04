using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.Commands;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.MiniTimeline.UI.FormDefinitions;
using Systems.MiniTimeline.Serialization;

namespace Systems.MiniTimeline.UI
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

            // Debug.Log($"TrackUIToolkit initialized for track: {track.Id}");
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
                clipUI.Initialize(editorUI, clip, clipsContainer, editorUI.ClipTemplate, editorUI.ClipStyleSheet, this);
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
        /// Shows track context menu using TimelineEditorUIToolkit
        /// </summary>
        private void ShowTrackContextMenuDynamic()
        {
            if (editorUI != null)
            {
                editorUI.ShowTrackMenu(this, Vector2.zero);
            }
            else
            {
                Debug.LogWarning("TimelineEditorUIToolkit not available");
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
            Debug.Log($"HandleTrackAction called with action: '{action}', track: {track?.Id}");

            switch (action)
            {
                case "addClip":
                    Debug.Log("Calling ShowCreateClipForm");
                    ShowCreateClipForm();
                    break;
                case "mute":
                    ToggleTrackMute();
                    break;
                case "solo":
                    SoloTrack();
                    break;
                case "delete":
                    DeleteTrack();
                    break;
                case "settings":
                    ShowTrackSettings();
                    break;
                default:
                    Debug.LogWarning($"Unknown track action: {action}");
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
                // Clip creation from UI form data not yet implemented
                Debug.LogWarning("[TrackUIToolkit] Clip creation from UI form not yet implemented");
                IMiniClip clipInstance = null;

                if (clipInstance != null)
                {
                    // Try to add clip directly since we don't have access to commands
                    editorUI.ExecuteCommand(new CreateClipCommand(track, clipInstance));
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
        /// Toggle track mute state using command for undo/redo support
        /// </summary>
        private void ToggleTrackMute()
        {
            if (track == null || editorUI == null)
            {
                Debug.LogWarning("Cannot toggle mute: track or editorUI is null");
                return;
            }

            bool oldEnabled = track.Enabled;
            bool newEnabled = !oldEnabled;
            
            // Create and execute mute command
            var muteCommand = new MuteTrackCommand(track, oldEnabled, newEnabled);
            editorUI.ExecuteCommand(muteCommand);
            
            Debug.Log($"Toggled mute for track: {track.GetType().Name} (enabled: {newEnabled})");
        }

        /// <summary>
        /// Solo this track (mute all others)
        /// </summary>
        private void SoloTrack()
        {
            Debug.Log($"Solo track: {track?.GetType().Name}");
            // TODO: Implement solo functionality - need access to all tracks through timeline editor
            // For now, just log
            Debug.LogWarning("Solo track functionality not yet implemented for UI Toolkit");
        }

        /// <summary>
        /// Delete this track with confirmation
        /// </summary>
        private void DeleteTrack()
        {
            if (track == null || editorUI == null)
            {
                Debug.LogError("Cannot delete track: track or timeline editor is null");
                return;
            }

            // Show confirmation dialog for track deletion
            ShowDeleteTrackConfirmation();
        }

        /// <summary>
        /// Show track deletion confirmation dialog
        /// </summary>
        private void ShowDeleteTrackConfirmation()
        {
            string trackDisplayName = GetTrackDisplayName(track);
            int clipCount = track.GetClips()?.Count() ?? 0;

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
                OnDeleteTrackConfirmed,
                OnDeleteTrackCancelled,
                editorUI?.transform
            );
        }

        /// <summary>
        /// Get display-friendly track name
        /// </summary>
        private string GetTrackDisplayName(IMiniTrack trackData)
        {
            if (trackData == null) return "Unknown Track";

            string typeName = trackData.GetType().Name;
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

        /// <summary>
        /// Handle track deletion confirmation
        /// </summary>
        private void OnDeleteTrackConfirmed(Dictionary<string, object> formData)
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
                ExecuteDeleteTrack(preserveClips);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process track deletion confirmation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle track deletion cancellation
        /// </summary>
        private void OnDeleteTrackCancelled()
        {
            Debug.Log("Track deletion cancelled by user");
        }

        /// <summary>
        /// Execute track deletion using command for undo/redo support
        /// </summary>
        private void ExecuteDeleteTrack(bool preserveClips = false)
        {
            try
            {
                if (track == null || editorUI == null || editorUI.Director == null)
                {
                    Debug.LogError("Cannot delete track: track, editorUI, or Director is null");
                    return;
                }

                string trackDisplayName = GetTrackDisplayName(track);

                if (preserveClips)
                {
                    Debug.Log($"Preserving clips data for debugging: {track.GetClips()?.Count() ?? 0} clips");
                    // Could store clips data here for debugging if needed
                }

                // Create and execute remove track command
                var removeCommand = new RemoveTrackCommand(editorUI.Director, track);
                editorUI.ExecuteCommand(removeCommand);

                Debug.Log($"Successfully deleted {trackDisplayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete track: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Show track settings dialog
        /// </summary>
        private void ShowTrackSettings()
        {
            if (track == null)
            {
                Debug.LogError("Cannot show track settings: track is null");
                return;
            }

            // Get the track type for form definition lookup
            string trackType = TrackUIHelper.GetTrackTypeString(track);

            // Get form field definitions from TrackFormDefinitions
            var fieldDefinitions = TrackFormDefinitions.GetTrackSettingsFields(trackType);

            // Set default values from current track state
            SetDefaultValuesForTrackSettings(fieldDefinitions);

            // Show the form using FormSubmitPanelUIToolkit
            FormSubmitPanelUIToolkit.Instance.Show(
                $"Track Settings - {TrackFormDefinitions.GetTrackTypeDisplayName(trackType)}",
                fieldDefinitions,
                OnTrackSettingsFormSubmitted,
                OnTrackSettingsFormCancelled,
                editorUI?.transform
            );

            Debug.Log($"Show track settings: {track?.GetType().Name}");
        }

        /// <summary>
        /// Set default values for track settings form
        /// </summary>
        private void SetDefaultValuesForTrackSettings(List<FormFieldDefinition> fieldDefinitions)
        {
            foreach (var field in fieldDefinitions)
            {
                switch (field.name)
                {
                    case "trackName":
                        field.defaultValue = GetTrackDisplayName(track);
                        break;
                    case "bindKey":
                        field.defaultValue = track.BindKey ?? "";
                        break;
                    case "enabled":
                        field.defaultValue = track.Enabled;
                        break;
                    case "order":
                        field.defaultValue = GetTrackOrder();
                        break;
                    // Track-specific properties can be set here
                }
            }
        }

        /// <summary>
        /// Get track order in timeline
        /// </summary>
        private int GetTrackOrder()
        {
            if (track == null || editorUI?.Director == null)
                return 0;

            var tracks = editorUI.Director.GetTracks<IMiniTrack>().ToList();
            return tracks.IndexOf(track);
        }

        /// <summary>
        /// Handle track settings form submission
        /// </summary>
        private void OnTrackSettingsFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                ApplyTrackSettings(formData);
                Debug.Log("Track settings updated successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply track settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle track settings form cancellation
        /// </summary>
        private void OnTrackSettingsFormCancelled()
        {
            Debug.Log("Track settings cancelled");
        }

        /// <summary>
        /// Apply track settings from form data
        /// </summary>
        private void ApplyTrackSettings(Dictionary<string, object> formData)
        {
            if (track == null || editorUI == null)
                return;

            // Get track type for validation
            string trackType = TrackUIHelper.GetTrackTypeString(track);

            // Validate settings
            if (!TrackFormDefinitions.ValidateTrackSettings(formData, trackType, out string errorMessage))
            {
                Debug.LogError($"Track settings validation failed: {errorMessage}");
                return;
            }

            // Apply settings directly (or create command for undo/redo)
            ApplyTrackSettingsDirectly(formData);
        }

        /// <summary>
        /// Apply track settings using command for undo/redo support
        /// </summary>
        private void ApplyTrackSettingsDirectly(Dictionary<string, object> formData)
        {
            if (track == null || editorUI == null || editorUI.Director == null)
                return;

            // Capture old settings for undo
            var oldSettings = new Dictionary<string, object>();
            
            foreach (var kvp in formData)
            {
                switch (kvp.Key)
                {
                    case "enabled":
                        oldSettings["enabled"] = track.Enabled;
                        break;
                    case "bindKey":
                        oldSettings["bindKey"] = track.BindKey ?? "";
                        break;
                    case "order":
                        oldSettings["order"] = GetTrackOrder();
                        break;
                    default:
                        // Capture track-specific properties
                        var property = track.GetType().GetProperty(kvp.Key);
                        if (property != null && property.CanRead)
                        {
                            oldSettings[kvp.Key] = property.GetValue(track);
                        }
                        break;
                }
            }

            // Create and execute command
            var updateCommand = new UpdateTrackSettingsCommand(
                track, 
                oldSettings, 
                formData, 
                editorUI.Director
            );
            
            editorUI.ExecuteCommand(updateCommand);
            
            Debug.Log($"Applied track settings for {GetTrackDisplayName(track)} via command");
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
            // Check if clip count matches track clip count
            var currentClips = track?.GetClips()?.ToList();
            if (currentClips != null && currentClips.Count != clipUIs.Count)
            {
                RebuildClipUIs();
                return;
            }

            // Also check if any clip IDs don't match (in case of replacement)
            if (currentClips != null)
            {
                foreach (var clip in currentClips)
                {
                    if (!clipUILookup.ContainsKey(clip.Id))
                    {
                        RebuildClipUIs();
                        return;
                    }
                }
            }

            // Update clip layouts
            foreach (var clipUI in clipUIs)
            {
                clipUI.UpdatePosition();
            }
        }

        /// <summary>
        /// Refresh clips to match data model (rebuild if structure changed, update layout if not)
        /// </summary>
        public void RefreshClips()
        {
            if (track == null) return;

            var currentClips = track.GetClips().ToList();

            // Check if rebuild is needed
            bool rebuildNeeded = false;

            if (currentClips.Count != clipUIs.Count)
            {
                rebuildNeeded = true;
            }
            else
            {
                // Check if any clip reference doesn't match
                for (int i = 0; i < currentClips.Count; i++)
                {
                    if (clipUIs[i].Clip != currentClips[i])
                    {
                        rebuildNeeded = true;
                        break;
                    }
                }
            }

            if (rebuildNeeded)
            {
                RebuildClipUIs();
            }
            else
            {
                UpdateLayout();
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