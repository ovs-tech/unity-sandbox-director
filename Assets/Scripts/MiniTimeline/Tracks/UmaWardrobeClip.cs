using Systems.MiniTimeline.Core;
using System;
using System.Collections.Generic;
using Sirenix.OdinSerializer;

namespace Systems.MiniTimeline.Tracks
{
    [Serializable]
    public class UmaWardrobeClip : MiniClipBase
    {
        [OdinSerialize]
        public string wardrobeJson;

        /// <summary>
        /// Runtime-only property, not serialized
        /// </summary>
        public Dictionary<string, string> WardrobeRecipes { get; set; }
        
        /// <summary>
        /// Runtime-only property, not serialized
        /// </summary>
        public Dictionary<string, UnityEngine.Color> WardrobeColors { get; set; }
    }
}