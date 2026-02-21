using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Systems.Persistence
{
  public abstract class BaseDataService : ScriptableObject, IDataService
  {
    public abstract void Delete(string name);
    public abstract void DeleteAll();
    public abstract void DeleteFile(string saveName, string ns);
    public abstract IEnumerable<string> ListFiles(string saveName);
    public abstract IEnumerable<string> ListSaves();
    public abstract GameData Load(string name);
    public abstract T Load<T>(string saveName, string ns);
    public abstract void Save(GameData data, bool overwrite = true);
    public abstract void Save<T>(T data, string saveName, string ns, bool overwrite = true);

    // Optional runtime setup hook for data services that require a serializer or other runtime-only
    // initialization. File-backed implementations use this to receive an `ISerializer` instance.
    [Header("Serializer")]
    [SerializeField]
    protected BaseSerializer serializer;

    // Default Setup performs any runtime initialization for the data service and ensures
    // a serializer exists (using the serialized `serializerAsset` or a runtime fallback).
    public virtual void Setup()
    {
      EnsureSerializerInitialized();
    }

    protected void EnsureSerializerInitialized()
    {
      if (serializer != null) return;

      // Fallback to runtime JSON serializer to avoid null-serializer errors at runtime.
      try
      {
        serializer = new JsonSerializer();
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"Failed to create fallback JsonSerializer: {ex.Message}");
      }
    }

    protected virtual void OnEnable()
    {
      if (!Application.isPlaying) return;
      try
      {
        EnsureSerializerInitialized();
        Setup();
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"BaseDataService.Setup() failed: {ex.Message}");
      }
    }
  }
}
