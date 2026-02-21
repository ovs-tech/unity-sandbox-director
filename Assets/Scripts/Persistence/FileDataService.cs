using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Systems.Persistence {
    public class FileDataService : IDataService {
        ISerializer serializer;
        string rootPath;
        string fileExtension;

        public FileDataService(ISerializer serializer) {
            this.rootPath = Path.Combine(Application.persistentDataPath, "saves");
            this.fileExtension = "json";
            this.serializer = serializer;

            if (!Directory.Exists(rootPath)) {
                Directory.CreateDirectory(rootPath);
            }
        }

        string GetPathToFile(string saveName, string ns) {
            string folder = Path.Combine(rootPath, saveName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, string.Concat(ns, ".", fileExtension));
        }

        public void Save(GameData data, bool overwrite = true) {
            Save<GameData>(data, data.Name, "gamedata", overwrite);
        }

        public GameData Load(string name) {
            return Load<GameData>(name, "gamedata");
        }

        public void Delete(string name) {
            string folder = Path.Combine(rootPath, name);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        public void DeleteAll() {
            if (Directory.Exists(rootPath)) {
                foreach (var dir in Directory.GetDirectories(rootPath)) {
                    Directory.Delete(dir, true);
                }
            }
        }

        public IEnumerable<string> ListSaves() {
            if (!Directory.Exists(rootPath)) yield break;
            foreach (string dir in Directory.EnumerateDirectories(rootPath)) {
                yield return Path.GetFileName(dir);
            }
        }

        public void Save<T>(T data, string saveName, string ns, bool overwrite = true) {
            string fileLocation = GetPathToFile(saveName, ns);

            if (!overwrite && File.Exists(fileLocation)) {
                throw new IOException($"The file '{ns}.{fileExtension}' already exists in save '{saveName}' and cannot be overwritten.");
            }

            File.WriteAllText(fileLocation, serializer.Serialize(data));
        }

        public T Load<T>(string saveName, string ns) {
            string fileLocation = GetPathToFile(saveName, ns);

            if (!File.Exists(fileLocation)) {
                throw new ArgumentException($"No persisted file '{ns}.{fileExtension}' in save '{saveName}'");
            }

            return serializer.Deserialize<T>(File.ReadAllText(fileLocation));
        }

        public IEnumerable<string> ListFiles(string saveName) {
            string folder = Path.Combine(rootPath, saveName);
            if (!Directory.Exists(folder)) yield break;
            foreach (string path in Directory.EnumerateFiles(folder)) {
                if (Path.GetExtension(path) == "." + fileExtension) {
                    yield return Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        public void DeleteFile(string saveName, string ns) {
            string fileLocation = GetPathToFile(saveName, ns);
            if (File.Exists(fileLocation)) File.Delete(fileLocation);
        }
    }
}