using UnityEngine;
using UnityEngine.UIElements;
using SceneSandbox.Data;

namespace SceneSandbox.UI
{
    /// <summary>
    /// UI component representing an object in the object palette using UI Toolkit
    /// Supports dragging to place objects in the scene
    /// </summary>
    public class ObjectPaletteItemUIToolkit
    {
        // Visual Elements
        private VisualElement _rootElement;
        private VisualElement _iconContainer;
        private VisualElement _iconElement;
        private Label _nameLabel;
        
        // Data
        private SceneObjectData _objectData;
        private ObjectPaletteUIToolkit _parentPalette;
        
        // Drag state
        private bool _isDragging;
        private Vector2 _dragStartPosition;
        private VisualElement _dragPreview;
        private float _dragOpacity = 0.7f;
        
        // Events
        public System.Action<ObjectPaletteItemUIToolkit> OnItemSelected;
        public System.Action<ObjectPaletteItemUIToolkit, Vector2> OnItemDragStarted;
        public System.Action<ObjectPaletteItemUIToolkit, Vector2> OnItemDragMoved;
        public System.Action<ObjectPaletteItemUIToolkit, Vector2> OnItemDragEnded;
        
        // Properties
        public SceneObjectData ObjectData => _objectData;
        public VisualElement RootElement => _rootElement;
        
        /// <summary>
        /// Initialize the palette item with object data
        /// </summary>
        public void Initialize(SceneObjectData objectData, ObjectPaletteUIToolkit parentPalette, 
                              VisualTreeAsset itemTemplate = null, Vector2 itemSize = default)
        {
            _objectData = objectData;
            _parentPalette = parentPalette;
            
            if (itemSize == default)
            {
                itemSize = new Vector2(100, 120);
            }
            
            CreateUI(itemTemplate, itemSize);
            UpdateUI();
            SetupInteractions();
        }
        
        private void CreateUI(VisualTreeAsset itemTemplate, Vector2 itemSize)
        {
            if (itemTemplate != null)
            {
                _rootElement = itemTemplate.CloneTree();
                QueryUIElements();
            }
            else
            {
                CreateUIFromCode(itemSize);
            }
        }
        
        private void CreateUIFromCode(Vector2 itemSize)
        {
            // Root container
            _rootElement = new VisualElement();
            _rootElement.name = "palette-item";
            _rootElement.AddToClassList("palette-item");
            _rootElement.style.width = itemSize.x;
            _rootElement.style.height = itemSize.y;
            _rootElement.style.marginRight = 8;
            _rootElement.style.marginBottom = 8;
            _rootElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _rootElement.style.borderTopLeftRadius = 4;
            _rootElement.style.borderTopRightRadius = 4;
            _rootElement.style.borderBottomLeftRadius = 4;
            _rootElement.style.borderBottomRightRadius = 4;
            _rootElement.style.borderTopWidth = 1;
            _rootElement.style.borderBottomWidth = 1;
            _rootElement.style.borderLeftWidth = 1;
            _rootElement.style.borderRightWidth = 1;
            _rootElement.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _rootElement.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _rootElement.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _rootElement.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Icon container (top 70%)
            _iconContainer = new VisualElement();
            _iconContainer.name = "icon-container";
            _iconContainer.style.flexGrow = 1;
            _iconContainer.style.justifyContent = Justify.Center;
            _iconContainer.style.alignItems = Align.Center;
            _rootElement.Add(_iconContainer);
            
            // Icon element
            _iconElement = new VisualElement();
            _iconElement.name = "icon";
            _iconElement.AddToClassList("palette-item__icon");
            _iconElement.style.width = 64;
            _iconElement.style.height = 64;
            _iconElement.style.backgroundColor = new Color(0.4f, 0.4f, 0.8f, 1f);
            _iconElement.style.borderTopLeftRadius = 4;
            _iconElement.style.borderTopRightRadius = 4;
            _iconElement.style.borderBottomLeftRadius = 4;
            _iconElement.style.borderBottomRightRadius = 4;
            _iconContainer.Add(_iconElement);
            
            // Name label (bottom 25%)
            _nameLabel = new Label();
            _nameLabel.name = "name-label";
            _nameLabel.AddToClassList("palette-item__name");
            _nameLabel.style.height = 30;
            _nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _nameLabel.style.fontSize = 12;
            _nameLabel.style.color = Color.white;
            _nameLabel.style.overflow = Overflow.Hidden;
            _nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            _nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
            _rootElement.Add(_nameLabel);
        }
        
        private void QueryUIElements()
        {
            _iconContainer = _rootElement.Q<VisualElement>("icon-container");
            _iconElement = _rootElement.Q<VisualElement>("icon");
            _nameLabel = _rootElement.Q<Label>("name-label");
        }
        
        /// <summary>
        /// Update the UI elements to reflect the object data
        /// </summary>
        public void UpdateUI()
        {
            if (_objectData == null) return;
            
            // Update icon
            if (_iconElement != null)
            {
                if (_objectData.icon != null)
                {
                    _iconElement.style.backgroundImage = new StyleBackground(_objectData.icon);
                    _iconElement.style.unityBackgroundImageTintColor = _objectData.iconColor;
                }
                else
                {
                    // Use default background color if no icon
                    _iconElement.style.backgroundColor = _objectData.iconColor;
                }
            }
            
            // Update name
            if (_nameLabel != null)
            {
                _nameLabel.text = _objectData.displayName;
            }
        }
        
        private void SetupInteractions()
        {
            if (_rootElement == null) return;
            
            // Click to select
            _rootElement.RegisterCallback<ClickEvent>(OnClick);
            
            // Drag to place
            _rootElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _rootElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _rootElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _rootElement.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            
            // Hover effects
            _rootElement.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            _rootElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
        
        private void OnClick(ClickEvent evt)
        {
            if (!_isDragging)
            {
                OnItemSelected?.Invoke(this);
            }
        }
        
        private void OnPointerDown(PointerDownEvent evt)
        {
            _dragStartPosition = evt.position;
            _isDragging = false;
            
            // Capture pointer for drag operation
            _rootElement.CapturePointer(evt.pointerId);
        }
        
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_rootElement.HasPointerCapture(evt.pointerId)) return;
            
            // Check if drag threshold exceeded
            float dragDistance = Vector2.Distance(_dragStartPosition, evt.position);
            if (!_isDragging && dragDistance > 5f)
            {
                _isDragging = true;
                CreateDragPreview(evt.position);
                OnItemDragStarted?.Invoke(this, evt.position);
            }
            
            if (_isDragging)
            {
                UpdateDragPreview(evt.position);
                OnItemDragMoved?.Invoke(this, evt.position);
            }
        }
        
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging)
            {
                DestroyDragPreview();
                OnItemDragEnded?.Invoke(this, evt.position);
            }
            
            _isDragging = false;
            _rootElement.ReleasePointer(evt.pointerId);
        }
        
        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_isDragging)
            {
                DestroyDragPreview();
                _isDragging = false;
            }
        }
        
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            if (!_isDragging)
            {
                _rootElement.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (!_isDragging)
            {
                _rootElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }
        
        private void CreateDragPreview(Vector2 screenPosition)
        {
            // Create a clone of this item for drag preview
            _dragPreview = new VisualElement();
            _dragPreview.name = "drag-preview";
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.style.width = _rootElement.resolvedStyle.width;
            _dragPreview.style.height = _rootElement.resolvedStyle.height;
            
            // Copy visual appearance
            _dragPreview.style.backgroundColor = _rootElement.resolvedStyle.backgroundColor;
            _dragPreview.style.borderTopLeftRadius = _rootElement.resolvedStyle.borderTopLeftRadius;
            _dragPreview.style.borderTopRightRadius = _rootElement.resolvedStyle.borderTopRightRadius;
            _dragPreview.style.borderBottomLeftRadius = _rootElement.resolvedStyle.borderBottomLeftRadius;
            _dragPreview.style.borderBottomRightRadius = _rootElement.resolvedStyle.borderBottomRightRadius;
            
            // Add icon clone
            if (_iconElement != null)
            {
                var iconClone = new VisualElement();
                iconClone.style.width = 64;
                iconClone.style.height = 64;
                iconClone.style.marginTop = 10;
                iconClone.style.alignSelf = Align.Center;
                
                if (_objectData.icon != null)
                {
                    iconClone.style.backgroundImage = new StyleBackground(_objectData.icon);
                    iconClone.style.unityBackgroundImageTintColor = _objectData.iconColor;
                }
                else
                {
                    iconClone.style.backgroundColor = _objectData.iconColor;
                }
                
                _dragPreview.Add(iconClone);
            }
            
            // Add name clone
            if (_nameLabel != null)
            {
                var nameClone = new Label(_nameLabel.text);
                nameClone.style.unityTextAlign = TextAnchor.MiddleCenter;
                nameClone.style.fontSize = 12;
                nameClone.style.color = Color.white;
                _dragPreview.Add(nameClone);
            }
            
            // Set opacity
            _dragPreview.style.opacity = _dragOpacity;
            
            // Add to root (screen space)
            var panelRoot = _rootElement.panel.visualTree;
            panelRoot.Add(_dragPreview);
            
            UpdateDragPreview(screenPosition);
        }
        
        private void UpdateDragPreview(Vector2 screenPosition)
        {
            if (_dragPreview == null) return;
            
            // Convert screen position to panel coordinates
            _dragPreview.style.left = screenPosition.x - (_dragPreview.resolvedStyle.width / 2);
            _dragPreview.style.top = screenPosition.y - (_dragPreview.resolvedStyle.height / 2);
        }
        
        private void DestroyDragPreview()
        {
            if (_dragPreview != null)
            {
                _dragPreview.RemoveFromHierarchy();
                _dragPreview = null;
            }
        }
        
        /// <summary>
        /// Set the selection state of this item
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_rootElement == null) return;
            
            if (selected)
            {
                _rootElement.style.borderTopColor = new Color(0.3f, 0.6f, 1f, 1f);
                _rootElement.style.borderBottomColor = new Color(0.3f, 0.6f, 1f, 1f);
                _rootElement.style.borderLeftColor = new Color(0.3f, 0.6f, 1f, 1f);
                _rootElement.style.borderRightColor = new Color(0.3f, 0.6f, 1f, 1f);
                _rootElement.style.borderTopWidth = 2;
                _rootElement.style.borderBottomWidth = 2;
                _rootElement.style.borderLeftWidth = 2;
                _rootElement.style.borderRightWidth = 2;
            }
            else
            {
                _rootElement.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                _rootElement.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                _rootElement.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                _rootElement.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                _rootElement.style.borderTopWidth = 1;
                _rootElement.style.borderBottomWidth = 1;
                _rootElement.style.borderLeftWidth = 1;
                _rootElement.style.borderRightWidth = 1;
            }
        }
        
        /// <summary>
        /// Set the enabled state of this item
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_rootElement == null) return;
            
            _rootElement.SetEnabled(enabled);
            _rootElement.style.opacity = enabled ? 1f : 0.5f;
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            if (_rootElement != null)
            {
                _rootElement.UnregisterCallback<ClickEvent>(OnClick);
                _rootElement.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _rootElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _rootElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                _rootElement.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                _rootElement.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
                _rootElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
                
                _rootElement.RemoveFromHierarchy();
                _rootElement = null;
            }
            
            DestroyDragPreview();
        }
    }
}
