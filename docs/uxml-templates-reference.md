# UXML Templates Quick Reference

## Overview

The Sandbox Builder UI Toolkit implementation includes three UXML template files that define the visual structure of the UI. These can be edited in Unity's UI Builder for visual design.

## Files

### 1. SandboxBuilderUIToolkit.uxml
Main UI structure with three-panel layout.

**Location:** `Assets/Scripts/SceneSandbox/UI/SandboxBuilderUIToolkit.uxml`

**Structure:**
```
main-container (flex-row)
├── left-panel (300px width)
│   ├── palette-title (Label)
│   └── palette-container (grows)
├── center-panel (flex-grow)
│   ├── scene-controls-panel
│   │   ├── panel__header (Label)
│   │   ├── project-controls-row
│   │   │   ├── project-name-input (TextField)
│   │   │   ├── new-project-button
│   │   │   ├── save-project-button
│   │   │   └── load-project-button
│   │   ├── scene-controls-row
│   │   │   ├── new-scene-button
│   │   │   ├── save-scene-button
│   │   │   ├── load-scene-button
│   │   │   └── clear-scene-button
│   │   ├── preview-controls-row
│   │   │   ├── preview-button
│   │   │   └── stop-preview-button
│   │   └── current-project-text (Label)
│   ├── scene-info-panel
│   │   └── scene-info-row
│   │       ├── scene-name-text
│   │       ├── object-count-text
│   │       └── preview-status-text
│   └── viewport-area (transparent, picking-mode: Ignore)
└── right-panel (300px width)
    └── object-controls-panel
        ├── panel__header (Label)
        ├── delete-object-button
        ├── Grid Settings section
        │   ├── snap-to-grid-toggle
        │   └── grid-size-slider
mobile-controls-panel (absolute, bottom)
    └── mobile-button-row
        ├── mobile-palette-toggle
        └── mobile-properties-toggle
```

**Key Element Names (for querying in C#):**
- `main-container`
- `left-panel`, `center-panel`, `right-panel`
- `new-scene-button`, `save-scene-button`, `load-scene-button`, `clear-scene-button`
- `preview-button`, `stop-preview-button`
- `new-project-button`, `save-project-button`, `load-project-button`
- `delete-object-button`
- `snap-to-grid-toggle`, `grid-size-slider`
- `scene-name-text`, `object-count-text`, `preview-status-text`
- `project-name-input`, `current-project-text`

### 2. ObjectPaletteUIToolkit.uxml
Object palette panel with filters and item grid.

**Location:** `Assets/Scripts/SceneSandbox/UI/ObjectPaletteUIToolkit.uxml`

**Structure:**
```
palette-container
├── panel__header (Label: "Object Palette")
├── filters-container
│   ├── type-filter (DropdownField)
│   ├── category-filter (DropdownField)
│   ├── search-field (TextField)
│   └── refresh-button (Button)
└── items-scroll-view (ScrollView)
    └── items-container (flex-row, flex-wrap)
```

**Key Element Names:**
- `palette-container`
- `type-filter`, `category-filter`, `search-field`
- `refresh-button`
- `items-scroll-view`, `items-container`

### 3. ObjectPaletteItemUIToolkit.uxml
Individual palette item template.

**Location:** `Assets/Scripts/SceneSandbox/UI/ObjectPaletteItemUIToolkit.uxml`

**Structure:**
```
palette-item (100x120px)
├── icon-container (flex-grow)
│   └── icon (64x64px)
└── name-label (30px height)
```

**Key Element Names:**
- `palette-item`
- `icon-container`, `icon`
- `name-label`

## Editing UXML in UI Builder

### Opening UI Builder

1. In Unity, go to **Window > UI Toolkit > UI Builder**
2. Click **Open** in the UI Builder window
3. Navigate to the UXML file you want to edit
4. The UI Builder will show:
   - **Hierarchy** (left): Element tree
   - **Viewport** (center): Visual preview
   - **Inspector** (right): Element properties
   - **StyleSheets** (top right): Attached USS files

### Common Editing Tasks

#### 1. Add a New Button
1. Drag `Button` from Library (left) to desired location in Hierarchy
2. Set `name` attribute in Inspector (for C# querying)
3. Set `text` attribute
4. Add USS classes: `button`, `button--primary` (or other variants)

#### 2. Change Layout
1. Select element in Hierarchy
2. In Inspector > Style > Flex:
   - `flex-direction`: row | column
   - `flex-grow`: 1 (to fill space)
   - `justify-content`: flex-start | center | space-between | space-around
   - `align-items`: stretch | center | flex-start | flex-end

#### 3. Add USS Classes
1. Select element
2. In Inspector > StyleSheet > USS Classes
3. Click `+` and type class name (e.g., `button`, `panel`)

#### 4. Adjust Sizing
1. Select element
2. In Inspector > Style > Size:
   - `width`: auto | px | % | flex-grow
   - `height`: auto | px | % | flex-grow
   - `min-width`, `max-width`, `min-height`, `max-height`

#### 5. Add Margins/Padding
1. Select element
2. In Inspector > Style > Margin / Padding:
   - Set top, right, bottom, left individually
   - Or use shorthand in Style Inspector

## C# Integration

### Querying Elements

After UXML is loaded, query elements by name:

```csharp
// In SandboxBuilderUIToolkit.cs
private void QueryUIElements()
{
    _newSceneButton = _rootElement.Q<Button>("new-scene-button");
    _sceneNameText = _rootElement.Q<Label>("scene-name-text");
    _leftPanel = _rootElement.Q<VisualElement>("left-panel");
}
```

### Loading UXML Template

```csharp
// Assign in Inspector
[SerializeField] private VisualTreeAsset _mainUITemplate;

// Load in code
private void InitializeUI()
{
    if (_mainUITemplate != null)
    {
        _mainUITemplate.CloneTree(_uiDocument.rootVisualElement);
    }
    else
    {
        CreateUIFromCode(); // Fallback
    }
}
```

### Cloning Item Templates

```csharp
// For repeating items (like palette items)
[SerializeField] private VisualTreeAsset _itemTemplate;

private void CreatePaletteItem(SceneObjectData data)
{
    VisualElement item = _itemTemplate.CloneTree();
    
    // Query elements within the clone
    var icon = item.Q<VisualElement>("icon");
    var nameLabel = item.Q<Label>("name-label");
    
    // Update content
    nameLabel.text = data.displayName;
    
    // Add to container
    _itemsContainer.Add(item);
}
```

## USS Class Reference

Classes used in UXML templates (defined in SandboxBuilderUIToolkit.uss):

### Layout Classes
- `main-container` - Main flex-row container
- `left-panel`, `right-panel` - Side panels
- `center-panel` - Main content area
- `panel` - Generic panel styling

### Component Classes
- `button` - Base button style (from GameTheme.tss)
- `button--primary`, `button--secondary`, `button--danger`, `button--success`, `button--warning` - Button variants
- `controls-panel` - Control panel styling
- `palette-panel` - Palette container
- `palette-item` - Individual item
- `palette-item__icon` - Item icon
- `palette-item__name` - Item name label

### Special Classes
- `panel__header` - Panel title/header
- `mobile-controls` - Mobile control bar
- `filters-container` - Filter section
- `items-container` - Grid container for items

## Best Practices

### 1. Always Set Element Names
```xml
<!-- ✅ Good - has name for C# querying -->
<ui:Button text="Save" name="save-button" />

<!-- ❌ Bad - no name, can't query from C# -->
<ui:Button text="Save" />
```

### 2. Use USS Classes for Styling
```xml
<!-- ✅ Good - uses classes -->
<ui:Button text="Save" name="save-button" class="button button--primary" />

<!-- ❌ Bad - inline styles -->
<ui:Button text="Save" name="save-button" 
    style="background-color: blue; padding: 8px;" />
```

### 3. Keep Structure Clean
```xml
<!-- ✅ Good - semantic structure -->
<ui:VisualElement name="controls-row" class="controls-row">
    <ui:Button name="save-button" class="button" />
    <ui:Button name="load-button" class="button" />
</ui:VisualElement>

<!-- ❌ Bad - flat structure -->
<ui:Button name="save-button" />
<ui:Button name="load-button" />
```

### 4. Use Descriptive Names
```xml
<!-- ✅ Good - clear, descriptive -->
<ui:Button text="New Scene" name="new-scene-button" />

<!-- ❌ Bad - vague -->
<ui:Button text="New Scene" name="btn1" />
```

## Troubleshooting

### UXML Not Loading
1. Check file is in correct location
2. Verify file has `.uxml` extension
3. Check in Inspector that `_mainUITemplate` is assigned
4. Look for errors in Console

### Elements Not Found in C#
1. Verify element has `name` attribute in UXML
2. Check spelling matches exactly (case-sensitive)
3. Use correct type in `Q<T>()` (e.g., `Q<Button>`, not `Q<VisualElement>`)
4. Ensure UXML is loaded before querying

### Styles Not Applied
1. Check USS file is attached to root in UXML or C#
2. Verify class names match between UXML and USS
3. Look for typos in class names
4. Check USS syntax is valid

### UI Builder Shows Errors
1. Check XML syntax (closing tags, quotes)
2. Verify namespace declarations at top
3. Ensure all referenced classes exist in USS
4. Check for invalid property values

## Examples

### Adding a New Control Panel

1. Open `SandboxBuilderUIToolkit.uxml` in UI Builder
2. In Hierarchy, right-click on `center-panel`
3. Select **Add > VisualElement**
4. Name it: `animation-controls-panel`
5. Add class: `panel`, `controls-panel`
6. Add children (buttons, labels, etc.)
7. Save UXML
8. In C#, query the element:

```csharp
private VisualElement _animationControlsPanel;

private void QueryUIElements()
{
    // ... existing queries ...
    _animationControlsPanel = _rootElement.Q<VisualElement>("animation-controls-panel");
}
```

### Creating a Custom Palette Item Template

1. Open UI Builder
2. Create new UXML document: `CustomPaletteItem.uxml`
3. Design the item structure
4. Save and assign to `ObjectPaletteUIToolkit._itemTemplate`
5. The palette will use your custom template

## Resources

- **UI Builder Manual:** https://docs.unity3d.com/Manual/UIBuilder.html
- **UXML Reference:** https://docs.unity3d.com/Manual/UIE-UXML.html
- **USS Reference:** https://docs.unity3d.com/Manual/UIE-USS.html
- **UI Toolkit Samples:** Window > Package Manager > UI Toolkit > Samples
