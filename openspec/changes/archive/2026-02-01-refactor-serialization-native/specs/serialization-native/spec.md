# Spec: Native JSON Serialization

## ADDED Requirements

### Requirement: Serialize Polymorphic Tracks
The system MUST serialize a heterogeneous list of `IMiniTrack` objects to a JSON format that preserves their concrete types.

#### Scenario: Save Project with Mixed Tracks
- **Given** a project containing an `AnimTrack`, a `SignalTrack`, and a `MorphTrack`
- **When** `ProjectSerializer.SaveToJson` is called
- **Then** the output JSON contains a `serializedTracks` array
- **And** each entry has the correct assembly-qualified `type` string
- **And** the `data` field contains the specific fields for that track type

### Requirement: Serialize Polymorphic Clips
The system MUST serialize a list of clips within a track, preserving their concrete types.

#### Scenario: Save Track with Mixed Clips
- **Given** a `MorphTrack` containing both `MorphKeyClip` and `MorphCurveClip`
- **When** the track is serialized
- **Then** the JSON output for the track contains a `serializedClips` array
- **And** each clip's specific data (e.g. `keys` vs `channels`) is preserved in the `data` payload

### Requirement: Self-Contained Serialization
The serialization logic MUST reside within the domain classes or standard Unity libraries, avoiding external dependencies like `OdinSerializer`.

#### Scenario: Verify Dependencies
- **Given** the `ProjectSerializer` class
- **When** the code is inspected
- **Then** there are no `using Sirenix.OdinSerializer;` directives
- **And** `SerializationUtility` is not used

### Requirement: Metadata Dictionary Serialization
The system MUST serialize the `editorData` dictionary in `ProjectMetadata`.

#### Scenario: Save Editor Data
- **Given** metadata with `editorData` containing string and numeric values
- **When** the project is serialized
- **Then** the dictionary is persisted (e.g. as keys/values lists)
- **And** is restored correctly upon deserialization