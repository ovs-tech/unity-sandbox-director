# ObjectPaletteItem Auto UI Creation

## Overview

The ObjectPaletteItem class now automatically creates its own UI components if they don't exist. This eliminates the need for prefabs and allows for fully programmatic UI creation.

## Features

### Automatic UI Creation
- **Icon Image**: Automatically created with default sprite if none provided
- **Name Text**: Created with proper styling and layout
- **Select Button**: Added with proper button styling and event handling
- **Layout**: Properly sized and positioned components

### Self-Creating UI Components

When an ObjectPaletteItem is instantiated, it will automatically:

1. Check if required UI components exist
2. Create missing components programmatically
3. Set up proper layout and styling
4. Wire up event handlers

## Usage Examples

### Direct Creation (No Prefab Needed)
```csharp
// Create directly using the factory method
var paletteItem = ObjectPaletteItem.CreatePaletteItem(parentTransform, objectData, objectPalette);
```

### Automatic Creation via ObjectPalette
```csharp
// ObjectPalette will automatically create items even without prefabs
var objectPalette = gameObject.AddComponent<ObjectPalette>();
objectPalette.ObjectLibrary = someLibrary; // Items will be created automatically
```

### Manual Addition to Existing GameObject
```csharp
// Add to any existing GameObject - UI will be created automatically
var paletteItem = existingGameObject.AddComponent<ObjectPaletteItem>();
// UI components will be created in Awake()
```

## UI Layout Structure

When auto-created, the ObjectPaletteItem creates:

```
ObjectPaletteItem (RectTransform + Button + Image)
├── Icon (Image) - Top 70% of item
└── Name Text (Text) - Bottom 25% of item
```

## Technical Details

### Auto-Creation Process
1. `Awake()` calls `CreateUIIfMissing()`
2. If any UI component is null, `CreatePaletteItemUI()` is called
3. Components are created with proper hierarchy and layout
4. Default styling and sizing is applied

### Default Settings
- **Item Size**: 100x120 pixels
- **Icon Area**: Top 70% of item (with 10% margins)
- **Text Area**: Bottom 25% of item (with 2px margins)
- **Font**: Unity's built-in Legacy Runtime font
- **Colors**: White icon, black text, light gray background

### Event Handling
- Button clicks automatically trigger `OnItemSelected` event
- Drag operations are fully supported with visual feedback
- Selection state changes button color

## Integration with QuickSandboxDemo

The QuickSandboxDemo now creates a complete working system:

```csharp
[ContextMenu("Create Quick Demo")]
public void CreateQuickDemo()
{
    // Creates EventSystem, Canvas, ObjectLibrary, SandboxBuilder, ObjectPalette, and UI
    // All components are automatically linked and functional
}
```

## Benefits

1. **No Prefab Dependencies**: Works without any pre-configured prefabs
2. **Fully Programmatic**: Can be created entirely in code
3. **Consistent Styling**: Automatic styling ensures visual consistency
4. **Mobile Ready**: Touch-responsive design by default
5. **Event Ready**: All event handlers are automatically connected

## Testing

To test the system:

1. Add QuickSandboxDemo to any GameObject
2. Click "Create Quick Demo" in the context menu or let it run on Start
3. The system will create a complete sandbox environment with object palette
4. Objects can be selected and dragged from the palette

All UI components are created automatically without requiring any prefabs or manual setup.