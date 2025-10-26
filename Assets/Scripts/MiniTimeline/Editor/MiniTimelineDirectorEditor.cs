#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MiniTimeline.Core;
using MiniTimeline.Serialization;

namespace MiniTimeline.Editor
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
        private SerializedProperty autoCreateEmptyProjectProp;
        private SerializedProperty defaultProjectNameProp;
        private SerializedProperty defaultProjectLengthProp;
        private SerializedProperty defaultFrameRateProp;
        private SerializedProperty bindingContextProp;
        
        // Project management
        private string newProjectName = "New Timeline Project";
        private float newProjectLength = 10f;
        private float newProjectFrameRate = 30f;
        private string lastLoadedPath = "";
        private string lastSavedPath = "";
        
        // Binding management
        private string newBindingKey = "";
        private UnityEngine.Object newBindingObject = null;
        
        // Foldouts
        private bool showTimelineSettings = true;
        private bool showProjectManagement = true;
        private bool showBindingContext = false;
        private bool showPlaybackControls = false;
        private bool showTrackInfo = false;
        private bool showDebugInfo = false;
        
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
            autoCreateEmptyProjectProp = serializedObject.FindProperty("autoCreateEmptyProject");
            defaultProjectNameProp = serializedObject.FindProperty("defaultProjectName");
            defaultProjectLengthProp = serializedObject.FindProperty("defaultProjectLength");
            defaultFrameRateProp = serializedObject.FindProperty("defaultFrameRate");
            bindingContextProp = serializedObject.FindProperty("bindingContext");
            
            // Setup event handlers for play mode updates
            if (Application.isPlaying)
            {
                EditorApplication.update += Repaint;
            }
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
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
            
            serializedObject.ApplyModifiedProperties();
            
            // Auto-repaint during playback for real-time updates
            if (Application.isPlaying && director.IsPlaying)
            {
                Repaint();
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
                
                // Auto-create project settings
                EditorGUILayout.LabelField("Auto-Create Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(autoCreateEmptyProjectProp, new GUIContent("Auto Create Empty Project", "Automatically create an empty project on Start if no project is loaded"));
                
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
            
            // BindingContext field
            EditorGUILayout.PropertyField(bindingContextProp, new GUIContent("Binding Context", "BindingContext component to use for object binding. Leave empty to auto-create."));
            
            // Helper text
            if (bindingContextProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No BindingContext assigned. One will be auto-created on the same GameObject when the scene starts.", MessageType.Info);
                
                // Button to manually create one
                if (GUILayout.Button("Create BindingContext Component", buttonStyle))
                {
                    CreateBindingContextComponent();
                }
            }
            else
            {
                var context = bindingContextProp.objectReferenceValue as BindingContext;
                if (context != null)
                {
                    EditorGUILayout.HelpBox($"Using BindingContext from '{context.gameObject.name}'", MessageType.None);
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
                
                // Current project info
                if (director.Project != null)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Current Project", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Name:", director.Project.name);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Version:", director.Project.version.ToString());
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Frame Rate:", director.Project.frameRate.ToString("F1") + " FPS");
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Project Tracks:", director.Project.tracks.Count.ToString());
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Runtime Tracks:", director.Tracks.Count.ToString());
                    EditorGUILayout.EndHorizontal();
                    
                    // Show warning if mismatch
                    if (director.Project.tracks.Count != director.Tracks.Count)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.HelpBox($"Track count mismatch! Project has {director.Project.tracks.Count} track definitions but only {director.Tracks.Count} runtime tracks were created. This usually indicates a track creation failure.", MessageType.Warning);
                        
                        if (GUILayout.Button("Rebuild Tracks", buttonStyle))
                        {
                            director.MarkDirty();
                            if (Application.isPlaying)
                            {
                                // Force evaluation to trigger rebuild
                                director.Seek(director.Time);
                            }
                            Debug.Log("[MiniTimelineDirectorEditor] Manually triggered track rebuild");
                        }
                    }
                    
                    if (GUILayout.Button("Close Project", buttonStyle))
                    {
                        CloseProject();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
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
                        
                        var keysToRemove = new System.Collections.Generic.List<string>();
                        var bindingsToAdd = new System.Collections.Generic.List<(string oldKey, string newKey, UnityEngine.Object obj)>();
                        
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
                                director.BindingContext.Bind(key, newObj);
                            }
                        }
                        
                        // Apply changes
                        foreach (var keyToRemove in keysToRemove)
                        {
                            director.BindingContext.Unbind(keyToRemove);
                        }
                        
                        foreach (var (oldKey, newKey, obj) in bindingsToAdd)
                        {
                            if (!bindingKeys.Contains(newKey)) // Avoid duplicate keys
                            {
                                director.BindingContext.Bind(newKey, obj);
                            }
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
                            director.BindingContext.Bind(newBindingKey, newBindingObject);
                            newBindingKey = "";
                            newBindingObject = null;
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
            showTrackInfo = EditorGUILayout.Foldout(showTrackInfo, $"Track Information ({director.Tracks.Count} tracks)", true);
            
            if (showTrackInfo)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                // Add Track Button
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Track Management", EditorStyles.boldLabel);
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
                    var tracksToRemove = new System.Collections.Generic.List<IMiniTrack>();
                    
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
                        bool isBound = IsTrackBound(track);
                        var bindColor = isBound ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.5f, 0.2f);
                        GUI.color = bindColor;
                        var bindIcon = isBound ? "🔗" : "⚠";
                        EditorGUILayout.LabelField(bindIcon, GUILayout.Width(20));
                        GUI.color = oldColor;
                        
                        EditorGUILayout.LabelField($"{track.Id} ({track.GetType().Name})", EditorStyles.boldLabel);
                        
                        GUILayout.FlexibleSpace();
                        
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
                        
                        var availableKeys = new System.Collections.Generic.List<string> { "None" };
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
                        
                        // Binding status details
                        EditorGUILayout.BeginHorizontal();
                        var boundObject = GetBoundObject(track);
                        
                        if (isBound && boundObject != null)
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
                        
                        // Enable/Disable toggle
                        EditorGUILayout.BeginHorizontal();
                        var newEnabled = EditorGUILayout.Toggle("Enabled", track.Enabled);
                        if (newEnabled != track.Enabled)
                        {
                            UpdateTrackEnabled(track, newEnabled);
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // Clip management section
                        var clipsProperty = track.GetType().GetProperty("Clips");
                        var clipsValue = clipsProperty?.GetValue(track);
                        
                        if (clipsValue is System.Collections.ICollection clips)
                        {
                            EditorGUILayout.Space(5);
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"Clips ({clips.Count})", EditorStyles.boldLabel);
                            
                            if (GUILayout.Button("+ Add Clip", EditorStyles.miniButton, GUILayout.Width(80)))
                            {
                                ShowAddClipMenu(track);
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            // Display clips
                            if (clips.Count > 0)
                            {
                                EditorGUI.indentLevel++;
                                
                                var clipsList = track.GetClips().ToList();
                                var clipsToRemove = new System.Collections.Generic.List<IMiniClip>();
                                
                                foreach (var clip in clipsList)
                                {
                                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                    
                                    // Clip header with delete button
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField($"📌 {clip.Id}", EditorStyles.miniLabel);
                                    
                                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                                    if (GUILayout.Button("×", GUILayout.Width(18), GUILayout.Height(18)))
                                    {
                                        if (EditorUtility.DisplayDialog("Delete Clip", 
                                            $"Delete clip '{clip.Id}'?", "Delete", "Cancel"))
                                        {
                                            clipsToRemove.Add(clip);
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                    EditorGUILayout.EndHorizontal();
                                    
                                    // Clip properties
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField("Start:", GUILayout.Width(40));
                                    var newStart = EditorGUILayout.FloatField(clip.Start, GUILayout.Width(60));
                                    if (!Mathf.Approximately(newStart, clip.Start))
                                    {
                                        UpdateClipStart(track, clip, newStart);
                                    }
                                    
                                    EditorGUILayout.LabelField("Duration:", GUILayout.Width(60));
                                    var newDuration = EditorGUILayout.FloatField(clip.Duration, GUILayout.Width(60));
                                    if (!Mathf.Approximately(newDuration, clip.Duration))
                                    {
                                        UpdateClipDuration(track, clip, Mathf.Max(0.1f, newDuration));
                                    }
                                    EditorGUILayout.EndHorizontal();
                                    
                                    EditorGUILayout.EndVertical();
                                }
                                
                                // Remove clips marked for deletion
                                foreach (var clip in clipsToRemove)
                                {
                                    RemoveClip(track, clip);
                                }
                                
                                EditorGUI.indentLevel--;
                            }
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
                            var trackData = director.Project.tracks[i];
                            var runtimeTrack = director.Tracks.FirstOrDefault(t => t.Id == trackData.id);
                            
                            var status = runtimeTrack != null ? "✓ Created" : "✗ Failed";
                            var statusColor = runtimeTrack != null ? Color.green : Color.red;
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"[{i}] {trackData.id} ({trackData.type}):", GUILayout.Width(200));
                            
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
            var project = new MiniTimelineProject
            {
                name = newProjectName,
                version = 1,
                length = newProjectLength,
                frameRate = newProjectFrameRate,
                tracks = new System.Collections.Generic.List<TrackData>()
            };
            
            director.SetProject(project);
            
            Debug.Log($"[MiniTimelineDirectorEditor] Created new project '{newProjectName}' with length {newProjectLength}s");
        }
        
        private void CreateSampleProject()
        {
            var project = SampleProjectCreator.CreateSampleProject();
            project.name = newProjectName + " (Sample)";
            project.length = newProjectLength;
            project.frameRate = newProjectFrameRate;
            
            director.SetProject(project);
            
            Debug.Log($"[MiniTimelineDirectorEditor] Created sample project '{project.name}'");
        }
        
        private void SaveProject()
        {
            if (director.Project == null)
            {
                EditorUtility.DisplayDialog("No Project", "No project loaded to save.", "OK");
                return;
            }
            
            var path = EditorUtility.SaveFilePanel(
                "Save Timeline Project",
                Application.dataPath,
                director.Project.name,
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                if (ProjectSerializer.SaveToFile(director.Project, path))
                {
                    lastSavedPath = path;
                    Debug.Log($"[MiniTimelineDirectorEditor] Project saved to: {path}");
                    EditorUtility.DisplayDialog("Save Successful", $"Project saved to:\n{path}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Save Failed", "Failed to save project. Check console for details.", "OK");
                }
            }
        }
        
        private void LoadProject()
        {
            var path = EditorUtility.OpenFilePanel(
                "Load Timeline Project",
                Application.dataPath,
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                var project = ProjectSerializer.LoadFromFile(path);
                if (project != null)
                {
                    director.SetProject(project);
                    lastLoadedPath = path;
                    Debug.Log($"[MiniTimelineDirectorEditor] Project loaded from: {path}");
                    EditorUtility.DisplayDialog("Load Successful", $"Project '{project.name}' loaded successfully.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Failed", "Failed to load project. Check console for details.", "OK");
                }
            }
        }
        
        private void QuickSave()
        {
            if (director.Project != null && !string.IsNullOrEmpty(lastSavedPath))
            {
                if (ProjectSerializer.SaveToFile(director.Project, lastSavedPath))
                {
                    Debug.Log($"[MiniTimelineDirectorEditor] Quick saved to: {lastSavedPath}");
                }
            }
        }
        
        private void QuickLoad()
        {
            if (!string.IsNullOrEmpty(lastLoadedPath))
            {
                var project = ProjectSerializer.LoadFromFile(lastLoadedPath);
                if (project != null)
                {
                    director.SetProject(project);
                    Debug.Log($"[MiniTimelineDirectorEditor] Quick loaded from: {lastLoadedPath}");
                }
            }
        }
        
        private void CloseProject()
        {
            if (EditorUtility.DisplayDialog("Close Project", "Are you sure you want to close the current project?", "Yes", "Cancel"))
            {
                director.CloseProject();
                Debug.Log("[MiniTimelineDirectorEditor] Project closed");
            }
        }
        
        #endregion
        
        #region Binding Management Methods
        
        private void CreateBindingContextComponent()
        {
            if (director == null) return;
            
            // Check if one already exists on the GameObject
            var existingContext = director.GetComponent<BindingContext>();
            if (existingContext != null)
            {
                // Assign it to the director
                director.BindingContext = existingContext;
                EditorUtility.SetDirty(director);
                Debug.Log("[MiniTimelineDirectorEditor] Found and assigned existing BindingContext component");
            }
            else
            {
                // Create new BindingContext component
                var newContext = director.gameObject.AddComponent<BindingContext>();
                director.BindingContext = newContext;
                EditorUtility.SetDirty(director);
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
            
            // Use BindingContext.AutoBind() to discover and bind all BindableObject components
            var existingBindingsCount = director.BindingContext.GetKeys().Count();
            
            director.BindingContext.AutoBind();
            
            var newBindingsCount = director.BindingContext.GetKeys().Count();
            var bindingsAdded = newBindingsCount - existingBindingsCount;
            
            Debug.Log($"[MiniTimelineDirectorEditor] Auto-bound {bindingsAdded} BindableObject components (Total bindings: {newBindingsCount})");
            EditorUtility.DisplayDialog("Auto-Binding Complete", $"Successfully auto-bound {bindingsAdded} BindableObject components.\n\nTotal bindings: {newBindingsCount}\n\nTip: Add BindableObject component to GameObjects you want to bind.", "OK");
        }
        
        private void ClearAllBindings()
        {
            if (director.BindingContext == null)
            {
                EditorUtility.DisplayDialog("No Binding Context", "Binding context is null.", "OK");
                return;
            }
            
            var keys = director.BindingContext.GetKeys().ToList();
            foreach (var key in keys)
            {
                director.BindingContext.Unbind(key);
            }
            
            Debug.Log($"[MiniTimelineDirectorEditor] Cleared {keys.Count} bindings");
        }
        
        #endregion
        
        #region Track Management Methods
        
        private void ShowAddTrackMenu()
        {
            var menu = new GenericMenu();
            
            // Get all registered track types from TrackFactory
            var trackTypes = MiniTimeline.Serialization.TrackFactory.GetRegisteredTrackTypes();
            
            foreach (var trackType in trackTypes)
            {
                menu.AddItem(new GUIContent(GetFriendlyTrackName(trackType)), false, () => AddTrack(trackType));
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
            
            // Generate unique track ID
            var trackId = $"{trackType}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Create track data
            var trackData = new MiniTimeline.Core.TrackData
            {
                id = trackId,
                type = trackType,
                bindKey = null,
                enabled = true,
                order = director.Project.tracks.Count,
                clips = new System.Collections.Generic.List<MiniTimeline.Core.ClipData>(),
                properties = new System.Collections.Generic.Dictionary<string, object>()
            };
            
            // Add to project
            director.Project.tracks.Add(trackData);
            
            // Rebuild tracks
            director.MarkDirty();
            
            Debug.Log($"[MiniTimelineDirectorEditor] Added new {trackType} track with ID '{trackId}'");
            
            // Repaint to show changes
            Repaint();
        }
        
        private void RemoveTrack(IMiniTrack track)
        {
            if (director.Project == null || track == null)
            {
                return;
            }
            
            // Find and remove track data from project
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData != null)
            {
                director.Project.tracks.Remove(trackData);
                
                // Rebuild tracks
                director.MarkDirty();
                
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
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData != null)
            {
                trackData.bindKey = newBindKey;
                
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
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData != null)
            {
                trackData.enabled = enabled;
                
                // Update runtime track using reflection
                if (track.GetType().GetProperty("Enabled")?.CanWrite == true)
                {
                    track.GetType().GetProperty("Enabled").SetValue(track, enabled);
                }
                
                Debug.Log($"[MiniTimelineDirectorEditor] Updated track '{track.Id}' enabled to {enabled}");
            }
        }
        
        private bool IsTrackBound(IMiniTrack track)
        {
            if (track == null || director.BindingContext == null) return false;
            if (string.IsNullOrEmpty(track.BindKey)) return false;
            
            // Check if the binding key exists in the context
            return director.BindingContext.HasKey(track.BindKey);
        }
        
        private UnityEngine.Object GetBoundObject(IMiniTrack track)
        {
            if (track == null || director.BindingContext == null) return null;
            if (string.IsNullOrEmpty(track.BindKey)) return null;
            
            // Try to resolve the bound object
            UnityEngine.Object boundObj = null;
            director.BindingContext.TryResolve(track.BindKey, out boundObj);
            return boundObj;
        }
        
        #endregion
        
        #region Clip Management Methods
        
        private void ShowAddClipMenu(IMiniTrack track)
        {
            var menu = new GenericMenu();
            
            // Get track type to determine appropriate clip types
            var trackType = GetTrackType(track);
            
            menu.AddItem(new GUIContent("Simple Clip (Default)"), false, () => AddSimpleClip(track));
            
            // Add track-specific clip creation options based on track type
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_SIGNAL:
                    menu.AddItem(new GUIContent("Signal Clip"), false, () => AddSignalClip(track));
                    break;
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    menu.AddItem(new GUIContent("Movement Clip"), false, () => AddMovementClip(track));
                    break;
            }
            
            menu.ShowAsContext();
        }
        
        private string GetTrackType(IMiniTrack track)
        {
            if (director.Project == null || track == null) return "";
            
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            return trackData?.type ?? "";
        }
        
        private void AddSimpleClip(IMiniTrack track)
        {
            if (director.Project == null || track == null) return;
            
            // Generate unique clip ID
            var clipId = $"clip_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Find track data
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData == null) return;
            
            // Determine clip start time (after last clip or at 0)
            var startTime = 0f;
            if (trackData.clips.Count > 0)
            {
                var lastClip = trackData.clips.OrderByDescending(c => c.start + c.duration).First();
                startTime = lastClip.start + lastClip.duration;
            }
            
            // Create clip data
            var clipData = new MiniTimeline.Core.ClipData
            {
                id = clipId,
                start = startTime,
                duration = 1.0f,
                payload = new System.Collections.Generic.Dictionary<string, object>()
            };
            
            // Add to track data
            trackData.clips.Add(clipData);
            
            // Rebuild tracks to create runtime clip
            director.MarkDirty();
            
            Debug.Log($"[MiniTimelineDirectorEditor] Added simple clip '{clipId}' to track '{track.Id}' at {startTime}s");
            
            Repaint();
        }
        
        private void AddSignalClip(IMiniTrack track)
        {
            if (director.Project == null || track == null) return;
            
            var clipId = $"signal_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData == null) return;
            
            var startTime = 0f;
            if (trackData.clips.Count > 0)
            {
                startTime = trackData.clips.Max(c => c.start + c.duration);
            }
            
            var clipData = new MiniTimeline.Core.ClipData
            {
                id = clipId,
                start = startTime,
                duration = 0f, // Signals have zero duration
                payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "eventId", "NewEvent" },
                    { "payload", "" },
                    { "edge", "Start" },
                    { "fireOnScrub", false }
                }
            };
            
            trackData.clips.Add(clipData);
            director.MarkDirty();
            
            Debug.Log($"[MiniTimelineDirectorEditor] Added signal clip '{clipId}' to track '{track.Id}'");
            Repaint();
        }
        
        private void AddMovementClip(IMiniTrack track)
        {
            if (director.Project == null || track == null) return;
            
            var clipId = $"movement_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData == null) return;
            
            var startTime = 0f;
            if (trackData.clips.Count > 0)
            {
                startTime = trackData.clips.Max(c => c.start + c.duration);
            }
            
            var clipData = new MiniTimeline.Core.ClipData
            {
                id = clipId,
                start = startTime,
                duration = 2.0f,
                payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "hasPosition", true },
                    { "startPosition", "0,0,0" },
                    { "endPosition", "0,0,0" },
                    { "hasRotation", false },
                    { "hasFieldOfView", false },
                    { "fadeIn", 0.5f },
                    { "fadeOut", 0.5f }
                }
            };
            
            trackData.clips.Add(clipData);
            director.MarkDirty();
            
            Debug.Log($"[MiniTimelineDirectorEditor] Added movement clip '{clipId}' to track '{track.Id}'");
            Repaint();
        }
        
        private void RemoveClip(IMiniTrack track, IMiniClip clip)
        {
            if (director.Project == null || track == null || clip == null) return;
            
            // Find and remove from track data
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData != null)
            {
                var clipData = trackData.clips.FirstOrDefault(c => c.id == clip.Id);
                if (clipData != null)
                {
                    trackData.clips.Remove(clipData);
                    
                    // Rebuild tracks
                    director.MarkDirty();
                    
                    Debug.Log($"[MiniTimelineDirectorEditor] Removed clip '{clip.Id}' from track '{track.Id}'");
                    Repaint();
                }
            }
        }
        
        private void UpdateClipStart(IMiniTrack track, IMiniClip clip, float newStart)
        {
            if (director.Project == null || track == null || clip == null) return;
            
            // Update in clip data
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData != null)
            {
                var clipData = trackData.clips.FirstOrDefault(c => c.id == clip.Id);
                if (clipData != null)
                {
                    clipData.start = newStart;
                    
                    // Update runtime clip using reflection
                    var startProp = clip.GetType().GetProperty("Start");
                    if (startProp?.CanWrite == true)
                    {
                        startProp.SetValue(clip, newStart);
                    }
                    
                    Debug.Log($"[MiniTimelineDirectorEditor] Updated clip '{clip.Id}' start to {newStart}s");
                }
            }
        }
        
        private void UpdateClipDuration(IMiniTrack track, IMiniClip clip, float newDuration)
        {
            if (director.Project == null || track == null || clip == null) return;
            
            // Update in clip data
            var trackData = director.Project.tracks.FirstOrDefault(t => t.id == track.Id);
            if (trackData != null)
            {
                var clipData = trackData.clips.FirstOrDefault(c => c.id == clip.Id);
                if (clipData != null)
                {
                    clipData.duration = newDuration;
                    
                    // Update runtime clip using reflection
                    var durationProp = clip.GetType().GetProperty("Duration");
                    if (durationProp?.CanWrite == true)
                    {
                        durationProp.SetValue(clip, newDuration);
                    }
                    
                    Debug.Log($"[MiniTimelineDirectorEditor] Updated clip '{clip.Id}' duration to {newDuration}s");
                }
            }
        }
        
        #endregion
    }
}
#endif