using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Cinemachine;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;
using Object = UnityEngine.Object;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class CinemachineTrackTests
    {
        private GameObject targetObject;
        private GameObject vcamObject;
        private CinemachineTrack track;
        private CinemachineCamera vcam;

        [SetUp]
        public void Setup()
        {
            // Setup target (the subject being filmed)
            targetObject = new GameObject("Target");

            // Setup VCam (the camera itself)
            vcamObject = new GameObject("VCam");
            vcam = vcamObject.AddComponent<CinemachineCamera>();

            track = new CinemachineTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null) Object.DestroyImmediate(targetObject);
            if (vcamObject != null) Object.DestroyImmediate(vcamObject);
        }

        private void PrepareTrack(CinemachineTrack track, GameObject target, GameObject vcamObj)
        {
            var baseType = typeof(MiniTrackBase<CinemachineClip>);
            var multiTargetType = typeof(MultiTargetMiniTrackBase<CinemachineClip>);

            // Set targetObject (The Camera itself)
            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null)
            {
                targetField.SetValue(track, vcamObj);
            }
            else
            {
                Assert.Fail("Could not find 'targetObject' field in MiniTrackBase.");
            }

            // Set isBound
            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null)
            {
                boundField.SetValue(track, true);
            }
            else
            {
                Assert.Fail("Could not find 'isBound' field in MiniTrackBase.");
            }

            // Set targets list (The Target Object for shots)
            // CinemachineTrack uses 'targets[clip.targetIndex]' to find the target Transform.
            var targetsField = multiTargetType.GetField("targets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetsField != null)
            {
                var list = new List<Transform>();
                list.Add(target.transform); // targetIndex 0
                targetsField.SetValue(track, list);
            }
            else
            {
                Assert.Fail("Could not find 'targets' field in MultiTargetMiniTrackBase.");
            }

            track.Prepare();
        }

        [Test]
        public void Test_NoAnimator_DefaultsToHeadHeight()
        {
            PrepareTrack(track, targetObject, vcamObject);

            // Add a clip: EyeLevel, Yaw=0, Dist=5
            var clip = track.AddShotClip(0, 10, CinemachineShotType.EyeLevel, 0, 5f);
            clip.yaw = 0f;

            // Target at (0,0,0) facing Z+ (0,0,1)
            targetObject.transform.position = Vector3.zero;
            targetObject.transform.rotation = Quaternion.identity;

            // Evaluate
            track.Evaluate(5f, false);

            // Expected:
            // Pos = TargetPos + HorizontalDir * Dist + HeightOffset
            // HorizontalDir = (0,0,1) since Yaw=0 relative to TargetForward(0,0,1)
            // HeightOffset = 1.6f (default)
            // Expected Pos = (0, 0, 0) + (0, 0, 5) + (0, 1.6, 0) = (0, 1.6, 5)

            Vector3 expectedPos = new Vector3(0, 1.6f, 5f);

            Assert.That(vcam.transform.position.x, Is.EqualTo(expectedPos.x).Within(0.01f));
            Assert.That(vcam.transform.position.y, Is.EqualTo(expectedPos.y).Within(0.01f));
            Assert.That(vcam.transform.position.z, Is.EqualTo(expectedPos.z).Within(0.01f));
        }

        [Test]
        public void Test_NoAnimator_RelativeOrientation()
        {
            PrepareTrack(track, targetObject, vcamObject);

            var clip = track.AddShotClip(0, 10, CinemachineShotType.EyeLevel, 0, 5f);
            clip.yaw = 0f;

            // Target at (0,0,0) facing X+ (1,0,0) (Rotated 90 deg Y)
            targetObject.transform.position = Vector3.zero;
            targetObject.transform.rotation = Quaternion.Euler(0, 90, 0);

            track.Evaluate(5f, false);

            // Expected:
            // TargetForward = (1, 0, 0)
            // HorizontalDir = Yaw(0) relative to TargetForward -> (1, 0, 0)
            // Expected Pos = (0, 0, 0) + (5, 0, 0) + (0, 1.6, 0) = (5, 1.6, 0)

            Vector3 expectedPos = new Vector3(5f, 1.6f, 0f);

            Assert.That(vcam.transform.position.x, Is.EqualTo(expectedPos.x).Within(0.01f));
            Assert.That(vcam.transform.position.y, Is.EqualTo(expectedPos.y).Within(0.01f));
            Assert.That(vcam.transform.position.z, Is.EqualTo(expectedPos.z).Within(0.01f));
        }

        [Test]
        public void Test_LowAngle_NoAnimator_DefaultsToLowHeight()
        {
             PrepareTrack(track, targetObject, vcamObject);

            var clip = track.AddShotClip(0, 10, CinemachineShotType.LowAngle, 0, 5f);
            // Ensure yaw is 0 for consistent direction
            clip.yaw = 0f;

            targetObject.transform.position = Vector3.zero;
            targetObject.transform.rotation = Quaternion.identity;

            track.Evaluate(5f, false);

            // LowAngle logic:
            // dist *= 0.9 -> 4.5
            // HorizontalDir = (0,0,1)
            // HeightOffset = 0.5f (fallback for LowAngle)
            // Pos = (0, 0, 4.5) + (0, 0.5, 0) = (0, 0.5, 4.5)

            Vector3 expectedPos = new Vector3(0, 0.5f, 4.5f);

            Assert.That(vcam.transform.position.x, Is.EqualTo(expectedPos.x).Within(0.01f));
            Assert.That(vcam.transform.position.y, Is.EqualTo(expectedPos.y).Within(0.01f));
            Assert.That(vcam.transform.position.z, Is.EqualTo(expectedPos.z).Within(0.01f));
        }
    }
}
