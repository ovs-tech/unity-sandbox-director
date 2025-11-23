using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SceneSandbox.Core;
using SceneSandbox.Data;

namespace SceneSandbox.Serialization
{
    /// <summary>
    /// Handles saving and loading of Scene Sandbox projects to/from JSON
    /// Supports versioning and backwards compatibility
    /// </summary>
    public static class SandboxProjectSerializer
    {
        /// <summary>
        /// Update project data from runtime state before saving.
        /// This ensures that runtime changes are captured in the serialization.
        /// </summary>
        /// <param name="project">Project to update</param>
        /// <param name="builder">Builder containing runtime state</param>
        public static void UpdateProjectFromRuntimeState(SandboxProjectData project, SceneSandboxBuilder builder)
        {
            if (project == null)
            {
                Debug.LogWarning("[SandboxProjectSerializer] Cannot update project: Project is null");
                return;
            }
            
            if (builder == null)
            {
                Debug.LogWarning("[SandboxProjectSerializer] Cannot update project: Builder is null");
                return;
            }
            
            // Update project metadata
            project.lastModified = DateTime.Now;
            
            // Update active scene configuration from current scene
            if (builder.CurrentScene != null)
            {
                var activeScene = project.GetActiveScene();
                if (activeScene != null)
                {
                    // Update the active scene with current scene data
                    var index = project.scenes.IndexOf(activeScene);
                    if (index >= 0)
                    {
                        project.scenes[index] = builder.CurrentScene;
                    }
                }
                else
                {
                    // No active scene, add current scene
                    project.scenes.Add(builder.CurrentScene);
                    project.activeSceneId = builder.CurrentScene.sceneId;
                }
            }
            
            // Update settings from builder's current configuration
            UpdateProjectSettingsFromBuilder(project.settings, builder);
        }
        
        /// <summary>
        /// Save a sandbox project to JSON string
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <param name="prettyPrint">Whether to format JSON for readability</param>
        /// <returns>JSON string</returns>
        public static string SaveToJson(SandboxProjectData project, bool prettyPrint = true)
        {
            try
            {
                var jsonProject = ConvertToJsonProject(project);
                return JsonUtility.ToJson(jsonProject, prettyPrint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error saving project to JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save a sandbox project to JSON string, updating from runtime state first
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <param name="builder">Builder containing runtime state</param>
        /// <param name="prettyPrint">Whether to format JSON for readability</param>
        /// <returns>JSON string</returns>
        public static string SaveToJson(SandboxProjectData project, SceneSandboxBuilder builder, bool prettyPrint = true)
        {
            try
            {
                // Update project from runtime state first
                UpdateProjectFromRuntimeState(project, builder);
                
                // Then serialize normally
                return SaveToJson(project, prettyPrint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error saving project with builder to JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save a sandbox project to file
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <param name="filePath">File path to save to</param>
        /// <returns>True if successful</returns>
        public static bool SaveToFile(SandboxProjectData project, string filePath)
        {
            try
            {
                string json = SaveToJson(project);
                if (json != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllText(filePath, json);
                    Debug.Log($"[SandboxProjectSerializer] Saved project to: {filePath}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error saving project to file '{filePath}': {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// Save a sandbox project to file, updating from runtime state first
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <param name="builder">Builder containing runtime state</param>
        /// <param name="filePath">File path to save to</param>
        /// <returns>True if successful</returns>
        public static bool SaveToFile(SandboxProjectData project, SceneSandboxBuilder builder, string filePath)
        {
            try
            {
                string json = SaveToJson(project, builder);
                if (json != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllText(filePath, json);
                    Debug.Log($"[SandboxProjectSerializer] Saved project with runtime state to: {filePath}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error saving project with builder to file '{filePath}': {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// Load a sandbox project from JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Loaded project or null if failed</returns>
        public static SandboxProjectData LoadFromJson(string json)
        {
            try
            {
                var jsonProject = JsonUtility.FromJson<JsonSandboxProject>(json);
                if (jsonProject == null)
                {
                    Debug.LogError("[SandboxProjectSerializer] Failed to parse JSON");
                    return null;
                }

                return ConvertFromJsonProject(jsonProject);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error loading project from JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load a sandbox project from file
        /// </summary>
        /// <param name="filePath">File path to load from</param>
        /// <returns>Loaded project or null if failed</returns>
        public static SandboxProjectData LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[SandboxProjectSerializer] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var project = LoadFromJson(json);

                if (project != null)
                {
                    Debug.Log($"[SandboxProjectSerializer] Loaded project from: {filePath}");
                }

                return project;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error loading project from file '{filePath}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get project metadata without loading the full project
        /// </summary>
        /// <param name="filePath">File path to analyze</param>
        /// <returns>Project metadata or null if failed</returns>
        public static SandboxProjectMetadata GetProjectMetadata(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                var jsonProject = JsonUtility.FromJson<JsonSandboxProject>(json);
                
                if (jsonProject == null)
                    return null;

                return new SandboxProjectMetadata
                {
                    projectName = jsonProject.projectName,
                    filePath = filePath,
                    created = DateTime.Parse(jsonProject.created),
                    lastModified = DateTime.Parse(jsonProject.lastModified),
                    description = jsonProject.description,
                    objectCount = jsonProject.scenes?.Sum(s => s.placedObjects?.Count ?? 0) ?? 0,
                    hasTimelineIntegration = jsonProject.hasTimelineIntegration,
                    sceneBounds = ParseVector3(jsonProject.settings?.sceneBounds ?? "20,10,20")
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SandboxProjectSerializer] Error getting metadata for '{filePath}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all project files in a directory with their metadata
        /// </summary>
        /// <param name="directoryPath">Directory to scan</param>
        /// <param name="searchPattern">File pattern to search for</param>
        /// <returns>List of project metadata</returns>
        public static List<SandboxProjectMetadata> GetProjectsInDirectory(string directoryPath, string searchPattern = "*.sbproj")
        {
            var projects = new List<SandboxProjectMetadata>();
            
            try
            {
                if (!Directory.Exists(directoryPath))
                    return projects;

                var files = Directory.GetFiles(directoryPath, searchPattern);
                
                foreach (var file in files)
                {
                    var metadata = GetProjectMetadata(file);
                    if (metadata != null)
                    {
                        projects.Add(metadata);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxProjectSerializer] Error scanning directory '{directoryPath}': {e.Message}");
            }
            
            return projects.OrderByDescending(p => p.lastModified).ToList();
        }

        #region Conversion Methods

        /// <summary>
        /// Convert SandboxProjectData to JSON-serializable format
        /// </summary>
        private static JsonSandboxProject ConvertToJsonProject(SandboxProjectData project)
        {
            var jsonProject = new JsonSandboxProject
            {
                version = project.version,
                projectName = project.projectName,
                description = project.description,
                created = project.created.ToString("O"), // ISO 8601 format
                lastModified = project.lastModified.ToString("O"),
                settings = ConvertToJsonSettings(project.settings),
                scenes = project.scenes,
                activeSceneId = project.activeSceneId,
                objectLibraryPath = project.objectLibraryPath,
                timelineProjectPath = project.timelineProjectPath,
                hasTimelineIntegration = project.hasTimelineIntegration,
                exportSettings = ConvertToJsonExportSettings(project.exportSettings)
            };

            return jsonProject;
        }

        /// <summary>
        /// Convert JSON project back to SandboxProjectData
        /// </summary>
        private static SandboxProjectData ConvertFromJsonProject(JsonSandboxProject jsonProject)
        {
            // Handle version compatibility
            if (jsonProject.version > 1)
            {
                Debug.LogWarning($"[SandboxProjectSerializer] Loading project with version {jsonProject.version}, current version is 1. Some features may not work correctly.");
            }

            var project = new SandboxProjectData
            {
                version = jsonProject.version,
                projectName = jsonProject.projectName ?? "Untitled Project",
                description = jsonProject.description ?? "",
                objectLibraryPath = jsonProject.objectLibraryPath ?? "",
                timelineProjectPath = jsonProject.timelineProjectPath ?? "",
                hasTimelineIntegration = jsonProject.hasTimelineIntegration,
                scenes = jsonProject.scenes ?? new List<SceneConfiguration> { new SceneConfiguration("New Scene") },
                activeSceneId = jsonProject.activeSceneId ?? "",
                settings = ConvertFromJsonSettings(jsonProject.settings),
                exportSettings = ConvertFromJsonExportSettings(jsonProject.exportSettings)
            };
            
            // Ensure there's always at least one scene and a valid active scene
            if (project.scenes.Count == 0)
            {
                project.scenes.Add(new SceneConfiguration("New Scene"));
            }
            
            // Set active scene ID if not set or invalid
            if (string.IsNullOrEmpty(project.activeSceneId) || project.GetScene(project.activeSceneId) == null)
            {
                project.activeSceneId = project.scenes[0].sceneId;
            }

            // Parse dates safely
            if (DateTime.TryParse(jsonProject.created, out DateTime created))
                project.created = created;
            
            if (DateTime.TryParse(jsonProject.lastModified, out DateTime lastModified))
                project.lastModified = lastModified;

            return project;
        }

        private static JsonSandboxProjectSettings ConvertToJsonSettings(SandboxProjectSettings settings)
        {
            return new JsonSandboxProjectSettings
            {
                snapToGrid = settings.snapToGrid,
                gridSize = settings.gridSize,
                defaultPlacementHeight = settings.defaultPlacementHeight,
                minimumPlacementHeight = settings.minimumPlacementHeight,
                useRaycastForPlacement = settings.useRaycastForPlacement,
                useConsistentHeight = settings.useConsistentHeight,
                placementLayers = settings.placementLayers.value,
                gridOffset = Vector3ToString(settings.gridOffset),
                gridPivotOffset = settings.gridPivotOffset.ToString(),
                sceneBounds = Vector3ToString(settings.sceneBounds),
                sceneBoundsOffset = Vector3ToString(settings.sceneBoundsOffset),
                sceneBoundsPivot = settings.sceneBoundsPivot.ToString(),
                sceneBoundsColor = ColorToString(settings.sceneBoundsColor),
                enableGizmos = settings.enableGizmos,
                showBoundsGizmo = settings.showBoundsGizmo,
                showAxesGizmo = settings.showAxesGizmo,
                showHandlesGizmo = settings.showHandlesGizmo,
                gizmoBoundsColor = ColorToString(settings.gizmoBoundsColor),
                gizmoAxisLength = settings.gizmoAxisLength,
                enableSceneGizmos = settings.enableSceneGizmos,
                showSceneGrid = settings.showSceneGrid,
                showSceneBounds = settings.showSceneBounds,
                showStageAreaGizmo = settings.showStageAreaGizmo,
                showPlacementHeightGizmo = settings.showPlacementHeightGizmo,
                placementHeightColor = ColorToString(settings.placementHeightColor),
                enableDropIndicator = settings.enableDropIndicator,
                validDropColor = ColorToString(settings.validDropColor),
                invalidDropColor = ColorToString(settings.invalidDropColor),
                dropIndicatorSize = settings.dropIndicatorSize,
                autoPreview = settings.autoPreview,
                previewDuration = settings.previewDuration
            };
        }

        private static SandboxProjectSettings ConvertFromJsonSettings(JsonSandboxProjectSettings jsonSettings)
        {
            if (jsonSettings == null)
                return new SandboxProjectSettings();

            return new SandboxProjectSettings
            {
                snapToGrid = jsonSettings.snapToGrid,
                gridSize = jsonSettings.gridSize,
                defaultPlacementHeight = jsonSettings.defaultPlacementHeight,
                minimumPlacementHeight = jsonSettings.minimumPlacementHeight,
                useRaycastForPlacement = jsonSettings.useRaycastForPlacement,
                useConsistentHeight = jsonSettings.useConsistentHeight,
                placementLayers = jsonSettings.placementLayers,
                gridOffset = ParseVector3(jsonSettings.gridOffset ?? "0,0,0"),
                gridPivotOffset = ParsePivotPoint(jsonSettings.gridPivotOffset ?? "Center"),
                sceneBounds = ParseVector3(jsonSettings.sceneBounds ?? "20,10,20"),
                sceneBoundsOffset = ParseVector3(jsonSettings.sceneBoundsOffset ?? "0,0,0"),
                sceneBoundsPivot = ParsePivotPoint(jsonSettings.sceneBoundsPivot ?? "Center"),
                sceneBoundsColor = ParseColor(jsonSettings.sceneBoundsColor ?? "#00FFFF"),
                enableGizmos = jsonSettings.enableGizmos,
                showBoundsGizmo = jsonSettings.showBoundsGizmo,
                showAxesGizmo = jsonSettings.showAxesGizmo,
                showHandlesGizmo = jsonSettings.showHandlesGizmo,
                gizmoBoundsColor = ParseColor(jsonSettings.gizmoBoundsColor ?? "#FFFF00"),
                gizmoAxisLength = jsonSettings.gizmoAxisLength,
                enableSceneGizmos = jsonSettings.enableSceneGizmos,
                showSceneGrid = jsonSettings.showSceneGrid,
                showSceneBounds = jsonSettings.showSceneBounds,
                showStageAreaGizmo = jsonSettings.showStageAreaGizmo,
                showPlacementHeightGizmo = jsonSettings.showPlacementHeightGizmo,
                placementHeightColor = ParseColor(jsonSettings.placementHeightColor ?? "#FFFF00"),
                enableDropIndicator = jsonSettings.enableDropIndicator,
                validDropColor = ParseColor(jsonSettings.validDropColor ?? "#00FF00"),
                invalidDropColor = ParseColor(jsonSettings.invalidDropColor ?? "#FF0000"),
                dropIndicatorSize = jsonSettings.dropIndicatorSize,
                autoPreview = jsonSettings.autoPreview,
                previewDuration = jsonSettings.previewDuration
            };
        }

        private static JsonSandboxExportSettings ConvertToJsonExportSettings(SandboxExportSettings settings)
        {
            return new JsonSandboxExportSettings
            {
                includeObjectLibrary = settings.includeObjectLibrary,
                includeTimelineData = settings.includeTimelineData,
                compressTextures = settings.compressTextures,
                optimizeMeshes = settings.optimizeMeshes,
                targetPlatforms = settings.targetPlatforms,
                packageName = settings.packageName,
                packageVersion = settings.packageVersion,
                packageAuthor = settings.packageAuthor,
                createExecutable = settings.createExecutable,
                createPackage = settings.createPackage,
                outputDirectory = settings.outputDirectory
            };
        }

        private static SandboxExportSettings ConvertFromJsonExportSettings(JsonSandboxExportSettings jsonSettings)
        {
            if (jsonSettings == null)
                return new SandboxExportSettings();

            return new SandboxExportSettings
            {
                includeObjectLibrary = jsonSettings.includeObjectLibrary,
                includeTimelineData = jsonSettings.includeTimelineData,
                compressTextures = jsonSettings.compressTextures,
                optimizeMeshes = jsonSettings.optimizeMeshes,
                targetPlatforms = jsonSettings.targetPlatforms ?? new List<string>(),
                packageName = jsonSettings.packageName ?? "",
                packageVersion = jsonSettings.packageVersion ?? "1.0.0",
                packageAuthor = jsonSettings.packageAuthor ?? "",
                createExecutable = jsonSettings.createExecutable,
                createPackage = jsonSettings.createPackage,
                outputDirectory = jsonSettings.outputDirectory ?? "Builds/"
            };
        }

        /// <summary>
        /// Update project settings from builder's current configuration
        /// </summary>
        private static void UpdateProjectSettingsFromBuilder(SandboxProjectSettings settings, SceneSandboxBuilder builder)
        {
            // Update settings from builder's serialized fields
            settings.sceneBounds = builder.SceneBounds;
            settings.sceneBoundsOffset = builder.SceneBoundsOffset;
            settings.sceneBoundsPivot = builder.SceneBoundsPivot;
            settings.gridOffset = builder.GridOffset;
            settings.gridPivotOffset = builder.GridPivotOffset;
            settings.enableGizmos = builder.GizmosEnabled;
            settings.enableSceneGizmos = builder.SceneGizmosEnabled;
            settings.enableDropIndicator = builder.DropIndicatorEnabled;
            
            // Note: Other settings would need to be exposed as properties on SceneSandboxBuilder
            // or we could use reflection to access private fields if needed
        }

        #endregion

        #region Utility Methods

        private static string Vector3ToString(Vector3 vector)
        {
            return $"{vector.x},{vector.y},{vector.z}";
        }

        private static Vector3 ParseVector3(string vectorString)
        {
            try
            {
                string[] parts = vectorString.Split(',');
                if (parts.Length == 3)
                {
                    return new Vector3(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2])
                    );
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors, return default
            }
            
            return new Vector3(20f, 10f, 20f);
        }

        private static string ColorToString(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        private static Color ParseColor(string colorString)
        {
            if (ColorUtility.TryParseHtmlString(colorString, out Color color))
                return color;
            
            return Color.white;
        }

        private static Core.PivotPoint ParsePivotPoint(string pivotString)
        {
            if (System.Enum.TryParse<Core.PivotPoint>(pivotString, out var pivot))
                return pivot;
            
            return Core.PivotPoint.Center;
        }

        #endregion

        #region JSON Data Classes

        [Serializable]
        private class JsonSandboxProject
        {
            public int version;
            public string projectName;
            public string description;
            public string created;
            public string lastModified;
            public JsonSandboxProjectSettings settings;
            public List<SceneConfiguration> scenes;
            public string activeSceneId;
            public string objectLibraryPath;
            public string timelineProjectPath;
            public bool hasTimelineIntegration;
            public JsonSandboxExportSettings exportSettings;
        }

        [Serializable]
        private class JsonSandboxProjectSettings
        {
            public bool snapToGrid;
            public float gridSize;
            public float defaultPlacementHeight;
            public float minimumPlacementHeight;
            public bool useRaycastForPlacement;
            public bool useConsistentHeight;
            public int placementLayers;
            public string gridOffset;
            public string gridPivotOffset;
            public string sceneBounds;
            public string sceneBoundsOffset;
            public string sceneBoundsPivot;
            public string sceneBoundsColor;
            public bool enableGizmos;
            public bool showBoundsGizmo;
            public bool showAxesGizmo;
            public bool showHandlesGizmo;
            public string gizmoBoundsColor;
            public float gizmoAxisLength;
            public bool enableSceneGizmos;
            public bool showSceneGrid;
            public bool showSceneBounds;
            public bool showStageAreaGizmo;
            public bool showPlacementHeightGizmo;
            public string placementHeightColor;
            public bool enableDropIndicator;
            public string validDropColor;
            public string invalidDropColor;
            public float dropIndicatorSize;
            public bool autoPreview;
            public float previewDuration;
        }

        [Serializable]
        private class JsonSandboxExportSettings
        {
            public bool includeObjectLibrary;
            public bool includeTimelineData;
            public bool compressTextures;
            public bool optimizeMeshes;
            public List<string> targetPlatforms;
            public string packageName;
            public string packageVersion;
            public string packageAuthor;
            public bool createExecutable;
            public bool createPackage;
            public string outputDirectory;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for creating sample sandbox projects
    /// </summary>
    public static class SampleSandboxProjectCreator
    {
        /// <summary>
        /// Create a sample sandbox project with basic objects for testing
        /// </summary>
        /// <returns>Sample project</returns>
        public static SandboxProjectData CreateSampleProject()
        {
            var project = new SandboxProjectData("Sample Sandbox Project")
            {
                description = "A sample project demonstrating Scene Sandbox Builder capabilities"
            };

            // Configure sample scene - get the active scene and update its name
            var activeScene = project.GetActiveScene();
            if (activeScene != null)
            {
                activeScene.sceneName = "Sample Scene";
            }
            
            // Add some sample placed objects (if we had object IDs to reference)
            // This would typically be done by the SandboxObjectFactory
            
            return project;
        }
    }
}