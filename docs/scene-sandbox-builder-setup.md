# Scene Sandbox Builder - Setup Guide

## Overview
The Scene Sandbox Builder provides a drag-and-drop interface for placing actors, props, cameras, and lights in Unity scenes. It supports both desktop (mouse/keyboard) and mobile (touch) interactions with save/load functionality and timeline preview integration.

## Quick Setup

### 1. Prerequisites
- Unity 2022.3+ with Universal Render Pipeline
- Input System package installed
- EventSystem in scene for UI interactions
- MiniTimeline system (for preview functionality)

### 2. Automatic Setup
1. Add the `SceneSandboxBuilderSetup` component to any GameObject in your scene
2. In the Inspector, click the context menu (three dots) and select "Auto-Setup Scene"
3. Assign the `DefaultSceneObjectLibrary` asset to the Object Library field
4. Run the scene - the system will initialize automatically

### 3. Manual Setup (Advanced)

#### Input Actions
The system uses the "SceneBuilder" action map from `UIAndGameplay.inputactions`:
- **Select**: Left mouse / Touch tap - Select and interact with objects
- **Delete**: Delete/X key - Remove selected objects
- **Duplicate**: Ctrl+D - Duplicate selected objects
- **ToggleSnapToGrid**: G key - Toggle grid snapping
- **Preview**: P key - Start scene preview
- **SaveScene**: Ctrl+S - Save current scene configuration
- **LoadScene**: Ctrl+O - Load scene configuration

#### Scene Structure
```
Scene Root
├── Stage (Transform) - Container for all placed objects
├── SandboxUI (Canvas) - UI container
│   ├── ObjectPalette - Object selection interface
│   └── SandboxBuilderUI - Main controls and properties
├── SandboxInputHandler - Input management
├── SceneSandboxBuilder - Main controller
└── GridVisualization (Optional) - Visual grid overlay
```

## Usage Instructions

### Desktop Controls
- **Left Click**: Select objects or place from palette
- **Drag**: Move selected objects around the stage
- **Delete/X**: Remove selected objects
- **Ctrl+D**: Duplicate selected objects
- **G**: Toggle snap-to-grid
- **P**: Preview scene with timeline
- **Ctrl+S**: Save scene
- **Ctrl+O**: Load scene

### Mobile Controls
- **Tap**: Select objects or place from palette
- **Drag**: Move objects with touch
- **Long Press**: Context menu for delete/duplicate
- **Two-Finger Pinch**: Camera zoom (if camera controls enabled)
- **UI Buttons**: Access save/load and preview functions

### Object Palette
- **Categories**: Filter objects by type (Actors, Props, Cameras, Lights)
- **Search**: Find specific objects by name
- **Drag to Stage**: Drag objects from palette to place them
- **Properties Panel**: Adjust selected object properties

### Scene Management
- **Save Scene**: Saves object positions, rotations, and properties to JSON
- **Load Scene**: Restores saved scene configuration
- **Preview**: Integrates with MiniTimeline to play the scene
- **Clear Scene**: Removes all placed objects

## Customization

### Adding New Objects
1. Create prefabs for your objects (actors, props, etc.)
2. Add entries to the `SceneObjectLibrary` asset:
   ```csharp
   var newObject = new SceneObjectData
   {
       id = "unique_id",
       displayName = "Display Name",
       objectType = SceneObjectType.Actor, // or Prop, Camera, Light
       category = "Category Name",
       prefab = yourPrefabReference,
       icon = iconTexture,
       description = "Object description",
       isAvailable = true,
       allowMultipleInstances = true,
       snapToGrid = true
   };
   ```

### Custom UI Styling
- Modify `ObjectPaletteItem` prefab for palette item appearance
- Customize `SandboxBuilderUI` for main interface styling
- Use Unity's UI Toolkit or uGUI styling systems

### Integration with Existing Systems
- **UMA Characters**: Use UMA prefabs as actor objects
- **Timeline Integration**: Preview functionality works with existing MiniTimeline
- **Custom Properties**: Extend `PlacedObjectData` for additional object data
- **Save System**: JSON format allows easy integration with game save systems

## File Structure

### Core Scripts
- `SceneSandboxBuilder.cs` - Main controller
- `SandboxInputHandler.cs` - Input management
- `DraggableItem.cs` - Object drag behavior
- `DropZone.cs` - Drop area validation

### Data Classes
- `SceneObjectData.cs` - Object definitions
- `SceneConfiguration.cs` - Save/load data structure
- `SceneObjectLibrary.cs` - Object library management

### UI Components
- `ObjectPalette.cs` - Object selection interface
- `ObjectPaletteItem.cs` - Individual palette items
- `SandboxBuilderUI.cs` - Main UI controller

### Setup Helper
- `SceneSandboxBuilderSetup.cs` - Automated scene setup

## Mobile Optimization

### Performance Tips
- Use object pooling for frequently placed/removed objects
- Limit simultaneous draggable objects
- Optimize UI for touch targets (minimum 44px)
- Use efficient raycasting for object selection

### Touch Gestures
- Single tap: Select/Place
- Drag: Move objects
- Long press: Context menu
- Two-finger gestures: Camera controls (optional)

## Integration Examples

### Basic Scene Setup
```csharp
// Get the sandbox builder
var builder = FindObjectOfType<SceneSandboxBuilder>();

// Place an object programmatically
var actorData = objectLibrary.GetObjectById("actor_male_01");
if (actorData != null)
{
    builder.PlaceObject(actorData, new Vector3(0, 0, 0));
}

// Save scene
builder.SaveScene("MyScene");

// Load scene
builder.LoadScene("MyScene");
```

### Custom Object Creation
```csharp
// Create a custom object type
[System.Serializable]
public class CustomObjectData : SceneObjectData
{
    public float customProperty;
    public bool enableSpecialFeature;
}

// Handle custom placement logic
public class CustomSandboxBuilder : SceneSandboxBuilder
{
    protected override GameObject CreateObjectInstance(SceneObjectData objectData, Vector3 position)
    {
        var instance = base.CreateObjectInstance(objectData, position);
        
        if (objectData is CustomObjectData customData)
        {
            // Apply custom properties
            var customComponent = instance.GetComponent<CustomComponent>();
            customComponent.customValue = customData.customProperty;
        }
        
        return instance;
    }
}
```

## Troubleshooting

### Common Issues

**Objects not dragging:**
- Ensure EventSystem is in scene
- Check that DraggableItem component is attached
- Verify Input System is properly configured

**Touch input not working:**
- Enable "Touch Simulation" in Input System settings for editor testing
- Ensure mobile input bindings are correctly configured
- Check that touch actions are enabled in the action map

**Save/Load not working:**
- Verify write permissions in build settings
- Check file paths are valid for target platform
- Ensure JSON serialization is working correctly

**UI not displaying:**
- Check Canvas render mode and sorting order
- Verify UI prefabs are assigned correctly
- Ensure Canvas Scaler is configured for responsive design

### Debug Tools
- Enable debug logging in `SceneSandboxBuilder.cs`
- Use Unity's Input System debugger
- Check console for initialization errors
- Verify object library references in Inspector

## Advanced Features

### Grid Snapping
- Configurable grid size
- Visual grid overlay option
- Per-object snap settings
- Runtime grid toggle

### Multi-Selection
- Shift+Click for multiple selection
- Drag multiple objects together
- Bulk operations (delete, duplicate)
- Group movement with constraints

### Undo/Redo System
- Command pattern implementation
- Configurable history depth
- UI buttons for undo/redo
- Keyboard shortcuts integration

This setup guide should help you integrate the Scene Sandbox Builder into your Unity project. The system is designed to be modular and extensible, allowing for easy customization based on your specific needs.