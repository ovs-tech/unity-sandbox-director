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

        string rootPath = "";

        public override void Setup()
        {
            base.Setup();
            ComputeRootPath();
        }

        private void ComputeRootPath()
        {
            rootPath = Path.Combine(Application.persistentDataPath, "saves");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
        }

        public override string GetRootPath()
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            return rootPath;
        }

        public override string GetRootPath(string ns)
        {
            var root = GetRootPath();
            return string.IsNullOrEmpty(ns) ? root : Path.Combine(root, ns);
        }

        /// <summary>
        /// Combines saveName, namespace, and optional fileName to get the full path.
        /// Result: saves/{saveName}/{ns}/{fileName}.json
        /// </summary>
        private string GetPathToFile(string saveName, string ns, string fileName = null)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();

            // Root save folder: saves/{saveName}
            string saveFolder = Path.Combine(rootPath, saveName);
            if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);

            // Namespace subfolder: saves/{saveName}/{ns}
            string targetFolder = Path.Combine(saveFolder, ns);
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            // If fileName is provided, use it. Otherwise fallback to saveName (backwards compatibility)
            string actualFileName = string.IsNullOrEmpty(fileName) ? saveName : fileName;
            return Path.Combine(targetFolder, string.Concat(actualFileName, ".", fileExtension));
        }

        public override void Save(GameData data, string saveName, bool overwrite = true)
        {
            Save(data, saveName, "gamedata", null, overwrite);
        }

        public override GameData Load(string saveName)
        {
            return Load<GameData>(saveName, "gamedata");
        }

        public override void Delete(string name)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            string path = Path.Combine(rootPath, name);
            if (Directory.Exists(path)) Directory.Delete(path, true);
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

            // In folder-per-save structure, each directory in root is a save
            foreach (string dir in Directory.EnumerateDirectories(rootPath))
            {
                string saveName = Path.GetFileName(dir);
                // Verify it has a game-data.json
                if (File.Exists(Path.Combine(dir, "gamedata", "gamedata." + fileExtension)))
                {
                    yield return saveName;
                }
            }
        }

        public override IEnumerable<string> ListSaves(string ns)
        {
            // For now, return all saves that have this namespace subfolder.
            foreach (var saveName in ListSaves())
            {
                string nsFolder = Path.Combine(rootPath, saveName, ns);
                if (Directory.Exists(nsFolder))
                {
                    yield return saveName;
                }
            }
        }

        public override void Save<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true)
        {
            if (serializer == null) throw new InvalidOperationException("FileDataService serializer not set. Call Setup(serializer) before using.");
            string fileLocation = GetPathToFile(saveName, ns, fileName);

            if (!overwrite && File.Exists(fileLocation))
            {
                throw new IOException($"The file already exists at '{fileLocation}' and cannot be overwritten.");
            }

            File.WriteAllText(fileLocation, serializer.Serialize(data));
        }

        public override T Load<T>(string saveName, string ns, string fileName = null)
        {
            if (serializer == null) throw new InvalidOperationException("FileDataService serializer not set. Call Setup(serializer) before using.");
            string fileLocation = GetPathToFile(saveName, ns, fileName);

            if (!File.Exists(fileLocation))
            {
                throw new ArgumentException($"No persisted file at '{fileLocation}'");
            }

            return serializer.Deserialize<T>(File.ReadAllText(fileLocation));
        }

        public override IEnumerable<string> ListFiles(string saveName)
        {
            if (string.IsNullOrEmpty(rootPath)) ComputeRootPath();
            string saveFolder = Path.Combine(rootPath, saveName);
            if (!Directory.Exists(saveFolder)) yield break;

            // Return all subfolders (namespaces) under this save
            foreach (string dir in Directory.EnumerateDirectories(saveFolder))
            {
                yield return Path.GetFileName(dir);
            }

            // Also include "gamedata" if game-data.json exists
            if (File.Exists(Path.Combine(saveFolder, "gamedata", "gamedata." + fileExtension)))
            {
                yield return "gamedata";
            }
        }

        public override void DeleteFile(string saveName, string ns, string fileName = null)
        {
            string fileLocation = GetPathToFile(saveName, ns, fileName);
            if (File.Exists(fileLocation)) File.Delete(fileLocation);
        }
    }
}