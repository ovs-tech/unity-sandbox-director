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
                if (_objectData.icon != null)
                {
                    _iconImage.sprite = _objectData.icon;
                    _iconImage.color = _objectData.iconColor;
                }
                else
                {
                    // Use default sprite if no icon provided
                    _iconImage.sprite = CreateDefaultSprite();
                    _iconImage.color = _objectData.iconColor;
                }
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
        /// Create a default sprite for items without icons
        /// </summary>
        private Sprite CreateDefaultSprite()
        {
            // Create a simple 64x64 texture with a border for visibility
            var texture = new Texture2D(64, 64);
            var pixels = new Color32[64 * 64];
            
            // Fill with a pattern to make it visible
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    int index = y * 64 + x;
                    
                    // Create a border
                    if (x == 0 || x == 63 || y == 0 || y == 63)
                    {
                        pixels[index] = new Color32(50, 50, 50, 255); // Dark border
                    }
                    // Create a gradient center
                    else if (x > 10 && x < 53 && y > 10 && y < 53)
                    {
                        byte intensity = (byte)(100 + (x + y) * 2);
                        pixels[index] = new Color32(intensity, intensity, 255, 255); // Blue gradient
                    }
                    else
                    {
                        pixels[index] = new Color32(200, 200, 200, 255); // Light gray
                    }
                }
            }
            
            texture.SetPixels32(pixels);
            texture.Apply();
            
            // Create sprite from texture
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
    }
}