# Spec Delta: Input Management

**Capability:** Input Management  
**Change Type:** ADDED  
**Change ID:** refactor-scene-sandbox-builder

---

## ADDED Requirements

### Requirement: Centralized Input Handling
SandboxInputManager SHALL centralize all input action handling. Input actions MUST be enabled/disabled based on current sandbox mode. The system SHALL emit events for all input actions rather than exposing raw input state.
**ID:** input-mgmt-001  
**New Requirement**

#### Scenario: Input action initialization
**Given** SandboxInputManager component is initialized  
**When** component starts  
**Then** all InputActionReferences are resolved  
**And** input actions are enabled  
**And** callbacks are registered for each action  
**And** initial state is set correctly

#### Scenario: Mode-aware input filtering
**Given** sandbox is in Play Mode  
**When** build-related input actions are triggered  
**Then** those inputs are ignored  
**And** only mode-appropriate inputs are processed  
**And** mode switch input remains active in all modes

#### Scenario: Input event emission
**Given** input action is triggered (e.g., pointer down)  
**When** SandboxInputManager processes the input  
**Then** appropriate event is emitted with correct parameters  
**And** all subscribed components receive notification  
**And** processing occurs in single frame

---

### Requirement: Gesture Detection
The system SHALL detect common gestures (drag, double-click, right-click) from raw input. Gesture detection MUST use configurable thresholds and timing windows. Gesture events SHALL include relevant context (position, delta, click count).
**ID:** input-mgmt-002  
**New Requirement**

#### Scenario: Drag detection
**Given** pointer is pressed and moves beyond threshold  
**When** pointer movement exceeds _dragThreshold  
**Then** OnPointerDrag event is emitted  
**And** drag state is maintained until pointer up  
**And** drag delta is calculated correctly

#### Scenario: Double-click detection
**Given** pointer is clicked twice rapidly  
**When** second click occurs within _doubleClickTime  
**Then** OnPointerClick event emits with clickCount=2  
**And** click timer is reset  
**And** click count resets after timeout

#### Scenario: Right-click context menu
**Given** right mouse button is pressed  
**When** pointer up occurs without drag  
**Then** context menu event is triggered  
**And** screen position is provided for menu placement

### Requirement: Hotkey Management
Hotkeys SHALL be configurable and can be enabled/disabled at runtime. Transform mode hotkeys MUST emit events with appropriate mode type. Axis toggle hotkey SHALL only function in Rotation and Scale modes.
**ID:** input-mgmt-003  
**New Requirement**

#### Scenario: Transform mode hotkeys
**Given** hotkeys are enabled (_enableHotkeys = true)  
**When** transform mode hotkey is pressed (Q/W/E/R)  
**Then** OnTransformModeHotkey event emits with correct mode  
**And** system switches to requested transform mode  
**And** visual feedback updates accordingly

#### Scenario: Hotkey enable/disable toggle
**Given** hotkeys can be enabled or disabled  
**When** _enableHotkeys is set to false  
**Then** hotkey inputs are ignored  
**And** only pointer inputs remain active  
**And** existing operations continue normally

#### Scenario: Axis toggle hotkey
**Given** transform mode is Rotation or Scale  
**When** axis toggle hotkey is pressed (X key)  
**Then** OnToggleTransformAxis event is emitted  
**And** axis cycles through X → Y → Z → All  
**And** Position mode ignores axis toggle

### Requirement: Input Performance
Input processing MUST complete within 0.1ms per frame. The system SHALL produce zero GC allocations in steady state. Input buffering MUST be prevented to avoid stale data processing.
**ID:** input-mgmt-004  
**New Requirement**

#### Scenario: Minimal Update overhead
**Given** input system is running  
**When** Update() is called each frame  
**Then** processing time is < 0.1ms per frame  
**And** no GC allocations occur in steady state  
**And** only necessary callbacks are invoked

#### Scenario: Input buffering prevention
**Given** multiple pointer events in single frame  
**When** processing input queue  
**Then** only most recent input is processed  
**And** no stale input data affects current frame  
**And** event emission is debounced appropriately
