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
        private SerializedProperty _sceneRoot;
        private SerializedProperty _stageArea;
        
        // Placement Settings
        private SerializedProperty _placementLayers;
        private SerializedProperty _snapToGrid;
        private SerializedProperty _gridSize;
        private SerializedProperty _gridOffset;
        private SerializedProperty _gridPivotOffset;
        private SerializedProperty _defaultPlacementHeight;
        private SerializedProperty _minimumPlacementHeight;
        private SerializedProperty _useRaycastForPlacement;
        private SerializedProperty _useConsistentHeight;
        private SerializedProperty _sceneBounds;
        private SerializedProperty _sceneBoundsOffset;
        private SerializedProperty _sceneBoundsPivot;
        
        // Preview Settings
        private SerializedProperty _timelineDirector;
        private SerializedProperty _autoPreview;
        private SerializedProperty _previewDuration;
        
        // Unified Gizmo Settings
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
        private bool _showAllGizmosFoldout = false;
        private bool _showProjectManagementFoldout = true;
        private bool _showObjectLibraryFoldout = false;
        private bool _showRuntimeInfoFoldout = true;
        
        // Project Management
        private string _newProjectName = "New Sandbox Project";
        private string _projectSaveFileName = "";
        private Vector2 _projectListScrollPos;
        private List<SandboxProjectMetadata> _availableProjects;
        
        // Scene Management (NEW)
        private bool _showSceneManagementFoldout = true;
        private string _newSceneName = "New Scene";
        private Vector2 _sceneListScrollPos;
        private List<SceneMetadata> _currentProjectScenes;
        
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
            RefreshCurrentProjectScenes();
            
            // Subscribe to scene changes for auto-refresh
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            
            // Subscribe to SceneView events for placement handling
            SceneView.duringSceneGui += HandleSceneGUI;
        }
        
        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            
            // Unsubscribe from SceneView events
            SceneView.duringSceneGui -= HandleSceneGUI;
        }
        
        private void HandleSceneGUI(SceneView sceneView)
        {
            if (_target == null || !_target.IsPlacementActive)
                return;

            // Disable Unity's built-in tools during placement
            Tools.current = Tool.None;
            
            // Handle input in Scene view during placement
            Event e = Event.current;
            
            // Get control ID for this handler
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            // Get mouse position in scene view
            Vector2 mousePos = e.mousePosition;
            mousePos.y = sceneView.camera.pixelHeight - mousePos.y; // Flip Y for screen space
            
            EventType eventType = e.GetTypeForControl(controlID);
            
            // Debug current event
            if (eventType == EventType.MouseDown || eventType == EventType.MouseUp)
            {
                Debug.Log($"[HandleSceneGUI] Event: {eventType}, button: {e.button}, controlID: {controlID}, hotControl: {GUIUtility.hotControl}");
            }
            
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.MouseDrag:
                    // Update ghost position
                    _target.UpdatePlacement(mousePos);
                    HandleUtility.Repaint();
                    break;
                    
                case EventType.MouseDown:
                    if (e.button == 0) // Left click
                    {
                        Debug.Log($"[HandleSceneGUI] MouseDown detected at {mousePos}");
                        
                        // Take control to prevent other handlers from processing this event
                        GUIUtility.hotControl = controlID;
                        
                        // Confirm placement on left click
                        GameObject placed = _target.ConfirmPlacement();
                        if (placed != null)
                        {
                            Debug.Log($"[Editor] Placed object: {placed.name}");
                            Selection.activeGameObject = placed;
                        }
                        else
                        {
                            Debug.LogWarning($"[Editor] ConfirmPlacement returned null");
                        }
                        
                        e.Use();
                        HandleUtility.Repaint();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                    
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        // Cancel placement on ESC
                        _target.CancelPlacement();
                        Debug.Log("[Editor] Placement cancelled");
                        e.Use();
                        HandleUtility.Repaint();
                    }
                    break;
                    
                case EventType.Layout:
                    // Add control during layout to receive events
                    HandleUtility.AddDefaultControl(controlID);
                    break;
                    
                case EventType.Repaint:
                    // Draw instruction overlay
                    Handles.BeginGUI();
                    GUIStyle style = new GUIStyle(GUI.skin.box);
                    style.normal.textColor = Color.yellow;
                    style.fontSize = 14;
                    style.alignment = TextAnchor.MiddleCenter;
                    
                    // Use GUI.Box instead of GUILayout to avoid layout issues during repaint
                    GUI.Box(new Rect(10, 10, 300, 60), "PLACEMENT MODE\nLeft Click: Place | ESC: Cancel", style);
                    Handles.EndGUI();
                    break;
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            DrawProjectManagement();
            DrawSceneManagement(); // NEW: Multi-scene management
            DrawRuntimeInfo();
            DrawConfiguration();
            DrawPlacementSettings();
            DrawPreviewSettings();
            DrawUnifiedGizmoSettings();
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
            _sceneRoot = serializedObject.FindProperty("_sceneRoot");
            _stageArea = serializedObject.FindProperty("_stageArea");
            
            // Placement Settings
            _placementLayers = serializedObject.FindProperty("_placementLayers");
            _snapToGrid = serializedObject.FindProperty("_snapToGrid");
            _gridSize = serializedObject.FindProperty("_gridSize");
            _gridOffset = serializedObject.FindProperty("_gridOffset");
            _gridPivotOffset = serializedObject.FindProperty("_gridPivotOffset");
            _defaultPlacementHeight = serializedObject.FindProperty("_defaultPlacementHeight");
            _minimumPlacementHeight = serializedObject.FindProperty("_minimumPlacementHeight");
            _useRaycastForPlacement = serializedObject.FindProperty("_useRaycastForPlacement");
            _useConsistentHeight = serializedObject.FindProperty("_useConsistentHeight");
            _sceneBounds = serializedObject.FindProperty("_sceneBounds");
            _sceneBoundsOffset = serializedObject.FindProperty("_sceneBoundsOffset");
            _sceneBoundsPivot = serializedObject.FindProperty("_sceneBoundsPivot");
            
            // Preview Settings
            _timelineDirector = serializedObject.FindProperty("_timelineDirector");
            _autoPreview = serializedObject.FindProperty("_autoPreview");
            _previewDuration = serializedObject.FindProperty("_previewDuration");
            
            // Unified Gizmo Settings
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
            
            // Scene status with count
            if (_target.CurrentProject != null)
            {
                int sceneCount = _target.GetSceneCount();
                string sceneStatus = _target.CurrentScene != null ? 
                    $"Scene: {_target.CurrentScene.sceneName} ({sceneCount} total)" : $"No Scene ({sceneCount} total)";
                EditorGUILayout.LabelField(sceneStatus);
            }
            else
            {
                string sceneStatus = _target.CurrentScene != null ? 
                    $"Scene: {_target.CurrentScene.sceneName}" : "No Scene";
                EditorGUILayout.LabelField(sceneStatus);
            }
            
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
                RefreshCurrentProjectScenes();
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
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Name: {projectInfo.projectName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Objects: {projectInfo.objectCount} | Scenes: {_target.GetSceneCount()}");
                    EditorGUILayout.LabelField($"Created: {projectInfo.created:yyyy-MM-dd HH:mm}");
                    EditorGUILayout.LabelField($"Last Modified: {projectInfo.lastModified:yyyy-MM-dd HH:mm}");
                    
                    // Display key settings
                    if (_target.CurrentProject.settings != null)
                    {
                        var settings = _target.CurrentProject.settings;
                        EditorGUILayout.Space(3);
                        EditorGUILayout.LabelField("Project Settings:", EditorStyles.miniBoldLabel);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField($"Grid: {(settings.snapToGrid ? $"Enabled ({settings.gridSize}m)" : "Disabled")}");
                        EditorGUILayout.LabelField($"Scene Bounds: {settings.sceneBounds.x} × {settings.sceneBounds.y} × {settings.sceneBounds.z}");
                        EditorGUILayout.LabelField($"Raycast Placement: {(settings.useRaycastForPlacement ? "Enabled" : "Disabled")}");
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(3);
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
                            RefreshCurrentProjectScenes();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "Failed to save project.", "OK");
                        }
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    
                    // Quick Actions
                    EditorGUILayout.Space(3);
                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = Application.isPlaying;
                    if (GUILayout.Button("Save As Copy"))
                    {
                        string newName = EditorUtility.SaveFilePanel(
                            "Save Project Copy",
                            _target.CurrentProject.projectName,
                            _projectSaveFileName + "_copy.sbproj",
                            "sbproj");
                        
                        if (!string.IsNullOrEmpty(newName))
                        {
                            if (_target.SaveProject(newName))
                            {
                                EditorUtility.DisplayDialog("Success", "Project copy saved!", "OK");
                                RefreshAvailableProjects();
                            }
                        }
                    }
                    
                    if (GUILayout.Button("Export Settings"))
                    {
                        // Export just the settings to JSON
                        string exportPath = EditorUtility.SaveFilePanel(
                            "Export Project Settings",
                            _target.CurrentProject.projectName,
                            _target.CurrentProject.projectName + "_settings.json",
                            "json");
                        
                        if (!string.IsNullOrEmpty(exportPath))
                        {
                            string json = JsonUtility.ToJson(_target.CurrentProject.settings, true);
                            System.IO.File.WriteAllText(exportPath, json);
                            EditorUtility.DisplayDialog("Success", "Settings exported!", "OK");
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
                            RefreshCurrentProjectScenes();
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
            
            EditorGUILayout.Space(10);
            
            // Save/Load Path Settings (Read-Only)
            EditorGUILayout.LabelField("Save/Load Path Settings (Read-Only)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Save paths are automatically managed using Application.persistentDataPath for cross-platform compatibility.\n" +
                "Root folder is locked to ensure data consistency across all platforms.\n\n" +
                $"Root: {Application.persistentDataPath}", 
                MessageType.Info);
            
            // Display current paths as read-only
            GUI.enabled = false;
            EditorGUILayout.TextField("Scene Save Path", _defaultSavePath.stringValue);
            EditorGUILayout.TextField("Project Save Path", _defaultProjectSavePath.stringValue);
            GUI.enabled = true;
            
            EditorGUILayout.PropertyField(_currentSceneName, new GUIContent("Current Scene Name", "Name for the current scene configuration"));
            
            EditorGUILayout.Space(5);
            
            // Quick access buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Scenes Folder"))
            {
                string scenesPath = System.IO.Path.Combine(Application.persistentDataPath, "SceneSandboxBuilder", "SavedScenes");
                if (System.IO.Directory.Exists(scenesPath))
                {
                    Application.OpenURL("file://" + scenesPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Folder Not Found", 
                        "Scenes folder doesn't exist yet. It will be created when you save your first scene.", 
                        "OK");
                }
            }
            
            if (GUILayout.Button("Open Projects Folder"))
            {
                string projectsPath = System.IO.Path.Combine(Application.persistentDataPath, "SceneSandboxBuilder", "SavedProjects");
                if (System.IO.Directory.Exists(projectsPath))
                {
                    Application.OpenURL("file://" + projectsPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Folder Not Found", 
                        "Projects folder doesn't exist yet. It will be created when you save your first project.", 
                        "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Open Root Data Folder"))
            {
                Application.OpenURL("file://" + Application.persistentDataPath);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneManagement()
        {
            if (_target.CurrentProject == null || !Application.isPlaying) return;
            
            _showSceneManagementFoldout = EditorGUILayout.Foldout(_showSceneManagementFoldout, 
                "Scene Management", true, EditorStyles.foldoutHeader);
            
            if (!_showSceneManagementFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Create New Scene
            EditorGUILayout.LabelField("Create New Scene", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newSceneName = EditorGUILayout.TextField("Scene Name:", _newSceneName);
            
            GUI.enabled = !string.IsNullOrEmpty(_newSceneName);
            if (GUILayout.Button("Create Scene", GUILayout.Width(100)))
            {
                _target.CreateNewSceneInProject(_newSceneName);
                RefreshCurrentProjectScenes();
                _newSceneName = "New Scene";
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Current Scene Actions
            if (_target.CurrentScene != null)
            {
                EditorGUILayout.LabelField("Current Scene Actions", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Duplicate Scene"))
                {
                    var duplicate = _target.DuplicateCurrentScene();
                    if (duplicate != null)
                    {
                        EditorUtility.DisplayDialog("Success", $"Scene duplicated: {duplicate.sceneName}", "OK");
                        RefreshCurrentProjectScenes();
                    }
                }
                
                if (GUILayout.Button("Rename Scene"))
                {
                    string newName = EditorUtility.SaveFilePanel("Rename Scene", "", _target.CurrentScene.sceneName, "");
                    if (!string.IsNullOrEmpty(newName))
                    {
                        newName = System.IO.Path.GetFileNameWithoutExtension(newName);
                        _target.RenameCurrentScene(newName);
                        RefreshCurrentProjectScenes();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // Scene List
            EditorGUILayout.LabelField($"Scenes in Project ({_target.GetSceneCount()})", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshCurrentProjectScenes();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            if (_currentProjectScenes != null && _currentProjectScenes.Count > 0)
            {
                _sceneListScrollPos = EditorGUILayout.BeginScrollView(_sceneListScrollPos, GUILayout.Height(200));
                
                foreach (var sceneMetadata in _currentProjectScenes)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Scene header
                    EditorGUILayout.BeginHorizontal();
                    
                    // Active indicator
                    if (sceneMetadata.isActive)
                    {
                        GUIStyle activeStyle = new GUIStyle(EditorStyles.boldLabel);
                        activeStyle.normal.textColor = Color.green;
                        EditorGUILayout.LabelField("● " + sceneMetadata.sceneName, activeStyle);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(sceneMetadata.sceneName, EditorStyles.boldLabel);
                    }
                    
                    GUILayout.FlexibleSpace();
                    
                    // Switch button
                    if (!sceneMetadata.isActive)
                    {
                        if (GUILayout.Button("Switch", GUILayout.Width(60)))
                        {
                            if (_target.SwitchToScene(sceneMetadata.sceneId))
                            {
                                RefreshCurrentProjectScenes();
                                Repaint();
                            }
                        }
                    }
                    
                    // Delete button (can't delete if only one scene)
                    GUI.enabled = _target.GetSceneCount() > 1;
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Scene", 
                            $"Are you sure you want to delete scene '{sceneMetadata.sceneName}'?\n\nThis action cannot be undone.", 
                            "Delete", "Cancel"))
                        {
                            if (_target.DeleteSceneFromProject(sceneMetadata.sceneId))
                            {
                                EditorUtility.DisplayDialog("Success", $"Scene '{sceneMetadata.sceneName}' deleted!", "OK");
                                RefreshCurrentProjectScenes();
                            }
                        }
                    }
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Scene details
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Objects: {sceneMetadata.objectCount}", EditorStyles.miniLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Modified: {sceneMetadata.lastModified:MM/dd HH:mm}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    if (!string.IsNullOrEmpty(sceneMetadata.description))
                    {
                        EditorGUILayout.LabelField($"Description: {sceneMetadata.description}", EditorStyles.wordWrappedMiniLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No scenes in project.", MessageType.Info);
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
            EditorGUILayout.PropertyField(_sceneRoot);
            EditorGUILayout.PropertyField(_stageArea);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Input handling is managed by TransformableSelectionManager (singleton)", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPlacementSettings()
        {
            _showPlacementFoldout = EditorGUILayout.Foldout(_showPlacementFoldout, 
                "Placement Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showPlacementFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_placementLayers, new GUIContent("Placement Layers"));
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_snapToGrid, new GUIContent("Snap to Grid"));
            
            if (_snapToGrid.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_gridSize, new GUIContent("Grid Cell Size", "Size of each grid cell"));
                EditorGUILayout.PropertyField(_gridOffset, new GUIContent("Grid Offset", "Additional offset from stage area (X,Z for 2D plane)"));
                EditorGUILayout.PropertyField(_gridPivotOffset, new GUIContent("Grid Pivot Offset", "Pivot offset relative to Scene Bounds Pivot"));
                EditorGUILayout.HelpBox("Grid is a 2D plane based on Scene Bounds (X,Z). Grid position = Scene Bounds Pivot + Grid Pivot Offset.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Height Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Height values are relative to the Grid's Y position (calculated from Scene Bounds + Pivots + Offsets).", MessageType.Info);
            EditorGUILayout.PropertyField(_defaultPlacementHeight, new GUIContent("Default Height", "Default Y offset from grid when placing objects (relative to grid)"));
            EditorGUILayout.PropertyField(_minimumPlacementHeight, new GUIContent("Minimum Height", "Minimum Y offset from grid (relative to grid)"));
            
            // Validation: Ensure default >= minimum
            if (_defaultPlacementHeight.floatValue < _minimumPlacementHeight.floatValue)
            {
                EditorGUILayout.HelpBox($"Warning: Default Height ({_defaultPlacementHeight.floatValue:F2}) is less than Minimum Height ({_minimumPlacementHeight.floatValue:F2}). This may cause unexpected behavior.", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(_useRaycastForPlacement, new GUIContent("Use Raycast"));
            EditorGUILayout.PropertyField(_useConsistentHeight, new GUIContent("Use Consistent Height"));
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Scene Bounds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_sceneBounds, new GUIContent("Bounds Size", "The size of the scene bounds (X, Y, Z)"));
            EditorGUILayout.PropertyField(_sceneBoundsOffset, new GUIContent("Bounds Offset", "Offset from stage area center (X, Y, Z)"));
            EditorGUILayout.PropertyField(_sceneBoundsPivot, new GUIContent("Bounds Pivot", "Pivot point for scene bounds positioning"));
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Drop Indicator", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableDropIndicator, new GUIContent("Enable Drop Indicator"));
            
            if (_enableDropIndicator.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_dropIndicatorPrefab, new GUIContent("Prefab"));
                EditorGUILayout.PropertyField(_validDropColor, new GUIContent("Valid Color"));
                EditorGUILayout.PropertyField(_invalidDropColor, new GUIContent("Invalid Color"));
                EditorGUILayout.PropertyField(_dropIndicatorSize, new GUIContent("Size"));
                EditorGUI.indentLevel--;
            }
            
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
        
        private void DrawUnifiedGizmoSettings()
        {
            _showAllGizmosFoldout = EditorGUILayout.Foldout(_showAllGizmosFoldout, 
                "Gizmo Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showAllGizmosFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Master toggle for all gizmos
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enable All Gizmos", EditorStyles.boldLabel, GUILayout.Width(150));
            
            if (!Application.isPlaying)
            {
                GUI.enabled = false;
            }
            
            // Toggle shows ON only when ALL gizmos are enabled
            bool enableAllGizmos = _target.GizmosEnabled && _target.SceneGizmosEnabled && _target.DropIndicatorEnabled;
            bool newEnableAllGizmos = EditorGUILayout.Toggle(enableAllGizmos);
            
            if (newEnableAllGizmos != enableAllGizmos && Application.isPlaying)
            {
                _target.SetGizmoVisibility(newEnableAllGizmos);
                _target.SetSceneGizmoVisibility(newEnableAllGizmos);
                _target.SetDropIndicatorEnabled(newEnableAllGizmos);
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Gizmo controls are only available in Play Mode", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Object Gizmos
            EditorGUILayout.LabelField("Object Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableGizmos, new GUIContent("Enable Object Gizmos"));
            
            if (_enableGizmos.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_showBoundsGizmo, new GUIContent("Show Bounds"));
                EditorGUILayout.PropertyField(_showAxesGizmo, new GUIContent("Show Axes"));
                EditorGUILayout.PropertyField(_showHandlesGizmo, new GUIContent("Show Handles"));
                EditorGUILayout.PropertyField(_gizmoBoundsColor, new GUIContent("Bounds Color"));
                EditorGUILayout.PropertyField(_gizmoAxisLength, new GUIContent("Axis Length"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);
            
            // Scene Gizmos
            EditorGUILayout.LabelField("Scene Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableSceneGizmos, new GUIContent("Enable Scene Gizmos"));
            
            if (_enableSceneGizmos.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_showSceneGrid, new GUIContent("Show Grid"));
                EditorGUILayout.PropertyField(_showSceneBounds, new GUIContent("Show Scene Bounds"));
                EditorGUILayout.PropertyField(_showStageAreaGizmo, new GUIContent("Show Stage Area"));
                EditorGUILayout.PropertyField(_showPlacementHeightGizmo, new GUIContent("Show Placement Height"));
                EditorGUILayout.PropertyField(_sceneBoundsColor, new GUIContent("Bounds Color"));
                EditorGUILayout.PropertyField(_placementHeightColor, new GUIContent("Placement Height Color"));
                EditorGUI.indentLevel--;
            }
            
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
                    
                    // Place button - triggers drag&drop flow
                    GUI.enabled = Application.isPlaying && _target.CurrentScene != null;
                    if (GUILayout.Button("Place", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        Debug.Log($"[Editor] Place button clicked for {objectData.displayName}, isPlaying={Application.isPlaying}");
                        
                        // Trigger drag&drop flow - ghost will appear at screen center
                        // User can then:
                        // - Move mouse to reposition ghost (follows cursor automatically)
                        // - Click to confirm placement (triggers OnEmptySpaceClicked event)
                        // - Press ESC to cancel (triggers OnCancelRequested event)
                        _target.BeginPlacement(objectData.id);
                        
                        Debug.Log($"[Editor] Started placement for {objectData.displayName}. Move mouse, click to place, ESC to cancel.");
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
                    RefreshCurrentProjectScenes();
                }
            }
            
            GUI.enabled = Application.isPlaying && _target.CurrentProject != null;
            if (GUILayout.Button("Save Project", EditorStyles.toolbarButton))
            {
                _target.SaveProject();
                RefreshAvailableProjects();
                RefreshCurrentProjectScenes();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            // Scene operations (NEW)
            GUI.enabled = Application.isPlaying && _target.CurrentProject != null;
            if (GUILayout.Button("New Scene", EditorStyles.toolbarButton))
            {
                _target.CreateNewSceneInProject();
                RefreshCurrentProjectScenes();
            }
            
            if (GUILayout.Button("Duplicate Scene", EditorStyles.toolbarButton))
            {
                if (_target.CurrentScene != null)
                {
                    _target.DuplicateCurrentScene();
                    RefreshCurrentProjectScenes();
                }
            }
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            
            // Scene count indicator
            if (Application.isPlaying && _target.CurrentProject != null)
            {
                int sceneCount = _target.GetSceneCount();
                string sceneLabel = sceneCount == 1 ? "scene" : "scenes";
                EditorGUILayout.LabelField($"{sceneCount} {sceneLabel}", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            
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
        
        private void RefreshCurrentProjectScenes()
        {
            if (_target != null && _target.CurrentProject != null)
            {
                _currentProjectScenes = _target.GetAllSceneMetadata();
            }
            else
            {
                _currentProjectScenes = new List<SceneMetadata>();
            }
        }
        
        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            RefreshAvailableProjects();
            RefreshCurrentProjectScenes();
        }
        
        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            RefreshAvailableProjects();
            RefreshCurrentProjectScenes();
        }
        
        #endregion
    }
}