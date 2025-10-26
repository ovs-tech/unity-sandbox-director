# Clip Serialized Data Display Feature

## Overview
Added a collapsible section in the MiniTimelineDirector inspector to display both runtime and serialized clip data, helping with debugging and inspection during development.

## Implementation Details

### 1. Foldout State Management
- Added `clipDataFoldouts` Dictionary to track the expanded/collapsed state of each clip's data section
- Key format: `"{trackId}_{clipId}"` to uniquely identify each clip
- Persists state during the editor session

### 2. UI Components

#### Foldout Button
Located below each clip's info section with the label "Serialized Data":
```csharp
clipDataFoldouts[clipFoldoutKey] = EditorGUILayout.Foldout(
    clipDataFoldouts[clipFoldoutKey], 
    "Serialized Data", 
    true,
    EditorStyles.foldout
);
```

#### Runtime Clip Data Display
Shows the current runtime state of the clip:
- **Type**: Clip's C# class name (e.g., `MovementClip`, `AnimatorClip`)
- **Start**: Start time in seconds
- **Duration**: Clip duration in seconds
- **End**: Calculated end time (start + duration)
- **ID**: Clip's unique identifier

#### Serialized Clip Data Display
Shows data from the project JSON file:
- **Start**: Serialized start time
- **Duration**: Serialized duration
- **Payload**: Dictionary of clip-specific properties

### 3. Payload Display
The payload dictionary is displayed as key-value pairs:
- Each entry shows the key (left) and value (right)
- Values are displayed in a selectable label for easy copying
- JSON-like strings are automatically formatted with indentation
- Handles null values gracefully

### 4. JSON Formatting
A simple JSON formatter (`FormatJsonForDisplay`) that:
- Adds indentation for nested structures
- Breaks lines at `{`, `}`, `[`, `]`, and `,`
- Preserves string content without modification
- Makes complex data structures more readable

## User Experience

### Visual Hierarchy
1. **Runtime Data** - Yellow tinted header (0.9, 0.9, 0.6)
   - Shows current in-memory clip state
   
2. **Serialized Data** - Cyan tinted header (0.6, 0.9, 0.9)
   - Shows saved project file data
   
3. **Info Boxes** - Context-aware messages:
   - "No payload data" - Clip has empty payload dictionary
   - "Serialized data not found (clip not saved yet)" - Clip exists only in memory
   - "Track data not found in project" - Track not in project structure
   - "No project loaded" - Director has no active project

### Use Cases

1. **Debugging Clip Issues**
   - Compare runtime vs. serialized values to find save/load issues
   - Verify payload data is correctly stored

2. **Development Inspection**
   - Quick access to clip properties without opening JSON files
   - Visual confirmation of data structure

3. **Data Validation**
   - Check if clips are properly saved to project
   - Verify timing values match expectations
   - Inspect custom clip payload data

## Technical Notes

### ClipData Structure
From `MiniTimelineProject.cs`:
```csharp
public class ClipData
{
    public string id;
    public float start;
    public float duration;
    public Dictionary<string, object> payload;
}
```

### Data Flow
1. Runtime clip (`IMiniClip`) → Created in memory
2. Serialized clip (`ClipData`) → Saved to project JSON
3. UI displays both side-by-side for comparison

### Dependencies
- **System.Collections.Generic** - For Dictionary type
- **System.Linq** - For FirstOrDefault() to find clip data
- **UnityEditor** - For EditorGUILayout components

## Code Location

### Files Modified
- `Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs`

### Methods Added
1. `DrawClipSerializedData(IMiniTrack track, IMiniClip clip)`
   - Main display logic for clip data
   - Handles both runtime and serialized data
   
2. `FormatJsonForDisplay(string json)`
   - Simple JSON formatter for readability
   - Adds indentation and line breaks

### Integration Point
The foldout is added in the clip rendering loop within `DrawTrackInfo()`:
- Positioned after "Additional clip info"
- Before `EditorGUILayout.EndVertical()` of clip section

## Future Enhancements

Potential improvements:
1. Add "Copy to Clipboard" button for payload data
2. Highlight differences between runtime and serialized values
3. Support editing payload values directly in inspector
4. Add search/filter for specific payload keys
5. Export clip data to separate JSON file
6. Show modification timestamp if available
