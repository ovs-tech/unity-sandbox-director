using Systems.MiniTimeline.Core;
using UnityEngine;


namespace Systems.MiniTimeline.Tracks
{
    [System.Serializable]
    public class UMAExpressionClip : MiniClipBase
    {
        
        public string expression;
        
        public float from = 0f;
        
        public float to = 1f;
        
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