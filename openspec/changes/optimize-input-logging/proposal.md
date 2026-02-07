# Change: Optimize Input Manager Logging

## Why
Debug logging inside the `OnPressUpdate` loop causes significant performance degradation when `DebugMode` is enabled. Measurements show it makes the update loop ~600x slower. Removing the excessive logging improves performance by ~10x in debug mode and avoids potential I/O bottlenecks.

## What Changes
- Remove the `Debug.Log` call in `InputInteractionManager.OnPressUpdate` that logs on every frame.

## Impact
- Affected specs: timeline-editor
- Affected code: `Assets/Scripts/MiniTimeline/UI/Input/InputInteractionManager.cs`
