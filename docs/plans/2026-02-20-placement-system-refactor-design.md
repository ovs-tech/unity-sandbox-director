# Placement System Architecture Refactor Design

## Overview
The goal of this refactor is to make the `PlacementSystem` more extensible and testable by decoupling its core dependencies from Unity's `MonoBehaviour` lifecycle where appropriate, and leveraging a code-driven pipeline for tools.

## Planned Changes

### 1. Tool Pipeline Architecture (Code-Driven)
- Introduce a new `IToolPipeline` and `ToolManager` to hold a pipeline of active tools.
- Remove hardcoded tool logic (`_placementTool = new PlacementTool();`, etc.) from the `PlacementController`.
- `PlacementController` will become a pure orchestrator that simply forwards `HandleInput()` and `Tick()` to `ToolManager`.
- External systems can inject custom tools by calling `ToolManager.RegisterTool(new MyCustomTool())`.

### 2. Validation System
- Make `IPlacementValidator` independent of `MonoBehaviour`.
- Create a `DefaultPlacementValidator : IPlacementValidator` (a pure C# class) to handle `PlaceableObject` validation logic.
- Register it into the `PlacementController` via initialization instead of Inspector attachment.

### 3. Visualizer and Strategy Systems
- Convert `IPlacementVisualizer` and `IPlacementStrategy` implementations from `MonoBehaviour`s to `ScriptableObject`s.
- This allows reusable visual feedback configurations (e.g. `StandardPlacementVisualizer` colors) and strategy settings (e.g. `GridPlacementStrategy` grid size) as project assets.
- `PlacementController` will reference these ScriptableObject assets directly in the Inspector.

### 4. Unit Testing
- Add significant unit test coverage to the pure C# Tools (`MoveTool`, `PlacementTool`, `SelectionTool`, etc.) by mocking the `PlacementToolContext`.
- Add unit tests for the new `ToolManager` pipeline and `DefaultPlacementValidator`.

## Trade-offs and Considerations
- **Editor Tooling:** `ScriptableObject`s for Strategy and Visualizer mean settings cannot easily be tweaked per-GameObject in the hierarchy, but instead are shared across scenes by default.
- **Dependency Context:** The `PlacementToolContext` will need to support injected interfaces to keep mocking simple in tests.
