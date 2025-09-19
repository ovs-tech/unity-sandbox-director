using System.Linq;
using UnityEngine;
using MiniTimeline.Core;
using MiniTimeline.Serialization;
using MiniTimeline.Tracks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiniTimeline.Demo
{
    /// <summary>
    /// Demo script to showcase Mini Timeline functionality
    /// Creates a sample project and demonstrates playback controls
    /// </summary>
    public class MiniTimelineDemo : MonoBehaviour
    {
        [Header("Timeline Setup")]
        [SerializeField] private MiniTimelineDirector director;
        [SerializeField] private GameObject characterObject;
        [SerializeField] private Camera demoCamera;
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private bool loadSampleProject = true;
        
        [Header("Controls")]
        [SerializeField] private KeyCode playKey = KeyCode.Space;
        [SerializeField] private KeyCode stopKey = KeyCode.S;
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private KeyCode resetCameraKey = KeyCode.C;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGUI = true;
        [SerializeField] private bool showTrackInfo = false;
        
        private MiniTimelineProject currentProject;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float originalCameraFOV;
        
        void Start()
        {
            SetupDirector();
            
            if (loadSampleProject)
            {
                LoadSampleProject();
            }
            
            // Store original camera settings
            if (demoCamera != null)
            {
                originalCameraPosition = demoCamera.transform.position;
                originalCameraRotation = demoCamera.transform.rotation;
                originalCameraFOV = demoCamera.fieldOfView;
            }
            
            if (autoPlay && director.Project != null)
            {
                director.Play();
            }
        }
        
        void Update()
        {
            HandleInput();
        }
        
        void OnGUI()
        {
            if (!showDebugGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Mini Timeline Demo", GUI.skin.label);
            GUILayout.Space(10);
            
            // Project info
            if (director.Project != null)
            {
                GUILayout.Label($"Project: {director.Project.name}");
                GUILayout.Label($"Length: {director.Length:F2}s");
                GUILayout.Label($"Tracks: {director.Tracks.Count}");
                GUILayout.Space(5);
                
                // Playback info
                GUILayout.Label($"State: {director.State}");
                GUILayout.Label($"Time: {director.Time:F2}s");
                GUILayout.Label($"Speed: {director.PlaybackSpeed:F1}x");
                GUILayout.Space(5);
                
                // Time scrubber
                GUILayout.Label("Scrub Time:");
                float newTime = GUILayout.HorizontalSlider(director.Time, 0f, director.Length);
                if (!Mathf.Approximately(newTime, director.Time))
                {
                    director.Seek(newTime);
                }
                
                GUILayout.Space(10);
                
                // Control buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(director.IsPlaying ? "Pause" : "Play"))
                {
                    if (director.IsPlaying)
                        director.Pause();
                    else
                        director.Play();
                }
                
                if (GUILayout.Button("Stop"))
                {
                    director.Stop();
                }
                
                if (GUILayout.Button("Restart"))
                {
                    director.Stop();
                    director.Play();
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(10);
                
                // Speed control
                GUILayout.Label("Playback Speed:");
                director.PlaybackSpeed = GUILayout.HorizontalSlider(director.PlaybackSpeed, 0.1f, 2f);
                
                GUILayout.Space(5);
                
                // Loop toggle
                director.Loop = GUILayout.Toggle(director.Loop, "Loop");
                
                GUILayout.Space(10);
                
                // Camera controls
                if (demoCamera != null)
                {
                    GUILayout.Label("Camera Controls:");
                    if (GUILayout.Button("Reset Camera"))
                    {
                        ResetCamera();
                    }
                }
                
                // Track info toggle
                showTrackInfo = GUILayout.Toggle(showTrackInfo, "Show Track Info");
                
                if (showTrackInfo)
                {
                    GUILayout.Space(5);
                    ShowTrackInfo();
                }
            }
            else
            {
                GUILayout.Label("No project loaded");
                
                if (GUILayout.Button("Load Sample Project"))
                {
                    LoadSampleProject();
                }
            }
            
            GUILayout.Space(10);
            
            // File operations
            GUILayout.Label("File Operations:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                SaveProject();
            }
            if (GUILayout.Button("Load"))
            {
                LoadProject();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label($"Play/Pause: {playKey}");
            GUILayout.Label($"Stop: {stopKey}");
            GUILayout.Label($"Restart: {restartKey}");
            GUILayout.Label($"Reset Camera: {resetCameraKey}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void SetupDirector()
        {
            if (director == null)
            {
                director = FindFirstObjectByType<MiniTimelineDirector>();
                if (director == null)
                {
                    // Create director if none exists
                    var directorObject = new GameObject("Mini Timeline Director");
                    director = directorObject.AddComponent<MiniTimelineDirector>();
                }
            }
            
            // Setup binding context
            if (characterObject != null)
            {
                director.BindingContext.Bind("character", characterObject);
            }
            
            if (demoCamera != null)
            {
                director.BindingContext.Bind("main_camera", demoCamera);
            }
            else
            {
                // Use main camera as fallback
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    director.BindingContext.Bind("main_camera", mainCamera);
                    demoCamera = mainCamera;
                }
            }
            
            // Subscribe to events
            director.OnStateChanged += OnStateChanged;
            director.OnTimeChanged += OnTimeChanged;
            director.OnProjectLoaded += OnProjectLoaded;
        }
        
        private void LoadSampleProject()
        {
            currentProject = SampleProjectCreator.CreateSampleProject();
            director.SetProject(currentProject);
            
            Debug.Log("[MiniTimelineDemo] Loaded sample project");
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(playKey))
            {
                if (director.IsPlaying)
                    director.Pause();
                else
                    director.Play();
            }
            
            if (Input.GetKeyDown(stopKey))
            {
                director.Stop();
            }
            
            if (Input.GetKeyDown(restartKey))
            {
                director.Stop();
                director.Play();
            }
            
            if (Input.GetKeyDown(resetCameraKey))
            {
                ResetCamera();
            }
        }
        
        private void SaveProject()
        {
            if (currentProject == null)
            {
                Debug.LogWarning("[MiniTimelineDemo] No project to save");
                return;
            }
            
            string path = Application.persistentDataPath + "/sample_project.json";
            bool success = ProjectSerializer.SaveToFile(currentProject, path);
            
            if (success)
            {
                Debug.Log($"[MiniTimelineDemo] Project saved to: {path}");
            }
        }
        
        private void LoadProject()
        {
            string path = Application.persistentDataPath + "/sample_project.json";
            var project = ProjectSerializer.LoadFromFile(path);
            
            if (project != null)
            {
                currentProject = project;
                director.SetProject(project);
                Debug.Log($"[MiniTimelineDemo] Project loaded from: {path}");
            }
        }
        
        private void ResetCamera()
        {
            if (demoCamera != null)
            {
                demoCamera.transform.position = originalCameraPosition;
                demoCamera.transform.rotation = originalCameraRotation;
                demoCamera.fieldOfView = originalCameraFOV;
                Debug.Log("[MiniTimelineDemo] Camera reset to original position");
            }
        }
        
        private void ShowTrackInfo()
        {
            if (director.Project == null) return;
            
            foreach (var track in director.Tracks)
            {
                GUILayout.BeginHorizontal();
                
                // Track type and status
                string trackType = track.GetType().Name;
                string status = track.Enabled ? "✓" : "✗";
                GUILayout.Label($"{status} {trackType}", GUILayout.Width(100));
                
                // Clip count
                var clips = track.GetClips();
                int totalClips = clips.Count();
                int activeClips = clips.Count(clip => clip.Contains(director.Time));
                GUILayout.Label($"({activeClips}/{totalClips})", GUILayout.Width(60));
                
                GUILayout.EndHorizontal();
            }
        }
        
        #region Event Handlers
        
        private void OnStateChanged(PlaybackState state)
        {
            Debug.Log($"[MiniTimelineDemo] State changed to: {state}");
        }
        
        private void OnTimeChanged(float time)
        {
            // Optional: Handle time changes
        }
        
        private void OnProjectLoaded()
        {
            Debug.Log("[MiniTimelineDemo] Project loaded successfully");
            
            // Setup event handlers for the event track
            var eventTrack = director.GetTrack<EventTrack>();
            if (eventTrack != null)
            {
                eventTrack.OnTimelineEvent += OnTimelineEvent;
            }
            
            // Log track information
            foreach (var track in director.Tracks)
            {
                string trackType = track.GetType().Name;
                int clipCount = track.GetClips().Count();
                Debug.Log($"[MiniTimelineDemo] Loaded {trackType} with {clipCount} clips (BindKey: {track.BindKey})");
            }
        }
        
        private void OnTimelineEvent(TimelineEvent evt)
        {
            Debug.Log($"[MiniTimelineDemo] Timeline event: {evt.eventId} - {evt.payload} at {evt.time:F2}s");
            
            // Handle specific events
            switch (evt.eventId)
            {
                case "StartSmile":
                    Debug.Log("Character should start smiling!");
                    break;
                    
                // Add more event handlers as needed
            }
        }
        
        #endregion
        
        void OnDestroy()
        {
            // Cleanup event subscriptions
            if (director != null)
            {
                director.OnStateChanged -= OnStateChanged;
                director.OnTimeChanged -= OnTimeChanged;
                director.OnProjectLoaded -= OnProjectLoaded;
                
                var eventTrack = director.GetTrack<EventTrack>();
                if (eventTrack != null)
                {
                    eventTrack.OnTimelineEvent -= OnTimelineEvent;
                }
            }
        }
    }
}