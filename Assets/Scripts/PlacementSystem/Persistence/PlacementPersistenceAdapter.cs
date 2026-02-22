using System;
using Systems.Persistence.Core;
using Systems.PlacementSystem.Core;
using UnityEngine;

namespace Systems.PlacementSystem.Persistence
{
  public class PlacementPersistenceAdapter : ISubsystemPersistence
  {
    readonly PlacementController _placementController;
    readonly string _sceneName;

    public PlacementPersistenceAdapter(PlacementController placementController, string sceneName = null)
    {
      _placementController = placementController;
      _sceneName = string.IsNullOrEmpty(sceneName) ? "scene" : sceneName;
    }

    public string Namespace => $"placement_{_sceneName}";

    public Type DataType => typeof(PlacementSceneData);

    public object GetSaveData()
    {
      var data = new PlacementSceneData
      {
        sceneName = _sceneName
      };

      if (_placementController == null) return data;

      foreach (var go in _placementController.GetPlacedObjects())
      {
        if (go == null) continue;
        var info = go.GetComponent<PlacedObjectInfo>();
        var record = new PlacedObjectRecord
        {
          id = info != null && !string.IsNullOrEmpty(info.Id) ? info.Id : Guid.NewGuid().ToString(),
          prefabName = info != null ? info.PrefabName : go.name,
          position = go.transform.position,
          rotation = go.transform.eulerAngles,
          scale = go.transform.localScale,
          customName = go.name
        };
        data.placedObjects.Add(record);
      }

      return data;
    }

    public void LoadData(object data)
    {
      if (data == null) return;
      var sceneData = data as PlacementSceneData;
      if (sceneData == null) return;

      if (_placementController == null) return;

      _placementController.ClearPlacedObjects(true);

      if (sceneData.placedObjects == null) return;

      foreach (var rec in sceneData.placedObjects)
      {
        try
        {
          _placementController.PlaceObjectFromRecord(rec);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"PlacementPersistenceAdapter: failed to place object '{rec.prefabName}': {ex.Message}");
        }
      }
    }
  }
}
