# Spec Delta: Scene Builder Architecture

**Capability:** Scene Builder Architecture  
**Change Type:** MODIFIED  
**Change ID:** refactor-scene-sandbox-builder

---

## MODIFIED Requirements

### Requirement: Scene Builder Component Structure
The system SHALL decompose SceneSandboxBuilder into focused components, each with single responsibility. Components SHALL communicate through well-defined interfaces and events. The orchestrator SHALL maintain backward compatibility with existing public API.
**ID:** scene-builder-arch-001  
**Previous State:** Monolithic SceneSandboxBuilder class with all functionality  
**New State:** Modular architecture with separated responsibilities

#### Scenario: Component initialization and lifecycle
**Given** a scene with SceneSandboxBuilder component  
**When** the scene starts  
**Then** all subsystem components are initialized in correct dependency order  
**And** components are properly wired with event connections  
**And** no circular dependencies exist  
**And** initialization completes without errors

#### Scenario: Backward compatible public API
**Given** existing code using SceneSandboxBuilder public methods  
**When** the refactored system is loaded  
**Then** all public methods remain accessible with same signatures  
**And** method behavior is functionally equivalent  
**And** no compilation errors occur in dependent code

#### Scenario: Mode coordination across components
**Given** multiple components managing different aspects  
**When** mode is changed from Build to Play  
**Then** SceneSandboxBuilder orchestrates shutdown across all components  
**And** each component responds appropriately to mode change  
**And** state transitions are clean with no orphaned objects

---

### Requirement: Component Communication
Components SHALL communicate through event-driven architecture. Direct method calls between components MUST be avoided to prevent tight coupling. Data flow SHALL be unidirectional where possible.
**ID:** scene-builder-arch-002  
**New Requirement**

#### Scenario: Event-driven communication between components
**Given** components need to communicate state changes  
**When** a component's state changes (e.g., placement starts)  
**Then** relevant events are emitted  
**And** subscribed components receive notifications  
**And** no direct method calls create tight coupling  
**And** communication is asynchronous and decoupled

#### Scenario: Cross-component data flow
**Given** one component needs data from another  
**When** requesting data (e.g., PlacementSystem needs raycast)  
**Then** data is provided through defined interfaces  
**And** requesting component doesn't hold direct references  
**And** data flow is unidirectional where possible

### Requirement: Serialization Compatibility
The system SHALL maintain backward compatibility with existing serialized scenes. FormerlySerializedAs attributes MUST be used for all migrated fields. Scenes saved with old version SHALL load without errors or data loss in refactored version.
**ID:** scene-builder-arch-003  
**New Requirement**

#### Scenario: Migrating serialized fields to new components
**Given** a scene saved with old monolithic SceneSandboxBuilder  
**When** scene is loaded in refactored version  
**Then** Unity automatically migrates serialized values using FormerlySerializedAs  
**And** all settings are preserved in new component structure  
**And** no data loss occurs  
**And** scene loads without warnings or errors

#### Scenario: Inspector configuration preservation
**Given** user has configured SceneSandboxBuilder in Inspector  
**When** upgrading to refactored version  
**Then** all configured values remain accessible  
**And** Inspector layout shows new component structure  
**And** workflow remains intuitive for existing users

---

## REMOVED Requirements

### Requirement: Single-class responsibility (deprecated)
**ID:** scene-builder-arch-legacy-001  
**Reason:** Replaced by modular component architecture

**Previous behavior:** All sandbox functionality in single MonoBehaviour  
**Migration:** Functionality distributed across specialized components
