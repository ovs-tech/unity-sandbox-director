using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MiniTimeline.Core;
using MiniTimeline.Tracks;

namespace MiniTimeline.Serialization
{
    /// <summary>
    /// Handles saving and loading of Mini Timeline projects to/from JSON
    /// Supports versioning and backwards compatibility
    /// </summary>
    public static class ProjectSerializer
    {
        /// <summary>
        /// Save a timeline project to JSON string
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <param name="prettyPrint">Whether to format JSON for readability</param>
        /// <returns>JSON string</returns>
        public static string SaveToJson(MiniTimelineProject project, bool prettyPrint = true)
        {
            try
            {
                var jsonProject = ConvertToJsonProject(project);
                return JsonUtility.ToJson(jsonProject, prettyPrint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error saving project to JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save a timeline project to file
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <param name="filePath">File path to save to</param>
        /// <returns>True if successful</returns>
        public static bool SaveToFile(MiniTimelineProject project, string filePath)
        {
            try
            {
                string json = SaveToJson(project);
                if (json != null)
                {
                    File.WriteAllText(filePath, json);
                    Debug.Log($"[ProjectSerializer] Saved project to: {filePath}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error saving project to file '{filePath}': {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// Load a timeline project from JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Loaded project or null if failed</returns>
        public static MiniTimelineProject LoadFromJson(string json)
        {
            try
            {
                var jsonProject = JsonUtility.FromJson<JsonTimelineProject>(json);
                if (jsonProject == null)
                {
                    Debug.LogError("[ProjectSerializer] Failed to parse JSON");
                    return null;
                }

                return ConvertFromJsonProject(jsonProject);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error loading project from JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load a timeline project from file
        /// </summary>
        /// <param name="filePath">File path to load from</param>
        /// <returns>Loaded project or null if failed</returns>
        public static MiniTimelineProject LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[ProjectSerializer] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var project = LoadFromJson(json);

                if (project != null)
                {
                    Debug.Log($"[ProjectSerializer] Loaded project from: {filePath}");
                }

                return project;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSerializer] Error loading project from file '{filePath}': {e.Message}");
                return null;
            }
        }

        #region Conversion Methods

        /// <summary>
        /// Convert MiniTimelineProject to JSON-serializable format
        /// </summary>
        private static JsonTimelineProject ConvertToJsonProject(MiniTimelineProject project)
        {
            var jsonProject = new JsonTimelineProject
            {
                version = project.version,
                name = project.name,
                length = project.length,
                frameRate = project.frameRate,
                tracks = new List<JsonTrackData>(),
                metadata = ConvertToJsonMetadata(project.metadata)
            };

            // Convert tracks
            foreach (var track in project.tracks)
            {
                var jsonTrack = ConvertToJsonTrack(track);
                if (jsonTrack != null)
                {
                    jsonProject.tracks.Add(jsonTrack);
                }
            }

            return jsonProject;
        }

        /// <summary>
        /// Convert JSON project back to MiniTimelineProject
        /// </summary>
        private static MiniTimelineProject ConvertFromJsonProject(JsonTimelineProject jsonProject)
        {
            // Handle version compatibility
            if (jsonProject.version > 1)
            {
                Debug.LogWarning($"[ProjectSerializer] Loading project with version {jsonProject.version}, current version is 1. Some features may not work correctly.");
            }

            var project = new MiniTimelineProject
            {
                version = jsonProject.version,
                name = jsonProject.name,
                length = jsonProject.length,
                frameRate = jsonProject.frameRate,
                tracks = new List<TrackData>(),
                metadata = ConvertFromJsonMetadata(jsonProject.metadata)
            };

            // Convert tracks
            foreach (var jsonTrack in jsonProject.tracks)
            {
                var track = ConvertFromJsonTrack(jsonTrack);
                if (track != null)
                {
                    project.tracks.Add(track);
                }
            }

            return project;
        }

        private static JsonTrackData ConvertToJsonTrack(TrackData track)
        {
            var jsonTrack = new JsonTrackData
            {
                id = track.id,
                type = track.type,
                bindKey = track.bindKey,
                enabled = track.enabled,
                order = track.order,
                clips = new List<JsonClipData>(),
                properties = track.properties
            };

            // Convert clips
            foreach (var clip in track.clips)
            {
                var jsonClip = ConvertToJsonClip(clip);
                if (jsonClip != null)
                {
                    jsonTrack.clips.Add(jsonClip);
                }
            }

            return jsonTrack;
        }

        private static TrackData ConvertFromJsonTrack(JsonTrackData jsonTrack)
        {
            return new TrackData
            {
                id = jsonTrack.id,
                type = jsonTrack.type,
                bindKey = jsonTrack.bindKey,
                enabled = jsonTrack.enabled,
                order = jsonTrack.order,
                clips = ConvertFromJsonClips(jsonTrack.clips),
                properties = jsonTrack.properties ?? new Dictionary<string, object>()
            };
        }

        private static JsonClipData ConvertToJsonClip(ClipData clip)
        {
            return new JsonClipData
            {
                id = clip.id,
                start = clip.start,
                duration = clip.duration,
                payload = clip.payload
            };
        }

        private static List<ClipData> ConvertFromJsonClips(List<JsonClipData> jsonClips)
        {
            var clips = new List<ClipData>();

            foreach (var jsonClip in jsonClips)
            {
                clips.Add(new ClipData
                {
                    id = jsonClip.id,
                    start = jsonClip.start,
                    duration = jsonClip.duration,
                    payload = jsonClip.payload ?? new Dictionary<string, object>()
                });
            }

            return clips;
        }

        private static JsonProjectMetadata ConvertToJsonMetadata(ProjectMetadata metadata)
        {
            return new JsonProjectMetadata
            {
                zoom = metadata.zoom,
                scrollPosition = metadata.scrollPosition,
                selection = metadata.selection,
                editorData = metadata.editorData
            };
        }

        private static ProjectMetadata ConvertFromJsonMetadata(JsonProjectMetadata jsonMetadata)
        {
            if (jsonMetadata == null)
            {
                return new ProjectMetadata();
            }

            return new ProjectMetadata
            {
                zoom = jsonMetadata.zoom,
                scrollPosition = jsonMetadata.scrollPosition,
                selection = jsonMetadata.selection ?? new List<string>(),
                editorData = jsonMetadata.editorData ?? new Dictionary<string, object>()
            };
        }

        #endregion

        #region JSON Data Classes

        [Serializable]
        private class JsonTimelineProject
        {
            public int version;
            public string name;
            public float length;
            public float frameRate;
            public List<JsonTrackData> tracks;
            public JsonProjectMetadata metadata;
        }

        [Serializable]
        private class JsonTrackData
        {
            public string id;
            public string type;
            public string bindKey;
            public bool enabled;
            public int order;
            public List<JsonClipData> clips;
            public Dictionary<string, object> properties;
        }

        [Serializable]
        private class JsonClipData
        {
            public string id;
            public float start;
            public float duration;
            public Dictionary<string, object> payload;
        }

        [Serializable]
        private class JsonProjectMetadata
        {
            public float zoom;
            public float scrollPosition;
            public List<string> selection;
            public Dictionary<string, object> editorData;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for creating sample projects
    /// </summary>
    public static class SampleProjectCreator
    {
        /// <summary>
        /// Create a sample project with basic tracks for testing
        /// </summary>
        /// <returns>Sample project</returns>
        public static MiniTimelineProject CreateSampleProject()
        {
            var project = new MiniTimelineProject
            {
                name = "Sample Project with Camera Animation",
                length = 12f, // Extended to accommodate camera clips
                frameRate = 30f
            };

            // Add animation track
            var animTrack = new TrackData
            {
                id = "anim_track_1",
                type = MiniTimelineConstants.TRACK_ANIM,
                bindKey = "character",
                enabled = true,
                order = 10
            };

            // Add sample animation clip
            var animClip = new ClipData
            {
                id = "anim_clip_1",
                start = 0f,
                duration = 3f,
                payload = new Dictionary<string, object>
                {
                    { "animationAsset", "addr:Animations/Run" },
                    { "speed", 1f },
                    { "wrapMode", "Loop" },
                    { "fadeIn", 0.2f },
                    { "fadeOut", 0.2f }
                }
            };
            animTrack.clips.Add(animClip);
            project.tracks.Add(animTrack);

            // Add camera track
            var movementTrack = new TrackData
            {
                id = "movement_track_1",
                type = MiniTimelineConstants.TRACK_MOVEMENT,
                bindKey = "main_camera",
                enabled = true,
                order = 20
            };

            // Add camera position movement
            var cameraPosClip = new ClipData
            {
                id = "camera_pos_clip_1",
                start = 0f,
                duration = 4f,
                payload = new Dictionary<string, object>
                {
                    { "hasPosition", true },
                    { "startPosition", "0,2,-5" },
                    { "endPosition", "5,3,-3" },
                    { "hasRotation", false },
                    { "hasFieldOfView", false },
                    { "animationCurve", "EaseInOut" },
                    { "fadeIn", 0.5f },
                    { "fadeOut", 0.5f }
                }
            };
            movementTrack.clips.Add(cameraPosClip);

            // Add camera rotation
            var cameraRotClip = new ClipData
            {
                id = "camera_rot_clip_1",
                start = 2f,
                duration = 3f,
                payload = new Dictionary<string, object>
                {
                    { "hasPosition", false },
                    { "hasRotation", true },
                    { "startRotation", "0,0,0,1" }, // Quaternion.identity
                    { "endRotation", "0.1305262,0.1305262,0,0.9829730" }, // Quaternion.Euler(15, 15, 0)
                    { "hasFieldOfView", false },
                    { "animationCurve", "Linear" }
                }
            };
            movementTrack.clips.Add(cameraRotClip);

            // Add field of view change (zoom effect)
            var cameraFOVClip = new ClipData
            {
                id = "camera_fov_clip_1",
                start = 5f,
                duration = 2f,
                payload = new Dictionary<string, object>
                {
                    { "hasPosition", false },
                    { "hasRotation", false },
                    { "hasFieldOfView", true },
                    { "startFieldOfView", 60f },
                    { "endFieldOfView", 30f }, // Zoom in effect
                    { "animationCurve", "EaseInOut" }
                }
            };
            movementTrack.clips.Add(cameraFOVClip);

            project.tracks.Add(movementTrack);

            // Add morph track
            var morphTrack = new TrackData
            {
                id = "morph_track_1",
                type = MiniTimelineConstants.TRACK_MORPH,
                bindKey = "character",
                enabled = true,
                order = 30
            };

            // Add sample morph clip
            var morphClip = new ClipData
            {
                id = "morph_clip_1",
                start = 1f,
                duration = 2f,
                payload = new Dictionary<string, object>
                {
                    { "blendMode", "Additive" },
                    { "weight", 1f },
                    { "keys", new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                { "id", "Smile" },
                                { "startValue", 0f },
                                { "endValue", 80f },
                                { "curve", CreateLinearCurveData() }
                            }
                        }
                    }
                }
            };
            morphTrack.clips.Add(morphClip);
            project.tracks.Add(morphTrack);

            // Add signal track
            var signalTrack = new TrackData
            {
                id = "signal_track_1",
                type = MiniTimelineConstants.TRACK_SIGNAL,
                bindKey = "",
                enabled = true,
                order = 0
            };

            // Add sample event
            var eventClip = new ClipData
            {
                id = "event_clip_1",
                start = 2.5f,
                duration = 0f,
                payload = new Dictionary<string, object>
                {
                    { "eventId", "StartSmile" },
                    { "payload", "Happy emotion" },
                    { "edge", "OnEnter" },
                    { "fireOnScrub", false }
                }
            };
            signalTrack.clips.Add(eventClip);
            project.tracks.Add(signalTrack);

            // Add animator track
            var animatorTrack = new TrackData
            {
                id = "animator_track_1",
                type = MiniTimelineConstants.TRACK_ANIMATOR,
                bindKey = "character", // Target GameObject with Animator
                enabled = true,
                order = 40
            };

            // Add sample animator parameter clips
            var animatorSpeedClip = new ClipData
            {
                id = "animator_speed_clip_1",
                start = 0f,
                duration = 3f,
                payload = new Dictionary<string, object>
                {
                    { "blendMode", "Override" },
                    { "fadeIn", 0.2f },
                    { "fadeOut", 0.2f },
                    { "parameterKeys", "Speed:Float:0:2:Linear" } // paramName:type:startValue:endValue:curveType
                }
            };
            animatorTrack.clips.Add(animatorSpeedClip);

            // Add sample boolean parameter clip
            var animatorDirectionClip = new ClipData
            {
                id = "animator_direction_clip_1",
                start = 0f,
                duration = 4f,
                payload = new Dictionary<string, object>
                {
                    { "blendMode", "Override" },
                    { "parameterKeys", "Direction:Float:0:1:Linear" }
                }
            };
            animatorTrack.clips.Add(animatorDirectionClip);

            project.tracks.Add(animatorTrack);

            var animator2Track = new TrackData
            {
                id = "animator_track_2",
                type = MiniTimelineConstants.TRACK_ANIMATOR,
                bindKey = "character_2", // Target GameObject with Animator
                enabled = true,
                order = 40
            };

            // Add sample animator parameter clips
            var animator2SpeedClip = new ClipData
            {
                id = "animator_speed_clip_2",
                start = 0f,
                duration = 3f,
                payload = new Dictionary<string, object>
                {
                    { "blendMode", "Override" },
                    { "fadeIn", 0.2f },
                    { "fadeOut", 0.2f },
                    { "parameterKeys", "Speed:Float:0:2:Linear" } // paramName:type:startValue:endValue:curveType
                }
            };
            animator2Track.clips.Add(animator2SpeedClip);
            // Add sample boolean parameter clip
            var animator2DirectionClip = new ClipData
            {
                id = "animator_direction_clip_2",
                start = 0f,
                duration = 4f,
                payload = new Dictionary<string, object>
                {
                    { "blendMode", "Override" },
                    { "parameterKeys", "Direction:Float:0:1:Linear" }
                }
            };
            animator2Track.clips.Add(animator2DirectionClip);

            project.tracks.Add(animator2Track);

            return project;
        }

        private static Dictionary<string, object> CreateLinearCurveData()
        {
            return new Dictionary<string, object>
            {
                { "keys", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "time", 0f },
                            { "value", 0f },
                            { "inTangent", 0f },
                            { "outTangent", 1f }
                        },
                        new Dictionary<string, object>
                        {
                            { "time", 1f },
                            { "value", 1f },
                            { "inTangent", 1f },
                            { "outTangent", 0f }
                        }
                    }
                }
            };
        }
    }
}