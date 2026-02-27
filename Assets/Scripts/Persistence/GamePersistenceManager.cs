using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Persistence.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Systems.Persistence
{
    [Serializable]
    public class SubsystemDataEntry
    {
        public string Key;
        public string JsonData;
    }

    [CreateAssetMenu(menuName = "Persistence/Game Data", fileName = "NewGameData")]
    public class GameData : ScriptableObject
    {
        public List<SubsystemDataEntry> EmbeddedSubsystems = new List<SubsystemDataEntry>();
    }

    public interface ISaveable
    {
        SerializableGuid Id { get; set; }
    }

    public interface IBind<TData> where TData : ISaveable
    {
        SerializableGuid Id { get; set; }
        void Bind(TData data);
    }

    public class GamePersistenceManager : PersistentSingleton<GamePersistenceManager>
    {
        [SerializeField] public GameData gameData;

        [SerializeField]
        BaseDataService dataService;

        // Registered subsystem persistence adapters
        List<ISubsystemPersistence> _subsystems = new List<ISubsystemPersistence>();

        // Current save name in use
        public string CurrentSaveName { get; private set; } = "Default";

        [Header("Autosave")]
        [SerializeField] bool AutoSaveEnabled = false;
        [SerializeField] float AutoSaveIntervalSeconds = 60f;

        [Header("Events")]
        public UnityEvent OnDataLoaded;

        void OnEnable()
        {
            if (AutoSaveEnabled)
            {
                InvokeRepeating(nameof(AutosaveTick), AutoSaveIntervalSeconds, AutoSaveIntervalSeconds);
            }
        }

        void OnDisable()
        {
            CancelInvoke(nameof(AutosaveTick));
        }

        void AutosaveTick()
        {
            try
            {
                SaveAll();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Autosave failed: {ex.Message}");
            }
        }

        void Bind<T, TData>(TData data) where T : MonoBehaviour, IBind<TData> where TData : ISaveable, new()
        {
            var entity = FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault();
            if (entity != null)
            {
                if (data == null)
                {
                    data = new TData { Id = entity.Id };
                }
                entity.Bind(data);
            }
        }

        void Bind<T, TData>(List<TData> datas) where T : MonoBehaviour, IBind<TData> where TData : ISaveable, new()
        {
            var entities = FindObjectsByType<T>(FindObjectsSortMode.None);

            foreach (var entity in entities)
            {
                var data = datas.FirstOrDefault(d => d.Id == entity.Id);
                if (data == null)
                {
                    data = new TData { Id = entity.Id };
                    datas.Add(data);
                }
                entity.Bind(data);
            }
        }

        // Save/Load orchestration
        public void SaveGame()
        {
            // First collect all embedded data into gameData
            UpdateEmbeddedData();

            dataService.Save(gameData, CurrentSaveName);

            // Then save external registered subsystems
            SaveAll();
        }

        private void UpdateEmbeddedData()
        {
            if (gameData == null) return;
            gameData.EmbeddedSubsystems.Clear();

            foreach (var s in _subsystems.Where(x => x.Target == PersistenceTarget.Embedded))
            {
                try
                {
                    var data = s.GetSaveData();
                    if (data != null)
                    {
                        // Use the data service's serializer to convert to JSON for storage
                        var json = JsonUtility.ToJson(data);
                        gameData.EmbeddedSubsystems.Add(new SubsystemDataEntry
                        {
                            Key = s.Namespace,
                            JsonData = json
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to embed subsystem '{s.Namespace}': {ex.Message}");
                }
            }
        }

        public void LoadGame(string gameName)
        {
            gameData = dataService.Load(gameName);
            CurrentSaveName = gameName;

            // Load all subsystem data and trigger events
            LoadAll();
            OnDataLoaded?.Invoke();
        }

        public void NewGame(string name)
        {
            gameData = ScriptableObject.CreateInstance<GameData>();
            CurrentSaveName = name;

            // Initialize with default state if needed
            SaveGame();
        }

        public void ReloadGame()
        {
            if (!string.IsNullOrEmpty(CurrentSaveName)) LoadGame(CurrentSaveName);
        }

        public void DeleteGame(string gameName) => dataService.Delete(gameName);

        // Subsystem registration
        public void RegisterSubsystem(ISubsystemPersistence subsystem)
        {
            if (subsystem == null) return;
            if (!_subsystems.Contains(subsystem)) _subsystems.Add(subsystem);
        }

        public void UnregisterSubsystem(ISubsystemPersistence subsystem)
        {
            if (subsystem == null) return;
            _subsystems.Remove(subsystem);
        }

        // Save all registered EXTERNAL subsystems into the current save folder
        public void SaveAll(string saveName = null)
        {
            string saveTo = saveName ?? CurrentSaveName ?? "Default";
            foreach (var s in _subsystems.Where(x => x.Target == PersistenceTarget.External))
            {
                try
                {
                    var data = s.GetSaveData();
                    if (data != null)
                    {
                        // Use the concrete DataType when saving generically
                        dataService.Save<object>(data, saveTo, s.Namespace, s.PersistentName, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save subsystem '{s.Namespace}': {ex.Message}");
                }
            }
        }

        public void LoadAll(string saveName = null)
        {
            string loadFrom = saveName ?? CurrentSaveName ?? "Default";
            foreach (var s in _subsystems)
            {
                try
                {
                    if (s.Target == PersistenceTarget.Embedded)
                    {
                        var entry = gameData?.EmbeddedSubsystems?.FirstOrDefault(e => e.Key == s.Namespace);
                        if (entry != null && !string.IsNullOrEmpty(entry.JsonData))
                        {
                            var loaded = JsonUtility.FromJson(entry.JsonData, s.DataType);
                            s.LoadData(loaded);
                        }
                    }
                    else
                    {
                        // Attempt to load the file for external systems
                        try
                        {
                            var loaded = dataService.Load<object>(loadFrom, s.Namespace, s.PersistentName);
                            s.LoadData(loaded);
                        }
                        catch (ArgumentException)
                        {
                            // File not found, expected for new projects/scenes
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load subsystem '{s.Namespace}' from save '{loadFrom}': {ex.Message}");
                }
            }
        }

        public IEnumerable<string> ListFiles(string saveName) => dataService.ListFiles(saveName);
        public IEnumerable<string> ListSaves() => dataService.ListSaves();
        public IEnumerable<string> ListSaves(string ns) => dataService.ListSaves(ns);

        // Return saves that contain data for the given subsystem namespace
        public IEnumerable<string> ListSaves(ISubsystemPersistence subsystem)
        {
            if (subsystem == null) return ListSaves();
            try
            {
                return ListSaves(subsystem.Namespace);
            }
            catch (Exception)
            {
                return new string[0];
            }
        }

        public void SaveFile<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true)
            => dataService.Save(data, saveName, ns, fileName, overwrite);
        public T LoadFile<T>(string saveName, string ns, string fileName = null)
            => dataService.Load<T>(saveName, ns, fileName);

        // Subsystem-aware save/load helpers
        public void SaveFile(ISubsystemPersistence subsystem, string saveName = null, object options = null, bool overwrite = true)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            string saveTo = saveName ?? CurrentSaveName ?? "Default";

            try
            {
                if (subsystem.Target == PersistenceTarget.Embedded)
                {
                    // Ensure gameData exists
                    if (gameData == null) gameData = ScriptableObject.CreateInstance<GameData>();

                    var data = subsystem.GetSaveData();
                    if (data != null)
                    {
                        var json = JsonUtility.ToJson(data);
                        var existing = gameData.EmbeddedSubsystems.FirstOrDefault(e => e.Key == subsystem.Namespace);
                        if (existing == null)
                        {
                            gameData.EmbeddedSubsystems.Add(new SubsystemDataEntry { Key = subsystem.Namespace, JsonData = json });
                        }
                        else
                        {
                            existing.JsonData = json;
                        }

                        // Persist the top-level game data
                        dataService.Save(gameData, saveTo);
                    }
                }
                else
                {
                    var data = subsystem.GetSaveData();
                    if (data != null)
                    {
                        dataService.Save<object>(data, saveTo, subsystem.Namespace, subsystem.PersistentName, overwrite);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save subsystem '{subsystem.Namespace}': {ex.Message}");
            }
        }

        public T LoadFile<T>(ISubsystemPersistence subsystem, string saveName = null)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            string loadFrom = saveName ?? CurrentSaveName ?? "Default";

            try
            {
                if (subsystem.Target == PersistenceTarget.Embedded)
                {
                    var entry = gameData?.EmbeddedSubsystems?.FirstOrDefault(e => e.Key == subsystem.Namespace);
                    if (entry != null && !string.IsNullOrEmpty(entry.JsonData))
                    {
                        return (T)JsonUtility.FromJson(entry.JsonData, typeof(T));
                    }
                    return default;
                }
                else
                {
                    return dataService.Load<T>(loadFrom, subsystem.Namespace, subsystem.PersistentName);
                }
            }
            catch (ArgumentException)
            {
                // File not found or invalid - return default
                return default;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load subsystem '{subsystem.Namespace}' from save '{loadFrom}': {ex.Message}");
                return default;
            }
        }

        // Expose data service root path for integration with systems that need filesystem locations.
        public string GetDataServiceRootPath()
        {
            try
            {
                return dataService?.GetRootPath();
            }
            catch { return null; }
        }

        public string GetDataServiceRootPath(string ns)
        {
            try
            {
                // If the underlying data service supports namespace-aware roots, use it.
                return dataService?.GetRootPath(ns);
            }
            catch { return null; }
        }

        // Subsystem-aware root path retrieval (extracts namespace from the subsystem)
        public string GetDataServiceRootPath(ISubsystemPersistence subsystem)
        {
            if (subsystem == null) return null;
            return GetDataServiceRootPath(subsystem.Namespace);
        }
    }
}
