using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using SceneSandbox.Core;
using SceneSandbox.Data;
using SceneSandbox.Serialization;

namespace SceneSandbox.Editor
{
    /// <summary>
    /// Custom editor for SceneSandboxBuilder with enhanced UI and project management
    /// Follows the Command Pattern for undo/redo operations and modular DLC architecture
    /// </summary>
    [CustomEditor(typeof(SceneSandboxBuilder))]
    public class SceneSandboxBuilderEditor : UnityEditor.Editor
    {
        #region SerializedProperties
        
        // Configuration
        private SerializedProperty _objectLibrary;
        private SerializedProperty _inputActions;
        private SerializedProperty _sceneRoot;
        private SerializedProperty _stageArea;
        
        // Placement Settings
        private SerializedProperty _placementLayers;
        private SerializedProperty _snapToGrid;
        private SerializedProperty _gridSize;
        private SerializedProperty _defaultPlacementHeight;
        private SerializedProperty _minimumPlacementHeight;
        private SerializedProperty _useRaycastForPlacement;
        private SerializedProperty _useConsistentHeight;
        
        // Preview Settings
        private SerializedProperty _timelineDirector;
        private SerializedProperty _autoPreview;
        private SerializedProperty _previewDuration;
        
        // Gizmo Settings
        private SerializedProperty _enableGizmos;
        private SerializedProperty _showBoundsGizmo;
        private SerializedProperty _showAxesGizmo;
        private SerializedProperty _showHandlesGizmo;
        private SerializedProperty _gizmoBoundsColor;
        private SerializedProperty _gizmoAxisLength;
        
        // Scene Gizmo Settings
        private SerializedProperty _enableSceneGizmos;
        private SerializedProperty _showSceneGrid;
        private SerializedProperty _showSceneBounds;
        private SerializedProperty _showStageAreaGizmo;
        private SerializedProperty _showPlacementHeightGizmo;
        private SerializedProperty _sceneBounds;
        private SerializedProperty _sceneBoundsColor;
        private SerializedProperty _placementHeightColor;
        
        // Drop Indicator Settings
        private SerializedProperty _enableDropIndicator;
        private SerializedProperty _dropIndicatorPrefab;
        private SerializedProperty _validDropColor;
        private SerializedProperty _invalidDropColor;
        private SerializedProperty _dropIndicatorSize;
        
        // Save/Load
        private SerializedProperty _defaultSavePath;
        private SerializedProperty _currentSceneName;
        private SerializedProperty _defaultProjectSavePath;
        
        #endregion
        
        #region Editor State
        
        private SceneSandboxBuilder _target;
        private bool _showConfigurationFoldout = true;
        private bool _showPlacementFoldout = true;
        private bool _showPreviewFoldout = false;
        private bool _showGizmoFoldout = false;
        private bool _showSceneGizmoFoldout = false;
        private bool _showDropIndicatorFoldout = false;
        private bool _showSaveLoadFoldout = false;
        private bool _showProjectManagementFoldout = true;
        private bool _showObjectLibraryFoldout = false;
        private bool _showRuntimeInfoFoldout = true;
        
        // Project Management
        private string _newProjectName = "New Sandbox Project";
        private string _projectSaveFileName = "";
        private Vector2 _projectListScrollPos;
        private List<SandboxProjectMetadata> _availableProjects;
        
        // Object Library Browser
        private Vector2 _objectLibraryScrollPos;
        private string _objectSearchFilter = "";
        
        // Runtime Info
        private Vector2 _runtimeInfoScrollPos;
        
        #endregion
        
        #region Unity Editor Callbacks
        
        private void OnEnable()
        {
            _target = (SceneSandboxBuilder)target;
            CacheSerializedProperties();
            RefreshAvailableProjects();
            
            // Subscribe to scene changes for auto-refresh
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }
        
        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            DrawProjectManagement();
            DrawRuntimeInfo();
            DrawConfiguration();
            DrawPlacementSettings();
            DrawPreviewSettings();
            DrawGizmoSettings();
            DrawSceneGizmoSettings();
            DrawDropIndicatorSettings();
            DrawSaveLoadSettings();
            DrawObjectLibraryBrowser();
            
            EditorGUILayout.Space(10);
            DrawToolbar();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
        
        #region Property Caching
        
        private void CacheSerializedProperties()
        {
            // Configuration
            _objectLibrary = serializedObject.FindProperty("_objectLibrary");
            _inputActions = serializedObject.FindProperty("_inputActions");
            _sceneRoot = serializedObject.FindProperty("_sceneRoot");
            _stageArea = serializedObject.FindProperty("_stageArea");
            
            // Placement Settings
            _placementLayers = serializedObject.FindProperty("_placementLayers");
            _snapToGrid = serializedObject.FindProperty("_snapToGrid");
            _gridSize = serializedObject.FindProperty("_gridSize");
            _defaultPlacementHeight = serializedObject.FindProperty("_defaultPlacementHeight");
            _minimumPlacementHeight = serializedObject.FindProperty("_minimumPlacementHeight");
            _useRaycastForPlacement = serializedObject.FindProperty("_useRaycastForPlacement");
            _useConsistentHeight = serializedObject.FindProperty("_useConsistentHeight");
            
            // Preview Settings
            _timelineDirector = serializedObject.FindProperty("_timelineDirector");
            _autoPreview = serializedObject.FindProperty("_autoPreview");
            _previewDuration = serializedObject.FindProperty("_previewDuration");
            
            // Gizmo Settings
            _enableGizmos = serializedObject.FindProperty("_enableGizmos");
            _showBoundsGizmo = serializedObject.FindProperty("_showBoundsGizmo");
            _showAxesGizmo = serializedObject.FindProperty("_showAxesGizmo");
            _showHandlesGizmo = serializedObject.FindProperty("_showHandlesGizmo");
            _gizmoBoundsColor = serializedObject.FindProperty("_gizmoBoundsColor");
            _gizmoAxisLength = serializedObject.FindProperty("_gizmoAxisLength");
            
            // Scene Gizmo Settings
            _enableSceneGizmos = serializedObject.FindProperty("_enableSceneGizmos");
            _showSceneGrid = serializedObject.FindProperty("_showSceneGrid");
            _showSceneBounds = serializedObject.FindProperty("_showSceneBounds");
            _showStageAreaGizmo = serializedObject.FindProperty("_showStageAreaGizmo");
            _showPlacementHeightGizmo = serializedObject.FindProperty("_showPlacementHeightGizmo");
            _sceneBounds = serializedObject.FindProperty("_sceneBounds");
            _sceneBoundsColor = serializedObject.FindProperty("_sceneBoundsColor");
            _placementHeightColor = serializedObject.FindProperty("_placementHeightColor");
            
            // Drop Indicator Settings
            _enableDropIndicator = serializedObject.FindProperty("_enableDropIndicator");
            _dropIndicatorPrefab = serializedObject.FindProperty("_dropIndicatorPrefab");
            _validDropColor = serializedObject.FindProperty("_validDropColor");
            _invalidDropColor = serializedObject.FindProperty("_invalidDropColor");
            _dropIndicatorSize = serializedObject.FindProperty("_dropIndicatorSize");
            
            // Save/Load
            _defaultSavePath = serializedObject.FindProperty("_defaultSavePath");
            _currentSceneName = serializedObject.FindProperty("_currentSceneName");
            _defaultProjectSavePath = serializedObject.FindProperty("_defaultProjectSavePath");
        }
        
        #endregion
        
        #region GUI Drawing Methods
        
        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("Scene Sandbox Builder", EditorStyles.largeLabel);
            GUILayout.Label("Ero Director - Mobile & AR Sandbox Edition", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(5);
            
            // Status indicators
            EditorGUILayout.BeginHorizontal();
            
            // Project status
            string projectStatus = _target.CurrentProject != null ? 
                $"Project: {_target.CurrentProject.projectName}" : "No Project Loaded";
            EditorGUILayout.LabelField("Status:", projectStatus);
            
            // Scene status
            string sceneStatus = _target.CurrentScene != null ? 
                $"Scene: {_target.CurrentScene.sceneName}" : "No Scene";
            EditorGUILayout.LabelField(sceneStatus);
            
            EditorGUILayout.EndHorizontal();
            
            // Object count
            if (Application.isPlaying)
            {
                int objectCount = _target.CurrentScene?.placedObjects?.Count ?? 0;
                EditorGUILayout.LabelField($"Placed Objects: {objectCount}");
                
                if (_target.IsInPreviewMode)
                {
                    EditorGUILayout.HelpBox("Preview Mode Active", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawProjectManagement()
        {
            _showProjectManagementFoldout = EditorGUILayout.Foldout(_showProjectManagementFoldout, 
                "Project Management", true, EditorStyles.foldoutHeader);
            
            if (!_showProjectManagementFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // New Project Section
            EditorGUILayout.LabelField("Create New Project", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newProjectName = EditorGUILayout.TextField("Project Name:", _newProjectName);
            
            GUI.enabled = !string.IsNullOrEmpty(_newProjectName) && Application.isPlaying;
            if (GUILayout.Button("Create", GUILayout.Width(80)))
            {
                _target.CreateNewProject(_newProjectName);
                RefreshAvailableProjects();
                _newProjectName = "New Sandbox Project";
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Current Project Section
            if (_target.CurrentProject != null)
            {
                EditorGUILayout.LabelField("Current Project", EditorStyles.boldLabel);
                var projectInfo = _target.GetCurrentProjectInfo();
                
                if (projectInfo != null)
                {
                    EditorGUILayout.LabelField($"Name: {projectInfo.projectName}");
                    EditorGUILayout.LabelField($"Objects: {projectInfo.objectCount}");
                    EditorGUILayout.LabelField($"Created: {projectInfo.created:yyyy-MM-dd HH:mm}");
                    EditorGUILayout.LabelField($"Modified: {projectInfo.lastModified:yyyy-MM-dd HH:mm}");
                    
                    EditorGUILayout.BeginHorizontal();
                    _projectSaveFileName = EditorGUILayout.TextField("File Name:", _projectSaveFileName);
                    
                    GUI.enabled = Application.isPlaying;
                    if (GUILayout.Button("Save", GUILayout.Width(80)))
                    {
                        string savePath = string.IsNullOrEmpty(_projectSaveFileName) 
                            ? null 
                            : System.IO.Path.Combine(_target.CurrentProject.projectName, _projectSaveFileName + ".sbproj");
                        
                        if (_target.SaveProject(savePath))
                        {
                            EditorUtility.DisplayDialog("Success", "Project saved successfully!", "OK");
                            RefreshAvailableProjects();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "Failed to save project.", "OK");
                        }
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.Space(5);
            
            // Available Projects Section
            EditorGUILayout.LabelField("Available Projects", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
            {
                RefreshAvailableProjects();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            if (_availableProjects != null && _availableProjects.Count > 0)
            {
                _projectListScrollPos = EditorGUILayout.BeginScrollView(_projectListScrollPos, GUILayout.Height(150));
                
                foreach (var project in _availableProjects)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(project.projectName, EditorStyles.boldLabel);
                    
                    GUI.enabled = Application.isPlaying;
                    if (GUILayout.Button("Load", GUILayout.Width(60)))
                    {
                        if (_target.LoadProject(project.filePath))
                        {
                            EditorUtility.DisplayDialog("Success", $"Project '{project.projectName}' loaded successfully!", "OK");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "Failed to load project.", "OK");
                        }
                    }
                    
                    GUI.enabled = true;
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Project", 
                            $"Are you sure you want to delete project '{project.projectName}'?\n\nThis action cannot be undone.", 
                            "Delete", "Cancel"))
                        {
                            if (_target.DeleteProject(project.filePath))
                            {
                                EditorUtility.DisplayDialog("Success", $"Project '{project.projectName}' deleted successfully!", "OK");
                                RefreshAvailableProjects();
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "Failed to delete project.", "OK");
                            }
                        }
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField($"Objects: {project.objectCount} | Created: {project.created:MM/dd/yyyy}");
                    if (!string.IsNullOrEmpty(project.description))
                    {
                        EditorGUILayout.LabelField($"Description: {project.description}", EditorStyles.wordWrappedMiniLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No saved projects found.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRuntimeInfo()
        {
            if (!Application.isPlaying) return;
            
            _showRuntimeInfoFoldout = EditorGUILayout.Foldout(_showRuntimeInfoFoldout, 
                "Runtime Information", true, EditorStyles.foldoutHeader);
            
            if (!_showRuntimeInfoFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Current state
            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Selected Object: {(_target.SelectedObject != null ? _target.SelectedObject.name : "None")}");
            EditorGUILayout.LabelField($"Gizmos Enabled: {_target.GizmosEnabled}");
            EditorGUILayout.LabelField($"Scene Gizmos Enabled: {_target.SceneGizmosEnabled}");
            EditorGUILayout.LabelField($"Drop Indicator Enabled: {_target.DropIndicatorEnabled}");
            EditorGUILayout.LabelField($"Preview Mode: {(_target.IsInPreviewMode ? "Active" : "Inactive")}");
            
            EditorGUILayout.Space(5);
            
            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Scene"))
            {
                if (EditorUtility.DisplayDialog("Clear Scene", 
                    "Are you sure you want to clear all objects from the scene?", "Yes", "No"))
                {
                    _target.ClearScene();
                }
            }
            
            if (GUILayout.Button("New Scene"))
            {
                _target.CreateNewScene();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (_target.IsInPreviewMode)
            {
                if (GUILayout.Button("Stop Preview"))
                {
                    _target.StopPreview();
                }
            }
            else
            {
                GUI.enabled = _target.CurrentScene != null && _target.CurrentScene.placedObjects.Count > 0;
                if (GUILayout.Button("Start Preview"))
                {
                    _target.StartPreview();
                }
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawConfiguration()
        {
            _showConfigurationFoldout = EditorGUILayout.Foldout(_showConfigurationFoldout, 
                "Configuration", true, EditorStyles.foldoutHeader);
            
            if (!_showConfigurationFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_objectLibrary);
            EditorGUILayout.PropertyField(_inputActions);
            EditorGUILayout.PropertyField(_sceneRoot);
            EditorGUILayout.PropertyField(_stageArea);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPlacementSettings()
        {
            _showPlacementFoldout = EditorGUILayout.Foldout(_showPlacementFoldout, 
                "Placement Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showPlacementFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_placementLayers);
            EditorGUILayout.PropertyField(_snapToGrid);
            
            if (_snapToGrid.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_gridSize);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(_defaultPlacementHeight);
            EditorGUILayout.PropertyField(_minimumPlacementHeight);
            EditorGUILayout.PropertyField(_useRaycastForPlacement);
            EditorGUILayout.PropertyField(_useConsistentHeight);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPreviewSettings()
        {
            _showPreviewFoldout = EditorGUILayout.Foldout(_showPreviewFoldout, 
                "Preview Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showPreviewFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_timelineDirector);
            EditorGUILayout.PropertyField(_autoPreview);
            EditorGUILayout.PropertyField(_previewDuration);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGizmoSettings()
        {
            _showGizmoFoldout = EditorGUILayout.Foldout(_showGizmoFoldout, 
                "Gizmo Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showGizmoFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_enableGizmos);
            
            if (_enableGizmos.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_showBoundsGizmo);
                EditorGUILayout.PropertyField(_showAxesGizmo);
                EditorGUILayout.PropertyField(_showHandlesGizmo);
                EditorGUILayout.PropertyField(_gizmoBoundsColor);
                EditorGUILayout.PropertyField(_gizmoAxisLength);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneGizmoSettings()
        {
            _showSceneGizmoFoldout = EditorGUILayout.Foldout(_showSceneGizmoFoldout, 
                "Scene Gizmo Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showSceneGizmoFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_enableSceneGizmos);
            
            if (_enableSceneGizmos.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_showSceneGrid);
                EditorGUILayout.PropertyField(_showSceneBounds);
                EditorGUILayout.PropertyField(_showStageAreaGizmo);
                EditorGUILayout.PropertyField(_showPlacementHeightGizmo);
                EditorGUILayout.PropertyField(_sceneBounds);
                EditorGUILayout.PropertyField(_sceneBoundsColor);
                EditorGUILayout.PropertyField(_placementHeightColor);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDropIndicatorSettings()
        {
            _showDropIndicatorFoldout = EditorGUILayout.Foldout(_showDropIndicatorFoldout, 
                "Drop Indicator Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showDropIndicatorFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_enableDropIndicator);
            
            if (_enableDropIndicator.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_dropIndicatorPrefab);
                EditorGUILayout.PropertyField(_validDropColor);
                EditorGUILayout.PropertyField(_invalidDropColor);
                EditorGUILayout.PropertyField(_dropIndicatorSize);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSaveLoadSettings()
        {
            _showSaveLoadFoldout = EditorGUILayout.Foldout(_showSaveLoadFoldout, 
                "Save/Load Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showSaveLoadFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_defaultSavePath);
            EditorGUILayout.PropertyField(_currentSceneName);
            EditorGUILayout.PropertyField(_defaultProjectSavePath);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawObjectLibraryBrowser()
        {
            if (_target.ObjectLibrary == null) return;
            
            _showObjectLibraryFoldout = EditorGUILayout.Foldout(_showObjectLibraryFoldout, 
                "Object Library Browser", true, EditorStyles.foldoutHeader);
            
            if (!_showObjectLibraryFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _objectSearchFilter = EditorGUILayout.TextField(_objectSearchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _objectSearchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Object list
            var objects = _target.ObjectLibrary.GetAllObjects();
            var filteredObjects = string.IsNullOrEmpty(_objectSearchFilter) 
                ? objects 
                : objects.Where(obj => obj.displayName.ToLower().Contains(_objectSearchFilter.ToLower())).ToList();
            
            if (filteredObjects.Any())
            {
                _objectLibraryScrollPos = EditorGUILayout.BeginScrollView(_objectLibraryScrollPos, GUILayout.Height(200));
                
                foreach (var objectData in filteredObjects)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Object info
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(objectData.displayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"ID: {objectData.id}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Category: {objectData.category}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    // Place button
                    GUI.enabled = Application.isPlaying && _target.CurrentScene != null;
                    if (GUILayout.Button("Place", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        Vector3 placePosition = _target.transform.position + Vector3.forward * 2f;
                        _target.PlaceObject(objectData.id, placePosition);
                    }
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No objects found.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // File operations
            if (GUILayout.Button("New Project", EditorStyles.toolbarButton))
            {
                if (Application.isPlaying)
                {
                    _target.CreateNewProject();
                    RefreshAvailableProjects();
                }
            }
            
            GUI.enabled = Application.isPlaying && _target.CurrentProject != null;
            if (GUILayout.Button("Save Project", EditorStyles.toolbarButton))
            {
                _target.SaveProject();
                RefreshAvailableProjects();
            }
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            
            // View toggles
            if (Application.isPlaying)
            {
                GUI.changed = false;
                bool gizmosEnabled = GUILayout.Toggle(_target.GizmosEnabled, "Gizmos", EditorStyles.toolbarButton);
                if (GUI.changed)
                {
                    _target.SetGizmoVisibility(gizmosEnabled);
                }
                
                GUI.changed = false;
                bool sceneGizmosEnabled = GUILayout.Toggle(_target.SceneGizmosEnabled, "Scene Gizmos", EditorStyles.toolbarButton);
                if (GUI.changed)
                {
                    _target.SetSceneGizmoVisibility(sceneGizmosEnabled);
                }
                
                GUI.changed = false;
                bool dropIndicatorEnabled = GUILayout.Toggle(_target.DropIndicatorEnabled, "Drop Indicator", EditorStyles.toolbarButton);
                if (GUI.changed)
                {
                    _target.SetDropIndicatorEnabled(dropIndicatorEnabled);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Helper Methods
        
        private void RefreshAvailableProjects()
        {
            if (_target != null)
            {
                _availableProjects = _target.GetAvailableProjects();
            }
        }
        
        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            RefreshAvailableProjects();
        }
        
        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            RefreshAvailableProjects();
        }
        
        #endregion
    }
}