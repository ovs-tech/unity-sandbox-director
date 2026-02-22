using System;
using System.Collections.Generic;
using System.IO;
using Systems.Persistence.Core;
using UnityEngine;

namespace Systems.Persistence.Services
{
    [CreateAssetMenu(menuName = "Persistence/File Data Service", fileName = "FileDataService")]
    public class FileDataService : BaseDataService
    {
        public enum RootLocation
        {
            PersistentDataPath,
            AssetsPath,
            CustomPath
        }

        [Header("Path Settings")]
        [SerializeField] RootLocation rootLocation = RootLocation.PersistentDataPath;
        [SerializeField] string customRootPath = "";
        [SerializeField] string savesFolderName = "saves";

        [Header("File Settings")]
        [SerializeField] string fileExtension = "json";

        protected string rootPath;

        public RootLocation RootPathMode => rootLocation;
        public string CustomRootPath => customRootPath;

        public override void Setup()
        {
            base.Setup();

            ComputeRootPath();
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
        }

        protected void ComputeRootPath()
        {
            // Normalize folder segments so inspectors using forward/back slashes work correctly
            var segments = (savesFolderName ?? string.Empty).Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string combined = segments.Length > 0 ? segments[0] : string.Empty;
            for (int i = 1; i < segments.Length; i++) combined = Path.Combine(combined, segments[i]);

            switch (rootLocation)
            {
                case RootLocation.AssetsPath:
                    rootPath = string.IsNullOrEmpty(combined) ? Application.dataPath : Path.Combine(Application.dataPath, combined);
                    break;
                case RootLocation.CustomPath:
                    var basePath = string.IsNullOrWhiteSpace(customRootPath) ? Application.persistentDataPath : customRootPath;
                    rootPath = string.IsNullOrEmpty(combined) ? basePath : Path.Combine(basePath, combined);
                    break;
                case RootLocation.PersistentDataPath:
                default:
                    rootPath = string.IsNullOrEmpty(combined) ? Application.persistentDataPath : Path.Combine(Application.persistentDataPath, combined);
                    break;
            }
        }

        protected string GetPathToFile(string saveName, string ns)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            string folder = Path.Combine(rootPath, saveName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, string.Concat(ns, ".", fileExtension));
        }

        public override void Save(GameData data, bool overwrite = true)
        {
            Save(data, data.Name, "gamedata", overwrite);
        }

        public override GameData Load(string name)
        {
            return Load<GameData>(name, "gamedata");
        }

        public override void Delete(string name)
        {
            string folder = Path.Combine(rootPath, name);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        public override void DeleteAll()
        {
            if (Directory.Exists(rootPath))
            {
                foreach (var dir in Directory.GetDirectories(rootPath))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        public override IEnumerable<string> ListSaves()
        {
            if (!Directory.Exists(rootPath)) yield break;
            foreach (string dir in Directory.EnumerateDirectories(rootPath))
            {
                yield return Path.GetFileName(dir);
            }
        }

        public override void Save<T>(T data, string saveName, string ns, bool overwrite = true)
        {
            if (serializer == null) throw new InvalidOperationException("FileDataService serializer not set. Call Setup(serializer) before using.");
            string fileLocation = GetPathToFile(saveName, ns);

            if (!overwrite && File.Exists(fileLocation))
            {
                throw new IOException($"The file '{ns}.{fileExtension}' already exists in save '{saveName}' and cannot be overwritten.");
            }

            File.WriteAllText(fileLocation, serializer.Serialize(data));
        }

        public override T Load<T>(string saveName, string ns)
        {
            if (serializer == null) throw new InvalidOperationException("FileDataService serializer not set. Call Setup(serializer) before using.");
            string fileLocation = GetPathToFile(saveName, ns);

            if (!File.Exists(fileLocation))
            {
                throw new ArgumentException($"No persisted file '{ns}.{fileExtension}' in save '{saveName}'");
            }

            return serializer.Deserialize<T>(File.ReadAllText(fileLocation));
        }

        public override IEnumerable<string> ListFiles(string saveName)
        {
            string folder = Path.Combine(rootPath, saveName);
            if (!Directory.Exists(folder)) yield break;
            foreach (string path in Directory.EnumerateFiles(folder))
            {
                if (Path.GetExtension(path) == "." + fileExtension)
                {
                    yield return Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        public override void DeleteFile(string saveName, string ns)
        {
            string fileLocation = GetPathToFile(saveName, ns);
            if (File.Exists(fileLocation)) File.Delete(fileLocation);
        }
    }
}