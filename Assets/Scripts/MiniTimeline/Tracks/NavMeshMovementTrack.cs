using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Track that moves NavMeshAgents (or objects with NavMeshAgent) using `NavMeshMovementClip` data.
    /// </summary>
    [Serializable]
    public class NavMeshMovementTrack : MultiTargetMiniTrackBase<NavMeshMovementClip>
    {
        protected override void OnPrepare()
        {
            base.OnPrepare();
            // No persistent per-track caching required. Agents are resolved per-target.
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            var activeClips = GetActiveClips(time);
            if (activeClips == null || activeClips.Count == 0) return;

            var clip = activeClips.Last();

            // Resolve target transform
            Transform target = null;
            if (clip.targetIndex >= 0 && clip.targetIndex < targets.Count)
            {
                target = targets[clip.targetIndex];
            }

            if (target == null) return;

            var agent = target.GetComponent<NavMeshAgent>();
            if (agent == null) return;

            // Determine desired position/speed/rotation
            Vector3 desiredPos = clip.hasPosition ? clip.GetPositionAtTime(time) : target.position;
            Quaternion desiredRot = clip.hasRotation ? clip.GetRotationAtTime(time) : target.rotation;
            float desiredSpeed = clip.hasSpeed ? clip.GetSpeedAtTime(time) : agent.speed;

            if (scrub)
            {
                // In editor scrub mode, teleport agent to position and stop motion
                agent.Warp(desiredPos);
                agent.velocity = Vector3.zero;
                agent.speed = desiredSpeed;
                target.rotation = desiredRot;
            }
            else
            {
                // Normal playback: set destination and speed. Rotation is handled by agent movement.
                agent.speed = desiredSpeed;
                agent.SetDestination(desiredPos);
            }
        }

        #region Public API

        public NavMeshMovementClip AddMovementClip(float start, float duration, int targetIndex = 0)
        {
            var clip = new NavMeshMovementClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                targetIndex = targetIndex
            };

            clips.Add(clip);
            return clip;
        }

        #endregion
    }
}
