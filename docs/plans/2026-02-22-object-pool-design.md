# Systems.ObjectPool Design

## Overview
A lightweight, static facade for Unity's native `UnityEngine.Pool.ObjectPool` to provide a zero-allocation, easy-to-use pooling solution that supports "namespaces" for grouping and managing different categories of objects.

## Goals
- **High Performance:** Use Unity 2021+ native `ObjectPool<T>` under the hood.
- **Extremely Simple API:** A static class allows fetching and releasing anywhere without needing DI or singletons.
- **Categorization:** Support namespaces (e.g., "vfx", "enemies", "default") to allow clearing specific groups of pools at once (like clearing all particle effects when a level ends).
- **Asset Store Ready:** Clean code, documented, with a custom Editor window for visualization.
- **No Boilerplate:** Automatically track which pool an instance came from so `ObjectPool.Release(instance)` just works without the caller needing to know the pool or namespace.

## Architecture

### `Systems.ObjectPool` (Static Class)
The main entry point for all pooling operations.
```csharp
public static class ObjectPool
{
    // Simplified usage (uses "default" namespace)
    public static GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);
    
    // Release an instance back to its originating pool (auto-resolved via PooledInstance)
    public static void Release(GameObject instance);

    // Namespace access
    public static IPoolNamespace Namespace(string name = "default");
    
    // Management
    public static void ClearNamespace(string name);
    public static void ClearAll();
}
```

### `IPoolNamespace` / `PoolNamespace`
Represents a group of object pools under a specific string ID.
```csharp
public interface IPoolNamespace
{
    GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);
    void Release(GameObject instance);
    void Clear();
}
```

### `PooledInstance` (MonoBehaviour)
Automatically attached to spawned GameObjects to track their native pool. 
When an object is disabled (or via explicit call to `ObjectPool.Release`), this component puts the instance back into its `IObjectPool<GameObject>`.

### Editor Tooling
A custom `EditorWindow` (e.g., `ObjectPoolDebugger`) that visualizes the current namespaces, their registered prefabs, and the sizes of their active/inactive pools.

## Memory & Hierarchy Management
- Spawning a new prefab for the first time creates an underlying `UnityEngine.Pool.ObjectPool`.
- Pooled, inactive objects are parented to a "Pool Root" GameObject to avoid cluttering the scene hierarchy.
- The user can optionally override the parent transform during `Spawn`.

## Open Questions/Decisions
- **Scene Changes:** Should `ClearAll()` be called automatically on scene load? (Decision: Yes, providing a manual opt-out or cleanup hook).
