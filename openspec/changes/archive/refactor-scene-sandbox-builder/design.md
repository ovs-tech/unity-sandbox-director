# Design Document: SceneSandboxBuilder Refactoring

**Change ID:** `refactor-scene-sandbox-builder`  
**Last Updated:** 2025-11-30

---

## Architecture Overview

This document details the technical design for decomposing the monolithic `SceneSandboxBuilder` class into a modular, maintainable architecture.

---

## Component Breakdown

### 1. SceneSandboxBuilder (Orchestrator)

**Responsibility:** High-level coordination and component lifecycle management

**Retained Fields:**
```csharp
[SerializeField] private SceneObjectLibrary _objectLibrary;
[SerializeField] private Transform _sceneRoot;
[SerializeField] private Transform _stageArea;
[SerializeField] private MiniTimelineDirector _timelineDirector;

// Component references (auto-injected)
private SandboxInputManager _inputManager;
private PlacementSystem _placementSystem;
private SelectionManager _selectionManager;
private TransformController _transformController;
private SceneSerializer _sceneSerializer;
private PreviewController _previewController;
private GridManager _gridManager;
private CameraRaycaster _cameraRaycaster;
```

**Key Methods:**
- `Awake()` - Initialize and wire up components
- `SetMode(SandboxMode)` - Coordinate mode changes across systems
- Public API facades for backward compatibility

**Lines of Code:** ~300-400

---

### 2. SandboxInputManager

**Responsibility:** Centralize all input handling and dispatch to appropriate systems

**Extracted Fields:**
```csharp
[Header("Input Action References")]
[SerializeField] private InputActionReference _toggleModeActionRef;
[SerializeField] private InputActionReference _exitAllModesActionRef;
[SerializeField] private InputActionReference _moveHotkeyActionRef;
[SerializeField] private InputActionReference _rotateHotkeyActionRef;
[SerializeField] private InputActionReference _scaleHotkeyActionRef;
[SerializeField] private InputActionReference _transformModeIncreaseActionRef;
[SerializeField] private InputActionReference _transformModeDecreaseActionRef;
[SerializeField] private InputActionReference _transformModeToggleAxisActionRef;
[SerializeField] private InputActionReference _pointerPositionActionRef;
[SerializeField] private InputActionReference _leftClickActionRef;
[SerializeField] private InputActionReference _rightClickActionRef;
[SerializeField] private InputActionReference _mouseScrollActionRef;
[SerializeField] private InputActionReference _cancelPlacementActionRef;

[Header("Input Settings")]
[SerializeField] private float _dragThreshold = 5f;
[SerializeField] private float _doubleClickTime = 0.3f;
[SerializeField] private bool _enableHotkeys = true;
```

**Events:**
```csharp
public event Action<Vector2> OnPointerDown;
public event Action<Vector2> OnPointerUp;
public event Action<Vector2> OnPointerMove;
public event Action<Vector2, int> OnPointerClick; // position, clickCount
public event Action<float> OnScrollDelta;
public event Action OnCancelAction;
public event Action<TransformModeType> OnTransformModeHotkey;
public event Action OnToggleTransformAxis;
```

**Key Features:**
- Event-driven architecture (no Update() polling in other systems)
- Mode-aware input filtering (disable build inputs in Play Mode)
- Gesture detection (drag, double-click, right-click)
- Hotkey management with enable/disable toggle

**Lines of Code:** ~400-450

---

### 3. PlacementSystem

**Responsibility:** Handle object placement workflow from start to confirmation

**Extracted Fields:**
```csharp
[Header("Placement Settings")]
[SerializeField] private LayerMask _placementLayers = -1;
[SerializeField] private float _defaultPlacementHeight = 0f;
[SerializeField] private float _minimumPlacementHeight = 0f;
[SerializeField] private bool _useRaycastForPlacement = true;
[SerializeField] private bool _useConsistentHeight = false;

[Header("Placement Validation")]
[SerializeField] private bool _checkCollisions = true;
[SerializeField] private LayerMask _collisionLayers = -1;
[SerializeField] private LayerMask _groundLayers = 0;
[SerializeField] private bool _ignoreStaticObjects = true;
[SerializeField] private bool _requireSurfaceBelow = false;
[SerializeField] private bool _enableCostSystem = false;
[SerializeField] private int _placementCost = 10;

[Header("Drop Indicator")]
[SerializeField] private bool _enableDropIndicator = true;
[SerializeField] private GameObject _dropIndicatorPrefab;
[SerializeField] private Color _validDropColor = Color.green;
[SerializeField] private Color _invalidDropColor = Color.red;
[SerializeField] private float _dropIndicatorSize = 1f;
```

**State:**
```csharp
private PlacementState _state = PlacementState.Idle;
private GameObject _currentPlacementObject;
private string _currentPlacementObjectId;
private Vector3 _currentPlacementPosition;
private Quaternion _currentPlacementRotation;
private Vector3 _currentPlacementScale;
private bool _isCurrentPlacementValid;
private GameObject _dropIndicator;
```

**Events:**
```csharp
public event Action<GameObject> OnPlacementStarted;
public event Action<Vector3, bool> OnPlacementUpdated; // position, isValid
public event Action<GameObject> OnPlacementConfirmed;
public event Action OnPlacementCancelled;
```

**Key Methods:**
```csharp
public void StartPlacement(string objectDataId, Vector2 screenPosition);
public void UpdatePlacement(Vector2 screenPosition);
public GameObject ConfirmPlacement();
public void CancelPlacement();
private bool ValidatePlacement(Vector3 position, GameObject obj);
private void UpdateDropIndicator(Vector3 position, bool isValid);
```

**Dependencies:**
- `CameraRaycaster` (for position calculation)
- `GridManager` (for snap-to-grid)
- `SceneObjectLibrary` (for prefab lookup)

**Lines of Code:** ~450-500

---

### 4. SelectionManager

**Responsibility:** Manage object selection, multi-select, and selection state

**Extracted Fields:**
```csharp
[Header("Selection Settings")]
[SerializeField] private LayerMask _selectionLayers = -1;
[SerializeField] private bool _autoEditOnSelect = true;
```

**State:**
```csharp
private readonly List<TransformableItem> _selectedItems = new List<TransformableItem>();
private TransformableItem _lastSelectedItem;
private GameObject _currentHoverItem;
```

**Events:**
```csharp
public event Action<GameObject> OnObjectSelected;
public event Action<GameObject> OnObjectDeselected;
public event Action<List<GameObject>> OnSelectionChanged;
public event Action<GameObject> OnObjectHoverEnter;
public event Action<GameObject> OnObjectHoverExit;
```

**Key Methods:**
```csharp
public void SelectObject(GameObject obj, bool additive = false);
public void DeselectObject(GameObject obj);
public void ClearSelection();
public void SelectMultiple(List<GameObject> objects);
public List<GameObject> GetSelectedObjects();
public GameObject GetPrimarySelection();
```

**Lines of Code:** ~300-350

---

### 5. TransformController

**Responsibility:** Handle transform modes (Position/Rotation/Scale) and axis constraints

**Extracted Fields:**
```csharp
[Header("Transform Mode Settings")]
[SerializeField] private float _rotationSensitivity = 1.0f;
[SerializeField] private float _scaleSensitivity = 0.01f;
[SerializeField] private float _scrollScaleSensitivity = 0.1f;
[SerializeField] private Vector3 _minScale = new Vector3(0.1f, 0.1f, 0.1f);
[SerializeField] private Vector3 _maxScale = new Vector3(10f, 10f, 10f);

[Header("Transform Mode Colors")]
[SerializeField] private Color _positionModeColor = new Color(0f, 1f, 1f, 0.5f);
[SerializeField] private Color _rotationModeColor = new Color(1f, 1f, 0f, 0.5f);
[SerializeField] private Color _scaleModeColor = new Color(1f, 0f, 1f, 0.5f);
```

**State:**
```csharp
private TransformModeType _currentMode = TransformModeType.Position;
private TransformAxis _currentAxis = TransformAxis.All;
private List<TransformableItem> _activeTransformItems = new List<TransformableItem>();
```

**Events:**
```csharp
public event Action<TransformModeType> OnTransformModeChanged;
public event Action<TransformAxis> OnTransformAxisChanged;
```

**Key Methods:**
```csharp
public void SetTransformMode(TransformModeType mode);
public void ToggleTransformAxis();
public void SetTransformAxis(TransformAxis axis);
public void ApplyTransformDelta(Vector2 dragDelta, List<GameObject> targets);
public void ApplyScrollTransform(float scrollDelta, List<GameObject> targets);
```

**Dependencies:**
- `SelectionManager` (get active selection)
- `GridManager` (for position snapping)

**Lines of Code:** ~350-400

---

### 6. GridManager

**Responsibility:** Grid snapping, bounds management, and spatial calculations

**Extracted Fields:**
```csharp
[Header("Grid Settings")]
[SerializeField] private bool _snapToGrid = true;
[SerializeField] private float _gridSize = 1f;
[SerializeField] private Vector3 _gridOffset = Vector3.zero;
[SerializeField] private PivotPoint _gridPivotOffset = PivotPoint.Center;

[Header("Scene Bounds")]
[SerializeField] private Vector3 _sceneBounds = new Vector3(20f, 10f, 20f);
[SerializeField] private Vector3 _sceneBoundsOffset = Vector3.zero;
[SerializeField] private PivotPoint _sceneBoundsPivot = PivotPoint.Center;

[Header("Gizmo Settings")]
[SerializeField] private bool _enableSceneGizmos = true;
[SerializeField] private bool _showSceneGrid = true;
[SerializeField] private bool _showSceneBounds = true;
[SerializeField] private bool _showStageAreaGizmo = true;
[SerializeField] private bool _showPlacementHeightGizmo = true;
[SerializeField] private Color _sceneBoundsColor = Color.cyan;
[SerializeField] private Color _placementHeightColor = Color.yellow;
```

**Key Methods:**
```csharp
public Vector3 SnapToGrid(Vector3 position);
public bool IsWithinBounds(Vector3 position);
public GridInfo GetGlobalGridInfo();
public Vector3 GetBoundsCenter();
public Bounds GetSceneBounds();
public void OnDrawGizmos(); // Draw grid and bounds
```

**Lines of Code:** ~250-300

---

### 7. CameraRaycaster

**Responsibility:** Perform raycasts and spatial queries

**Extracted Fields:**
```csharp
[Header("Raycast Settings")]
[SerializeField] private Camera _sceneCamera;
[SerializeField] private float _maxRaycastDistance = 1000f;
```

**Key Methods:**
```csharp
public bool RaycastFromScreen(Vector2 screenPos, out RaycastHit hit, LayerMask layers);
public bool RaycastFromScreenFiltered(Vector2 screenPos, out RaycastHit hit, LayerMask layers, GameObject[] ignoreObjects);
public GameObject GetObjectUnderPointer(Vector2 screenPos, LayerMask layers);
public Vector3 GetWorldPositionOnPlane(Vector2 screenPos, float planeHeight);
public bool IsPointerOverUI();
```

**Lines of Code:** ~200-250

---

### 8. SceneSerializer

**Responsibility:** Scene and project save/load operations

**Extracted Fields:**
```csharp
[Header("Save/Load Settings")]
[SerializeField] private string _defaultSavePath = "";
[SerializeField] private string _defaultProjectSavePath = "";
[SerializeField] private string _currentSceneName = "Untitled Scene";
[SerializeField] private bool _autoLoadFirstProject = false;
```

**State:**
```csharp
private SceneConfiguration _currentScene;
private SandboxProjectData _currentProject;
private Dictionary<string, GameObject> _placedObjects;
```

**Events:**
```csharp
public event Action<SceneConfiguration> OnSceneLoaded;
public event Action<SceneConfiguration> OnSceneSaved;
public event Action OnSceneCleared;
```

**Key Methods:**
```csharp
public void SaveScene(string sceneName);
public void LoadScene(string sceneName);
public void SaveProject(string projectName);
public void LoadProject(string projectName);
public void ClearScene();
public SceneConfiguration GetCurrentScene();
public SandboxProjectData GetCurrentProject();
```

**Lines of Code:** ~400-450

---

### 9. PreviewController

**Responsibility:** Timeline preview and playback management

**Extracted Fields:**
```csharp
[Header("Preview Settings")]
[SerializeField] private bool _autoPreview = false;
[SerializeField] private float _previewDuration = 10f;
```

**State:**
```csharp
private List<GameObject> _previewObjects;
private bool _isInPreviewMode;
```

**Events:**
```csharp
public event Action<bool> OnPreviewStateChanged;
```

**Key Methods:**
```csharp
public void StartPreview();
public void StopPreview();
public bool IsInPreviewMode();
```

**Dependencies:**
- `MiniTimelineDirector` (for playback control)

**Lines of Code:** ~150-200

---

## Communication Patterns

### Event Flow Example: Object Placement

1. **User drags from palette**
   - `ObjectPaletteItem` → `SceneSandboxBuilder.StartPlacement()`
   - `SceneSandboxBuilder` → `PlacementSystem.StartPlacement()`

2. **Placement system emits event**
   - `PlacementSystem.OnPlacementStarted` → Subscribers notified

3. **User moves pointer**
   - `SandboxInputManager.OnPointerMove` → `PlacementSystem.UpdatePlacement()`
   - `PlacementSystem` → `CameraRaycaster.RaycastFromScreen()`
   - `PlacementSystem` → `GridManager.SnapToGrid()`
   - `PlacementSystem.OnPlacementUpdated` → UI updates indicator

4. **User confirms placement**
   - `SandboxInputManager.OnPointerUp` → `PlacementSystem.ConfirmPlacement()`
   - `PlacementSystem.OnPlacementConfirmed` → `SelectionManager.SelectObject()`
   - `PlacementSystem.OnPlacementConfirmed` → `SceneSerializer` adds to scene data

### Mode Change Flow

1. **User presses toggle mode key**
   - `SandboxInputManager` detects input → `SceneSandboxBuilder.ToggleMode()`
   - `SceneSandboxBuilder.SetMode(SandboxMode.Play)`

2. **Orchestrator coordinates shutdown**
   - `SceneSandboxBuilder` → `PlacementSystem.CancelPlacement()` if active
   - `SceneSandboxBuilder` → `SelectionManager.ClearSelection()`
   - `SceneSandboxBuilder` → `SandboxInputManager.DisableBuildInputs()`

3. **Mode event emitted**
   - `SceneSandboxBuilder.OnModeChanged` → UI updates, systems adjust

---

## Component Initialization

### Dependency Graph
```
SceneSandboxBuilder (root)
├── SandboxInputManager (no dependencies)
├── CameraRaycaster (Camera reference)
├── GridManager (StageArea reference)
├── PreviewController (MiniTimelineDirector reference)
├── SelectionManager (depends: CameraRaycaster)
├── TransformController (depends: SelectionManager, GridManager)
├── PlacementSystem (depends: CameraRaycaster, GridManager, SceneObjectLibrary)
└── SceneSerializer (depends: PlacementSystem for placed object tracking)
```

### Initialization Order (in Awake)
1. Create/GetComponent for all managers
2. Initialize CameraRaycaster (requires Camera)
3. Initialize GridManager (requires StageArea)
4. Initialize SandboxInputManager
5. Initialize SelectionManager (requires CameraRaycaster)
6. Initialize TransformController (requires SelectionManager, GridManager)
7. Initialize PlacementSystem (requires CameraRaycaster, GridManager)
8. Initialize SceneSerializer
9. Initialize PreviewController
10. Wire up cross-component events
11. Initialize default state

---

## Performance Considerations

### Update() Optimization

**Before:**
- Single massive `Update()` with all input polling and logic

**After:**
- `SandboxInputManager.Update()` - Input event detection only
- `PlacementSystem.Update()` - Only when `_state == Active`
- No Update() in SelectionManager, TransformController, etc. (event-driven)

### Memory Management

**Object Pooling:**
- Drop indicators (reuse instead of Instantiate/Destroy)
- Raycast hit results (cache and reuse arrays)

**GC Allocation Reduction:**
- Use `List.Clear()` instead of creating new lists
- Cache frequently used GetComponent calls
- Avoid LINQ in hot paths

### Raycast Optimization
- Batch raycasts when possible
- Use layer masks to filter early
- Cache raycast results for same frame queries
- Skip raycasts when pointer over UI

---

## Testing Strategy

### Unit Tests (per component)

**Example: PlacementSystem Tests**
```csharp
[Test] public void StartPlacement_CreatesObject()
[Test] public void UpdatePlacement_SnapsToGrid_WhenEnabled()
[Test] public void ConfirmPlacement_ReturnsNull_WhenInvalid()
[Test] public void CancelPlacement_DestroysObject()
[Test] public void ValidatePlacement_ReturnsFalse_WhenColliding()
```

**Example: SelectionManager Tests**
```csharp
[Test] public void SelectObject_AddsToSelection()
[Test] public void SelectObject_ClearsExisting_WhenNotAdditive()
[Test] public void DeselectObject_RemovesFromSelection()
[Test] public void ClearSelection_EmptiesSelectionList()
```

### Integration Tests
- Full placement workflow (start → update → confirm)
- Mode switching with active operations
- Multi-object selection and transform
- Scene save/load with placed objects

### Performance Tests
- Measure Update() overhead (target: < 0.1ms per frame)
- Profile GC allocations (target: 0 per frame in steady state)
- Raycast frequency (target: max 1 per input event)
- Memory footprint comparison (before/after refactoring)

---

## Migration & Backward Compatibility

### Serialization Compatibility

**Strategy: FormerlySerializedAs Attribute**
```csharp
// In extracted component
[FormerlySerializedAs("_snapToGrid")] // Original field name in SceneSandboxBuilder
[SerializeField] private bool _snapToGrid = true;
```

Unity will automatically migrate serialized values when loading old scenes.

### Public API Preservation

**Facade Methods in SceneSandboxBuilder:**
```csharp
// Original public method - preserved
public void PlaceObject(string objectDataId, Vector3 position, bool autoSelect = true)
{
    // Delegate to PlacementSystem
    _placementSystem.PlaceObjectAt(objectDataId, position);
    if (autoSelect)
        _selectionManager.SelectLastPlaced();
}
```

### Event Migration

**Deprecated Events (marked with Obsolete):**
```csharp
[Obsolete("Use PlacementSystem.OnPlacementConfirmed instead")]
public GameObjectEvent OnObjectPlaced => _placementSystem.OnPlacementConfirmed;
```

---

## File Structure

```
Assets/Scripts/SceneSandbox/Core/
├── SceneSandboxBuilder.cs (orchestrator, ~400 lines)
├── SandboxInputManager.cs (new, ~450 lines)
├── PlacementSystem.cs (new, ~500 lines)
├── SelectionManager.cs (new, ~350 lines)
├── TransformController.cs (new, ~400 lines)
├── GridManager.cs (new, ~300 lines)
├── CameraRaycaster.cs (new, ~250 lines)
├── SceneSerializer.cs (new, ~450 lines)
├── PreviewController.cs (new, ~200 lines)
├── TransformableItem.cs (existing, unchanged)
├── DropZone.cs (existing, unchanged)
└── [Enums].cs (existing, unchanged)
```

**Total Line Count:** ~3,300 lines (vs 4,169 original)
- Reduction from removed deprecated code and cleaner organization
- More maintainable with focused responsibilities

---

## Open Questions

1. **Dependency Injection Framework**: Use Zenject/VContainer or manual property injection?
   - **Decision:** Manual property injection for now (simpler, no external dependency)

2. **ScriptableObject Configs**: Should settings be moved to ScriptableObjects?
   - **Decision:** Keep SerializeField for now (preserve Inspector workflow), consider SO in future iteration

3. **Command Pattern**: Should all actions be converted to commands for undo/redo?
   - **Decision:** Out of scope for this refactoring; address in separate change proposal

4. **Input System Asset**: Create separate InputActions asset for SandboxInputManager?
   - **Decision:** Continue using InputActionReferences (consistent with current approach)

---

## References

- Project Architecture: `openspec/project.md`
- Original Implementation: `Assets/Scripts/SceneSandbox/Core/SceneSandboxBuilder.cs`
- Unity Component Best Practices: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
