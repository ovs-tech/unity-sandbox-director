using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.MiniTimeline.Serialization
{
    /// <summary>
    /// Wrapper for polymorphic types in JSON serialization
    /// </summary>
    [Serializable]
    public struct SerializedWrapper
    {
        public string type;
        public string data;
    }

    /// <summary>
    /// Helper for dictionary serialization in JsonUtility
    /// </summary>
    [Serializable]
    public struct SerializedMetadataMap
    {
        public List<string> keys;
        public List<string> values;
        public List<string> valueTypes;
    }
}
