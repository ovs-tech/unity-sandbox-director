# Unity Events and Validation Results Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor the Placement System validation returning a `ValidationResult` struct with error messages and add `UnityEvents` to `PlacementController` so non-coders can hook up UI, Audio, and VFX easily.

**Architecture:** Introduce `ValidationResult` struct. Update `IPlacementValidator`, `PlacementRule`, and `Part` to return this struct. Expose `UnityEvent` fields in `PlacementController` for key lifecycle events (Started, Success, Failed, Cancelled, ToolChanged) and trigger them from the relevant Tools.

**Tech Stack:** Unity C#, UnityEngine.Events

---

### Task 1: Create ValidationResult Struct

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Validation/ValidationResult.cs`

**Step 1: Write minimal implementation**

```csharp
using UnityEngine;

namespace Systems.PlacementSystem.Validation
{
    public struct ValidationResult {
        public bool IsValid;
        public string ErrorMessage;

        public static ValidationResult Success => new ValidationResult { IsValid = true, ErrorMessage = string.Empty };
        public static ValidationResult Failure(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Validation/ValidationResult.cs
git commit -m "feat: add ValidationResult struct for detailed placement feedback"
```

### Task 2: Refactor Part and PlacementRule

**Files:**
- Modify: `Assets/Scripts/PlacementSystem/Validation/PlacementRule.cs`
- Modify: `Assets/Scripts/PlacementSystem/Validation/Part.cs`

**Step 1: Modify PlacementRule**

```csharp
// In PlacementRule.cs, change CheckRule signature:
public abstract ValidationResult CheckRule(Vector3 position, Quaternion rotation, GameObject ghostObject);
```

**Step 2: Modify Part**

```csharp
// In Part.cs, change ValidatePlacement signature and body:
public ValidationResult ValidatePlacement(Vector3 position, Quaternion rotation, GameObject ghostObject)
{
    if (_placementRules == null || _placementRules.Count == 0)
        return ValidationResult.Success;

    foreach (var rule in _placementRules)
    {
        if (rule == null) continue;
        
        var result = rule.CheckRule(position, rotation, ghostObject);
        if (!result.IsValid)
        {
            // Stop at first failure and return it
            return result;
        }
    }
    return ValidationResult.Success;
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/PlacementSystem/Validation/PlacementRule.cs Assets/Scripts/PlacementSystem/Validation/Part.cs
git commit -m "refactor: update PlacementRule and Part to use ValidationResult"
```

### Task 3: Refactor Concrete Rules

**Files:**
- Modify: `Assets/Scripts/PlacementSystem/Validation/ClearanceRule.cs`
- Modify: `Assets/Scripts/PlacementSystem/Validation/RequireSurfaceRule.cs`

**Step 1: Update ClearanceRule**
```csharp
// Change return type to ValidationResult
// Replace `return validOverlaps <= _maxAllowedOverlaps;` with:
bool isValid = validOverlaps <= _maxAllowedOverlaps;
return isValid ? ValidationResult.Success : ValidationResult.Failure("Space is occupied by another object.");
```

**Step 2: Update RequireSurfaceRule**
```csharp
// Change return type to ValidationResult
// Replace `return false;` in angle check with `return ValidationResult.Failure("Surface is too steep.");`
// Replace `return true;` with `return ValidationResult.Success;`
// Outside if: `return ValidationResult.Failure("Valid placement surface not found.");`
```

**Step 3: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Validation/ClearanceRule.cs Assets/Scripts/PlacementSystem/Validation/RequireSurfaceRule.cs
git commit -m "refactor: update concrete rules to return ValidationResult"
```

### Task 4: Refactor IPlacementValidator and DefaultPlacementValidator

**Files:**
- Modify: `Assets/Scripts/PlacementSystem/Core/IPlacementValidator.cs`
- Modify: `Assets/Scripts/PlacementSystem/Validation/DefaultPlacementValidator.cs`

**Step 1: Update IPlacementValidator**
```csharp
// Change IsPlacementValid to return ValidationResult
ValidationResult ValidatePlacement(Vector3 position, Quaternion rotation, GameObject ghostObject);
```

**Step 2: Update DefaultPlacementValidator**
```csharp
// Change IsPlacementValid to ValidatePlacement and return ValidationResult
public ValidationResult ValidatePlacement(Vector3 position, Quaternion rotation, GameObject ghostObject)
{
    if (ghostObject == null) return ValidationResult.Failure("Ghost object is null.");
    var Part = ghostObject.GetComponent<Part>();
    if (Part == null) return ValidationResult.Success;
    return Part.ValidatePlacement(position, rotation, ghostObject);
}
```

**Step 3: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Core/IPlacementValidator.cs Assets/Scripts/PlacementSystem/Validation/DefaultPlacementValidator.cs
git commit -m "refactor: update IPlacementValidator to ValidatePlacement returning ValidationResult"
```

### Task 5: Update PlacementTool to use ValidationResult

**Files:**
- Modify: `Assets/Scripts/PlacementSystem/Tools/PlacementTool.cs`

**Step 1: Update PlacementTool Callbacks**
```csharp
// Add OnPlacementFailed
public System.Action<string> OnPlacementFailed { get; set; }
```

**Step 2: Update ConfirmPlacement and UpdateGhostPosition**
```csharp
// In ConfirmPlacement, change:
var result = _context.PlacementValidator.ValidatePlacement(
    _ghostObject.transform.position,
    _ghostObject.transform.rotation,
    _ghostObject
);

if (!result.IsValid)
{
    OnPlacementFailed?.Invoke(result.ErrorMessage);
    return;
}

// In UpdateGhostPosition, change:
var result = _context.PlacementValidator.ValidatePlacement(
    targetPosition,
    targetRotation,
    _ghostObject
);

_context.PlacementVisualizer?.UpdateVisual(result.IsValid);

if (result.IsValid)
{
    _lastValidPosition = targetPosition;
    _lastValidRotation = targetRotation;
}
```

**Step 3: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Tools/PlacementTool.cs
git commit -m "refactor: update PlacementTool to use ValidationResult and add callback"
```

### Task 6: Add UnityEvents to PlacementController

**Files:**
- Modify: `Assets/Scripts/PlacementSystem/Core/PlacementController.cs`

**Step 1: Add UnityEvents**
```csharp
using UnityEngine.Events;

// In fields section add:
[Header("Events")]
public UnityEvent<GameObject> OnPlacementStartedEvent;
public UnityEvent<GameObject> OnPlacementSuccessEvent;
public UnityEvent<string> OnPlacementFailedEvent;
public UnityEvent OnPlacementCancelledEvent;
public UnityEvent<PlacementToolType> OnToolChangedEvent;
```

**Step 2: Hook up events**
```csharp
// In Awake, add to placementTool subscriptions:
placementTool.OnPlacementFailed = HandlePlacementFailed;

// Add handler:
private void HandlePlacementFailed(string reason)
{
    OnPlacementFailedEvent?.Invoke(reason);
}

// In HandlePlacementConfirmed, add:
OnPlacementSuccessEvent?.Invoke(placedObject);

// In HandlePlacementCancelled, add:
OnPlacementCancelledEvent?.Invoke();

// In StartPlacement, at the end add:
OnPlacementStartedEvent?.Invoke(_currentGhost);

// In SetActiveTool, at the end add:
OnToolChangedEvent?.Invoke(toolType);
```

**Step 3: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Core/PlacementController.cs
git commit -m "feat: add UnityEvents to PlacementController for zero-code integration"
```

### Task 7: Fix Unit Tests

**Files:**
- Run terminal command: `dotnet build`
- Modify: Any files failing to compile in `Tests/PlacementSystem/` due to boolean return type change to `ValidationResult`.

**Step 1: Fix tests**
```csharp
// In failing tests, change `.Returns(true)` to `.Returns(ValidationResult.Success)`
// and `.Returns(false)` to `.Returns(ValidationResult.Failure("reason"))`.
```

**Step 2: Run test suite to verify**
Run the tests using Unity Test Runner logic (or run them manually to pass).

**Step 3: Commit Test Fixes**
```bash
git add Tests/PlacementSystem/
git commit -m "test: fix tests for ValidationResult signature changes"
```
