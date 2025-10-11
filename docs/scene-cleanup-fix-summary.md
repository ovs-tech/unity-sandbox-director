# Scene Cleanup Fix - Manager Persistence Issue

## Problem
When closing scenes in Unity, the following error appeared:
```
Some objects were not cleaned up when closing the scene... SelectionManager
```

This was caused by singleton managers using `DontDestroyOnLoad()` without proper scene-aware cleanup.

## Root Cause
Two singleton managers were persisting across scenes without cleaning up scene-specific data:
- `TransformableSelectionManager` - kept references to selected items from unloaded scenes
- `TransformControlManager` - maintained active transform items from unloaded scenes

## Solution Implemented

### 1. Added Scene Management Event Handlers
Both managers now subscribe to Unity's SceneManager events:
- `SceneManager.sceneUnloaded` - for cleanup when scenes are unloaded
- `SceneManager.sceneLoaded` - for re-initialization when scenes are loaded

### 2. TransformableSelectionManager Changes
- **OnSceneUnloaded**: Calls `ClearSelection()` to remove all selected items
- **OnSceneLoaded**: Calls `RegisterExistingDraggableItems()` to register items in the new scene
- **OnDestroy**: Properly unsubscribes from scene events

### 3. TransformControlManager Changes
- **OnSceneUnloaded**: 
  - Clears `_activeTransformItems` list
  - Resets `_currentActiveItem` to null
  - Calls `ExitAllTransformModes()` to clean up active states
- **OnSceneLoaded**: Items auto-register, no manual action needed
- **OnDestroy**: Properly unsubscribes from scene events

## Files Modified
- `Assets/Scripts/SceneSandbox/Core/TransformableSelectionManager.cs`
- `Assets/Scripts/SceneSandbox/Core/TransformControlManager.cs`

## Benefits
1. **No More Cleanup Warnings**: Managers properly clean up scene-specific data
2. **Memory Efficiency**: Prevents holding references to destroyed GameObjects
3. **Scene Isolation**: Each scene starts with a clean state
4. **Proper Persistence**: Managers still persist across scenes for global functionality
5. **Debug Logging**: Added logging to track scene transitions

## Testing
After implementing these changes:
1. Load a scene with transformable items
2. Select and manipulate items
3. Load a different scene
4. Verify no cleanup warnings appear in console
5. Verify new scene starts with clean selection state

## Note
The managers still use `DontDestroyOnLoad()` to maintain their singleton pattern and global input handling, but now they properly manage scene-specific data lifecycle.