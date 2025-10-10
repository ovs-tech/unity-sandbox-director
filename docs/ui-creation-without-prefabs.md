# Scene Sandbox Builder - UI Creation Guide

## Creating UI Without Prefabs

The Scene Sandbox Builder can automatically create all necessary UI components when prefabs are not available. This is perfect for quick prototyping or when you want to get started immediately without setting up complex prefab hierarchies.

## Quick Start (No Prefabs Required)

### Option 1: QuickSandboxDemo Script (Easiest)

1. **Add the QuickSandboxDemo component** to any GameObject in your scene
2. **Check "Create On Start"** in the inspector
3. **Run the scene** - everything will be created automatically!

The QuickSandboxDemo script will create:
- Complete UI layout with all panels
- Scene Sandbox Builder system
- Object palette with demo objects
- Proper canvas and event system setup
- A simple stage area for testing

### Option 2: Manual Setup with Auto-Generated UI

1. **Create a Canvas** in your scene (if none exists)
2. **Add SandboxBuilderUI component** to a GameObject under the Canvas
3. **Add ObjectPalette component** to another GameObject under the Canvas
4. **Run the scene** - UI will be automatically generated!

Both `SandboxBuilderUI` and `ObjectPalette` will detect missing UI components and create them programmatically.

## What Gets Created Automatically

### SandboxBuilderUI Components
- **Scene Controls Panel** (top of screen)
  - New Scene, Save Scene, Load Scene buttons
  - Clear Scene, Preview, Stop Preview buttons
  - Scene name and object count display
  - Preview status indicator

- **Properties Panel** (right side of screen)
  - Object information section
  - Transform editing (position, rotation, scale)
  - Object controls (delete, grid settings)
  - Snap to grid toggle and grid size slider

- **Mobile Controls Panel** (bottom of screen, mobile only)
  - Palette toggle button
  - Properties toggle button

### ObjectPalette Components
- **Header Section**
  - Object Palette title
  - Type filter dropdown (All Types, Actor, Prop, Camera, Light)
  - Category filter dropdown
  - Search input field
  - Refresh button

- **Item Container**
  - Scrollable grid layout for object items
  - Automatic item prefab generation
  - Responsive layout that adapts to screen size

## Customization

### Styling the Auto-Generated UI

The auto-generated UI uses basic colors and layouts. You can customize these by:

1. **Modifying the UI creation methods** in `SandboxBuilderUI.cs` and `ObjectPalette.cs`
2. **Changing colors, fonts, and sizes** in the helper methods
3. **Adding your own styling** after the UI is created

Example customization in `SandboxBuilderUI.cs`:
```csharp
private Button CreateButton(Transform parent, string text)
{
    // ... existing code ...
    
    // Customize button appearance
    buttonImage.color = new Color(0.2f, 0.5f, 0.8f, 1f); // Blue theme
    textComponent.fontSize = 14; // Larger text
    
    return button;
}
```

### Adding Your Own Object Library

Replace the demo objects with your own:

```csharp
var myObjects = new SceneObjectData[]
{
    new SceneObjectData("My Actor", myActorPrefab, SceneObjectType.Actor)
    {
        id = "my_actor_01",
        category = "Characters",
        icon = myActorIcon
    },
    // Add more objects...
};
```

### Mobile-Specific Adaptations

The UI automatically adapts for mobile devices:
- Larger touch targets
- Mobile control panel with toggle buttons
- Responsive layouts
- Touch-optimized spacing

## UI Layout Structure

The auto-generated UI follows this hierarchy:

```
Canvas (Screen Space Overlay)
├── SandboxBuilderUI (fills canvas)
│   ├── Scene Controls Panel (top, 80px height)
│   │   ├── New Scene Button
│   │   ├── Save Scene Button
│   │   ├── ... other buttons
│   │   └── Scene Info Texts
│   ├── Properties Panel (right, 300px width)
│   │   └── Scroll View
│   │       └── Content (vertical layout)
│   │           ├── Object Info Section
│   │           ├── Transform Section
│   │           └── Object Controls Section
│   └── Mobile Controls Panel (bottom, 60px height, mobile only)
├── ObjectPalette (left, 250px width)
│   ├── Header (filters and search)
│   └── Item Container (scrollable grid)
└── Stage Area (3D scene space)
    ├── Ground Plane
    ├── Camera
    └── Lighting
```

## Performance Considerations

The auto-generated UI is designed for:
- **Rapid prototyping** - Get started quickly without setup
- **Learning and testing** - Understand the system structure
- **Mobile compatibility** - Works on touch devices

For production use, consider:
- Creating optimized prefabs based on the generated structure
- Using Unity's UI Toolkit for better performance
- Implementing object pooling for palette items
- Adding custom animations and transitions

## Troubleshooting

### UI Not Appearing
1. Ensure there's a Canvas in the scene
2. Check that EventSystem exists for UI interaction
3. Verify Canvas render mode is set to Screen Space Overlay
4. Make sure Canvas sorting order is appropriate

### Mobile Controls Not Working
1. Enable "Touch Simulation" in Input System settings for editor testing
2. Check that mobile controls are enabled in the script
3. Verify touch input actions are properly configured

### Objects Not Dragging
1. Ensure EventSystem is present
2. Check that GraphicRaycaster is on the Canvas
3. Verify the object library has valid object data
4. Make sure DraggableItem components are properly initialized

## Advanced Features

### Custom UI Themes
You can create themed UI by modifying the color schemes in the helper methods:

```csharp
// Dark theme example
private static readonly Color DarkBackground = new Color(0.1f, 0.1f, 0.1f, 0.9f);
private static readonly Color DarkButton = new Color(0.2f, 0.2f, 0.2f, 1f);
private static readonly Color AccentColor = new Color(0.3f, 0.6f, 1f, 1f);
```

### Dynamic Layout Adaptation
The UI automatically adapts to different screen sizes and orientations. You can extend this by:
- Adding breakpoints for different screen sizes
- Implementing responsive font scaling
- Creating adaptive layouts for different aspect ratios

### Integration with Existing UI Systems
If you have an existing UI framework:
1. Disable auto-generation by setting the SerializeField references
2. Use the auto-generated UI as a reference for manual implementation
3. Connect your existing UI elements to the sandbox builder events

This approach gives you maximum flexibility while providing a solid foundation to build upon!