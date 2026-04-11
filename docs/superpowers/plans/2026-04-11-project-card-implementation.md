# ProjectCard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `ProjectCard` from a row-style item into the approved grid card component from the Projects reference while preserving current binding flow and adding badge/menu presentation APIs.

**Architecture:** Keep the change focused on the existing `ProjectCard` component and one small integration point in `ProjectItemView`. Preserve `dataSource`-driven text binding for `name` and `meta`, add component-owned visual state for image, badge, and menu affordances, and verify the contract with Edit Mode UI tests rather than broad page-level rewrites.

**Tech Stack:** Unity UI Toolkit (`VisualElement`, UXML, USS), C# custom elements, NUnit Edit Mode tests, Unity Test Runner batch mode.

---

## File Structure

Primary files in scope:

- Modify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.cs`
- Modify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml`
- Modify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uss`
- Modify: `Assets/Scripts/UI/Pages/ListProjectPage/ProjectItemView.cs`
- Create: `Assets/Tests/UI/Components/ProjectCardTests.cs`

Reference-only files:

- Read: `Assets/Scripts/UI/Pages/ListProjectPage/ListProjectPage.uxml`
- Read: `Assets/Scripts/UI/Pages/ListProjectPage/ListProjectPage.uss`
- Read: `Assets/Scripts/UI/Screens/Projects/Projects.html`
- Read: `docs/superpowers/specs/2026-04-11-project-card-design.md`

Out of scope:

- `Assets/Scripts/UI/Models/Project.cs`
- `Assets/Scripts/UI/Pages/ListProjectPage/ListProjectPage.cs`
- New shared `Card` base abstraction
- Any filtering, navigation, or business-rule badge logic

## Scope Notes

- The current GitNexus index did not resolve `ProjectCard` or `ProjectItemView` by symbol name, so scope should be verified directly from the file references above.
- Current known direct usage of `ProjectCard` is in `Assets/Scripts/UI/Pages/ListProjectPage/ProjectItemView.cs`.
- The current `Project` model only exposes `name` and `meta`, so badge state must remain optional and hidden by default.
- Current baseline deficiencies are intentional inputs to this plan, not prerequisites already satisfied: the test file does not exist yet, the current UXML does not contain the new named elements, the current C# API does not include badge/menu methods or the menu event, and the current USS is still row-based.
- The workspace task uses a stale Unity path. For command-line test execution, use `/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity` instead of the current default task, and close any open Unity editor instance first to avoid project lock failures.

---

### Task 1: Add component tests for the new ProjectCard contract

**Files:**
- Create: `Assets/Tests/UI/Components/ProjectCardTests.cs`
- Read: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.cs`
- Read: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml`

- [ ] **Step 1: Write the failing Edit Mode test file for ProjectCard structure and API behavior**

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Systems.UI.Tests
{
    public class ProjectCardTests
    {
        [Test]
        public void Constructor_ClonesExpectedElements()
        {
            var card = new ProjectCard();

            Assert.IsNotNull(card.Q<VisualElement>("card-media"));
            Assert.IsNotNull(card.Q<VisualElement>("card-image"));
            Assert.IsNotNull(card.Q<Label>("card-badge"));
            Assert.IsNotNull(card.Q<VisualElement>("card-menu-button"));
            Assert.IsNotNull(card.Q<VisualElement>("card-gradient"));
            Assert.IsNotNull(card.Q<Label>("project-name"));
            Assert.IsNotNull(card.Q<Label>("project-meta"));
        }
    }
}
```

- [ ] **Step 2: Add failing tests for `Initialize`, `SetBadge`, `SetMenuVisible`, and `MenuClicked`**

```csharp
[Test]
public void SetBadge_UpdatesTextAndCanHideBadge() { }

[Test]
public void SetMenuVisible_HidesMenuWithoutThrowing() { }

[Test]
public void MenuButtonClick_RaisesMenuClickedOnce() { }
```

- [ ] **Step 3: Run the new tests to verify they fail for the current row-style implementation**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -runTests -testPlatform editmode -testFilter Systems.UI.Tests.ProjectCardTests -testResults Temp/ProjectCardTests.xml
```

Expected: FAIL because the current `ProjectCard` tree and API do not yet provide the new elements and methods.

- [ ] **Step 4: Commit the red test baseline**

```bash
git add Assets/Tests/UI/Components/ProjectCardTests.cs
git commit -m "test(ui): define ProjectCard component contract"
```

---

### Task 2: Rebuild ProjectCard UXML and USS to match the approved card structure

**Files:**
- Modify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml`
- Modify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uss`
- Read: `Assets/Scripts/UI/Screens/Projects/Projects.html`
- Test: `Assets/Tests/UI/Components/ProjectCardTests.cs`

- [ ] **Step 1: Replace the old row-style UXML tree with the approved named element structure**

Required element names:

- `card-media`
- `card-image`
- `card-badge`
- `card-menu-button`
- `card-gradient`
- `project-name`
- `project-meta`

- [ ] **Step 2: Preserve text bindings for `project-name` and `project-meta` in the new UXML tree**

- [ ] **Step 3: Rewrite `ProjectCard.uss` for the vertical card presentation from the spec**

Required first-pass constants:

- card padding `8px`
- outer radius `12px`
- media radius `8px`
- title size `14px`
- meta size `12px`
- media width near `220px`
- media height near `275px`

- [ ] **Step 4: Implement badge, menu, and gradient positioning in USS with safe fallbacks if UI Toolkit styling is limited**

- [ ] **Step 5: Run the ProjectCard tests and verify the structure test now passes while behavior tests still drive the next code change**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -runTests -testPlatform editmode -testFilter Systems.UI.Tests.ProjectCardTests -testResults Temp/ProjectCardTests.xml
```

Expected: some tests still fail until the C# API is updated, but the named element queries should now succeed.

- [ ] **Step 6: Commit the visual structure update**

```bash
git add Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uss
git commit -m "feat(ui): rebuild ProjectCard visual structure"
```

---

### Task 3: Implement the ProjectCard presentation API and menu event

**Files:**
- Modify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.cs`
- Test: `Assets/Tests/UI/Components/ProjectCardTests.cs`

- [ ] **Step 1: Add the failing assertions for the exact API behaviors not yet covered deeply enough**

```csharp
Assert.AreEqual("Late Night Scene 01", card.Q<Label>("project-name").text);
Assert.AreEqual("2h ago • 3 Actors", card.Q<Label>("project-meta").text);
```

- [ ] **Step 2: Cache the queried child elements in `ProjectCard` after `UxmlCloneTree()`**

- [ ] **Step 3: Add `public event Action MenuClicked;` and wire it to `card-menu-button` click handling**

- [ ] **Step 4: Implement `SetBadge(string text, bool visible)` using layout-collapsing visibility**

- [ ] **Step 5: Implement `SetMenuVisible(bool visible)` using layout-collapsing visibility**

- [ ] **Step 6: Keep `Initialize(...)` limited to title, meta, and image setup, and keep `SetImage(Texture2D image)` safe when image is null**

- [ ] **Step 7: Run the ProjectCard test suite and verify all tests pass**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -runTests -testPlatform editmode -testFilter Systems.UI.Tests.ProjectCardTests -testResults Temp/ProjectCardTests.xml
```

Expected: PASS for all `Systems.UI.Tests.ProjectCardTests` cases.

- [ ] **Step 8: Commit the API implementation**

```bash
git add Assets/Scripts/UI/Components/ProjectCard/ProjectCard.cs Assets/Tests/UI/Components/ProjectCardTests.cs
git commit -m "feat(ui): add ProjectCard presentation api"
```

---

### Task 4: Apply the minimal integration update in ProjectItemView

**Files:**
- Modify: `Assets/Scripts/UI/Pages/ListProjectPage/ProjectItemView.cs`
- Test: `Assets/Tests/UI/Components/ProjectCardTests.cs`

- [ ] **Step 1: Add one failing integration-oriented test or assertion proving `ProjectItemView` can still create and configure `ProjectCard` safely**

```csharp
[Test]
public void ProjectItemView_CreatesProjectCard_AndKeepsSafeDefaults() { }
```

- [ ] **Step 2: Keep `m_ProjectCard.dataSource = m_ViewModel?.Project` as the primary text-binding path**

- [ ] **Step 3: Explicitly apply safe presentation defaults in `BindDataContext()`**

Defaults:

- badge hidden
- menu visible
- no fabricated badge text

- [ ] **Step 4: Avoid page-level behavior changes in `ListProjectPage` unless test evidence shows they are necessary**

- [ ] **Step 5: Re-run the ProjectCard test suite and confirm no regression in component construction or binding setup**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -runTests -testPlatform editmode -testFilter Systems.UI.Tests.ProjectCardTests -testResults Temp/ProjectCardTests.xml
```

Expected: PASS with `ProjectItemView` still functioning as the owner of the card instance.

- [ ] **Step 6: Commit the integration touch**

```bash
git add Assets/Scripts/UI/Pages/ListProjectPage/ProjectItemView.cs Assets/Tests/UI/Components/ProjectCardTests.cs
git commit -m "refactor(ui): keep ProjectItemView aligned with ProjectCard contract"
```

---

### Task 5: Run focused verification and visual smoke checks

**Files:**
- Modify: none expected unless verification fails
- Verify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.cs`
- Verify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml`
- Verify: `Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uss`
- Verify: `Assets/Scripts/UI/Pages/ListProjectPage/ProjectItemView.cs`
- Verify: `Assets/Tests/UI/Components/ProjectCardTests.cs`

- [ ] **Step 1: Run the focused Edit Mode test suite one final time**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -nographics -projectPath "$PWD" -runTests -testPlatform editmode -testFilter Systems.UI.Tests.ProjectCardTests -testResults Temp/ProjectCardTests.xml
```

Expected: PASS with no new compilation errors in the touched UI files.

- [ ] **Step 2: Open the Projects screen in the Unity editor and manually verify the card matches the approved reference closely enough**

Manual checklist:

- the card is vertical, not row-based
- the image region reads as portrait and close to `4:5`
- the badge is hidden by default
- the menu affordance is visible and correctly positioned
- title and meta remain readable in the current list layout
- the page does not require changes to `Project` data just to render the card

- [ ] **Step 3: Check the Unity console for compile or UI Toolkit warnings introduced by the change**

- [ ] **Step 4: Commit the verification checkpoint**

```bash
git add Assets/Scripts/UI/Components/ProjectCard/ProjectCard.cs Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uss Assets/Scripts/UI/Pages/ListProjectPage/ProjectItemView.cs Assets/Tests/UI/Components/ProjectCardTests.cs
git commit -m "test(ui): verify ProjectCard redesign"
```

---

## Verification Matrix

- `ProjectCard` clones the full named tree from UXML.
- `Initialize`, `SetImage`, `SetBadge`, and `SetMenuVisible` behave safely with partial data.
- Menu clicks raise `MenuClicked` exactly once per click.
- `ProjectItemView` still owns the card and preserves text binding through `dataSource`.
- The rendered card visually aligns with the approved Projects reference without introducing badge business logic.

## Risk Controls and Rollback

- Keep the change contained to the existing card component and one integration file.
- Do not introduce a shared `Card` base class in this change.
- Do not modify `Project` or `ListProjectPage` unless a failing test proves it is required.
- If layout regressions appear in the list, revert the latest task commit first and inspect `ProjectCard.uss` width/height assumptions before broadening scope.

## Suggested Execution Order

1. Task 1 component tests
2. Task 2 UXML and USS rebuild
3. Task 3 C# API and event wiring
4. Task 4 `ProjectItemView` alignment
5. Task 5 focused verification