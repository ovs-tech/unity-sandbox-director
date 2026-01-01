using System;
using System.IO;
using MiniTimeline.Core;
using UnityEngine;
using Sirenix.OdinSerializer;

namespace MiniTimeline.Serialization
{
    /// <summary>
    /// Unified serializer for MiniTimelineProject.
    /// Uses Odin Serializer to directly serialize polymorphic IMiniTrack/IMiniClip runtime instances.
    /// </summary>
    public static class ProjectSerializer
    {
        public static string SaveToJson(MiniTimelineProject project)
        {
            if (project == null)
            {
                Debug.LogError("[ProjectSerializer] Cannot save: project is null");
                return null;
            }
            try
            {
                // Use Odin Serializer with JSON format for human-readable output
                byte[] bytes = SerializationUtility.SerializeValue(project, DataFormat.JSON);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error saving project to JSON: {e.Message}");
                return null;
            }
        }

        public static bool SaveToFile(MiniTimelineProject project, string filePath)
        {
            try
            {
                var json = SaveToJson(project);
                if (json == null)
                    return false;
                File.WriteAllText(filePath, json);
                Debug.Log($"[ProjectSerializer] Saved project to: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error saving project to file '{filePath}': {e.Message}");
                return false;
            }
        }

        public static MiniTimelineProject LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[ProjectSerializer] Cannot load: JSON is null or empty");
                return null;
            }
            try
            {
                // Use Odin Serializer to deserialize with polymorphic support
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                return SerializationUtility.DeserializeValue<MiniTimelineProject>(bytes, DataFormat.JSON);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error loading project from JSON: {e.Message}");
                return null;
            }
        }

        public static MiniTimelineProject LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[ProjectSerializer] File not found: {filePath}");
                    return null;
                }
                var json = File.ReadAllText(filePath);
                var project = LoadFromJson(json);
                if (project != null)
                {
                    Debug.Log($"[ProjectSerializer] Loaded project from: {filePath}");
                }
                return project;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error loading project from file '{filePath}': {e.Message}");
                return null;
            }
        }
    }
}
