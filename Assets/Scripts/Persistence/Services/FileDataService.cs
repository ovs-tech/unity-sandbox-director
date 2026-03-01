using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Systems.Persistence.Core;
using UnityEngine;

namespace Systems.Persistence.Services
{
    [CreateAssetMenu(menuName = "Persistence/File Data Service", fileName = "FileDataService")]
    public class FileDataService : BaseDataService
    {
        [Header("File Settings")]
        [SerializeField] string fileExtension = "json";

        public enum RootLocation
        {
            PersistentData,
            Assets
        }

        [Header("Root Path")]
        [SerializeField]
        RootLocation rootLocation = RootLocation.PersistentData;

        [Header("Subpath")]
        [SerializeField]
        string rootSubfolder = "saves";

        string rootPath = "";

        public override void Setup()
        {
            base.Setup();
            InitializeRootPath();
        }

        private void InitializeRootPath()
        {
            rootPath = rootLocation == RootLocation.PersistentData
                ? Path.Combine(Application.persistentDataPath, rootSubfolder)
                : Path.Combine(Application.dataPath, rootSubfolder);

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }
        }

        public override string GetRootPath()
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                InitializeRootPath();
            }
            return rootPath;
        }

        public override void Delete(string saveName)
        {
            var savePath = Path.Combine(GetRootPath(), saveName);
            if (Directory.Exists(savePath))
            {
                Directory.Delete(savePath, true);
            }
        }

        public override void DeleteAll()
        {
            var rootDir = GetRootPath();
            if (Directory.Exists(rootDir))
            {
                IEnumerable<string> dirs = Directory.GetDirectories(rootDir);
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        public override void DeleteFile(string saveName, string ns, string fileName = null)
        {
            var filePath = GetFilePath(saveName, ns, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                CleanupEmptyDirectories(Path.GetDirectoryName(filePath));
            }
        }

        public override IEnumerable<string> ListFiles(string saveName, string ns)
        {
            var nsPath = Path.Combine(GetRootPath(), saveName, ns);
            if (!Directory.Exists(nsPath))
            {
                return Enumerable.Empty<string>();
            }

            if (string.IsNullOrEmpty(fileExtension))
            {
                return Directory.GetFiles(nsPath)
                    .Select(f => Path.GetFileNameWithoutExtension(f));
            }

            return Directory.GetFiles(nsPath, $"*.{fileExtension}")
                .Select(f => Path.GetFileNameWithoutExtension(f));
        }

        public override IEnumerable<string> ListSaves()
        {
            var rootDir = GetRootPath();
            if (!Directory.Exists(rootDir))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetDirectories(rootDir)
                .Select(d => Path.GetFileName(d));
        }

        public override IEnumerable<string> ListSaves(string ns)
        {
            var rootDir = GetRootPath();
            if (!Directory.Exists(rootDir))
            {
                return Enumerable.Empty<string>();
            }

            var saves = new List<string>();
            foreach (var saveDir in Directory.GetDirectories(rootDir))
            {
                var nsPath = Path.Combine(saveDir, ns);
                if (Directory.Exists(nsPath))
                {
                    saves.Add(Path.GetFileName(saveDir));
                }
            }
            return saves;
        }

        public override T Load<T>(string saveName)
        {
            var filePath = GetFilePath(saveName, null, saveName);
            if (!File.Exists(filePath))
            {
                return default;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {filePath}: {ex.Message}");
                return default;
            }
        }

        public override T Load<T>(string saveName, string ns, string fileName = null)
        {
            var filePath = GetFilePath(saveName, ns, fileName);
            if (!File.Exists(filePath))
            {
                return default;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load {filePath}: {ex.Message}");
                return default;
            }
        }

        public override void Save<T>(T data, string saveName, bool overwrite = true)
        {
            Save(data, saveName, null, saveName, overwrite);
        }

        public override void Save<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true)
        {
            var filePath = GetFilePath(saveName, ns, fileName);
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(filePath) && !overwrite)
            {
                Debug.LogWarning($"File {filePath} already exists and overwrite is false");
                return;
            }

            try
            {
                string json = serializer.Serialize(data);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save {filePath}: {ex.Message}");
            }
        }

        private string GetFilePath(string saveName, string ns, string fileName)
        {
            var path = Path.Combine(GetRootPath(), saveName);

            if (!string.IsNullOrEmpty(ns))
            {
                path = Path.Combine(path, ns);
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = saveName;
            }

            var fileNameWithExtension = string.IsNullOrEmpty(fileExtension)
                ? fileName
                : $"{fileName}.{fileExtension}";
            return Path.Combine(path, fileNameWithExtension);
        }

        private void CleanupEmptyDirectories(string directory)
        {
            try
            {
                if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0)
                {
                    Directory.Delete(directory);
                    var parent = Path.GetDirectoryName(directory);
                    if (parent != GetRootPath())
                    {
                        CleanupEmptyDirectories(parent);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to cleanup directory {directory}: {ex.Message}");
            }
        }
    }
}