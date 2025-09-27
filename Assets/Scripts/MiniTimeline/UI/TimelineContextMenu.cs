using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
                        Debug.LogWarning("TimelineContextMenu: No instance found. Creating new one.");
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
    private List<ContextMenuItem> currentMenuItems = new List<ContextMenuItem>();
    private List<GameObject> instantiatedButtons = new List<GameObject>();
    private MenuContext currentContext;
        
        // References
        private TimelineEditorUI timelineEditor;
        private Canvas parentCanvas;
        
        // Dynamic menu system
        private readonly ContextMenuRegistry registry = ContextMenuRegistry.Instance;
        
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
                Debug.LogWarning("Multiple TimelineContextMenu instances detected. Destroying duplicate.");
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
                registry.Clear(); // Clean up all registrations
                instance = null;
            }
        }
        
        #endregion
        
        #region Dynamic Registration
        
        /// <summary>
        /// Register a component that can provide context menu items
        /// </summary>
        /// <param name="provider">Component to register</param>
        public static void RegisterMenuProvider(IContextMenuRegisterable provider)
        {
            if (HasInstance)
            {
                Instance.registry.Register(provider);
            }
            else
            {
                // Queue for registration when instance is available
                Debug.LogWarning($"TimelineContextMenu: Attempted to register {provider.ComponentId} before instance is available");
            }
        }
        
        /// <summary>
        /// Unregister a context menu provider
        /// </summary>
        /// <param name="provider">Component to unregister</param>
        public static void UnregisterMenuProvider(IContextMenuRegisterable provider)
        {
            if (HasInstance && Instance.registry != null)
            {
                Instance.registry.Unregister(provider);
            }
        }
        
        /// <summary>
        /// Unregister a provider by component ID
        /// </summary>
        /// <param name="componentId">ID of component to unregister</param>
        public static void UnregisterMenuProvider(string componentId)
        {
            if (HasInstance && Instance.registry != null)
            {
                Instance.registry.Unregister(componentId);
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
                Debug.Log("No menu button prefab assigned, creating default");
                CreateDefaultButtonPrefab();
            }
            
            // Verify prefab was created successfully
            if (menuButtonPrefab == null)
            {
                Debug.LogError("Failed to create default button prefab!");
            }
            else
            {
                Debug.Log("Menu button prefab ready");
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
            
            Debug.Log("Created default button prefab successfully");
        }
        
        #endregion
        
        #region Menu Operations
        
        /// <summary>
        /// Show context menu at specified screen position using dynamic registration
        /// </summary>
        /// <param name="screenPosition">Screen position for menu</param>
        /// <param name="target">Target object for the menu</param>
        /// <param name="menuType">Type of menu</param>
        public void ShowDynamicMenu(Vector2 screenPosition, object target = null, string menuType = "default")
        {
            var context = new MenuContext(screenPosition, target, menuType);
            var dynamicMenuItems = registry.BuildContextMenu(context);
            
            ShowMenu(screenPosition, dynamicMenuItems, context);
        }
        
        /// <summary>
        /// Show context menu at specified screen position
        /// </summary>
        /// <param name="screenPosition">Screen position for menu</param>
        /// <param name="menuItems">Menu items to display</param>
        /// <param name="context">Optional menu context</param>
        public void ShowMenu(Vector2 screenPosition, List<ContextMenuItem> menuItems, MenuContext context = null)
        {
            // Don't close and immediately reopen - this causes the immediate close issue
            // Instead, update the content if menu is already open
            if (isMenuOpen)
            {
                Debug.Log("Menu already open - updating content instead of closing/reopening");
                // Update the menu content and position without closing
                currentMenuItems = menuItems ?? new List<ContextMenuItem>();
                menuPosition = screenPosition;
                currentContext = context;
                
                // Rebuild menu buttons with new content
                BuildMenuButtons();
                
                // Reposition menu
                PositionMenu();
                
                return;
            }
            
            Debug.Log("Showing new menu");
            currentMenuItems = menuItems ?? new List<ContextMenuItem>();
            menuPosition = screenPosition;
            currentContext = context;
            
            // Build menu buttons
            BuildMenuButtons();
            
            // Position menu
            PositionMenu();
            
            // Show menu with animation
            ShowMenuAnimated();
        }
        


        
        /// <summary>
        /// Show track context menu
        /// </summary>
        /// <param name="track">Target track</param>
        /// <param name="screenPosition">Screen position</param>
        public void ShowTrackMenu(TrackUI track, Vector2 screenPosition)
        {
            // Try dynamic registration first
            var context = new MenuContext(screenPosition, track, "track");
            var dynamicMenuItems = registry.BuildContextMenu(context);
            
            // Fall back to static items if no dynamic items found
            if (dynamicMenuItems.Count == 0)
            {
                var menuItems = new List<ContextMenuItem>
                {
                    new ContextMenuItem("Add Clip", () => AddClipToTrack(track), MenuCategory.Create, MenuPriority.Normal, "➕"),
                    new ContextMenuItem("Mute Track", () => MuteTrack(track), MenuCategory.Action, MenuPriority.Normal, "🔇"),
                    new ContextMenuItem("Solo Track", () => SoloTrack(track), MenuCategory.Action, MenuPriority.Normal, "🔊"),
                    new ContextMenuItem("Delete Track", () => DeleteTrack(track), MenuCategory.Edit, MenuPriority.Delete, "🗑️"),
                    new ContextMenuItem("Track Settings", () => ShowTrackSettings(track), MenuCategory.Properties, MenuPriority.Properties, "⚙️")
                };
                ShowMenu(screenPosition, menuItems, context);
            }
            else
            {
                ShowMenu(screenPosition, dynamicMenuItems, context);
            }
        }
        
        /// <summary>
        /// Show timeline context menu
        /// </summary>
        /// <param name="screenPosition">Screen position</param>
        /// <param name="timePosition">Time position on timeline</param>
        public void ShowTimelineMenu(Vector2 screenPosition, float timePosition)
        {
            // Try dynamic registration first
            var context = new MenuContext(screenPosition, null, "timeline");
            context.SetProperty("timePosition", timePosition);
            
            var dynamicMenuItems = registry.BuildContextMenu(context);
            
            // Fall back to static items if no dynamic items found
            if (dynamicMenuItems.Count == 0)
            {
                var menuItems = new List<ContextMenuItem>
                {
                    new ContextMenuItem("Add Track", () => ShowAddTrackMenu(screenPosition), MenuCategory.Create, MenuPriority.Normal, "➕"),
                    new ContextMenuItem("Paste", () => PasteAtTime(timePosition), MenuCategory.Edit, MenuPriority.Paste, "📋"),
                    new ContextMenuItem("Add Marker", () => AddMarker(timePosition), MenuCategory.Create, MenuPriority.Low, "📍"),
                    new ContextMenuItem("Zoom to Fit", () => ZoomToFit(), MenuCategory.View, MenuPriority.Normal, "🔍"),
                    new ContextMenuItem("Reset Zoom", () => ResetZoom(), MenuCategory.View, MenuPriority.Low, "🔍")
                };
                ShowMenu(screenPosition, menuItems, context);
            }
            else
            {
                ShowMenu(screenPosition, dynamicMenuItems, context);
            }
        }
        
        /// <summary>
        /// Close the context menu
        /// </summary>
        public void CloseMenu()
        {
            Debug.Log("CloseMenu called");
            if (!isMenuOpen) return;
            isMenuOpen = false;
            canCloseMenu = false;
            // Hide menu with animation
            HideMenuAnimated();
            OnMenuClosed?.Invoke();
        }

        // Backdrop click handler with delay logic
        private void OnBackdropClicked()
        {
            Debug.Log($"Backdrop clicked. canCloseMenu: {canCloseMenu}, fromLongPress: {fromLongPress}");
            if (canCloseMenu)
            {
                CloseMenu();
            }
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
            if (menuButtonPrefab == null || buttonContainer == null) 
            {
                Debug.LogError("Cannot create menu button: missing prefab or container");
                return;
            }
            
            // Handle separators
            if (item.isSeparator)
            {
                CreateSeparator();
                return;
            }
            
            // Instantiate the button prefab
            var buttonGO = Instantiate(menuButtonPrefab, buttonContainer);
            buttonGO.SetActive(true); // Make sure the instantiated button is active
            
            // Setup button text
            var text = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string buttonText = item.text;
                if (!string.IsNullOrEmpty(item.icon))
                {
                    buttonText = $"{item.icon} {buttonText}";
                }
                text.text = buttonText;
                
                // Apply custom text color if specified
                if (item.textColor.HasValue)
                {
                    text.color = item.textColor.Value;
                }
            }
            else
            {
                Debug.LogWarning("No TextMeshProUGUI component found in menu button prefab");
            }
            
            // Setup button action
            var button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"Menu button clicked: {item.text}");
                    item.action?.Invoke();
                    CloseMenu();
                });
                
                // Set button interactable state
                button.interactable = item.enabled;
            }
            else
            {
                Debug.LogWarning("No Button component found in menu button prefab");
            }
            
            instantiatedButtons.Add(buttonGO);
            Debug.Log($"Created menu button: {item.text}");
        }
        
        private void CreateSeparator()
        {
            if (buttonContainer == null) return;
            
            // Create a simple separator line
            var separatorGO = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            separatorGO.transform.SetParent(buttonContainer, false);
            
            var rectTransform = separatorGO.GetComponent<RectTransform>();
            var image = separatorGO.GetComponent<Image>();
            
            // Set up separator appearance
            rectTransform.sizeDelta = new Vector2(0f, 1f);
            image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            instantiatedButtons.Add(separatorGO);
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
            Debug.Log($"ShowMenuAnimated called. fromLongPress: {fromLongPress}");
            gameObject.SetActive(true);
            isMenuOpen = true;
            canCloseMenu = false;
            // Always assign correct backdrop handler and remove old ones
            if (menuBackdrop != null)
            {
                menuBackdrop.onClick.RemoveAllListeners();
                menuBackdrop.onClick.AddListener(OnBackdropClicked);
                // Force backdrop to be topmost
                menuBackdrop.transform.SetAsLastSibling();
            }
            // Also ensure menuContainer is above backdrop
            if (menuContainer != null)
            {
                menuContainer.transform.SetAsLastSibling();
            }
            // Simple fade in animation
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            LeanTween.alphaCanvas(canvasGroup, 1f, 0.2f)
                .setEaseOutQuart()
                .setOnComplete(() => {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                });
            // Scale animation for menu container
            if (menuContainer != null)
            {
                menuContainer.localScale = Vector3.zero;
                LeanTween.scale(menuContainer.gameObject, Vector3.one, 0.2f)
                    .setEaseOutBack();
            }
            // Allow backdrop to close menu after one frame
            StartCoroutine(EnableMenuCloseNextFrame());
        }

        private System.Collections.IEnumerator EnableMenuCloseNextFrame()
        {
            // Use longer delay if menu was shown from long press
            if (fromLongPress)
            {
                Debug.Log("Enabling backdrop close with long press delay (0.3s)");
                yield return new WaitForSeconds(0.3f); // Longer delay for long press
                fromLongPress = false; // Reset flag
            }
            else
            {
                Debug.Log("Enabling backdrop close with normal delay (1 frame)");
                yield return null; // Single frame delay for normal cases
            }
            canCloseMenu = true;
            Debug.Log("Backdrop close enabled");
        }
        
        private void HideMenuAnimated()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
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
}