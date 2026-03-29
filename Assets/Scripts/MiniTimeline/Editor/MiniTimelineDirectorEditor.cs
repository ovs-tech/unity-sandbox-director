using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Editor
{
    /// <summary>
    /// Custom editor for MiniTimelineDirector component
    /// Provides a user-friendly inspector interface for managing timeline projects
    /// </summary>
    [CustomEditor(typeof(MiniTimelineDirector))]
    public class MiniTimelineDirectorEditor : UnityEditor.Editor
    {
        #region Private Fields
        
        private MiniTimelineDirector director;
        private SerializedProperty lengthProp;
        private SerializedProperty playbackSpeedProp;
        private SerializedProperty loopProp;
        private SerializedProperty playOnAwakeProp;
        private SerializedProperty debugModeProp;
        private SerializedProperty autoLoadFirstProjectProp;
        private SerializedProperty autoCreateEmptyProjectProp;
        private SerializedProperty defaultProjectNameProp;
        private SerializedProperty defaultProjectLengthProp;
        private SerializedProperty defaultFrameRateProp;
        private SerializedProperty bindableObjectManagerProp;
        
        // Project management
        private string newProjectName = "New Timeline Project";
        private float newProjectLength = 10f;
        private float newProjectFrameRate = 30f;
        private string lastLoadedPath = "";
        private string lastSavedPath = "";
        private string statusMessage = "";
        private MessageType statusMessageType = MessageType.Info;
        private double statusMessageExpiresAt = 0d;
        
        // Binding management
        private string newBindingKey = "";
        private UnityEngine.Object newBindingObject = null;
        
        // Foldouts
        private bool showTimelineSettings = true;
        private bool showProjectManagement = true;
        private bool showProjectList = true;
        private bool showBindingContext = false;
        private bool showPlaybackControls = false;
        private bool showTrackInfo = false;
        private bool showDebugInfo = false;
        private Dictionary<string, bool> clipDataFoldouts = new Dictionary<string, bool>();
        private double nextAutoRepaintTime = 0d;

        private const string SessionKeyPrefix = "MiniTimelineDirectorEditor";
        private const double AutoRepaintIntervalSeconds = 0.05d;
        private const double StatusMessageDurationSeconds = 2.5d;

        private enum CrudPanelMode
        {
            View,
            UpdateForm
        }

        private enum CrudEntityKind
        {
            None,
            Track,
            Clip
        }
        
        // CRUD form state (inline editing, replaces DrawTrackInfo content)
        private CrudPanelMode crudPanelMode = CrudPanelMode.View;
        private CrudEntityKind crudEntityKind = CrudEntityKind.None;
        private IMiniTrack editingTrack = null;
        private IMiniClip editingClip = null;
        private string editingClipType = "";
        private string editingTrackType = "";
        private string formClipId = "";
        private float formStartTime = 0f;
        private float formDuration = 1f;
        private readonly List<ClipDetailField> formClipDetailFields = new List<ClipDetailField>();
        private readonly Dictionary<string, object> formClipDetailValues = new Dictionary<string, object>();
        private readonly Dictionary<string, bool> formClipDetailGroupFoldouts = new Dictionary<string, bool>();
        private string formClipDetailInfoMessage = string.Empty;
        private bool isCreateMode = false;
        private string formTrackId = "";
        private string formTrackBindKey = "";
        private bool formTrackEnabled = true;
        private EvaluateMode formTrackEvaluateMode = EvaluateMode.Continuous;

        private sealed class ClipDetailField
        {
            public string Name;
            public string Label;
            public string Group;
            public FieldInfo Field;
            public int SortPriority;
            public int DeclarationOrder;
        }
        
        // Styles
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private bool stylesInitialized = false;
        
        #endregion
        
        #region Unity Editor Lifecycle
        
        private void OnEnable()
        {
            director = (MiniTimelineDirector)target;
            
            // Find serialized properties
            lengthProp = serializedObject.FindProperty("length");
            playbackSpeedProp = serializedObject.FindProperty("playbackSpeed");
            loopProp = serializedObject.FindProperty("loop");
            playOnAwakeProp = serializedObject.FindProperty("playOnAwake");
            debugModeProp = serializedObject.FindProperty("debugMode");
            autoLoadFirstProjectProp = serializedObject.FindProperty("autoLoadFirstProject");
            autoCreateEmptyProjectProp = serializedObject.FindProperty("autoCreateEmptyProject");
            defaultProjectNameProp = serializedObject.FindProperty("defaultProjectName");
            defaultProjectLengthProp = serializedObject.FindProperty("defaultProjectLength");
            defaultFrameRateProp = serializedObject.FindProperty("defaultFrameRate");
            bindableObjectManagerProp = serializedObject.FindProperty("bindableObjectManager");

            LoadSessionState();
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            SaveSessionState();
            EditorApplication.update -= OnEditorUpdate;
        }
        
        public override void OnInspectorGUI()
        {
            InitializeStyles();
            
            serializedObject.Update();
            
            EditorGUILayout.Space(5);
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawTimelineSettings();
            EditorGUILayout.Space(5);
            
            DrawBindingSetup();
            EditorGUILayout.Space(5);
            
            DrawProjectManagement();
            EditorGUILayout.Space(5);
            
            DrawProjectList();
            EditorGUILayout.Space(5);
            
            if (director.Project != null)
            {
                DrawBindingContext();
                EditorGUILayout.Space(5);
                
                if (Application.isPlaying)
                {
                    DrawPlaybackControls();
                    EditorGUILayout.Space(5);
                }
                
                DrawTrackInfo();
                EditorGUILayout.Space(5);
            }
            
            DrawDebugInfo();
            DrawStatusMessage();
            
            serializedObject.ApplyModifiedProperties();

            SaveSessionState();
        }

        private void OnEditorUpdate()
        {
            if (director == null || !Application.isPlaying || !director.IsPlaying)
            {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            if (now < nextAutoRepaintTime)
            {
                return;
            }

            nextAutoRepaintTime = now + AutoRepaintIntervalSeconds;
            Repaint();
        }
        
        #endregion

        #region Editor State Helpers

        private string GetSessionKey(string suffix)
        {
            var objectKey = "no-target";
            if (director != null)
            {
                objectKey = GlobalObjectId.GetGlobalObjectIdSlow(director).ToString();
            }

            return $"{SessionKeyPrefix}.{objectKey}.{suffix}";
        }

        private void SaveSessionState()
        {
            SessionState.SetBool(GetSessionKey("showTimelineSettings"), showTimelineSettings);
            SessionState.SetBool(GetSessionKey("showProjectManagement"), showProjectManagement);
            SessionState.SetBool(GetSessionKey("showProjectList"), showProjectList);
            SessionState.SetBool(GetSessionKey("showBindingContext"), showBindingContext);
            SessionState.SetBool(GetSessionKey("showPlaybackControls"), showPlaybackControls);
            SessionState.SetBool(GetSessionKey("showTrackInfo"), showTrackInfo);
            SessionState.SetBool(GetSessionKey("showDebugInfo"), showDebugInfo);
            SessionState.SetString(GetSessionKey("lastLoadedPath"), lastLoadedPath ?? string.Empty);
            SessionState.SetString(GetSessionKey("lastSavedPath"), lastSavedPath ?? string.Empty);
        }

        private void LoadSessionState()
        {
            showTimelineSettings = SessionState.GetBool(GetSessionKey("showTimelineSettings"), true);
            showProjectManagement = SessionState.GetBool(GetSessionKey("showProjectManagement"), true);
            showProjectList = SessionState.GetBool(GetSessionKey("showProjectList"), true);
            showBindingContext = SessionState.GetBool(GetSessionKey("showBindingContext"), false);
            showPlaybackControls = SessionState.GetBool(GetSessionKey("showPlaybackControls"), false);
            showTrackInfo = SessionState.GetBool(GetSessionKey("showTrackInfo"), false);
            showDebugInfo = SessionState.GetBool(GetSessionKey("showDebugInfo"), false);
            lastLoadedPath = SessionState.GetString(GetSessionKey("lastLoadedPath"), string.Empty);
            lastSavedPath = SessionState.GetString(GetSessionKey("lastSavedPath"), string.Empty);
        }

        private void DrawStatusMessage()
        {
            if (string.IsNullOrEmpty(statusMessage))
            {
                return;
            }

            if (EditorApplication.timeSinceStartup > statusMessageExpiresAt)
            {
                statusMessage = string.Empty;
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(statusMessage, statusMessageType);
        }

        private void SetStatusMessage(string message, MessageType type)
        {
            statusMessage = message;
            statusMessageType = type;
            statusMessageExpiresAt = EditorApplication.timeSinceStartup + StatusMessageDurationSeconds;
            Repaint();
        }

        private void RegisterUndo(string actionName, bool includeBindingContext = false)
        {
            if (director == null)
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(director, actionName);
            if (includeBindingContext && director.BindingContext != null)
            {
                Undo.RegisterCompleteObjectUndo(director.BindingContext, actionName);
            }
        }

        private void MarkEditorDirty(bool includeBindingContext = false)
        {
            if (director != null)
            {
                EditorUtility.SetDirty(director);
            }

            if (includeBindingContext && director != null && director.BindingContext != null)
            {
                EditorUtility.SetDirty(director.BindingContext);
            }
        }

        #endregion
        
        #region Style Initialization
        
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
            
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };
            
            stylesInitialized = true;
        }
        
        #endregion
        
        #region Drawing Methods
        
        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.LabelField("Mini Timeline Director", headerStyle);
            
            // Status indicators
            EditorGUILayout.BeginHorizontal();
            
            // Project status
            var projectStatus = director.Project != null ? 
                $"Project: {director.Project.name}" : "No Project Loaded";
            EditorGUILayout.LabelField(projectStatus, EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            // Playback status
            if (Application.isPlaying)
            {
                var stateColor = director.State switch
                {
                    PlaybackState.Playing => Color.green,
                    PlaybackState.Paused => Color.yellow,
                    _ => Color.gray
                };
                
                var oldColor = GUI.color;
                GUI.color = stateColor;
                EditorGUILayout.LabelField($"●{director.State}", EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = oldColor;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTimelineSettings()
        {
            showTimelineSettings = EditorGUILayout.Foldout(showTimelineSettings, "Timeline Settings", true);
            
            if (showTimelineSettings)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.PropertyField(lengthProp, new GUIContent("Timeline Length (s)", "Total duration of the timeline in seconds"));
                EditorGUILayout.PropertyField(playbackSpeedProp, new GUIContent("Playback Speed", "Speed multiplier for playback (0.1x - 2.0x)"));
                EditorGUILayout.PropertyField(loopProp, new GUIContent("Loop", "Whether the timeline should loop when it reaches the end"));
                EditorGUILayout.PropertyField(playOnAwakeProp, new GUIContent("Play on Awake", "Start playing automatically when the component awakes"));
                EditorGUILayout.PropertyField(debugModeProp, new GUIContent("Debug Mode", "Enable debug logging for timeline operations"));
                
                EditorGUILayout.Space(10);
                
                // Auto-load/create project settings
                EditorGUILayout.LabelField("Auto-Load/Create Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(autoLoadFirstProjectProp, new GUIContent("Auto Load First Project", "Automatically load the first available project on Start if no project is loaded"));
                EditorGUILayout.PropertyField(autoCreateEmptyProjectProp, new GUIContent("Auto Create Empty Project", "Automatically create an empty project on Start if no project is loaded (only if auto-load is disabled or no projects exist)"));
                
                // Show auto-create project settings only if enabled
                if (autoCreateEmptyProjectProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(defaultProjectNameProp, new GUIContent("Default Project Name", "Name for the auto-created project"));
                    EditorGUILayout.PropertyField(defaultProjectLengthProp, new GUIContent("Default Length (s)", "Length of the auto-created project in seconds"));
                    EditorGUILayout.PropertyField(defaultFrameRateProp, new GUIContent("Default Frame Rate", "Frame rate for the auto-created project"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawBindingSetup()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.LabelField("Binding Setup", EditorStyles.boldLabel);

            // BindableObjectManager field
            EditorGUILayout.PropertyField(bindableObjectManagerProp, new GUIContent("Bindable Objects", "BindableObjectManager component to use for object binding. Leave empty to auto-create."));

            // Helper text
            if (bindableObjectManagerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No BindableObjectManager assigned. One will be auto-created on the same GameObject when the scene starts.", MessageType.Info);

                // Button to manually create one
                if (GUILayout.Button("Create BindableObjectManager Component", buttonStyle))
                {
                    CreateBindingContextComponent();
                }
            }
            else
            {
                var context = bindableObjectManagerProp.objectReferenceValue as BindableObjectManager;
                if (context != null)
                {
                    EditorGUILayout.HelpBox($"Using BindableObjectManager from '{context.gameObject.name}'", MessageType.None);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawProjectManagement()
        {
            showProjectManagement = EditorGUILayout.Foldout(showProjectManagement, "Project Management", true);
            
            if (showProjectManagement)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                // Create new project section
                EditorGUILayout.LabelField("Create New Project", EditorStyles.boldLabel);
                newProjectName = EditorGUILayout.TextField("Project Name", newProjectName);
                newProjectLength = EditorGUILayout.FloatField("Length (s)", newProjectLength);
                newProjectFrameRate = EditorGUILayout.FloatField("Frame Rate", newProjectFrameRate);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Empty Project", buttonStyle))
                {
                    CreateNewProject();
                }
                if (GUILayout.Button("Create Sample Project", buttonStyle))
                {
                    CreateSampleProject();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                // Load/Save section
                EditorGUILayout.LabelField("Load/Save Project", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Project...", buttonStyle))
                {
                    SaveProject();
                }
                
                if (GUILayout.Button("Load Project...", buttonStyle))
                {
                    LoadProject();
                }
                EditorGUILayout.EndHorizontal();
                
                // Quick save/load (last used path)
                if (!string.IsNullOrEmpty(lastSavedPath) || !string.IsNullOrEmpty(lastLoadedPath))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    if (!string.IsNullOrEmpty(lastSavedPath) && GUILayout.Button($"Quick Save", EditorStyles.miniButton))
                    {
                        QuickSave();
                    }
                    if (!string.IsNullOrEmpty(lastLoadedPath) && GUILayout.Button($"Reload", EditorStyles.miniButton))
                    {
                        QuickLoad();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Close project (only shown when a project is loaded)
                if (director.Project != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Current Project", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Loaded: {director.Project.name}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("Close Project", EditorStyles.miniButton, GUILayout.Width(100)))
                    {
                        CloseProject();
                    }
                    GUI.backgroundColor = oldBg;
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawProjectList()
        {
            showProjectList = EditorGUILayout.Foldout(showProjectList, "Available Projects", true);
            
            if (showProjectList)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                var projectsFolder = director.GetProjectsFolder();
                var hasProjectsFolder = !string.IsNullOrEmpty(projectsFolder);
                
                // Project folder info and actions
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Projects Folder:", EditorStyles.boldLabel);
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("🔄 Refresh", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    Repaint();
                }
                
                EditorGUI.BeginDisabledGroup(!hasProjectsFolder);
                if (GUILayout.Button("📁 Open Folder", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    EditorUtility.RevealInFinder(projectsFolder);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField(hasProjectsFolder ? projectsFolder : "(Persistence root unavailable in EditMode)", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(5);
                
                // Get available projects
                var availableProjects = director.GetAvailableProjects();
                
                if (availableProjects.Length > 0)
                {
                    // Sort by modified date (most recent first)
                    var sortedProjects = availableProjects
                        .Select(name => new { 
                            Name = name, 
                            Path = director.GetProjectFilePath(name),
                            ModifiedTime = File.Exists(director.GetProjectFilePath(name)) 
                                ? new FileInfo(director.GetProjectFilePath(name)).LastWriteTime 
                                : DateTime.MinValue
                        })
                        .OrderByDescending(p => p.ModifiedTime)
                        .Select(p => p.Name)
                        .ToArray();
                    
                    EditorGUILayout.LabelField($"Found {sortedProjects.Length} project(s):", EditorStyles.boldLabel);
                    EditorGUILayout.Space(3);
                    
                    // Render each project as a card
                    foreach (var projectName in sortedProjects)
                    {
                        DrawProjectCard(projectName);
                    }
                }
                else
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox("No projects found. Create a new project to get started!", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawProjectCard(string projectName)
        {
            var isCurrentProject = director.Project != null && director.Project.name == projectName;
            
            // Different style for current project
            var cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };
            
            if (isCurrentProject)
            {
                var savedBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f, 0.3f);
                EditorGUILayout.BeginVertical(cardStyle);
                GUI.backgroundColor = savedBgColor;
            }
            else
            {
                EditorGUILayout.BeginVertical(cardStyle);
            }
            
            // Project name and current indicator
            EditorGUILayout.BeginHorizontal();
            
            // Icon and name
            if (isCurrentProject)
            {
                var savedColor = GUI.color;
                GUI.color = new Color(0.2f, 0.8f, 0.2f);
                EditorGUILayout.LabelField("▶", EditorStyles.boldLabel, GUILayout.Width(20));
                GUI.color = savedColor;
                
                EditorGUILayout.LabelField(projectName, EditorStyles.boldLabel);
                
                // Current badge
                var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.2f, 0.7f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField("[LOADED]", badgeStyle, GUILayout.Width(60));
            }
            else
            {
                EditorGUILayout.LabelField("📄", GUILayout.Width(20));
                EditorGUILayout.LabelField(projectName, EditorStyles.boldLabel);
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            // File info
            string filePath = director.GetProjectFilePath(projectName);
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📅 {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"💾 {fileInfo.Length / 1024f:F1} KB", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            // Show track count for current project
            if (isCurrentProject && director.Project != null)
            {
                EditorGUILayout.LabelField($"🎬 {director.Project.tracks.Count} tracks, {director.Project.length:F1}s @ {director.Project.frameRate:F0}fps", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space(5);
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            // Load button
            if (!isCurrentProject)
            {
                if (GUILayout.Button("📂 Load", GUILayout.Height(26)))
                {
                    LoadProjectByName(projectName);
                }
            }
            else
            {
                // Save button (only if this is the current project)
                if (GUILayout.Button("💾 Save", GUILayout.Height(26)))
                {
                    if (director.SaveProject())
                    {
                        Debug.Log($"[MiniTimelineDirectorEditor] Saved project: {projectName}");
                        SetStatusMessage($"Saved project '{projectName}'.", MessageType.Info);
                        Repaint(); // Refresh to update modified date
                    }
                }
                
                // Save As button
                if (GUILayout.Button("Save As...", GUILayout.Height(26), GUILayout.Width(80)))
                {
                    var newName = EditorUtility.SaveFilePanel(
                        "Save Project As",
                        director.GetProjectsFolder(),
                        projectName,
                        "json"
                    );
                    
                    if (!string.IsNullOrEmpty(newName))
                    {
                        string newProjectName = Path.GetFileNameWithoutExtension(newName);
                        if (director.SaveProject(newProjectName))
                        {
                            Debug.Log($"[MiniTimelineDirectorEditor] Saved project as: {newProjectName}");
                            SetStatusMessage($"Saved project as '{newProjectName}'.", MessageType.Info);
                            Repaint();
                        }
                    }
                }
            }
            
            GUILayout.FlexibleSpace();
            
            // Delete button
            var savedBgColor2 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("🗑️ Delete", GUILayout.Width(80), GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Delete Project", 
                    $"Are you sure you want to delete '{projectName}'?\n\nThis cannot be undone.", 
                    "Delete", "Cancel"))
                {
                    if (isCurrentProject)
                    {
                        director.CloseProject();
                    }
                    
                    if (director.DeleteProject(projectName))
                    {
                        Debug.Log($"[MiniTimelineDirectorEditor] Deleted project: {projectName}");
                        Repaint();
                    }
                }
            }
            GUI.backgroundColor = savedBgColor2;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(3);
        }
        
        private void DrawBindingContext()
        {
            showBindingContext = EditorGUILayout.Foldout(showBindingContext, "Binding Context", true);
            
            if (showBindingContext)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                if (director.BindingContext != null)
                {
                    var bindingKeys = director.BindingContext.GetKeys().ToList();
                    
                    if (bindingKeys.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No bindings configured. Use MiniTimelineDemo component or setup bindings manually.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Active Bindings ({bindingKeys.Count}):", EditorStyles.boldLabel);
                        
                        var keysToRemove = new List<string>();
                        var bindingsToAdd = new List<(string oldKey, string newKey, UnityEngine.Object obj)>();
                        bool bindingsChanged = false;
                        
                        foreach (var key in bindingKeys)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // Editable binding key
                            EditorGUILayout.LabelField("Key:", GUILayout.Width(30));
                            var newKey = EditorGUILayout.TextField(key, GUILayout.Width(120));
                            
                            // Object field
                            UnityEngine.Object currentObj = null;
                            director.BindingContext.TryResolve(key, out currentObj);
                            
                            var newObj = EditorGUILayout.ObjectField(currentObj, typeof(UnityEngine.Object), true);
                            
                            // Remove button
                            if (GUILayout.Button("×", GUILayout.Width(20)))
                            {
                                keysToRemove.Add(key);
                            }
                            
                            EditorGUILayout.EndHorizontal();
                            
                            // Handle key changes
                            if (newKey != key && !string.IsNullOrEmpty(newKey))
                            {
                                bindingsToAdd.Add((key, newKey, newObj ?? currentObj));
                                keysToRemove.Add(key);
                            }
                            // Handle object changes
                            else if (newObj != currentObj)
                            {
                                RegisterUndo("Update Timeline Binding", includeBindingContext: true);
                                director.BindingContext.Bind(key, newObj);
                                bindingsChanged = true;
                            }
                        }
                        
                        // Apply changes
                        foreach (var keyToRemove in keysToRemove)
                        {
                            RegisterUndo("Remove Timeline Binding", includeBindingContext: true);
                            director.BindingContext.Unbind(keyToRemove);
                            bindingsChanged = true;
                        }
                        
                        foreach (var (oldKey, newKey, obj) in bindingsToAdd)
                        {
                            if (!bindingKeys.Contains(newKey)) // Avoid duplicate keys
                            {
                                RegisterUndo("Update Timeline Binding Key", includeBindingContext: true);
                                director.BindingContext.Bind(newKey, obj);
                                bindingsChanged = true;
                            }
                        }
                        
                        // Rebind all tracks if bindings were modified
                        if (bindingsChanged)
                        {
                            MarkEditorDirty(includeBindingContext: true);
                            RebindAllTracks();
                        }
                    }
                    
                    // Auto-binding section
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Auto Binding:", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Auto-Bind Scene Objects", buttonStyle))
                    {
                        AutoBindSceneObjects();
                    }
                    if (GUILayout.Button("Clear All Bindings", buttonStyle))
                    {
                        if (EditorUtility.DisplayDialog("Clear Bindings", "Are you sure you want to clear all bindings?", "Yes", "Cancel"))
                        {
                            ClearAllBindings();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Add new binding section
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Add New Binding:", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Key:", GUILayout.Width(30));
                    newBindingKey = EditorGUILayout.TextField(newBindingKey, GUILayout.Width(120));
                    newBindingObject = EditorGUILayout.ObjectField(newBindingObject, typeof(UnityEngine.Object), true);
                    
                    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newBindingKey) || newBindingObject == null);
                    if (GUILayout.Button("Add", GUILayout.Width(50)))
                    {
                        if (!bindingKeys.Contains(newBindingKey))
                        {
                            RegisterUndo("Add Timeline Binding", includeBindingContext: true);
                            director.BindingContext.Bind(newBindingKey, newBindingObject);
                            newBindingKey = "";
                            newBindingObject = null;
                            
                            // Rebind all tracks to update their IsBound status
                            MarkEditorDirty(includeBindingContext: true);
                            RebindAllTracks();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Duplicate Key", $"Binding key '{newBindingKey}' already exists.", "OK");
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Binding context is null.", MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPlaybackControls()
        {
            showPlaybackControls = EditorGUILayout.Foldout(showPlaybackControls, "Playback Controls", true);
            
            if (showPlaybackControls)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                // Time display
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Time:", GUILayout.Width(90));
                var timeText = $"{director.Time:F2}s / {director.Length:F2}s";
                EditorGUILayout.LabelField(timeText);
                EditorGUILayout.EndHorizontal();
                
                // Time scrubber
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scrub:", GUILayout.Width(45));
                var newTime = EditorGUILayout.Slider(director.Time, 0f, director.Length);
                if (!Mathf.Approximately(newTime, director.Time))
                {
                    director.Seek(newTime);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Control buttons
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button(director.IsPlaying ? "Pause" : "Play", buttonStyle))
                {
                    if (director.IsPlaying)
                        director.Pause();
                    else
                        director.Play();
                }
                
                if (GUILayout.Button("Stop", buttonStyle))
                {
                    director.Stop();
                }
                
                if (GUILayout.Button("Restart", buttonStyle))
                {
                    director.Stop();
                    director.Play();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawTrackInfo()
        {
            // Show dynamic CRUD form when Update mode is active.
            if (crudPanelMode == CrudPanelMode.UpdateForm && crudEntityKind != CrudEntityKind.None)
            {
                DrawActiveDetailForm();
                return;
            }
            
            showTrackInfo = EditorGUILayout.Foldout(showTrackInfo, $"Track Information ({director.Tracks.Count} tracks)", true);
            
            if (showTrackInfo)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                // Add Track Button
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Track Management", EditorStyles.boldLabel);
                var viewMode = GUILayout.Toggle(crudPanelMode == CrudPanelMode.View, "View", EditorStyles.miniButtonLeft, GUILayout.Width(65));
                var updateMode = GUILayout.Toggle(crudPanelMode == CrudPanelMode.UpdateForm, "Update Form", EditorStyles.miniButtonRight, GUILayout.Width(95));
                if (viewMode && crudPanelMode != CrudPanelMode.View)
                {
                    EnterViewMode();
                }
                else if (updateMode && crudPanelMode != CrudPanelMode.UpdateForm)
                {
                    if (crudEntityKind == CrudEntityKind.None)
                    {
                        EditorUtility.DisplayDialog("Select Item", "Open a track or clip in edit/create mode first.", "OK");
                    }
                    else
                    {
                        crudPanelMode = CrudPanelMode.UpdateForm;
                    }
                }
                if (GUILayout.Button("+ Add Track", buttonStyle, GUILayout.Width(100)))
                {
                    ShowAddTrackMenu();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                if (director.Tracks.Count == 0)
                {
                    EditorGUILayout.HelpBox("No tracks in current project. Click 'Add Track' to create one.", MessageType.Info);
                }
                else
                {
                    var tracksToRemove = new List<IMiniTrack>();
                    
                    foreach (var track in director.Tracks.ToList())
                    {
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        
                        // Track header with actions
                        EditorGUILayout.BeginHorizontal();
                        
                        // Track name and status
                        var enabledColor = track.Enabled ? Color.green : Color.red;
                        var oldColor = GUI.color;
                        GUI.color = enabledColor;
                        EditorGUILayout.LabelField(track.Enabled ? "●" : "○", GUILayout.Width(15));
                        GUI.color = oldColor;
                        
                        // Binding status indicator
                        bool isBound = track.IsBound;
                        var bindColor = isBound ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.5f, 0.2f);
                        GUI.color = bindColor;
                        var bindIcon = isBound ? "🔗" : "⚠";
                        EditorGUILayout.LabelField(bindIcon, GUILayout.Width(20));
                        GUI.color = oldColor;
                        
                        // Ready status indicator
                        bool isReady = track.IsReady;
                        var readyColor = isReady ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.5f, 0.2f);
                        GUI.color = readyColor;
                        var readyIcon = isReady ? "✓" : "✗";
                        EditorGUILayout.LabelField(readyIcon, GUILayout.Width(20));
                        GUI.color = oldColor;
                        
                        EditorGUILayout.LabelField($"{track.Id} ({track.GetType().Name})", EditorStyles.boldLabel);
                        
                        GUILayout.FlexibleSpace();
                        
                        // Status legend (tooltip)
                        var statusTooltip = $"● Enabled: {track.Enabled}\n🔗 Bound: {isBound}\n✓ Ready: {isReady}";
                        EditorGUILayout.LabelField(new GUIContent("ℹ", statusTooltip), GUILayout.Width(20));

                        if (GUILayout.Button("✎", GUILayout.Width(24), GUILayout.Height(20)))
                        {
                            EditTrack(track);
                        }
                        
                        // Delete button
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                        if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Track", 
                                $"Are you sure you want to delete track '{track.Id}'?", 
                                "Delete", "Cancel"))
                            {
                                tracksToRemove.Add(track);
                            }
                        }
                        GUI.backgroundColor = Color.white;
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // Track details
                        EditorGUILayout.BeginHorizontal();
                        
                        // Binding key selector
                        EditorGUILayout.LabelField("Bind Key:", GUILayout.Width(60));
                        
                        var availableKeys = new List<string> { "None" };
                        if (director.BindingContext != null)
                        {
                            availableKeys.AddRange(director.BindingContext.GetKeys());
                        }
                        
                        var currentBindKey = track.BindKey ?? "None";
                        var currentIndex = availableKeys.IndexOf(currentBindKey);
                        if (currentIndex == -1)
                        {
                            // If current bind key is not in available keys, add it to show
                            availableKeys.Add(currentBindKey);
                            currentIndex = availableKeys.Count - 1;
                        }
                        
                        var newIndex = EditorGUILayout.Popup(currentIndex, availableKeys.ToArray(), GUILayout.Width(120));
                        if (newIndex != currentIndex && newIndex >= 0 && newIndex < availableKeys.Count)
                        {
                            var newBindKey = availableKeys[newIndex];
                            if (newBindKey == "None")
                            {
                                newBindKey = null;
                            }
                            
                            // Update track bind key
                            UpdateTrackBindKey(track, newBindKey);
                        }
                        
                        EditorGUILayout.LabelField("Order:", track.Order.ToString(), EditorStyles.miniLabel, GUILayout.Width(60));
                        EditorGUILayout.EndHorizontal();
                        
                        // Status information
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
                        
                        // Enabled status
                        var enabledStatusColor = GUI.color;
                        GUI.color = track.Enabled ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
                        EditorGUILayout.LabelField(track.Enabled ? "● Enabled" : "○ Disabled", EditorStyles.miniLabel, GUILayout.Width(80));
                        
                        // Bound status
                        GUI.color = track.IsBound ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.5f, 0.2f);
                        EditorGUILayout.LabelField(track.IsBound ? "🔗 Bound" : "⚠ Not Bound", EditorStyles.miniLabel, GUILayout.Width(90));
                        
                        // Ready status
                        GUI.color = track.IsReady ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.5f, 0.2f);
                        EditorGUILayout.LabelField(track.IsReady ? "✓ Ready" : "✗ Not Ready", EditorStyles.miniLabel, GUILayout.Width(80));
                        
                        GUI.color = enabledStatusColor;
                        EditorGUILayout.EndHorizontal();
                        
                        // Binding status details
                        EditorGUILayout.BeginHorizontal();
                        UnityEngine.Object boundObject = null;
                        if (director.BindingContext != null && !string.IsNullOrEmpty(track.BindKey))
                        {
                            director.BindingContext.TryResolve(track.BindKey, out boundObject);
                        }
                        
                        if (track.IsBound && boundObject != null)
                        {
                            var statusColor = GUI.color;
                            GUI.color = new Color(0.3f, 0.8f, 0.3f);
                            EditorGUILayout.LabelField($"✓ Bound to: {boundObject.name}", EditorStyles.miniLabel);
                            GUI.color = statusColor;
                        }
                        else if (!string.IsNullOrEmpty(track.BindKey))
                        {
                            var statusColor = GUI.color;
                            GUI.color = new Color(0.8f, 0.5f, 0.2f);
                            EditorGUILayout.LabelField($"⚠ Not bound (key '{track.BindKey}' not found)", EditorStyles.miniLabel);
                            GUI.color = statusColor;
                        }
                        else
                        {
                            EditorGUILayout.LabelField("○ No binding key assigned", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // Multi-target bindings
                        DrawMultiTargetBindings(track);
                        
                        // EvaluateMode display
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Evaluate Mode:", GUILayout.Width(100));
                        var modeColor = GUI.color;
                        GUI.color = new Color(0.6f, 0.8f, 1f);
                        var modeText = track.EvaluateMode switch
                        {
                            EvaluateMode.OnEnter => "🚀 OnEnter",
                            EvaluateMode.OnExit => "🏁 OnExit",
                            EvaluateMode.OnEnterAndExit => "🔄 OnEnter+Exit",
                            EvaluateMode.Continuous => "▶️ Continuous",
                            _ => track.EvaluateMode.ToString()
                        };
                        EditorGUILayout.LabelField(modeText, EditorStyles.miniLabel);
                        GUI.color = modeColor;
                        EditorGUILayout.EndHorizontal();
                        
                        // Enable/Disable toggle
                        EditorGUILayout.BeginHorizontal();
                        var newEnabled = EditorGUILayout.Toggle("Enabled", track.Enabled);
                        if (newEnabled != track.Enabled)
                        {
                            UpdateTrackEnabled(track, newEnabled);
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // Clip management section
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginHorizontal();
                        
                        var clipsList = track.GetClips().ToList();
                        EditorGUILayout.LabelField($"Clips ({clipsList.Count})", EditorStyles.boldLabel);
                        
                        if (GUILayout.Button("+ Add Clip", EditorStyles.miniButton, GUILayout.Width(80)))
                        {
                            ShowAddClipMenu(track);
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // Display clips
                        if (clipsList.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            
                            var clipsToRemove = new List<IMiniClip>();
                            
                            // Calculate timeline visualization scale
                            var maxTime = Mathf.Max(director.Length, clipsList.Max(c => c.Start + c.Duration));
                            
                            foreach (var clip in clipsList.OrderBy(c => c.Start))
                                {
                                    var clipBoxStyle = new GUIStyle(EditorStyles.helpBox)
                                    {
                                        padding = new RectOffset(8, 8, 6, 6)
                                    };
                                    
                                    EditorGUILayout.BeginVertical(clipBoxStyle);
                                    
                                    // Clip header with type indicator, edit button, and delete button
                                    EditorGUILayout.BeginHorizontal();
                                    
                                    // Clip type icon
                                    var clipIcon = GetClipIcon(clip);
                                    var clipColor = GetClipColor(clip);
                                    var savedColor = GUI.color;
                                    GUI.color = clipColor;
                                    EditorGUILayout.LabelField(clipIcon, GUILayout.Width(20));
                                    GUI.color = savedColor;
                                    
                                    // Clip ID
                                    EditorGUILayout.LabelField(clip.Id, EditorStyles.boldLabel);
                                    
                                    GUILayout.FlexibleSpace();
                                    
                                    // Edit button
                                    if (GUILayout.Button("✎ Edit", EditorStyles.miniButton, GUILayout.Width(60)))
                                    {
                                        EditClip(track, clip);
                                    }
                                    
                                    // Delete button
                                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                                    if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
                                    {
                                        if (EditorUtility.DisplayDialog("Delete Clip", 
                                            $"Delete clip '{clip.Id}'?", "Delete", "Cancel"))
                                        {
                                            clipsToRemove.Add(clip);
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                    EditorGUILayout.EndHorizontal();
                                    
                                    // Timeline visualization bar
                                    DrawClipTimelineBar(clip, maxTime);

                                    EditorGUILayout.Space(3);

                                    // Clip type info
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField($"Type: {clip.GetType().Name}", EditorStyles.miniLabel, GUILayout.Width(200));
                                    GUILayout.FlexibleSpace();
                                    EditorGUILayout.EndHorizontal();
                                    
                                    EditorGUILayout.Space(2);
                                    
                                    // Clip timing properties
                                    EditorGUILayout.BeginHorizontal();
                                    
                                    EditorGUILayout.LabelField("Start:", GUILayout.Width(40));
                                    var newStart = EditorGUILayout.FloatField(clip.Start, GUILayout.Width(60));
                                    if (!Mathf.Approximately(newStart, clip.Start))
                                    {
                                        UpdateClipStart(track, clip, Mathf.Max(0f, newStart));
                                    }
                                    
                                    EditorGUILayout.LabelField("Duration:", GUILayout.Width(60));
                                    var newDuration = EditorGUILayout.FloatField(clip.Duration, GUILayout.Width(60));
                                    if (!Mathf.Approximately(newDuration, clip.Duration))
                                    {
                                        UpdateClipDuration(track, clip, Mathf.Max(0.01f, newDuration));
                                    }
                                    
                                    // End time display (read-only)
                                    var endTime = clip.Start + clip.Duration;
                                    EditorGUILayout.LabelField($"End: {endTime:F2}s", EditorStyles.miniLabel, GUILayout.Width(80));
                                    
                                    EditorGUILayout.EndHorizontal();
                                    
                                    // Additional clip info
                                    var clipInfo = GetClipTypeInfo(clip);
                                    if (!string.IsNullOrEmpty(clipInfo))
                                    {
                                        GUI.color = new Color(0.7f, 0.7f, 0.7f);
                                        EditorGUILayout.LabelField($"ℹ {clipInfo}", EditorStyles.miniLabel);
                                        GUI.color = savedColor;
                                    }
                                    
                                    // Serialized Data Foldout
                                    EditorGUILayout.Space(3);
                                    string clipFoldoutKey = $"{track.Id}_{clip.Id}";
                                    if (!clipDataFoldouts.ContainsKey(clipFoldoutKey))
                                    {
                                        clipDataFoldouts[clipFoldoutKey] = false;
                                    }
                                    
                                    clipDataFoldouts[clipFoldoutKey] = EditorGUILayout.Foldout(
                                        clipDataFoldouts[clipFoldoutKey], 
                                        "Serialized Data", 
                                        true,
                                        EditorStyles.foldout
                                    );
                                    
                                    if (clipDataFoldouts[clipFoldoutKey])
                                    {
                                        EditorGUI.indentLevel++;
                                        DrawClipSerializedData(track, clip);
                                        EditorGUI.indentLevel--;
                                    }
                                    
                                    EditorGUILayout.EndVertical();
                                    
                                    EditorGUILayout.Space(2);
                                }
                                
                                // Remove clips marked for deletion
                                foreach (var clip in clipsToRemove)
                                {
                                    RemoveClip(track, clip);
                                }
                                
                                EditorGUI.indentLevel--;
                            }
                        
                        EditorGUILayout.EndVertical();
                    }
                    
                    // Remove tracks that were marked for deletion
                    foreach (var track in tracksToRemove)
                    {
                        RemoveTrack(track);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawActiveDetailForm()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(isCreateMode ? "Create Form" : "Update Form", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("← Back to View", GUILayout.Width(120)))
            {
                EnterViewMode();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            if (crudEntityKind == CrudEntityKind.Track)
            {
                DrawTrackDetailForm();
            }
            else
            {
                DrawClipDetailForm();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTrackDetailForm()
        {
            EditorGUILayout.LabelField(isCreateMode ? "Create Track" : "Edit Track", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.8f, 0.85f, 1f);
            EditorGUILayout.HelpBox($"Type: {GetFriendlyTrackName(editingTrackType)}", MessageType.Info);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Track ID", EditorStyles.boldLabel);
            formTrackId = EditorGUILayout.TextField(formTrackId);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Bind Key", EditorStyles.boldLabel);
            var availableKeys = new List<string> { "None" };
            if (director.BindingContext != null)
            {
                availableKeys.AddRange(director.BindingContext.GetKeys());
            }

            var currentBind = string.IsNullOrEmpty(formTrackBindKey) ? "None" : formTrackBindKey;
            var bindIndex = availableKeys.IndexOf(currentBind);
            if (bindIndex < 0)
            {
                availableKeys.Add(currentBind);
                bindIndex = availableKeys.Count - 1;
            }

            var newBindIndex = EditorGUILayout.Popup(bindIndex, availableKeys.ToArray());
            if (newBindIndex >= 0 && newBindIndex < availableKeys.Count)
            {
                formTrackBindKey = availableKeys[newBindIndex] == "None" ? null : availableKeys[newBindIndex];
            }

            EditorGUILayout.Space(4);
            formTrackEnabled = EditorGUILayout.Toggle("Enabled", formTrackEnabled);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Evaluate Mode", EditorStyles.boldLabel);
            formTrackEvaluateMode = (EvaluateMode)EditorGUILayout.EnumPopup(formTrackEvaluateMode);

            EditorGUILayout.Space(12);
            var hasId = !string.IsNullOrWhiteSpace(formTrackId);
            if (!hasId)
            {
                EditorGUILayout.HelpBox("Track ID is required.", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = hasId;
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            if (GUILayout.Button(isCreateMode ? "✓ Create Track" : "✓ Update Track", GUILayout.Height(30)))
            {
                if (isCreateMode)
                {
                    CreateTrackFromForm();
                }
                else
                {
                    UpdateTrackFromForm();
                }
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕ Cancel", GUILayout.Height(30)))
            {
                EnterViewMode();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawClipDetailForm()
        {
            EditorGUILayout.LabelField(isCreateMode ? "Create Clip" : "Edit Clip", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Display clip type
            GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
            EditorGUILayout.HelpBox($"Type: {editingClipType}", MessageType.Info);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // Clip ID field
            EditorGUILayout.LabelField("Clip ID", EditorStyles.boldLabel);
            formClipId = EditorGUILayout.TextField(formClipId);
            EditorGUILayout.Space(5);

            // Start time field
            EditorGUILayout.LabelField("Start Time (seconds)", EditorStyles.boldLabel);
            formStartTime = EditorGUILayout.FloatField(formStartTime);
            EditorGUILayout.Space(5);

            // Duration field
            EditorGUILayout.LabelField("Duration (seconds)", EditorStyles.boldLabel);
            formDuration = EditorGUILayout.FloatField(formDuration);
            DrawClipDynamicDetailsSection();
            EditorGUILayout.Space(15);

            // Validation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);

            if (string.IsNullOrWhiteSpace(formClipId))
            {
                EditorGUILayout.LabelField("⚠ Clip ID required", EditorStyles.helpBox);
            }
            else if (formDuration <= 0)
            {
                EditorGUILayout.LabelField("⚠ Duration must be > 0", EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.LabelField("✓ Ready to submit", EditorStyles.helpBox);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Buttons
            EditorGUILayout.BeginHorizontal();

            bool canSubmit = !string.IsNullOrWhiteSpace(formClipId) && formDuration > 0;

            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            GUI.enabled = canSubmit;
            if (GUILayout.Button(isCreateMode ? "✓ Create" : "✓ Update", GUILayout.Height(30)))
            {
                if (isCreateMode)
                {
                    CreateClipFromForm();
                }
                else
                {
                    UpdateClipFromForm();
                }
                EnterViewMode();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕ Cancel", GUILayout.Height(30)))
            {
                EnterViewMode();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void CreateClipFromForm()
        {
            if (director == null || editingTrack == null) return;

            var newClip = CreateClipInstanceByType(editingClipType);

            if (newClip != null)
            {
                if (newClip is MiniClipBase newClipBase)
                {
                    newClipBase.Id = formClipId;
                    newClipBase.Start = Mathf.Max(0f, formStartTime);
                    newClipBase.Duration = Mathf.Max(0.01f, formDuration);
                }

                ApplyDynamicDetailValuesToClip(newClip);

                RegisterUndo("Add Timeline Clip");
                if (director.AddClip(newClip, editingTrack.Id))
                {
                    director.MarkDirty();
                    MarkEditorDirty();
                    Debug.Log($"[MiniTimelineDirectorEditor] Created {editingClipType} '{formClipId}' on track '{editingTrack.Id}'");
                    SetStatusMessage($"Created clip '{formClipId}'.", MessageType.Info);
                    Repaint();
                }
            }
        }

        private void UpdateClipFromForm()
        {
            if (editingClip == null || editingTrack == null || director == null || director.Project == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(formClipId) || formDuration <= 0f)
            {
                return;
            }

            var trackClips = editingTrack.GetClips();
            if (trackClips.Any(c => !ReferenceEquals(c, editingClip) && c.Id == formClipId))
            {
                EditorUtility.DisplayDialog("Duplicate Clip ID", $"Clip ID '{formClipId}' already exists on track '{editingTrack.Id}'.", "OK");
                return;
            }

            RegisterUndo("Update Timeline Clip");

            if (editingClip is MiniClipBase clipBase)
            {
                clipBase.Id = formClipId;
                clipBase.Start = Mathf.Max(0f, formStartTime);
                clipBase.Duration = Mathf.Max(0.01f, formDuration);
                ApplyDynamicDetailValuesToClip(editingClip);

                director.MarkDirty();
                MarkEditorDirty();
                Debug.Log($"[MiniTimelineDirectorEditor] Updated clip '{clipBase.Id}' on track '{editingTrack.Id}'");
                SetStatusMessage($"Updated clip '{clipBase.Id}'.", MessageType.Info);
                Repaint();
            }
        }
        
        private void DrawDebugInfo()
        {
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Information", true);
            
            if (showDebugInfo)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.LabelField("Component State", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"State: {director.State}");
                EditorGUILayout.LabelField($"Current Time: {director.Time:F3}s");
                EditorGUILayout.LabelField($"Previous Time: {director.PreviousTime:F3}s");
                EditorGUILayout.LabelField($"Length: {director.Length:F3}s");
                EditorGUILayout.LabelField($"Speed: {director.PlaybackSpeed:F2}x");
                EditorGUILayout.LabelField($"Loop: {director.Loop}");
                EditorGUILayout.LabelField($"Playing: {director.IsPlaying}");
                
                if (director.Project != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Project Info", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Name: {director.Project.name}");
                    EditorGUILayout.LabelField($"Version: {director.Project.version}");
                    EditorGUILayout.LabelField($"Frame Rate: {director.Project.frameRate} FPS");
                    EditorGUILayout.LabelField($"Project Tracks: {director.Project.tracks.Count}");
                    EditorGUILayout.LabelField($"Runtime Tracks: {director.Tracks.Count}");
                    
                    // Show detailed track information if there's a mismatch
                    if (director.Project.tracks.Count != director.Tracks.Count)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Track Creation Details:", EditorStyles.boldLabel);
                        
                        for (int i = 0; i < director.Project.tracks.Count; i++)
                        {
                            var track = director.Project.tracks[i];
                            
                            var status = "✓ Ready";
                            var statusColor = Color.green;
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"[{i}] {track.Id} ({track.GetType().Name}):", GUILayout.Width(200));
                            
                            var oldColor = GUI.color;
                            GUI.color = statusColor;
                            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
                            GUI.color = oldColor;
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    if (director.BindingContext != null)
                    {
                        var bindingKeys = director.BindingContext.GetKeys().ToList();
                        EditorGUILayout.LabelField($"Bindings: {bindingKeys.Count}");
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        #endregion
        
        #region Project Management Methods
        
        private void CreateNewProject()
        {
            RegisterUndo("Create Timeline Project");
            if (director.CreateNewProject(newProjectName, newProjectLength, newProjectFrameRate))
            {
                MarkEditorDirty();
                Debug.Log($"[MiniTimelineDirectorEditor] Created new project '{newProjectName}' with length {newProjectLength}s");
                SetStatusMessage($"Created project '{newProjectName}'.", MessageType.Info);
            }
        }
        
        private void CreateSampleProject()
        {
            // Create project with sample tracks and clips
            var project = new MiniTimelineProject
            {
                name = newProjectName + " (Sample)",
                length = newProjectLength,
                frameRate = newProjectFrameRate,
                tracks = new List<IMiniTrack>()
            };

            RegisterUndo("Create Sample Timeline Project");
            director.SetProject(project);
            
            try
            {
                // Add sample Movement Track
                var movementTrack = new Tracks.MovementTrack
                {
                    Id = "SampleMovementTrack",
                    BindKey = "Actor1",
                    Enabled = true
                };
                
                // Add movement clip
                var moveClip = new Tracks.MovementClip
                {
                    Id = "Move_Forward",
                    Start = 2f,
                    Duration = 3f,
                    hasPosition = true,
                    startPosition = Vector3.zero,
                    endPosition = new Vector3(0, 0, 5),
                    animationCurve = Tracks.CameraAnimationCurve.Linear
                };
                movementTrack.AddClip(moveClip);
                
                director.AddTrack(movementTrack);
                
                // Add sample Signal Track
                var signalTrack = new Tracks.SignalTrack
                {
                    Id = "SampleSignalTrack",
                    BindKey = null, // Signals don't require binding
                    Enabled = true
                };
                
                // Add signal clips
                var startSignal = new Tracks.SignalClip
                {
                    Id = "StartSignal",
                    Start = 0f,
                    eventId = "OnTimelineStart",
                    payload = "Timeline started"
                };
                signalTrack.AddClip(startSignal);
                
                var midSignal = new Tracks.SignalClip
                {
                    Id = "MidpointSignal",
                    Start = 5f,
                    eventId = "OnMidpoint",
                    payload = "Midpoint reached"
                };
                signalTrack.AddClip(midSignal);
                
                var endSignal = new Tracks.SignalClip
                {
                    Id = "EndSignal",
                    Start = 9.9f,
                    eventId = "OnTimelineEnd",
                    payload = "Timeline ended"
                };
                signalTrack.AddClip(endSignal);
                
                director.AddTrack(signalTrack);
                
                // Save the sample project
                if (director.SaveProject())
                {
                    MarkEditorDirty();
                    Debug.Log($"[MiniTimelineDirectorEditor] Created and saved sample project '{project.name}' with {director.Tracks.Count} tracks");
                    SetStatusMessage($"Created sample project '{project.name}' ({director.Tracks.Count} tracks).", MessageType.Info);
                }
                else
                {
                    Debug.LogWarning("[MiniTimelineDirectorEditor] Sample project created but failed to save");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirectorEditor] Failed to create sample project: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog(
                    "Error Creating Sample", 
                    $"Failed to create sample project:\n{e.Message}\n\nCheck console for details.", 
                    "OK"
                );
            }
        }
        
        private void SaveProject()
        {
            if (director.Project == null)
            {
                EditorUtility.DisplayDialog("No Project", "No project loaded to save.", "OK");
                return;
            }
            
            // Option 1: Quick save to persistent path
            var quickSaveChoice = EditorUtility.DisplayDialogComplex(
                "Save Timeline Project",
                $"Save '{director.Project.name}' to persistent data path?\n\nPath: {director.GetProjectsFolder()}",
                "Save to Persistent Path",
                "Cancel",
                "Browse Custom Location"
            );
            
            if (quickSaveChoice == 0) // Save to persistent path
            {
                if (director.SaveProject())
                {
                    lastSavedPath = director.GetProjectFilePath(director.Project.name);
                    Debug.Log($"[MiniTimelineDirectorEditor] Project saved to: {lastSavedPath}");
                    SetStatusMessage($"Saved project to persistent path.", MessageType.Info);
                }
                else
                {
                    EditorUtility.DisplayDialog("Save Failed", "Failed to save project. Check console for details.", "OK");
                }
            }
            else if (quickSaveChoice == 2) // Browse custom location
            {
                var path = EditorUtility.SaveFilePanel(
                    "Save Timeline Project",
                    director.GetProjectsFolder(),
                    director.Project?.name ?? "NewProject",
                    "json"
                );
                
                if (!string.IsNullOrEmpty(path))
                {
                    // Save using the director/persistence system then export the saved file to the chosen path
                    if (director.SaveProject())
                    {
                        var savedPath = director.GetProjectFilePath(director.Project.name);
                        try
                        {
                            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                            {
                                File.Copy(savedPath, path, true);
                                lastSavedPath = path;
                                Debug.Log($"[MiniTimelineDirectorEditor] Project exported to: {path}");
                                SetStatusMessage($"Exported project to '{Path.GetFileName(path)}'.", MessageType.Info);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Export Failed", "Saved project file not found to export.", "OK");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[MiniTimelineDirectorEditor] Failed to export project: {e.Message}");
                            EditorUtility.DisplayDialog("Export Failed", $"Failed to export project:\n{e.Message}", "OK");
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Save Failed", "Failed to save project via persistence manager. Check console for details.", "OK");
                    }
                }
            }
        }
        
        private void LoadProject()
        {
            // Get available projects from persistent path
            var availableProjects = director.GetAvailableProjects();
            
            if (availableProjects.Length > 0)
            {
                // Show choice dialog
                var loadChoice = EditorUtility.DisplayDialogComplex(
                    "Load Timeline Project",
                    $"Load from persistent data path?\n\nFound {availableProjects.Length} project(s)\nPath: {director.GetProjectsFolder()}",
                    "Select from List",
                    "Cancel",
                    "Browse Custom Location"
                );
                
                if (loadChoice == 0) // Select from list
                {
                    ShowProjectSelectionMenu(availableProjects);
                }
                else if (loadChoice == 2) // Browse custom location
                {
                    LoadProjectFromCustomPath();
                }
            }
            else
            {
                // No projects found, offer custom browse
                if (EditorUtility.DisplayDialog(
                    "No Projects Found",
                    $"No projects found in persistent data path:\n{director.GetProjectsFolder()}\n\nBrowse for a project file?",
                    "Browse",
                    "Cancel"))
                {
                    LoadProjectFromCustomPath();
                }
            }
        }
        
        private void ShowProjectSelectionMenu(string[] projectNames)
        {
            var menu = new GenericMenu();
            
            foreach (var projectName in projectNames)
            {
                menu.AddItem(new GUIContent(projectName), false, () => LoadProjectByName(projectName));
            }
            
            menu.ShowAsContext();
        }
        
        private void LoadProjectByName(string projectName)
        {
            RegisterUndo("Load Timeline Project");
            if (director.LoadProject(projectName))
            {
                lastLoadedPath = director.GetProjectFilePath(projectName);
                MarkEditorDirty();
                Debug.Log($"[MiniTimelineDirectorEditor] Project loaded from: {lastLoadedPath}");
                SetStatusMessage($"Loaded project '{projectName}'.", MessageType.Info);
            }
            else
            {
                EditorUtility.DisplayDialog("Load Failed", "Failed to load project. Check console for details.", "OK");
            }
        }
        
        private void LoadProjectFromCustomPath()
        {
            var path = EditorUtility.OpenFilePanel(
                "Load Timeline Project",
                director.GetProjectsFolder(),
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                // Import the selected file into the persistent projects folder and load it via the director
                if (string.IsNullOrEmpty(director.GetProjectsFolder()))
                {
                    EditorUtility.DisplayDialog("Import Failed", "Persistent projects folder is not available.", "OK");
                    return;
                }

                var destPath = Path.Combine(director.GetProjectsFolder(), Path.GetFileName(path));
                try
                {
                    File.Copy(path, destPath, true);
                    // Attempt to load by project name (file name without extension)
                    var projectName = Path.GetFileNameWithoutExtension(path);
                    RegisterUndo("Load Timeline Project");
                    if (director.LoadProject(projectName))
                    {
                        lastLoadedPath = destPath;
                        MarkEditorDirty();
                        Debug.Log($"[MiniTimelineDirectorEditor] Project imported and loaded from: {destPath}");
                        SetStatusMessage($"Imported and loaded '{projectName}'.", MessageType.Info);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Load Failed", "Imported file failed to load via persistence manager.", "OK");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiniTimelineDirectorEditor] Failed to import/load project: {e.Message}");
                    EditorUtility.DisplayDialog("Import Failed", $"Failed to import project:\n{e.Message}", "OK");
                }
            }
        }
        
        private void QuickSave()
        {
            if (director.Project != null)
            {
                // Check if last saved path is in persistent data or custom location
                if (!string.IsNullOrEmpty(lastSavedPath) && lastSavedPath.StartsWith(director.GetProjectsFolder()))
                {
                    // Quick save to persistent path using director method
                    if (director.SaveProject())
                    {
                        Debug.Log($"[MiniTimelineDirectorEditor] Quick saved to: {lastSavedPath}");
                    }
                }
                else if (!string.IsNullOrEmpty(lastSavedPath))
                {
                    // For custom last-saved locations, export the latest persistent save to that custom path
                    if (director.SaveProject())
                    {
                        var savedPath = director.GetProjectFilePath(director.Project.name);
                        try
                        {
                            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                            {
                                File.Copy(savedPath, lastSavedPath, true);
                                Debug.Log($"[MiniTimelineDirectorEditor] Quick exported to: {lastSavedPath}");
                            }
                            else
                            {
                                Debug.LogWarning("[MiniTimelineDirectorEditor] Quick save export failed: source file not found.");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[MiniTimelineDirectorEditor] Quick save export failed: {e.Message}");
                        }
                    }
                }
                else
                {
                    // No last path, use default persistent path
                    if (director.SaveProject())
                    {
                        lastSavedPath = director.GetProjectFilePath(director.Project.name);
                        Debug.Log($"[MiniTimelineDirectorEditor] Quick saved to: {lastSavedPath}");
                    }
                }
            }
        }
        
        private void QuickLoad()
        {
            if (!string.IsNullOrEmpty(lastLoadedPath))
            {
                // Check if it's from persistent data or custom location
                if (lastLoadedPath.StartsWith(director.GetProjectsFolder()))
                {
                    // Load using director method
                    string projectName = Path.GetFileNameWithoutExtension(lastLoadedPath);
                    RegisterUndo("Load Timeline Project");
                    if (director.LoadProject(projectName))
                    {
                        MarkEditorDirty();
                        Debug.Log($"[MiniTimelineDirectorEditor] Quick loaded from: {lastLoadedPath}");
                        SetStatusMessage($"Quick loaded '{projectName}'.", MessageType.Info);
                    }
                }
                else
                {
                    // For custom last-loaded locations, import the project into persistent folder and load it
                    try
                    {
                        var destPath = Path.Combine(director.GetProjectsFolder(), Path.GetFileName(lastLoadedPath));
                        File.Copy(lastLoadedPath, destPath, true);
                        var projectName = Path.GetFileNameWithoutExtension(lastLoadedPath);
                        RegisterUndo("Load Timeline Project");
                        if (director.LoadProject(projectName))
                        {
                            MarkEditorDirty();
                            Debug.Log($"[MiniTimelineDirectorEditor] Quick imported and loaded from: {lastLoadedPath}");
                            SetStatusMessage($"Quick loaded '{projectName}'.", MessageType.Info);
                        }
                        else
                        {
                            Debug.LogWarning($"[MiniTimelineDirectorEditor] Quick import succeeded but load failed for: {lastLoadedPath}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MiniTimelineDirectorEditor] Quick load import failed: {e.Message}");
                    }
                }
            }
        }
        
        private void CloseProject()
        {
            if (EditorUtility.DisplayDialog("Close Project", "Are you sure you want to close the current project?", "Yes", "Cancel"))
            {
                RegisterUndo("Close Timeline Project");
                director.CloseProject();
                MarkEditorDirty();
                Debug.Log("[MiniTimelineDirectorEditor] Project closed");
                SetStatusMessage("Closed current project.", MessageType.Info);
            }
        }
        
        #endregion
        
        #region Binding Management Methods
        
        private void CreateBindingContextComponent()
        {
            if (director == null) return;
            
            // Check if one already exists on the GameObject
            var existingContext = director.GetComponent<BindableObjectManager>();
            if (existingContext != null)
            {
                RegisterUndo("Assign BindableObjectManager", includeBindingContext: true);
                // Assign it to the director
                director.BindingContext = existingContext;
                MarkEditorDirty(includeBindingContext: true);
                Debug.Log("[MiniTimelineDirectorEditor] Found and assigned existing BindingContext component");
            }
            else
            {
                // Create new BindingContext component
                RegisterUndo("Create BindableObjectManager", includeBindingContext: true);
                var newContext = Undo.AddComponent<BindableObjectManager>(director.gameObject);
                director.BindingContext = newContext;
                MarkEditorDirty(includeBindingContext: true);
                Debug.Log("[MiniTimelineDirectorEditor] Created and assigned new BindingContext component");
            }
            
            serializedObject.Update();
        }
        
        private void AutoBindSceneObjects()
        {
            if (director.BindingContext == null)
            {
                EditorUtility.DisplayDialog("No Binding Context", "Binding context is null.", "OK");
                return;
            }
            
            RegisterUndo("Auto-Bind Timeline Scene Objects", includeBindingContext: true);
            // Use the director's AutoBindSceneObjects method which handles both binding and rebinding
            director.RebindAllTracks();
            MarkEditorDirty(includeBindingContext: true);
            
            // Repaint to show updated UI
            Repaint();
            
            int totalBindings = director.BindingContext.GetKeys().Count();
            Debug.Log($"[MiniTimelineDirectorEditor] Auto-bound BindableObject components (Total bindings: {totalBindings})");
            EditorUtility.DisplayDialog("Auto-Binding Complete", $"Successfully auto-bound BindableObject components.\n\nTotal bindings: {totalBindings}\n\nTip: Add BindableObject component to GameObjects you want to bind.", "OK");
        }
        
        private void RebindAllTracks()
        {
            // Use the public method from director
            director.RebindAllTracks();
            MarkEditorDirty(includeBindingContext: true);
            Repaint();
        }
        
        private void ClearAllBindings()
        {
            if (director.BindingContext == null)
            {
                EditorUtility.DisplayDialog("No Binding Context", "Binding context is null.", "OK");
                return;
            }
            
            var keys = director.BindingContext.GetKeys().ToList();
            RegisterUndo("Clear Timeline Bindings", includeBindingContext: true);
            foreach (var key in keys)
            {
                director.BindingContext.Unbind(key);
            }
            
            // Rebind all tracks to update their IsBound status
            MarkEditorDirty(includeBindingContext: true);
            RebindAllTracks();
            
            Debug.Log($"[MiniTimelineDirectorEditor] Cleared {keys.Count} bindings");
        }
        
        #endregion
        
        #region Track Management Methods

        private void EnterViewMode()
        {
            crudPanelMode = CrudPanelMode.View;
            crudEntityKind = CrudEntityKind.None;
            editingTrack = null;
            editingClip = null;
            editingClipType = string.Empty;
            editingTrackType = string.Empty;
            isCreateMode = false;
            formClipDetailFields.Clear();
            formClipDetailValues.Clear();
            formClipDetailGroupFoldouts.Clear();
            formClipDetailInfoMessage = string.Empty;
        }

        private void ShowTrackDetailForm(string trackType)
        {
            if (director?.Project == null)
            {
                EditorUtility.DisplayDialog("No Project", "No project loaded. Create or load a project first.", "OK");
                return;
            }

            isCreateMode = true;
            crudPanelMode = CrudPanelMode.UpdateForm;
            crudEntityKind = CrudEntityKind.Track;
            editingTrack = null;
            editingTrackType = trackType;
            formTrackId = $"track_{trackType}_{Guid.NewGuid().ToString().Substring(0, 6)}";
            formTrackBindKey = null;
            formTrackEnabled = true;
            formTrackEvaluateMode = EvaluateMode.Continuous;
        }

        private void EditTrack(IMiniTrack track)
        {
            if (track == null)
            {
                return;
            }

            isCreateMode = false;
            crudPanelMode = CrudPanelMode.UpdateForm;
            crudEntityKind = CrudEntityKind.Track;
            editingTrack = track;
            editingTrackType = track.GetType().Name;
            formTrackId = track.Id;
            formTrackBindKey = track.BindKey;
            formTrackEnabled = track.Enabled;
            formTrackEvaluateMode = track.EvaluateMode;
        }

        private void CreateTrackFromForm()
        {
            if (director?.Project == null)
            {
                return;
            }

            if (director.Tracks.Any(t => t != null && t.Id == formTrackId))
            {
                EditorUtility.DisplayDialog("Duplicate Track ID", $"Track ID '{formTrackId}' already exists.", "OK");
                return;
            }

            var track = CreateTrackInstance(editingTrackType, formTrackId);
            if (track == null)
            {
                EditorUtility.DisplayDialog(
                    "Unsupported Track Type",
                    $"Cannot create track of type '{GetFriendlyTrackName(editingTrackType)}'.",
                    "OK");
                return;
            }

            RegisterUndo("Create Timeline Track");

            track.BindKey = formTrackBindKey;
            track.Enabled = formTrackEnabled;

            if (director.AddTrack(track))
            {
                if (track.GetType().GetProperty("EvaluateMode")?.CanWrite == true)
                {
                    track.GetType().GetProperty("EvaluateMode")?.SetValue(track, formTrackEvaluateMode);
                }

                director.MarkDirty();
                MarkEditorDirty();
                SetStatusMessage($"Created track '{track.Id}'.", MessageType.Info);
                EnterViewMode();
                Repaint();
            }
        }

        private void UpdateTrackFromForm()
        {
            if (editingTrack == null || director?.Project == null)
            {
                return;
            }

            if (director.Tracks.Any(t => t != null && !ReferenceEquals(t, editingTrack) && t.Id == formTrackId))
            {
                EditorUtility.DisplayDialog("Duplicate Track ID", $"Track ID '{formTrackId}' already exists.", "OK");
                return;
            }

            RegisterUndo("Update Timeline Track");

            var previousTrackId = editingTrack.Id;
            if (editingTrack.GetType().GetProperty("Id")?.CanWrite == true)
            {
                editingTrack.GetType().GetProperty("Id")?.SetValue(editingTrack, formTrackId);
            }

            editingTrack.BindKey = formTrackBindKey;
            editingTrack.Enabled = formTrackEnabled;

            if (editingTrack.GetType().GetProperty("EvaluateMode")?.CanWrite == true)
            {
                editingTrack.GetType().GetProperty("EvaluateMode")?.SetValue(editingTrack, formTrackEvaluateMode);
            }

            var trackData = director.Project.tracks.FirstOrDefault(t => t != null && t.Id == previousTrackId);
            if (trackData?.GetType().GetProperty("Id")?.CanWrite == true)
            {
                trackData.GetType().GetProperty("Id")?.SetValue(trackData, formTrackId);
            }

            trackData?.GetType().GetProperty("BindKey")?.SetValue(trackData, formTrackBindKey);
            trackData?.GetType().GetProperty("Enabled")?.SetValue(trackData, formTrackEnabled);
            if (trackData?.GetType().GetProperty("EvaluateMode")?.CanWrite == true)
            {
                trackData.GetType().GetProperty("EvaluateMode")?.SetValue(trackData, formTrackEvaluateMode);
            }

            if (director.BindingContext != null)
            {
                editingTrack.Bind(director.BindingContext);
            }

            director.MarkDirty();
            MarkEditorDirty();
            SetStatusMessage($"Updated track '{formTrackId}'.", MessageType.Info);
            EnterViewMode();
            Repaint();
        }
        
        private void ShowAddTrackMenu()
        {
            var menu = new GenericMenu();
            
            var trackTypes = new string[]
            {
                MiniTimelineConstants.TRACK_ANIM,
                MiniTimelineConstants.TRACK_ANIMATOR,
                MiniTimelineConstants.TRACK_MORPH,
                MiniTimelineConstants.TRACK_EXPRESSION,
                MiniTimelineConstants.TRACK_MOVEMENT,
                MiniTimelineConstants.TRACK_SIGNAL,
                MiniTimelineConstants.TRACK_CINEMACHINE,
                "NavMeshMovementTrack",
                MiniTimelineConstants.TRACK_UMA_WARDROBE,
                MiniTimelineConstants.TRACK_UMA_EXPRESSION
            };
            
            foreach (var trackType in trackTypes)
            {
                var capturedType = trackType; // Capture for closure
                menu.AddItem(new GUIContent(GetFriendlyTrackName(capturedType)), false, () => ShowTrackDetailForm(capturedType));
            }
            
            menu.ShowAsContext();
        }
        
        private string GetFriendlyTrackName(string trackType)
        {
            return trackType switch
            {
                MiniTimelineConstants.TRACK_ANIM => "Animation Track",
                MiniTimelineConstants.TRACK_ANIMATOR => "Animator Track",
                MiniTimelineConstants.TRACK_MORPH => "Morph Track",
                MiniTimelineConstants.TRACK_MOVEMENT => "Movement Track",
                MiniTimelineConstants.TRACK_SIGNAL => "Signal Track",
                MiniTimelineConstants.TRACK_AUDIO => "Audio Track",
                MiniTimelineConstants.TRACK_EXPRESSION => "Expression Track",
                MiniTimelineConstants.TRACK_CINEMACHINE => "Cinemachine Track",
                "NavMeshMovementTrack" => "NavMesh Movement Track",
                MiniTimelineConstants.TRACK_UMA_WARDROBE => "UMA Wardrobe Track",
                MiniTimelineConstants.TRACK_UMA_EXPRESSION => "UMA Expression Track",
                _ => trackType + " Track"
            };
        }
        
        private void AddTrack(string trackType)
        {
            if (director.Project == null)
            {
                EditorUtility.DisplayDialog("No Project", "No project loaded. Create or load a project first.", "OK");
                return;
            }

            var track = CreateTrackInstance(trackType);
            if (track == null)
            {
                EditorUtility.DisplayDialog(
                    "Unsupported Track Type",
                    $"Cannot create track of type '{GetFriendlyTrackName(trackType)}'.\n\nThe track type is defined but may require additional plugins (e.g. UMA) to be installed.",
                    "OK");
                return;
            }

            RegisterUndo("Add Timeline Track");
            if (director.AddTrack(track))
            {
                director.MarkDirty();
                MarkEditorDirty();
                Debug.Log($"[MiniTimelineDirectorEditor] Added '{GetFriendlyTrackName(trackType)}' with id '{track.Id}'");
                Repaint();
            }
        }

        private IMiniTrack CreateTrackInstance(string trackType, string explicitTrackId = null)
        {
            var trackId = string.IsNullOrWhiteSpace(explicitTrackId)
                ? $"track_{trackType}_{Guid.NewGuid().ToString().Substring(0, 6)}"
                : explicitTrackId;

            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return new Tracks.AnimTrack { Id = trackId, Name = "Animation Track", Enabled = true };

                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return new Tracks.AnimatorTrack { Id = trackId, Name = "Animator Track", Enabled = true };

                case MiniTimelineConstants.TRACK_MORPH:
                    return new Tracks.MorphTrack { Id = trackId, Name = "Morph Track", Enabled = true };

                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return new Tracks.MovementTrack { Id = trackId, Name = "Movement Track", Enabled = true };

                case MiniTimelineConstants.TRACK_SIGNAL:
                    return new Tracks.SignalTrack { Id = trackId, Name = "Signal Track", Enabled = true };

                case MiniTimelineConstants.TRACK_CINEMACHINE:
                    return new Tracks.CinemachineTrack { Id = trackId, Name = "Cinemachine Track", Enabled = true };

                case "NavMeshMovementTrack":
                    return new Tracks.NavMeshMovementTrack { Id = trackId, Name = "NavMesh Movement Track", Enabled = true };

                default:
                    // Try dynamic resolution for UMA and other plugin tracks
                    var type = TypeResolver.ResolveType(trackType);
                    if (type != null && typeof(IMiniTrack).IsAssignableFrom(type))
                    {
                        try
                        {
                            var instance = System.Activator.CreateInstance(type) as IMiniTrack;
                            if (instance != null)
                            {
                                type.GetProperty("Id")?.SetValue(instance, trackId);
                                type.GetProperty("Name")?.SetValue(instance, GetFriendlyTrackName(trackType));
                                type.GetProperty("Enabled")?.SetValue(instance, true);
                                return instance;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[MiniTimelineDirectorEditor] Failed to instantiate track type '{trackType}': {e.Message}");
                        }
                    }
                    return null;
            }
        }
        
        private void RemoveTrack(IMiniTrack track)
        {
            if (director.Project == null || track == null)
            {
                return;
            }
            
            // Find and remove track data from project
            var trackData = director.Project.tracks.FirstOrDefault(t => t != null && t.Id == track.Id);
            if (trackData != null)
            {
                RegisterUndo("Remove Timeline Track");
                director.Project.tracks.Remove(trackData);
                
                // Rebuild tracks
                director.MarkDirty();
                MarkEditorDirty();
                
                Debug.Log($"[MiniTimelineDirectorEditor] Removed track '{track.Id}'");
                
                // Repaint to show changes
                Repaint();
            }
        }
        
        private void UpdateTrackBindKey(IMiniTrack track, string newBindKey)
        {
            if (director.Project == null || track == null)
            {
                return;
            }
            
            // Update in track data
            var trackData = director.Project.tracks.FirstOrDefault(t => t != null && t.Id == track.Id);
            if (trackData != null)
            {
                RegisterUndo("Update Timeline Track Bind Key");
                // Update runtime track bindKey
                var bindProp = trackData.GetType().GetProperty("BindKey");
                if (bindProp?.CanWrite == true)
                {
                    bindProp.SetValue(trackData, newBindKey);
                }
                
                // Update runtime track using reflection
                if (track.GetType().GetProperty("BindKey")?.CanWrite == true)
                {
                    track.GetType().GetProperty("BindKey").SetValue(track, newBindKey);
                }
                
                // Rebind track
                if (director.BindingContext != null)
                {
                    track.Bind(director.BindingContext);
                }

                MarkEditorDirty();
                
                Debug.Log($"[MiniTimelineDirectorEditor] Updated track '{track.Id}' bind key to '{newBindKey ?? "None"}'");
            }
        }
        
        private void UpdateTrackEnabled(IMiniTrack track, bool enabled)
        {
            if (director.Project == null || track == null)
            {
                return;
            }
            
            // Update in track data
            var trackData = director.Project.tracks.FirstOrDefault(t => t != null && t.Id == track.Id);
            if (trackData != null)
            {
                RegisterUndo("Update Timeline Track Enabled");
                // Update via reflection since DTO field is no longer available
                var enabledProp = trackData.GetType().GetProperty("Enabled");
                if (enabledProp?.CanWrite == true)
                {
                    enabledProp.SetValue(trackData, enabled);
                }
                
                // Update runtime track using reflection
                if (track.GetType().GetProperty("Enabled")?.CanWrite == true)
                {
                    track.GetType().GetProperty("Enabled").SetValue(track, enabled);
                }

                MarkEditorDirty();
                
                Debug.Log($"[MiniTimelineDirectorEditor] Updated track '{track.Id}' enabled to {enabled}");
            }
        }

        private void DrawMultiTargetBindings(IMiniTrack track)
        {
            var targetBindKeysProp = track.GetType().GetProperty("TargetBindKeys");
            if (targetBindKeysProp == null) return;

            var targetBindKeys = targetBindKeysProp.GetValue(track) as List<string>;
            // If the list is null, we can't really edit it. The track usually initializes it.
            if (targetBindKeys == null) return;

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Multi-Target Bindings:", EditorStyles.boldLabel);
            
            var keysToRemove = new List<int>();
            var keysToUpdate = new Dictionary<int, string>();
            
            for (int i = 0; i < targetBindKeys.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Target {i}:", GUILayout.Width(60));
                
                // Dropdown for available keys
                var availableKeys = new List<string> { "None" };
                if (director.BindingContext != null)
                {
                    availableKeys.AddRange(director.BindingContext.GetKeys());
                }

                var currentKey = targetBindKeys[i] ?? "None";
                var currentIndex = availableKeys.IndexOf(currentKey);
                if (currentIndex == -1) 
                {
                     availableKeys.Add(currentKey);
                     currentIndex = availableKeys.Count - 1;
                }

                var newIndex = EditorGUILayout.Popup(currentIndex, availableKeys.ToArray());
                if (newIndex != currentIndex)
                {
                    var newKey = availableKeys[newIndex];
                    if (newKey == "None") newKey = null; // Map "None" back to null or keep as null
                    keysToUpdate[i] = newKey;
                }

                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    keysToRemove.Add(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            // Add new target button
            if (GUILayout.Button("+ Add Target"))
            {
                // We need to create a new list to trigger the update logic properly if we want to be safe, 
                // or just modify and call update.
                // Since UpdateTrackTargetBindKeys sets the property, let's copy the list.
                var newList = new List<string>(targetBindKeys);
                newList.Add(null);
                UpdateTrackTargetBindKeys(track, newList);
            }

            // Apply updates
            if (keysToUpdate.Count > 0 || keysToRemove.Count > 0)
            {
                var newList = new List<string>(targetBindKeys);
                
                foreach (var kvp in keysToUpdate)
                {
                    if (kvp.Key < newList.Count)
                        newList[kvp.Key] = kvp.Value;
                }
                
                // Remove in reverse order
                keysToRemove.Sort((a, b) => b.CompareTo(a));
                foreach (var index in keysToRemove)
                {
                    if (index < newList.Count)
                        newList.RemoveAt(index);
                }

                UpdateTrackTargetBindKeys(track, newList);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void UpdateTrackTargetBindKeys(IMiniTrack track, List<string> newKeys)
        {
             if (director.Project == null || track == null) return;

            RegisterUndo("Update Timeline Target Bindings");

            // Update in track data (Project.tracks)
            var trackData = director.Project.tracks.FirstOrDefault(t => t != null && t.Id == track.Id);
            if (trackData != null)
            {
                var prop = trackData.GetType().GetProperty("TargetBindKeys");
                if (prop?.CanWrite == true)
                {
                    prop.SetValue(trackData, newKeys);
                }
            }
            
            // Update runtime track
             var runtimeProp = track.GetType().GetProperty("TargetBindKeys");
            if (runtimeProp?.CanWrite == true)
            {
                 runtimeProp.SetValue(track, newKeys);
            }
            
            // Rebind
            if (director.BindingContext != null)
            {
                track.Bind(director.BindingContext);
            }
            
            director.MarkDirty();
            MarkEditorDirty();
            Debug.Log($"[MiniTimelineDirectorEditor] Updated target bind keys for track '{track.Id}'");
        }

        
        #endregion
        
        #region Clip Management Methods
        
        private void ShowAddClipMenu(IMiniTrack track)
        {
            var menu = new GenericMenu();
            var trackType = GetTrackType(track);

            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    menu.AddItem(new GUIContent("Animation Clip"), false, () => ShowClipDetailForm(track, "AnimClip"));
                    break;

                case MiniTimelineConstants.TRACK_ANIMATOR:
                    menu.AddItem(new GUIContent("Animator Clip"), false, () => ShowClipDetailForm(track, "AnimatorClip"));
                    break;

                case MiniTimelineConstants.TRACK_MORPH:
                    menu.AddItem(new GUIContent("Morph Key Clip"), false, () => ShowClipDetailForm(track, "MorphKeyClip"));
                    break;

                case MiniTimelineConstants.TRACK_MOVEMENT:
                    menu.AddItem(new GUIContent("Movement Clip"), false, () => ShowClipDetailForm(track, "MovementClip"));
                    break;

                case MiniTimelineConstants.TRACK_SIGNAL:
                    menu.AddItem(new GUIContent("Signal Clip"), false, () => ShowClipDetailForm(track, "SignalClip"));
                    break;

                case MiniTimelineConstants.TRACK_CINEMACHINE:
                    menu.AddItem(new GUIContent("Cinemachine Clip"), false, () => ShowClipDetailForm(track, "CinemachineClip"));
                    break;

                case MiniTimelineConstants.TRACK_NAV_MESH_MOVEMENT:
                    menu.AddItem(new GUIContent("NavMesh Movement Clip"), false, () => ShowClipDetailForm(track, "NavMeshMovementClip"));
                    break;

                default:
                    // Fallback for UMA or unknown track types
                    menu.AddItem(new GUIContent("Generic Clip"), false, () => ShowClipDetailForm(track, "GenericClip"));
                    break;
            }

            menu.ShowAsContext();
        }

        /// <summary>Opens the clip detail form inline (replaces DrawTrackInfo content).</summary>
        private void ShowClipDetailForm(IMiniTrack track, string clipType)
        {
            crudPanelMode = CrudPanelMode.UpdateForm;
            crudEntityKind = CrudEntityKind.Clip;
            isCreateMode = true;
            editingTrack = track;
            editingClip = null;
            editingClipType = clipType;

            // Auto-generate clip ID
            formClipId = $"clip_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Auto-position after last clip
            float nextStart = 0f;
            var clips = track.GetClips();
            if (clips != null && clips.Any())
            {
                nextStart = clips.Max(c => c.Start + c.Duration);
            }
            formStartTime = nextStart;
            formDuration = 1f;
            InitializeDynamicClipDetailState(clipType, null);
        }

        /// <summary>Opens the clip detail form inline to edit an existing clip.</summary>
        private void EditClip(IMiniTrack track, IMiniClip clip)
        {
            if (clip == null || track == null) return;

            crudPanelMode = CrudPanelMode.UpdateForm;
            crudEntityKind = CrudEntityKind.Clip;
            isCreateMode = false;
            editingTrack = track;
            editingClip = clip;
            editingClipType = clip.GetType().Name;
            formClipId = clip.Id;
            formStartTime = clip.Start;
            formDuration = clip.Duration;
            InitializeDynamicClipDetailState(editingClipType, clip);
        }

        private void DrawClipDynamicDetailsSection()
        {
            if (formClipDetailFields.Count == 0)
            {
                if (!string.IsNullOrEmpty(formClipDetailInfoMessage))
                {
                    EditorGUILayout.HelpBox(formClipDetailInfoMessage, MessageType.Info);
                }
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Clip Properties", EditorStyles.boldLabel);

            var groups = formClipDetailFields
                .GroupBy(f => f.Group)
                .OrderBy(g => g.Key == "Other" ? 1 : 0)
                .ThenBy(g => g.Key);

            foreach (var group in groups)
            {
                if (!formClipDetailGroupFoldouts.ContainsKey(group.Key))
                {
                    formClipDetailGroupFoldouts[group.Key] = true;
                }

                formClipDetailGroupFoldouts[group.Key] = EditorGUILayout.Foldout(formClipDetailGroupFoldouts[group.Key], group.Key, true);
                if (!formClipDetailGroupFoldouts[group.Key])
                {
                    continue;
                }

                EditorGUI.indentLevel++;
                foreach (var field in group.OrderBy(f => f.SortPriority).ThenBy(f => f.DeclarationOrder))
                {
                    DrawDynamicClipField(field);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }

            if (!string.IsNullOrEmpty(formClipDetailInfoMessage))
            {
                EditorGUILayout.HelpBox(formClipDetailInfoMessage, MessageType.Info);
            }
        }

        private void DrawDynamicClipField(ClipDetailField field)
        {
            if (field == null || field.Field == null)
            {
                return;
            }

            if (!formClipDetailValues.TryGetValue(field.Name, out var value))
            {
                value = GetDefaultValue(field.Field.FieldType);
                formClipDetailValues[field.Name] = value;
            }

            var fieldType = field.Field.FieldType;

            if (fieldType == typeof(bool))
            {
                formClipDetailValues[field.Name] = EditorGUILayout.Toggle(field.Label, value is bool boolValue && boolValue);
                return;
            }

            if (fieldType == typeof(int))
            {
                formClipDetailValues[field.Name] = EditorGUILayout.IntField(field.Label, value is int intValue ? intValue : 0);
                return;
            }

            if (fieldType == typeof(float))
            {
                formClipDetailValues[field.Name] = EditorGUILayout.FloatField(field.Label, value is float floatValue ? floatValue : 0f);
                return;
            }

            if (fieldType == typeof(Vector3))
            {
                formClipDetailValues[field.Name] = EditorGUILayout.Vector3Field(field.Label, value is Vector3 vector3Value ? vector3Value : Vector3.zero);
                return;
            }

            if (fieldType == typeof(Quaternion))
            {
                var currentQuaternion = value is Quaternion quaternionValue ? quaternionValue : Quaternion.identity;
                var eulerAngles = EditorGUILayout.Vector3Field(field.Label, currentQuaternion.eulerAngles);
                formClipDetailValues[field.Name] = Quaternion.Euler(eulerAngles);
                return;
            }

            if (fieldType.IsEnum)
            {
                var enumValue = value as Enum;
                if (enumValue == null)
                {
                    enumValue = (Enum)Enum.GetValues(fieldType).GetValue(0);
                }

                formClipDetailValues[field.Name] = EditorGUILayout.EnumPopup(field.Label, enumValue);
                return;
            }

            EditorGUILayout.LabelField($"{field.Label}:", $"Unsupported type ({fieldType.Name})", EditorStyles.miniLabel);
        }

        private void InitializeDynamicClipDetailState(string clipType, IMiniClip sourceClip)
        {
            formClipDetailFields.Clear();
            formClipDetailValues.Clear();
            formClipDetailGroupFoldouts.Clear();
            formClipDetailInfoMessage = string.Empty;

            var clipTypeRuntime = ResolveClipRuntimeType(clipType);
            if (clipTypeRuntime == null)
            {
                formClipDetailInfoMessage = "No dynamic details available for this clip type.";
                return;
            }

            var workingSource = sourceClip ?? CreateClipInstanceByType(clipType);
            var fields = clipTypeRuntime.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsStatic)
                .Where(f => f.Name != nameof(IMiniClip.Id) && f.Name != nameof(IMiniClip.Start) && f.Name != nameof(IMiniClip.Duration))
                .OrderBy(f => f.MetadataToken)
                .ToList();

            if (fields.Count == 0)
            {
                formClipDetailInfoMessage = "This clip type does not expose editable public fields.";
                return;
            }

            var unsupportedCount = 0;
            foreach (var field in fields)
            {
                var supported = IsDynamicFieldTypeSupported(field.FieldType);
                if (!supported)
                {
                    unsupportedCount++;
                }

                var clipField = new ClipDetailField
                {
                    Name = field.Name,
                    Label = ToInspectorLabel(field.Name),
                    Group = InferGroupName(field.Name),
                    Field = field,
                    SortPriority = GetFieldSortPriority(field.Name),
                    DeclarationOrder = field.MetadataToken
                };

                formClipDetailFields.Add(clipField);

                var rawValue = workingSource != null ? field.GetValue(workingSource) : null;
                formClipDetailValues[field.Name] = rawValue ?? GetDefaultValue(field.FieldType);
            }

            if (unsupportedCount > 0)
            {
                formClipDetailInfoMessage =
                    $"{unsupportedCount} field(s) use unsupported types and are shown as read-only hints. " +
                    "Fallback rendering is otherwise automatic for all supported fields.";
            }
        }

        private void ApplyDynamicDetailValuesToClip(IMiniClip clip)
        {
            if (clip == null || formClipDetailFields.Count == 0)
            {
                return;
            }

            foreach (var detailField in formClipDetailFields)
            {
                if (detailField?.Field == null || !detailField.Field.IsPublic)
                {
                    continue;
                }

                if (!formClipDetailValues.TryGetValue(detailField.Name, out var value))
                {
                    continue;
                }

                try
                {
                    detailField.Field.SetValue(clip, value);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MiniTimelineDirectorEditor] Failed setting clip field '{detailField.Name}': {ex.Message}");
                }
            }
        }

        private IMiniClip CreateClipInstanceByType(string clipType)
        {
            switch (clipType)
            {
                case "AnimClip":
                    return new Tracks.AnimClip
                    {
                        speed = 1f,
                        wrapMode = Tracks.AnimWrapMode.Once,
                        weight = 1f
                    };
                case "AnimatorClip":
                    return new Tracks.AnimatorClip();
                case "MorphKeyClip":
                    return new Tracks.MorphKeyClip();
                case "MovementClip":
                    return new Tracks.MovementClip();
                case "SignalClip":
                    return new Tracks.SignalClip();
                case "CinemachineClip":
                    return new Tracks.CinemachineClip
                    {
                        shotType = Tracks.CinemachineShotType.EyeLevel
                    };
                case "NavMeshMovementClip":
                    return new Tracks.NavMeshMovementClip
                    {
                        hasPosition = true,
                        startPosition = Vector3.zero,
                        endPosition = Vector3.zero,
                        hasSpeed = false,
                        startSpeed = 3.5f,
                        endSpeed = 3.5f,
                        animationCurve = Tracks.CameraAnimationCurve.Linear
                    };
                default:
                    Debug.LogWarning($"[MiniTimelineDirectorEditor] Unknown clip type: {clipType}, using AnimClip fallback.");
                    return new Tracks.AnimClip();
            }
        }

        private Type ResolveClipRuntimeType(string clipType)
        {
            switch (clipType)
            {
                case "AnimClip":
                    return typeof(Tracks.AnimClip);
                case "AnimatorClip":
                    return typeof(Tracks.AnimatorClip);
                case "MorphKeyClip":
                    return typeof(Tracks.MorphKeyClip);
                case "MovementClip":
                    return typeof(Tracks.MovementClip);
                case "SignalClip":
                    return typeof(Tracks.SignalClip);
                case "CinemachineClip":
                    return typeof(Tracks.CinemachineClip);
                case "NavMeshMovementClip":
                    return typeof(Tracks.NavMeshMovementClip);
                default:
                    if (editingClip != null)
                    {
                        return editingClip.GetType();
                    }
                    return null;
            }
        }

        private bool IsDynamicFieldTypeSupported(Type fieldType)
        {
            return fieldType == typeof(bool) ||
                   fieldType == typeof(int) ||
                   fieldType == typeof(float) ||
                   fieldType == typeof(Vector3) ||
                     fieldType == typeof(Quaternion) ||
                   fieldType.IsEnum;
        }

        private object GetDefaultValue(Type fieldType)
        {
            if (fieldType == typeof(bool)) return false;
            if (fieldType == typeof(int)) return 0;
            if (fieldType == typeof(float)) return 0f;
            if (fieldType == typeof(Vector3)) return Vector3.zero;
            if (fieldType == typeof(Quaternion)) return Quaternion.identity;
            if (fieldType.IsEnum)
            {
                var enumValues = Enum.GetValues(fieldType);
                return enumValues.Length > 0 ? enumValues.GetValue(0) : Activator.CreateInstance(fieldType);
            }

            return fieldType.IsValueType ? Activator.CreateInstance(fieldType) : null;
        }

        private string InferGroupName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return "Other";

            if (fieldName.StartsWith("has", StringComparison.Ordinal) && fieldName.Length > 3)
            {
                return ToInspectorLabel(fieldName.Substring(3));
            }

            if (fieldName.StartsWith("start", StringComparison.Ordinal) && fieldName.Length > 5)
            {
                return ToInspectorLabel(fieldName.Substring(5));
            }

            if (fieldName.StartsWith("end", StringComparison.Ordinal) && fieldName.Length > 3)
            {
                return ToInspectorLabel(fieldName.Substring(3));
            }

            return "Other";
        }

        private int GetFieldSortPriority(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return 99;
            if (fieldName.StartsWith("has", StringComparison.Ordinal)) return 0;
            if (fieldName.StartsWith("start", StringComparison.Ordinal)) return 1;
            if (fieldName.StartsWith("end", StringComparison.Ordinal)) return 2;
            return 50;
        }

        private string ToInspectorLabel(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;
            return ObjectNames.NicifyVariableName(rawName);
        }

        private string GetTrackType(IMiniTrack track)
        {
            if (track == null) return "";
            return track.GetType().Name;
        }

        /// <summary>Returns the start time placed after the last existing clip on the track.</summary>
        private float GetNextClipStartTime(IMiniTrack track, float defaultDuration = 1f)
        {
            var clips = track.GetClips();
            return clips != null && clips.Any() ? clips.Max(c => c.Start + c.Duration) : 0f;
        }
        
        private void RemoveClip(IMiniTrack track, IMiniClip clip)
        {
            if (track == null || clip == null) return;
            
            RegisterUndo("Remove Timeline Clip");
            // Remove from track runtime
            track.RemoveClip(clip);
            
            // Mark director dirty to rebuild
            director.MarkDirty();
            MarkEditorDirty();
            
            Debug.Log($"[MiniTimelineDirectorEditor] Removed clip '{clip.Id}' from track '{track.Id}'");
            Repaint();
        }
        
        private void UpdateClipStart(IMiniTrack track, IMiniClip clip, float newStart)
        {
            if (track == null || clip == null) return;
            
            // Update via runtime clip cast
            var clipBase = clip as MiniClipBase;
            if (clipBase != null)
            {
                RegisterUndo("Update Timeline Clip Start");
                clipBase.Start = newStart;
                director.MarkDirty();
                MarkEditorDirty();
                Debug.Log($"[MiniTimelineDirectorEditor] Updated clip '{clip.Id}' start to {newStart}s");
            }
        }
        
        private void UpdateClipDuration(IMiniTrack track, IMiniClip clip, float newDuration)
        {
            if (track == null || clip == null) return;
            
            // Update via runtime clip cast
            var clipBase = clip as MiniClipBase;
            if (clipBase != null)
            {
                RegisterUndo("Update Timeline Clip Duration");
                clipBase.Duration = newDuration;
                director.MarkDirty();
                MarkEditorDirty();
                Debug.Log($"[MiniTimelineDirectorEditor] Updated clip '{clip.Id}' duration to {newDuration}s");
            }
        }
        
        #endregion
        
        #region Clip Visualization Helper Methods
        
        private void DrawClipTimelineBar(IMiniClip clip, float maxTime)
        {
            if (maxTime <= 0) maxTime = director.Length;
            
            var rect = EditorGUILayout.GetControlRect(false, 20);
            var timelineRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 16);
            
            // Draw background (full timeline)
            EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            
            // Calculate clip position and width
            var clipStartNormalized = clip.Start / maxTime;
            var clipDurationNormalized = clip.Duration / maxTime;
            
            var clipX = timelineRect.x + (timelineRect.width * clipStartNormalized);
            var clipWidth = timelineRect.width * clipDurationNormalized;
            
            // Ensure minimum visible width for zero-duration clips (signals)
            if (clipWidth < 2f)
            {
                clipWidth = 2f;
            }
            
            var clipRect = new Rect(clipX, timelineRect.y, clipWidth, timelineRect.height);
            
            // Draw clip bar with color based on type
            var clipColor = GetClipColor(clip);
            EditorGUI.DrawRect(clipRect, clipColor);
            
            // Draw clip borders
            var borderColor = clipColor * 0.7f;
            borderColor.a = 1f;
            
            // Left border
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, 1, clipRect.height), borderColor);
            // Right border
            EditorGUI.DrawRect(new Rect(clipRect.xMax - 1, clipRect.y, 1, clipRect.height), borderColor);
            // Top border
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, 1), borderColor);
            // Bottom border
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.yMax - 1, clipRect.width, 1), borderColor);
            
            // Draw time markers
            var markerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 8,
                alignment = TextAnchor.MiddleLeft
            };
            
            // Start time marker
            var startLabel = $"{clip.Start:F1}s";
            var startLabelRect = new Rect(timelineRect.x + 2, timelineRect.y, 50, timelineRect.height);
            GUI.Label(startLabelRect, startLabel, markerStyle);
            
            // End time marker (if there's space)
            if (clipRect.width > 30)
            {
                var endTime = clip.Start + clip.Duration;
                var endLabel = $"{endTime:F1}s";
                var endLabelSize = markerStyle.CalcSize(new GUIContent(endLabel));
                var endLabelRect = new Rect(clipRect.xMax - endLabelSize.x - 2, timelineRect.y, endLabelSize.x, timelineRect.height);
                
                // Draw with contrasting color
                var oldColor = GUI.color;
                GUI.color = Color.white;
                GUI.Label(endLabelRect, endLabel, markerStyle);
                GUI.color = oldColor;
            }
        }
        
        private string GetClipIcon(IMiniClip clip)
        {
            var clipTypeName = clip.GetType().Name.ToLower();
            
            if (clipTypeName.Contains("signal"))
                return "⚡";
            else if (clipTypeName.Contains("movement") || clipTypeName.Contains("camera"))
                return "🎬";
            else if (clipTypeName.Contains("anim"))
                return "🎭";
            else if (clipTypeName.Contains("audio"))
                return "🔊";
            else if (clipTypeName.Contains("morph") || clipTypeName.Contains("expression"))
                return "😊";
            else if (clipTypeName.Contains("wardrobe"))
                return "👔";
            
            return "📌";
        }
        
        private Color GetClipColor(IMiniClip clip)
        {
            var clipTypeName = clip.GetType().Name.ToLower();
            
            if (clipTypeName.Contains("signal"))
                return new Color(1f, 0.8f, 0.2f, 0.8f); // Yellow/Gold for signals
            else if (clipTypeName.Contains("movement") || clipTypeName.Contains("camera"))
                return new Color(0.3f, 0.7f, 1f, 0.8f); // Blue for movement/camera
            else if (clipTypeName.Contains("anim"))
                return new Color(0.8f, 0.3f, 0.8f, 0.8f); // Purple for animation
            else if (clipTypeName.Contains("audio"))
                return new Color(0.3f, 1f, 0.3f, 0.8f); // Green for audio
            else if (clipTypeName.Contains("morph") || clipTypeName.Contains("expression"))
                return new Color(1f, 0.5f, 0.3f, 0.8f); // Orange for morph/expression
            else if (clipTypeName.Contains("wardrobe"))
                return new Color(0.9f, 0.3f, 0.5f, 0.8f); // Pink for wardrobe
            
            return new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray for unknown
        }
        
        private void DrawClipSerializedData(IMiniTrack track, IMiniClip clip)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Display runtime clip data
            GUI.color = new Color(0.9f, 0.9f, 0.6f);
            EditorGUILayout.LabelField("Runtime Clip Data:", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.LabelField($"Type: {clip.GetType().Name}");
            EditorGUILayout.LabelField($"Start: {clip.Start:F3}s");
            EditorGUILayout.LabelField($"Duration: {clip.Duration:F3}s");
            EditorGUILayout.LabelField($"End: {(clip.Start + clip.Duration):F3}s");
            EditorGUILayout.LabelField($"ID: {clip.Id}");
            
            // Try to find serialized data
            if (director.Project != null)
            {
                var trackData = director.Project.tracks?.FirstOrDefault(t => t != null && t.Id == track.Id);
                if (trackData != null)
                {
                    var clipList = trackData.GetClips();
                    var clipData = clipList?.FirstOrDefault(c => c.Id == clip.Id);
                    if (clipData != null)
                    {
                        EditorGUILayout.Space(5);
                        GUI.color = new Color(0.6f, 0.9f, 0.9f);
                        EditorGUILayout.LabelField("Clip Data:", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                        
                        EditorGUILayout.LabelField($"Start: {clipData.Start:F3}s");
                        EditorGUILayout.LabelField($"Duration: {clipData.Duration:F3}s");
                        EditorGUILayout.LabelField($"ID: {clipData.Id}");
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Clip data not found", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Track data not found in project", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No project loaded", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private string GetClipTypeInfo(IMiniClip clip)
        {
            var clipTypeName = clip.GetType().Name;
            
            // Try to extract useful info from clip type
            if (clipTypeName.Contains("Signal"))
            {
                return "Signal Clip (Zero Duration)";
            }
            else if (clipTypeName.Contains("Movement"))
            {
                return "Movement Clip";
            }
            else if (clipTypeName.Contains("Anim"))
            {
                return "Animation Clip";
            }
            else if (clipTypeName.Contains("Audio"))
            {
                return "Audio Clip";
            }
            else if (clipTypeName.Contains("Morph"))
            {
                return "Morph Clip";
            }
            else if (clipTypeName.Contains("Expression"))
            {
                return "Expression Clip";
            }
            else if (clipTypeName.Contains("Wardrobe"))
            {
                return "Wardrobe Clip";
            }
            
            return clipTypeName;
        }
        
        #endregion
    }
}