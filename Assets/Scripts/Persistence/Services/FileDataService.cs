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

        public override string GetRootPath()
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            return rootPath;
        }

        // Returns the root path concatenated with a namespace subfolder when provided.
        public string GetRootPath(string ns)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            return string.IsNullOrEmpty(ns) ? rootPath : Path.Combine(rootPath, ns);
        }

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
            // Resolve the base root path including namespace. Do NOT make a subfolder per saveName;
            // files live directly under the namespace folder.
            string basePath = GetRootPath(ns);
            string folder = basePath;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            // Filename is based on the saveName (e.g. "mysave.json")
            return Path.Combine(folder, string.Concat(saveName, ".", fileExtension));
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
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();

            // Delete any files named '{name}.{ext}' in the root and all namespace subfolders.
            string fileName = string.Concat(name, ".", fileExtension);

            // Root-level file
            var rootFile = Path.Combine(rootPath, fileName);
            if (File.Exists(rootFile)) File.Delete(rootFile);

            // Namespace subfolders
            if (Directory.Exists(rootPath))
            {
                foreach (var dir in Directory.EnumerateDirectories(rootPath))
                {
                    var p = Path.Combine(dir, fileName);
                    if (File.Exists(p)) File.Delete(p);
                }
            }
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
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            if (!Directory.Exists(rootPath)) yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Files directly under root
            foreach (string f in Directory.EnumerateFiles(rootPath))
            {
                if (Path.GetExtension(f) == "." + fileExtension)
                {
                    seen.Add(Path.GetFileNameWithoutExtension(f));
                }
            }

            // Files under namespace folders
            foreach (string dir in Directory.EnumerateDirectories(rootPath))
            {
                foreach (string f in Directory.EnumerateFiles(dir))
                {
                    if (Path.GetExtension(f) == "." + fileExtension)
                    {
                        seen.Add(Path.GetFileNameWithoutExtension(f));
                    }
                }
            }

            foreach (var name in seen)
            {
                yield return name;
            }
        }

        public override IEnumerable<string> ListSaves(string ns)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            if (!Directory.Exists(rootPath)) yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Check the specified namespace folder for files
            string nsFolder = Path.Combine(rootPath, ns);
            if (Directory.Exists(nsFolder))
            {
                foreach (string f in Directory.EnumerateFiles(nsFolder))
                {
                    if (Path.GetExtension(f) == "." + fileExtension)
                    {
                        seen.Add(Path.GetFileNameWithoutExtension(f));
                    }
                }
            }

            foreach (var name in seen)
            {
                yield return name;
            }
        }

        public override void Save<T>(T data, string saveName, string ns, bool overwrite = true)
        {
            if (serializer == null) throw new InvalidOperationException("FileDataService serializer not set. Call Setup(serializer) before using.");
            string fileLocation = GetPathToFile(saveName, ns);

            if (!overwrite && File.Exists(fileLocation))
            {
                throw new IOException($"The file '{saveName}.{fileExtension}' already exists in save '{saveName}' and cannot be overwritten.");
            }

            File.WriteAllText(fileLocation, serializer.Serialize(data));
        }

        public override T Load<T>(string saveName, string ns)
        {
            if (serializer == null) throw new InvalidOperationException("FileDataService serializer not set. Call Setup(serializer) before using.");
            string fileLocation = GetPathToFile(saveName, ns);

            if (!File.Exists(fileLocation))
            {
                throw new ArgumentException($"No persisted file '{saveName}.{fileExtension}' in save '{saveName}'");
            }

            return serializer.Deserialize<T>(File.ReadAllText(fileLocation));
        }

        public override IEnumerable<string> ListFiles(string saveName)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            if (!Directory.Exists(rootPath)) yield break;

            string targetFileName = string.Concat(saveName, ".", fileExtension);

            // Check all namespace directories for the presence of this save file and return namespaces that contain it
            foreach (string dir in Directory.EnumerateDirectories(rootPath))
            {
                var candidate = Path.Combine(dir, targetFileName);
                if (File.Exists(candidate))
                {
                    yield return Path.GetFileName(dir);
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