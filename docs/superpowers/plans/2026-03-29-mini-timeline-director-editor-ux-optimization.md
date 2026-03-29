# MiniTimeline Director Editor UX Optimization Implementation Plan

> For agentic workers: REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

Goal: Improve MiniTimelineDirectorEditor usability, safety, and iteration speed without changing MiniTimeline runtime behavior.

Architecture: Keep all behavior changes isolated to the custom inspector and new editor tests. Introduce reversible editor mutations through Unity Undo, unify clip editing paths, and reduce modal dialog friction while preserving destructive confirmations. Avoid runtime API or persistence contract changes unless separately approved.

Tech Stack: Unity Editor IMGUI, Unity Undo API, Edit Mode tests, MiniTimeline editor/runtime assemblies.

---

## GitNexus Planning Inputs

- Impact analysis on MiniTimelineDirectorEditor: LOW risk, 0 direct upstream dependents.
- Impact analysis on MiniTimelineDirector: CRITICAL risk, 94 direct upstream import dependents, 109 impacted symbols.
- Planning decision: Keep this effort editor-only and do not modify MiniTimelineDirector runtime behavior in this change.

Primary files in scope:
- Modify: Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs
- Optional create: Assets/Tests/MiniTimeline/EditorTests/MiniTimelineDirectorEditorUxTests.cs
- Optional modify: Assets/Scripts/MiniTimeline/Core/MiniTimelineDirector.cs only if an editor-only helper is strictly required and separately approved

Out of scope:
- Runtime track evaluation logic
- Runtime persistence semantics
- MVVM timeline UI toolkit behavior

---

### Task 1: Add Undo-safe editor mutations

Files:
- Modify: Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs
- Test: Assets/Tests/MiniTimeline/EditorTests/MiniTimelineDirectorEditorUxTests.cs

- [ ] Step 1: Write failing Edit Mode tests for undoable operations
- [ ] Step 2: Verify tests fail because Undo entries are missing
- [ ] Step 3: Add Undo.RecordObject and Undo.RegisterCompleteObjectUndo before track, clip, binding, and project mutations
- [ ] Step 4: Replace direct AddComponent with Undo.AddComponent in binding-context creation flow
- [ ] Step 5: Ensure dirty flags are set only when actual mutation occurs
- [ ] Step 6: Run Edit Mode tests and confirm Undo test cases pass
- [ ] Step 7: Commit with message: feat(editor): add undo-safe MiniTimeline inspector mutations

Validation commands:
- Unity Edit Mode tests for MiniTimeline editor scope

Expected outcome:
- Every user-triggered mutation in inspector can be reversed with Cmd+Z.

---

### Task 2: Unify clip editing behavior and remove no-op update path

Files:
- Modify: Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs
- Test: Assets/Tests/MiniTimeline/EditorTests/MiniTimelineDirectorEditorUxTests.cs

- [ ] Step 1: Write failing tests for clip detail form update behavior
- [ ] Step 2: Decide one path and implement consistently: either editable detail form or list-only timing edits
- [ ] Step 3: Remove contradictory read-only messaging if timing remains editable
- [ ] Step 4: Implement real UpdateClipFromForm behavior or remove update action entirely
- [ ] Step 5: Preserve validation feedback for clip id and duration
- [ ] Step 6: Run tests and verify clip edits persist after save and inspector refresh
- [ ] Step 7: Commit with message: fix(editor): unify clip edit workflow and remove no-op update

Expected outcome:
- Users see one consistent editing model with no fake update actions.

---

### Task 3: Reduce modal interruption in save and load workflows

Files:
- Modify: Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs
- Test: Assets/Tests/MiniTimeline/EditorTests/MiniTimelineDirectorEditorUxTests.cs

- [ ] Step 1: Write tests for save and load success paths to avoid blocking dialogs
- [ ] Step 2: Keep confirmation dialogs only for destructive actions (delete, close project, clear bindings)
- [ ] Step 3: Convert success dialogs to non-blocking feedback (console log plus optional lightweight help box status)
- [ ] Step 4: Keep failure dialogs for actionable errors
- [ ] Step 5: Validate quick-save and quick-load behavior remains intact
- [ ] Step 6: Run tests and manual editor smoke checks
- [ ] Step 7: Commit with message: ux(editor): reduce modal dialogs in project save-load flows

Expected outcome:
- Faster repetitive save/load iteration with lower click fatigue.

---

### Task 4: Consolidate repaint strategy for play-mode responsiveness

Files:
- Modify: Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs
- Test: Assets/Tests/MiniTimeline/EditorTests/MiniTimelineDirectorEditorUxTests.cs

- [ ] Step 1: Write a failing test or instrumentation assertion for duplicate repaint triggers
- [ ] Step 2: Keep a single repaint driver (EditorApplication.update or guarded OnInspectorGUI repaint)
- [ ] Step 3: Add minimal throttling or state-based guard to avoid unnecessary repaints
- [ ] Step 4: Verify realtime playback UI still updates smoothly
- [ ] Step 5: Run tests and manual play-mode smoke checks
- [ ] Step 6: Commit with message: perf(editor): remove redundant repaint path in timeline inspector

Expected outcome:
- Lower editor overhead during playback without stale UI.

---

### Task 5: Persist inspector UX session state

Files:
- Modify: Assets/Scripts/MiniTimeline/Editor/MiniTimelineDirectorEditor.cs
- Test: Assets/Tests/MiniTimeline/EditorTests/MiniTimelineDirectorEditorUxTests.cs

- [ ] Step 1: Write failing tests for foldout and quick-action state reset across domain reload simulation
- [ ] Step 2: Persist selected foldouts and recent path fields using SessionState or EditorPrefs keys
- [ ] Step 3: Restore state in OnEnable and validate defaults on first use
- [ ] Step 4: Ensure state keys are namespaced to avoid collisions
- [ ] Step 5: Run tests and verify persistence behavior manually
- [ ] Step 6: Commit with message: feat(editor): persist MiniTimeline inspector ui session state

Expected outcome:
- Less repetitive setup after recompiles and assembly reloads.

---

## Verification Matrix

- Edit Mode tests pass for new UX behavior and Undo support.
- Existing MiniTimeline core tests remain unaffected.
- Manual smoke checklist:
  - Create, edit, remove track and clip operations are undoable
  - Save and load success feedback is non-blocking
  - Destructive actions still require confirmation
  - Clip editing path is consistent and predictable
  - Play mode inspector updates remain responsive
  - Foldout and recent path state survive domain reloads

---

## Risk Controls and Rollback

- Risk warning: MiniTimelineDirector runtime surface is CRITICAL blast radius in GitNexus.
- Control: constrain edits to MiniTimelineDirectorEditor and editor tests only.
- If runtime changes become necessary, run new GitNexus impact analysis on the exact runtime symbol before editing and open a separate scoped change.
- Rollback plan: revert each task commit independently if regressions appear.

---

## Suggested Execution Order

1. Task 1 Undo safety
2. Task 2 Clip edit consistency
3. Task 3 Save/load modal reduction
4. Task 4 Repaint consolidation
5. Task 5 Session state persistence

This order reduces user risk first, then improves workflow speed and polish.