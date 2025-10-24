using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using SceneSandbox.Data;

namespace SceneSandbox.UI
{
    /// <summary>
    /// UI component representing an object in the object palette using UI Toolkit.
    /// Handles visual representation, user interactions, and drag-and-drop operations.
    /// </summary>
    public class ObjectPaletteItemUIToolkit
    {
        #region Constants
        
        private const float DRAG_THRESHOLD = 5f;
        private const float DRAG_PREVIEW_OPACITY = 0.7f;
        private const float ICON_SIZE = 64f;
        private const float NAME_LABEL_HEIGHT = 30f;
        private const float ITEM_MARGIN = 8f;
        
        #endregion
        
        #region Visual Elements
        
        private VisualElement _rootElement;
        private VisualElement _iconContainer;
        private VisualElement _iconElement;
        private Label _nameLabel;
        private VisualElement _dragPreview;
        
        #endregion
        
        #region Data
        
        private SceneObjectData _objectData;
        private ObjectPaletteUIToolkit _parentPalette;
        private InputAction _pointAction; // Mouse/pointer position from InputActions
        
        #endregion
        
        #region Drag State
        
        private bool _isDragging;
        private Vector2 _dragStartPosition;
        private int _capturedPointerId;
        
        #endregion
        
        #region Events
        
        public System.Action<ObjectPaletteItemUIToolkit> OnItemSelected;
        public System.Action<ObjectPaletteItemUIToolkit, Vector2> OnItemDragStarted;
        public System.Action<ObjectPaletteItemUIToolkit, Vector2> OnItemDragMoved;
        public System.Action<ObjectPaletteItemUIToolkit, Vector2> OnItemDragEnded;
        
        #endregion
        
        #region Properties
        
        public SceneObjectData ObjectData => _objectData;
        public VisualElement RootElement => _rootElement;
        public bool IsDragging => _isDragging;
        
        #endregion
        
        #region Initialization
        
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
            _rootElement = CreateRootElement(itemSize);
            
            // Icon container with icon
            _iconContainer = CreateIconContainer();
            _iconElement = CreateIconElement();
            _iconContainer.Add(_iconElement);
            _rootElement.Add(_iconContainer);
            
            // Name label
            _nameLabel = CreateNameLabel();
            _rootElement.Add(_nameLabel);
        }
        
        private VisualElement CreateRootElement(Vector2 itemSize)
        {
            var root = new VisualElement
            {
                name = "palette-item"
            };
            
            root.AddToClassList("palette-item");
            
            // Size
            root.style.width = itemSize.x;
            root.style.height = itemSize.y;
            root.style.marginRight = ITEM_MARGIN;
            root.style.marginBottom = ITEM_MARGIN;
            
            // Background and border
            root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            SetBorderRadius(root, 4);
            SetBorderWidth(root, 1);
            SetBorderColor(root, new Color(0.3f, 0.3f, 0.3f, 1f));
            
            return root;
        }
        
        private VisualElement CreateIconContainer()
        {
            var container = new VisualElement
            {
                name = "icon-container"
            };
            
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            
            return container;
        }
        
        private VisualElement CreateIconElement()
        {
            var icon = new VisualElement
            {
                name = "icon"
            };
            
            icon.AddToClassList("palette-item__icon");
            icon.style.width = ICON_SIZE;
            icon.style.height = ICON_SIZE;
            icon.style.backgroundColor = new Color(0.4f, 0.4f, 0.8f, 1f);
            SetBorderRadius(icon, 4);
            
            return icon;
        }
        
        private Label CreateNameLabel()
        {
            var label = new Label
            {
                name = "name-label"
            };
            
            label.AddToClassList("palette-item__name");
            label.style.height = NAME_LABEL_HEIGHT;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 12;
            label.style.color = Color.white;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            
            return label;
        }
        
        #endregion
        
        #region UI Helper Methods
        
        private void SetBorderRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
        
        private void SetBorderWidth(VisualElement element, float width)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
        }
        
        private void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
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
            if (_rootElement == null)
            {
                Debug.LogError($"[ITEM] Cannot setup interactions - rootElement is null for '{_objectData?.displayName}'");
                return;
            }
            
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
        
        #endregion
        
        #region Event Handlers
        
        private void OnClick(ClickEvent evt)
        {
            if (!_isDragging)
            {
                OnItemSelected?.Invoke(this);
            }
        }
        
        private void OnPointerDown(PointerDownEvent evt)
        {
            // Use localPosition instead of position for consistent coordinates
            _dragStartPosition = evt.localPosition;
            
            _isDragging = false;
            _capturedPointerId = evt.pointerId;
            
            _rootElement.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
        
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_rootElement.HasPointerCapture(evt.pointerId))
            {
                return;
            }
            
            // Use localPosition for consistent coordinates
            Vector2 currentPosition = evt.localPosition;
            
            // Check if drag threshold exceeded
            if (!_isDragging && ShouldStartDrag(currentPosition))
            {
                StartDrag(evt);
            }
            
            if (_isDragging)
            {
                ContinueDrag(evt);
            }
        }
        
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging)
            {
                EndDrag(evt);
            }
            
            _isDragging = false;
            _rootElement.ReleasePointer(evt.pointerId);
        }
        
        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_isDragging)
            {
                CancelDrag();
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
        
        #endregion
        
        #region Drag Operations
        
        private bool ShouldStartDrag(Vector2 currentPosition)
        {
            float dragDistance = Vector2.Distance(_dragStartPosition, currentPosition);
            return dragDistance > DRAG_THRESHOLD;
        }
        
        private void StartDrag(PointerMoveEvent evt)
        {
            _isDragging = true;
            
            // Get actual screen position using New Input System
            Vector2 screenPosition = GetMouseScreenPosition();
            
            CreateDragPreview(evt.localPosition);
            OnItemDragStarted?.Invoke(this, screenPosition);
        }
        
        private void ContinueDrag(PointerMoveEvent evt)
        {
            // Get actual screen position using New Input System
            Vector2 screenPosition = GetMouseScreenPosition();
            
            UpdateDragPreview(evt.localPosition);
            OnItemDragMoved?.Invoke(this, screenPosition);
            evt.StopPropagation();
        }
        
        private void EndDrag(PointerUpEvent evt)
        {
            // Get actual screen position using New Input System
            Vector2 screenPosition = GetMouseScreenPosition();
            
            DestroyDragPreview();
            OnItemDragEnded?.Invoke(this, screenPosition);
            evt.StopPropagation();
        }
        
        private void CancelDrag()
        {
            DestroyDragPreview();
            _isDragging = false;
        }
        
        /// <summary>
        /// Get mouse screen position using New Input System
        /// </summary>
        private Vector2 GetMouseScreenPosition()
        {
            // Try InputAction first (preferred)
            if (_pointAction != null)
            {
                return _pointAction.ReadValue<Vector2>();
            }
            
            // Fallback to Mouse.current
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            
            // Final fallback to legacy input
            Debug.LogWarning("[ITEM] No Input System available, using legacy Input.mousePosition");
            return UnityEngine.Input.mousePosition;
        }
        
        #endregion
        
        #region Drag Preview
        
        private void CreateDragPreview(Vector2 localPosition)
        {
            _dragPreview = CreateDragPreviewElement();
            
            AddIconToDragPreview();
            AddNameToDragPreview();
            
            AttachDragPreviewToPanel();
            UpdateDragPreview(localPosition);
        }
        
        private VisualElement CreateDragPreviewElement()
        {
            var preview = new VisualElement
            {
                name = "drag-preview",
                pickingMode = PickingMode.Ignore
            };
            
            preview.AddToClassList("palette-item-drag-preview");
            
            // Position and size
            preview.style.position = Position.Absolute;
            preview.style.width = _rootElement.resolvedStyle.width;
            preview.style.height = _rootElement.resolvedStyle.height;
            preview.style.opacity = DRAG_PREVIEW_OPACITY;
            
            // Visual styling
            preview.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            SetBorderRadius(preview, _rootElement.resolvedStyle.borderTopLeftRadius);
            SetBorderWidth(preview, 2);
            SetBorderColor(preview, new Color(0.3f, 0.6f, 1f, 1f)); // Blue highlight
            
            return preview;
        }
        
        private void AddIconToDragPreview()
        {
            if (_iconElement == null) return;
            
            var iconClone = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            
            iconClone.style.width = ICON_SIZE;
            iconClone.style.height = ICON_SIZE;
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
        
        private void AddNameToDragPreview()
        {
            if (_nameLabel == null) return;
            
            var nameClone = new Label(_nameLabel.text)
            {
                pickingMode = PickingMode.Ignore
            };
            
            nameClone.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameClone.style.fontSize = 12;
            nameClone.style.color = Color.white;
            
            _dragPreview.Add(nameClone);
        }
        
        private void AttachDragPreviewToPanel()
        {
            var panelRoot = _rootElement.panel.visualTree;
            panelRoot.Add(_dragPreview);
        }
        
        private void UpdateDragPreview(Vector2 localPosition)
        {
            if (_dragPreview == null) return;
            
            // Convert local position to panel position for absolute positioning
            Vector2 panelPosition = _rootElement.LocalToWorld(localPosition);
            
            float halfWidth = _dragPreview.resolvedStyle.width / 2;
            float halfHeight = _dragPreview.resolvedStyle.height / 2;
            
            _dragPreview.style.left = panelPosition.x - halfWidth;
            _dragPreview.style.top = panelPosition.y - halfHeight;
        }
        
        private void DestroyDragPreview()
        {
            if (_dragPreview != null)
            {
                _dragPreview.RemoveFromHierarchy();
                _dragPreview = null;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set the Point InputAction for reading mouse position
        /// </summary>
        public void SetPointAction(InputAction pointAction)
        {
            _pointAction = pointAction;
        }
        
        /// <summary>
        /// Set the selection state of this item
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_rootElement == null) return;
            
            var borderColor = selected 
                ? new Color(0.3f, 0.6f, 1f, 1f)  // Blue when selected
                : new Color(0.3f, 0.3f, 0.3f, 1f); // Gray when not selected
            
            var borderWidth = selected ? 2 : 1;
            
            SetBorderColor(_rootElement, borderColor);
            SetBorderWidth(_rootElement, borderWidth);
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
        /// Clean up resources and unregister callbacks
        /// </summary>
        public void Dispose()
        {
            UnregisterCallbacks();
            RemoveFromHierarchy();
            DestroyDragPreview();
        }
        
        #endregion
        
        #region Cleanup
        
        private void UnregisterCallbacks()
        {
            if (_rootElement == null) return;
            
            _rootElement.UnregisterCallback<ClickEvent>(OnClick);
            _rootElement.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _rootElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _rootElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _rootElement.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            _rootElement.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            _rootElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
        
        private void RemoveFromHierarchy()
        {
            if (_rootElement == null) return;
            
            _rootElement.RemoveFromHierarchy();
            _rootElement = null;
        }
        
        #endregion
    }
}
