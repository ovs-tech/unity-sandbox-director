using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Persistence.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.Persistence {
    [Serializable] public class GameData { 
        public string Name;
        public string CurrentLevelName;
        // public PlayerData playerData;
        // Store inventory data as ISaveable to avoid direct dependency on Systems.Inventory
        public ISaveable inventoryData;
    }
        
    public interface ISaveable  {
        SerializableGuid Id { get; set; }
    }
    
    public interface IBind<TData> where TData : ISaveable {
        SerializableGuid Id { get; set; }
        void Bind(TData data);
    }
    
    public class SaveLoadSystem : PersistentSingleton<SaveLoadSystem> {
        [SerializeField] public GameData gameData;

        [SerializeField]
        BaseDataService dataService;

        

        // No setup call here; BaseDataService will initialize its serializer asset on enable.

        // Registered subsystem persistence adapters
        List<ISubsystemPersistence> _subsystems = new List<ISubsystemPersistence>();

        // Current save name in use
        public string CurrentSaveName { get; private set; } = "Default";

        [Header("Autosave")]
        [SerializeField] bool AutoSaveEnabled = false;
        [SerializeField] float AutoSaveIntervalSeconds = 60f;

        void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (AutoSaveEnabled) {
                InvokeRepeating(nameof(AutosaveTick), AutoSaveIntervalSeconds, AutoSaveIntervalSeconds);
            }
        }

        void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            CancelInvoke(nameof(AutosaveTick));
        }

        void AutosaveTick() {
            try {
                SaveAll();
            } catch (Exception ex) {
                Debug.LogError($"Autosave failed: {ex.Message}");
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (scene.name == "Menu") return;

            // Let registered subsystems load their data for this save
            LoadAll();
        }
        
        void Bind<T, TData>(TData data) where T : MonoBehaviour, IBind<TData> where TData : ISaveable, new() {
            var entity = FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault();
            if (entity != null) {
                if (data == null) {
                    data = new TData { Id = entity.Id };
                }
                entity.Bind(data);
            }
        }

        void Bind<T, TData>(List<TData> datas) where T: MonoBehaviour, IBind<TData> where TData : ISaveable, new() {
            var entities = FindObjectsByType<T>(FindObjectsSortMode.None);

            foreach(var entity in entities) {
                var data = datas.FirstOrDefault(d=> d.Id == entity.Id);
                if (data == null) {
                    data = new TData { Id = entity.Id };
                    datas.Add(data); 
                }
                entity.Bind(data);
            }
        }
        
        // Save/Load orchestration
        public void SaveGame() {
            dataService.Save(gameData);
            // Also save registered subsystems under the current save folder
            SaveAll();
        }

        public void LoadGame(string gameName) {
            gameData = dataService.Load(gameName);

            if (String.IsNullOrWhiteSpace(gameData.CurrentLevelName)) {
                gameData.CurrentLevelName = "Demo";
            }

            CurrentSaveName = gameData.Name;

            // Load subsystem files after level load if needed by callers
            SceneManager.LoadScene(gameData.CurrentLevelName);
        }

        public void ReloadGame() => LoadGame(gameData.Name);

        public void DeleteGame(string gameName) => dataService.Delete(gameName);

        // Subsystem registration
        public void RegisterSubsystem(ISubsystemPersistence subsystem) {
            if (subsystem == null) return;
            if (!_subsystems.Contains(subsystem)) _subsystems.Add(subsystem);
        }

        public void UnregisterSubsystem(ISubsystemPersistence subsystem) {
            if (subsystem == null) return;
            _subsystems.Remove(subsystem);
        }

        [ContextMenu("All Saves")]
        // Save all registered subsystems into the current save folder
        public void SaveAll(string saveName = null) {
            string saveTo = saveName ?? CurrentSaveName ?? gameData?.Name ?? "Default";
            foreach (var s in _subsystems) {
                try {
                    var data = s.GetSaveData();
                    if (data != null) {
                        // Use the concrete DataType when saving generically
                        dataService.Save<object>(data, saveTo, s.Namespace, true);
                    }
                } catch (Exception ex) {
                    Debug.LogError($"Failed to save subsystem '{s.Namespace}': {ex.Message}");
                }
            }
        }

        public void LoadAll(string saveName = null) {
            string loadFrom = saveName ?? CurrentSaveName ?? gameData?.Name ?? "Default";
            foreach (var s in _subsystems) {
                try {
                    // Attempt to load the file and pass to subsystem
                    var list = dataService.ListFiles(loadFrom);
                    if (list != null && list.Contains(s.Namespace)) {
                        var loaded = dataService.Load<object>(loadFrom, s.Namespace);
                        s.LoadData(loaded);
                    }
                } catch (Exception ex) {
                    Debug.LogWarning($"Failed to load subsystem '{s.Namespace}' from save '{loadFrom}': {ex.Message}");
                }
            }
        }

        public IEnumerable<string> ListFiles(string saveName) => dataService.ListFiles(saveName);
        public IEnumerable<string> ListSaves() => dataService.ListSaves();

        public void SaveFile<T>(T data, string saveName, string ns, bool overwrite = true) => dataService.Save(data, saveName, ns, overwrite);
        public T LoadFile<T>(string saveName, string ns) => dataService.Load<T>(saveName, ns);
    }
}