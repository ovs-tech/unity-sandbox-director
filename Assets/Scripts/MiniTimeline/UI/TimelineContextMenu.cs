using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Mobile-friendly context menu system for timeline editor
    /// Supports touch-friendly button layout and gesture handling
    /// </summary>
    public class TimelineContextMenu : MonoBehaviour
    {
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
        private Vector2 menuPosition;
        private CanvasGroup canvasGroup;
        private List<ContextMenuItem> currentMenuItems = new List<ContextMenuItem>();
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
            SetupContextMenu();
        }
        
        private void Start()
        {
            // Start hidden
            gameObject.SetActive(false);
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
            
            // Setup backdrop for closing menu
            if (menuBackdrop != null)
            {
                menuBackdrop.onClick.AddListener(CloseMenu);
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
            
            backdropImage.color = new Color(0f, 0f, 0f, 0.3f); // Semi-transparent overlay
            
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
            var buttonGO = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            
            var buttonRect = buttonGO.GetComponent<RectTransform>();
            var buttonImage = buttonGO.GetComponent<Image>();
            var button = buttonGO.GetComponent<Button>();
            
            // Setup button appearance
            buttonRect.sizeDelta = new Vector2(0f, buttonHeight);
            buttonImage.color = buttonNormalColor;
            
            // Setup button colors
            var colors = button.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = colors;
            
            // Add text component
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.GetComponent<RectTransform>();
            var text = textGO.GetComponent<Text>();
            
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
            
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            
            menuButtonPrefab = buttonGO;
        }
        
        #endregion
        
        #region Menu Operations
        
        /// <summary>
        /// Show context menu at specified screen position
        /// </summary>
        /// <param name="screenPosition">Screen position for menu</param>
        /// <param name="menuItems">Menu items to display</param>
        public void ShowMenu(Vector2 screenPosition, List<ContextMenuItem> menuItems)
        {
            if (isMenuOpen) CloseMenu();
            
            currentMenuItems = menuItems ?? new List<ContextMenuItem>();
            menuPosition = screenPosition;
            
            // Build menu buttons
            BuildMenuButtons();
            
            // Position menu
            PositionMenu();
            
            // Show menu with animation
            ShowMenuAnimated();
        }
        
        /// <summary>
        /// Show clip context menu
        /// </summary>
        /// <param name="clip">Target clip</param>
        /// <param name="screenPosition">Screen position</param>
        public void ShowClipMenu(ClipUI clip, Vector2 screenPosition)
        {
            var menuItems = new List<ContextMenuItem>
            {
                new ContextMenuItem("Copy", () => CopyClip(clip), "📋"),
                new ContextMenuItem("Cut", () => CutClip(clip), "✂️"),
                new ContextMenuItem("Delete", () => DeleteClip(clip), "🗑️"),
                new ContextMenuItem("Duplicate", () => DuplicateClip(clip), "📄"),
                new ContextMenuItem("Properties", () => ShowClipProperties(clip), "⚙️")
            };
            
            ShowMenu(screenPosition, menuItems);
        }
        
        /// <summary>
        /// Show track context menu
        /// </summary>
        /// <param name="track">Target track</param>
        /// <param name="screenPosition">Screen position</param>
        public void ShowTrackMenu(TrackUI track, Vector2 screenPosition)
        {
            var menuItems = new List<ContextMenuItem>
            {
                new ContextMenuItem("Add Clip", () => AddClipToTrack(track), "➕"),
                new ContextMenuItem("Mute Track", () => MuteTrack(track), "🔇"),
                new ContextMenuItem("Solo Track", () => SoloTrack(track), "🔊"),
                new ContextMenuItem("Delete Track", () => DeleteTrack(track), "🗑️"),
                new ContextMenuItem("Track Settings", () => ShowTrackSettings(track), "⚙️")
            };
            
            ShowMenu(screenPosition, menuItems);
        }
        
        /// <summary>
        /// Show timeline context menu
        /// </summary>
        /// <param name="screenPosition">Screen position</param>
        /// <param name="timePosition">Time position on timeline</param>
        public void ShowTimelineMenu(Vector2 screenPosition, float timePosition)
        {
            var menuItems = new List<ContextMenuItem>
            {
                new ContextMenuItem("Add Track", () => ShowAddTrackMenu(screenPosition), "➕"),
                new ContextMenuItem("Paste", () => PasteAtTime(timePosition), "📋"),
                new ContextMenuItem("Add Marker", () => AddMarker(timePosition), "📍"),
                new ContextMenuItem("Zoom to Fit", () => ZoomToFit(), "🔍"),
                new ContextMenuItem("Reset Zoom", () => ResetZoom(), "🔍")
            };
            
            ShowMenu(screenPosition, menuItems);
        }
        
        /// <summary>
        /// Close the context menu
        /// </summary>
        public void CloseMenu()
        {
            if (!isMenuOpen) return;
            
            isMenuOpen = false;
            
            // Hide menu with animation
            HideMenuAnimated();
            
            OnMenuClosed?.Invoke();
        }
        
        #endregion
        
        #region Menu Building
        
        private void BuildMenuButtons()
        {
            // Clear existing buttons
            ClearMenuButtons();
            
            // Create buttons for each menu item
            foreach (var item in currentMenuItems)
            {
                CreateMenuButton(item);
            }
        }
        
        private void CreateMenuButton(ContextMenuItem item)
        {
            if (menuButtonPrefab == null || buttonContainer == null) return;
            
            var buttonGO = Instantiate(menuButtonPrefab, buttonContainer);
            
            // Setup button text
            var text = buttonGO.GetComponentInChildren<Text>();
            if (text != null)
            {
                string buttonText = item.text;
                if (!string.IsNullOrEmpty(item.icon))
                {
                    buttonText = $"{item.icon} {buttonText}";
                }
                text.text = buttonText;
            }
            
            // Setup button action
            var button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    item.action?.Invoke();
                    CloseMenu();
                });
            }
            
            instantiatedButtons.Add(buttonGO);
        }
        
        private void ClearMenuButtons()
        {
            foreach (var button in instantiatedButtons)
            {
                if (button != null)
                    DestroyImmediate(button);
            }
            instantiatedButtons.Clear();
        }
        
        #endregion
        
        #region Menu Positioning
        
        private void PositionMenu()
        {
            if (menuContainer == null || parentCanvas == null) return;
            
            // Convert screen position to canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform, 
                menuPosition, 
                parentCanvas.worldCamera, 
                out Vector2 localPosition);
            
            // Calculate menu size
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer);
            Vector2 menuSize = buttonContainer.sizeDelta;
            menuSize.x = 200f; // Fixed width
            menuSize.y += menuPadding * 2f; // Add padding
            
            // Position menu to avoid screen edges
            Vector2 canvasSize = (parentCanvas.transform as RectTransform).sizeDelta;
            
            float x = localPosition.x;
            float y = localPosition.y;
            
            // Adjust X position to keep menu on screen
            if (x + menuSize.x > canvasSize.x * 0.5f)
            {
                x = localPosition.x - menuSize.x;
            }
            
            // Adjust Y position to keep menu on screen
            if (y - menuSize.y < -canvasSize.y * 0.5f)
            {
                y = localPosition.y + menuSize.y;
            }
            
            // Set menu container position and size
            menuContainer.sizeDelta = menuSize;
            menuContainer.anchoredPosition = new Vector2(x, y);
        }
        
        #endregion
        
        #region Animation
        
        private void ShowMenuAnimated()
        {
            gameObject.SetActive(true);
            isMenuOpen = true;
            
            // Simple fade in animation
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            
            LeanTween.alphaCanvas(canvasGroup, 1f, 0.2f)
                .setEaseOutQuart()
                .setOnComplete(() => {
                    canvasGroup.interactable = true;
                });
            
            // Scale animation for menu container
            if (menuContainer != null)
            {
                menuContainer.localScale = Vector3.zero;
                LeanTween.scale(menuContainer.gameObject, Vector3.one, 0.2f)
                    .setEaseOutBack();
            }
        }
        
        private void HideMenuAnimated()
        {
            canvasGroup.interactable = false;
            
            LeanTween.alphaCanvas(canvasGroup, 0f, 0.15f)
                .setEaseInQuart()
                .setOnComplete(() => {
                    gameObject.SetActive(false);
                });
            
            // Scale animation for menu container
            if (menuContainer != null)
            {
                LeanTween.scale(menuContainer.gameObject, Vector3.zero, 0.15f)
                    .setEaseInBack();
            }
        }
        
        #endregion
        
        #region Menu Actions (Placeholder implementations)
        
        private void CopyClip(ClipUI clip)
        {
            Debug.Log($"Copy clip: {clip.Clip.Id}");
            // TODO: Implement copy functionality
        }
        
        private void CutClip(ClipUI clip)
        {
            Debug.Log($"Cut clip: {clip.Clip.Id}");
            // TODO: Implement cut functionality
        }
        
        private void DeleteClip(ClipUI clip)
        {
            Debug.Log($"Delete clip: {clip.Clip.Id}");
            // TODO: Create delete command
        }
        
        private void DuplicateClip(ClipUI clip)
        {
            Debug.Log($"Duplicate clip: {clip.Clip.Id}");
            // TODO: Implement duplicate functionality
        }
        
        private void ShowClipProperties(ClipUI clip)
        {
            Debug.Log($"Show properties for clip: {clip.Clip.Id}");
            // TODO: Show clip properties panel
        }
        
        private void AddClipToTrack(TrackUI track)
        {
            Debug.Log($"Add clip to track: {track.Track.Id}");
            // TODO: Show clip creation dialog
        }
        
        private void MuteTrack(TrackUI track)
        {
            Debug.Log($"Mute track: {track.Track.Id}");
            // TODO: Implement track muting
        }
        
        private void SoloTrack(TrackUI track)
        {
            Debug.Log($"Solo track: {track.Track.Id}");
            // TODO: Implement track soloing
        }
        
        private void DeleteTrack(TrackUI track)
        {
            Debug.Log($"Delete track: {track.Track.Id}");
            // TODO: Create delete track command
        }
        
        private void ShowTrackSettings(TrackUI track)
        {
            Debug.Log($"Show track settings: {track.Track.Id}");
            // TODO: Show track settings panel
        }
        
        private void ShowAddTrackMenu(Vector2 position)
        {
            Debug.Log("Show add track menu");
            // TODO: Show submenu for track types
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
    
    /// <summary>
    /// Data structure for context menu items
    /// </summary>
    [System.Serializable]
    public class ContextMenuItem
    {
        public string text;
        public Action action;
        public string icon;
        public bool enabled;
        
        public ContextMenuItem(string itemText, Action itemAction, string itemIcon = "", bool itemEnabled = true)
        {
            text = itemText;
            action = itemAction;
            icon = itemIcon;
            enabled = itemEnabled;
        }
    }
}