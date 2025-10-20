# Timeline Editor UI Toolkit

This document describes the UI Toolkit-based implementation of the Timeline Editor, which provides a modern, performant alternative to the uGUI-based timeline editor.

## Overview

The `TimelineEditorUIToolkit` class provides the same functionality as the original `TimelineEditorUI` but uses Unity's UI Toolkit system instead of uGUI. This offers several advantages:

- **Better Performance**: UI Toolkit is optimized for complex UIs and large numbers of elements
- **Modern Styling**: USS (Unity Style Sheets) provide CSS-like styling capabilities
- **Better Responsive Design**: Flexbox-based layout system adapts better to different screen sizes
- **Data Binding**: Built-in support for data binding and MVVM patterns
- **Vector Graphics**: Resolution-independent UI rendering

## Files Structure

```
Assets/Scripts/MiniTimeline/UI/
├── TimelineEditorUIToolkit.cs        # Main UI Toolkit timeline editor
├── TimelineEditorUIToolkit.uxml      # UI structure definition
├── TimelineEditorUIToolkit.uss       # Styling definitions
├── TimelineEditorUIToolkitSetup.cs   # Setup helper script
└── ITimelineEditorUI.cs              # Interface for timeline editors
```

## Setup Instructions

### 1. Basic Setup

1. Create a new GameObject in your scene
2. Add the `TimelineEditorUIToolkitSetup` component
3. Add a `UIDocument` component
4. Assign the `TimelineEditorUIToolkit.uxml` file to the UIDocument's Visual Tree Asset field
5. The setup script will automatically configure the timeline editor

### 2. Manual Setup

If you prefer manual setup:

1. Create a GameObject with `UIDocument` component
2. Set the Visual Tree Asset to `TimelineEditorUIToolkit.uxml`
3. Add `TimelineEditorUIToolkit` component to the same GameObject
4. Configure the following in the inspector:
   - Set `uiDocument` reference
   - Configure `pixelsPerSecond`, zoom limits, etc.
   - Set USS class names if you've customized them

### 3. Timeline Director Setup

Ensure you have a `MiniTimelineDirector` in your scene:

1. Create a GameObject with `MiniTimelineDirector` component
2. The timeline editor will automatically find it, or you can assign it manually
3. Create sample bindings using the setup script's context menu

## UXML Structure

The timeline editor expects the following UXML structure:

```xml
<ui:VisualElement name="timeline-editor" class="timeline-editor">
    <ui:VisualElement class="controls-container">
        <!-- Playback, edit, and project controls -->
    </ui:VisualElement>
    
    <ui:ScrollView name="timeline-scroll">
        <ui:VisualElement class="timeline-container">
            <ui:VisualElement class="ruler-container" />
            <ui:VisualElement class="tracks-container" />
            <ui:VisualElement name="playhead" class="playhead" />
        </ui:VisualElement>
    </ui:ScrollView>
    
    <ui:VisualElement class="status-bar">
        <!-- Status text and zoom controls -->
    </ui:VisualElement>
</ui:VisualElement>
```

## USS Styling

The timeline editor uses CSS-like styling through USS. Key classes include:

- `.timeline-editor` - Root container
- `.controls-container` - Top toolbar
- `.timeline-container` - Main timeline area
- `.ruler-container` - Time ruler
- `.tracks-container` - Track list
- `.track` - Individual track
- `.clip` - Individual clip
- `.playhead` - Current time indicator

You can customize the appearance by modifying the USS file or creating your own stylesheet.

## Key Features

### Track Management
- Add new tracks through the toolbar
- Support for different track types (Animation, Audio, Camera, Custom)
- Drag and drop track reordering
- Track enable/disable functionality

### Clip Editing
- Visual clip representation with resize handles
- Drag clips to move them in time
- Resize clips by dragging edges
- Multi-select support for batch operations

### Timeline Navigation
- Zoom in/out with mouse wheel or zoom slider
- Pan with middle mouse drag or scrollbar
- Frame-accurate snapping
- Playhead scrubbing

### Binding Management
- Automatic scene object detection
- Manual binding creation and removal
- Real-time binding status display
- Support for `BindableObject` components

### Project Management
- Save/load timeline projects to JSON
- Project information display
- Undo/redo support for all operations

## Differences from uGUI Version

### Architecture Changes
1. **Element Creation**: Uses `VisualElement` hierarchy instead of GameObject prefabs
2. **Event Handling**: Uses `RegisterCallback<T>()` instead of UnityEvents
3. **Layout**: Uses Flexbox layout instead of RectTransform anchoring
4. **Styling**: Uses USS instead of component-based styling

### Performance Improvements
1. **No GameObjects**: Timeline clips are lightweight VisualElements
2. **Efficient Updates**: Only visible elements are updated
3. **GPU-Accelerated**: UI rendering uses GPU acceleration
4. **Memory Efficient**: Lower memory footprint for large timelines

### Feature Parity
All features from the original uGUI timeline editor are preserved:
- ✅ Track creation and management
- ✅ Clip editing and manipulation
- ✅ Binding management
- ✅ Project save/load
- ✅ Undo/redo system
- ✅ Context menus and forms
- ✅ Playback controls

## Migration Guide

To migrate from the uGUI version:

1. **Replace Component**: Change `TimelineEditorUI` to `TimelineEditorUIToolkit`
2. **Update References**: Any code referencing the editor should use `ITimelineEditorUI` interface
3. **Remove Prefabs**: UI Toolkit doesn't use prefabs for UI elements
4. **Update Styling**: Convert any custom styling to USS format

## Customization

### Custom Styling
Create your own USS file and assign it to the UIDocument:

```css
.my-custom-timeline {
    background-color: rgb(30, 30, 30);
}

.my-custom-clip {
    background-color: rgb(100, 150, 200);
    border-radius: 5px;
}
```

### Custom Track Types
Extend `TrackUIToolkit` class to create custom track representations:

```csharp
public class MyCustomTrackUI : TrackUIToolkit
{
    protected override void CreateTrackElement()
    {
        base.CreateTrackElement();
        // Add custom elements
    }
}
```

### Custom Clip Types
Extend `ClipUIToolkit` class for custom clip visualization:

```csharp
public class MyCustomClipUI : ClipUIToolkit
{
    protected override void CreateClipElement()
    {
        base.CreateClipElement();
        // Add custom styling or elements
    }
}
```

## Troubleshooting

### Common Issues

1. **UI Not Showing**: Ensure UXML file is assigned to UIDocument
2. **Styling Issues**: Check that USS file is applied to root element
3. **Timeline Director Not Found**: Ensure MiniTimelineDirector exists in scene
4. **Binding Errors**: Use the binding manager to verify scene object bindings

### Performance Tips

1. **Limit Visible Elements**: Use virtualization for very long timelines
2. **Optimize USS**: Avoid complex selectors and animations
3. **Update Frequency**: Reduce update frequency for non-critical elements
4. **Memory Management**: Dispose of unused VisualElements properly

## Future Enhancements

Planned improvements include:
- Virtual scrolling for large timelines
- Advanced clip grouping and layers
- Real-time collaborative editing
- Plugin system for custom track types
- Improved accessibility support
- Touch and gamepad input support

## API Reference

### TimelineEditorUIToolkit

Main class providing the timeline editor functionality.

#### Properties
- `PixelsPerSecond`: Current zoom level in pixels per second
- `CurrentZoom`: Current zoom factor
- `Director`: Associated timeline director
- `SelectedClips`: Currently selected clips

#### Methods
- `SetDirector(director)`: Assign timeline director
- `BuildTimelineUI()`: Rebuild UI from project data
- `SetZoom(zoom)`: Set timeline zoom level
- `ExecuteCommand(command)`: Execute timeline command with undo support

### ITimelineEditorUI

Interface implemented by both uGUI and UI Toolkit editors for compatibility.

#### Methods
- `BuildTimelineUI()`: Rebuild timeline UI

This ensures commands and other systems can work with both editor implementations.