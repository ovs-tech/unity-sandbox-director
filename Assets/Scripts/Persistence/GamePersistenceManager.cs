using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

#if UNITY_EDITOR
        [ContextMenu("Refresh Persistence Settings")]
        private void RefreshPersistenceSettings()
        {
            bool changed = false;

            try
            {
                // Attempt to auto-assign a GameData asset if missing
                if (gameData == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:GameData");
                    if (guids != null && guids.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        var gd = UnityEditor.AssetDatabase.LoadAssetAtPath<GameData>(path);
                        if (gd != null)
                        {
                            gameData = gd;
                            Debug.Log($"[GamePersistenceManager] Assigned GameData asset: {path}");
                            changed = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[GamePersistenceManager] No GameData asset found in project.");
                    }
                }

                // Attempt to auto-assign a BaseDataService asset if missing
                if (dataService == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:BaseDataService");
                    if (guids != null && guids.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        var svc = UnityEditor.AssetDatabase.LoadAssetAtPath<Systems.Persistence.Core.BaseDataService>(path);
                        if (svc != null)
                        {
                            dataService = svc;
                            Debug.Log($"[GamePersistenceManager] Assigned DataService asset: {path}");
                            changed = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[GamePersistenceManager] No BaseDataService asset found in project.");
                    }
                }

                // Attempt runtime setup for the data service so root paths are available in-editor
                if (dataService != null)
                {
                    try
                    {
                        dataService.Setup();
                        Debug.Log("[GamePersistenceManager] DataService.Setup() invoked successfully.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[GamePersistenceManager] DataService.Setup() threw: {ex.Message}");
                    }
                }

                if (changed)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    if (!Application.isPlaying)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GamePersistenceManager] RefreshPersistenceSettings failed: {ex.Message}");
            }
        }
#endif

        /// <summary>
        /// Runtime refresh: re-setup the data service and re-discover & register all
        /// ISubsystemPersistence instances in the scene. Useful when subsystems are
        /// added/removed dynamically and you want the manager to re-sync.
        /// </summary>
        [ContextMenu("Refresh And Re-register Subsystems")]
        public void RefreshAndReregisterSubsystems()
        {
            try
            {
                // Ensure data service is set up so subsystems can query roots/serializers
                try { dataService?.Setup(); } catch (Exception ex) { Debug.LogWarning($"[GamePersistenceManager] dataService.Setup() threw: {ex.Message}"); }

                // Find all MonoBehaviours that implement ISubsystemPersistence
                var found = new List<ISubsystemPersistence>();
                var monos = FindObjectsByType<MonoBehaviour>();
                foreach (var m in monos)
                {
                    if (m is ISubsystemPersistence s)
                    {
                        found.Add(s);
                    }
                }

                // Replace internal list with discovered subsystems (preserve uniqueness)
                _subsystems.Clear();
                foreach (var s in found)
                {
                    if (!_subsystems.Contains(s)) _subsystems.Add(s);
                }

                Debug.Log($"[GamePersistenceManager] Refreshed and registered {_subsystems.Count} subsystems.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GamePersistenceManager] RefreshAndReregisterSubsystems failed: {ex.Message}");
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Persistence/Refresh And Re-register Subsystems")]
        private static void MenuRefreshAndReregister()
        {
            var mgr = Instance;
            if (mgr == null)
            {
                Debug.LogWarning("No GamePersistenceManager instance found to refresh.");
                return;
            }
            mgr.RefreshAndReregisterSubsystems();
        }
#endif

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
            var entity = FindObjectsByType<T>().FirstOrDefault();
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
            var entities = FindObjectsByType<T>();

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
            gameData = dataService.Load<GameData>(gameName);
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

        public IEnumerable<string> ListFiles(string saveName, string ns = "") => dataService.ListFiles(saveName, ns);
        public IEnumerable<string> ListFiles(ISubsystemPersistence subsystem, string saveName = null)
        {
            if (subsystem == null) return new string[0];
            string saveTo = saveName ?? CurrentSaveName ?? "Default";
            try
            {
                return ListFiles(saveTo, subsystem.Namespace);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to list files for subsystem '{subsystem.Namespace}' in save '{saveTo}': {ex.Message}");
                return new string[0];
            }
        }

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

        public Task SaveFileAsync<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true, CancellationToken token = default)
            => dataService.SaveAsync(data, saveName, ns, fileName, overwrite, token);
        public Task<T> LoadFileAsync<T>(string saveName, string ns, string fileName = null, CancellationToken token = default)
            => dataService.LoadAsync<T>(saveName, ns, fileName, token);

        // Subsystem-aware save/load helpers
        public void SaveFile(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, bool overwrite = true)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            string saveTo = saveName ?? CurrentSaveName ?? "Default";
            string fileNameToUse = fileName ?? subsystem.PersistentName;

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
                        dataService.Save<object>(data, saveTo, subsystem.Namespace, fileNameToUse, overwrite);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save subsystem '{subsystem.Namespace}': {ex.Message}");
            }
        }

        public T LoadFile<T>(ISubsystemPersistence subsystem, string saveName = null, string fileName = null)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            string loadFrom = saveName ?? CurrentSaveName ?? "Default";
            string fileNameToUse = fileName ?? subsystem.PersistentName;

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
                    return dataService.Load<T>(loadFrom, subsystem.Namespace, fileNameToUse);
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

        public async Task SaveFileAsync(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, bool overwrite = true, CancellationToken token = default)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            token.ThrowIfCancellationRequested();

            string saveTo = saveName ?? CurrentSaveName ?? "Default";
            string fileNameToUse = fileName ?? subsystem.PersistentName;

            try
            {
                if (subsystem.Target == PersistenceTarget.Embedded)
                {
                    // Embedded payload is stored in gameData and persisted via game-level save.
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

                        dataService.Save(gameData, saveTo);
                    }
                }
                else
                {
                    var data = subsystem.GetSaveData();
                    if (data != null)
                    {
                        await dataService.SaveAsync<object>(data, saveTo, subsystem.Namespace, fileNameToUse, overwrite, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save subsystem '{subsystem.Namespace}': {ex.Message}");
            }
        }

        public async Task<T> LoadFileAsync<T>(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, CancellationToken token = default)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            token.ThrowIfCancellationRequested();

            string loadFrom = saveName ?? CurrentSaveName ?? "Default";
            string fileNameToUse = fileName ?? subsystem.PersistentName;

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

                return await dataService.LoadAsync<T>(loadFrom, subsystem.Namespace, fileNameToUse, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                return default;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load subsystem '{subsystem.Namespace}' from save '{loadFrom}': {ex.Message}");
                return default;
            }
        }

        public void DeleteFile(ISubsystemPersistence subsystem, string saveName = null, string fileName = null)
        {
            if (subsystem == null) throw new ArgumentNullException(nameof(subsystem));

            string saveTo = saveName ?? CurrentSaveName ?? "Default";
            string fileNameToUse = fileName ?? subsystem.PersistentName;

            try
            {
                if (subsystem.Target == PersistenceTarget.Embedded)
                {
                    var existing = gameData?.EmbeddedSubsystems?.FirstOrDefault(e => e.Key == subsystem.Namespace);
                    if (existing != null)
                    {
                        gameData.EmbeddedSubsystems.Remove(existing);
                        dataService.Save(gameData, saveTo);
                    }
                }
                else
                {
                    dataService.DeleteFile(saveTo, subsystem.Namespace, fileNameToUse);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete file for subsystem '{subsystem.Namespace}': {ex.Message}");
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
