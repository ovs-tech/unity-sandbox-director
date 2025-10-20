# Sandbox Builder UI Toolkit Implementation

## Overview

This document describes the UI Toolkit implementation of the Scene Sandbox Builder UI, replacing the legacy Unity UI (uGUI) system with modern UI Toolkit.

## Files Created

### 1. **SandboxBuilderUIToolkit.cs**
Main UI controller using UI Toolkit (`UIDocument` and `VisualElement` based).

**Key Features:**
- Three-panel layout (left: palette, center: scene viewport, right: controls)
- Scene controls (New, Save, Load, Clear, Preview)
- Project management (New Project, Save, Load, Clear)
- Object controls (Delete, Grid settings)
- Mobile-specific controls with toggle buttons
- Integration with `FormSubmitPanelUIToolkit` for dialogs
- Event-driven architecture compatible with `SceneSandboxBuilder`

**Main Components:**
- `VisualElement`-based UI hierarchy
- `Button`, `Toggle`, `Slider`, `TextField`, `Label`, `DropdownField`
- Event handling via `RegisterCallback` and `clicked` events
- Dynamic panel visibility control
- Programmatic UI creation (no UXML dependency, but supports it)

### 2. **ObjectPaletteUIToolkit.cs**
Object palette panel using UI Toolkit.

**Key Features:**
- Displays available objects from `SceneObjectLibrary`
- Filtering by type, category, and search text
- Grid layout for palette items
- Drag-and-drop support for placing objects in scene
- Dynamic item creation and management
- Refresh functionality

**Main Components:**
- `DropdownField` for filters
- `TextField` for search
- `ScrollView` with grid container for items
- Item management with `ObjectPaletteItemUIToolkit`

### 3. **ObjectPaletteItemUIToolkit.cs**
Individual palette item using UI Toolkit (pure C# class, not MonoBehaviour).

**Key Features:**
- Visual representation of scene objects
- Icon display (sprite or color)
- Name label
- Click to select
- Drag-and-drop interaction
- Hover effects
- Selection highlighting

**Main Components:**
- `VisualElement` root
- Icon container with background image or color
- Label for object name
- Pointer event handling for drag operations
- Visual feedback for selection state

### 4. **SandboxBuilderUIToolkit.uxml**
UXML template for visual structure (optional, can be edited in UI Builder).

**Key Features:**
- Three-panel layout definition
- All UI elements with proper names and classes
- Hierarchical structure
- Can be edited visually in Unity's UI Builder
- Fallback to programmatic creation if not assigned

### 5. **ObjectPaletteUIToolkit.uxml**
UXML template for the object palette panel.

**Key Features:**
- Filter controls (type, category, search)
- Scroll view for items
- Refresh button

### 6. **ObjectPaletteItemUIToolkit.uxml**
UXML template for individual palette items.

**Key Features:**
- Icon container
- Name label
- Proper sizing and layout

### 7. **SandboxBuilderUIToolkit.uss**
Unity Style Sheet for visual styling.

**Key Features:**
- Panel layouts and colors
- Palette item styling
- Button styles (using GameTheme.tss classes)
- Mobile controls styling
- Hover and selection states
- Responsive layout rules

## Architecture Comparison

### Legacy Unity UI (uGUI)
```
MonoBehaviour
├── Canvas
├── GameObject hierarchy
├── RectTransform
├── Button.onClick.AddListener
└── Manual GameObject creation
```

### UI Toolkit
```
MonoBehaviour
├── UIDocument
├── VisualElement hierarchy
├── Style (USS)
├── Button.clicked +=
└── Programmatic or UXML creation
```

## Key Differences from Legacy UI

| Feature | Legacy UI (uGUI) | UI Toolkit |
|---------|------------------|------------|
| **Base Class** | MonoBehaviour with UI components | MonoBehaviour with UIDocument |
| **Elements** | GameObject + RectTransform | VisualElement |
| **Styling** | Inline properties | USS (Unity Style Sheets) |
| **Layout** | LayoutGroup components | Flexbox (CSS-like) |
| **Events** | onClick.AddListener | clicked += or RegisterCallback |
| **Creation** | Instantiate prefabs | CloneTree or code |
| **Performance** | GameObject overhead | Lighter weight elements |

## Usage

### Basic Setup

#### Option A: Using UXML Templates (Recommended)

1. **Add UIDocument Component:**
```csharp
var go = new GameObject("SandboxBuilderUI");
var uiDoc = go.AddComponent<UIDocument>();
var ui = go.AddComponent<SandboxBuilderUIToolkit>();
```

2. **Assign Visual Assets in Inspector:**
- `UI Document` → Reference to UIDocument component
- `Main UI Template` → SandboxBuilderUIToolkit.uxml
- `Main UI Style Sheet` → SandboxBuilderUIToolkit.uss
- `Sandbox Builder` → Reference to SceneSandboxBuilder
- `Panel Pos` → Transform for FormSubmitPanel positioning

3. **For Object Palette:**
- Assign `Palette Template` → ObjectPaletteUIToolkit.uxml
- Assign `Item Template` → ObjectPaletteItemUIToolkit.uxml
- Assign `Palette Style Sheet` → SandboxBuilderUIToolkit.uss

#### Option B: Programmatic Creation (Fallback)

1. **Add Component Only:**
```csharp
var go = new GameObject("SandboxBuilderUI");
var ui = go.AddComponent<SandboxBuilderUIToolkit>();
```

2. **Leave UXML/USS fields empty:**
The UI will automatically create itself programmatically on `Awake()` if no UXML is provided.

3. **Auto-Initialization:**
The system detects missing templates and falls back to code-based UI creation.

### Integration with Existing Systems

**SceneSandboxBuilder Events:**
```csharp
sandboxBuilder.OnSceneLoaded += OnSceneLoaded;
sandboxBuilder.OnObjectPlaced += OnObjectPlaced;
sandboxBuilder.OnObjectSelected += OnObjectSelected;
// ... etc
```

**FormSubmitPanelUIToolkit Integration:**
```csharp
FormSubmitPanelUIToolkit.Instance.Show(
    "Dialog Title",
    fieldDefinitions,
    onSubmit: (data) => { /* handle submit */ },
    onCancel: () => { /* handle cancel */ }
);
```

### Object Palette Usage

```csharp
// Setup
var palette = gameObject.AddComponent<ObjectPaletteUIToolkit>();
palette.ObjectLibrary = mySceneObjectLibrary;
palette.RefreshPalette();

// Filtering
palette.SetTypeFilter(SceneObjectType.Character);
palette.SetCategoryFilter("Furniture");
palette.SetSearchFilter("chair");

// Events
palette.OnObjectSelected += (objectData) => { /* handle selection */ };
palette.OnObjectDraggedToScene += (objectData, position) => { /* handle drop */ };
```

## Styling Customization

### USS Variables (from GameTheme.tss)
```css
--color-primary: #4f46e5;
--color-bg: #ffffff;
--space-md: 16px;
--border-radius-md: 8px;
```

### Custom Styles
Modify `SandboxBuilderUIToolkit.uss` or add additional USS files:

```css
.palette-item {
    background-color: rgba(50, 50, 50, 1);
    border-radius: 4px;
}

.palette-item:hover {
    background-color: rgba(70, 70, 70, 1);
}
```

## Mobile Considerations

- Mobile controls panel appears at bottom on mobile platforms
- Toggle buttons for Palette and Properties panels
- Touch-friendly button sizes
- Responsive layout adjusts to screen size

## Benefits Over Legacy UI

1. **Performance:** Lighter weight, GPU-accelerated rendering
2. **Maintainability:** Separation of structure (C#), presentation (USS), and logic
3. **Flexibility:** Easier dynamic UI creation and modification
4. **Modern:** Industry-standard flexbox layout system
5. **Scalability:** Better support for responsive design and DPI scaling
6. **Debugging:** UI Debugger in Unity for inspecting element hierarchy

## Migration Notes

### From SandboxBuilderUI to SandboxBuilderUIToolkit

**Component Replacement:**
- `Button` → `Button` (different namespace)
- `Toggle` → `Toggle` (UI Toolkit)
- `Slider` → `Slider` (UI Toolkit)
- `TMP_InputField` → `TextField`
- `TextMeshProUGUI` → `Label`
- `TMP_Dropdown` → `DropdownField`

**Event Handling:**
```csharp
// Old (uGUI)
button.onClick.AddListener(OnClick);

// New (UI Toolkit)
button.clicked += OnClick;
```

**Layout:**
```csharp
// Old (uGUI)
var layout = new GameObject();
layout.AddComponent<HorizontalLayoutGroup>();

// New (UI Toolkit)
var container = new VisualElement();
container.style.flexDirection = FlexDirection.Row;
```

## Known Limitations

1. **No prefab workflow yet:** UI Toolkit doesn't support prefabs like uGUI
   - Workaround: Use UXML templates or programmatic creation
   
2. **Different drag-and-drop model:** Requires pointer events instead of EventSystem
   - Implemented in `ObjectPaletteItemUIToolkit`

3. **Learning curve:** Developers familiar with uGUI need to learn flexbox and USS

## Future Enhancements

1. **UXML Templates:** Create `.uxml` files for visual editing in UI Builder
2. **Theme System:** Expand USS theming with light/dark modes
3. **Animation:** Add UI Toolkit transitions for smooth state changes
4. **Accessibility:** Add ARIA-like accessibility features
5. **Custom Controls:** Create reusable custom VisualElement components

## Testing Checklist

- [ ] Scene controls work (New, Save, Load, Clear)
- [ ] Project controls work (New Project, Save, Load, Clear)
- [ ] Object palette displays items correctly
- [ ] Filtering by type, category, and search works
- [ ] Drag-and-drop places objects in scene
- [ ] Object selection highlights palette item
- [ ] Properties panel opens with FormSubmitPanelUIToolkit
- [ ] Mobile controls appear on mobile platforms
- [ ] Grid snap toggle and size slider work
- [ ] Preview mode enables/disables controls correctly
- [ ] UI updates when scene/project changes
- [ ] Styles match GameTheme.tss design system

## References

- **Unity UI Toolkit Documentation:** https://docs.unity3d.com/Manual/UIElements.html
- **GameTheme.tss:** Design system tokens and component styles
- **FormSubmitPanelUIToolkit:** Dynamic form system integration
- **SceneSandboxBuilder:** Core sandbox functionality

## Code Example: Custom Palette Item Style

```csharp
// In CreateUIFromCode()
_rootElement.AddToClassList("palette-item");
_rootElement.AddToClassList("palette-item--custom");

// In USS
.palette-item--custom {
    background-color: rgba(100, 50, 150, 1);
    border-color: rgba(150, 100, 200, 1);
}

.palette-item--custom:hover {
    background-color: rgba(120, 70, 170, 1);
}
```

## Support

For issues or questions:
1. Check UI Toolkit documentation
2. Review existing FormSubmitPanelUIToolkit implementation
3. Inspect elements using UI Toolkit Debugger (Window > UI Toolkit > Debugger)
4. Refer to this document and code comments
