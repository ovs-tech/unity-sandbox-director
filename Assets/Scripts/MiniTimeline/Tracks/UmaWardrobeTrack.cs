using System;
using UnityEngine;
using MiniTimeline.Core;
using System.Collections.Generic;

namespace MiniTimeline.Tracks
{
    [Serializable]
    public class UmaWardrobeTrack : MiniTrackBase<UmaWardrobeClip>
    {
        private UmaAvatar umaAvatar;
        private UmaWardrobeClip lastAppliedClip;

        [System.Serializable]
        private class WardrobeData
        {
            public List<WardrobeItem> wardrobe;
            public List<ColorItem> colors;
        }

        [System.Serializable]
        private class WardrobeItem
        {
            public string slot;
            public string recipe;
        }

        [System.Serializable]
        private class ColorItem
        {
            public string name;
            public string color;
        }

        protected override void OnPrepare()
        {
            lastAppliedClip = null;
            if (targetObject is GameObject go)
            {
                umaAvatar = go.GetComponent<UmaAvatar>();
            }

            if (umaAvatar == null)
            {
                Debug.LogError("[UmaWardrobeTrack] UmaAvatar component not found on target object.");
                return;
            }

            foreach (var clip in clips)
            {
                ParseClipData(clip);
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            if (umaAvatar == null) return;

            var activeClips = GetActiveClips(time);
            UmaWardrobeClip currentClip = null;
            if (activeClips.Count > 0)
            {
                // For simplicity, apply the last active clip.
                currentClip = activeClips[activeClips.Count - 1];
            }

            if (currentClip != lastAppliedClip)
            {
                lastAppliedClip = currentClip;
                if (currentClip != null)
                {
                    if (currentClip.WardrobeRecipes != null)
                    {
                        umaAvatar.SetWardrobe(currentClip.WardrobeRecipes);
                    }
                    if (currentClip.WardrobeColors != null)
                    {
                        umaAvatar.SetColors(currentClip.WardrobeColors);
                    }
                }
            }
        }

        protected override void OnCleanup()
        {
            lastAppliedClip = null;
        }

        private void ParseClipData(UmaWardrobeClip clip)
        {
            if (string.IsNullOrEmpty(clip.wardrobeJson))
            {
                clip.WardrobeRecipes = new Dictionary<string, string>();
                clip.WardrobeColors = new Dictionary<string, Color>();
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<WardrobeData>(clip.wardrobeJson);

                clip.WardrobeRecipes = new Dictionary<string, string>();
                foreach (var item in data.wardrobe)
                {
                    clip.WardrobeRecipes[item.slot] = item.recipe;
                }

                clip.WardrobeColors = new Dictionary<string, Color>();
                foreach (var item in data.colors)
                {
                    if (ColorUtility.TryParseHtmlString(item.color, out Color color))
                    {
                        clip.WardrobeColors[item.name] = color;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UmaWardrobeTrack] Failed to parse wardrobe JSON: {e.Message}");
                clip.WardrobeRecipes = new Dictionary<string, string>();
                clip.WardrobeColors = new Dictionary<string, Color>();
            }
        }
    }
}