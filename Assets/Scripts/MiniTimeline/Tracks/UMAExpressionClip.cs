using MiniTimeline.Core;
using UnityEngine;
using Sirenix.OdinSerializer;

namespace MiniTimeline.Tracks
{
    [System.Serializable]
    public class UMAExpressionClip : MiniClipBase
    {
        [OdinSerialize]
        public string expression;
        [OdinSerialize]
        public float from = 0f;
        [OdinSerialize]
        public float to = 1f;
        [OdinSerialize]
        public AnimationCurve blendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        /// <summary>
        /// Get friendly display name for the clip
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(expression))
            {
                return $"Expression: {expression}";
            }
            return Id;
        }
    }
}