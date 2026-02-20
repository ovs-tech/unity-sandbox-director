# PlacementSystem

A highly modular, extensible, and flexible 3D object placement system for Unity, designed for scene-builder games (similar to The Sims build mode or stage builders).

## Architecture Overview

The system follows **SOLID principles** with heavy use of:
- **Strategy Pattern** for placement modes
- **Dependency Injection** for loose coupling
- **Open-Closed Principle** via ScriptableObject-based rules
- **Interface abstraction** for all major components
- **Tool Pattern** for runtime interaction modes

## Core Components

### 1. Interfaces (Core/)

All major systems are defined via interfaces for maximum flexibility:

- **IInputProvider**: Abstracts input handling (Legacy Input vs New Input System)
- **IPlacementStrategy**: Defines how objects snap/position (Free, Grid, Hex, etc.)
- **IPlacementValidator**: Validates placement legality
- **IPlacementVisualizer**: Handles visual feedback (colors, outlines, etc.)

### 2. PlacementController

The main orchestrator that:
- Builds a shared tool context
- Owns all placement tools and routes input/tick to the active tool
- Creates ghost objects for placement
- Manages selection state shared by tools
- Handles socket snapping (if available)

**Key Features:**
- Dependencies injected via inspector (MonoBehaviour references that implement interfaces)
- Runtime strategy switching with `SetPlacementStrategy()`
- Tool switching with `SetActiveTool()`
- Automatic ghost object creation with disabled physics
- Socket override for magnetic snapping

### 3. Tools (Tools/)

Tools are plain C# classes that implement `IPlacementTool`:

- **PlacementTool**: Ghost placement workflow (raycast, validate, confirm/cancel)
- **SelectionTool**: Selection input + shared selection state
- **MoveTool**: Move primary selection with pointer
- **RotateTool**: Rotate primary selection
- **DeleteTool**: Delete selection on input

### 4. Validation System (Validation/)

**ScriptableObject-driven rules** - no hardcoded validation!

#### PlacementRule (abstract base)
Override `CheckRule()` to create custom validation logic.

#### Included Rules:
- **ClearanceRule**: Uses `Physics.OverlapBox` to ensure no obstructions
- **RequireSurfaceRule**: Raycasts down to verify surface layer/angle

#### PlaceableObject Component
Attach to prefabs with a list of `PlacementRule` assets. All rules must pass for valid placement.

### 5. Socket System (Sockets/)

**Modular magnetic snapping:**

#### SocketType (ScriptableObject)
Acts as a type-safe tag (no enums = fully extensible).

#### Socket (MonoBehaviour)
Place in the scene to define snap points. Features:
- `IsOccupied` flag
- Gizmo visualization (position + orientation)
- Type checking via `SocketType`

#### SnapManager
Efficiently finds nearest unoccupied socket using:
- `Physics.OverlapSphere` for spatial queries
- Optional socket caching for performance
- Registration/unregistration for dynamic sockets

When a `PlaceableObject` with `RequiredSocketType` is near a matching socket, placement snaps exactly to the socket's transform.

### 6. Input Providers (Input/)

Two implementations provided:

#### LegacyInputProvider
Uses `UnityEngine.Input` (KeyCode-based configuration)

#### NewInputSystemProvider
Uses `InputActionReference` for New Input System (requires package)

Both implement `IInputProvider` - swap easily in inspector!

### 7. Placement Strategies (Strategies/)

Three implementations provided:

#### FreePositionStrategy
No snapping, places exactly where raycast hits.

#### GridPlacementStrategy
Snaps to regular 2D grid (configurable size, origin, rotation snap).
Includes grid visualization gizmo.

#### HexPlacementStrategy
Snaps to hexagonal grid (flat-top or pointy-top orientation).
Full hex coordinate math included.

**Extensibility:** Create new strategies by implementing `IPlacementStrategy` - no controller changes needed!

### 8. Visualizers (Visualization/)

#### StandardPlacementVisualizer
Changes ghost material colors (green = valid, red = invalid).
Creates transparent materials at runtime.

#### OutlinePlacementVisualizer
Advanced visualizer with outline effects and optional pulse animation.

## Setup Guide

### 1. Create a Placement Scene

1. Add a **PlacementController** to a GameObject
2. Assign dependencies in inspector:
   - Camera (defaults to Main Camera)
   - Placement Surface LayerMask
   - Selection Layer (for selection raycasts)
   - Object To Place (prefab reference)
3. Create and assign components:
   - Input Provider (LegacyInputProvider or NewInputSystemProvider)
   - Placement Strategy (GridPlacementStrategy, etc.)
   - Placement Validator (PlacementValidation)
   - Placement Visualizer (StandardPlacementVisualizer)
4. Optional: Add SnapManager for socket support

### 2. Create Validation Rules (ScriptableObjects)

Right-click in Project window:
- **Create → Placement System → Rules → Clearance Rule**
- **Create → Placement System → Rules → Require Surface Rule**

Configure in inspector:
- Clearance: Set box size, obstacle layer mask
- Surface: Set required surface layer, max angle

### 3. Setup Placeable Prefabs

On your prefab:
1. Add **PlaceableObject** component
2. Drag your rule assets into the Rules list
3. Optional: Set `RequiredSocketType` and `SnapRange` for socket snapping

### 4. Add Sockets (Optional)

For magnetic snapping:
1. Create **SocketType** asset: **Create → Placement System → Socket Type**
2. Add **Socket** component to GameObjects in scene
3. Assign SocketType to each socket
4. Add **SnapManager** to scene (reference in PlacementController)

### 5. Start Placement

Call `PlacementController.StartPlacement()` from UI or input:
```csharp
placementController.SetObjectToPlace(myPrefab);
placementController.StartPlacement();
```

## Usage Examples

### Switching Placement Modes at Runtime

```csharp
// Switch to grid mode
var gridStrategy = gameObject.AddComponent<GridPlacementStrategy>();
placementController.SetPlacementStrategy(gridStrategy);

// Switch to free mode
var freeStrategy = gameObject.AddComponent<FreePositionStrategy>();
placementController.SetPlacementStrategy(freeStrategy);
```

### Switching Tools at Runtime

```csharp
placementController.SetActiveTool(PlacementController.PlacementToolType.Selection);
placementController.SetActiveTool(PlacementController.PlacementToolType.Move);
placementController.SetActiveTool(PlacementController.PlacementToolType.Rotate);
placementController.SetActiveTool(PlacementController.PlacementToolType.Delete);
```

### Creating Custom Rules

```csharp
[CreateAssetMenu(fileName = "CustomRule", menuName = "Placement System/Rules/Custom Rule")]
public class CustomRule : PlacementRule
{
    public override bool CheckRule(Vector3 position, Quaternion rotation, GameObject ghostObject)
    {
        // Your validation logic here
        return true;
    }
}
```

### Dynamic Rule Assignment

```csharp
var placeableObject = prefab.GetComponent<PlaceableObject>();
placeableObject.AddRule(myCustomRule);
```

## Extension Points

The system is designed to be extended without modifying core code:

1. **New Input Systems**: Implement `IInputProvider`
2. **New Placement Modes**: Implement `IPlacementStrategy`
3. **New Validation Rules**: Extend `PlacementRule` ScriptableObject
4. **New Visualizers**: Implement `IPlacementVisualizer`
5. **New Socket Types**: Create `SocketType` assets

## Performance Considerations

- Ghost objects have physics disabled (kinematic rigidbodies, trigger colliders)
- SnapManager uses spatial queries (`OverlapSphere`) for efficient socket detection
- Optional socket caching for large scenes
- Max socket checks per frame limit (configurable)

## Best Practices

1. **Layer Management**: Use dedicated layers for:
   - Placement surfaces
   - Placed objects (for clearance checks)
   - Sockets
   
2. **Rule Composition**: Mix and match rules per object type:
   - Furniture: Surface + Clearance
   - Wall decorations: Surface + custom height check
   - Floor tiles: Surface only

3. **Socket Organization**: Group sockets under parent objects:
   - Wall → Wall Sockets
   - Ceiling → Ceiling Sockets
   - Makes management easier

4. **Prefab Setup**: Always test prefabs with PlaceableObject before building
   - Verify collider sizes match visual bounds
   - Test rules in isolation first

## Dependencies

- Unity 2020.3+ (tested on Unity 6000.2.6f2)
- Optional: New Input System package (for NewInputSystemProvider)

## Architecture Diagram

```
PlacementController (Orchestrator)
    ├── IInputProvider (Abstraction)
    │   ├── LegacyInputProvider
    │   └── NewInputSystemProvider
    ├── IPlacementStrategy (Strategy Pattern)
    │   ├── FreePositionStrategy
    │   ├── GridPlacementStrategy
    │   └── HexPlacementStrategy
    ├── IPlacementValidator (Validation)
    │   └── PlacementValidation
    │       └── PlaceableObject
    │           └── PlacementRule[] (ScriptableObjects)
    ├── IPlacementVisualizer (Feedback)
    │   ├── StandardPlacementVisualizer
    │   └── OutlinePlacementVisualizer
    └── SnapManager (Optional Snapping)
        └── Socket[]
            └── SocketType (ScriptableObject)
```

## License

This system is designed for the Ero Director project.
