using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniTimeline.Core
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
    public class MiniTimelineDirector : MonoBehaviour
    {
        [Header("Timeline Settings")]
        [SerializeField] private float length = 10f;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool playOnAwake = false;
        [SerializeField] private bool autoCreateEmptyProject = false;
        
        [Header("Auto-Create Project Settings")]
        [SerializeField] private string defaultProjectName = "New Timeline Project";
        [SerializeField] private float defaultProjectLength = 10f;
        [SerializeField] private float defaultFrameRate = 30f;
        
        [Header("Binding")]
        [SerializeField] private BindingContext bindingContext;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        // Events
        public event Action<float> OnTimeChanged;
        public event Action<PlaybackState> OnStateChanged;
        public event Action OnProjectLoaded;
        public event Action OnProjectClosed;
        
        // State
        private PlaybackState state = PlaybackState.Stopped;
        private float currentTime = 0f;
        private float previousTime = 0f;
        private bool isDirty = false;
        
        // Project data
        private MiniTimelineProject project;
        private List<IMiniTrack> tracks = new List<IMiniTrack>();
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
        /// All tracks (read-only)
        /// </summary>
        public IReadOnlyList<IMiniTrack> Tracks => tracks;
        
        /// <summary>
        /// Current project
        /// </summary>
        public MiniTimelineProject Project => project;
        
        /// <summary>
        /// Binding context
        /// </summary>
        public BindingContext BindingContext
        {
            get => bindingContext;
            set => bindingContext = value;
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
            if (bindingContext == null)
            {
                bindingContext = GetComponent<BindingContext>();
                
                if (bindingContext == null)
                {
                    bindingContext = gameObject.AddComponent<BindingContext>();
                    if (debugMode)
                        Debug.Log("[MiniTimelineDirector] Auto-created BindingContext component");
                }
            }
        }
        
        private void Start()
        {
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
                
                if (debugMode && UnityEngine.Time.frameCount % 30 == 0) // Log every 30 frames to avoid spam
                    Debug.Log($"[MiniTimelineDirector] Update - deltaTime: {deltaTime:F4}, currentTime: {currentTime:F3}, newTime: {newTime:F3}, length: {length:F3}");
                
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
            string projectsPath = System.IO.Path.Combine(Application.persistentDataPath, "TimelineProjects");
            
            // Ensure directory exists
            if (!System.IO.Directory.Exists(projectsPath))
            {
                System.IO.Directory.CreateDirectory(projectsPath);
                Debug.Log($"[MiniTimelineDirector] Created projects folder: {projectsPath}");
            }
            
            return projectsPath;
        }
        
        /// <summary>
        /// Get full path for a project file
        /// </summary>
        /// <param name="projectName">Name of the project (without extension)</param>
        /// <returns>Full file path</returns>
        public static string GetProjectFilePath(string projectName)
        {
            return System.IO.Path.Combine(GetProjectsFolder(), projectName + ".json");
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
                    tracks = new System.Collections.Generic.List<TrackData>()
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
                
                string filePath = GetProjectFilePath(saveName);
                
                // Save with runtime tracks
                bool success = Serialization.ProjectSerializer.SaveToFile(project, this, filePath);
                
                if (success && debugMode)
                    Debug.Log($"[MiniTimelineDirector] Saved project '{saveName}' to: {filePath}");
                
                return success;
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
                string filePath = GetProjectFilePath(projectName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogWarning($"[MiniTimelineDirector] Project file not found: {filePath}");
                    return false;
                }
                
                var loadedProject = Serialization.ProjectSerializer.LoadFromFile(filePath);
                
                if (loadedProject != null)
                {
                    SetProject(loadedProject);
                    
                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Loaded project '{projectName}' from: {filePath}");
                    
                    return true;
                }
                
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
                string projectsFolder = GetProjectsFolder();
                
                if (!System.IO.Directory.Exists(projectsFolder))
                {
                    return new string[0];
                }
                
                var jsonFiles = System.IO.Directory.GetFiles(projectsFolder, "*.json");
                var projectNames = new string[jsonFiles.Length];
                
                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    projectNames[i] = System.IO.Path.GetFileNameWithoutExtension(jsonFiles[i]);
                }
                
                return projectNames;
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
                string filePath = GetProjectFilePath(projectName);
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Debug.Log($"[MiniTimelineDirector] Deleted project: {filePath}");
                    return true;
                }
                
                Debug.LogWarning($"[MiniTimelineDirector] Project file not found: {filePath}");
                return false;
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
            string filePath = GetProjectFilePath(projectName);
            return System.IO.File.Exists(filePath);
        }
        
        /// <summary>
        /// Load a timeline project
        /// </summary>
        /// <param name="newProject">Project data to load</param>
        /// <param name="context">Binding context (optional, will create new if null)</param>
        public void SetProject(MiniTimelineProject newProject, BindingContext context = null)
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
                bindingContext = context;
            
            // Build tracks from project data
            BuildTracks();
            
            // Reset state
            Stop();
            isDirty = false;
            
            OnProjectLoaded?.Invoke();
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Loaded project '{project.name}' with {tracks.Count} tracks");
        }
        
        /// <summary>
        /// Close current project and cleanup
        /// </summary>
        public void CloseProject()
        {
            if (project == null) return;
            
            Stop();
            
            // Cleanup all tracks
            foreach (var track in tracks)
            {
                track.OnProjectClosed();
            }
            
            tracks.Clear();
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
                tracks = new System.Collections.Generic.List<TrackData>()
            };
            
            SetProject(emptyProject);
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Auto-created empty project '{emptyProject.name}' with length {emptyProject.length}s");
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
            return tracks.OfType<T>().FirstOrDefault();
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
            return tracks.OfType<T>();
        }
        
        #endregion
        
        #region Evaluation
        
        /// <summary>
        /// Evaluate all tracks at current time
        /// </summary>
        /// <param name="scrub">Whether this is a scrub operation</param>
        private void Evaluate(bool scrub)
        {
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Starting evaluation at time {currentTime:F3}, scrub: {scrub}, project: {(project != null ? project.name : "null")}, tracks: {tracks.Count}");
            
            if (project == null || tracks.Count == 0)
            {
                if (debugMode)
                    Debug.LogWarning($"[MiniTimelineDirector] Cannot evaluate - project: {(project != null ? "loaded" : "null")}, track count: {tracks.Count}");
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
            tempTrackList.AddRange(tracks.Where(t => t.Enabled));
            tempTrackList.Sort((a, b) => a.Order.CompareTo(b.Order));
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Evaluating {tempTrackList.Count} enabled tracks out of {tracks.Count} total tracks");
            
            int successCount = 0;
            int errorCount = 0;
            
            foreach (var track in tempTrackList)
            {
                try
                {
                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Evaluating track '{track.Id}' (Order: {track.Order}, Type: {track.GetType().Name})");
                    
                    track.Evaluate(currentTime, scrub);
                    successCount++;
                    
                    if (debugMode)
                        Debug.Log($"[MiniTimelineDirector] Successfully evaluated track '{track.Id}'");
                }
                catch (Exception e)
                {
                    errorCount++;
                    Debug.LogError($"[MiniTimelineDirector] Error evaluating track '{track.Id}': {e.Message}\nStackTrace: {e.StackTrace}");
                }
            }
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Evaluation complete - Success: {successCount}, Errors: {errorCount}");
            
            tempTrackList.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Build tracks from project data
        /// </summary>
        private void BuildTracks()
        {
            if (project == null)
            {
                if (debugMode)
                    Debug.LogWarning("[MiniTimelineDirector] Cannot build tracks - no project loaded");
                return;
            }
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Building tracks from project '{project.name}' with {project.tracks.Count} track definitions");
            
            // Clear existing tracks
            foreach (var track in tracks)
            {
                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Cleaning up existing track '{track.Id}'");
                track.OnProjectClosed();
            }
            tracks.Clear();
            trackLookup.Clear();
            
            int successCount = 0;
            int failureCount = 0;
            
            // Create tracks from project data
            foreach (var trackData in project.tracks)
            {
                if (debugMode)
                    Debug.Log($"[MiniTimelineDirector] Creating track '{trackData.id}' of type '{trackData.type}'");
                
                var track = CreateTrack(trackData);
                if (track != null)
                {
                    tracks.Add(track);
                    trackLookup[track.Id] = track;
                    
                    try
                    {
                        // Bind and prepare track
                        if (debugMode)
                            Debug.Log($"[MiniTimelineDirector] Binding track '{track.Id}' with bind key '{track.BindKey}'");
                        track.Bind(bindingContext);
                        
                        if (debugMode)
                            Debug.Log($"[MiniTimelineDirector] Preparing track '{track.Id}'");
                        track.Prepare();
                        
                        successCount++;
                        if (debugMode)
                            Debug.Log($"[MiniTimelineDirector] Successfully created and prepared track '{track.Id}'");
                    }
                    catch (Exception e)
                    {
                        failureCount++;
                        Debug.LogError($"[MiniTimelineDirector] Failed to bind/prepare track '{track.Id}': {e.Message}\nStackTrace: {e.StackTrace}");
                        
                        // Remove failed track
                        tracks.Remove(track);
                        trackLookup.Remove(track.Id);
                    }
                }
                else
                {
                    failureCount++;
                    Debug.LogError($"[MiniTimelineDirector] Failed to create track '{trackData.id}' of type '{trackData.type}'");
                }
            }
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Track building complete - Success: {successCount}, Failures: {failureCount}, Total tracks: {tracks.Count}");
        }
        
        /// <summary>
        /// Create a track instance from track data
        /// </summary>
        /// <param name="data">Track data</param>
        /// <returns>Created track or null if type not supported</returns>
        private IMiniTrack CreateTrack(TrackData data)
        {
            return Serialization.TrackFactory.CreateTrack(data);
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