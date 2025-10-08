# Clip Serialization Fix - Test Instructions

## Problem Summary
The issue was that clips added at runtime were not being serialized when saving projects. This was because:

1. **Projects contain `TrackData`** (serializable format) with `ClipData`
2. **Director contains `IMiniTrack`** (runtime format) with `IMiniClip` objects  
3. **During save**: Only the original `TrackData` was serialized, not the updated runtime tracks with clips

## Solution Implemented
Added a new method `UpdateProjectFromRuntimeTracks()` to `MiniTimelineDirector` that:

1. Converts runtime `IMiniTrack` objects back to `TrackData`
2. Converts runtime `IMiniClip` objects back to `ClipData` 
3. Extracts type-specific properties into the payload dictionary
4. Updates the `project.tracks` before serialization

## Test Steps

### Before Testing
1. Open Unity project
2. Open the scene with Timeline Editor
3. Create a new empty project or load an existing one

### Test Procedure
1. **Add some clips** to tracks at runtime (using the UI)
2. **Save the project** using the new save button
3. **Load the saved project** 
4. **Verify clips appear** correctly after loading

### Expected Results
- ✅ Clips added at runtime should now be saved
- ✅ Clips should load correctly with all their properties
- ✅ No more empty clips arrays in saved JSON files

### Debug Information
The save process now includes a debug log:
```
[MiniTimelineDirector] Updated project data from X runtime tracks
```

If you see this message, the fix is working correctly.

## Files Modified

### `MiniTimelineDirector.cs`
- Added `UpdateProjectFromRuntimeTracks()` method
- Added `ConvertRuntimeTrackToData()` method
- Added `ConvertRuntimeClipToData()` method
- Added `ExtractClipPayload()` method with support for:
  - `AnimClip`
  - `UMAExpressionClip` 
  - `UmaWardrobeClip`
  - `SignalClip`
  - `MovementClip`
- Added `GetTrackTypeName()` method
- Added `using MiniTimeline.Tracks;`

### `TimelineEditorUI.cs`
- Modified `OnSaveProjectFormSubmitted()` to call `director.UpdateProjectFromRuntimeTracks()` before saving

## Technical Details

The fix implements a **reverse serialization system** that converts runtime objects back to data structures:

```
Runtime Format     →     Serializable Format
IMiniTrack         →     TrackData
IMiniClip          →     ClipData
Clip Properties    →     payload Dictionary
```

This ensures that any changes made to tracks/clips at runtime are captured in the project data before serialization.