using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Track for playing sub-timeline projects
    /// </summary>
    [Serializable]
    public class SubTimelineTrack : MiniTrackBase<SubTimelineClip>
    {
        private Dictionary<string, MiniTimelineDirector> directors = new Dictionary<string, MiniTimelineDirector>();
        private Transform directorContainer;

        protected override void OnPrepare()
        {
            if (directorContainer == null)
            {
                var go = new GameObject($"SubTimelines_{Id}");
                go.transform.SetParent(targetObject != null ? (targetObject as Component)?.transform : null);
                directorContainer = go.transform;
            }

            foreach (var clip in clips)
            {
                if (string.IsNullOrEmpty(clip.projectPath)) continue;

                if (!directors.ContainsKey(clip.Id))
                {
                    // Create a new director for this clip
                    var directorGO = new GameObject($"Director_{clip.Id}");
                    directorGO.transform.SetParent(directorContainer);

                    var director = directorGO.AddComponent<MiniTimelineDirector>();

                    // Sanitize project name (remove .json extension if present)
                    string projectName = clip.projectPath;
                    if (projectName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        projectName = System.IO.Path.GetFileNameWithoutExtension(projectName);
                    }

                    // Load project
                    if (director.LoadProject(projectName))
                    {
                        director.Stop();
                        director.AutoLoadFirstProject = false;
                        director.AutoCreateEmptyProject = false;

                        // Disable by default until played
                        director.gameObject.SetActive(false);

                        directors[clip.Id] = director;
                    }
                    else
                    {
                        Debug.LogWarning($"[SubTimelineTrack] Failed to load project '{projectName}' for clip '{clip.Id}'");
                        GameObject.Destroy(directorGO);
                    }
                }
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            foreach (var clip in clips)
            {
                if (directors.TryGetValue(clip.Id, out var director))
                {
                    if (clip.Contains(time))
                    {
                        if (!director.gameObject.activeSelf)
                        {
                            director.gameObject.SetActive(true);
                        }

                        // Calculate local time for the sub-timeline
                        float clipTime = time - clip.Start;
                        float subTime = clipTime * clip.speed;

                        // Clamp to director length
                        subTime = Mathf.Clamp(subTime, 0, director.Length);

                        director.Seek(subTime);
                        director.Evaluate(scrub);
                    }
                    else
                    {
                        if (director.gameObject.activeSelf)
                        {
                            StopDirectorCleanly(director);
                        }
                    }
                }
            }
        }

        private void StopDirectorCleanly(MiniTimelineDirector director)
        {
            // Force tracks to exit by evaluating at a time where nothing should be active
            // -1.0f is a safe bet for timelines starting at 0.0f
            foreach (var track in director.Tracks)
            {
                if (track != null)
                {
                    track.Evaluate(-1f, false);
                }
            }

            director.Stop();
            director.gameObject.SetActive(false);
        }

        protected override void OnCleanup()
        {
            foreach (var director in directors.Values)
            {
                if (director != null)
                {
                    StopDirectorCleanly(director); // Ensure cleanup logic runs
                    if (director.gameObject != null)
                    {
                        GameObject.Destroy(director.gameObject);
                    }
                }
            }
            directors.Clear();

            if (directorContainer != null)
            {
                GameObject.Destroy(directorContainer.gameObject);
            }
        }
    }
}
