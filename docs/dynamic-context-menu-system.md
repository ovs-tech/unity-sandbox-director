# Dynamic Context Menu System

The Timeline Context Menu system has been redesigned as a dynamic, singleton-based panel that allows UI components to self-register their context menu items. This provides a flexible and extensible way to manage context menus throughout the timeline editor.

## Key Features

- **Singleton Pattern**: Single TimelineContextMenu instance manages all context menus
- **Dynamic Registration**: UI components can register/unregister menu items at runtime
- **Automatic Categorization**: Menu items are automatically grouped and separated by category
- **Priority-based Ordering**: Control the order of menu items within categories
- **Conditional Visibility**: Menu items can have visibility conditions
- **Rich Metadata**: Support for tooltips, custom colors, and icons

## Architecture

### Core Components

1. **TimelineContextMenu** - Singleton manager for all context menus
2. **IContextMenuRegisterable** - Interface for components that provide menu items
3. **ContextMenuBuilder** - Utility for building organized menus
4. **ContextMenuRegistry** - Registry managing all menu providers
5. **MenuContext** - Context information passed to menu providers

### Menu Categories

Menu items are automatically organized into these categories with separators:

- **Edit** (Cut, Copy, Paste, Delete)
- **Create** (Add new items)
- **Transform** (Move, Resize, Split)
- **View** (Zoom, Pan, Focus)
- **Properties** (Settings, Options)
- **Action** (Custom actions)
- **Debug** (Development tools)

## Usage Examples

### Basic Implementation

Any UI component can implement `IContextMenuRegisterable` to provide context menu items:

```csharp
public class MyComponent : MonoBehaviour, IContextMenuRegisterable
{
    public string ComponentId => $"MyComponent_{GetInstanceID()}";
    
    public IEnumerable<ContextMenuItem> GetContextMenuItems(MenuContext menuContext)
    {
        // Only provide items for relevant contexts
        if (menuContext.MenuType != "mytype")
            yield break;
            
        yield return new ContextMenuItem("My Action", () => DoAction(), 
                                       MenuCategory.Action, MenuPriority.Normal, "🔧")
            .WithTooltip("Performs my custom action");
    }
    
    public void RegisterContextMenuItems()
    {
        TimelineContextMenu.RegisterMenuProvider(this);
    }
    
    public void UnregisterContextMenuItems()
    {
        TimelineContextMenu.UnregisterMenuProvider(this);
    }
    
    public bool CanProvideMenuItems(MenuContext menuContext)
    {
        return menuContext.MenuType == "mytype";
    }
    
    // Register in lifecycle methods
    void Start() => RegisterContextMenuItems();
    void OnDestroy() => UnregisterContextMenuItems();
}
```

### Advanced Menu Items

```csharp
public IEnumerable<ContextMenuItem> GetContextMenuItems(MenuContext menuContext)
{
    // Conditional visibility
    yield return new ContextMenuItem("Delete", () => Delete(), 
                                   MenuCategory.Edit, MenuPriority.Delete, "🗑️")
        .WithVisibility(() => CanDelete())
        .WithTextColor(Color.red)
        .WithTooltip("Delete this item permanently");
    
    // Different priorities within same category
    yield return new ContextMenuItem("Copy", () => Copy(), 
                                   MenuCategory.Edit, MenuPriority.Copy, "📋");
    
    yield return new ContextMenuItem("Cut", () => Cut(), 
                                   MenuCategory.Edit, MenuPriority.Cut, "✂️");
    
    // Custom separators
    yield return ContextMenuItem.CreateSeparator(MenuCategory.Edit);
    
    // Debug items (only in development)
    #if UNITY_EDITOR
    yield return new ContextMenuItem("Debug Info", () => LogDebugInfo(), 
                                   MenuCategory.Debug, MenuPriority.Normal, "🐛");
    #endif
}
```

### Showing Context Menus

```csharp
// Using dynamic registration (recommended)
TimelineContextMenu.Instance.ShowDynamicMenu(screenPosition, targetObject, "clip");

// Using static menu items (legacy support)
var menuItems = new List<ContextMenuItem>
{
    new ContextMenuItem("Action", () => DoAction(), MenuCategory.Action)
};
TimelineContextMenu.Instance.ShowMenu(screenPosition, menuItems);
```

## Implementation in ClipUI

The ClipUI class demonstrates the full implementation:

- **Registration**: Automatically registers on Initialize(), unregisters on OnDestroy()
- **Context Filtering**: Only provides items when the menu context targets this specific clip
- **Rich Items**: Uses tooltips, icons, conditional visibility, and color coding
- **Categories**: Organizes items into Edit, Transform, Properties, and Debug categories
- **Integration**: Actions integrate with the existing command system

## Migration Guide

### For UI Components

1. Implement `IContextMenuRegisterable` interface
2. Provide menu items in `GetContextMenuItems()`
3. Add registration calls to component lifecycle
4. Update context menu trigger code to use dynamic system

### For Menu Consumers

Replace direct TimelineContextMenu method calls:

```csharp
// Old way
contextMenu.ShowClipMenu(clip, screenPosition);

// New way (still supported for backward compatibility)
contextMenu.ShowClipMenu(clip, screenPosition);

// Or fully dynamic
contextMenu.ShowDynamicMenu(screenPosition, clip, "clip");
```

## Benefits

- **Modularity**: Each component manages its own menu items
- **Extensibility**: Easy to add new menu items without modifying core menu code  
- **Consistency**: Automatic organization and styling
- **Maintainability**: Decoupled menu logic from menu presentation
- **Flexibility**: Rich customization options for menu items

## Future Enhancements

- Hierarchical/nested menus
- Keyboard shortcuts display
- Menu themes and styling options
- Plugin system for third-party menu providers
- Animated menu transitions
- Touch-friendly gesture support