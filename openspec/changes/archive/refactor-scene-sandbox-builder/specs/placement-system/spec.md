# Spec Delta: Placement System

**Capability:** Placement System  
**Change Type:** MODIFIED  
**Change ID:** refactor-scene-sandbox-builder

---

## MODIFIED Requirements

### Requirement: Object Placement Workflow
PlacementSystem SHALL manage the complete placement lifecycle (start, update, confirm, cancel). Placement state MUST transition through defined states (Idle, Active). Events SHALL be emitted at each lifecycle stage.
**ID:** placement-001  
**Previous State:** Placement logic embedded in SceneSandboxBuilder  
**New State:** Dedicated PlacementSystem component

#### Scenario: Starting placement from palette
**Given** user drags object from palette  
**When** StartPlacement() is called with objectDataId and screen position  
**Then** object prefab is instantiated at initial position  
**And** placement state transitions to Active  
**And** OnPlacementStarted event is emitted  
**And** drop indicator becomes visible if enabled

#### Scenario: Updating placement during drag
**Given** placement is active  
**When** UpdatePlacement() is called with new screen position  
**Then** object position is calculated via raycast or plane projection  
**And** position is snapped to grid if enabled  
**And** placement validity is checked (collisions, bounds)  
**And** OnPlacementUpdated event emits with position and validity  
**And** drop indicator updates color based on validity

#### Scenario: Confirming valid placement
**Given** placement is active and valid  
**When** ConfirmPlacement() is called  
**Then** object is finalized at current position  
**And** TransformableItem component is added  
**And** object is registered in scene configuration  
**And** OnPlacementConfirmed event is emitted  
**And** placement state transitions to Idle  
**And** placed object is returned

#### Scenario: Canceling placement
**Given** placement is active  
**When** CancelPlacement() is called or ESC is pressed  
**Then** placement object is destroyed  
**And** object is removed from scene configuration if registered  
**And** OnPlacementCancelled event is emitted  
**And** placement state transitions to Idle  
**And** drop indicator is hidden

---

### Requirement: Placement Validation
ValidatePlacement() SHALL check all enabled validation rules. Collision detection MUST respect layer masks and ignore settings. Bounds validation SHALL use configured scene bounds and pivot points.
**ID:** placement-002  
**Previous State:** Validation mixed with placement logic  
**New State:** Separate validation method with clear rules

#### Scenario: Collision detection validation
**Given** collision checking is enabled (_checkCollisions = true)  
**When** ValidatePlacement() is called  
**Then** OverlapBox check is performed at target position  
**And** collisions with _collisionLayers are detected  
**And** ground layers are excluded from collision check  
**And** static objects are ignored if _ignoreStaticObjects = true  
**And** returns false if any invalid collision exists

#### Scenario: Bounds validation
**Given** scene bounds are defined  
**When** ValidatePlacement() is called  
**Then** object bounds are checked against scene bounds  
**And** placement is invalid if any part extends beyond scene bounds  
**And** bounds check uses configured pivot point

#### Scenario: Surface requirement validation
**Given** _requireSurfaceBelow is enabled  
**When** ValidatePlacement() is called  
**Then** downward raycast is performed from object position  
**And** placement is invalid if no surface is found within threshold  
**And** surface normal is stored for alignment if needed

### Requirement: Drop Indicator System
Drop indicator SHALL provide visual feedback during placement. Indicator color MUST change based on placement validity (green for valid, red for invalid). Indicator SHALL be properly cleaned up when placement ends.
**ID:** placement-003  
**New Requirement**

#### Scenario: Drop indicator creation
**Given** drop indicator is enabled  
**When** placement starts  
**Then** drop indicator is instantiated from prefab (or created if null)  
**And** indicator is positioned at placement location  
**And** indicator size is set to _dropIndicatorSize  
**And** indicator remains active throughout placement

#### Scenario: Drop indicator visual feedback
**Given** drop indicator is active  
**When** placement validity changes  
**Then** indicator color changes to _validDropColor if valid  
**Or** indicator color changes to _invalidDropColor if invalid  
**And** color transition is smooth  
**And** indicator rotation aligns with surface normal if available

#### Scenario: Drop indicator cleanup
**Given** placement ends (confirmed or cancelled)  
**When** placement state transitions to Idle  
**Then** drop indicator is hidden or destroyed  
**And** indicator references are cleared  
**And** no orphaned indicators remain in scene

### Requirement: Grid Snapping Integration
Placement system SHALL integrate with GridManager for position snapping. Grid snapping MUST respect grid size, offset, and pivot point configuration. Snapping SHALL be toggleable via _snapToGrid setting.
**ID:** placement-004  
**New Requirement**

#### Scenario: Snap-to-grid during placement
**Given** grid snapping is enabled (_snapToGrid = true)  
**When** UpdatePlacement() calculates position  
**Then** position is passed to GridManager.SnapToGrid()  
**And** snapped position respects grid size and offset  
**And** snapped position accounts for object pivot point  
**And** object moves smoothly to snapped position

#### Scenario: Grid snapping disabled
**Given** grid snapping is disabled (_snapToGrid = false)  
**When** UpdatePlacement() calculates position  
**Then** raw raycast/plane position is used  
**And** no grid alignment is applied  
**And** object follows pointer precisely

---

### Requirement: Placement Performance
Placement operations MUST complete within 0.5ms per frame. Position SHALL be recalculated only once per frame. Raycasts MUST use layer masks for early filtering. Zero GC allocations SHALL occur during placement updates.
**ID:** placement-005  
**New Requirement**

#### Scenario: Efficient Update during placement
**Given** placement is active  
**When** Update() is called  
**Then** position is recalculated only once per frame  
**And** validation runs only when position changes  
**And** no GC allocations occur from placement logic  
**And** frame time remains < 0.5ms for placement operations

#### Scenario: Raycast optimization
**Given** multiple raycasts needed for placement  
**When** performing spatial queries  
**Then** raycasts use appropriate layer masks to filter early  
**And** raycast distance is limited to _maxRaycastDistance  
**And** results are cached within same frame if needed  
**And** redundant raycasts are avoided

---

## REMOVED Requirements

### Requirement: Ghost preview system (deprecated)
**ID:** placement-legacy-001  
**Reason:** Replaced by drop indicator system

**Previous behavior:** Created ghost prefab with modified materials  
**New behavior:** Use drop indicator for placement feedback  
**Migration:** Remove _enableGhostPreview, _validGhostColor, _invalidGhostColor fields
