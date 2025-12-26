# MiniTimeline Module Overview

The `MiniTimeline` folder contains a lightweight, extensible timeline system built for Unity. It is broken into several submodules that separate runtime core logic, track implementations, serialization helpers, editor extensions, and UI bindings.

## Directory Structure

- `Core/` – Runtime foundations of the timeline system. Defines the data models and orchestration logic such as `MiniTimelineDirector`, `MiniTimelineProject`, and interfaces for tracks (`IMiniTrack`) and clips (`IMiniClip`). It also includes the command infrastructure (`ITimelineCommand`, `TimelineCommand`, `TimelineCommandManager`) and binding utilities used by the UI layer (`BindableObject`, `BindingContext`).
- `Tracks/` – Concrete runtime track and clip implementations. Out-of-the-box support includes animation (`AnimTrack`, `AnimClip`), animator-driven playback (`AnimatorTrack`, `AnimatorClip`), movement (`MovementTrack`, `MovementClip`), morph targets (`MorphTrack`, `MorphClips`), event signaling (`SignalTrack`, `SignalClip`), and utility tracks like `EventTrack`.
- `UI/` – Runtime UI for interacting with the mini timeline. Contains timeline editor widgets (`TimelineEditorUI`, `TimelineRuler`, `ClipUI`, `TrackUI`), reusable form controls (`FormFields`, `FormSubmitPanel`, `ClipFormDefinitions`, `TrackFormDefinitions`), UI helper utilities (`TrackUIHelper`), a snapping system (`TimelineSnapSystem`), and an interaction framework (`Input/` with tap, drag, and hold handlers). The `Commands/` subfolder wraps user actions (create clip, move clip, zoom, etc.) into command objects that integrate with the core command manager. Context menus are built via `TimelineContextMenu` and `ContextMenuBuilder`.
- `Serialization/` – Persistence helpers. `ProjectSerializer` uses Odin Serializer to directly save/load `MiniTimelineProject` data with polymorphic track and clip instances.
- `Editor/` – Unity Editor extensions for better authoring workflows, including utilities (`MiniTimelineEditorUtils`) and a custom property drawer for the serializable dictionary (`StringObjectDictionaryDrawer`).
- `Demo/` – Example MonoBehaviours (`MiniTimelineDemo`, `TimelineEditorDemo`) demonstrating how to drive the timeline director and UI in a sample scene.

Each subfolder contains corresponding `.meta` files required by Unity; these are generated automatically and typically should remain untouched unless assets are being moved or renamed within the Unity Editor.

## Extending the Timeline

1. **Create new tracks or clips** by implementing `IMiniTrack`/`IMiniClip` and adding the class to the `Tracks/` directory. Add `[Serializable]` and `[OdinSerialize]` attributes to enable persistence via Odin Serializer.
2. **Add UI tools** in `UI/Commands/` and `UI/Input/` to expose new interactions or editor affordances.
3. **Customize persistence** by extending `ProjectSerializer` or adding helper classes within the `Serialization/` folder.

## Serialization Architecture

The timeline uses **Odin Serializer** to directly serialize runtime track and clip instances:

- **No intermediate DTOs**: Runtime classes (`IMiniTrack`, `IMiniClip` implementations) are serialized directly
- **Polymorphic support**: Odin handles interface-to-concrete-type serialization automatically
- **JSON format**: Human-readable project files for version control and debugging
- **Attribute-based**: Use `[Serializable]` and `[OdinSerialize]` on track/clip classes and fields

### Adding New Track Types

1. Create your track class implementing `IMiniTrack` (e.g., extend `MiniTrackBase<TClip>`)
2. Add `[Serializable]` attribute to the class
3. Mark serializable fields with `[OdinSerialize]`
4. Mark transient fields with `[NonSerialized]`
5. Add the track type to `MiniTimelineConstants` if needed
6. Register in `AddTrackCommand.CreateRuntimeTrack()` for UI support

No factory registration or DTO conversion is required—Odin handles serialization automatically.

For additional context, inspect the demo scripts in `Demo/` to see how the director, UI, and serialization pieces work together at runtime.
