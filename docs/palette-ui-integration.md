# ObjectPalette UI Integration with SandboxBuilder

## Summary
Integrated the ObjectPaletteUIToolkit's visual elements directly into the SandboxBuilderUIToolkit's main UI by attaching it to the `palette-container` element defined in the UXML template.

## Problem Solved
Previously, the ObjectPalette managed its own separate UIDocument, leading to:
- ❌ Duplicate UI rendering
- ❌ Conflicting UIDocument hierarchies
- ❌ Difficulty controlling visibility
- ❌ Inconsistent styling and layout

Now, the palette UI is seamlessly integrated into the main SandboxBuilder UI.

## Implementation Details

### 1. AttachPaletteToContainer Method
This new method handles the integration of palette UI into the main UI:

```csharp
private void AttachPaletteToContainer()
{
    if (_objectPalette == null || _rootElement == null) return;
    
    // Find the palette container in the main UI
    var paletteContainer = _rootElement.Q<VisualElement>("palette-container");
    
    if (paletteContainer == null)
    {
        Debug.LogWarning("SandboxBuilderUIToolkit: palette-container not found in UXML.");
        return;
    }
    
    // Get the palette's UIDocument
    var paletteUIDocument = _objectPalette.GetComponent<UIDocument>();
    
    if (paletteUIDocument == null)
    {
        Debug.LogWarning("SandboxBuilderUIToolkit: ObjectPalette has no UIDocument.");
        return;
    }
    
    // Start coroutine to attach after palette initialization
    StartCoroutine(AttachPaletteUICoroutine(paletteContainer, paletteUIDocument));
}
```

**Key Features:**
- ✅ Finds the palette-container element in UXML
- ✅ Gets the palette's UIDocument component
- ✅ Uses coroutine for proper timing
- ✅ Comprehensive error checking

### 2. AttachPaletteUICoroutine
Handles the actual UI element reparenting:

```csharp
private System.Collections.IEnumerator AttachPaletteUICoroutine(
    VisualElement paletteContainer, 
    UIDocument paletteUIDocument)
{
    // Wait for palette UI to initialize
    yield return null;
    
    // Get palette's root container
    var paletteRoot = paletteUIDocument.rootVisualElement
        .Q<VisualElement>("palette-container");
    
    if (paletteRoot == null)
    {
        Debug.LogWarning("Could not find palette-container in ObjectPalette UI.");
        yield break;
    }
    
    // Clear target container
    paletteContainer.Clear();
    
    // Reparent palette UI
    paletteRoot.RemoveFromHierarchy();
    paletteContainer.Add(paletteRoot);
    
    // Style adjustments
    paletteRoot.style.flexGrow = 1;
    
    // Disable palette's UIDocument to avoid conflicts
    paletteUIDocument.enabled = false;
    
    Debug.Log("Successfully attached palette UI to palette-container.");
}
```

**Process Flow:**
1. ⏳ Wait one frame for palette initialization
2. 🔍 Find palette's root container element
3. 🧹 Clear the target container
4. 🔀 Reparent palette UI to main UI
5. 🎨 Apply styling (flexGrow)
6. ❌ Disable palette's separate UIDocument

### 3. Updated SetupPaletteIntegration
Now includes UI attachment:

```csharp
private void SetupPaletteIntegration()
{
    if (_objectPalette == null) return;
    
    // Subscribe to palette events
    _objectPalette.OnObjectSelected += OnPaletteObjectSelected;
    _objectPalette.OnObjectDraggedToScene += OnPaletteObjectDraggedToScene;
    
    // Attach palette UI to the palette-container
    AttachPaletteToContainer();
    
    Debug.Log("Palette integration setup complete.");
}
```

### 4. Simplified UpdatePanelVisibility
No longer needs to manage palette GameObject:

```csharp
private void UpdatePanelVisibility()
{
    if (_leftPanel != null)
    {
        _leftPanel.style.display = _isPaletteVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    // The palette UI is now integrated into our UIDocument
    // Visibility is controlled through the left panel
}
```

## UXML Structure

The `SandboxBuilderUIToolkit.uxml` defines the container:

```xml
<ui:VisualElement name="left-panel" class="panel left-panel">
    <ui:Label text="Object Palette" name="palette-title" class="panel__header" />
    <ui:VisualElement name="palette-container" style="flex-grow: 1;" />
</ui:VisualElement>
```

**After integration, the runtime hierarchy becomes:**

```
main-container
├─ left-panel
│  ├─ palette-title (Label: "Object Palette")
│  └─ palette-container ← Palette UI attached here
│     └─ palette-container (from ObjectPalette)
│        ├─ filters-container
│        │  ├─ type-filter
│        │  ├─ category-filter
│        │  └─ search-field
│        └─ items-scroll-view
│           └─ items-container
│              ├─ palette-item
│              ├─ palette-item
│              └─ ...
├─ center-panel
│  └─ ...
└─ right-panel
   └─ ...
```

## Benefits of This Approach

### 1. Single UI Hierarchy ✅
- Only one UIDocument renders everything
- No conflicting rendering pipelines
- Cleaner visual tree

### 2. Consistent Styling 🎨
- Shared StyleSheets apply across all elements
- Unified theme and appearance
- Easier CSS management

### 3. Better Performance ⚡
- Single render pass
- No duplicate UIDocument overhead
- Reduced memory footprint

### 4. Simplified Visibility Control 👁️
- Control through parent panel
- No need to manage GameObject active state
- Works seamlessly with mobile toggles

### 5. Layout Integration 📐
- Palette respects main UI layout
- Flexbox works across entire UI
- Responsive to screen size changes

## Timing Considerations

### Why Use Coroutine?
The palette's UI initialization happens in its `Awake` method:
```csharp
// ObjectPaletteUIToolkit.Awake()
private void Awake()
{
    InitializeUI();  // Creates UI elements
    SetupUI();       // Binds events
    SetupMobileControls();
}
```

By the time `SandboxBuilderUIToolkit.Start()` runs, we need to ensure:
1. ObjectPalette's `Awake` has completed
2. ObjectPalette's `InitializeUI` has created visual elements
3. ObjectPalette's UIDocument is ready

**Solution**: `yield return null` ensures all `Awake` calls complete before we access the palette's UI elements.

## Error Handling

The implementation includes comprehensive null checks:

```csharp
// Check palette exists
if (_objectPalette == null) return;

// Check main UI root exists
if (_rootElement == null) return;

// Check palette container exists in UXML
if (paletteContainer == null) {
    Debug.LogWarning("palette-container not found in UXML");
    return;
}

// Check palette has UIDocument
if (paletteUIDocument == null) {
    Debug.LogWarning("ObjectPalette has no UIDocument");
    return;
}

// Check palette UI initialized
if (paletteRoot == null) {
    Debug.LogWarning("palette-container not found in ObjectPalette UI");
    yield break;
}
```

## Usage Example

### Scene Setup
```
GameObject: SandboxBuilderUI
├─ SandboxBuilderUIToolkit (Component)
│  ├─ UI Document: MainUITemplate.uxml ← Contains palette-container
│  ├─ Main UI Template: [Assigned]
│  ├─ Main UI StyleSheet: [Assigned]
│  └─ Object Palette: [Auto or Manual]

GameObject: ObjectPalette (Created automatically or manually)
└─ ObjectPaletteUIToolkit (Component)
   └─ UI Document: [Created by component]
      └─ Root (disabled after integration)
```

### At Runtime
1. `SandboxBuilderUIToolkit.Awake()` initializes main UI
2. `SandboxBuilderUIToolkit.Start()` finds or creates ObjectPalette
3. `SetupPaletteIntegration()` subscribes to events
4. `AttachPaletteToContainer()` starts coroutine
5. **Next frame**: Coroutine executes
6. Palette UI elements moved to `palette-container`
7. Palette's UIDocument disabled
8. ✅ **Complete integration**

## Testing Checklist

- [ ] **Visual Verification**: Palette appears in left panel
- [ ] **No Duplicate UI**: Only one palette visible
- [ ] **Styling Applied**: Palette matches main UI theme
- [ ] **Functional**: Can select items from palette
- [ ] **Drag & Drop**: Can drag items to scene
- [ ] **Filters Work**: Type/category/search filters functional
- [ ] **Toggle Visibility**: Left panel shows/hides correctly
- [ ] **Mobile Controls**: Mobile toggle buttons work
- [ ] **No Errors**: Console clean of warnings/errors
- [ ] **Performance**: No frame drops or lag

## Troubleshooting

### Palette Not Visible
**Symptoms**: Palette container is empty
**Solutions**:
1. Check console for warnings
2. Verify UXML has `palette-container` element
3. Ensure ObjectPalette's UIDocument initialized
4. Check coroutine completed (add debug logs)

### Duplicate Palettes
**Symptoms**: Two palettes visible
**Solutions**:
1. Ensure palette UIDocument disabled after integration
2. Check no manual palette GameObject in scene
3. Verify only one SandboxBuilderUIToolkit exists

### Styling Issues
**Symptoms**: Palette looks different
**Solutions**:
1. Ensure StyleSheet applied to main UIDocument
2. Check CSS selectors match palette elements
3. Verify palette StyleSheet not conflicting

### Events Not Firing
**Symptoms**: Selection/drag not working
**Solutions**:
1. Verify event subscriptions in `SetupPaletteIntegration`
2. Check palette component is active
3. Ensure UI elements not blocked by other elements

## Future Enhancements

1. **Dynamic Switching**: Switch between different palette configurations
2. **Layout Options**: Support for different palette positions (left/right/bottom)
3. **Collapsible**: Add collapse/expand functionality
4. **Multiple Palettes**: Tabbed interface for multiple object palettes
5. **Docking**: Drag to reposition palette within UI

## Related Files
- `SandboxBuilderUIToolkit.cs` - Main UI controller with integration logic
- `SandboxBuilderUIToolkit.uxml` - UXML template with palette-container
- `ObjectPaletteUIToolkit.cs` - Palette component
- `ObjectPaletteUIToolkit.uxml` - Palette UXML template
- `SandboxBuilderUIToolkit.uss` - Styling for entire UI
