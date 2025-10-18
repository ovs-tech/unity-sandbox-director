# TimelineRulerToolkit USS and UXML Separation

This document explains the separation of TimelineRulerToolkit's styling (USS) and structure (UXML) into separate files.

## Files Created

### 1. TimelineRulerToolkit.uxml
**Location:** `Assets/Scripts/MiniTimeline/UI/Styles/TimelineRulerToolkit.uxml`

Contains the UXML structure for the timeline ruler including:
- Main ruler container
- Current time indicator
- Snap guide element
- Playhead interaction area
- Comments showing the structure of dynamically generated markers

### 2. TimelineRulerToolkit.uss
**Location:** `Assets/Scripts/MiniTimeline/UI/Styles/TimelineRulerToolkit.uss`

Contains comprehensive styling for:
- **Ruler container** - Main ruler appearance and background
- **Time markers** - Major/minor markers with different styles
- **Time labels** - Text formatting for time values
- **Current time indicator** - Red line showing playhead position
- **Snap guides** - Yellow guidelines for snapping feedback
- **Zoom-dependent styling** - Different appearances at various zoom levels
- **Hover and interaction states** - Visual feedback

## Code Changes

### TimelineRulerToolkit.cs
- Added support for UXML/USS template loading
- Enhanced `Initialize()` method to accept templates as parameters
- Added fallback to Resources loading if templates not provided
- Improved marker generation with major/minor intervals
- Added zoom-aware styling classes
- Enhanced time formatting and snap guide functionality

### TimelineEditorUIToolkit.cs
- Added `rulerTemplate` and `rulerStyleSheet` serialized fields
- Added public properties for template access
- Updated `InitializeRuler()` to pass templates to ruler

## Usage

### In Inspector
1. Assign the UXML and USS files to the `Ruler Template` and `Ruler Style Sheet` fields in TimelineEditorUIToolkit
2. If not assigned, the system will attempt to load from `Resources/UI/TimelineRulerToolkit`

### Customization
- **Styling**: Modify `TimelineRulerToolkit.uss` to change colors, sizes, fonts
- **Structure**: Modify `TimelineRulerToolkit.uxml` to add/remove elements
- **Behavior**: Modify `TimelineRulerToolkit.cs` for different marker intervals or interactions

## Features

### Zoom-Adaptive Styling
The ruler adapts its appearance based on zoom level:
- **Low zoom** (< 0.5x): Hides minor markers, shows only major intervals
- **High zoom** (> 2x): Shows all markers with enhanced visibility

### Marker Types
- **Major markers**: Every 5-10 seconds with time labels
- **Minor markers**: Sub-second intervals for precision

### Interactive Elements
- **Current time indicator**: Red line tracking playhead position
- **Snap guides**: Yellow lines showing snap positions during drag operations
- **Playhead area**: Full-width interaction zone for time scrubbing

## Benefits of Separation

1. **Maintainability**: Styling and structure separated from logic
2. **Designer-friendly**: Artists can modify USS without touching code
3. **Reusability**: Templates can be shared across different timeline implementations
4. **Performance**: UXML templates are more efficient than programmatic creation
5. **Consistency**: Matches the pattern used by other UI Toolkit components

## Future Enhancements

- Add support for different time display formats (frames, samples, SMPTE)
- Implement theme switching for dark/light modes
- Add animation support for smooth zoom transitions
- Extend for custom ruler backgrounds and textures