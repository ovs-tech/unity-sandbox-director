using System;
using System.Collections.Generic;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Legacy DTOs kept for UI and editor tooling compatibility during refactor.
    /// These are not used by runtime serialization or director logic.
    /// </summary>
    // Legacy DTO kept temporarily; do not treat as error in compilation.
    [Serializable]
    public class TrackData
    {
        public string id;
        public string type;
        public string bindKey;
        public bool enabled = true;
        public int order = 0;
        public int evaluateMode = 3;
        public List<ClipData> clips = new List<ClipData>();
        public Dictionary<string, object> properties = new Dictionary<string, object>();
    }

    // Legacy DTO kept temporarily; do not treat as error in compilation.
    [Serializable]
    public class ClipData
    {
        public string id;
        public float start;
        public float duration;
        public Dictionary<string, object> payload = new Dictionary<string, object>();
    }
}
