# UI Migration Guide: Legacy UI to UI Toolkit

## Side-by-Side Comparison

This document provides a direct comparison between the legacy Unity UI (uGUI) implementation and the new UI Toolkit implementation of the Sandbox Builder.

## File Comparison

| Legacy UI (uGUI) | UI Toolkit | Purpose |
|------------------|------------|---------|
| `SandboxBuilderUI.cs` | `SandboxBuilderUIToolkit.cs` | Main UI controller |
| `ObjectPalette.cs` | `ObjectPaletteUIToolkit.cs` | Object palette panel |
| `ObjectPaletteItem.cs` | `ObjectPaletteItemUIToolkit.cs` | Individual palette items |
| N/A (inline styles) | `SandboxBuilderUIToolkit.uss` | Visual styling |

## Code Pattern Comparison

### 1. Component Declaration

**Legacy UI:**
```csharp
public class SandboxBuilderUI : MonoBehaviour
{
    [SerializeField] private Button _newSceneButton;
    [SerializeField] private Toggle _snapToGridToggle;
    [SerializeField] private Slider _gridSizeSlider;
    [SerializeField] private TMP_InputField _projectNameInput;
    [SerializeField] private TextMeshProUGUI _sceneNameText;
}
```

**UI Toolkit:**
```csharp
public class SandboxBuilderUIToolkit : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    
    private Button _newSceneButton;
    private Toggle _snapToGridToggle;
    private Slider _gridSizeSlider;
    private TextField _projectNameInput;
    private Label _sceneNameText;
}
```

### 2. Button Creation

**Legacy UI:**
```csharp
private Button CreateButton(Transform parent, string text)
{
    GameObject buttonGO = new GameObject($"Button_{text}");
    buttonGO.transform.SetParent(parent);
    
    var rectTransform = buttonGO.AddComponent<RectTransform>();
    rectTransform.sizeDelta = new Vector2(120, 40);
    
    var button = buttonGO.AddComponent<Button>();
    var image = buttonGO.AddComponent<Image>();
    image.color = primaryColor;
    
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(buttonGO.transform);
    var textComponent = textGO.AddComponent<TextMeshProUGUI>();
    textComponent.text = text;
    textComponent.alignment = TextAlignmentOptions.Center;
    
    return button;
}
```

**UI Toolkit:**
```csharp
private Button CreateButton(string text, string styleClass = null)
{
    var button = new Button { text = text };
    button.AddToClassList("button");
    if (!string.IsNullOrEmpty(styleClass))
    {
        button.AddToClassList(styleClass);
    }
    return button;
}
```

### 3. Event Binding

**Legacy UI:**
```csharp
if (_newSceneButton != null)
    _newSceneButton.onClick.AddListener(OnNewSceneClicked);
```

**UI Toolkit:**
```csharp
if (_newSceneButton != null)
    _newSceneButton.clicked += OnNewSceneClicked;
```

### 4. Layout Container

**Legacy UI:**
```csharp
private Transform CreateHorizontalGroup(Transform parent)
{
    GameObject groupGO = new GameObject("HorizontalGroup");
    groupGO.transform.SetParent(parent);
    
    var rectTransform = groupGO.AddComponent<RectTransform>();
    var layout = groupGO.AddComponent<HorizontalLayoutGroup>();
    layout.spacing = 8;
    layout.childAlignment = TextAnchor.MiddleLeft;
    layout.childControlWidth = false;
    layout.childControlHeight = false;
    
    return groupGO.transform;
}
```

**UI Toolkit:**
```csharp
private VisualElement CreateHorizontalGroup(string name)
{
    var group = new VisualElement { name = name };
    group.style.flexDirection = FlexDirection.Row;
    group.style.marginTop = 8;
    return group;
}
```

### 5. Updating Text

**Legacy UI:**
```csharp
if (_sceneNameText != null)
{
    _sceneNameText.text = $"Scene: {sceneName}";
}
```

**UI Toolkit:**
```csharp
if (_sceneNameText != null)
{
    _sceneNameText.text = $"Scene: {sceneName}";
}
// Same!
```

### 6. Showing/Hiding Elements

**Legacy UI:**
```csharp
if (_objectPalette != null)
{
    _objectPalette.gameObject.SetActive(_isPaletteVisible);
}
```

**UI Toolkit:**
```csharp
if (_leftPanel != null)
{
    _leftPanel.style.display = _isPaletteVisible ? DisplayStyle.Flex : DisplayStyle.None;
}
```

### 7. Palette Item - MonoBehaviour vs Pure Class

**Legacy UI (MonoBehaviour):**
```csharp
public class ObjectPaletteItem : MonoBehaviour, 
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _nameText;
    
    private void Awake()
    {
        // Setup
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        CreateDragPreview(eventData);
    }
}
```

**UI Toolkit (Pure C# Class):**
```csharp
public class ObjectPaletteItemUIToolkit
{
    private VisualElement _rootElement;
    private VisualElement _iconElement;
    private Label _nameLabel;
    
    public void Initialize(SceneObjectData data, ...)
    {
        CreateUI();
        SetupInteractions();
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        // Handle drag start
    }
}
```

### 8. Drag and Drop Implementation

**Legacy UI:**
```csharp
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
    OnItemDragEnded?.Invoke(this, eventData.position);
}
```

**UI Toolkit:**
```csharp
private void SetupInteractions()
{
    _rootElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
    _rootElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
    _rootElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
}

private void OnPointerDown(PointerDownEvent evt)
{
    _dragStartPosition = evt.position;
    _rootElement.CapturePointer(evt.pointerId);
}

private void OnPointerMove(PointerMoveEvent evt)
{
    if (!_rootElement.HasPointerCapture(evt.pointerId)) return;
    
    float dragDistance = Vector2.Distance(_dragStartPosition, evt.position);
    if (!_isDragging && dragDistance > 5f)
    {
        _isDragging = true;
        CreateDragPreview(evt.position);
        OnItemDragStarted?.Invoke(this, evt.position);
    }
}
```

### 9. Styling

**Legacy UI (Inline):**
```csharp
var image = buttonGO.AddComponent<Image>();
image.color = new Color(0.2f, 0.4f, 1f);

var rectTransform = buttonGO.GetComponent<RectTransform>();
rectTransform.sizeDelta = new Vector2(120, 40);
```

**UI Toolkit (USS):**
```csharp
// In C#
button.AddToClassList("button");
button.AddToClassList("button--primary");

// In USS file
.button {
    padding: 8px 12px;
    background-color: var(--color-primary);
}

.button--primary {
    background-color: rgba(50, 100, 255, 1);
}
```

### 10. Query Elements

**Legacy UI:**
```csharp
private void QueryComponents()
{
    // Manually assigned in Inspector or found
    _newSceneButton = transform.Find("SceneControls/NewButton")?.GetComponent<Button>();
    _sceneNameText = transform.Find("InfoPanel/SceneName")?.GetComponent<TextMeshProUGUI>();
}
```

**UI Toolkit:**
```csharp
private void QueryUIElements()
{
    _newSceneButton = _rootElement.Q<Button>("new-scene-button");
    _sceneNameText = _rootElement.Q<Label>("scene-name-text");
}
```

## Performance Comparison

| Aspect | Legacy UI (uGUI) | UI Toolkit |
|--------|------------------|------------|
| **GameObject Count** | 1 per UI element | 1 UIDocument only |
| **Memory** | Higher (GameObject overhead) | Lower (VisualElement) |
| **Rendering** | Canvas batching | GPU-accelerated |
| **Layout Calculation** | CPU (LayoutGroup) | Flexbox (optimized) |
| **Dynamic Creation** | Instantiate() overhead | Lightweight element creation |

## Best Practices

### When to Use Each

**Legacy UI (uGUI):**
- ✅ Existing projects with heavy uGUI investment
- ✅ Need prefab workflow
- ✅ 3D world space UI
- ✅ Complex physics-based UI

**UI Toolkit:**
- ✅ New projects
- ✅ Editor extensions and tools
- ✅ Screen space UI
- ✅ Performance-critical UI
- ✅ Responsive/dynamic layouts
- ✅ Data-driven UI

## Migration Checklist

- [ ] Replace `GameObject` hierarchies with `VisualElement` hierarchies
- [ ] Convert `MonoBehaviour` UI components to pure C# classes where possible
- [ ] Change event bindings from `.onClick.AddListener` to `.clicked +=`
- [ ] Replace `RectTransform` sizing with USS flexbox
- [ ] Convert inline styles to USS files
- [ ] Update drag-and-drop from `IBeginDragHandler` to pointer events
- [ ] Replace `SetActive()` with `style.display`
- [ ] Update text components from `TextMeshProUGUI` to `Label`
- [ ] Convert input fields from `TMP_InputField` to `TextField`
- [ ] Replace dropdowns from `TMP_Dropdown` to `DropdownField`

## Common Pitfalls

### 1. Forgetting to Query Elements
```csharp
// ❌ Wrong - element is null
_button.clicked += OnClick;

// ✅ Correct - query first
QueryUIElements();
if (_button != null)
    _button.clicked += OnClick;
```

### 2. Not Using USS for Styling
```csharp
// ❌ Wrong - inline styling
element.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
element.style.width = 100;
element.style.height = 40;

// ✅ Correct - use classes
element.AddToClassList("my-button");
```

### 3. Creating Too Many Visual Elements
```csharp
// ❌ Wrong - recreating every frame
void Update()
{
    _container.Clear();
    foreach (var item in items)
    {
        var element = new VisualElement();
        _container.Add(element);
    }
}

// ✅ Correct - create once, update content
void Initialize()
{
    foreach (var item in items)
    {
        var element = new VisualElement();
        _container.Add(element);
        _cachedElements.Add(element);
    }
}
```

## Resources

- **Unity UI Toolkit Docs:** https://docs.unity3d.com/Manual/UIElements.html
- **UI Toolkit vs uGUI:** https://docs.unity3d.com/Manual/UIElements-Runtime-comparison.html
- **Flexbox Guide:** https://css-tricks.com/snippets/css/a-guide-to-flexbox/
- **USS Reference:** https://docs.unity3d.com/Manual/UIE-USS.html

## Conclusion

UI Toolkit provides:
- **Better performance** through lighter weight elements
- **Easier maintenance** with separation of concerns (C#, USS, UXML)
- **Modern layout system** with flexbox
- **Future-proof** architecture aligned with Unity's direction

The migration requires learning new patterns, but the benefits are worth the investment for new projects and features.
