using MiniTimeline.Core;
using System;
using System.Collections.Generic;

namespace MiniTimeline.Tracks
{
    [Serializable]
    public class UmaWardrobeClip : MiniClipBase
    {
        public string wardrobeJson;

        public Dictionary<string, string> WardrobeRecipes { get; set; }
        public Dictionary<string, UnityEngine.Color> WardrobeColors { get; set; }
    }
}