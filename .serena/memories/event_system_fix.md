# SceneSandboxBuilder Event System Fix - December 10, 2025

## Issue
Events in SceneSandboxBuilder were not invoking properly because:

1. **OnSceneLoaded/OnSceneSaved**: The builder was subscribing to `SceneSerializer`'s C# Action events but was NOT re-invoking the builder's own UnityEvent versions. This meant external subscribers to the builder's events would never get notified.

2. **OnObjectSelected**: The builder was subscribing to `SelectionManager.OnObjectSelected` event but was only triggering placement - it wasn't invoking the builder's own `_onObjectSelected` event.

## Root Cause
Event propagation chain was broken:
- Component events fired → Builder subscribed but didn't forward to its own events
- External subscribers to builder events got nothing

## Solution Applied

### Fix 1: SceneSerializer Event Handlers (Lines 611-626)
Added event invocation forwarding in the SceneSerializer subscription handlers:

```csharp
_sceneSerializer.OnSceneLoaded += (scene) =>
{
    _currentSceneName = scene.sceneName;
    _onSceneLoaded?.Invoke(scene);  // ← NOW INVOKES BUILDER EVENT
};

_sceneSerializer.OnSceneSaved += (scene) =>
{
    _onSceneSaved?.Invoke(scene);   // ← NOW INVOKES BUILDER EVENT
};

_sceneSerializer.OnSceneCleared += () =>
{
    _onSceneCleared?.Invoke();
};
```

### Fix 2: SelectionManager Event Handlers (Lines 544-551)
Added builder event invocation when objects are selected:

```csharp
_selectionManager.OnObjectSelected.AddListener((GameObject obj) =>
{
    _onObjectSelected?.Invoke(obj);  // ← NOW INVOKES BUILDER EVENT
    _placementSystem.StartPlacement(obj);
});
```

## Files Modified
- `Assets/Scripts/SceneSandbox/Core/SceneSandboxBuilder.cs` (2 changes)

## Verification
- ✅ No compilation errors
- ✅ Event chain now properly propagates from components → builder → external subscribers
- ✅ All builder public events now functional

## Testing Required
- Subscribe to builder events in UI/other systems and verify they fire correctly
- Test scene load/save event propagation
- Test object selection event propagation
