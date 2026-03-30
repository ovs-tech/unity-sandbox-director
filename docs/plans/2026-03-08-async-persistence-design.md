# Async Persistence for MiniTimeline and GamePersistenceManager

## Context
MiniTimeline currently uses synchronous save and load calls through GamePersistenceManager. When projects grow, synchronous file IO and serialization can stall the main thread and cause frame hitches, especially on mobile.

## Goals
- Add Task-based async save and load to the persistence stack for external subsystem files.
- Provide coroutine wrappers for Unity call sites, starting with MiniTimelineDirector.
- Keep existing synchronous APIs and behavior intact for compatibility.

## Non-Goals
- Changing serialization format, schema versioning, or migration logic.
- Reworking editor UI beyond wiring to new async entry points.
- Expanding async coverage to every persistence operation unless needed for MiniTimeline.

## Decision Summary
Use a hybrid approach (Option C): Task-based async core plus coroutine wrappers for Unity call sites.

## Proposed API Surface
- GamePersistenceManager
  - SaveFileAsync(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, bool overwrite = true, CancellationToken token = default)
  - LoadFileAsync<T>(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, CancellationToken token = default)
- BaseDataService
  - SaveAsync<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true, CancellationToken token = default)
  - LoadAsync<T>(string saveName, string ns, string fileName = null, CancellationToken token = default)
- FileDataService
  - Use File.ReadAllTextAsync and File.WriteAllTextAsync when available.
  - Fallback to Task.Run wrapping the existing sync methods if async IO is not supported.
- MiniTimelineDirector
  - IEnumerator SaveProjectAsync(string projectName, Action<bool> onDone = null)
  - IEnumerator LoadProjectAsync(string projectName, Action<bool> onDone = null)

## Data Flow
Save:
- Main thread: call GetSaveData and validate project exists.
- Background task: serialize to JSON and write file via data service.
- Main thread: return success and fire any completion callbacks.

Load:
- Background task: read file and deserialize to MiniTimelineProject.
- Main thread: SetProject, rebuild tracks, and fire OnProjectLoaded.

## Threading and Safety
- Unity object access (SetProject, Bind, Debug.Log, events) stays on the main thread.
- Async methods return default or false on failure and do not mutate live state on background threads.

## Error Handling
- Async methods capture exceptions and return failure results.
- Coroutine wrappers log errors on the main thread to keep Unity console usage safe.

## Testing Plan (Implementation Phase)
- Async roundtrip: SaveProjectAsync then LoadProjectAsync, verify canonical fields.
- Error handling: load missing file returns false and does not change current project.
- Main thread safety: SetProject only runs from coroutine completion path.

## OpenSpec Alignment
Extend the existing change mini-timeline-persistence-integration with async requirements and tasks covering the APIs above.
