# Placement Editor Framework Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a user-friendly robust "Easy Build" Placement Editor Framework consisting of a central Hub window, clean Inspectors, and a quick-start scene setup wizard.

**Architecture:** We will create custom Editor scripts inside `Assets/Scripts/PlacementSystem/Editor`. We will use standard Unity UI Toolkit or IMGUI (preferably IMGUI for simplicity and backwards compatibility unless specified) to build the Hub window, and `Editor` inheritance with `CustomEditor` attributes for the Inspectors. Sockets will be manipulated in the scene view via `OnSceneGUI`.

**Tech Stack:** Unity Editor Scripting, C#, IMGUI / `UnityEditor.Editor`.

---

### Task 1: "Quick Start" Scene Setup Wizard

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Editor/PlacementSetupWizard.cs`
- Test: `Assets/Tests/PlacementSystem/EditorTests/PlacementSetupWizardTests.cs`

**Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.EditorTests
{
    public class PlacementSetupWizardTests
    {
        [Test]
        public void AddToScene_CreatesPlacementManagerWithComponents()
        {
            var go = new GameObject("Placement Manager");
            var controller = go.AddComponent<PlacementController>();
            Assert.IsNotNull(controller);
            Object.DestroyImmediate(go);
        }
    }
}
```

**Step 2: Run test to verify it fails**
Run: Unity Test Runner or equivalent command
Expected: FAIL if folders don't compile.

**Step 3: Write minimal implementation**

```csharp
using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Editor
{
    public static class PlacementSetupWizard
    {
        [MenuItem("Tools/Placement System/Add To Scene")]
        public static void SetupScene()
        {
            var go = new GameObject("Placement Manager");
            go.AddComponent<PlacementController>();
            Selection.activeGameObject = go;
            Debug.Log("Placement System setup complete!");
        }
    }
}
```

**Step 4: Run test to verify it passes**
Run: Unity Test Runner
Expected: PASS

**Step 5: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Editor/PlacementSetupWizard.cs Assets/Tests/PlacementSystem/EditorTests/PlacementSetupWizardTests.cs
git commit -m "feat(editor): add quick start scene setup wizard"
```

---

### Task 2: Part Custom Inspector (Summary View)

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Editor/PartEditor.cs`
- Test: `Assets/Tests/PlacementSystem/EditorTests/PartEditorTests.cs`

**Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.EditorTests
{
    public class PartEditorTests
    {
        [Test]
        public void CustomEditorExistsForPart()
        {
            var go = new GameObject("TestObj");
            var placeable = go.AddComponent<Part>();
            var editor = UnityEditor.Editor.CreateEditor(placeable);
            Assert.AreEqual("PartEditor", editor.GetType().Name);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(editor);
        }
    }
}
```

**Step 2: Run test to verify it fails**
Expected: FAIL (Type name is not PartEditor)

**Step 3: Write minimal implementation**

```csharp
using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Editor
{
    [CustomEditor(typeof(Part))]
    public class PartEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var placeable = (Part)target;
            EditorGUILayout.HelpBox($"Rules: {placeable.GetRules().Count}", MessageType.Info);
            
            if (GUILayout.Button("Open in Prefab Creator", GUILayout.Height(30)))
            {
                // To be linked to the hub window later
            }
            DrawDefaultInspector();
        }
    }
}
```

**Step 4: Run test to verify it passes**
Expected: PASS

**Step 5: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Editor/PartEditor.cs Assets/Tests/PlacementSystem/EditorTests/PartEditorTests.cs
git commit -m "feat(editor): add Part custom inspector"
```

---

### Task 3: Base Prefab Creator Hub Window

**Files:**
- Create: `Assets/Scripts/PlacementSystem/Editor/PrefabCreatorWindow.cs`
- Test: `Assets/Tests/PlacementSystem/EditorTests/PrefabCreatorWindowTests.cs`

**Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEditor;

namespace Systems.PlacementSystem.EditorTests
{
    public class PrefabCreatorWindowTests
    {
        [Test]
        public void WindowCanBeOpened()
        {
            var window = EditorWindow.GetWindow(typeof(Systems.PlacementSystem.Editor.PrefabCreatorWindow));
            Assert.IsNotNull(window);
            window.Close();
        }
    }
}
```

**Step 2: Run test to verify it fails**
Expected: FAIL (Type not found)

**Step 3: Write minimal implementation**

```csharp
using UnityEditor;
using UnityEngine;

namespace Systems.PlacementSystem.Editor
{
    public class PrefabCreatorWindow : EditorWindow
    {
        [MenuItem("Window/Placement System/Prefab Creator")]
        public static void ShowWindow()
        {
            GetWindow<PrefabCreatorWindow>("Prefab Creator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Placement Asset Hub", EditorStyles.boldLabel);
        }
    }
}
```

**Step 4: Run test to verify it passes**
Expected: PASS

**Step 5: Commit**
```bash
git add Assets/Scripts/PlacementSystem/Editor/PrefabCreatorWindow.cs Assets/Tests/PlacementSystem/EditorTests/PrefabCreatorWindowTests.cs
git commit -m "feat(editor): create base Prefab Creator Hub window"
```
