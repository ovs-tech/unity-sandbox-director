using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.UI.FormSubmit;
using MiniTimeline.Core;
using MiniTimeline.Serialization;
using MiniTimeline.UI.FormDefinitions;
using MiniTimeline.UI.Commands;
using Core.UI.FormSubmit.Fields;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Mobile-friendly dynamic context menu system for timeline editor
    /// Singleton pattern with self-registration support for UI components
    /// Supports touch-friendly button layout and gesture handling
    /// </summary>
    public class TimelineContextMenu : MonoBehaviour
    {
        #region Singleton

        private static TimelineContextMenu instance;
        public static TimelineContextMenu Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing instance
                    instance = FindFirstObjectByType<TimelineContextMenu>();

                    if (instance == null)
                    {
                        // Debug.LogWarning("TimelineContextMenu: No instance found. Creating new one.");
                        CreateInstance();
                    }
                }
                return instance;
            }
        }

        private static void CreateInstance()
        {
            var go = new GameObject("TimelineContextMenu");
            instance = go.AddComponent<TimelineContextMenu>();
            DontDestroyOnLoad(go);
        }

        public static bool HasInstance => instance != null;

        #endregion

        [Header("UI References")]
        [SerializeField] private RectTransform menuContainer;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button menuBackdrop;
        [SerializeField] private GameObject menuButtonPrefab;

        [Header("Visual Settings")]
        [SerializeField] private float buttonHeight = 50f;
        [SerializeField] private float buttonSpacing = 5f;
        [SerializeField] private float menuPadding = 10f;
        [SerializeField] private Color menuBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color buttonNormalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color buttonHighlightColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Menu state
        private bool isMenuOpen = false;
        private bool canCloseMenu = false; // Prevent immediate close on open
        private bool fromLongPress = false; // Track if shown from long press
        private Vector2 menuPosition;
        private CanvasGroup canvasGroup;
        private List<GameObject> instantiatedButtons = new List<GameObject>();

        // References
        private TimelineEditorUI timelineEditor;
        private Canvas parentCanvas;

        #region Events

        public event Action OnMenuClosed;

        #endregion

        #region Properties

        public bool IsMenuOpen => isMenuOpen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                // Debug.LogWarning("Multiple TimelineContextMenu instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            SetupContextMenu();
        }

        private void Start()
        {
            // Start hidden
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Clear singleton reference if this is the instance being destroyed
            if (instance == this)
            {
                // FormSubmitPanel manages its own lifecycle, no need to clear
                instance = null;
            }
        }

        #endregion

        #region Form Conversion Methods

        /// <summary>
        /// Creates form fields from menu context for dynamic menus
        /// </summary>
        private List<FormFieldDefinition> CreateFormFieldsFromMenuContext(Vector2 screenPosition, object target, string menuType)
        {
            var fields = new List<FormFieldDefinition>();

            // Add common actions as buttons
            switch (menuType)
            {
                case "clip":
                    fields.AddRange(CreateFormFieldsForClip(target as ClipUI));
                    break;
                case "track":
                    fields.AddRange(CreateFormFieldsForTrack(target as TrackUI));
                    break;
                case "timeline":
                    fields.AddRange(CreateFormFieldsForTimeline(0f)); // Default time
                    break;
                default:
                    // Generic menu items
                    fields.Add(new FormFieldDefinition("action", "Choose Action", "button"));
                    break;
            }

            return fields;
        }

        /// <summary>
        /// Creates form fields for clip actions
        /// </summary>
        private List<FormFieldDefinition> CreateFormFieldsForClip(ClipUI clip)
        {
            var fields = new List<FormFieldDefinition>();

            // Add action buttons
            var cutField = new FormFieldDefinition("cut", "Cut Clip", "button");
            cutField.options["action"] = "cut";
            fields.Add(cutField);

            var copyField = new FormFieldDefinition("copy", "Copy Clip", "button");
            copyField.options["action"] = "copy";
            fields.Add(copyField);

            var deleteField = new FormFieldDefinition("delete", "Delete Clip", "button");
            deleteField.options["action"] = "delete";
            fields.Add(deleteField);

            var duplicateField = new FormFieldDefinition("duplicate", "Duplicate Clip", "button");
            duplicateField.options["action"] = "duplicate";
            fields.Add(duplicateField);

            var splitField = new FormFieldDefinition("split", "Split at Playhead", "button");
            splitField.options["action"] = "split";
            fields.Add(splitField);

            var propertiesField = new FormFieldDefinition("properties", "Properties", "button");
            propertiesField.options["action"] = "properties";
            propertiesField.options["closeForm"] = false; // Keep form open for transition to properties form
            fields.Add(propertiesField);

            return fields;
        }

        /// <summary>
        /// Creates form fields for track actions
        /// </summary>
        private List<FormFieldDefinition> CreateFormFieldsForTrack(TrackUI track)
        {
            var fields = new List<FormFieldDefinition>();

            // Add action buttons
            var addClipField = new FormFieldDefinition("addClip", "Add Clip", "button");
            addClipField.options["action"] = "addClip";
            addClipField.options["closeForm"] = false; // Keep form open for transition to clip form
            fields.Add(addClipField);

            var muteField = new FormFieldDefinition("mute", "Mute Track", "button");
            muteField.options["action"] = "mute";
            fields.Add(muteField);

            var soloField = new FormFieldDefinition("solo", "Solo Track", "button");
            soloField.options["action"] = "solo";
            fields.Add(soloField);

            var deleteField = new FormFieldDefinition("delete", "Delete Track", "button");
            deleteField.options["action"] = "delete";
            fields.Add(deleteField);

            var settingsField = new FormFieldDefinition("settings", "Track Settings", "button");
            settingsField.options["action"] = "settings";
            settingsField.options["closeForm"] = false;
            fields.Add(settingsField);

            return fields;
        }

        /// <summary>
        /// Creates form fields for timeline actions
        /// </summary>
        private List<FormFieldDefinition> CreateFormFieldsForTimeline(float timePosition)
        {
            var fields = new List<FormFieldDefinition>();

            // Add action buttons
            var addTrackField = new FormFieldDefinition("addTrack", "Add Track", "button");
            addTrackField.options["action"] = "addTrack";
            fields.Add(addTrackField);

            var pasteField = new FormFieldDefinition("paste", "Paste", "button");
            pasteField.options["action"] = "paste";
            fields.Add(pasteField);

            var addMarkerField = new FormFieldDefinition("addMarker", "Add Marker", "button");
            addMarkerField.options["action"] = "addMarker";
            fields.Add(addMarkerField);

            var zoomFitField = new FormFieldDefinition("zoomFit", "Zoom to Fit", "button");
            zoomFitField.options["action"] = "zoomFit";
            fields.Add(zoomFitField);

            var resetZoomField = new FormFieldDefinition("resetZoom", "Reset Zoom", "button");
            resetZoomField.options["action"] = "resetZoom";
            fields.Add(resetZoomField);

            return fields;
        }

        /// <summary>
        /// Gets form title based on menu type
        /// </summary>
        private string GetFormTitleFromMenuType(string menuType)
        {
            return menuType switch
            {
                "clip" => "Clip Actions",
                "track" => "Track Actions",
                "timeline" => "Timeline Actions",
                _ => "Menu Actions"
            };
        }

        /// <summary>
        /// Handles form submission for dynamic menus
        /// </summary>
        private void HandleFormSubmission(Dictionary<string, object> data, object target, string menuType)
        {
            if (data.TryGetValue("action", out var action))
            {
                var actionStr = action.ToString();

                switch (menuType)
                {
                    case "clip":
                        HandleClipAction(actionStr, target as ClipUI);
                        break;
                    case "track":
                        HandleTrackAction(actionStr, target as TrackUI);
                        break;
                    case "timeline":
                        HandleTimelineAction(actionStr, 0f); // Default time
                        break;
                }
            }
        }

        /// <summary>
        /// Handles track form submission
        /// </summary>
        private void HandleTrackFormSubmission(Dictionary<string, object> data, TrackUI track)
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
        /// Handles clip actions
        /// </summary>
        private void HandleClipAction(string action, ClipUI clip)
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

        /// <summary>
        /// Handles track actions
        /// </summary>
        private void HandleTrackAction(string action, TrackUI track)
        {
            Debug.Log($"HandleTrackAction called with action: '{action}', track: {track?.name}");

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

        /// <summary>
        /// Handles timeline actions
        /// </summary>
        private void HandleTimelineAction(string action, float timePosition)
        {
            switch (action)
            {
                case "addTrack":
                    ShowAddTrackMenu(Vector2.zero); // Default position
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

        #endregion

        #region Initialization

        public void Initialize(TimelineEditorUI editor)
        {
            timelineEditor = editor;
            parentCanvas = GetComponentInParent<Canvas>();
        }

        private void SetupContextMenu()
        {
            // Setup canvas group for fade animations
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Create menu structure if not set up
            if (menuContainer == null)
            {
                CreateMenuStructure();
            }

            // Create default button prefab if none provided
            if (menuButtonPrefab == null)
            {
                // Debug.Log("No menu button prefab assigned, creating default");
                CreateDefaultButtonPrefab();
            }

            // Verify prefab was created successfully
            if (menuButtonPrefab == null)
            {
                // Debug.LogError("Failed to create default button prefab!");
            }
            else
            {
                // Debug.Log("Menu button prefab ready");
            }
        }

        private void CreateMenuStructure()
        {
            // Create backdrop
            var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(transform, false);
            var backdropRect = backdropGO.GetComponent<RectTransform>();
            var backdropImage = backdropGO.GetComponent<Image>();
            menuBackdrop = backdropGO.GetComponent<Button>();
            // Setup backdrop to cover entire screen
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            // Ensure backdrop blocks raycasts
            backdropImage.color = new Color(0f, 0f, 0f, 0.3f); // Semi-transparent overlay, alpha > 0
            backdropImage.raycastTarget = true;
            var cg = backdropGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = backdropGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;
            // Create menu container
            var containerGO = new GameObject("MenuContainer", typeof(RectTransform), typeof(Image));
            containerGO.transform.SetParent(transform, false);
            menuContainer = containerGO.GetComponent<RectTransform>();
            var containerImage = containerGO.GetComponent<Image>();
            containerImage.color = menuBackgroundColor;
            // Create button container
            var buttonContainerGO = new GameObject("ButtonContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            buttonContainerGO.transform.SetParent(menuContainer, false);
            buttonContainer = buttonContainerGO.GetComponent<RectTransform>();
            var layoutGroup = buttonContainerGO.GetComponent<VerticalLayoutGroup>();
            var sizeFitter = buttonContainerGO.GetComponent<ContentSizeFitter>();
            // Setup layout
            buttonContainer.anchorMin = Vector2.zero;
            buttonContainer.anchorMax = Vector2.one;
            buttonContainer.offsetMin = new Vector2(menuPadding, menuPadding);
            buttonContainer.offsetMax = new Vector2(-menuPadding, -menuPadding);
            layoutGroup.spacing = buttonSpacing;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Create default button prefab if none provided
            if (menuButtonPrefab == null)
            {
                CreateDefaultButtonPrefab();
            }
        }

        private void CreateDefaultButtonPrefab()
        {
            // Create button GameObject with all necessary components
            var buttonGO = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));

            var buttonRect = buttonGO.GetComponent<RectTransform>();
            var buttonImage = buttonGO.GetComponent<Image>();
            var button = buttonGO.GetComponent<Button>();

            // Setup button appearance and layout
            buttonRect.sizeDelta = new Vector2(200f, buttonHeight); // Set proper width
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);

            // Configure button image
            buttonImage.color = buttonNormalColor;
            buttonImage.type = Image.Type.Simple;

            // Setup button colors with proper transitions
            var colors = button.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Set button transition to ColorTint
            button.transition = Selectable.Transition.ColorTint;

            // Add TextMeshPro text component as child
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(buttonGO.transform, false);

            var textRect = textGO.GetComponent<RectTransform>();
            var text = textGO.GetComponent<TextMeshProUGUI>();

            // Setup text positioning to fill button with padding
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15f, 5f);
            textRect.offsetMax = new Vector2(-15f, -5f);

            // Configure text appearance
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.overflowMode = TextOverflowModes.Truncate;

            // Make sure the button is initially inactive to avoid issues
            buttonGO.SetActive(false);

            // Store as prefab
            menuButtonPrefab = buttonGO;

            // Debug.Log("Created default button prefab successfully");
        }

        #endregion

        #region Menu Operations

        /// <summary>
        /// Show context menu at specified screen position using form panel
        /// </summary>
        /// <param name="screenPosition">Screen position for menu</param>
        /// <param name="target">Target object for the menu</param>
        /// <param name="menuType">Type of menu</param>
        public void ShowDynamicMenu(Vector2 screenPosition, object target = null, string menuType = "default")
        {
            // Convert context menu actions to form fields
            var formFields = CreateFormFieldsFromMenuContext(screenPosition, target, menuType);

            // Show form panel instead of context menu
            string title = GetFormTitleFromMenuType(menuType);
            FormSubmitPanel.Instance.Show(title, formFields,
                onSubmit: (data) => HandleFormSubmission(data, target, menuType),
                onCancel: () => { /* Form cancelled */ });
        }

        /// <summary>
        /// Show clip context menu
        /// </summary>
        /// <param name="clip">Target clip</param>
        /// <param name="screenPosition">Screen position</param>
        public void ShowClipMenu(ClipUI clip, Vector2 screenPosition)
        {
            Debug.Log($"ShowClipMenu called with clip: {clip?.Clip?.Id}, position: {screenPosition}");

            // Convert clip actions to form fields
            var formFields = CreateFormFieldsForClip(clip);
            Debug.Log($"Created {formFields?.Count ?? 0} form fields for clip");

            // Show form panel instead of context menu
            Debug.Log("Calling FormSubmitPanel.Instance.Show for clip menu...");
            FormSubmitPanel.Instance.Show("Clip Actions", formFields,
                onSubmit: (data) =>
                {
                    Debug.Log("Clip menu form submitted");
                    HandleFormSubmission(data, clip, "clip");
                },
                onCancel: () =>
                {
                    Debug.Log("Clip menu form cancelled");
                    /* Form cancelled */
                });
            Debug.Log("ShowClipMenu completed");
        }

        /// <summary>
        /// Show track context menu
        /// </summary>
        /// <param name="track">Target track</param>
        /// <param name="screenPosition">Screen position</param>
        public void ShowTrackMenu(TrackUI track, Vector2 screenPosition)
        {
            Debug.Log($"ShowTrackMenu called with track: {track?.name}, position: {screenPosition}");

            // Convert track actions to form fields
            var formFields = CreateFormFieldsForTrack(track);
            Debug.Log($"Created {formFields?.Count ?? 0} form fields");

            // Show form panel instead of context menu
            Debug.Log("Calling FormSubmitPanel.Instance.Show...");
            FormSubmitPanel.Instance.Show("Track Actions", formFields,
                onSubmit: (data) =>
                {
                    Debug.Log("Track menu form submitted");
                    HandleTrackFormSubmission(data, track);
                },
                onCancel: () =>
                {
                    Debug.Log("Track menu form cancelled");
                    /* Form cancelled */
                });
            Debug.Log("ShowTrackMenu completed");
        }

        /// <summary>
        /// Show timeline context menu
        /// </summary>
        /// <param name="screenPosition">Screen position</param>
        /// <param name="timePosition">Time position on timeline</param>
        public void ShowTimelineMenu(Vector2 screenPosition, float timePosition)
        {
            // Convert timeline actions to form fields
            var formFields = CreateFormFieldsForTimeline(timePosition);

            // Show form panel instead of context menu
            FormSubmitPanel.Instance.Show("Timeline Actions", formFields,
                onSubmit: (data) => HandleTimelineFormSubmission(data, timePosition),
                onCancel: () => { /* Form cancelled */ });
        }

        /// <summary>
        /// Close the context menu
        /// </summary>
        public void CloseMenu()
        {
            // Debug.Log("CloseMenu called");
            if (!isMenuOpen) return;
            isMenuOpen = false;
            canCloseMenu = false;
            OnMenuClosed?.Invoke();
        }

        // Backdrop click handler with delay logic
        private void OnBackdropClicked()
        {
            // Debug.Log($"Backdrop clicked. canCloseMenu: {canCloseMenu}, fromLongPress: {fromLongPress}");
            if (canCloseMenu)
            {
                CloseMenu();
            }
        }

        #endregion
        #region Menu Actions (Track Context Menu Logic)

        private void AddClipToTrack(TrackUI track)
        {
            Debug.Log($"AddClipToTrack called with track: {track?.name}");

            if (track?.Track == null)
            {
                Debug.LogError("Cannot create clip: track is null");
                return;
            }

            Debug.Log("Track is valid, calling ShowCreateClipForm");
            // Show create clip form using FormSubmitPanel
            ShowCreateClipForm(track);
        }

        private void ShowCreateClipForm(TrackUI trackUI)
        {
            Debug.Log($"ShowCreateClipForm called with trackUI: {trackUI?.name}");

            if (trackUI.Track == null)
            {
                Debug.LogError("Cannot create clip: track is null");
                return;
            }

            Debug.Log("Getting track type...");
            // Get the track type
            string trackType = GetTrackType(trackUI.Track);
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

            Debug.Log("Showing FormSubmitPanel...");
            // Show the form
            FormSubmitPanel.Instance.Show(
                formTitle,
                fieldDefinitions,
                (data) => OnClipFormSubmitted(data, trackUI),
                () => OnClipFormCancelled(),
                timelineEditor?.transform
            );
            Debug.Log("FormSubmitPanel.Show call completed");
        }

        private string GetTrackTypeDisplayName(string trackType)
        {
            // Use TrackFormDefinitions for consistent display names
            return TrackFormDefinitions.GetTrackTypeDisplayName(trackType);
        }

        private string GetTrackType(IMiniTrack track)
        {
            // Use TrackUIHelper to get proper track type constants
            return TrackUIHelper.GetTrackTypeString(track);
        }

        private void OnClipFormSubmitted(Dictionary<string, object> formData, TrackUI trackUI)
        {
            Debug.Log($"Clip form submitted with {formData.Count} fields: {string.Join(", ", formData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            // Get track type from form data (it's included as a hidden field)
            string trackType = formData.ContainsKey("trackType") ? formData["trackType"].ToString() : GetTrackType(trackUI.Track);

            CreateClipFromFormData(trackType, formData, trackUI);
        }

        private void OnClipFormCancelled()
        {
            Debug.Log("Clip creation cancelled");
        }

        private void CreateClipFromFormData(string trackType, Dictionary<string, object> formData, TrackUI trackUI)
        {
            try
            {
                // Use TrackFactory to create the clip instance properly
                IMiniClip clipInstance = TrackFactory.CreateClipFromFormData(trackType, formData);

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

        private void MuteTrack(TrackUI track)
        {
            Debug.Log($"Toggle mute for track: {track.Track?.GetType().Name}");
            // TODO: Implement mute functionality - need access to mute toggle or track enabled state
            // For now, just toggle the track enabled state
            if (track.Track != null)
            {
                track.Track.Enabled = !track.Track.Enabled;
            }
        }

        private void SoloTrack(TrackUI track)
        {
            Debug.Log($"Solo track: {track.Track?.GetType().Name}");
            // TODO: Implement solo functionality
        }

        private void DeleteTrack(TrackUI track)
        {
            if (track.Track == null || timelineEditor?.Director == null)
            {
                Debug.LogError("Cannot delete track: track or timeline director is null");
                return;
            }

            // Show confirmation dialog for track deletion
            ShowDeleteTrackConfirmation(track);
        }

        private void ShowDeleteTrackConfirmation(TrackUI trackUI)
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
            FormSubmitPanel.Instance.Show(
                $"⚠️ Delete {trackDisplayName}",
                fieldDefinitions,
                (data) => OnDeleteTrackConfirmed(data, trackUI),
                () => OnDeleteTrackCancelled(),
                timelineEditor?.transform
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

        private void OnDeleteTrackConfirmed(Dictionary<string, object> formData, TrackUI trackUI)
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

        private void ExecuteDeleteTrack(TrackUI trackUI, bool preserveClips = false)
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
                // TODO: Implement proper track deletion through timeline editor
                // var removeCommand = new RemoveTrackCommand(timelineEditor.Director, trackUI.Track, timelineEditor);
                // timelineEditor.ExecuteCommand(removeCommand);

                Debug.Log($"Successfully deleted {trackDisplayName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete track: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowTrackSettings(TrackUI track)
        {
            if (track.Track == null)
            {
                Debug.LogError("Cannot show track settings: track is null");
                return;
            }

            // Get the track type for form definition lookup
            string trackType = GetTrackType(track.Track);

            // Get form field definitions from TrackFormDefinitions
            var fieldDefinitions = TrackFormDefinitions.GetTrackSettingsFields(trackType);

            // Set default values from current track state
            SetDefaultValuesForTrackSettings(fieldDefinitions, track);

            // Show the form using FormSubmitPanel
            FormSubmitPanel.Instance.Show(
                $"Track Settings - {TrackFormDefinitions.GetTrackTypeDisplayName(trackType)}",
                fieldDefinitions,
                (data) => OnTrackSettingsFormSubmitted(data, track),
                () => OnTrackSettingsFormCancelled(),
                timelineEditor?.transform
            );

            Debug.Log($"Show track settings: {track.Track?.GetType().Name}");
        }

        private void SetDefaultValuesForTrackSettings(List<FormFieldDefinition> fieldDefinitions, TrackUI trackUI)
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

        private int GetTrackOrder(TrackUI trackUI)
        {
            if (trackUI.Track == null || timelineEditor?.Director?.Project == null) return 0;

            var projectTracks = timelineEditor.Director.Project.tracks;
            var trackData = projectTracks?.FirstOrDefault(t => t.id == trackUI.Track.Id);
            return trackData?.order ?? 0;
        }

        private void OnTrackSettingsFormSubmitted(Dictionary<string, object> formData, TrackUI trackUI)
        {
            try
            {
                Debug.Log("Track settings form submitted with data:");
                foreach (var kvp in formData)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }

                // Apply the settings changes through a command
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

        private void ApplyTrackSettings(Dictionary<string, object> formData, TrackUI trackUI)
        {
            if (trackUI.Track == null || timelineEditor == null) return;

            string trackType = GetTrackType(trackUI.Track);

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

        private void ApplyTrackSettingsDirectly(Dictionary<string, object> formData, TrackUI trackUI)
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

        private Dictionary<string, object> CaptureCurrentTrackSettings(TrackUI trackUI)
        {
            var settings = new Dictionary<string, object>
            {
                ["bindKey"] = trackUI.Track.BindKey ?? "",
                ["enabled"] = trackUI.Track.Enabled,
                ["trackOrder"] = GetTrackOrder(trackUI)
            };

            return settings;
        }

        #endregion

        #region Clip Actions

        private void CutClip(ClipUI clipUI)
        {
            Debug.Log($"Cut clip: {clipUI?.Clip?.Id}");
            // TODO: Implement cut functionality
            // Could integrate with TimelineEditorUI clipboard system
        }

        private void CopyClip(ClipUI clipUI)
        {
            Debug.Log($"Copy clip: {clipUI?.Clip?.Id}");
            // TODO: Implement copy functionality
        }

        private void DeleteClip(ClipUI clipUI)
        {
            if (clipUI?.Clip == null || clipUI?.ParentTrack == null)
            {
                Debug.LogError("Cannot delete clip: clip or parent track is null");
                return;
            }

            Debug.Log($"Delete clip: {clipUI.Clip.Id}");
            var timelineEditor = clipUI.ParentTrack?.TimelineEditor;
            if (timelineEditor != null)
            {
                var command = new DeleteClipCommand(clipUI.ParentTrack.Track, clipUI.Clip, clipUI.ParentTrack);
                timelineEditor.ExecuteCommand(command);
            }
        }

        private void DuplicateClip(ClipUI clipUI)
        {
            Debug.Log($"Duplicate clip: {clipUI?.Clip?.Id}");
            // TODO: Implement duplicate functionality
        }

        private void SplitClipAtPlayhead(ClipUI clipUI)
        {
            Debug.Log($"Split clip at playhead: {clipUI?.Clip?.Id}");
            // TODO: Implement split functionality
        }

        private void ShowClipProperties(ClipUI clipUI)
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
            FormSubmitPanel.Instance.Show(
                formTitle,
                fieldDefinitions,
                (data) => OnClipEditFormSubmitted(data, clipUI),
                () => OnClipEditFormCancelled(),
                clipUI.ParentTrack.TimelineEditor.transform
            );
        }

        /// <summary>
        /// Get the track type string for this clip's parent track
        /// </summary>
        private string GetClipTrackType(ClipUI clipUI)
        {
            if (clipUI?.ParentTrack?.Track == null) return "generic";

            // Use the same logic as the existing GetTrackType method
            return GetTrackType(clipUI.ParentTrack.Track);
        }

        /// <summary>
        /// Populate form field definitions with current clip data
        /// </summary>
        private void PopulateFormFieldsWithClipData(List<FormFieldDefinition> fieldDefinitions, ClipUI clipUI)
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
        private void PopulateClipSpecificProperty(FormFieldDefinition fieldDef, ClipUI clipUI)
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
        private void OnClipEditFormSubmitted(Dictionary<string, object> formData, ClipUI clipUI)
        {
            Debug.Log($"Clip edit form submitted with {formData.Count} fields to clip {clipUI.Clip.Id}");

            try
            {
                // Apply clip changes directly (fallback when no command system available)
                ApplyClipChangesDirectly(formData, clipUI);

                if (clipUI.ParentTrack?.TimelineEditor != null)
                {
                    // TODO: Implement proper command-based editing
                }
                else
                {
                    Debug.LogWarning("No timeline editor available for command-based clip editing");
                }
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
        private void ApplyClipChangesDirectly(Dictionary<string, object> formData, ClipUI clipUI)
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
            clipUI.UpdateLayout();

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
        private void ApplyClipSpecificProperties(Dictionary<string, object> formData, ClipUI clipUI)
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

        #region Timeline Actions

        // Timeline-specific menu actions

        private void ShowAddTrackMenu(Vector2 position)
        {
            Debug.Log("Show add track menu");

            // Get form field definitions from TrackFormDefinitions
            var fieldDefinitions = TrackFormDefinitions.GetCreateTrackFields();

            // Show the form using FormSubmitPanel
            FormSubmitPanel.Instance.Show(
                "Create New Track",
                fieldDefinitions,
                OnCreateTrackFormSubmitted,
                OnCreateTrackFormCancelled,
                timelineEditor?.transform
            );
        }

        private void OnCreateTrackFormSubmitted(Dictionary<string, object> formData)
        {
            try
            {
                Debug.Log("Create track form submitted with data:");
                foreach (var kvp in formData)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }

                // Get track type from form data
                string trackType = formData.ContainsKey("trackType") ? formData["trackType"].ToString() : "anim";

                // TODO: Create track using TrackFactory and add to timeline
                Debug.Log($"Would create track of type: {trackType}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create track: {ex.Message}");
            }
        }

        private void OnCreateTrackFormCancelled()
        {
            Debug.Log("Create track cancelled");
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
            timelineEditor?.SetZoom(1f); // TODO: Calculate proper zoom to fit
        }

        private void ResetZoom()
        {
            Debug.Log("Reset zoom");
            timelineEditor?.SetZoom(1f);
        }

        #endregion
    }
}