using System;
using System.Linq;
using UnityEngine;
using Systems.MiniTimeline.Core;
using Unity.Cinemachine;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Track for controlling a camera (specifically designed for Cinemachine Virtual Cameras)
    /// using high-level shot presets relative to one or more targets.
    /// </summary>
    [Serializable]
    public class CinemachineTrack : MultiTargetMiniTrackBase<CinemachineClip>
    {
        private CinemachineCamera vcam;
        private CinemachineThirdPersonFollow thirdPerson;
        private CinemachineOrbitalFollow orbital;
        
        protected override void OnPrepare()
        {
            base.OnPrepare();
            
            vcam = null;
            thirdPerson = null;
            orbital = null;
            Transform vcamTransform = null;
            
            if (targetObject is Transform t)
            {
                vcamTransform = t;
            }
            else if (targetObject is GameObject go)
            {
                vcamTransform = go.transform;
            }
            else if (targetObject is Component comp)
            {
                vcamTransform = comp.transform;
            }
            
            if (vcamTransform != null)
            {
                vcam = vcamTransform.GetComponent<CinemachineCamera>();
                // Cache common Cinemachine body components if present
                thirdPerson = vcamTransform.GetComponent<CinemachineThirdPersonFollow>();
                orbital = vcamTransform.GetComponent<CinemachineOrbitalFollow>();
            }
            
            if (vcam == null)
            {
                Debug.LogWarning($"[CinemachineTrack] No CinemachineCamera component found on target for track '{Id}'");
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            if (vcam == null) return;

            var activeClips = GetActiveClips(time);
            if (activeClips.Count == 0) return;

            // Simple implementation: use the latest active clip
            var clip = activeClips.Last();
            
            // Resolve target
            Transform target = null;
            if (clip.targetIndex >= 0 && clip.targetIndex < targets.Count)
            {
                target = targets[clip.targetIndex];
            }

            if (target == null)
            {
                return;
            }

            CalculateAndApplyShot(clip, target);
        }

        private void CalculateAndApplyShot(CinemachineClip clip, Transform target)
        {
            // Set targets
            if (vcam.Follow != target) vcam.Follow = target;
            if (vcam.LookAt != target) vcam.LookAt = target;

            // Apply Lens settings
            if (clip.overrideFOV)
            {
                var lens = vcam.Lens;
                lens.FieldOfView = clip.fieldOfView;
                vcam.Lens = lens;
            }

            // Calculate desired world position (for manual override or fallback)
            Vector3 targetPos = target.position + clip.offset;
            Vector3 desiredWorldPos = vcam.transform.position;
            Quaternion desiredWorldRot = vcam.transform.rotation;

            // Calculate base direction relative to target (so Yaw=0 is Front View)
            Vector3 forward = target.forward;
            Vector3 horizontalDir = Quaternion.Euler(0, clip.yaw, 0) * forward;

            // Use an effective distance for body components (allows per-shot tuning)
            float distanceForComponent = clip.distance;
            Vector3 calculatedWorldOffset = Vector3.zero;

            // Adjust base height to approximate human head/eye level if target is at feet
            float heightOffset = 1.6f;

            switch (clip.shotType)
            {
                case CinemachineShotType.EyeLevel:
                    calculatedWorldOffset = horizontalDir * distanceForComponent + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.HighAngle:
                    distanceForComponent *= 0.9f;
                    calculatedWorldOffset = horizontalDir * distanceForComponent + Vector3.up * (distanceForComponent * 0.7f + heightOffset);
                    break;

                case CinemachineShotType.LowAngle:
                    distanceForComponent *= 0.9f;
                    // Low angle: Camera near ground looking up.
                    calculatedWorldOffset = horizontalDir * distanceForComponent + Vector3.up * 0.5f;
                    break;

                case CinemachineShotType.Overhead:
                    distanceForComponent *= 1.2f;
                    calculatedWorldOffset = Vector3.up * distanceForComponent + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.BirdEye:
                    distanceForComponent *= 3f;
                    calculatedWorldOffset = horizontalDir * (distanceForComponent * 2f) + Vector3.up * (distanceForComponent * 2f);
                    break;

                case CinemachineShotType.WormEye:
                    distanceForComponent *= 0.8f;
                    // Worm eye: very low, near-ground looking up
                    calculatedWorldOffset = horizontalDir * distanceForComponent + Vector3.up * 0.1f;
                    break;

                case CinemachineShotType.SideView:
                    distanceForComponent *= 1f;
                    Vector3 sideDir = Vector3.Cross(Vector3.up, horizontalDir);
                    calculatedWorldOffset = sideDir * distanceForComponent + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.CloseUp:
                    distanceForComponent *= 0.3f;
                    calculatedWorldOffset = horizontalDir * (distanceForComponent) + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.DutchAngle:
                    // Tilted shot: position similar to EyeLevel but apply roll
                    calculatedWorldOffset = horizontalDir * distanceForComponent + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.OverTheShoulder:
                    // Position behind the shoulder: back and to the side relative to target forward
                    distanceForComponent *= 0.8f;
                    Vector3 back = -target.forward * (distanceForComponent * 0.6f);
                    Vector3 otsSide = Vector3.Cross(Vector3.up, target.forward).normalized * (distanceForComponent * 0.4f);
                    calculatedWorldOffset = back + otsSide + Vector3.up * (heightOffset - 0.1f);
                    break;

                case CinemachineShotType.POV:
                    // Point-of-view: sit very close to target, adopt target rotation
                    distanceForComponent = 0.15f;
                    calculatedWorldOffset = target.forward * distanceForComponent + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.ExtremeLongShot:
                    distanceForComponent *= 4f;
                    calculatedWorldOffset = horizontalDir * (distanceForComponent) + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.LongShot:
                    distanceForComponent *= 2f;
                    calculatedWorldOffset = horizontalDir * (distanceForComponent) + Vector3.up * heightOffset;
                    break;

                case CinemachineShotType.MediumShot:
                    distanceForComponent *= 1f;
                    calculatedWorldOffset = horizontalDir * (distanceForComponent) + Vector3.up * (heightOffset * 0.75f);
                    break;

                case CinemachineShotType.ExtremeCloseUp:
                    distanceForComponent *= 0.05f;
                    calculatedWorldOffset = horizontalDir * (distanceForComponent) + Vector3.up * heightOffset;
                    break;

                default:
                    calculatedWorldOffset = horizontalDir * distanceForComponent + Vector3.up * heightOffset;
                    break;
            }
            
            // Apply pitch to rotation calc only (for fallback)
            desiredWorldPos = targetPos + calculatedWorldOffset;

            // Calculate look target (approximate head position)
            Vector3 lookAtPos = targetPos + Vector3.up * heightOffset;

            desiredWorldRot = Quaternion.LookRotation(lookAtPos - desiredWorldPos);
            if (clip.pitch != 0) desiredWorldRot *= Quaternion.Euler(clip.pitch, 0, 0);
            if (clip.roll != 0) desiredWorldRot *= Quaternion.Euler(0, 0, clip.roll);

            // 1. Try to set via cached CinemachineThirdPersonFollow
            if (thirdPerson != null)
            {
                thirdPerson.CameraDistance = distanceForComponent;
                // Shoulder offset approximates the directional offset
                // But ThirdPerson usually handles rotation via Input.
                // We can force the rig orientation? No easy way without input provider override.
                // So fallback to transform override might be cleaner for forced shots.
                
                // If we simply set transform, the body will overwrite it.
                // UNLESS we are in "Do Nothing" mode.
            }

            // 2. Try to set via cached CinemachineOrbitalFollow
            if (orbital != null)
            {
                // We can set the orbit angles
                orbital.Radius = distanceForComponent;
                // orbital.VerticalAxis.Value = ... 
                // orbital.HorizontalAxis.Value = ...
                // This requires manipulating the Axis objects which drives the camera.
                // We can set Value directly.
                
                // orbital.HorizontalAxis.Value = clip.yaw;
                // orbital.VerticalAxis.Value = clip.pitch; // or derived from shot type
                
                // This would be the "Cinemachine Way".
            }

            // Fallback: Force transform. 
            // This works if the VCam body is "Do Nothing" or if we accept fighting the update loop (MiniTimeline usually updates in Update).
            // Cinemachine updates in LateUpdate. So our change here will be overwritten if there is an active Body.
            // But if we use "Hard Lock To Target", we can't offset.
            
            // Since the user wants "Simple Setting", forcing transform is the most robust "Shot" enforcer.
            // But to "use its functions", we set Follow/LookAt.
            
            // Hybrid approach: 
            // We set Follow/LookAt.
            // We ALSO set transform.position/rotation.
            // If the VCam has a body, the body wins in LateUpdate.
            // If the user wants these shots to work, they should probably use a VCam with "Do Nothing" body or appropriate body.
            
            vcam.transform.position = desiredWorldPos;
            vcam.transform.rotation = desiredWorldRot;
            
            // If we have a Transposer (CinemachineFollow), we can try to set the offset to match our calculation?
            // This is complex because Transposer offset is in Local or World space depending on config.
        }

        #region Public API

        /// <summary>
        /// Adds a new cinemachine shot clip to the track.
        /// </summary>
        public CinemachineClip AddShotClip(float start, float duration, CinemachineShotType shotType, int targetIndex = 0, float distance = 5f)
        {
            var clip = new CinemachineClip
            {
                Id = Guid.NewGuid().ToString(),
                Start = start,
                Duration = duration,
                shotType = shotType,
                targetIndex = targetIndex,
                distance = distance
            };

            clips.Add(clip);
            return clip;
        }

        #endregion
    }
}