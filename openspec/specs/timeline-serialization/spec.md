# timeline-serialization Specification

## Purpose
TBD - created by archiving change refactor-timeline-serialization-odin. Update Purpose after archive.
## Requirements
### Requirement: Odin-Based Direct Serialization
The MiniTimeline system SHALL use Odin Serializer to directly serialize runtime track and clip instances without intermediate data transfer objects.

**Rationale**: Eliminates code duplication, reduces maintenance burden, and enables support for complex Unity types.

#### Scenario: Save Timeline Project
**Given** a timeline project with multiple tracks of various types (AnimTrack, MorphTrack, MovementTrack)  
**And** each track contains multiple clips with type-specific properties  
**When** the user saves the project  
**Then** the system shall serialize all tracks and clips directly using Odin Serializer  
**And** the serialized data shall preserve all track and clip properties without data loss  
**And** polymorphic types (IMiniTrack, IMiniClip) shall be serialized with type information

#### Scenario: Load Timeline Project
**Given** a saved timeline project file in Odin JSON format  
**When** the user loads the project  
**Then** the system shall deserialize tracks and clips directly to their runtime instances  
**And** all polymorphic interfaces shall resolve to correct concrete types  
**And** all track and clip properties shall be restored accurately  
**And** no manual conversion or factory methods shall be required

#### Scenario: Round-Trip Preservation
**Given** a timeline project with all track types and complex properties  
**When** the project is saved and then immediately loaded  
**Then** the loaded project shall be identical to the original  
**And** all track orders shall be preserved  
**And** all clip timings and properties shall be exact matches  
**And** no precision loss shall occur for floating-point values

---

### Requirement: Unified Serialization API
The system SHALL provide a single unified serialization API via `ProjectSerializer` that handles save/load using Odin and eliminates the need for `TrackFactory`.

**Rationale**: Simplifies architecture by consolidating responsibilities and removing the conversion layer.

#### Scenario: Single Entry Point
**Given** a timeline project  
**When** the user saves or loads a project  
**Then** operations shall be performed exclusively through `ProjectSerializer.Save()` and `ProjectSerializer.Load()`  
**And** no factory methods shall be invoked  
**And** runtime track instances shall be persisted and restored directly

#### Scenario: No Legacy Format Support
**Given** a legacy JSON project file using DTOs (TrackData/ClipData)  
**When** the user attempts to load it  
**Then** the system shall fail gracefully with an informative error  
**And** it shall not attempt to auto-migrate the file  
**And** documentation shall instruct manual recreation using runtime classes

#### Scenario: Direct Runtime Usage
**Given** a project loaded via the unified serializer  
**When** tracks are accessed in `MiniTimelineDirector`  
**Then** they shall be available as runtime `IMiniTrack` instances directly from `project.tracks`  
**And** no conversion or mapping step shall be required

---

### Requirement: Serialization Attributes on Runtime Classes
All track and clip runtime classes SHALL have appropriate serialization attributes for Odin Serializer.

**Rationale**: Enables direct serialization without intermediate DTOs.

#### Scenario: Track Serialization Attributes
**Given** any track class (AnimTrack, MorphTrack, etc.)  
**Then** the class shall have `[Serializable]` attribute  
**And** all persistent fields shall have `[OdinSerialize]` attribute  
**And** transient runtime fields (Unity object references, state flags) shall have `[NonSerialized]` attribute  
**And** the clips list shall be serializable

#### Scenario: Clip Serialization Attributes
**Given** any clip class (AnimClip, MorphClip, etc.)  
**Then** the class shall have `[Serializable]` attribute  
**And** all data properties (timing, settings, asset references) shall have `[OdinSerialize]` attribute  
**And** no Dictionary payload fields shall be used (use strongly-typed fields instead)

#### Scenario: MiniTimelineProject Serialization
**Given** the MiniTimelineProject class  
**Then** it shall have `[Serializable]` attribute  
**And** the tracks field shall be `List<IMiniTrack>` with `[OdinSerialize]`  
**And** all metadata fields shall be serializable  
**And** the project shall serialize without requiring manual pre-processing

---

### Requirement: Removal of Intermediate Serialization Layer
The TrackData, ClipData classes and TrackFactory conversion methods SHALL be removed from the codebase (after migration period).

**Rationale**: Reduces code complexity and maintenance burden.

#### Scenario: No TrackData References
**Given** the codebase after refactoring is complete  
**When** searching for `TrackData` class usage  
**Then** it shall only appear in legacy migration code  
**And** it shall not be used in any save/load paths for new projects  
**And** it shall be marked as `[Obsolete]` during transition period

#### Scenario: No TrackFactory Conversions
**Given** the MiniTimelineDirector loading a project  
**When** tracks are initialized  
**Then** no calls to `TrackFactory.CreateTrack()` shall occur  
**And** no calls to `TrackFactory.ConvertRuntimeTrackToData()` shall occur  
**And** tracks shall be used directly from `project.tracks` list

#### Scenario: Simplified Code Metrics
**Given** the serialization layer before and after refactoring  
**When** comparing lines of code  
**Then** the serialization codebase shall be reduced by approximately 800-1000 lines  
**And** the TrackFactory file shall be reduced to only legacy support (~200 lines)  
**And** maintenance burden shall be measurably reduced

---

### Requirement: Performance Equal or Better
The Odin-based serialization SHALL have equal or better performance compared to the legacy JSON serialization.

**Rationale**: Refactoring should not degrade user experience.

#### Scenario: Load Performance
**Given** a medium-sized timeline project (10 tracks, 50 clips)  
**When** the project is loaded using Odin deserialization  
**Then** the load time shall be no more than 50ms  
**And** the load time shall be equal to or faster than legacy load time (~50-100ms)  
**And** memory allocation shall be comparable or lower

#### Scenario: Save Performance
**Given** a medium-sized timeline project  
**When** the project is saved using Odin serialization  
**Then** the save time shall be no more than 50ms  
**And** the save time shall be equal to or faster than legacy save time  
**And** the resulting file size shall be comparable (within 20%)

#### Scenario: Large Project Handling
**Given** a large timeline project (50 tracks, 500 clips)  
**When** the project is saved and loaded  
**Then** operations shall complete without noticeable lag (<500ms)  
**And** no memory pressure shall be observed  
**And** the Unity Editor shall remain responsive

---

### Requirement: Error Handling and Robustness
The serialization system SHALL handle errors gracefully and provide helpful error messages.

**Rationale**: Improves developer experience and debugging.

#### Scenario: Serialization Failure
**Given** a project that fails to serialize (e.g., due to null reference)  
**When** save is attempted  
**Then** the system shall log a descriptive error message  
**And** the error message shall include the track/clip that caused the failure  
**And** the save operation shall not corrupt the existing project file  
**And** the user shall be notified via Unity Console

#### Scenario: Deserialization Failure
**Given** a corrupted or invalid project file  
**When** load is attempted  
**Then** the system shall log a descriptive error message  
**And** the system shall attempt legacy format loading as fallback  
**And** the system shall not crash or throw unhandled exceptions  
**And** partial project data shall not be loaded (all-or-nothing)

#### Scenario: Version Mismatch Handling
**Given** a project saved with a newer version of the track class structure  
**When** loaded in an older version of the editor  
**Then** Odin's version tolerance shall allow loading with missing fields set to defaults  
**And** a warning shall be logged about version mismatch  
**And** core functionality shall remain operational

---

### Requirement: Integration with MiniTimelineDirector
The MiniTimelineDirector SHALL seamlessly integrate with the Odin-based serialization without requiring workflow changes.

**Rationale**: Ensures the refactoring is transparent to other systems.

#### Scenario: Load Project in Director
**Given** a MiniTimelineDirector instance  
**When** `LoadProject()` is called with a project name  
**Then** the project shall be loaded using Odin deserialization  
**And** tracks shall be immediately available in `director.Tracks` list  
**And** no additional conversion steps shall be required  
**And** track binding shall work as before

#### Scenario: Save Project from Director
**Given** a MiniTimelineDirector with an active project  
**And** the project has been modified (tracks added/removed)  
**When** `SaveProject()` is called  
**Then** the project shall be saved using Odin serialization  
**And** all runtime track state shall be preserved  
**And** the save shall complete without errors  
**And** the file shall be immediately loadable

#### Scenario: Track Management Operations
**Given** a loaded project in the director  
**When** tracks are added, removed, or modified via `AddTrack()`, `RemoveTrack()`, etc.  
**Then** changes shall be reflected in `project.tracks` list  
**And** no separate TrackData update shall be required  
**And** the project shall remain in a consistent state  
**And** save/load cycles shall preserve all changes

---

