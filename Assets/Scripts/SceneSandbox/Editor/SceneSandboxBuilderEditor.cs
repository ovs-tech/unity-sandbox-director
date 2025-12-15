using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Systems.SceneSandbox.Core;
using Systems.SceneSandbox.Data;
using Systems.SceneSandbox.Serialization;

namespace Systems.SceneSandbox.Editor
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
        private SerializedProperty _libraryController;
        private SerializedProperty _sceneRoot;
        private SerializedProperty _stageArea;
        
        // Input Action References
        private SerializedProperty _toggleModeActionRef;
        private SerializedProperty _exitAllModesActionRef;
        private SerializedProperty _moveHotkeyActionRef;
        private SerializedProperty _rotateHotkeyActionRef;
        private SerializedProperty _scaleHotkeyActionRef;
        private SerializedProperty _transformModeIncreaseActionRef;
        private SerializedProperty _transformModeDecreaseActionRef;
        private SerializedProperty _transformModeToggleAxisActionRef;
        private SerializedProperty _pointerPositionActionRef;
        private SerializedProperty _leftClickActionRef;
        private SerializedProperty _rightClickActionRef;
        private SerializedProperty _mouseScrollActionRef;
        private SerializedProperty _cancelPlacementActionRef;
        private SerializedProperty _switchPlacementItemActionRef;
        private SerializedProperty _startPlacementActionRef;
        
        // Raycast & Input Settings
        private SerializedProperty _maxRaycastDistance;
        private SerializedProperty _dragThreshold;
        private SerializedProperty _doubleClickTime;
        private SerializedProperty _enableHotkeys;
        
        // Placement Settings
        private SerializedProperty _placementLayers;
        private SerializedProperty _selectionLayers;
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
        
        // Placement Validation Settings
        private SerializedProperty _checkCollisions;
        private SerializedProperty _collisionLayers;
        private SerializedProperty _groundLayers;
        private SerializedProperty _ignoreStaticObjects;
        private SerializedProperty _requireSurfaceBelow;
        private SerializedProperty _enableCostSystem;
        private SerializedProperty _placementCost;
        
        // Save/Load
        private SerializedProperty _defaultSavePath;
        private SerializedProperty _currentSceneName;
        private SerializedProperty _defaultProjectSavePath;
        private SerializedProperty _autoLoadFirstProject;
        
        // Debug Settings
        private SerializedProperty _debugLogs;
        
        // Events
        private SerializedProperty _onSceneLoaded;
        private SerializedProperty _onSceneSaved;
        private SerializedProperty _onObjectPlaced;
        private SerializedProperty _onObjectRemoved;
        private SerializedProperty _onObjectSelected;
        private SerializedProperty _onSceneCleared;
        private SerializedProperty _onPreviewStateChanged;
        private SerializedProperty _onModeChanged;
        
        #endregion
        
        #region Editor State
        
        private SceneSandboxBuilder _target;
        private bool _showConfigurationFoldout = true;
        private bool _showPlacementFoldout = true;
        private bool _showInputSettingsFoldout = true;
        private bool _showPreviewFoldout = false;
        private bool _showAllGizmosFoldout = false;
        private bool _showProjectManagementFoldout = true;
        private bool _showObjectLibraryFoldout = false;
        private bool _showRuntimeInfoFoldout = true;
        private bool _showEventsFoldout = false;
        
        // Project Management
        private string _newProjectName = "New Sandbox Project";
        private string _projectSaveFileName = "";
        private Vector2 _projectListScrollPos;
        private List<SandboxProjectMetadata> _availableProjects;
        
        // Scene Management (NEW)
        private string _newSceneName = "New Scene";
        private Vector2 _sceneListScrollPos;
        private List<SceneMetadata> _currentProjectScenes;
        
        // Object Library Browser
        private Vector2 _objectLibraryScrollPos;
        private string _objectSearchFilter = "";
        
        // Runtime Info
        private Vector2 _runtimeInfoScrollPos;
        
        // Transform Items Manager
        private Vector2 _transformItemsScrollPos;
        
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
            if (_target == null || !_target.PlacementSystem.IsActive)
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
                if (_target.DebugLogs) Debug.Log($"[HandleSceneGUI] Event: {eventType}, button: {e.button}, controlID: {controlID}, hotControl: {GUIUtility.hotControl}");
            }
            
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.MouseDrag:
                    HandleUtility.Repaint();
                    break;
                    
                case EventType.MouseDown:
                    if (e.button == 0) // Left click
                    {
                        if (_target.DebugLogs) Debug.Log($"[HandleSceneGUI] MouseDown detected at {mousePos}");
                        
                        // Take control to prevent other handlers from processing this event
                        GUIUtility.hotControl = controlID;
                        
                        // Confirm placement on left click
                        GameObject placed = _target.ConfirmPlacement();
                        if (placed != null)
                        {
                            if (_target.DebugLogs) Debug.Log($"[Editor] Placed object: {placed.name}");
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
                        if (_target.DebugLogs) Debug.Log("[Editor] Placement cancelled");
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
            
            // Setup button for editor initialization
            DrawSetupButton();
            
            EditorGUILayout.Space(10);
            
            DrawProjectAndSceneManagement();
            DrawObjectLibraryBrowser();
            DrawRuntimeInfoAndTransformItems();
            DrawConfiguration();
            DrawInputSettings(); // NEW: Input configuration section
            DrawPlacementSettings();
            DrawPreviewSettings();
            DrawDebugSettings();
            DrawUnifiedGizmoSettings();
            DrawEvents(); // NEW: Events section
            
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
            _libraryController = serializedObject.FindProperty("_libraryController");
            _sceneRoot = serializedObject.FindProperty("_sceneRoot");
            _stageArea = serializedObject.FindProperty("_stageArea");
            
            // Input Action References
            _toggleModeActionRef = serializedObject.FindProperty("_toggleModeActionRef");
            _exitAllModesActionRef = serializedObject.FindProperty("_exitAllModesActionRef");
            _moveHotkeyActionRef = serializedObject.FindProperty("_moveHotkeyActionRef");
            _rotateHotkeyActionRef = serializedObject.FindProperty("_rotateHotkeyActionRef");
            _scaleHotkeyActionRef = serializedObject.FindProperty("_scaleHotkeyActionRef");
            _transformModeIncreaseActionRef = serializedObject.FindProperty("_transformModeIncreaseActionRef");
            _transformModeDecreaseActionRef = serializedObject.FindProperty("_transformModeDecreaseActionRef");
            _transformModeToggleAxisActionRef = serializedObject.FindProperty("_transformModeToggleAxisActionRef");
            _pointerPositionActionRef = serializedObject.FindProperty("_pointerPositionActionRef");
            _leftClickActionRef = serializedObject.FindProperty("_leftClickActionRef");
            _rightClickActionRef = serializedObject.FindProperty("_rightClickActionRef");
            _mouseScrollActionRef = serializedObject.FindProperty("_mouseScrollActionRef");
            _cancelPlacementActionRef = serializedObject.FindProperty("_cancelPlacementActionRef");
            _switchPlacementItemActionRef = serializedObject.FindProperty("_switchPlacementItemActionRef");
            _startPlacementActionRef = serializedObject.FindProperty("_startPlacementActionRef");
            
            // Raycast & Input Settings
            _maxRaycastDistance = serializedObject.FindProperty("_maxRaycastDistance");
            _dragThreshold = serializedObject.FindProperty("_dragThreshold");
            _doubleClickTime = serializedObject.FindProperty("_doubleClickTime");
            _enableHotkeys = serializedObject.FindProperty("_enableHotkeys");
            
            // Placement Settings
            _placementLayers = serializedObject.FindProperty("_placementLayers");
            _selectionLayers = serializedObject.FindProperty("_selectionLayers");
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
            
            // Events
            _onSceneLoaded = serializedObject.FindProperty("_onSceneLoaded");
            _onSceneSaved = serializedObject.FindProperty("_onSceneSaved");
            _onObjectPlaced = serializedObject.FindProperty("_onObjectPlaced");
            _onObjectRemoved = serializedObject.FindProperty("_onObjectRemoved");
            _onObjectSelected = serializedObject.FindProperty("_onObjectSelected");
            _onSceneCleared = serializedObject.FindProperty("_onSceneCleared");
            _onPreviewStateChanged = serializedObject.FindProperty("_onPreviewStateChanged");
            _onModeChanged = serializedObject.FindProperty("_onModeChanged");
            
            // Drop Indicator Settings
            _enableDropIndicator = serializedObject.FindProperty("_enableDropIndicator");
            _dropIndicatorPrefab = serializedObject.FindProperty("_dropIndicatorPrefab");
            _validDropColor = serializedObject.FindProperty("_validDropColor");
            _invalidDropColor = serializedObject.FindProperty("_invalidDropColor");
            _dropIndicatorSize = serializedObject.FindProperty("_dropIndicatorSize");
            
            // Placement Validation Settings
            _checkCollisions = serializedObject.FindProperty("_checkCollisions");
            _collisionLayers = serializedObject.FindProperty("_collisionLayers");
            _groundLayers = serializedObject.FindProperty("_groundLayers");
            _ignoreStaticObjects = serializedObject.FindProperty("_ignoreStaticObjects");
            _requireSurfaceBelow = serializedObject.FindProperty("_requireSurfaceBelow");
            _enableCostSystem = serializedObject.FindProperty("_enableCostSystem");
            _placementCost = serializedObject.FindProperty("_placementCost");
            
            // Save/Load
            _defaultSavePath = serializedObject.FindProperty("_defaultSavePath");
            _currentSceneName = serializedObject.FindProperty("_currentSceneName");
            _defaultProjectSavePath = serializedObject.FindProperty("_defaultProjectSavePath");
            _autoLoadFirstProject = serializedObject.FindProperty("_autoLoadFirstProject");
            
            // Debug Settings
            _debugLogs = serializedObject.FindProperty("_debugLogs");
        }
        
        #endregion
        
        #region GUI Drawing Methods
        
        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("Scene Sandbox Builder", EditorStyles.largeLabel);
            GUILayout.Label("Ero Director - Mobile & AR Sandbox Edition", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(5);
            
            // Mode indicator
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Mode:", GUILayout.Width(100));
            
            var modeColor = _target.CurrentMode == SandboxMode.Build ? Color.green : Color.cyan;
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = modeColor;
            
            string modeText = _target.CurrentMode == SandboxMode.Build ? "BUILD MODE" : "PLAY MODE";
            GUILayout.Label(modeText, EditorStyles.helpBox, GUILayout.Height(25));
            
            GUI.backgroundColor = prevColor;
            EditorGUILayout.EndHorizontal();
            
            if (_target.CurrentMode == SandboxMode.Play)
            {
                EditorGUILayout.HelpBox("Play Mode: Input actions disabled, read-only preview", MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
            
            // Status indicators
            EditorGUILayout.BeginHorizontal();
            
            // Project status
            string projectStatus = _target.SceneSerializer?.CurrentProject != null ?
                $"Project: {_target.SceneSerializer.CurrentProject.projectName}" : "No Project Loaded";
            EditorGUILayout.LabelField("Status:", projectStatus);
            
            // Scene status with count
            if (_target.SceneSerializer?.CurrentProject != null)
            {
                int sceneCount = _target.GetSceneCount();
                string sceneStatus = _target.SceneSerializer?.CurrentScene != null ?
                    $"Scene: {_target.SceneSerializer?.CurrentScene.sceneName} ({sceneCount} total)" : $"No Scene ({sceneCount} total)";
                EditorGUILayout.LabelField(sceneStatus);
            }
            else
            {
                string sceneStatus = _target.SceneSerializer?.CurrentScene != null ?
                    $"Scene: {_target.SceneSerializer?.CurrentScene.sceneName}" : "No Scene";
                EditorGUILayout.LabelField(sceneStatus);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Object count
            if (Application.isPlaying)
            {
                int objectCount = _target.SceneSerializer?.CurrentScene?.placedObjects?.Count ?? 0;
                EditorGUILayout.LabelField($"Placed Objects: {objectCount}");
                
                if (_target.PreviewController?.IsInPreviewMode ?? false)
                {
                    EditorGUILayout.HelpBox("Preview Mode Active", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw setup button for initializing components in editor mode
        /// </summary>
        private void DrawSetupButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f); // Light blue
            
            if (GUILayout.Button("Setup Components in Editor", EditorStyles.toolbarButton, GUILayout.Height(30)))
            {
                SetupComponentsInEditor();
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.HelpBox("Click to initialize all components (GridManager, PlacementSystem, SandboxGizmoRenderer, etc.). Safe to run multiple times.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Initialize all components on the SceneSandboxBuilder in editor mode
        /// </summary>
        private void SetupComponentsInEditor()
        {
            if (_target == null)
            {
                EditorUtility.DisplayDialog("Error", "No SceneSandboxBuilder selected.", "OK");
                return;
            }
            
            try
            {
                // Call the private InitializeComponents method to set up all components
                var initMethod = typeof(SceneSandboxBuilder).GetMethod("InitializeComponents", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (initMethod != null)
                {
                    initMethod.Invoke(_target, null);
                    EditorUtility.DisplayDialog("Success", "Components initialized successfully!", "OK");
                    EditorUtility.SetDirty(_target);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Could not find InitializeComponents method on SceneSandboxBuilder.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to initialize components: {ex.Message}", "OK");
            }
        }
        
        private void DrawProjectAndSceneManagement()
        {
            _showProjectManagementFoldout = EditorGUILayout.Foldout(_showProjectManagementFoldout, 
                "Project & Scene Management", true, EditorStyles.foldoutHeader);
            
            if (!_showProjectManagementFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // ===== PROJECT MANAGEMENT SECTION =====
            EditorGUILayout.LabelField("PROJECT MANAGEMENT", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Auto-Load Settings
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_autoLoadFirstProject, new GUIContent("Auto Load First Project", 
                "Automatically load the first available project when entering Play mode"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(5);
            
            // New Project Section
            EditorGUILayout.LabelField("Create New Project", EditorStyles.miniBoldLabel);
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
            if (_target.SceneSerializer?.CurrentProject != null)
            {
                EditorGUILayout.LabelField("Current Project", EditorStyles.miniBoldLabel);
                var projectInfo = _target.GetCurrentProjectInfo();
                
                if (projectInfo != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Name: {projectInfo.projectName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Objects: {projectInfo.objectCount} | Scenes: {_target.GetSceneCount()}");
                    EditorGUILayout.LabelField($"Created: {projectInfo.created:yyyy-MM-dd HH:mm}");
                    EditorGUILayout.LabelField($"Last Modified: {projectInfo.lastModified:yyyy-MM-dd HH:mm}");
                    
                    // Display key settings
                    if (_target.SceneSerializer?.CurrentProject.settings != null)
                    {
                        var settings = _target.SceneSerializer?.CurrentProject.settings;
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
                            : System.IO.Path.Combine(_target.SceneSerializer?.CurrentProject.projectName, _projectSaveFileName + ".sbproj");
                        
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
                            _target.SceneSerializer?.CurrentProject.projectName,
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
                            _target.SceneSerializer?.CurrentProject.projectName,
                            _target.SceneSerializer?.CurrentProject.projectName + "_settings.json",
                            "json");
                        
                        if (!string.IsNullOrEmpty(exportPath))
                        {
                            string json = JsonUtility.ToJson(_target.SceneSerializer?.CurrentProject.settings, true);
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
            EditorGUILayout.LabelField("Available Projects", EditorStyles.miniBoldLabel);
            
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
            EditorGUILayout.LabelField("Save/Load Path Settings (Read-Only)", EditorStyles.miniBoldLabel);
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
            
            // ===== SCENE MANAGEMENT SECTION =====
            if (_target.SceneSerializer?.CurrentProject != null && Application.isPlaying)
            {
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("SCENE MANAGEMENT", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                // Create New Scene
                EditorGUILayout.LabelField("Create New Scene", EditorStyles.miniBoldLabel);
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
                if (_target.SceneSerializer?.CurrentScene != null)
                {
                    EditorGUILayout.LabelField("Current Scene Actions", EditorStyles.miniBoldLabel);
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
                        string newName = EditorUtility.SaveFilePanel("Rename Scene", "", _target.SceneSerializer?.CurrentScene.sceneName, "");
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
                EditorGUILayout.LabelField($"Scenes in Project ({_target.GetSceneCount()})", EditorStyles.miniBoldLabel);
                
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
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneManagement()
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
            if (_target.SceneSerializer?.CurrentProject != null)
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
                    if (_target.SceneSerializer?.CurrentProject.settings != null)
                    {
                        var settings = _target.SceneSerializer?.CurrentProject.settings;
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
                            : System.IO.Path.Combine(_target.SceneSerializer?.CurrentProject.projectName, _projectSaveFileName + ".sbproj");
                        
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
                            _target.SceneSerializer?.CurrentProject.projectName,
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
                            _target.SceneSerializer?.CurrentProject.projectName,
                            _target.SceneSerializer?.CurrentProject.projectName + "_settings.json",
                            "json");
                        
                        if (!string.IsNullOrEmpty(exportPath))
                        {
                            string json = JsonUtility.ToJson(_target.SceneSerializer?.CurrentProject.settings, true);
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
        
        private void DrawRuntimeInfoAndTransformItems()
        {
            if (!Application.isPlaying) return;
            
            _showRuntimeInfoFoldout = EditorGUILayout.Foldout(_showRuntimeInfoFoldout, 
                "Runtime Info & Transform Items", true, EditorStyles.foldoutHeader);
            
            if (!_showRuntimeInfoFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // ===== RUNTIME INFORMATION SECTION =====
            EditorGUILayout.LabelField("RUNTIME INFORMATION", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Current state
            EditorGUILayout.LabelField("Current State", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Selected Object: {(_target.SelectedObject != null ? _target.SelectedObject.name : "None")}");
            EditorGUILayout.LabelField("Gizmos Enabled: (managed by SandboxGizmoRenderer)");
            EditorGUILayout.LabelField("Scene Gizmos Enabled: (managed by SandboxGizmoRenderer)");
            EditorGUILayout.LabelField($"Drop Indicator Enabled: {_target.DropIndicatorEnabled}");
            EditorGUILayout.LabelField($"Preview Mode: {(_target.PreviewController?.IsInPreviewMode ?? false ? "Active" : "Inactive")}");
            
            EditorGUILayout.Space(5);
            
            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);
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
            
            if ((_target.PreviewController?.IsInPreviewMode ?? false))
            {
                if (GUILayout.Button("Stop Preview"))
                {
                    _target.StopPreview();
                }
            }
            else
            {
                GUI.enabled = _target.SceneSerializer?.CurrentScene != null && _target.SceneSerializer?.CurrentScene.placedObjects.Count > 0;
                if (GUILayout.Button("Start Preview"))
                {
                    _target.StartPreview();
                }
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // ===== TRANSFORM ITEMS MANAGER SECTION =====
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("TRANSFORM ITEMS MANAGER", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            // Get managed objects from SceneSandboxBuilder
            if (_target.SceneSerializer?.CurrentScene == null || _target.SceneSerializer?.CurrentScene.placedObjects == null || _target.SceneSerializer?.CurrentScene.placedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No managed objects in current scene.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Managed Objects: {_target.SceneSerializer?.CurrentScene.placedObjects.Count}", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(5);
                
                // Scroll view for item list
                _transformItemsScrollPos = EditorGUILayout.BeginScrollView(_transformItemsScrollPos, GUILayout.Height(300));
                
                foreach (var placedObj in _target.SceneSerializer?.CurrentScene.placedObjects)
                {
                    if (placedObj == null || string.IsNullOrEmpty(placedObj.id)) continue;
                    
                    // Find the GameObject - need to access SceneSandboxBuilder's internal dictionary
                    // Since GetPlacedObjectById might not be public, we'll iterate through scene objects
                    GameObject go = GameObject.Find(placedObj.id);
                    if (go == null)
                    {
                        // Try to find by matching TransformableItem.ObjectId
                        TransformableItem[] allItems = FindObjectsByType<TransformableItem>(FindObjectsSortMode.None);
                        foreach (var testItem in allItems)
                        {
                            if (testItem.ObjectId == placedObj.id)
                            {
                                go = testItem.gameObject;
                                break;
                            }
                        }
                    }
                    
                    if (go == null) continue;
                    
                    // Get TransformableItem component
                    TransformableItem item = go.GetComponent<TransformableItem>();
                    if (item == null) continue;
                    
                    // Draw compact item row
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Object name (truncated if too long)
                    string displayName = go.name.Length > 20 ? go.name.Substring(0, 17) + "..." : go.name;
                    EditorGUILayout.LabelField(displayName, GUILayout.Width(150));

                    // Select button
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        _target.SelectObject(go, false);
                    }
                    
                    if (GUILayout.Button("Select (Focus)", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        _target.SelectObject(go, false);
                        Selection.activeGameObject = go;
                        SceneView.FrameLastActiveSceneView();
                    }
                    
                    // Transform Mode buttons (compact)
                    EditorGUILayout.LabelField("|", GUILayout.Width(10));
                    
                    Color originalBg = GUI.backgroundColor;
                    
                    // None
                    GUI.backgroundColor = item.CurrentTransformModeType == TransformModeType.None ? Color.green : originalBg;
                    if (GUILayout.Button("N", EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        item.SetTransformModeType(TransformModeType.None);
                    }
                    
                    // Position
                    GUI.backgroundColor = item.CurrentTransformModeType == TransformModeType.Position ? Color.green : originalBg;
                    if (GUILayout.Button("P", EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        item.SetTransformModeType(TransformModeType.Position);
                    }
                    
                    // Rotation
                    GUI.backgroundColor = item.CurrentTransformModeType == TransformModeType.Rotation ? Color.green : originalBg;
                    if (GUILayout.Button("R", EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        item.SetTransformModeType(TransformModeType.Rotation);
                    }
                    
                    // Scale
                    GUI.backgroundColor = item.CurrentTransformModeType == TransformModeType.Scale ? Color.green : originalBg;
                    if (GUILayout.Button("S", EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        item.SetTransformModeType(TransformModeType.Scale);
                    }
                    
                    GUI.backgroundColor = originalBg;
                    
                    // Transform Axis buttons (only show if in Position, Rotation or Scale mode)
                    if (item.CurrentTransformModeType == TransformModeType.Position || 
                        item.CurrentTransformModeType == TransformModeType.Rotation || 
                        item.CurrentTransformModeType == TransformModeType.Scale)
                    {
                        EditorGUILayout.LabelField("|", GUILayout.Width(10));
                        
                        // All axes
                        GUI.backgroundColor = item.CurrentTransformAxis == TransformAxis.All ? Color.magenta : originalBg;
                        if (GUILayout.Button("All", EditorStyles.miniButton, GUILayout.Width(30)))
                        {
                            item.SetTransformAxis(TransformAxis.All);
                        }
                        
                        // X axis
                        GUI.backgroundColor = item.CurrentTransformAxis == TransformAxis.X ? Color.red : originalBg;
                        if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
                        {
                            item.SetTransformAxis(TransformAxis.X);
                        }
                        
                        // Y axis
                        GUI.backgroundColor = item.CurrentTransformAxis == TransformAxis.Y ? Color.green : originalBg;
                        if (GUILayout.Button("Y", EditorStyles.miniButton, GUILayout.Width(25)))
                        {
                            item.SetTransformAxis(TransformAxis.Y);
                        }
                        
                        // Z axis
                        GUI.backgroundColor = item.CurrentTransformAxis == TransformAxis.Z ? Color.blue : originalBg;
                        if (GUILayout.Button("Z", EditorStyles.miniButton, GUILayout.Width(25)))
                        {
                            item.SetTransformAxis(TransformAxis.Z);
                        }
                        
                        GUI.backgroundColor = originalBg;
                    }
                    
                    // Snap Mode buttons
                    EditorGUILayout.LabelField("|", GUILayout.Width(10));
                    
                    // Extend
                    GUI.backgroundColor = item.SnapMode == SnapMode.Extend ? Color.cyan : originalBg;
                    if (GUILayout.Button("Ext", EditorStyles.miniButton, GUILayout.Width(35)))
                    {
                        item.SetSnapMode(SnapMode.Extend);
                        EditorUtility.SetDirty(item);
                    }
                    
                    // Self
                    GUI.backgroundColor = item.SnapMode == SnapMode.Self ? Color.yellow : originalBg;
                    if (GUILayout.Button("Self", EditorStyles.miniButton, GUILayout.Width(35)))
                    {
                        item.SetSnapMode(SnapMode.Self);
                        EditorUtility.SetDirty(item);
                    }
                    
                    GUI.backgroundColor = originalBg;
                    
                    // Snap Grid toggle
                    EditorGUILayout.LabelField("|", GUILayout.Width(10));
                    bool newSnapEnabled = EditorGUILayout.Toggle(item.EnableSnapGrid, GUILayout.Width(20));
                    if (newSnapEnabled != item.EnableSnapGrid)
                    {
                        item.EnableSnapGrid = newSnapEnabled;
                        EditorUtility.SetDirty(item);
                    }
                    
                    // Grid size (if snap enabled)
                    if (item.EnableSnapGrid)
                    {
                        float newGridSize = EditorGUILayout.FloatField(item.SnapGridSize, GUILayout.Width(50));
                        if (!Mathf.Approximately(newGridSize, item.SnapGridSize))
                        {
                            item.SnapGridSize = Mathf.Max(0.01f, newGridSize);
                            EditorUtility.SetDirty(item);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawConfiguration()
        {
            _showConfigurationFoldout = EditorGUILayout.Foldout(_showConfigurationFoldout, 
                "Configuration", true, EditorStyles.foldoutHeader);
            
            if (!_showConfigurationFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(_objectLibrary);
            EditorGUILayout.PropertyField(_libraryController, new GUIContent("Library Controller", "UI Controller for the Scene Object Library"));
            EditorGUILayout.PropertyField(_sceneRoot);
            EditorGUILayout.PropertyField(_stageArea);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInputSettings()
        {
            _showInputSettingsFoldout = EditorGUILayout.Foldout(_showInputSettingsFoldout, 
                "Input Settings", true, EditorStyles.foldoutHeader);
            
            if (!_showInputSettingsFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox("Input actions are managed directly by SceneSandboxBuilder. All input handling, selection, and transform controls are integrated.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // Mode Toggle
            EditorGUILayout.LabelField("Mode Control", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_toggleModeActionRef, new GUIContent("Toggle Build/Play Mode", "Toggle between Build Mode (editing enabled) and Play Mode (read-only, performance optimized)"));
            
            EditorGUILayout.Space(5);
            
            // Transform Mode Hotkeys
            EditorGUILayout.LabelField("Transform Mode Hotkeys", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_exitAllModesActionRef, new GUIContent("Exit All Modes"));
            EditorGUILayout.PropertyField(_moveHotkeyActionRef, new GUIContent("Move Mode (Position)"));
            EditorGUILayout.PropertyField(_rotateHotkeyActionRef, new GUIContent("Rotate Mode"));
            EditorGUILayout.PropertyField(_scaleHotkeyActionRef, new GUIContent("Scale Mode"));
            EditorGUILayout.PropertyField(_transformModeIncreaseActionRef, new GUIContent("Increase Transform"));
            EditorGUILayout.PropertyField(_transformModeDecreaseActionRef, new GUIContent("Decrease Transform"));
            EditorGUILayout.PropertyField(_transformModeToggleAxisActionRef, new GUIContent("Toggle Axis", "Toggle between X, Y, Z, and All axes for Rotation/Scale modes"));
            
            EditorGUILayout.Space(5);
            
            // Pointer Input Actions
            EditorGUILayout.LabelField("Pointer Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pointerPositionActionRef, new GUIContent("Pointer Position"));
            EditorGUILayout.PropertyField(_leftClickActionRef, new GUIContent("Left Click"));
            EditorGUILayout.PropertyField(_rightClickActionRef, new GUIContent("Right Click"));
            EditorGUILayout.PropertyField(_mouseScrollActionRef, new GUIContent("Mouse Scroll"));
            
            EditorGUILayout.Space(5);
            
            // Placement Input Actions
            EditorGUILayout.LabelField("Placement Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cancelPlacementActionRef, new GUIContent("Cancel Placement"));
            EditorGUILayout.PropertyField(_switchPlacementItemActionRef, new GUIContent("Switch Placement Item", "Hotkey to cycle through objects in the library"));
            EditorGUILayout.PropertyField(_startPlacementActionRef, new GUIContent("Start Placement", "Hotkey to start placement with current object at pointer position"));
            
            EditorGUILayout.Space(5);
            
            // Raycast & Input Settings
            EditorGUILayout.LabelField("Raycast & Detection Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_selectionLayers, new GUIContent("Selection Layers", "Layers used for object selection raycasts (separate from placement layers)"));
            EditorGUILayout.PropertyField(_maxRaycastDistance, new GUIContent("Max Raycast Distance"));
            EditorGUILayout.PropertyField(_dragThreshold, new GUIContent("Drag Threshold (pixels)"));
            EditorGUILayout.PropertyField(_doubleClickTime, new GUIContent("Double Click Time (seconds)"));
            EditorGUILayout.PropertyField(_enableHotkeys, new GUIContent("Enable Hotkeys"));
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPlacementSettings()
        {
            _showPlacementFoldout = EditorGUILayout.Foldout(_showPlacementFoldout, 
                "Placement Settings & Validation", true, EditorStyles.foldoutHeader);
            
            if (!_showPlacementFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // ===== PLACEMENT SETTINGS SECTION =====
            EditorGUILayout.LabelField("PLACEMENT SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.PropertyField(_placementLayers, new GUIContent("Placement Layers"));
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.miniBoldLabel);
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
            EditorGUILayout.LabelField("Height Settings", EditorStyles.miniBoldLabel);
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
            EditorGUILayout.LabelField("Scene Bounds", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_sceneBounds, new GUIContent("Bounds Size", "The size of the scene bounds (X, Y, Z)"));
            EditorGUILayout.PropertyField(_sceneBoundsOffset, new GUIContent("Bounds Offset", "Offset from stage area center (X, Y, Z)"));
            EditorGUILayout.PropertyField(_sceneBoundsPivot, new GUIContent("Bounds Pivot", "Pivot point for scene bounds positioning"));
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Drop Indicator", EditorStyles.miniBoldLabel);
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
            
            // ===== PLACEMENT VALIDATION SECTION =====
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("PLACEMENT VALIDATION", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.HelpBox("Configure validation rules for object placement. These settings determine if a placement position is valid.", MessageType.Info);
            
            EditorGUILayout.Space(3);
            
            // Collision Checking
            EditorGUILayout.LabelField("Collision Detection", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_checkCollisions, new GUIContent("Check Collisions", "Enable collision checking during placement"));
            
            if (_checkCollisions.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_collisionLayers, new GUIContent("Collision Layers", "Layers to check for collisions"));
                EditorGUILayout.PropertyField(_groundLayers, new GUIContent("Ground Layers", "Layers to ignore in collision checks (e.g., floor, terrain)"));
                EditorGUILayout.PropertyField(_ignoreStaticObjects, new GUIContent("Ignore Static Objects", "Skip collision checks with static objects"));
                
                if (!_checkCollisions.boolValue)
                {
                    EditorGUILayout.HelpBox("Collision checking is disabled. Objects can be placed anywhere within scene bounds.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Surface Requirements
            EditorGUILayout.LabelField("Surface Requirements", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_requireSurfaceBelow, new GUIContent("Require Surface Below", "Objects must have a surface below them to be placed"));
            
            if (_requireSurfaceBelow.boolValue)
            {
                EditorGUILayout.HelpBox("Objects can only be placed where a surface exists below them.", MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
            
            // Cost System (Future Feature)
            EditorGUILayout.LabelField("Resource System (Future)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_enableCostSystem, new GUIContent("Enable Cost System", "Enable resource/cost system for placement"));
            
            if (_enableCostSystem.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_placementCost, new GUIContent("Placement Cost", "Base cost for placing objects"));
                EditorGUILayout.HelpBox("Cost system is a placeholder for future resource management features.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            // Debug Helper
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Debug Validation", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("Validation logs are enabled. Check the Console for detailed placement validation information.", MessageType.Info);
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
        
        private void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            
            if (_debugLogs != null)
            {
                EditorGUILayout.PropertyField(_debugLogs, new GUIContent("Enable Debug Logs", "Enable debug logging for placement, mode changes, and other operations"));
                
                if (_debugLogs.boolValue)
                {
                    EditorGUILayout.HelpBox("Debug logs are enabled. Check the Console for detailed operation logs.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Debug Logs property not found.", MessageType.Warning);
            }
            
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
            bool enableAllGizmos = _target.DropIndicatorEnabled; // Gizmo visibility now managed by SandboxGizmoRenderer
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
            if (_enableGizmos != null)
            {
                EditorGUILayout.PropertyField(_enableGizmos, new GUIContent("Enable Object Gizmos"));
                
                if (_enableGizmos.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (_showBoundsGizmo != null) EditorGUILayout.PropertyField(_showBoundsGizmo, new GUIContent("Show Bounds"));
                    if (_showAxesGizmo != null) EditorGUILayout.PropertyField(_showAxesGizmo, new GUIContent("Show Axes"));
                    if (_showHandlesGizmo != null) EditorGUILayout.PropertyField(_showHandlesGizmo, new GUIContent("Show Handles"));
                    if (_gizmoBoundsColor != null) EditorGUILayout.PropertyField(_gizmoBoundsColor, new GUIContent("Bounds Color"));
                    if (_gizmoAxisLength != null) EditorGUILayout.PropertyField(_gizmoAxisLength, new GUIContent("Axis Length"));
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Object Gizmo properties not found. Gizmos are managed by SandboxGizmoRenderer component.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Scene Gizmos
            EditorGUILayout.LabelField("Scene Gizmos", EditorStyles.boldLabel);
            if (_enableSceneGizmos != null)
            {
                EditorGUILayout.PropertyField(_enableSceneGizmos, new GUIContent("Enable Scene Gizmos"));
                
                if (_enableSceneGizmos.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (_showSceneGrid != null) EditorGUILayout.PropertyField(_showSceneGrid, new GUIContent("Show Grid"));
                    if (_showSceneBounds != null) EditorGUILayout.PropertyField(_showSceneBounds, new GUIContent("Show Scene Bounds"));
                    if (_showStageAreaGizmo != null) EditorGUILayout.PropertyField(_showStageAreaGizmo, new GUIContent("Show Stage Area"));
                    if (_showPlacementHeightGizmo != null) EditorGUILayout.PropertyField(_showPlacementHeightGizmo, new GUIContent("Show Placement Height"));
                    if (_sceneBoundsColor != null) EditorGUILayout.PropertyField(_sceneBoundsColor, new GUIContent("Bounds Color"));
                    if (_placementHeightColor != null) EditorGUILayout.PropertyField(_placementHeightColor, new GUIContent("Placement Height Color"));
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Scene Gizmo properties not found. Gizmos are managed by SandboxGizmoRenderer component.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEvents()
        {
            _showEventsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showEventsFoldout, "Events");
            
            if (!_showEventsFoldout) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox("UnityEvents that are invoked when specific actions occur. You can add listeners in the Inspector or via code.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Scene Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onSceneLoaded, new GUIContent("On Scene Loaded"));
            EditorGUILayout.PropertyField(_onSceneSaved, new GUIContent("On Scene Saved"));
            EditorGUILayout.PropertyField(_onSceneCleared, new GUIContent("On Scene Cleared"));
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Object Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onObjectPlaced, new GUIContent("On Object Placed"));
            EditorGUILayout.PropertyField(_onObjectRemoved, new GUIContent("On Object Removed"));
            EditorGUILayout.PropertyField(_onObjectSelected, new GUIContent("On Object Selected"));
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Mode Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onModeChanged, new GUIContent("On Mode Changed"));
            EditorGUILayout.PropertyField(_onPreviewStateChanged, new GUIContent("On Preview State Changed"));
            
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
                    GUI.enabled = Application.isPlaying && _target.SceneSerializer?.CurrentScene != null;
                    if (GUILayout.Button("Place", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        if (_target.DebugLogs) Debug.Log($"[Editor] Place button clicked for {objectData.displayName}, isPlaying={Application.isPlaying}");
                        
                        // Trigger drag&drop flow - ghost will appear at screen center
                        // User can then:
                        // - Move mouse to reposition ghost (follows cursor automatically)
                        // - Click to confirm placement (triggers OnEmptySpaceClicked event)
                        // - Press ESC to cancel (triggers OnCancelRequested event)
                        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                        _target.StartPlacement(objectData.id, screenCenter);
                        
                        if (_target.DebugLogs) Debug.Log($"[Editor] Started placement for {objectData.displayName}. Move mouse, click to place, ESC to cancel.");
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
            
            GUI.enabled = Application.isPlaying && _target.SceneSerializer?.CurrentProject != null;
            if (GUILayout.Button("Save Project", EditorStyles.toolbarButton))
            {
                _target.SaveProject();
                RefreshAvailableProjects();
                RefreshCurrentProjectScenes();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            // Scene operations (NEW)
            GUI.enabled = Application.isPlaying && _target.SceneSerializer?.CurrentProject != null;
            if (GUILayout.Button("New Scene", EditorStyles.toolbarButton))
            {
                _target.CreateNewSceneInProject();
                RefreshCurrentProjectScenes();
            }
            
            if (GUILayout.Button("Duplicate Scene", EditorStyles.toolbarButton))
            {
                if (_target.SceneSerializer?.CurrentScene != null)
                {
                    _target.DuplicateCurrentScene();
                    RefreshCurrentProjectScenes();
                }
            }
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            
            // Scene count indicator
            if (Application.isPlaying && _target.SceneSerializer?.CurrentProject != null)
            {
                int sceneCount = _target.GetSceneCount();
                string sceneLabel = sceneCount == 1 ? "scene" : "scenes";
                EditorGUILayout.LabelField($"{sceneCount} {sceneLabel}", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            
            // View toggles
            if (Application.isPlaying)
            {
                GUI.changed = false;
                EditorGUILayout.HelpBox("Gizmo visibility is now managed by SandboxGizmoRenderer component", MessageType.Info);
                if (GUI.changed)
                {
                    // Gizmo visibility controlled via SandboxGizmoRenderer
                }
                
                GUI.changed = false;
                EditorGUILayout.HelpBox("Scene Gizmo visibility is now managed by SandboxGizmoRenderer component", MessageType.Info);
                if (GUI.changed)
                {
                    // Scene gizmo visibility controlled via SandboxGizmoRenderer
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
            if (_target != null && _target.SceneSerializer?.CurrentProject != null)
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
