using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Systems.Persistence;
using Systems.Persistence.Core;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Playback state of the timeline
    /// </summary>
    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused
    }

    /// <summary>
    /// Main director class that manages Mini Timeline playback
    /// Controls time, state, and coordinates all tracks
    /// </summary>
    public class MiniTimelineDirector : MonoBehaviour, ISubsystemPersistence
    {
        [Header("Timeline Settings")]
        [SerializeField] private float length = 10f;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool playOnAwake = false;
        [SerializeField] private bool autoLoadFirstProject = false;
        [SerializeField] private bool autoCreateEmptyProject = false;

        [Header("Auto-Create Project Settings")]
        [SerializeField] private string defaultProjectName = "New Timeline Project";
        [SerializeField] private float defaultProjectLength = 10f;
        [SerializeField] private float defaultFrameRate = 30f;

        [Header("Binding")]
        [SerializeField] private BindableObjectManager bindableObjectManager;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Events
        public event Action<float> OnTimeChanged;
        public event Action<PlaybackState> OnStateChanged;
        public event Action OnProjectLoaded;
        public event Action OnProjectClosed;
        
        // Track events
        public event Action<IMiniTrack> OnTrackAdded;
        public event Action<string> OnTrackRemoved;
        public event Action<IMiniTrack> OnTrackUpdated;
        
        // Clip events
        public event Action<IMiniClip, string> OnClipAdded; // clip, trackId
        public event Action<string, string> OnClipRemoved; // clipId, trackId
        public event Action<IMiniClip, string> OnClipUpdated; // clip, trackId

        // State
        private PlaybackState state = PlaybackState.Stopped;
        private float currentTime = 0f;
        private float previousTime = 0f;
        private bool isDirty = false;

        // Project data
        private MiniTimelineProject project;
        private Dictionary<string, IMiniTrack> trackLookup = new Dictionary<string, IMiniTrack>();

        // Performance optimization
        private static readonly List<IMiniTrack> tempTrackList = new List<IMiniTrack>();

        #region Properties

        /// <summary>
        /// Total length of timeline in seconds
        /// </summary>
        public float Length
        {
            get => length;
            set
            {
                if (Math.Abs(length - value) > 0.001f)
                {
                    length = Mathf.Max(0.1f, value);
                    if (currentTime > length)
                    {
                        Seek(length);
                    }
                }
            }
        }

        /// <summary>
        /// Current playback time in seconds
        /// </summary>
        public float Time
        {
            get => currentTime;
            private set
            {
                previousTime = currentTime;
                currentTime = Mathf.Clamp(value, 0f, length);
                OnTimeChanged?.Invoke(currentTime);
            }
        }

        /// <summary>
        /// Previous frame time (for event detection)
        /// </summary>
        public float PreviousTime => previousTime;

        /// <summary>
        /// Playback speed multiplier
        /// </summary>
        public float PlaybackSpeed
        {
            get => playbackSpeed;
            set => playbackSpeed = Mathf.Clamp(value, 0.1f, 2f);
        }

        /// <summary>
        /// Whether timeline loops
        /// </summary>
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        /// <summary>
        /// Current playback state
        /// </summary>
        public PlaybackState State
        {
            get => state;
            private set
            {
                if (state != value)
                {
                    state = value;
                    OnStateChanged?.Invoke(state);
                }
            }
        }

        /// <summary>
        /// Whether timeline is currently playing
        /// </summary>
        public bool IsPlaying => state == PlaybackState.Playing;

        /// <summary>
        /// All tracks (read-only) - directly from project.tracks
        /// </summary>
        public IReadOnlyList<IMiniTrack> Tracks => (IReadOnlyList<IMiniTrack>)(project?.tracks ?? new List<IMiniTrack>());

        /// <summary>
        /// Current project
        /// </summary>
        public MiniTimelineProject Project => project;

        /// <summary>
        /// Binding context
        /// </summary>
        public BindableObjectManager BindingContext
        {
            get => bindableObjectManager;
            set => bindableObjectManager = value;
        }

        /// <summary>
        /// Whether to auto-load the first available project on Start if no project is loaded
        /// </summary>
        public bool AutoLoadFirstProject
        {
            get => autoLoadFirstProject;
            set => autoLoadFirstProject = value;
        }

        /// <summary>
        /// Whether to auto-create an empty project on Start if no project is loaded
        /// </summary>
        public bool AutoCreateEmptyProject
        {
            get => autoCreateEmptyProject;
            set => autoCreateEmptyProject = value;
        }

        /// <summary>
        /// Default name for auto-created projects
        /// </summary>
        public string DefaultProjectName
        {
            get => defaultProjectName;
            set => defaultProjectName = value;
        }

        /// <summary>
        /// Default length for auto-created projects
        /// </summary>
        public float DefaultProjectLength
        {
            get => defaultProjectLength;
            set => defaultProjectLength = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Default frame rate for auto-created projects
        /// </summary>
        public float DefaultFrameRate
        {
            get => defaultFrameRate;
            set => defaultFrameRate = Mathf.Max(1f, value);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // If no BindingContext is assigned, try to find one or create one
            if (bindableObjectManager == null)
            {
                bindableObjectManager = GetComponent<BindableObjectManager>();

                if (bindableObjectManager == null)
                {
                    bindableObjectManager = gameObject.AddComponent<BindableObjectManager>();
                    if (debugMode)
                        Debug.Log("[MiniTimelineDirector] Auto-created BindingContext component");
                }
            }
        }

        private void Start()
        {
            // Auto-load first project if enabled and no project is loaded
            if (autoLoadFirstProject && project == null)
            {
                TryAutoLoadFirstProject();
            }
            
            // Auto-create empty project if enabled and no project is loaded
            if (autoCreateEmptyProject && project == null)
            {
                CreateDefaultEmptyProject();
            }

            if (playOnAwake && project != null)
            {
                Play();
            }
        }

        private void Update()
        {
            if (IsPlaying)
            {
                // Advance time
                float deltaTime = UnityEngine.Time.deltaTime * playbackSpeed;
                float newTime = currentTime + deltaTime;

                // Handle looping or stopping at end
                if (newTime >= length)
                {
                    if (loop)
                    {
                        newTime = newTime % length;
                        if (debugMode)
                            Debug.Log($"[MiniTimelineDirector] Looped to time {newTime:F3}");
                    }
                    else
                    {
                        newTime = length;
                        if (debugMode)
                            Debug.Log($"[MiniTimelineDirector] Reached end, stopping at time {newTime:F3}");
                        Stop();
                    }
                }

                Time = newTime;
                Evaluate(false);
            }
        }

        private void OnDestroy()
        {
            CloseProject();
        }

        private void OnEnable()
        {
            try
            {
                var mgr = GamePersistenceManager.Instance;
                if (mgr != null)
                {
                    mgr.RegisterSubsystem(this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MiniTimelineDirector] Failed to register with GamePersistenceManager: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                var mgr = GamePersistenceManager.Instance;
                if (mgr != null)
                {
                    mgr.UnregisterSubsystem(this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MiniTimelineDirector] Failed to unregister from GamePersistenceManager: {ex.Message}");
            }
        }

        #endregion

        #region Playback Control

        /// <summary>
        /// Start or resume playback
        /// </summary>
        public void Play()
        {
            if (project == null)
            {
                Debug.LogWarning("[MiniTimelineDirector] Cannot play without a loaded project");
                return;
            }

            if (currentTime >= length && !loop)
            {
                Seek(0f);
            }

            State = PlaybackState.Playing;

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Playing from time {currentTime:F2}");
        }

        /// <summary>
        /// Pause playback
        /// </summary>
        public void Pause()
        {
            State = PlaybackState.Paused;

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Paused at time {currentTime:F2}");
        }

        /// <summary>
        /// Stop playback and reset to beginning
        /// </summary>
        public void Stop()
        {
            State = PlaybackState.Stopped;
            Seek(0f);

            if (debugMode)
                Debug.Log("[MiniTimelineDirector] Stopped");
        }

        /// <summary>
        /// Seek to specific time
        /// </summary>
        /// <param name="time">Target time in seconds</param>
        public void Seek(float time)
        {
            Time = time;
        }

        /// <summary>
        /// Jump to normalized position (0-1)
        /// </summary>
        /// <param name="normalizedTime">Normalized time (0-1)</param>
        public void SeekNormalized(float normalizedTime)
        {
            Seek(normalizedTime * length);
        }

        #endregion

        #region Project Management

        /// <summary>
        /// Get the projects folder path (uses Application.persistentDataPath)
        /// </summary>
        /// <returns>Full path to projects folder</returns>
        public static string GetProjectsFolder()
        {
            // Use persistence data service root path exclusively — no fallback to Application paths
            var mgr = GamePersistenceManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[MiniTimelineDirector] GamePersistenceManager not available - cannot determine projects folder");
                return null;
            }

            // Request the data service root path scoped to the MiniTimeline namespace
            var serviceRoot = mgr.GetDataServiceRootPath("MiniTimelineProject");
            if (string.IsNullOrEmpty(serviceRoot))
            {
                Debug.LogError("[MiniTimelineDirector] Data service root path not available - cannot determine projects folder");
                return null;
            }

            if (!Directory.Exists(serviceRoot))
            {
                Directory.CreateDirectory(serviceRoot);
                Debug.Log($"[MiniTimelineDirector] Created projects folder (from data service): {serviceRoot}");
            }

            return serviceRoot;
        }

        /// <summary>
        /// Get full path for a project file
        /// </summary>
        /// <param name="projectName">Name of the project (without extension)</param>
        /// <returns>Full file path</returns>
        public static string GetProjectFilePath(string projectName)
        {
            // Require the persistence data service root path; no fallback.
            var mgr = GamePersistenceManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[MiniTimelineDirector] GamePersistenceManager not available - cannot build project file path");
                return null;
            }

            var serviceRoot = mgr.GetDataServiceRootPath("MiniTimelineProject");
            if (string.IsNullOrEmpty(serviceRoot))
            {
                Debug.LogError("[MiniTimelineDirector] Data service root path not available - cannot build project file path");
                return null;
            }

            return Path.Combine(serviceRoot, projectName + ".json");
        }

        /// <summary>
        /// Create a new empty project
        /// </summary>
        /// <param name="projectName">Name for the new project</param>
        /// <param name="length">Length in seconds</param>
        /// <param name="frameRate">Frame rate</param>
        /// <returns>True if successful</returns>
        public bool CreateNewProject(string projectName, float length = 10f, float frameRate = 30f)
        {
            try
            {
                var newProject = new MiniTimelineProject
                {
                    name = projectName,
                    version = 1,
                    length = length,
                    frameRate = frameRate,
                    tracks = new List<IMiniTrack>()
                };

                SetProject(newProject);

                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Created new project '{projectName}' with length {length}s");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to create new project: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save current project to persistent data path
        /// </summary>
        /// <param name="projectName">Name to save as (optional, uses current project name if null)</param>
        /// <returns>True if successful</returns>
        public bool SaveProject(string projectName = null)
        {
            if (project == null)
            {
                Debug.LogWarning("[MiniTimelineDirector] Cannot save: No project loaded");
                return false;
            }

            try
            {
                // Use provided name or current project name
                string saveName = string.IsNullOrEmpty(projectName) ? project.name : projectName;

                // Update project name if changed
                if (projectName != null && projectName != project.name)
                {
                    project.name = projectName;
                }

                // Prefer persistence subsystem when available
                var mgr = GamePersistenceManager.Instance;
                if (mgr == null)
                {
                    Debug.LogError("[MiniTimelineDirector] GamePersistenceManager not available - cannot save project");
                    return false;
                }

                mgr.SaveFile(project, saveName, "MiniTimelineProject", true);
                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Saved project '{saveName}' via GamePersistenceManager");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to save project: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a project from persistent data path
        /// </summary>
        /// <param name="projectName">Name of the project to load (without extension)</param>
        /// <returns>True if successful</returns>
        public bool LoadProject(string projectName)
        {
            try
            {
                var mgr = GamePersistenceManager.Instance;
                if (mgr == null)
                {
                    Debug.LogError("[MiniTimelineDirector] GamePersistenceManager not available - cannot load project");
                    return false;
                }

                var files = mgr.ListFiles(projectName);
                if (files == null || !System.Linq.Enumerable.Contains(files, "MiniTimelineProject"))
                {
                    Debug.LogWarning($"[MiniTimelineDirector] Persistence save '{projectName}' does not contain a MiniTimelineProject file");
                    return false;
                }

                var loadedProject = mgr.LoadFile<MiniTimelineProject>(projectName, "MiniTimelineProject");
                if (loadedProject != null)
                {
                    SetProject(loadedProject);
                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Loaded project '{projectName}' via GamePersistenceManager");
                    return true;
                }

                Debug.LogWarning($"[MiniTimelineDirector] Failed to load MiniTimelineProject from persistence save '{projectName}'");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to load project: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of all available project names
        /// </summary>
        /// <returns>Array of project names (without extension)</returns>
        public static string[] GetAvailableProjects()
        {
            try
            {
                var mgr = GamePersistenceManager.Instance;
                if (mgr == null)
                {
                    Debug.LogWarning("[MiniTimelineDirector] GamePersistenceManager not available - no projects listed");
                    return new string[0];
                }

                var saves = mgr.ListSaves();
                var list = new System.Collections.Generic.List<string>();
                if (saves != null)
                {
                    foreach (var s in saves)
                    {
                        try
                        {
                            var files = mgr.ListFiles(s);
                            if (files != null && System.Linq.Enumerable.Contains(files, "MiniTimelineProject"))
                            {
                                list.Add(s);
                            }
                        }
                        catch { }
                    }
                }

                return list.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to get available projects: {e.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Delete a project file
        /// </summary>
        /// <param name="projectName">Name of the project to delete (without extension)</param>
        /// <returns>True if successful</returns>
        public static bool DeleteProject(string projectName)
        {
            try
            {
                var mgr = GamePersistenceManager.Instance;
                if (mgr == null)
                {
                    Debug.LogError("[MiniTimelineDirector] GamePersistenceManager not available - cannot delete project");
                    return false;
                }

                try
                {
                    mgr.DeleteGame(projectName);
                    Debug.Log($"[MiniTimelineDirector] Deleted persistence save: {projectName}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MiniTimelineDirector] Failed to delete persistence save '{projectName}': {ex.Message}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to delete project: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a project exists
        /// </summary>
        /// <param name="projectName">Name of the project (without extension)</param>
        /// <returns>True if project file exists</returns>
        public static bool ProjectExists(string projectName)
        {
            var mgr = GamePersistenceManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[MiniTimelineDirector] GamePersistenceManager not available - cannot determine project existence");
                return false;
            }

            try
            {
                var files = mgr.ListFiles(projectName);
                if (files != null && System.Linq.Enumerable.Contains(files, "MiniTimelineProject")) return true;
            }
            catch { }

            return false;
        }

        // ISubsystemPersistence implementation
        public string Namespace => "MiniTimelineProject";

        public object GetSaveData()
        {
            if (project == null) return null;
            return project;
        }

        public void LoadData(object data)
        {
            if (data is MiniTimelineProject proj)
            {
                SetProject(proj);
            }
            else
            {
                Debug.LogWarning($"[MiniTimelineDirector] LoadData received unexpected type: {data?.GetType()}");
            }
        }

        public Type DataType => typeof(MiniTimelineProject);

        /// <summary>
        /// Load a timeline project
        /// </summary>
        /// <param name="newProject">Project data to load</param>
        /// <param name="context">Binding context (optional, will create new if null)</param>
        public void SetProject(MiniTimelineProject newProject, BindableObjectManager context = null)
        {
            if (newProject == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot set null project");
                return;
            }

            // Close existing project
            CloseProject();

            // Set new project
            project = newProject;
            length = project.length;

            if (context != null)
                bindableObjectManager = context;

            // Ensure bindableObjectManager is initialized before building tracks
            if (bindableObjectManager == null)
            {
                bindableObjectManager = GetComponent<BindableObjectManager>();
                if (bindableObjectManager == null)
                {
                    bindableObjectManager = gameObject.AddComponent<BindableObjectManager>();
                    if (debugMode)
                        Debug.Log("[MiniTimelineDirector] Auto-created BindingContext component in SetProject");
                }
            }

            // Build tracks from project data
            BuildTracks();

            // Reset state
            Stop();
            isDirty = false;

            OnProjectLoaded?.Invoke();

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Loaded project '{project.name}' with {(project?.tracks?.Count ?? 0)} tracks");
        }

        /// <summary>
        /// Close current project and cleanup
        /// </summary>
        public void CloseProject()
        {
            if (project == null) return;

            Stop();

            // Cleanup all tracks
            foreach (var track in (project?.tracks ?? System.Linq.Enumerable.Empty<IMiniTrack>()).ToList())
            {
                try { track.OnProjectClosed(); } catch { }
            }

            project?.tracks?.Clear();
            trackLookup.Clear();
            project = null;

            OnProjectClosed?.Invoke();

            if (debugMode)
                Debug.Log("[MiniTimelineDirector] Project closed");
        }

        /// <summary>
        /// Mark project as dirty (needs rebuild)
        /// </summary>
        public void MarkDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// Create a default empty project with configured settings
        /// </summary>
        public void CreateDefaultEmptyProject()
        {
            var emptyProject = new MiniTimelineProject
            {
                name = defaultProjectName,
                version = 1,
                length = defaultProjectLength,
                frameRate = defaultFrameRate,
                tracks = new System.Collections.Generic.List<IMiniTrack>()
            };

            SetProject(emptyProject);

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Auto-created empty project '{emptyProject.name}' with length {emptyProject.length}s");
        }

        /// <summary>
        /// Attempt to automatically load the first available project
        /// </summary>
        private void TryAutoLoadFirstProject()
        {
            var availableProjects = GetAvailableProjects();
            
            if (availableProjects != null && availableProjects.Length > 0)
            {
                var firstProjectName = availableProjects[0];
                
                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Auto-loading first project: {firstProjectName}");
                
                if (LoadProject(firstProjectName))
                {
                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Successfully auto-loaded project: {firstProjectName}");
                }
                else
                {
                    Debug.LogWarning($"[MiniTimelineDirector] Failed to auto-load project: {firstProjectName}");
                }
            }
            else
            {
                if (debugMode)
                    Debug.Log("[MiniTimelineDirector] No projects available to auto-load");
            }
        }

        #endregion

        #region Track Management

        /// <summary>
        /// Get track by ID
        /// </summary>
        /// <param name="id">Track ID</param>
        /// <returns>Track or null if not found</returns>
        public IMiniTrack GetTrack(string id)
        {
            trackLookup.TryGetValue(id, out var track);
            return track;
        }

        /// <summary>
        /// Get track by type
        /// </summary>
        /// <typeparam name="T">Track type</typeparam>
        /// <returns>First track of type T or null</returns>
        public T GetTrack<T>() where T : class, IMiniTrack
        {
            return (project?.tracks ?? System.Linq.Enumerable.Empty<IMiniTrack>()).OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Get track by ID and type
        /// </summary>
        /// <typeparam name="T">Track type</typeparam>
        /// <param name="id">Track ID</param>
        /// <returns>Track of type T or null</returns>
        public T GetTrack<T>(string id) where T : class, IMiniTrack
        {
            return GetTrack(id) as T;
        }

        /// <summary>
        /// Get all tracks of specific type
        /// </summary>
        /// <typeparam name="T">Track type</typeparam>
        /// <returns>Collection of tracks</returns>
        public IEnumerable<T> GetTracks<T>() where T : class, IMiniTrack
        {
            return (project?.tracks ?? System.Linq.Enumerable.Empty<IMiniTrack>()).OfType<T>();
        }

        /// <summary>
        /// Add an existing track to the project
        /// </summary>
        /// <param name="track">Track instance to add</param>
        /// <returns>True if successful</returns>
        public bool AddTrack(IMiniTrack track)
        {
            if (project == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot add track - no project loaded");
                return false;
            }

            if (track == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot add null track");
                return false;
            }

            if (trackLookup.ContainsKey(track.Id))
            {
                Debug.LogWarning($"[MiniTimelineDirector] Track with ID '{track.Id}' already exists");
                return false;
            }

            try
            {
                // Add to project runtime list
                project.tracks.Add(track);
                trackLookup[track.Id] = track;

                // Bind and prepare
                track.Bind(bindableObjectManager);
                track.Prepare();

                // Publish event
                OnTrackAdded?.Invoke(track);

                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Added track '{track.Id}' of type '{track.GetType().Name}'");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to add track '{track.Id}': {e.Message}");
                // Rollback
                var existing = project.tracks.FirstOrDefault(t => t != null && t.Id == track.Id);
                if (existing != null)
                    project.tracks.Remove(existing);
                trackLookup.Remove(track.Id);
                return false;
            }
        }

        /// <summary>
        /// Remove a track from the project
        /// </summary>
        /// <param name="trackId">Track ID to remove</param>
        /// <returns>True if successful</returns>
        public bool RemoveTrack(string trackId)
        {
            if (project == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot remove track - no project loaded");
                return false;
            }

            if (!trackLookup.TryGetValue(trackId, out var track))
            {
                Debug.LogWarning($"[MiniTimelineDirector] Track '{trackId}' not found");
                return false;
            }

            try
            {
                // Remove from project runtime list
                var existing = project.tracks.FirstOrDefault(t => t != null && t.Id == trackId);
                if (existing != null)
                {
                    project.tracks.Remove(existing);
                }

                // Remove from runtime
                trackLookup.Remove(trackId);

                // Cleanup
                track.OnProjectClosed();

                // Publish event
                OnTrackRemoved?.Invoke(trackId);

                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Removed track '{trackId}'");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to remove track '{trackId}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update track enabled state
        /// </summary>
        /// <param name="trackId">Track ID to update</param>
        /// <param name="enabled">Enable/disable track</param>
        /// <returns>True if successful</returns>
        public bool UpdateTrack(string trackId, IMiniTrack trackData)
        {
            if (trackData == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot update track - null track data provided");
                return false;
            }

            if (!trackLookup.TryGetValue(trackId, out var track))
            {
                Debug.LogWarning($"[MiniTimelineDirector] Track '{trackId}' not found");
                return false;
            }

            track.Enabled = trackData.Enabled;
            track.BindKey = trackData.BindKey;
            track.Name = trackData.Name;
            track.Order = trackData.Order;

            track.Bind(bindableObjectManager);

            // Publish event
            OnTrackUpdated?.Invoke(track);

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Updated track '{trackId}'");

            return true;
        }

        #endregion

        #region Clip Management

        /// <summary>
        /// Get clip by ID and track ID
        /// </summary>
        /// <param name="clipId">Clip ID</param>
        /// <param name="trackId">Track ID</param>
        /// <returns>Clip or null if not found</returns>
        public IMiniClip GetClip(string clipId, string trackId)
        {
            if (!trackLookup.TryGetValue(trackId, out var track))
            {
                return null;
            }

            var clips = track.GetClips();
            return clips?.FirstOrDefault(c => c.Id == clipId);
        }

        /// <summary>
        /// Add an existing clip to a track
        /// </summary>
        /// <param name="clip">Clip instance to add</param>
        /// <param name="trackId">Track to add clip to</param>
        /// <returns>True if successful</returns>
        public bool AddClip(IMiniClip clip, string trackId)
        {
            if (project == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot add clip - no project loaded");
                return false;
            }

            if (clip == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot add null clip");
                return false;
            }

            if (!trackLookup.TryGetValue(trackId, out var track))
            {
                Debug.LogWarning($"[MiniTimelineDirector] Track '{trackId}' not found");
                return false;
            }

            try
            {
                // Add clip to track
                track.AddClip(clip);

                // Publish event
                OnClipAdded?.Invoke(clip, trackId);

                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Added clip '{clip.Id}' to track '{trackId}'");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to add clip '{clip.Id}' to track '{trackId}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove a clip from a track
        /// </summary>
        /// <param name="clipId">Clip ID to remove</param>
        /// <param name="trackId">Track ID</param>
        /// <returns>True if successful</returns>
        public bool RemoveClip(string clipId, string trackId)
        {
            if (project == null)
            {
                Debug.LogError("[MiniTimelineDirector] Cannot remove clip - no project loaded");
                return false;
            }

            if (!trackLookup.TryGetValue(trackId, out var track))
            {
                Debug.LogWarning($"[MiniTimelineDirector] Track '{trackId}' not found");
                return false;
            }

            try
            {
                var clip = GetClip(clipId, trackId);
                if (clip == null)
                {
                    Debug.LogWarning($"[MiniTimelineDirector] Clip '{clipId}' not found in track '{trackId}'");
                    return false;
                }

                // Remove from track
                track.RemoveClip(clip);

                // Publish event
                OnClipRemoved?.Invoke(clipId, trackId);

                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Removed clip '{clipId}' from track '{trackId}'");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to remove clip '{clipId}' from track '{trackId}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update clip properties
        /// </summary>
        /// <param name="clipId">Clip ID to update</param>
        /// <param name="trackId">Track ID</param>
        /// <param name="startTime">New start time (optional)</param>
        /// <param name="duration">New duration (optional)</param>
        /// <returns>True if successful</returns>
        public bool UpdateClip(string clipId, string trackId, float? startTime = null, float? duration = null)
        {
            var clip = GetClip(clipId, trackId);
            if (clip == null)
            {
                Debug.LogWarning($"[MiniTimelineDirector] Clip '{clipId}' not found in track '{trackId}'");
                return false;
            }

            try
            {
                // Update runtime clip via concrete base type
                var clipBase = clip as MiniClipBase;
                if (clipBase != null)
                {
                    if (startTime.HasValue)
                        clipBase.Start = startTime.Value;
                    if (duration.HasValue)
                        clipBase.Duration = duration.Value;
                }

                // Publish event
                OnClipUpdated?.Invoke(clip, trackId);

                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Updated clip '{clipId}' in track '{trackId}'");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniTimelineDirector] Failed to update clip '{clipId}' in track '{trackId}': {e.Message}");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Evaluate all tracks at current time
        /// </summary>
        /// <param name="scrub">Whether this is a scrub operation</param>
        private void Evaluate(bool scrub)
        {
            // Randomly log (5% chance) to reduce spam
            bool shouldLog = debugMode && UnityEngine.Random.value < 0.05f;

            if (project == null || project.tracks == null || project.tracks.Count == 0)
            {
                if (debugMode && UnityEngine.Random.value < 0.1f) // 10% chance for warnings
                    Debug.LogWarning($"[MiniTimelineDirector] Cannot evaluate - project: {(project != null ? "loaded" : "null")}, track count: {(project?.tracks?.Count ?? 0)}");
                return;
            }

            // Rebuild tracks if dirty
            if (isDirty)
            {
                if (debugMode)
                    Debug.Log("[MiniTimelineDirector] Tracks are dirty, rebuilding...");
                BuildTracks();
                isDirty = false;
            }

            // Sort tracks by evaluation order and evaluate
            tempTrackList.Clear();
            tempTrackList.AddRange((project.tracks ?? System.Linq.Enumerable.Empty<IMiniTrack>()).Where(t => t.Enabled));
            tempTrackList.Sort((a, b) => a.Order.CompareTo(b.Order));

            if (shouldLog)
                Debug.Log($"[MiniTimelineDirector] Evaluating {tempTrackList.Count} enabled tracks out of {(project?.tracks?.Count ?? 0)} total tracks");

            int successCount = 0;
            int errorCount = 0;

            foreach (var track in tempTrackList)
            {
                try
                {
                    if (shouldLog)
                        Debug.Log($"[MiniTimelineDirector] Evaluating track '{track.Id}' (Order: {track.Order}, Type: {track.GetType().Name})");

                    track.Evaluate(currentTime, scrub);
                    successCount++;
                }
                catch (Exception e)
                {
                    errorCount++;
                    Debug.LogError($"[MiniTimelineDirector] Error evaluating track '{track.Id}': {e.Message}\nStackTrace: {e.StackTrace}");
                }
            }

            if (shouldLog)
                Debug.Log($"[MiniTimelineDirector] Evaluation complete - Success: {successCount}, Errors: {errorCount}");

            tempTrackList.Clear();
        }

        #region Private Methods

        /// <summary>
        /// Build tracks from project runtime instances
        /// </summary>
        private void BuildTracks()
        {
            if (project == null)
            {
                if (debugMode)
                    Debug.LogWarning("[MiniTimelineDirector] Cannot build tracks - no project loaded");
                return;
            }

            // Ensure tracks list is initialized
            if (project.tracks == null)
            {
                project.tracks = new List<IMiniTrack>();
                if (debugMode)
                    Debug.LogWarning("[MiniTimelineDirector] Project tracks list was null, initialized empty list");
            }

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Building tracks from project '{project.name}' with {project.tracks.Count} runtime tracks");

            // Clear existing runtime state (use lookup to cleanup previous runtime instances)
            foreach (var existing in trackLookup.Values.ToList())
            {
                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Cleaning up existing track '{existing.Id}'");
                existing.OnProjectClosed();
            }
            trackLookup.Clear();

            int successCount = 0;
            int failureCount = 0;

            // Use runtime tracks directly
            foreach (var runtimeTrack in project.tracks)
            {
                if (runtimeTrack == null)
                {
                    failureCount++;
                    Debug.LogWarning("[MiniTimelineDirector] Null track found in project runtime list");
                    continue;
                }

                trackLookup[runtimeTrack.Id] = runtimeTrack;

                try
                {
                    // Bind and prepare track
                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Binding track '{runtimeTrack.Id}' with bind key '{runtimeTrack.BindKey}'");
                    runtimeTrack.Bind(bindableObjectManager);

                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Preparing track '{runtimeTrack.Id}'");
                    runtimeTrack.Prepare();

                    successCount++;
                }
                catch (Exception e)
                {
                    failureCount++;
                    Debug.LogError($"[MiniTimelineDirector] Failed to bind/prepare track '{runtimeTrack.Id}': {e.Message}\nStackTrace: {e.StackTrace}");

                    // Remove failed track
                    trackLookup.Remove(runtimeTrack.Id);
                }
            }

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Track building complete - Success: {successCount}, Failures: {failureCount}, Total tracks: {(project?.tracks?.Count ?? 0)}");
        }

        // No factory-based creation; project contains runtime tracks.
        
        /// <summary>
        /// Rebind all tracks to the current binding context.
        /// Useful when bindings have changed and tracks need to re-resolve their bound objects.
        /// This will also re-prepare tracks if their target object has changed.
        /// </summary>
        public void RebindAllTracks()
        {
            if (bindableObjectManager == null)
            {
                if (debugMode)
                    Debug.LogWarning("[MiniTimelineDirector] Cannot rebind tracks - no binding context available");
                return;
            }

            int reboundCount = 0;
            int repreparedCount = 0;

            foreach (var track in (project?.tracks ?? System.Linq.Enumerable.Empty<IMiniTrack>()))
            {
                try
                {
                    track.Bind(bindableObjectManager);
                    reboundCount++;
                    track.Prepare();
                    repreparedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiniTimelineDirector] Failed to rebind/prepare track '{track.Id}': {e.Message}");
                }
            }

            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Rebound {reboundCount} tracks; reprepared {repreparedCount}");
        }

        public void AutoBindSceneObjects()
        {
            if (bindableObjectManager == null)
            {
                if (debugMode)
                    Debug.LogWarning("[MiniTimelineDirector] Cannot auto-bind scene - no binding context available");
                return;
            }

            bindableObjectManager.AutoBind();

            if (debugMode)
                Debug.Log("[MiniTimelineDirector] Auto-bound scene objects to binding context");

            // Rebind all tracks after auto-binding
            RebindAllTracks();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Convert time to frame number
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <returns>Frame number</returns>
        public int TimeToFrame(float time)
        {
            float frameRate = project?.frameRate ?? MiniTimelineConstants.DEFAULT_FRAME_RATE;
            return Mathf.RoundToInt(time * frameRate);
        }

        /// <summary>
        /// Convert frame number to time
        /// </summary>
        /// <param name="frame">Frame number</param>
        /// <returns>Time in seconds</returns>
        public float FrameToTime(int frame)
        {
            float frameRate = project?.frameRate ?? MiniTimelineConstants.DEFAULT_FRAME_RATE;
            return frame / frameRate;
        }

        /// <summary>
        /// Snap time to nearest frame
        /// </summary>
        /// <param name="time">Input time</param>
        /// <returns>Snapped time</returns>
        public float SnapToFrame(float time)
        {
            return FrameToTime(TimeToFrame(time));
        }

        #endregion
    }
}