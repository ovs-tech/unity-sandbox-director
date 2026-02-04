using System;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// NavMesh movement clip for moving NavMeshAgents or objects using the MiniTimeline.
    /// </summary>
    [Serializable]
    public class NavMeshMovementClip : MiniClipBase
    {
        [Header("Target")]
        public int targetIndex = 0;

        [Header("Position")]
        public bool hasPosition = false;
        public Vector3 startPosition = Vector3.zero;
        public Vector3 endPosition = Vector3.zero;

        [Header("Rotation")]
        public bool hasRotation = false;
        public Quaternion startRotation = Quaternion.identity;
        public Quaternion endRotation = Quaternion.identity;

        [Header("Agent Properties")]
        public bool hasSpeed = false;
        public float startSpeed = 3.5f;
        public float endSpeed = 3.5f;

        [Header("Animation")]
        public CameraAnimationCurve animationCurve = CameraAnimationCurve.Linear;
        public AnimationCurve customCurve;

        public float GetLocalTime(float globalTime)
        {
            return globalTime - Start;
        }

        public Vector3 GetPositionAtTime(float globalTime)
        {
            if (!hasPosition) return Vector3.zero;
            float normalizedTime = GetNormalizedTime(globalTime);
            return EvaluateVector3(startPosition, endPosition, normalizedTime);
        }

        public Quaternion GetRotationAtTime(float globalTime)
        {
            if (!hasRotation) return Quaternion.identity;
            float normalizedTime = GetNormalizedTime(globalTime);
            return EvaluateQuaternion(startRotation, endRotation, normalizedTime);
        }

        public float GetSpeedAtTime(float globalTime)
        {
            if (!hasSpeed) return startSpeed;
            float normalizedTime = GetNormalizedTime(globalTime);
            return EvaluateFloat(startSpeed, endSpeed, normalizedTime);
        }

        #region Interpolation Helpers

        private Vector3 EvaluateVector3(Vector3 a, Vector3 b, float t)
        {
            switch (animationCurve)
            {
                case CameraAnimationCurve.EaseInOut:
                    return Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
                case CameraAnimationCurve.EaseIn:
                    return Vector3.Lerp(a, b, t * t);
                case CameraAnimationCurve.EaseOut:
                    return Vector3.Lerp(a, b, 1f - (1f - t) * (1f - t));
                case CameraAnimationCurve.Custom:
                    if (customCurve != null) return Vector3.Lerp(a, b, customCurve.Evaluate(t));
                    return Vector3.Lerp(a, b, t);
                case CameraAnimationCurve.Linear:
                default:
                    return Vector3.Lerp(a, b, t);
            }
        }

        private Quaternion EvaluateQuaternion(Quaternion a, Quaternion b, float t)
        {
            switch (animationCurve)
            {
                case CameraAnimationCurve.EaseInOut:
                    return Quaternion.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
                case CameraAnimationCurve.EaseIn:
                    return Quaternion.Lerp(a, b, t * t);
                case CameraAnimationCurve.EaseOut:
                    return Quaternion.Lerp(a, b, 1f - (1f - t) * (1f - t));
                case CameraAnimationCurve.Custom:
                    if (customCurve != null) return Quaternion.Lerp(a, b, customCurve.Evaluate(t));
                    return Quaternion.Lerp(a, b, t);
                case CameraAnimationCurve.Linear:
                default:
                    return Quaternion.Lerp(a, b, t);
            }
        }

        private float EvaluateFloat(float a, float b, float t)
        {
            switch (animationCurve)
            {
                case CameraAnimationCurve.EaseInOut:
                    return Mathf.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
                case CameraAnimationCurve.EaseIn:
                    return Mathf.Lerp(a, b, t * t);
                case CameraAnimationCurve.EaseOut:
                    return Mathf.Lerp(a, b, 1f - (1f - t) * (1f - t));
                case CameraAnimationCurve.Custom:
                    if (customCurve != null) return Mathf.Lerp(a, b, customCurve.Evaluate(t));
                    return Mathf.Lerp(a, b, t);
                case CameraAnimationCurve.Linear:
                default:
                    return Mathf.Lerp(a, b, t);
            }
        }

        #endregion
    }
}
