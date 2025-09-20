using UnityEngine;
using UnityEngine.UI;
using MiniTimeline.Core;
using MiniTimeline.UI;

namespace MiniTimeline.Demo
{
    /// <summary>
    /// Demo setup script for the Mini Timeline Editor
    /// Creates a complete timeline editor UI with sample data
    /// </summary>
    public class TimelineEditorDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool createSampleProject = true;
        [SerializeField] private float sampleProjectLength = 30f;
        
        [Header("UI Canvas")]
        [SerializeField] private Canvas uiCanvas;
        
        // Components
        private MiniTimelineDirector director;
        private TimelineEditorUI editorUI;
        private GameObject editorUIObject;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            SetupTimelineEditor();
            
            if (createSampleProject)
            {
                CreateSampleProject();
            }
        }
        
        #endregion
        
        #region Setup
        
        private void SetupTimelineEditor()
        {
            // Find or create canvas
            if (uiCanvas == null)
            {
                uiCanvas = FindFirstObjectByType<Canvas>();
                if (uiCanvas == null)
                {
                    CreateUICanvas();
                }
            }
            
            // Find or create director
            director = FindFirstObjectByType<MiniTimelineDirector>();
            if (director == null)
            {
                CreateDirector();
            }
            
            // Create timeline editor UI
            CreateTimelineEditorUI();
        }
        
        private void CreateUICanvas()
        {
            var canvasGO = new GameObject("TimelineCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            
            uiCanvas = canvasGO.GetComponent<Canvas>();
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            
            // Setup canvas for screen space overlay
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100;
            
            // Setup scaler for mobile
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        
        private void CreateDirector()
        {
            var directorGO = new GameObject("MiniTimelineDirector", typeof(MiniTimelineDirector));
            director = directorGO.GetComponent<MiniTimelineDirector>();
        }
        
        private void CreateTimelineEditorUI()
        {
            // Create main editor UI GameObject
            editorUIObject = new GameObject("TimelineEditorUI", typeof(RectTransform), typeof(TimelineEditorUI));
            editorUIObject.transform.SetParent(uiCanvas.transform, false);
            
            editorUI = editorUIObject.GetComponent<TimelineEditorUI>();
            var rectTransform = editorUIObject.GetComponent<RectTransform>();
            
            // Setup full screen layout
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Create UI structure
            CreateEditorUIStructure();
            
            // Connect director to editor
            editorUI.SetDirector(director);
        }
        
        private void CreateEditorUIStructure()
        {
            var rectTransform = editorUIObject.GetComponent<RectTransform>();
            
            // Create main container with background
            var backgroundGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundGO.transform.SetParent(editorUIObject.transform, false);
            
            var bgRect = backgroundGO.GetComponent<RectTransform>();
            var bgImage = backgroundGO.GetComponent<Image>();
            
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            // Create toolbar area
            CreateToolbar();
            
            // Create timeline area
            CreateTimelineArea();
            
            // Create context menu
            CreateContextMenu();
        }
        
        private void CreateToolbar()
        {
            var toolbarGO = new GameObject("Toolbar", typeof(RectTransform), typeof(Image));
            toolbarGO.transform.SetParent(editorUIObject.transform, false);
            
            var toolbarRect = toolbarGO.GetComponent<RectTransform>();
            var toolbarImage = toolbarGO.GetComponent<Image>();
            
            // Position at top
            toolbarRect.anchorMin = new Vector2(0f, 1f);
            toolbarRect.anchorMax = new Vector2(1f, 1f);
            toolbarRect.sizeDelta = new Vector2(0f, 60f);
            toolbarRect.anchoredPosition = new Vector2(0f, -30f);
            
            toolbarImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create playback controls
            CreatePlaybackControls(toolbarGO);
            
            // Create time display
            CreateTimeDisplay(toolbarGO);
        }
        
        private void CreatePlaybackControls(GameObject parent)
        {
            var controlsGO = new GameObject("PlaybackControls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            controlsGO.transform.SetParent(parent.transform, false);
            
            var controlsRect = controlsGO.GetComponent<RectTransform>();
            var layoutGroup = controlsGO.GetComponent<HorizontalLayoutGroup>();
            
            controlsRect.anchorMin = new Vector2(0f, 0f);
            controlsRect.anchorMax = new Vector2(0f, 1f);
            controlsRect.sizeDelta = new Vector2(200f, 0f);
            controlsRect.anchoredPosition = new Vector2(100f, 0f);
            
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;
            
            // Create play button
            CreateButton(controlsGO, "Play", "▶", () => director?.Play());
            
            // Create pause button
            CreateButton(controlsGO, "Pause", "⏸", () => director?.Pause());
            
            // Create stop button
            CreateButton(controlsGO, "Stop", "⏹", () => director?.Stop());
        }
        
        private void CreateButton(GameObject parent, string name, string text, System.Action action)
        {
            var buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent.transform, false);
            
            var buttonImage = buttonGO.GetComponent<Image>();
            var button = buttonGO.GetComponent<Button>();
            
            buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            button.onClick.AddListener(() => action?.Invoke());
            
            // Add text
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.GetComponent<RectTransform>();
            var textComponent = textGO.GetComponent<Text>();
            
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateTimeDisplay(GameObject parent)
        {
            var timeDisplayGO = new GameObject("TimeDisplay", typeof(RectTransform), typeof(Text));
            timeDisplayGO.transform.SetParent(parent.transform, false);
            
            var timeRect = timeDisplayGO.GetComponent<RectTransform>();
            var timeText = timeDisplayGO.GetComponent<Text>();
            
            timeRect.anchorMin = new Vector2(1f, 0f);
            timeRect.anchorMax = new Vector2(1f, 1f);
            timeRect.sizeDelta = new Vector2(150f, 0f);
            timeRect.anchoredPosition = new Vector2(-75f, 0f);
            
            timeText.text = "00:00:00";
            timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText.fontSize = 14;
            timeText.color = Color.white;
            timeText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateTimelineArea()
        {
            var timelineAreaGO = new GameObject("TimelineArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            timelineAreaGO.transform.SetParent(editorUIObject.transform, false);
            
            var areaRect = timelineAreaGO.GetComponent<RectTransform>();
            var scrollRect = timelineAreaGO.GetComponent<ScrollRect>();
            var areaImage = timelineAreaGO.GetComponent<Image>();
            
            // Position below toolbar
            areaRect.anchorMin = new Vector2(0f, 0f);
            areaRect.anchorMax = new Vector2(1f, 1f);
            areaRect.offsetMin = new Vector2(0f, 0f);
            areaRect.offsetMax = new Vector2(0f, -60f); // Account for toolbar
            
            areaImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            // Setup scroll rect
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            // Create content area
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(timelineAreaGO.transform, false);
            
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.sizeDelta = new Vector2(2000f, 600f); // Will be adjusted by timeline
            
            scrollRect.content = contentRect;
            
            // Create ruler area
            CreateRulerArea(contentGO);
            
            // Create tracks area
            CreateTracksArea(contentGO);
        }
        
        private void CreateRulerArea(GameObject parent)
        {
            var rulerGO = new GameObject("Ruler", typeof(RectTransform), typeof(TimelineRuler));
            rulerGO.transform.SetParent(parent.transform, false);
            
            var rulerRect = rulerGO.GetComponent<RectTransform>();
            
            rulerRect.anchorMin = new Vector2(0f, 1f);
            rulerRect.anchorMax = new Vector2(1f, 1f);
            rulerRect.sizeDelta = new Vector2(0f, 40f);
            rulerRect.anchoredPosition = new Vector2(0f, -20f);
        }
        
        private void CreateTracksArea(GameObject parent)
        {
            var tracksGO = new GameObject("Tracks", typeof(RectTransform));
            tracksGO.transform.SetParent(parent.transform, false);
            
            var tracksRect = tracksGO.GetComponent<RectTransform>();
            
            tracksRect.anchorMin = new Vector2(0f, 0f);
            tracksRect.anchorMax = new Vector2(1f, 1f);
            tracksRect.offsetMin = new Vector2(0f, 0f);
            tracksRect.offsetMax = new Vector2(0f, -40f); // Account for ruler
        }
        
        private void CreateContextMenu()
        {
            var menuGO = new GameObject("ContextMenu", typeof(RectTransform), typeof(TimelineContextMenu));
            menuGO.transform.SetParent(editorUIObject.transform, false);
            
            var menuRect = menuGO.GetComponent<RectTransform>();
            var contextMenu = menuGO.GetComponent<TimelineContextMenu>();
            
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = Vector2.zero;
            menuRect.offsetMax = Vector2.zero;
            
            contextMenu.Initialize(editorUI);
        }
        
        #endregion
        
        #region Sample Project Creation
        
        private void CreateSampleProject()
        {
            var project = new MiniTimelineProject
            {
                name = "Demo Project",
                length = sampleProjectLength,
                frameRate = 30f
            };
            
            // Create sample tracks
            CreateSampleAnimTrack(project);
            CreateSampleMorphTrack(project);
            CreateSampleCameraTrack(project);
            CreateSampleAudioTrack(project);
            
            // Setup binding context
            var bindingContext = new BindingContext();
            SetupSampleBindings(bindingContext);
            
            // Load project
            director.SetProject(project, bindingContext);
            
            Debug.Log("Sample timeline project created!");
        }
        
        private void CreateSampleAnimTrack(MiniTimelineProject project)
        {
            var track = new TrackData
            {
                id = "anim_track_01",
                type = MiniTimelineConstants.TRACK_ANIM,
                bindKey = "character",
                enabled = true,
                order = 1
            };
            
            // Add sample clips
            track.clips.Add(new ClipData
            {
                id = "idle_clip",
                start = 0f,
                duration = 5f
            });
            
            track.clips.Add(new ClipData
            {
                id = "walk_clip", 
                start = 5f,
                duration = 10f
            });
            
            track.clips.Add(new ClipData
            {
                id = "run_clip",
                start = 15f,
                duration = 8f
            });
            
            project.tracks.Add(track);
        }
        
        private void CreateSampleMorphTrack(MiniTimelineProject project)
        {
            var track = new TrackData
            {
                id = "morph_track_01",
                type = MiniTimelineConstants.TRACK_MORPH,
                bindKey = "character",
                enabled = true,
                order = 2
            };
            
            // Add sample morph clips
            track.clips.Add(new ClipData
            {
                id = "smile_clip",
                start = 2f,
                duration = 3f
            });
            
            track.clips.Add(new ClipData
            {
                id = "blink_clip",
                start = 8f,
                duration = 0.5f
            });
            
            project.tracks.Add(track);
        }
        
        private void CreateSampleCameraTrack(MiniTimelineProject project)
        {
            var track = new TrackData
            {
                id = "camera_track_01",
                type = MiniTimelineConstants.TRACK_EVENT,
                bindKey = "main_camera",
                enabled = true,
                order = 3
            };
            
            // Add camera clips
            track.clips.Add(new ClipData
            {
                id = "wide_shot",
                start = 0f,
                duration = 12f
            });
            
            track.clips.Add(new ClipData
            {
                id = "close_up",
                start = 12f,
                duration = 8f
            });
            
            project.tracks.Add(track);
        }
        
        private void CreateSampleAudioTrack(MiniTimelineProject project)
        {
            var track = new TrackData
            {
                id = "audio_track_01",
                type = MiniTimelineConstants.TRACK_AUDIO,
                bindKey = "audio_source",
                enabled = true,
                order = 4
            };
            
            // Add audio clips
            track.clips.Add(new ClipData
            {
                id = "bgm_clip",
                start = 0f,
                duration = sampleProjectLength
            });
            
            project.tracks.Add(track);
        }
        
        private void SetupSampleBindings(BindingContext context)
        {
            // Bind sample objects (would normally be real GameObjects)
            context.Bind("character", gameObject); // Use this GameObject as placeholder
            context.Bind("main_camera", Camera.main?.gameObject);
            context.Bind("audio_source", GetComponent<AudioSource>());
        }
        
        #endregion
    }
}