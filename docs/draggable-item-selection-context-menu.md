# DraggableItem Click Selection and Context Menu

## Overview

The `DraggableItem` component now supports click-based selection and context menus triggered by long press or right-click. This implementation provides a complete selection system for scene objects with visual feedback and extensible context menu functionality.

## Features

### 1. Click Selection
- **Single Click**: Selects the item and shows visual feedback
- **Double Click**: Triggers the `OnItemSelected` event for special actions
- **Selection Manager**: Ensures only one item is selected at a time (configurable)
- **Visual Feedback**: Selected items show different material/color

### 2. Long Press & Context Menu
- **Long Press Detection**: Configurable duration (default 0.8 seconds)
- **Movement Threshold**: Cancels long press if moved too far (20 pixels)
- **Context Menu**: Shows on long press or right-click
- **Extensible Actions**: Easy to add custom menu items

### 3. Selection Management
- **SelectionManager**: Centralized selection state management
- **Events**: Subscribe to selection changes across the application
- **Background Clicks**: Clear selection when clicking empty space

## Components

### DraggableItem (Enhanced)
```csharp
// New properties
public bool IsSelected { get; }
public bool IsLongPressing { get; }

// New events
public System.Action<DraggableItem, bool> OnSelectionChanged;
public System.Action<DraggableItem, Vector2> OnItemLongPressed;
```

### SelectionManager (New)
```csharp
// Access the singleton
SelectionManager.Instance.SelectItem(item);
SelectionManager.Instance.ClearSelection();

// Check selection state
bool isSelected = SelectionManager.Instance.IsSelected(item);
var selectedItem = SelectionManager.Instance.SelectedItem;
```

### Context Menu System
The `DraggableItem` implements `IContextMenuRegisterable` providing:
- Select/Deselect
- Copy/Delete
- Reset Position
- Lock/Unlock
- Debug Info

## Setup Instructions

### 1. Existing DraggableItems
Existing `DraggableItem` components will automatically gain the new functionality. The system is backward compatible.

### 2. Scene Setup
```csharp
// The SelectionManager is automatically created as a singleton
// Optionally, add the BackgroundClickHandler to your camera:
Camera.main.gameObject.AddComponent<BackgroundClickHandler>();
```

### 3. Testing
Use the `DraggableTestSpawner` component to test the functionality:
```csharp
// Add to any GameObject in your scene
gameObject.AddComponent<DraggableTestSpawner>();
```

## Usage

### Basic Selection
```csharp
// Select an item programmatically
SelectionManager.Instance.SelectItem(draggableItem);

// Listen for selection changes
SelectionManager.Instance.OnSelectionChanged += (items) => {
    Debug.Log($"Selection changed: {items.Count} items selected");
};
```

### Custom Context Menu Items
```csharp
// In your custom component implementing IContextMenuRegisterable
public IEnumerable<ContextMenuItem> GetContextMenuItems(MenuContext menuContext)
{
    yield return new ContextMenuItem("My Action", () => DoMyAction(), 
        MenuCategory.Action, MenuPriority.Normal, "🎯");
}
```

### Long Press Detection
```csharp
// Configure long press settings in the inspector
[SerializeField] private float _longPressDuration = 0.8f; // seconds
```

## Controls (Test Mode)

When using `DraggableTestSpawner`:
- **C** - Spawn Cube
- **S** - Spawn Sphere  
- **Delete** - Delete Selected Items
- **Esc** - Clear Selection
- **Click** - Select Item
- **Long Press** - Show Context Menu
- **Right Click** - Show Context Menu

## Events

### Selection Events
```csharp
// DraggableItem events
draggableItem.OnSelectionChanged += (item, isSelected) => { };
draggableItem.OnItemLongPressed += (item, screenPos) => { };

// SelectionManager events
SelectionManager.Instance.OnItemSelected += (item) => { };
SelectionManager.Instance.OnItemDeselected += (item) => { };
SelectionManager.Instance.OnSelectionChanged += (items) => { };
```

## Visual Feedback

### Selection State
The `SetSelectedState(bool)` method handles visual feedback:
- Shows hover/selection material when selected
- Restores original material when deselected
- Prioritizes drag material during dragging

### Materials
Configure materials in the inspector:
- `_hoverMaterial` - Used for hover and selection feedback
- `_dragMaterial` - Used during drag operations

## Integration Notes

### Existing Systems
- **Drag System**: Works alongside the existing drag functionality
- **EventSystem**: Uses Unity's EventSystem for pointer events
- **Context Menu**: Integrates with the existing `Core.UI.ContextMenu` system

### Performance
- Selection state is managed efficiently through events
- Long press detection uses coroutines (automatically cleaned up)
- Context menu items are generated on-demand

## Customization

### Selection Behavior
```csharp
// Allow multiple selection (in SelectionManager)
SelectionManager.Instance._allowMultipleSelection = true;
```

### Long Press Timing
```csharp
// Adjust in DraggableItem inspector
_longPressDuration = 1.2f; // Longer press required
_longPressMoveThreshold = 30f; // Allow more movement
```

### Context Menu Actions
Extend the `GetContextMenuItems` method in `DraggableItem` or create your own `IContextMenuRegisterable` components.

## Troubleshooting

### Selection Not Working
1. Ensure EventSystem exists in scene
2. Check camera has PhysicsRaycaster component
3. Verify SelectionManager is instantiated

### Context Menu Not Showing
1. Check long press duration setting
2. Verify movement threshold
3. Ensure context menu registry is working

### Visual Feedback Issues
1. Check material assignments in inspector
2. Verify renderer components exist
3. Look for material conflicts with other systems