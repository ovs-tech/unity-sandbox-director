using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MiniTimeline.Core;
using MiniTimeline.UI.Commands;
using Debug = UnityEngine.Debug;

namespace MiniTimeline.UI
{
    /// <summary>
    /// UI representation of a timeline track
    /// Displays track header and contains clip UI elements
    /// </summary>
    public class TrackUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform headerContainer;
        [SerializeField] private RectTransform clipContainer;
        [SerializeField] private Text trackNameText;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Button trackOptionsButton;
        [SerializeField] private Image trackBackground;
        
        [Header("Visual Settings")]
        [SerializeField] private float trackHeight = 60f;
        [SerializeField] private Color[] trackColors = {
            new Color(0.3f, 0.5f, 0.7f, 0.8f),  // Blue
            new Color(0.7f, 0.3f, 0.5f, 0.8f),  // Red
            new Color(0.5f, 0.7f, 0.3f, 0.8f),  // Green
            new Color(0.7f, 0.7f, 0.3f, 0.8f),  // Yellow
            new Color(0.5f, 0.3f, 0.7f, 0.8f),  // Purple
            new Color(0.7f, 0.5f, 0.3f, 0.8f),  // Orange
        };
        
        // References
        private TimelineEditorUI timelineEditor;
        private IMiniTrack track;
        private RectTransform rectTransform;
        
        // UI State
        private readonly List<ClipUI> clipUIs = new List<ClipUI>();
        private readonly Dictionary<string, ClipUI> clipUILookup = new Dictionary<string, ClipUI>();
        private int trackColorIndex = 0;
        
        #region Properties
        
        public IMiniTrack Track => track;
        public float TrackHeight => trackHeight;
        public IReadOnlyList<ClipUI> ClipUIs => clipUIs;
        public RectTransform ClipContainer => clipContainer;
        public TimelineEditorUI TimelineEditor => timelineEditor;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // Setup track UI structure if not already set up
            if (headerContainer == null || clipContainer == null)
            {
                SetupTrackStructure();
            }
            
            // Setup event handlers
            SetupEventHandlers();
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize(TimelineEditorUI editor, IMiniTrack trackData)
        {
            timelineEditor = editor;
            track = trackData;
            
            // Assign color based on track type
            AssignTrackColor();
            
            // Update visual appearance
            UpdateTrackHeader();
            
            // Build clip UIs
            BuildClipUIs();
        }
        
        private void SetupTrackStructure()
        {
            // Set track size
            rectTransform.sizeDelta = new Vector2(0f, trackHeight);
            
            // Create header container if not exists
            if (headerContainer == null)
            {
                var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image));
                headerGO.transform.SetParent(transform, false);
                
                headerContainer = headerGO.GetComponent<RectTransform>();
                var headerImage = headerGO.GetComponent<Image>();
                
                // Setup header layout
                headerContainer.anchorMin = new Vector2(0f, 0f);
                headerContainer.anchorMax = new Vector2(0f, 1f);
                headerContainer.sizeDelta = new Vector2(200f, 0f);
                headerContainer.anchoredPosition = Vector2.zero;
                
                headerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                CreateHeaderElements(headerContainer);
            }
            
            // Create clip container if not exists
            if (clipContainer == null)
            {
                var clipGO = new GameObject("Clips", typeof(RectTransform));
                clipGO.transform.SetParent(transform, false);
                
                clipContainer = clipGO.GetComponent<RectTransform>();
                
                // Setup clip container layout
                clipContainer.anchorMin = new Vector2(0f, 0f);
                clipContainer.anchorMax = new Vector2(1f, 1f);
                clipContainer.offsetMin = new Vector2(200f, 0f); // Start after header
                clipContainer.offsetMax = Vector2.zero;
            }
            
            // Create track background
            if (trackBackground == null)
            {
                var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGO.transform.SetParent(clipContainer, false);
                bgGO.transform.SetAsFirstSibling(); // Behind clips
                
                var bgRect = bgGO.GetComponent<RectTransform>();
                trackBackground = bgGO.GetComponent<Image>();
                
                // Setup background
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                
                trackBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }
        }
        
        private void CreateHeaderElements(RectTransform parent)
        {
            // Track name text
            var nameGO = new GameObject("TrackName", typeof(RectTransform), typeof(Text));
            nameGO.transform.SetParent(parent, false);
            
            var nameRect = nameGO.GetComponent<RectTransform>();
            trackNameText = nameGO.GetComponent<Text>();
            
            nameRect.anchorMin = new Vector2(0.05f, 0.6f);
            nameRect.anchorMax = new Vector2(0.95f, 0.95f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            
            trackNameText.text = "Track";
            trackNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            trackNameText.fontSize = 12;
            trackNameText.color = Color.white;
            trackNameText.alignment = TextAnchor.MiddleLeft;
            
            // Mute toggle
            var muteGO = new GameObject("MuteToggle", typeof(RectTransform), typeof(Toggle));
            muteGO.transform.SetParent(parent, false);
            
            var muteRect = muteGO.GetComponent<RectTransform>();
            muteToggle = muteGO.GetComponent<Toggle>();
            
            muteRect.anchorMin = new Vector2(0.05f, 0.1f);
            muteRect.anchorMax = new Vector2(0.3f, 0.5f);
            muteRect.offsetMin = Vector2.zero;
            muteRect.offsetMax = Vector2.zero;
            
            // Create toggle background and checkmark
            CreateToggleGraphics(muteToggle);
            
            // Options button
            var optionsGO = new GameObject("Options", typeof(RectTransform), typeof(Button), typeof(Image));
            optionsGO.transform.SetParent(parent, false);
            
            var optionsRect = optionsGO.GetComponent<RectTransform>();
            trackOptionsButton = optionsGO.GetComponent<Button>();
            var optionsImage = optionsGO.GetComponent<Image>();
            
            optionsRect.anchorMin = new Vector2(0.7f, 0.1f);
            optionsRect.anchorMax = new Vector2(0.95f, 0.5f);
            optionsRect.offsetMin = Vector2.zero;
            optionsRect.offsetMax = Vector2.zero;
            
            optionsImage.color = new Color(0.6f, 0.6f, 0.6f);
            
            // Add "..." text to options button
            var optionsTextGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            optionsTextGO.transform.SetParent(optionsGO.transform, false);
            
            var optionsTextRect = optionsTextGO.GetComponent<RectTransform>();
            var optionsText = optionsTextGO.GetComponent<Text>();
            
            optionsTextRect.anchorMin = Vector2.zero;
            optionsTextRect.anchorMax = Vector2.one;
            optionsTextRect.offsetMin = Vector2.zero;
            optionsTextRect.offsetMax = Vector2.zero;
            
            optionsText.text = "⋯";
            optionsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            optionsText.fontSize = 14;
            optionsText.color = Color.white;
            optionsText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateToggleGraphics(Toggle toggle)
        {
            // Background
            var background = toggle.gameObject.AddComponent<Image>();
            background.color = new Color(0.3f, 0.3f, 0.3f);
            toggle.targetGraphic = background;
            
            // Checkmark
            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGO.transform.SetParent(toggle.transform, false);
            
            var checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
            var checkmarkImage = checkmarkGO.GetComponent<Image>();
            
            checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            
            checkmarkImage.color = Color.green;
            toggle.graphic = checkmarkImage;
            
            // Create simple checkmark sprite (or use a character)
            var checkmarkTextGO = new GameObject("CheckText", typeof(RectTransform), typeof(Text));
            checkmarkTextGO.transform.SetParent(checkmarkGO.transform, false);
            
            var checkTextRect = checkmarkTextGO.GetComponent<RectTransform>();
            var checkText = checkmarkTextGO.GetComponent<Text>();
            
            checkTextRect.anchorMin = Vector2.zero;
            checkTextRect.anchorMax = Vector2.one;
            checkTextRect.offsetMin = Vector2.zero;
            checkTextRect.offsetMax = Vector2.zero;
            
            checkText.text = "✓";
            checkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            checkText.fontSize = 12;
            checkText.color = Color.white;
            checkText.alignment = TextAnchor.MiddleCenter;
            
            // Remove the image component and use text instead
            DestroyImmediate(checkmarkImage);
            toggle.graphic = checkText;
        }
        
        private void SetupEventHandlers()
        {
            if (muteToggle != null)
            {
                muteToggle.onValueChanged.AddListener(OnMuteToggled);
            }
            
            if (trackOptionsButton != null)
            {
                trackOptionsButton.onClick.AddListener(OnOptionsClicked);
            }
        }
        
        #endregion
        
        #region Track Management
        
        private void AssignTrackColor()
        {
            if (track == null) return;
            
            // Assign color based on track type
            string trackType = track.GetType().Name;
            trackColorIndex = Math.Abs(trackType.GetHashCode()) % trackColors.Length;
            
            if (trackBackground != null)
            {
                Color bgColor = trackColors[trackColorIndex];
                bgColor.a = 0.3f;
                trackBackground.color = bgColor;
            }
        }
        
        private void UpdateTrackHeader()
        {
            if (track == null) return;
            
            if (trackNameText != null)
            {
                string trackTypeName = GetFriendlyTrackName(track.GetType().Name);
                trackNameText.text = $"{trackTypeName}\n{track.BindKey}";
            }
            
            if (muteToggle != null)
            {
                // Temporarily disable the toggle event to prevent recursion
                muteToggle.onValueChanged.RemoveListener(OnMuteToggled);
                muteToggle.isOn = track.Enabled;
                muteToggle.onValueChanged.AddListener(OnMuteToggled);
            }
        }
        
        /// <summary>
        /// Public method to refresh the track UI state (used by commands)
        /// </summary>
        public void RefreshUI()
        {
            UpdateTrackHeader();
        }
        
        private string GetFriendlyTrackName(string typeName)
        {
            return typeName switch
            {
                "AnimTrack" => "Animation",
                "MorphTrack" => "Morph",
                "ExpressionTrack" => "Expression",
                "CameraTrack" => "Camera",
                "PoseIKTrack" => "IK Pose",
                "AudioTrack" => "Audio",
                "FxLightTrack" => "FX Light",
                "SignalTrack" => "Events",
                _ => typeName.Replace("Track", "")
            };
        }
        
        #endregion
        
        #region Clip UI Management
        
        private void BuildClipUIs()
        {
            ClearClipUIs();
            
            if (track == null) 
            {
                Debug.LogWarning("TrackUI: BuildClipUIs called but track is null");
                return;
            }
            
            var clips = track.GetClips().ToList();
            Debug.Log($"TrackUI: Building clip UIs for track {track.Id}, found {clips.Count} clips");

            foreach (var clip in clips)
            {
                Debug.Log($"TrackUI: Creating UI for clip {clip.Id} (Start: {clip.Start}, Duration: {clip.Duration})");
                CreateClipUI(clip);
            }
            
            Debug.Log($"TrackUI: Finished building clip UIs, total created: {clipUIs.Count}");
        }
        
        private void CreateClipUI(IMiniClip clip)
        {
            if (clipContainer == null)
            {
                Debug.LogError("TrackUI: Cannot create clip UI - clipContainer is null");
                return;
            }

            if (timelineEditor?.ClipUIPrefab == null)
            {
                Debug.Log($"TrackUI: No ClipUIPrefab provided, creating simple clip UI for {clip.Id}");
                // Create a simple clip UI if no prefab is provided
                CreateSimpleClipUI(clip);
                return;
            }
            
            Debug.Log($"TrackUI: Creating clip UI from prefab for {clip.Id}");
            var clipGO = Instantiate(timelineEditor.ClipUIPrefab, clipContainer);
            var clipUI = clipGO.GetComponent<ClipUI>();
            
            if (clipUI != null)
            {
                Debug.Log($"TrackUI: Initializing clip UI for {clip.Id}");
                clipUI.Initialize(this, clip);
                clipUIs.Add(clipUI);
                clipUILookup[clip.Id] = clipUI;
                
                // Subscribe to clip interaction events
                clipUI.OnStartInteraction += OnClipStartInteraction;
                clipUI.OnEndInteraction += OnClipEndInteraction;
                
                // Verify clip was positioned
                var rect = clipUI.GetComponent<RectTransform>();
                Debug.Log($"TrackUI: Clip {clip.Id} positioned at {rect.anchoredPosition} with size {rect.sizeDelta}");
            }
            else
            {
                Debug.LogError($"TrackUI: ClipUIPrefab does not have ClipUI component for clip {clip.Id}");
            }
        }
        
        private void CreateSimpleClipUI(IMiniClip clip)
        {
            Debug.Log($"TrackUI: Creating simple clip UI for {clip.Id}");
            var clipGO = new GameObject($"Clip_{clip.Id}", typeof(RectTransform), typeof(ClipUI));
            clipGO.transform.SetParent(clipContainer, false);
            
            var clipUI = clipGO.GetComponent<ClipUI>();
            clipUI.Initialize(this, clip);
            
            clipUIs.Add(clipUI);
            clipUILookup[clip.Id] = clipUI;
            
            // Subscribe to clip interaction events
            clipUI.OnStartInteraction += OnClipStartInteraction;
            clipUI.OnEndInteraction += OnClipEndInteraction;
            
            // Verify clip was positioned
            var rect = clipUI.GetComponent<RectTransform>();
            Debug.Log($"TrackUI: Simple clip {clip.Id} positioned at {rect.anchoredPosition} with size {rect.sizeDelta}");
        }
        
        private void ClearClipUIs()
        {
            foreach (var clipUI in clipUIs)
            {
                if (clipUI != null)
                {
                    // Unsubscribe from events before destroying
                    clipUI.OnStartInteraction -= OnClipStartInteraction;
                    clipUI.OnEndInteraction -= OnClipEndInteraction;
                    
                    if (clipUI.gameObject != null)
                        DestroyImmediate(clipUI.gameObject);
                }
            }
            
            clipUIs.Clear();
            clipUILookup.Clear();
        }
        
        public ClipUI GetClipUI(string clipId)
        {
            clipUILookup.TryGetValue(clipId, out var clipUI);
            return clipUI;
        }
        
        #endregion
        
        #region Layout and Zoom
        
        public void UpdateZoom()
        {
            // Update clip positions and sizes based on new zoom
            foreach (var clipUI in clipUIs)
            {
                clipUI.UpdateLayout();
            }
        }
        
        public void UpdateLayout()
        {
            UpdateZoom();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnMuteToggled(bool muted)
        {
            if (track == null || timelineEditor == null)
            {
                Debug.LogWarning("Cannot mute track: track or timeline editor is null");
                return;
            }
            
            // Get the current enabled state before the toggle
            bool oldEnabledState = track.Enabled;
            bool newEnabledState = muted;
            
            // Only create and execute command if the state actually changed
            if (oldEnabledState != newEnabledState)
            {
                var muteCommand = new MuteTrackCommand(track, oldEnabledState, newEnabledState, this);
                timelineEditor.ExecuteCommand(muteCommand);
            }
        }
        
        private void OnOptionsClicked()
        {
            // TODO: Show track options menu
            Debug.Log($"Track options clicked for {track?.GetType().Name}");
        }
        
        #endregion
        
        #region Clip Interaction Event Handlers
        
        private void OnClipStartInteraction(ClipUI clipUI)
        {
            // Forward the event to TimelineEditorUI
            timelineEditor?.OnClipStartInteraction(clipUI);
        }
        
        private void OnClipEndInteraction(ClipUI clipUI)
        {
            // Forward the event to TimelineEditorUI
            timelineEditor?.OnClipEndInteraction(clipUI);
        }
        
        #endregion
        
        #region Hit Testing
        
        public ClipUI GetClipAtPosition(Vector2 localPosition)
        {
            foreach (var clipUI in clipUIs)
            {
                if (clipUI.ContainsPosition(localPosition))
                {
                    return clipUI;
                }
            }
            return null;
        }
        
        public bool ContainsPosition(Vector2 localPosition)
        {
            return rectTransform.rect.Contains(localPosition);
        }
        
        #endregion
    }
}