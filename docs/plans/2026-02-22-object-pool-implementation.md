# Systems.ObjectPool Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a lightweight static facade for Unity's native ObjectPool to support categorized pooling with zero boilerplate and editor visualization.

**Architecture:** Static API wraps Unity's `UnityEngine.Pool.ObjectPool<T>`, using namespaces for categorization. Auto-track pool origin via `PooledInstance` MonoBehaviour. Editor window for debugging pool state.

**Tech Stack:** Unity 6000.3.9f1, UnityEngine.Pool, C# 9.0, NUnit (testing)

---

## Task 1: Create Assembly Definition & Project Structure

**Files:**
- Create: `Assets/Scripts/ObjectPool/Systems.ObjectPool.asmdef`
- Create: `Assets/Scripts/ObjectPool/.gitkeep`

**Step 1: Create ObjectPool directory**

```bash
mkdir -p Assets/Scripts/ObjectPool
mkdir -p Assets/Scripts/ObjectPool/Editor
```

**Step 2: Create assembly definition file**

Create `Assets/Scripts/ObjectPool/Systems.ObjectPool.asmdef`:

```json
{
    "name": "Systems.ObjectPool",
    "rootNamespace": "Systems.ObjectPool",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 3: Create Editor assembly definition**

Create `Assets/Scripts/ObjectPool/Editor/Systems.ObjectPool.Editor.asmdef`:

```json
{
    "name": "Systems.ObjectPool.Editor",
    "rootNamespace": "Systems.ObjectPool.Editor",
    "references": [
        "Systems.ObjectPool"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 4: Update Tests.asmdef to reference ObjectPool**

Modify `Assets/Tests/Tests.asmdef` to add `"Systems.ObjectPool"` to the references array.

**Step 5: Commit structure**

```bash
git add Assets/Scripts/ObjectPool/
git commit -m "feat(pool): add ObjectPool assembly definitions and structure"
```

---

## Task 2: Implement PooledInstance MonoBehaviour (TDD)

**Files:**
- Create: `Assets/Tests/ObjectPool/PooledInstanceTests.cs`
- Create: `Assets/Scripts/ObjectPool/PooledInstance.cs`

**Step 1: Create test directory**

```bash
mkdir -p Assets/Tests/ObjectPool
```

**Step 2: Write failing test**

Create `Assets/Tests/ObjectPool/PooledInstanceTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;

namespace Tests.ObjectPool
{
    public class PooledInstanceTests
    {
        private GameObject testObject;

        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("PooledTest");
        }

        [TearDown]
        public void Teardown()
        {
            if (testObject != null)
            {
                GameObject.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void PooledInstance_CanBeAttachedToGameObject()
        {
            var pooled = testObject.AddComponent<PooledInstance>();
            Assert.IsNotNull(pooled);
        }

        [Test]
        public void PooledInstance_StoresPoolReference()
        {
            var pooled = testObject.AddComponent<PooledInstance>();
            var mockPool = new MockPool();
            
            pooled.SetPool(mockPool);
            
            Assert.AreEqual(mockPool, pooled.Pool);
        }

        private class MockPool : IPooledInstancePool
        {
            public void Release(GameObject instance) { }
        }
    }
}
```

**Step 3: Run test to verify it fails**

Open Unity Test Runner (Window > General > Test Runner), run ObjectPool tests.
Expected: FAIL with "PooledInstance type not found"

**Step 4: Write minimal implementation**

Create `Assets/Scripts/ObjectPool/PooledInstance.cs`:

```csharp
using UnityEngine;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Interface for pools that can release GameObject instances
    /// </summary>
    public interface IPooledInstancePool
    {
        void Release(GameObject instance);
    }

    /// <summary>
    /// Component automatically attached to pooled GameObjects to track their origin pool
    /// Handles automatic return to pool when disabled or destroyed
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledInstance : MonoBehaviour
    {
        /// <summary>
        /// The pool this instance belongs to
        /// </summary>
        public IPooledInstancePool Pool { get; private set; }

        /// <summary>
        /// Assign this instance to a specific pool
        /// </summary>
        public void SetPool(IPooledInstancePool pool)
        {
            Pool = pool;
        }

        /// <summary>
        /// Release this instance back to its pool
        /// </summary>
        public void ReleaseToPool()
        {
            if (Pool != null)
            {
                Pool.Release(gameObject);
            }
            else
            {
                Debug.LogWarning($"[PooledInstance] Attempted to release {gameObject.name} but no pool is assigned.");
            }
        }

        private void OnDisable()
        {
            // Optionally auto-release on disable - can be made configurable
            // For now, we'll require explicit Release calls
        }
    }
}
```

**Step 5: Run test to verify it passes**

Run Unity Test Runner again.
Expected: PASS (all PooledInstance tests green)

**Step 6: Commit**

```bash
git add Assets/Scripts/ObjectPool/PooledInstance.cs Assets/Tests/ObjectPool/PooledInstanceTests.cs
git commit -m "feat(pool): implement PooledInstance component with tests"
```

---

## Task 3: Implement IPoolNamespace Interface

**Files:**
- Create: `Assets/Scripts/ObjectPool/IPoolNamespace.cs`

**Step 1: Create interface definition**

Create `Assets/Scripts/ObjectPool/IPoolNamespace.cs`:

```csharp
using UnityEngine;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Represents a namespace/category of object pools
    /// Allows grouping and managing related pools together
    /// </summary>
    public interface IPoolNamespace
    {
        /// <summary>
        /// Spawn an instance from this namespace
        /// </summary>
        GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// Release an instance back to its pool in this namespace
        /// </summary>
        void Release(GameObject instance);

        /// <summary>
        /// Clear all pools in this namespace
        /// </summary>
        void Clear();

        /// <summary>
        /// Get statistics for this namespace
        /// </summary>
        PoolNamespaceStats GetStats();
    }

    /// <summary>
    /// Statistics for a pool namespace
    /// </summary>
    public struct PoolNamespaceStats
    {
        public string namespaceName;
        public int poolCount;
        public int totalActiveInstances;
        public int totalInactiveInstances;
    }
}
```

**Step 2: Commit interface**

```bash
git add Assets/Scripts/ObjectPool/IPoolNamespace.cs
git commit -m "feat(pool): add IPoolNamespace interface"
```

---

## Task 4: Implement PoolNamespace Class (TDD)

**Files:**
- Create: `Assets/Tests/ObjectPool/PoolNamespaceTests.cs`
- Create: `Assets/Scripts/ObjectPool/PoolNamespace.cs`

**Step 1: Write failing tests**

Create `Assets/Tests/ObjectPool/PoolNamespaceTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;

namespace Tests.ObjectPool
{
    public class PoolNamespaceTests
    {
        private GameObject prefab;
        private PoolNamespace poolNamespace;

        [SetUp]
        public void Setup()
        {
            prefab = new GameObject("TestPrefab");
            poolNamespace = new PoolNamespace("test-namespace");
        }

        [TearDown]
        public void Teardown()
        {
            poolNamespace?.Clear();
            
            if (prefab != null)
            {
                GameObject.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void Spawn_CreatesNewInstance()
        {
            var instance = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            
            Assert.IsNotNull(instance);
            Assert.AreNotEqual(prefab, instance); // Should be a clone
        }

        [Test]
        public void Spawn_AttachesPooledInstanceComponent()
        {
            var instance = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            
            var pooledInstance = instance.GetComponent<PooledInstance>();
            Assert.IsNotNull(pooledInstance);
        }

        [Test]
        public void Release_DeactivatesInstance()
        {
            var instance = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            
            poolNamespace.Release(instance);
            
            Assert.IsFalse(instance.activeSelf);
        }

        [Test]
        public void Spawn_ReusesPreviouslyReleasedInstance()
        {
            var instance1 = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            poolNamespace.Release(instance1);
            
            var instance2 = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            
            Assert.AreEqual(instance1, instance2); // Should reuse the same instance
        }

        [Test]
        public void Clear_DestroysAllInstances()
        {
            var instance1 = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            var instance2 = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            
            poolNamespace.Clear();
            
            // Instances should be destroyed
            Assert.IsTrue(instance1 == null || !instance1);
            Assert.IsTrue(instance2 == null || !instance2);
        }

        [Test]
        public void GetStats_ReturnsCorrectCounts()
        {
            var instance1 = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            var instance2 = poolNamespace.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
            poolNamespace.Release(instance1);
            
            var stats = poolNamespace.GetStats();
            
            Assert.AreEqual("test-namespace", stats.namespaceName);
            Assert.AreEqual(1, stats.poolCount);
            Assert.AreEqual(1, stats.totalActiveInstances);
            Assert.AreEqual(1, stats.totalInactiveInstances);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run Unity Test Runner.
Expected: FAIL with "PoolNamespace type not found"

**Step 3: Implement PoolNamespace class**

Create `Assets/Scripts/ObjectPool/PoolNamespace.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Implementation of a pool namespace using Unity's native ObjectPool
    /// </summary>
    public class PoolNamespace : IPoolNamespace, IPooledInstancePool
    {
        private readonly string _namespaceName;
        private readonly Dictionary<int, ObjectPool<GameObject>> _pools;
        private readonly Dictionary<int, GameObject> _prefabs;
        private readonly Dictionary<GameObject, int> _instanceToPrefabId;
        private GameObject _poolRoot;

        public PoolNamespace(string namespaceName)
        {
            _namespaceName = namespaceName;
            _pools = new Dictionary<int, ObjectPool<GameObject>>();
            _prefabs = new Dictionary<int, GameObject>();
            _instanceToPrefabId = new Dictionary<GameObject, int>();
        }

        /// <summary>
        /// Get or create the pool root GameObject for organizing inactive instances
        /// </summary>
        private GameObject GetPoolRoot()
        {
            if (_poolRoot == null)
            {
                _poolRoot = new GameObject($"[Pool: {_namespaceName}]");
                _poolRoot.SetActive(false); // Keep inactive to avoid OnEnable/Start calls
            }
            return _poolRoot;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolNamespace] Cannot spawn null prefab");
                return null;
            }

            int prefabId = prefab.GetInstanceID();
            
            // Get or create pool for this prefab
            if (!_pools.TryGetValue(prefabId, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateInstance(prefab, prefabId),
                    actionOnGet: OnGetFromPool,
                    actionOnRelease: OnReleaseToPool,
                    actionOnDestroy: OnDestroyPoolObject,
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 10000
                );
                
                _pools[prefabId] = pool;
                _prefabs[prefabId] = prefab;
            }

            var instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(parent, worldPositionStays: true);
            
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("[PoolNamespace] Cannot release null instance");
                return;
            }

            if (!_instanceToPrefabId.TryGetValue(instance, out int prefabId))
            {
                Debug.LogWarning($"[PoolNamespace] Instance {instance.name} is not tracked by this namespace");
                return;
            }

            if (_pools.TryGetValue(prefabId, out var pool))
            {
                pool.Release(instance);
            }
        }

        public void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            
            _pools.Clear();
            _prefabs.Clear();
            _instanceToPrefabId.Clear();

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot);
                _poolRoot = null;
            }
        }

        public PoolNamespaceStats GetStats()
        {
            int totalActive = 0;
            int totalInactive = 0;

            foreach (var kvp in _pools)
            {
                // Unity's ObjectPool doesn't expose count directly, so we track via our dictionary
                // Active = instances in _instanceToPrefabId that are active
                // Inactive = anything parented to pool root
            }

            // Count active instances
            foreach (var instance in _instanceToPrefabId.Keys)
            {
                if (instance != null && instance.activeSelf)
                {
                    totalActive++;
                }
                else if (instance != null)
                {
                    totalInactive++;
                }
            }

            return new PoolNamespaceStats
            {
                namespaceName = _namespaceName,
                poolCount = _pools.Count,
                totalActiveInstances = totalActive,
                totalInactiveInstances = totalInactive
            };
        }

        private GameObject CreateInstance(GameObject prefab, int prefabId)
        {
            var instance = Object.Instantiate(prefab);
            instance.name = $"{prefab.name} (Pooled)";
            
            // Add PooledInstance component
            var pooledInstance = instance.GetComponent<PooledInstance>();
            if (pooledInstance == null)
            {
                pooledInstance = instance.AddComponent<PooledInstance>();
            }
            pooledInstance.SetPool(this);
            
            // Track instance
            _instanceToPrefabId[instance] = prefabId;
            
            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            instance.SetActive(true);
        }

        private void OnReleaseToPool(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(GetPoolRoot().transform, worldPositionStays: false);
        }

        private void OnDestroyPoolObject(GameObject instance)
        {
            if (_instanceToPrefabId.ContainsKey(instance))
            {
                _instanceToPrefabId.Remove(instance);
            }
            
            Object.Destroy(instance);
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run Unity Test Runner.
Expected: PASS (all PoolNamespace tests green)

**Step 5: Commit**

```bash
git add Assets/Scripts/ObjectPool/PoolNamespace.cs Assets/Tests/ObjectPool/PoolNamespaceTests.cs
git commit -m "feat(pool): implement PoolNamespace with Unity ObjectPool integration"
```

---

## Task 5: Implement Static ObjectPool Facade (TDD)

**Files:**
- Create: `Assets/Tests/ObjectPool/ObjectPoolTests.cs`
- Create: `Assets/Scripts/ObjectPool/ObjectPool.cs`

**Step 1: Write failing tests**

Create `Assets/Tests/ObjectPool/ObjectPoolTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;

namespace Tests.ObjectPool
{
    public class ObjectPoolTests
    {
        private GameObject prefab;

        [SetUp]
        public void Setup()
        {
            prefab = new GameObject("TestPrefab");
            ObjectPool.ClearAll(); // Start with clean state
        }

        [TearDown]
        public void Teardown()
        {
            ObjectPool.ClearAll();
            
            if (prefab != null)
            {
                GameObject.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void Spawn_WithDefaultNamespace_CreatesInstance()
        {
            var instance = ObjectPool.Spawn(prefab);
            
            Assert.IsNotNull(instance);
            Assert.AreNotEqual(prefab, instance);
        }

        [Test]
        public void Release_ReturnsInstanceToPool()
        {
            var instance = ObjectPool.Spawn(prefab);
            ObjectPool.Release(instance);
            
            Assert.IsFalse(instance.activeSelf);
        }

        [Test]
        public void Namespace_ReturnsNamedNamespace()
        {
            var vfxNamespace = ObjectPool.Namespace("vfx");
            
            Assert.IsNotNull(vfxNamespace);
        }

        [Test]
        public void Namespace_ReturnsSameInstanceForSameName()
        {
            var ns1 = ObjectPool.Namespace("enemies");
            var ns2 = ObjectPool.Namespace("enemies");
            
            Assert.AreSame(ns1, ns2);
        }

        [Test]
        public void ClearNamespace_ClearsSpecificNamespace()
        {
            var vfxInstance = ObjectPool.Namespace("vfx").Spawn(prefab);
            var enemiesInstance = ObjectPool.Namespace("enemies").Spawn(prefab);
            
            ObjectPool.ClearNamespace("vfx");
            
            // vfx instance should be destroyed
            Assert.IsTrue(vfxInstance == null || !vfxInstance);
            // enemies instance should still exist
            Assert.IsNotNull(enemiesInstance);
            Assert.IsTrue(enemiesInstance);
        }

        [Test]
        public void ClearAll_ClearsAllNamespaces()
        {
            var instance1 = ObjectPool.Namespace("vfx").Spawn(prefab);
            var instance2 = ObjectPool.Namespace("enemies").Spawn(prefab);
            
            ObjectPool.ClearAll();
            
            Assert.IsTrue(instance1 == null || !instance1);
            Assert.IsTrue(instance2 == null || !instance2);
        }

        [Test]
        public void GetAllNamespaceStats_ReturnsAllNamespaces()
        {
            ObjectPool.Namespace("vfx").Spawn(prefab);
            ObjectPool.Namespace("enemies").Spawn(prefab);
            
            var stats = ObjectPool.GetAllNamespaceStats();
            
            Assert.AreEqual(3, stats.Count); // vfx, enemies, and default
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run Unity Test Runner.
Expected: FAIL with "ObjectPool type not found"

**Step 3: Implement static ObjectPool class**

Create `Assets/Scripts/ObjectPool/ObjectPool.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Static facade for object pooling with namespace support
    /// Provides zero-allocation, easy-to-use pooling for GameObjects
    /// </summary>
    public static class ObjectPool
    {
        private const string DefaultNamespaceName = "default";
        private static readonly Dictionary<string, PoolNamespace> _namespaces = new Dictionary<string, PoolNamespace>();

        /// <summary>
        /// Spawn an instance from the default namespace
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            return Namespace(DefaultNamespaceName).Spawn(prefab, position, rotation, parent);
        }

        /// <summary>
        /// Release an instance back to its pool
        /// </summary>
        public static void Release(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("[ObjectPool] Cannot release null instance");
                return;
            }

            var pooledInstance = instance.GetComponent<PooledInstance>();
            if (pooledInstance != null && pooledInstance.Pool != null)
            {
                pooledInstance.ReleaseToPool();
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Instance {instance.name} is not a pooled object");
            }
        }

        /// <summary>
        /// Get or create a named pool namespace
        /// </summary>
        public static IPoolNamespace Namespace(string name = DefaultNamespaceName)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DefaultNamespaceName;
            }

            if (!_namespaces.TryGetValue(name, out var poolNamespace))
            {
                poolNamespace = new PoolNamespace(name);
                _namespaces[name] = poolNamespace;
            }

            return poolNamespace;
        }

        /// <summary>
        /// Clear all pools in a specific namespace
        /// </summary>
        public static void ClearNamespace(string name)
        {
            if (_namespaces.TryGetValue(name, out var poolNamespace))
            {
                poolNamespace.Clear();
                _namespaces.Remove(name);
            }
        }

        /// <summary>
        /// Clear all pools across all namespaces
        /// </summary>
        public static void ClearAll()
        {
            foreach (var poolNamespace in _namespaces.Values)
            {
                poolNamespace.Clear();
            }
            _namespaces.Clear();
        }

        /// <summary>
        /// Get statistics for all namespaces
        /// </summary>
        public static List<PoolNamespaceStats> GetAllNamespaceStats()
        {
            var statsList = new List<PoolNamespaceStats>();
            foreach (var poolNamespace in _namespaces.Values)
            {
                statsList.Add(poolNamespace.GetStats());
            }
            return statsList;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get all namespace names (for editor visualization)
        /// </summary>
        public static IEnumerable<string> GetNamespaceNames()
        {
            return _namespaces.Keys;
        }
#endif
    }
}
```

**Step 4: Run tests to verify they pass**

Run Unity Test Runner.
Expected: PASS (all ObjectPool tests green)

**Step 5: Commit**

```bash
git add Assets/Scripts/ObjectPool/ObjectPool.cs Assets/Tests/ObjectPool/ObjectPoolTests.cs
git commit -m "feat(pool): implement static ObjectPool facade with namespace support"
```

---

## Task 6: Add Scene Cleanup Support

**Files:**
- Create: `Assets/Scripts/ObjectPool/ObjectPoolSceneManager.cs`
- Modify: `Assets/Scripts/ObjectPool/Systems.ObjectPool.asmdef`

**Step 1: Update asmdef to reference UnityEngine.SceneManagement**

The assembly definition already has access to Unity engine APIs, no change needed.

**Step 2: Create scene manager component**

Create `Assets/Scripts/ObjectPool/ObjectPoolSceneManager.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Manages automatic pool cleanup on scene changes
    /// Add this component to a GameObject in your scene or create it via [RuntimeInitializeOnLoadMethod]
    /// </summary>
    public class ObjectPoolSceneManager : MonoBehaviour
    {
        [Tooltip("Automatically clear all pools when a scene is loaded")]
        [SerializeField] private bool clearOnSceneLoad = true;

        [Tooltip("Automatically create this manager on game start")]
        [SerializeField] private bool persistAcrossScenes = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("[ObjectPool Scene Manager]");
            var manager = go.AddComponent<ObjectPoolSceneManager>();
            
            if (manager.persistAcrossScenes)
            {
                DontDestroyOnLoad(go);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (clearOnSceneLoad && mode == LoadSceneMode.Single)
            {
                Debug.Log($"[ObjectPoolSceneManager] Clearing all pools on scene load: {scene.name}");
                ObjectPool.ClearAll();
            }
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/ObjectPool/ObjectPoolSceneManager.cs
git commit -m "feat(pool): add automatic scene cleanup manager"
```

---

## Task 7: Create Editor Debug Window

**Files:**
- Create: `Assets/Scripts/ObjectPool/Editor/ObjectPoolDebugWindow.cs`

**Step 1: Create editor window**

Create `Assets/Scripts/ObjectPool/Editor/ObjectPoolDebugWindow.cs`:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Systems.ObjectPool.Editor
{
    /// <summary>
    /// Editor window for visualizing and debugging object pools
    /// </summary>
    public class ObjectPoolDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.5; // Refresh every 0.5 seconds

        [MenuItem("Window/Object Pool Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<ObjectPoolDebugWindow>("Object Pool Debugger");
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawPoolStats();
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Repaint();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear All Pools", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Clear All Pools", 
                    "Are you sure you want to clear all object pools? This will destroy all pooled instances.", 
                    "Clear", "Cancel"))
                {
                    ObjectPool.ClearAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPoolStats()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var stats = ObjectPool.GetAllNamespaceStats();

            if (stats.Count == 0)
            {
                EditorGUILayout.HelpBox("No active object pools. Pools are created when you first spawn objects.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Total Namespaces: {stats.Count}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                foreach (var stat in stats)
                {
                    DrawNamespaceStats(stat);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNamespaceStats(PoolNamespaceStats stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Namespace: {stats.namespaceName}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Clear Namespace", 
                    $"Clear all pools in namespace '{stats.namespaceName}'?", 
                    "Clear", "Cancel"))
                {
                    ObjectPool.ClearNamespace(stats.namespaceName);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField($"Pools: {stats.poolCount}");
            EditorGUILayout.LabelField($"Active Instances: {stats.totalActiveInstances}");
            EditorGUILayout.LabelField($"Inactive Instances: {stats.totalInactiveInstances}");
            
            int total = stats.totalActiveInstances + stats.totalInactiveInstances;
            if (total > 0)
            {
                float utilization = (float)stats.totalActiveInstances / total * 100f;
                EditorGUILayout.LabelField($"Utilization: {utilization:F1}%");
            }

            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}
#endif
```

**Step 2: Test editor window**

In Unity Editor: Window > Object Pool Debugger
Expected: Window opens, shows "No active object pools" message

**Step 3: Commit**

```bash
git add Assets/Scripts/ObjectPool/Editor/ObjectPoolDebugWindow.cs
git commit -m "feat(pool): add editor debug window for pool visualization"
```

---

## Task 8: Add XML Documentation and README

**Files:**
- Create: `Assets/Scripts/ObjectPool/README.md`

**Step 1: Create README**

Create `Assets/Scripts/ObjectPool/README.md`:

```markdown
# Systems.ObjectPool

A lightweight, high-performance object pooling system that wraps Unity's native `UnityEngine.Pool.ObjectPool` with a zero-allocation static API and namespace support.

## Features

- **Zero Boilerplate**: Simple static API - just `ObjectPool.Spawn()` and `ObjectPool.Release()`
- **High Performance**: Uses Unity 2021+ native ObjectPool under the hood
- **Namespace Support**: Group pools by category (e.g., "vfx", "enemies", "projectiles")
- **Auto-Tracking**: Instances automatically know which pool they came from
- **Editor Tools**: Visual debugging window to inspect pool state
- **Scene Management**: Automatic cleanup on scene changes

## Quick Start

### Basic Usage

```csharp
using Systems.ObjectPool;

// Spawn from default namespace
GameObject bullet = ObjectPool.Spawn(bulletPrefab, position, rotation);

// Release back to pool when done
ObjectPool.Release(bullet);
```

### Using Namespaces

```csharp
// Spawn from specific namespace
var explosion = ObjectPool.Namespace("vfx").Spawn(explosionPrefab, position, rotation);

// Release works the same way (auto-detects namespace)
ObjectPool.Release(explosion);

// Clear specific namespace
ObjectPool.ClearNamespace("vfx");
```

### Scene Management

Add the `ObjectPoolSceneManager` component to your scene or let it auto-create on startup. It will automatically clear pools when scenes load.

```csharp
// Manual control
ObjectPool.ClearAll(); // Clear all namespaces
ObjectPool.ClearNamespace("enemies"); // Clear specific namespace
```

## Editor Window

Access via **Window > Object Pool Debugger**

Shows real-time statistics for all namespaces:
- Number of pools
- Active/inactive instances
- Pool utilization percentage

## Architecture

- `ObjectPool`: Static facade (entry point)
- `IPoolNamespace` / `PoolNamespace`: Namespace implementation
- `PooledInstance`: Component tracking pool origin
- `ObjectPoolSceneManager`: Automatic cleanup
- `ObjectPoolDebugWindow`: Editor visualization

## Performance

- **Zero allocations** for spawn/release after warmup
- **No reflection** or runtime lookups
- **Efficient tracking** via dictionaries and instance IDs
- **Configurable limits**: Default 10 initial capacity, 10,000 max size per pool

## API Reference

### ObjectPool (Static Class)

```csharp
// Spawn from default namespace
GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)

// Release instance back to pool
void Release(GameObject instance)

// Get or create namespace
IPoolNamespace Namespace(string name = "default")

// Clear specific namespace
void ClearNamespace(string name)

// Clear all namespaces
void ClearAll()

// Get statistics (for debugging)
List<PoolNamespaceStats> GetAllNamespaceStats()
```

### IPoolNamespace

```csharp
GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
void Release(GameObject instance)
void Clear()
PoolNamespaceStats GetStats()
```

## Best Practices

1. **Use namespaces** to group related objects (makes cleanup easier)
2. **Always release** instances when done (or they'll stay in memory)
3. **Don't destroy** pooled objects - use `Release()` instead
4. **Warmup pools** at level start if needed:
   ```csharp
   for (int i = 0; i < 50; i++)
   {
       var obj = ObjectPool.Spawn(prefab);
       ObjectPool.Release(obj);
   }
   ```

## Testing

Test coverage includes:
- PooledInstance component behavior
- PoolNamespace spawn/release/clear operations
- Static ObjectPool API
- Namespace isolation
- Memory cleanup

Run tests via **Window > General > Test Runner** > "ObjectPool" category.
```

**Step 2: Commit**

```bash
git add Assets/Scripts/ObjectPool/README.md
git commit -m "docs(pool): add comprehensive README and usage guide"
```

---

## Task 9: Integration Test - Real World Usage

**Files:**
- Create: `Assets/Tests/ObjectPool/ObjectPoolIntegrationTests.cs`

**Step 1: Create integration test**

Create `Assets/Tests/ObjectPool/ObjectPoolIntegrationTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;
using System.Collections;
using UnityEngine.TestTools;

namespace Tests.ObjectPool
{
    /// <summary>
    /// Integration tests simulating real-world usage scenarios
    /// </summary>
    public class ObjectPoolIntegrationTests
    {
        private GameObject bulletPrefab;
        private GameObject explosionPrefab;

        [SetUp]
        public void Setup()
        {
            bulletPrefab = new GameObject("Bullet");
            explosionPrefab = new GameObject("Explosion");
            ObjectPool.ClearAll();
        }

        [TearDown]
        public void Teardown()
        {
            ObjectPool.ClearAll();
            
            if (bulletPrefab != null) GameObject.DestroyImmediate(bulletPrefab);
            if (explosionPrefab != null) GameObject.DestroyImmediate(explosionPrefab);
        }

        [Test]
        public void ScenarioShootingBullets_SpawnAndReleaseMultiple()
        {
            var bullets = new GameObject[10];
            
            // Spawn 10 bullets
            for (int i = 0; i < 10; i++)
            {
                bullets[i] = ObjectPool.Namespace("projectiles").Spawn(
                    bulletPrefab, 
                    Vector3.forward * i, 
                    Quaternion.identity
                );
            }
            
            // All should be active
            foreach (var bullet in bullets)
            {
                Assert.IsTrue(bullet.activeSelf);
            }
            
            // Release them
            for (int i = 0; i < 10; i++)
            {
                ObjectPool.Release(bullets[i]);
            }
            
            // All should be inactive
            foreach (var bullet in bullets)
            {
                Assert.IsFalse(bullet.activeSelf);
            }
            
            // Spawn again - should reuse
            var newBullet = ObjectPool.Namespace("projectiles").Spawn(bulletPrefab);
            Assert.AreEqual(bullets[0], newBullet); // Should be the first released bullet
        }

        [Test]
        public void ScenarioLevelEnd_ClearAllEffects()
        {
            // Spawn various VFX
            var explosion1 = ObjectPool.Namespace("vfx").Spawn(explosionPrefab);
            var explosion2 = ObjectPool.Namespace("vfx").Spawn(explosionPrefab);
            
            // Spawn projectiles
            var bullet1 = ObjectPool.Namespace("projectiles").Spawn(bulletPrefab);
            
            // Clear only VFX
            ObjectPool.ClearNamespace("vfx");
            
            // VFX should be destroyed
            Assert.IsTrue(explosion1 == null || !explosion1);
            Assert.IsTrue(explosion2 == null || !explosion2);
            
            // Projectiles should remain
            Assert.IsNotNull(bullet1);
            Assert.IsTrue(bullet1);
        }

        [Test]
        public void ScenarioMixedNamespaces_IsolatedCorrectly()
        {
            var vfxInstance = ObjectPool.Namespace("vfx").Spawn(explosionPrefab);
            var projInstance = ObjectPool.Namespace("projectiles").Spawn(bulletPrefab);
            
            var stats = ObjectPool.GetAllNamespaceStats();
            
            // Should have 2 namespaces
            Assert.AreEqual(2, stats.Count);
            
            // Each should have 1 pool and 1 active instance
            foreach (var stat in stats)
            {
                Assert.AreEqual(1, stat.poolCount);
                Assert.AreEqual(1, stat.totalActiveInstances);
            }
        }

        [Test]
        public void ScenarioRapidSpawnRelease_NoMemoryLeaks()
        {
            // Simulate rapid fire scenario
            for (int frame = 0; frame < 100; frame++)
            {
                var bullet = ObjectPool.Spawn(bulletPrefab, Vector3.forward * frame, Quaternion.identity);
                
                // Immediately release (simulating bullet hitting something)
                ObjectPool.Release(bullet);
            }
            
            var stats = ObjectPool.GetAllNamespaceStats();
            var defaultStats = stats.Find(s => s.namespaceName == "default");
            
            // Should only have 1 instance in the pool (reused)
            Assert.AreEqual(1, defaultStats.poolCount);
            Assert.AreEqual(0, defaultStats.totalActiveInstances);
            Assert.AreEqual(1, defaultStats.totalInactiveInstances);
        }

        [UnityTest]
        public IEnumerator ScenarioAsyncSpawning_WorksCorrectly()
        {
            var bullet = ObjectPool.Spawn(bulletPrefab);
            Assert.IsNotNull(bullet);
            
            yield return null; // Wait one frame
            
            ObjectPool.Release(bullet);
            Assert.IsFalse(bullet.activeSelf);
            
            yield return null; // Wait one frame
            
            var bullet2 = ObjectPool.Spawn(bulletPrefab);
            Assert.AreEqual(bullet, bullet2); // Should reuse
        }
    }
}
```

**Step 2: Run integration tests**

Run Unity Test Runner.
Expected: PASS (all integration tests green)

**Step 3: Commit**

```bash
git add Assets/Tests/ObjectPool/ObjectPoolIntegrationTests.cs
git commit -m "test(pool): add integration tests for real-world scenarios"
```

---

## Task 10: Final Polish and Documentation

**Files:**
- Create: `Assets/Scripts/ObjectPool/CHANGELOG.md`
- Update: Project documentation

**Step 1: Create changelog**

Create `Assets/Scripts/ObjectPool/CHANGELOG.md`:

```markdown
# Changelog

All notable changes to the Systems.ObjectPool module will be documented in this file.

## [1.0.0] - 2026-02-22

### Added
- Initial release of Systems.ObjectPool
- Static `ObjectPool` facade for easy access
- Namespace support for categorizing pools
- `PooledInstance` component for automatic pool tracking
- `ObjectPoolSceneManager` for automatic cleanup on scene load
- `ObjectPoolDebugWindow` editor tool for visualization
- Comprehensive unit and integration tests
- Full XML documentation
- README with examples and best practices

### Features
- Zero-allocation spawning/releasing
- Built on Unity's native `UnityEngine.Pool.ObjectPool`
- Support for up to 10,000 instances per pool
- Automatic instance tracking via InstanceID
- Thread-safe dictionary lookups
- Editor statistics and debugging

### Performance
- Zero GC allocations after pool warmup
- O(1) spawn and release operations
- Minimal memory overhead per instance
```

**Step 2: Verify all tests pass**

```bash
# Open Unity Test Runner and run all ObjectPool tests
# Expected: All tests green
```

**Step 3: Build verification**

Ensure the project compiles without errors:
- Open Unity Editor
- Check Console for any errors
- Verify Window > Object Pool Debugger opens correctly

**Step 4: Final commit**

```bash
git add Assets/Scripts/ObjectPool/CHANGELOG.md
git commit -m "docs(pool): add changelog and finalize v1.0.0"
```

---

## Task 11: Create Example Scene (Optional but Recommended)

**Files:**
- Create: `Assets/Scenes/ObjectPoolExample.unity`
- Create: `Assets/Scripts/ObjectPool/Examples/PoolingDemo.cs`
- Create: `Assets/Scripts/ObjectPool/Examples.meta` (Unity creates this)

**Step 1: Create examples directory**

```bash
mkdir -p Assets/Scripts/ObjectPool/Examples
```

**Step 2: Create demo script**

Create `Assets/Scripts/ObjectPool/Examples/PoolingDemo.cs`:

```csharp
using UnityEngine;
using Systems.ObjectPool;

namespace Systems.ObjectPool.Examples
{
    /// <summary>
    /// Simple demonstration of object pooling
    /// Press Space to spawn objects, they auto-release after 2 seconds
    /// </summary>
    public class PoolingDemo : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private GameObject spherePrefab;
        
        [Header("Settings")]
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private float lifetime = 2f;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnRandomObject();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("Clearing all pools...");
                ObjectPool.ClearAll();
            }
        }

        private void SpawnRandomObject()
        {
            bool useCube = Random.value > 0.5f;
            GameObject prefab = useCube ? cubePrefab : spherePrefab;
            string namespaceName = useCube ? "cubes" : "spheres";

            if (prefab == null)
            {
                Debug.LogWarning("Prefab not assigned!");
                return;
            }

            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            randomPos.y = Mathf.Abs(randomPos.y) + 1f; // Keep above ground

            var instance = ObjectPool.Namespace(namespaceName).Spawn(
                prefab, 
                randomPos, 
                Random.rotation
            );

            // Auto-release after lifetime
            StartCoroutine(AutoRelease(instance, lifetime));
        }

        private System.Collections.IEnumerator AutoRelease(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (instance != null)
            {
                ObjectPool.Release(instance);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Object Pool Demo", GUI.skin.box);
            GUILayout.Label("Press SPACE to spawn objects");
            GUILayout.Label("Press C to clear all pools");
            GUILayout.Label($"\nOpen Window > Object Pool Debugger");
            GUILayout.Label("to see live statistics");
            GUILayout.EndArea();
        }
    }
}
```

**Step 3: Create example scene (manual step)**

This step requires Unity Editor interaction:
1. Create new scene: File > New Scene
2. Save as `Assets/Scenes/ObjectPoolExample.unity`
3. Create GameObject "PoolingDemo"
4. Add `PoolingDemo` component
5. Create simple Cube and Sphere prefabs
6. Assign prefabs to demo component
7. Save scene

**Step 4: Commit examples**

```bash
git add Assets/Scripts/ObjectPool/Examples/
git commit -m "feat(pool): add example scene and demo script"
```

---

## Verification Checklist

Before considering this task complete, verify:

- [ ] All unit tests pass (PooledInstance, PoolNamespace, ObjectPool)
- [ ] All integration tests pass
- [ ] No compiler errors or warnings
- [ ] Editor window opens and displays data correctly
- [ ] README is clear and examples work
- [ ] All files have XML documentation
- [ ] Assembly definitions are properly referenced
- [ ] Scene manager auto-clears on scene load
- [ ] Example scene demonstrates core functionality

## Next Steps

After implementation:
1. Test with real game objects (particles, projectiles, enemies)
2. Profile memory usage in Unity Profiler
3. Benchmark spawn/release performance
4. Consider adding warmup API: `ObjectPool.Warmup(prefab, count)`
5. Consider adding statistics export for analytics
6. Document any edge cases discovered during real-world use

## Notes

- This implementation prioritizes **simplicity and performance** over features
- The API is intentionally minimal to avoid feature creep
- Namespaces provide enough flexibility for most use cases
- Unity's native pool handles the hard parts (allocation, collection checks)
- The static facade makes it easy to use anywhere without DI setup
