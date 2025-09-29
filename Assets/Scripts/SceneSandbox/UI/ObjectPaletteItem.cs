using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SceneSandbox.Data;

namespace SceneSandbox.UI
{
    /// <summary>
    /// UI component representing an object in the object palette
    /// Supports dragging to place objects in the scene
    /// </summary>
    public class ObjectPaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Button _selectButton;
        
        [Header("Drag Settings")]
        [SerializeField] private GameObject _dragPreviewPrefab;
        [SerializeField] private Canvas _dragCanvas;
        [SerializeField] private float _dragOpacity = 0.7f;
        
        // Data
        private SceneObjectData _objectData;
        private ObjectPalette _parentPalette;
        
        // Drag state
        private GameObject _dragPreview;
        private RectTransform _dragPreviewTransform;
        private CanvasGroup _dragCanvasGroup;
        private Vector2 _dragOffset;
        
        // Events
        public System.Action<ObjectPaletteItem> OnItemSelected;
        public System.Action<ObjectPaletteItem, Vector2> OnItemDragStarted;
        public System.Action<ObjectPaletteItem, Vector2> OnItemDragMoved;
        public System.Action<ObjectPaletteItem, Vector2> OnItemDragEnded;
        
        // Properties
        public SceneObjectData ObjectData => _objectData;
        
        private void Awake()
        {
            // Create UI components if they don't exist
            CreateUIIfMissing();
            
            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(() => OnItemSelected?.Invoke(this));
            }
            
            // Find drag canvas if not assigned
            if (_dragCanvas == null)
            {
                _dragCanvas = GetComponentInParent<Canvas>();
            }
        }
        
        /// <summary>
        /// Initialize the palette item with object data
        /// </summary>
        public void Initialize(SceneObjectData objectData, ObjectPalette parentPalette)
        {
            _objectData = objectData;
            _parentPalette = parentPalette;
            
            UpdateUI();
        }
        
        /// <summary>
        /// Update the UI elements to reflect the object data
        /// </summary>
        public void UpdateUI()
        {
            if (_objectData == null) return;
            
            // Update icon
            if (_iconImage != null)
            {
                _iconImage.sprite = _objectData.icon;
                _iconImage.color = _objectData.iconColor;
            }
            
            // Update name
            if (_nameText != null)
            {
                _nameText.text = _objectData.displayName;
            }
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            CreateDragPreview(eventData);
            OnItemDragStarted?.Invoke(this, eventData.position);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            UpdateDragPreview(eventData);
            OnItemDragMoved?.Invoke(this, eventData.position);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            DestroyDragPreview();
            
            // Check if dropped on a valid area
            var dropResult = eventData.pointerCurrentRaycast.gameObject;
            OnItemDragEnded?.Invoke(this, eventData.position);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 1)
            {
                OnItemSelected?.Invoke(this);
            }
        }
        
        private void CreateDragPreview(PointerEventData eventData)
        {
            if (_dragCanvas == null) return;
            
            // Create drag preview
            if (_dragPreviewPrefab != null)
            {
                _dragPreview = Instantiate(_dragPreviewPrefab, _dragCanvas.transform);
            }
            else
            {
                // Create a simple copy of this UI element
                _dragPreview = Instantiate(gameObject, _dragCanvas.transform);
                
                // Remove interactive components from the copy
                var button = _dragPreview.GetComponent<Button>();
                if (button != null) Destroy(button);
                
                var paletteItem = _dragPreview.GetComponent<ObjectPaletteItem>();
                if (paletteItem != null) Destroy(paletteItem);
            }
            
            _dragPreviewTransform = _dragPreview.GetComponent<RectTransform>();
            
            // Set up canvas group for opacity
            _dragCanvasGroup = _dragPreview.GetComponent<CanvasGroup>();
            if (_dragCanvasGroup == null)
            {
                _dragCanvasGroup = _dragPreview.AddComponent<CanvasGroup>();
            }
            _dragCanvasGroup.alpha = _dragOpacity;
            _dragCanvasGroup.blocksRaycasts = false;
            
            // Calculate drag offset
            RectTransform rectTransform = GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out _dragOffset);
            
            UpdateDragPreview(eventData);
        }
        
        private void UpdateDragPreview(PointerEventData eventData)
        {
            if (_dragPreview == null || _dragCanvas == null) return;
            
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragCanvas.transform as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localPosition);
            
            _dragPreviewTransform.localPosition = localPosition - _dragOffset;
        }
        
        private void DestroyDragPreview()
        {
            if (_dragPreview != null)
            {
                Destroy(_dragPreview);
                _dragPreview = null;
                _dragPreviewTransform = null;
                _dragCanvasGroup = null;
            }
        }
        
        /// <summary>
        /// Set the selection state of this item
        /// </summary>
        public void SetSelected(bool selected)
        {
            // Visual feedback for selection
            if (_selectButton != null)
            {
                var colors = _selectButton.colors;
                colors.normalColor = selected ? colors.selectedColor : Color.white;
                _selectButton.colors = colors;
            }
        }
        
        /// <summary>
        /// Set the enabled state of this item
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_selectButton != null)
            {
                _selectButton.interactable = enabled;
            }
            
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = enabled ? 1f : 0.5f;
            }
        }
        
        /// <summary>
        /// Create UI components if they don't exist
        /// </summary>
        private void CreateUIIfMissing()
        {
            if (_iconImage == null || _nameText == null || _selectButton == null)
            {
                CreatePaletteItemUI();
            }
        }
        
        /// <summary>
        /// Create the complete UI for this palette item
        /// </summary>
        private void CreatePaletteItemUI()
        {
            // Ensure we have a RectTransform
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            // Set default size for palette item
            rectTransform.sizeDelta = new Vector2(100, 120);
            
            // Create background button (this acts as the main selectable area)
            if (_selectButton == null)
            {
                _selectButton = gameObject.AddComponent<Button>();
                
                // Create background image for button
                var bgImage = gameObject.AddComponent<Image>();
                bgImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                _selectButton.targetGraphic = bgImage;
                
                // Set button colors
                var colors = _selectButton.colors;
                colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                colors.highlightedColor = new Color(0.8f, 0.8f, 1f, 1f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.9f, 1f);
                colors.selectedColor = new Color(0.6f, 0.8f, 1f, 1f);
                _selectButton.colors = colors;
            }
            
            // Create icon image
            if (_iconImage == null)
            {
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(transform, false);
                
                _iconImage = iconGO.AddComponent<Image>();
                _iconImage.color = Color.white;
                
                var iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                
                // Set default icon (a simple colored square)
                _iconImage.sprite = CreateDefaultSprite();
            }
            
            // Create name text
            if (_nameText == null)
            {
                var textGO = new GameObject("Name Text");
                textGO.transform.SetParent(transform, false);
                
                _nameText = textGO.AddComponent<Text>();
                _nameText.text = "Object";
                _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _nameText.fontSize = 12;
                _nameText.color = Color.black;
                _nameText.alignment = TextAnchor.MiddleCenter;
                _nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _nameText.verticalOverflow = VerticalWrapMode.Truncate;
                
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 0.25f);
                textRect.offsetMin = new Vector2(2, 2);
                textRect.offsetMax = new Vector2(-2, -2);
            }
        }
        
        /// <summary>
        /// Create a default sprite for items without icons
        /// </summary>
        private Sprite CreateDefaultSprite()
        {
            // Create a simple 32x32 white texture
            var texture = new Texture2D(32, 32);
            var pixels = new Color32[32 * 32];
            
            // Fill with white color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            texture.SetPixels32(pixels);
            texture.Apply();
            
            // Create sprite from texture
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// Static factory method to create a complete ObjectPaletteItem from scratch
        /// </summary>
        public static ObjectPaletteItem CreatePaletteItem(Transform parent, SceneObjectData objectData, ObjectPalette parentPalette = null)
        {
            var itemGO = new GameObject($"PaletteItem_{objectData.displayName}");
            itemGO.transform.SetParent(parent, false);
            
            var paletteItem = itemGO.AddComponent<ObjectPaletteItem>();
            
            if (parentPalette != null && objectData != null)
            {
                paletteItem.Initialize(objectData, parentPalette);
            }
            
            return paletteItem;
        }
    }
}