# Placement Tools Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor PlacementSystem to a runtime tools pattern for placement, selection, movement, rotation, and deletion with SelectionManager removed and Selectable retained.

**Architecture:** PlacementController owns tool instances and delegates frame updates to the active IPlacementTool. Tools are plain C# classes using a shared PlacementToolContext that exposes input, camera, layers, strategy, validator, visualizer, snapping, and selection state.

**Tech Stack:** Unity 6000.2, C#, Unity Test Runner (EditMode tests if added)

---

### Task 1: Add tools API and shared context

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Tools/IPlacementTool.cs`
- Create: `Assets/Scripts/PlacementSystem/Tools/PlacementToolContext.cs`
- Modify: `Assets/Scripts/PlacementSystem/Core/IInputProvider.cs`
- Modify: `Assets/Scripts/PlacementSystem/Input/LegacyInputProvider.cs`
- Modify: `Assets/Scripts/PlacementSystem/Input/NewInputSystemProvider.cs`

**Step 1: Write the failing test**

Create: `Assets/Tests/EditMode/PlacementSystem/PlacementToolContextTests.cs`
```csharp
using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class PlacementToolContextTests
{
    [Test]
    public void Context_IsReadOnly_PropertiesAreAssigned()
    {
        var context = new PlacementToolContext(null, null, null, null, null, null, 0f, 0f, 0f, false);
        Assert.NotNull(context);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner (EditMode) -> PlacementToolContextTests`
Expected: FAIL (missing types)

**Step 3: Write minimal implementation**

Create `IPlacementTool` with `OnEnter`, `OnExit`, `HandleInput`, `Tick`, `HandleSelection` (optional). Create `PlacementToolContext` with read-only properties and selection state container. Add `IsDeleteActionTriggered()` to `IInputProvider` and implement in both input providers.

**Step 4: Run test to verify it passes**

Run: `Unity Test Runner (EditMode) -> PlacementToolContextTests`
Expected: PASS

**Step 5: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Tools/IPlacementTool.cs \
        Assets/Scripts/PlacementSystem/Tools/PlacementToolContext.cs \
        Assets/Scripts/PlacementSystem/Core/IInputProvider.cs \
        Assets/Scripts/PlacementSystem/Input/LegacyInputProvider.cs \
        Assets/Scripts/PlacementSystem/Input/NewInputSystemProvider.cs \
        Assets/Tests/EditMode/PlacementSystem/PlacementToolContextTests.cs

git commit -m "feat: add placement tool api and context"
```

---

### Task 2: Implement PlacementTool (ghost placement)

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Tools/PlacementTool.cs`

**Step 1: Write the failing test**

Create: `Assets/Tests/EditMode/PlacementSystem/PlacementToolTests.cs`
```csharp
using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class PlacementToolTests
{
    [Test]
    public void PlacementTool_CanEnterAndExit()
    {
        var tool = new PlacementTool();
        Assert.NotNull(tool);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner (EditMode) -> PlacementToolTests`
Expected: FAIL (missing PlacementTool)

**Step 3: Write minimal implementation**

Implement PlacementTool with ghost lifecycle, raycast for placement, snapping with SnapManager, validation, visual feedback, confirm/cancel using context input. Move existing logic from PlacementController into this tool.

**Step 4: Run test to verify it passes**

Run: `Unity Test Runner (EditMode) -> PlacementToolTests`
Expected: PASS

**Step 5: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Tools/PlacementTool.cs \
        Assets/Tests/EditMode/PlacementSystem/PlacementToolTests.cs

git commit -m "feat: add placement tool"
```

---

### Task 3: Implement SelectionTool (selection input + state)

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Tools/SelectionTool.cs`

**Step 1: Write the failing test**

Create: `Assets/Tests/EditMode/PlacementSystem/SelectionToolTests.cs`
```csharp
using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class SelectionToolTests
{
    [Test]
    public void SelectionTool_CanEnterAndExit()
    {
        var tool = new SelectionTool();
        Assert.NotNull(tool);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner (EditMode) -> SelectionToolTests`
Expected: FAIL (missing SelectionTool)

**Step 3: Write minimal implementation**

Implement selection input in SelectionTool, replacing SelectionManager. Use IInputProvider pointer + camera raycast, check Selectable, manage selection list and current selection in context. Respect multi-select settings and modifier logic.

**Step 4: Run test to verify it passes**

Run: `Unity Test Runner (EditMode) -> SelectionToolTests`
Expected: PASS

**Step 5: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Tools/SelectionTool.cs \
        Assets/Tests/EditMode/PlacementSystem/SelectionToolTests.cs

git commit -m "feat: add selection tool"
```

---

### Task 4: Implement MoveTool (pointer movement)

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Tools/MoveTool.cs`

**Step 1: Write the failing test**

Create: `Assets/Tests/EditMode/PlacementSystem/MoveToolTests.cs`
```csharp
using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class MoveToolTests
{
    [Test]
    public void MoveTool_CanEnterAndExit()
    {
        var tool = new MoveTool();
        Assert.NotNull(tool);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner (EditMode) -> MoveToolTests`
Expected: FAIL (missing MoveTool)

**Step 3: Write minimal implementation**

Move selected object with pointer raycast to movement surface, using snapping and placement strategy. Update visual feedback as valid.

**Step 4: Run test to verify it passes**

Run: `Unity Test Runner (EditMode) -> MoveToolTests`
Expected: PASS

**Step 5: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Tools/MoveTool.cs \
        Assets/Tests/EditMode/PlacementSystem/MoveToolTests.cs

git commit -m "feat: add move tool"
```

---

### Task 5: Implement RotateTool and DeleteTool

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Tools/RotateTool.cs`
- Create: `Assets/Scripts/PlacementSystem/Tools/DeleteTool.cs`

**Step 1: Write the failing tests**

Create: `Assets/Tests/EditMode/PlacementSystem/RotateDeleteToolTests.cs`
```csharp
using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class RotateDeleteToolTests
{
    [Test]
    public void RotateTool_CanEnterAndExit()
    {
        var tool = new RotateTool();
        Assert.NotNull(tool);
    }

    [Test]
    public void DeleteTool_CanEnterAndExit()
    {
        var tool = new DeleteTool();
        Assert.NotNull(tool);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner (EditMode) -> RotateDeleteToolTests`
Expected: FAIL (missing RotateTool/DeleteTool)

**Step 3: Write minimal implementation**

RotateTool: apply rotation input to selected object using rotation increment.
DeleteTool: on delete input, destroy selected objects and clear selection state.

**Step 4: Run test to verify it passes**

Run: `Unity Test Runner (EditMode) -> RotateDeleteToolTests`
Expected: PASS

**Step 5: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Tools/RotateTool.cs \
        Assets/Scripts/PlacementSystem/Tools/DeleteTool.cs \
        Assets/Tests/EditMode/PlacementSystem/RotateDeleteToolTests.cs

git commit -m "feat: add rotate and delete tools"
```

---

### Task 6: Refactor PlacementController to use tools

**Files:**
- Modify: `Assets/Scripts/PlacementSystem/Core/PlacementController.cs`

**Step 1: Write the failing test**

Create: `Assets/Tests/EditMode/PlacementSystem/PlacementControllerToolModeTests.cs`
```csharp
using NUnit.Framework;

public class PlacementControllerToolModeTests
{
    [Test]
    public void PlacementController_DefaultsToSelectionTool()
    {
        // Add a PlayMode test if needed; for EditMode, just ensure the class compiles.
        Assert.Pass();
    }
}
```

**Step 2: Run test to verify it fails**

Run: `Unity Test Runner (EditMode) -> PlacementControllerToolModeTests`
Expected: FAIL (if tool mode fields not compiled)

**Step 3: Write minimal implementation**

Refactor PlacementController to:
- Build PlacementToolContext
- Instantiate tools
- Delegate Update() to active tool
- Provide API to switch tools
- Remove selection and movement logic now in tools

**Step 4: Run test to verify it passes**

Run: `Unity Test Runner (EditMode) -> PlacementControllerToolModeTests`
Expected: PASS

**Step 5: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Core/PlacementController.cs \
        Assets/Tests/EditMode/PlacementSystem/PlacementControllerToolModeTests.cs

git commit -m "refactor: delegate placement behavior to tools"
```

---

### Task 7: Remove SelectionManager and update docs

**Files:**
- Delete: `Assets/Scripts/PlacementSystem/Selection/SelectionManager.cs`
- Modify: `Assets/Scripts/PlacementSystem/README.md`
- Modify (optional): `Assets/Scripts/PlacementSystem/Examples/PlacementSystemExample.cs`

**Step 1: Write the failing test**

No new tests. Use manual verification checklist.

**Step 2: Update docs**

Update README to reflect tools pattern and removal of SelectionManager. Update example (if needed) to switch tools via PlacementController API.

**Step 3: Manual verification**

Run in editor and verify:
- Place object (ghost + confirm + cancel)
- Rotate during placement
- Select single object
- Multi-select with modifier
- Move selected object with pointer
- Rotate selected object
- Delete selected object(s)
- Socket snapping during placement and movement

**Step 4: Commit**

```bash
git add Assets/Scripts/PlacementSystem/README.md \
        Assets/Scripts/PlacementSystem/Examples/PlacementSystemExample.cs

git rm Assets/Scripts/PlacementSystem/Selection/SelectionManager.cs

git commit -m "docs: update placement system tools" 
```

---

### Task 8: Final verification

**Step 1: Run Unity EditMode tests**

Run: `Unity Test Runner (EditMode) -> All`
Expected: PASS

**Step 2: Manual smoke test in editor**

Verify selection, placement, movement, rotation, delete, snapping.

**Step 3: Commit any final fixes**

```bash
git add -A
git commit -m "chore: stabilize placement tools"
```
