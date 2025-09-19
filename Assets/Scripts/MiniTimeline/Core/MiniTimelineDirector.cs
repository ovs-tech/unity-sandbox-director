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
        private bool isDirty = true;
        
        // Project data
        private MiniTimelineProject project;
        private BindingContext bindingContext;
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
        public BindingContext BindingContext => bindingContext;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            bindingContext = new BindingContext();
        }
        
        private void Start()
        {
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
                    }
                    else
                    {
                        newTime = length;
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
            Evaluate(true); // Force evaluation for scrubbing
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Seeked to time {currentTime:F2}");
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
            if (project == null || tracks.Count == 0) return;
            
            // Rebuild tracks if dirty
            if (isDirty)
            {
                BuildTracks();
                isDirty = false;
            }
            
            // Sort tracks by evaluation order and evaluate
            tempTrackList.Clear();
            tempTrackList.AddRange(tracks.Where(t => t.Enabled));
            tempTrackList.Sort((a, b) => a.Order.CompareTo(b.Order));
            
            foreach (var track in tempTrackList)
            {
                try
                {
                    track.Evaluate(currentTime, scrub);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiniTimelineDirector] Error evaluating track '{track.Id}': {e.Message}");
                }
            }
            
            tempTrackList.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Build tracks from project data
        /// </summary>
        private void BuildTracks()
        {
            if (project == null) return;
            
            // Clear existing tracks
            foreach (var track in tracks)
            {
                track.OnProjectClosed();
            }
            tracks.Clear();
            trackLookup.Clear();
            
            // Create tracks from project data
            foreach (var trackData in project.tracks)
            {
                var track = CreateTrack(trackData);
                if (track != null)
                {
                    tracks.Add(track);
                    trackLookup[track.Id] = track;
                    
                    // Bind and prepare track
                    track.Bind(bindingContext);
                    track.Prepare();
                }
            }
            
            if (debugMode)
                Debug.Log($"[MiniTimelineDirector] Built {tracks.Count} tracks");
        }
        
        /// <summary>
        /// Create a track instance from track data
        /// </summary>
        /// <param name="data">Track data</param>
        /// <returns>Created track or null if type not supported</returns>
        private IMiniTrack CreateTrack(TrackData data)
        {
            return MiniTimeline.Serialization.TrackFactory.CreateTrack(data);
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