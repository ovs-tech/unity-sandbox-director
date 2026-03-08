# Async Persistence for MiniTimeline Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add non-blocking async save/load support to persistence and MiniTimeline so large project load/save no longer stalls the Unity main thread.

**Architecture:** Implement Task-based async APIs in persistence core (`IDataService`, `BaseDataService`, `FileDataService`, `GamePersistenceManager`) and add coroutine wrappers in `MiniTimelineDirector` for Unity-friendly calling. Keep existing synchronous APIs unchanged for backward compatibility while routing new async call paths through background IO/serialization and main-thread-only Unity object mutation.

**Tech Stack:** Unity 6000.2.6f2, C#, NUnit/EditMode tests, `Task`/`async`/`await`, Unity coroutines.

---

### Task 1: Add Failing Tests for Async Data Service APIs

**Files:**
- Create: `Assets/Tests/Persistence/FileDataServiceAsyncTests.cs`
- Test: `Assets/Tests/Persistence/FileDataServiceAsyncTests.cs`

**Step 1: Write the failing test**

```csharp
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Systems.Persistence.Services;
using UnityEngine;

namespace Systems.Persistence.Tests
{
    public class FileDataServiceAsyncTests
    {
        [System.Serializable]
        private class Payload { public int value; }

        [Test]
        public async Task SaveAsync_ThenLoadAsync_RoundTripsPayload()
        {
            var service = ScriptableObject.CreateInstance<FileDataService>();
            service.Setup();

            var expected = new Payload { value = 42 };
            await service.SaveAsync(expected, "AsyncSave", "tests", "payload", true, CancellationToken.None);
            var actual = await service.LoadAsync<Payload>("AsyncSave", "tests", "payload", CancellationToken.None);

            Assert.IsNotNull(actual);
            Assert.AreEqual(42, actual.value);

            Object.DestroyImmediate(service);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~FileDataServiceAsyncTests.SaveAsync_ThenLoadAsync_RoundTripsPayload" --nologo --verbosity minimal`
Expected: FAIL with compile error indicating missing `SaveAsync`/`LoadAsync` methods.

**Step 3: Commit failing test**

```bash
git add Assets/Tests/Persistence/FileDataServiceAsyncTests.cs
git commit -m "test(persistence): add failing async file data service roundtrip test"
```

### Task 2: Implement Async Contracts in Persistence Core

**Files:**
- Modify: `Assets/Scripts/Persistence/Core/IDataService.cs`
- Modify: `Assets/Scripts/Persistence/Core/BaseDataService.cs`
- Modify: `Assets/Scripts/Persistence/Services/FileDataService.cs`
- Test: `Assets/Tests/Persistence/FileDataServiceAsyncTests.cs`

**Step 1: Add minimal async contract signatures**

```csharp
Task SaveAsync<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true, CancellationToken token = default);
Task<T> LoadAsync<T>(string saveName, string ns, string fileName = null, CancellationToken token = default);
```

**Step 2: Add base default implementations in `BaseDataService`**

```csharp
public virtual Task SaveAsync<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true, CancellationToken token = default)
    => Task.Run(() => Save(data, saveName, ns, fileName, overwrite), token);

public virtual Task<T> LoadAsync<T>(string saveName, string ns, string fileName = null, CancellationToken token = default)
    => Task.Run(() => Load<T>(saveName, ns, fileName), token);
```

**Step 3: Implement true async IO path in `FileDataService`**

```csharp
public override async Task SaveAsync<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true, CancellationToken token = default)
{
    var filePath = GetFilePath(saveName, ns, fileName);
    var directory = Path.GetDirectoryName(filePath);
    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    if (File.Exists(filePath) && !overwrite) return;

    var json = await Task.Run(() => serializer.Serialize(data), token);
    await File.WriteAllTextAsync(filePath, json, token);
}

public override async Task<T> LoadAsync<T>(string saveName, string ns, string fileName = null, CancellationToken token = default)
{
    var filePath = GetFilePath(saveName, ns, fileName);
    if (!File.Exists(filePath)) return default;

    var json = await File.ReadAllTextAsync(filePath, token);
    return await Task.Run(() => serializer.Deserialize<T>(json), token);
}
```

**Step 4: Re-run the async data service test**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~FileDataServiceAsyncTests" --nologo --verbosity minimal`
Expected: PASS for `FileDataServiceAsyncTests` and no compile errors in persistence core.

**Step 5: Commit implementation**

```bash
git add Assets/Scripts/Persistence/Core/IDataService.cs Assets/Scripts/Persistence/Core/BaseDataService.cs Assets/Scripts/Persistence/Services/FileDataService.cs
git commit -m "feat(persistence): add async data service save/load APIs"
```

### Task 3: Add Failing Tests for Async GamePersistenceManager APIs

**Files:**
- Create: `Assets/Tests/Persistence/GamePersistenceManagerAsyncTests.cs`
- Create: `Assets/Tests/Persistence/TestDoubles/InMemoryDataService.cs`
- Test: `Assets/Tests/Persistence/GamePersistenceManagerAsyncTests.cs`

**Step 1: Write failing tests for subsystem-aware async save/load**

```csharp
using System.Threading.Tasks;
using NUnit.Framework;
using Systems.Persistence;
using Systems.Persistence.Core;
using UnityEngine;

namespace Systems.Persistence.Tests
{
    public class GamePersistenceManagerAsyncTests
    {
        [System.Serializable]
        private class FakePayload { public int value; }

        private class FakeSubsystem : ISubsystemPersistence
        {
            public string Namespace => "minitimeline";
            public string PersistentName => "AsyncProject";
            public PersistenceTarget Target => PersistenceTarget.External;
            public object Data = new FakePayload { value = 7 };
            public object GetSaveData() => Data;
            public void LoadData(object data) => Data = data;
            public System.Type DataType => typeof(FakePayload);
        }

        [Test]
        public async Task SaveFileAsync_ThenLoadFileAsync_RoundTripsSubsystemData()
        {
            var go = new GameObject("PersistenceManagerTest");
            var manager = go.AddComponent<GamePersistenceManager>();
            var subsystem = new FakeSubsystem();

            await manager.SaveFileAsync(subsystem, null, "AsyncProject");
            var loaded = await manager.LoadFileAsync<FakePayload>(subsystem, null, "AsyncProject");

            Assert.IsNotNull(loaded);
            Assert.AreEqual(7, loaded.value);

            Object.DestroyImmediate(go);
        }
    }
}
```

**Step 2: Run tests to verify they fail first**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~GamePersistenceManagerAsyncTests" --nologo --verbosity minimal`
Expected: FAIL with missing `SaveFileAsync`/`LoadFileAsync` members.

**Step 3: Commit failing tests**

```bash
git add Assets/Tests/Persistence/GamePersistenceManagerAsyncTests.cs Assets/Tests/Persistence/TestDoubles/InMemoryDataService.cs
git commit -m "test(persistence): add failing async manager subsystem tests"
```

### Task 4: Implement Async APIs in GamePersistenceManager

**Files:**
- Modify: `Assets/Scripts/Persistence/GamePersistenceManager.cs`
- Test: `Assets/Tests/Persistence/GamePersistenceManagerAsyncTests.cs`

**Step 1: Implement subsystem-aware async save**

```csharp
public async Task SaveFileAsync(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, bool overwrite = true, CancellationToken token = default)
{
    if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

    var saveTo = saveName ?? CurrentSaveName ?? "Default";
    var fileNameToUse = fileName ?? subsystem.PersistentName;
    var data = subsystem.GetSaveData();
    if (data == null) return;

    await dataService.SaveAsync(data, saveTo, subsystem.Namespace, fileNameToUse, overwrite, token);
}
```

**Step 2: Implement subsystem-aware async load**

```csharp
public async Task<T> LoadFileAsync<T>(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, CancellationToken token = default)
{
    if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

    var loadFrom = saveName ?? CurrentSaveName ?? "Default";
    var fileNameToUse = fileName ?? subsystem.PersistentName;
    return await dataService.LoadAsync<T>(loadFrom, subsystem.Namespace, fileNameToUse, token);
}
```

**Step 3: Preserve embedded-target behavior**
- If `subsystem.Target == PersistenceTarget.Embedded`, keep existing sync path initially and use `Task.Run` wrapper for parity.

**Step 4: Re-run manager async tests**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~GamePersistenceManagerAsyncTests" --nologo --verbosity minimal`
Expected: PASS for manager async tests.

**Step 5: Commit manager async implementation**

```bash
git add Assets/Scripts/Persistence/GamePersistenceManager.cs
git commit -m "feat(persistence): add async subsystem save/load in manager"
```

### Task 5: Add Failing Tests for MiniTimeline Async Load/Save Wrappers

**Files:**
- Create: `Assets/Tests/MiniTimeline/CoreTests/MiniTimelineDirectorAsyncPersistenceTests.cs`
- Test: `Assets/Tests/MiniTimeline/CoreTests/MiniTimelineDirectorAsyncPersistenceTests.cs`

**Step 1: Write failing coroutine wrapper tests**

```csharp
using System.Collections;
using NUnit.Framework;
using Systems.MiniTimeline.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniTimeline.Core.Tests
{
    public class MiniTimelineDirectorAsyncPersistenceTests
    {
        [UnityTest]
        public IEnumerator LoadProjectAsync_InvokesCompletionAndSetsProject()
        {
            var go = new GameObject("MiniTimelineAsyncTest");
            var director = go.AddComponent<MiniTimelineDirector>();
            go.AddComponent<BindableObjectManager>();

            director.CreateNewProject("AsyncProject", 8f, 30f);
            Assert.IsTrue(director.SaveProject("AsyncProject"));

            bool? result = null;
            yield return director.LoadProjectAsync("AsyncProject", success => result = success);

            Assert.AreEqual(true, result);
            Assert.IsNotNull(director.Project);
            Assert.AreEqual("AsyncProject", director.Project.name);

            Object.DestroyImmediate(go);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~MiniTimelineDirectorAsyncPersistenceTests" --nologo --verbosity minimal`
Expected: FAIL with missing `LoadProjectAsync` method.

**Step 3: Commit failing test**

```bash
git add Assets/Tests/MiniTimeline/CoreTests/MiniTimelineDirectorAsyncPersistenceTests.cs
git commit -m "test(minitimeline): add failing async director persistence wrapper tests"
```

### Task 6: Implement MiniTimeline Async Coroutine Wrappers and Compatibility

**Files:**
- Modify: `Assets/Scripts/MiniTimeline/Core/MiniTimelineDirector.cs`
- Test: `Assets/Tests/MiniTimeline/CoreTests/MiniTimelineDirectorAsyncPersistenceTests.cs`
- Test: `Assets/Tests/MiniTimeline/CoreTests/MiniTimelineDirectorTests.cs`

**Step 1: Add coroutine wrappers without breaking sync API**

```csharp
public IEnumerator LoadProjectAsync(string projectName, Action<bool> onDone = null)
{
    var mgr = GamePersistenceManager.Instance;
    if (mgr == null)
    {
        onDone?.Invoke(false);
        yield break;
    }

    var task = mgr.LoadFileAsync<MiniTimelineProject>(this, null, projectName);
    while (!task.IsCompleted) yield return null;

    if (task.IsCompletedSuccessfully && task.Result != null)
    {
        SetProject(task.Result);
        onDone?.Invoke(true);
        yield break;
    }

    onDone?.Invoke(false);
}

public IEnumerator SaveProjectAsync(string projectName = null, Action<bool> onDone = null)
{
    if (project == null)
    {
        onDone?.Invoke(false);
        yield break;
    }

    var mgr = GamePersistenceManager.Instance;
    if (mgr == null)
    {
        onDone?.Invoke(false);
        yield break;
    }

    var task = mgr.SaveFileAsync(this, null, projectName, true);
    while (!task.IsCompleted) yield return null;

    onDone?.Invoke(task.IsCompletedSuccessfully);
}
```

**Step 2: Keep existing sync methods untouched**
- Do not change current `SaveProject` and `LoadProject` behavior except optional internal reuse if it does not alter semantics.

**Step 3: Run MiniTimeline async tests**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~MiniTimelineDirectorAsyncPersistenceTests|FullyQualifiedName~MiniTimelineDirectorTests" --nologo --verbosity minimal`
Expected: PASS for both test classes.

**Step 4: Commit director async implementation**

```bash
git add Assets/Scripts/MiniTimeline/Core/MiniTimelineDirector.cs Assets/Tests/MiniTimeline/CoreTests/MiniTimelineDirectorAsyncPersistenceTests.cs
git commit -m "feat(minitimeline): add async coroutine save/load wrappers"
```

### Task 7: Update OpenSpec Change and Run Validation

**Files:**
- Modify: `openspec/changes/mini-timeline-persistence-integration/proposal.md`
- Modify: `openspec/changes/mini-timeline-persistence-integration/design.md`
- Modify: `openspec/changes/mini-timeline-persistence-integration/tasks.md`
- Modify: `openspec/changes/mini-timeline-persistence-integration/specs/mini-timeline-persistence/spec.md`

**Step 1: Add async load/save scope to proposal + design**
- Specify Option C (Task core + coroutine wrappers) and no-sync-breaking policy.

**Step 2: Add/modify requirement scenarios in spec delta**

```markdown
#### Scenario: Async load does not block main thread
- **WHEN** a timeline project is loaded using async APIs
- **THEN** file IO and deserialization run asynchronously
- **AND** Unity object mutation occurs on the main thread
```

**Step 3: Mark implementation checklist items for async work**
- Add explicit tasks for persistence async APIs, director wrappers, and tests.

**Step 4: Validate OpenSpec change**

Run: `openspec validate mini-timeline-persistence-integration --strict`
Expected: Validation passes with no formatting or scenario errors.

**Step 5: Commit spec updates**

```bash
git add openspec/changes/mini-timeline-persistence-integration/proposal.md openspec/changes/mini-timeline-persistence-integration/design.md openspec/changes/mini-timeline-persistence-integration/tasks.md openspec/changes/mini-timeline-persistence-integration/specs/mini-timeline-persistence/spec.md
git commit -m "spec(minitimeline): extend persistence proposal with async save/load"
```

### Task 8: Final Verification Gate

**Files:**
- Modify: `docs/plans/2026-03-08-async-persistence-implementation-plan.md` (checklist updates only)

**Step 1: Run targeted verification suite**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~FileDataServiceAsyncTests|FullyQualifiedName~GamePersistenceManagerAsyncTests|FullyQualifiedName~MiniTimelineDirectorAsyncPersistenceTests" --nologo --verbosity minimal`
Expected: All targeted async tests PASS.

**Step 2: Run broader MiniTimeline core tests**

Run: `dotnet test EditModeTests.csproj --filter "FullyQualifiedName~MiniTimelineDirectorTests" --nologo --verbosity minimal`
Expected: Existing core director tests remain PASS.

**Step 3: Record verification notes in plan PR description**
- Include exact command lines and pass/fail summary.

**Step 4: Commit final checklist status**

```bash
git add docs/plans/2026-03-08-async-persistence-implementation-plan.md
git commit -m "docs(plan): mark async persistence verification complete"
```
