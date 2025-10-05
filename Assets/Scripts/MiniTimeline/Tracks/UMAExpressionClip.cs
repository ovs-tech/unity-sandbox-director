using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.Tracks
{
    [System.Serializable]
    public class UMAExpressionClip : MiniClipBase
    {
        public string expression;
        public AnimationCurve blendCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        public override string displayName => $"{base.displayName} ({expression})";
    }
}