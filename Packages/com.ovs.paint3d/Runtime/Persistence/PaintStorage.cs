using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Paint3D
{
    /// <summary>Where saved paint lives. Swap <see cref="PaintStorage.Backend"/> to store paint in the cloud, a save slot, etc.</summary>
    public interface IPaintStorageBackend
    {
        bool TryRead(string key, out byte[] data);
        void Write(string key, byte[] data);
        void Delete(string key);
        bool Exists(string key);
    }

    /// <summary>
    /// Stores PNG files under <c>Application.persistentDataPath/Paint3D</c>.
    /// On WebGL this path is backed by IndexedDB; Unity flushes it automatically in recent versions.
    /// </summary>
    public sealed class FilePaintStorageBackend : IPaintStorageBackend
    {
        private string _root;

        public FilePaintStorageBackend(string root = null)
        {
            _root = root;
        }

        public string Root => _root ??= Path.Combine(Application.persistentDataPath, "Paint3D");

        public bool TryRead(string key, out byte[] data)
        {
            string path = GetPath(key);
            if (!File.Exists(path))
            {
                data = null;
                return false;
            }

            data = File.ReadAllBytes(path);
            return true;
        }

        public void Write(string key, byte[] data)
        {
            Directory.CreateDirectory(Root);
            File.WriteAllBytes(GetPath(key), data);
        }

        public void Delete(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool Exists(string key) => File.Exists(GetPath(key));

        public string GetPath(string key) => Path.Combine(Root, PaintStorage.SanitizeKey(key) + ".png");
    }

    /// <summary>Saves and loads paintable textures as PNG through a pluggable backend.</summary>
    public static class PaintStorage
    {
        private static IPaintStorageBackend _backend;

        public static IPaintStorageBackend Backend
        {
            get => _backend ??= new FilePaintStorageBackend();
            set => _backend = value;
        }

        public static bool Save(PaintableTexture slot, string key)
        {
            if (slot == null || !slot.IsAllocated)
            {
                return false;
            }

            Texture2D texture = slot.ReadToTexture2D();
            try
            {
                Backend.Write(key, texture.EncodeToPNG());
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Paint3D] Failed to save '{key}': {exception.Message}");
                return false;
            }
            finally
            {
                PaintTextureUtility.Destroy(texture);
            }
        }

        public static bool Load(PaintableTexture slot, string key)
        {
            if (slot == null || !slot.IsAllocated)
            {
                return false;
            }

            byte[] data;
            try
            {
                if (!Backend.TryRead(key, out data) || data == null || data.Length == 0)
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Paint3D] Failed to load '{key}': {exception.Message}");
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, !slot.IsColor);
            try
            {
                if (!texture.LoadImage(data, false))
                {
                    return false;
                }
                slot.LoadFrom(texture);
                return true;
            }
            finally
            {
                PaintTextureUtility.Destroy(texture);
            }
        }

        public static void Delete(string key)
        {
            try
            {
                Backend.Delete(key);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Paint3D] Failed to delete '{key}': {exception.Message}");
            }
        }

        public static bool Exists(string key) => Backend.Exists(key);

        /// <summary>Turns any key into a safe file name.</summary>
        public static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "unnamed";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(key.Length);
            foreach (char c in key)
            {
                builder.Append(c == '/' || c == '\\' || c == ':' || Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return builder.ToString();
        }
    }
}
