# Placement Tools Design

**Goal**
Refactor PlacementSystem to a runtime tools pattern for placement, selection, movement, rotation, and deletion while removing SelectionManager and retaining Selectable.

**Non-goals**
- Replacing or redesigning SceneSandbox selection systems outside PlacementSystem.
- Adding new UI or input actions beyond the current input provider contract.

## Approaches Considered
1. **Tool-driven controller (recommended)**
   - PlacementController owns tool instances and delegates input/tick to the active tool.
   - Shared PlacementToolContext provides dependencies and selection state.
   - Pros: clear separation, easy extensibility, mirrors plan. Cons: controller refactor required.
2. **Keep SelectionManager + add tools**
   - Minimal change, only wrap placement/move/rotate/delete.
   - Pros: less refactor risk. Cons: conflicts with goal to remove SelectionManager.
3. **MonoBehaviour tools per mode**
   - Tools as components attached at runtime.
   - Pros: inspector driven. Cons: heavier lifecycle, more coupling than plain classes.

## Architecture
- **PlacementController** owns tools and routes `HandleInput` + `Tick` per frame to active `IPlacementTool`.
- **IPlacementTool** lifecycle: `OnEnter`, `OnExit`, `HandleInput`, `Tick`, `HandleSelection`.
- **PlacementToolContext** exposes dependencies: input, camera, layers, strategy, validator, visualizer, snap manager, rotation settings, and **PlacementSelectionState**.
- **Selection state** lives in `PlacementSelectionState` and is shared across tools.

## Components and Responsibilities
- **PlacementTool**
  - Ghost lifecycle, placement raycast, validation, snapping, visual feedback, confirm/cancel.
- **SelectionTool**
  - Raycast from input pointer, detect `Selectable`, manage primary selection and multi-select in `PlacementSelectionState`.
- **MoveTool**
  - Moves primary selection using raycast to movement surface; applies snapping + strategy; updates visual state.
- **RotateTool**
  - Applies rotation input to primary selection using rotation increment.
- **DeleteTool**
  - Deletes selected objects on delete action and clears selection state.

## Data Flow
- Input provider -> active tool `HandleInput()`.
- Tool uses `PlacementToolContext` to query input, camera, and dependencies.
- Tool mutates `PlacementSelectionState` only, not controller fields.
- PlacementController only coordinates tool switching and context creation.

## Error Handling and Edge Cases
- Tools early-out when dependencies or selection are missing.
- SelectionTool ignores non-selectable objects or `Selectable.IsSelectable == false`.
- DeleteTool clears selection even if some objects are null/destroyed.

## Testing and Verification
- Add minimal EditMode tests per tool class (constructible and compiles).
- Manual verification in editor for placement, selection, move, rotate, delete, and snapping.

## Documentation Updates
- Update PlacementSystem README to describe tools pattern and removal of SelectionManager.
- Update example usage to show tool switching (if example exists and still relevant).
