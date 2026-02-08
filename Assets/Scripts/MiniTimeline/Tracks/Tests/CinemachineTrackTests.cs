using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Cinemachine;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

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
            // Setup target
            targetObject = new GameObject("Target");

            // Setup VCam (needs to be on target or we mock it)
            // The code expects the VCam on the targetObject: vcamTransform.GetComponent<CinemachineCamera>()
            vcam = targetObject.AddComponent<CinemachineCamera>();
            vcamObject = targetObject; // Same object in this setup

            track = new CinemachineTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null) Object.DestroyImmediate(targetObject);
        }

        private void PrepareTrack(CinemachineTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<CinemachineClip>);

            // Set targetObject
            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null) targetField.SetValue(track, target);

            // Set isBound
            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null) boundField.SetValue(track, true);

            track.Prepare();
        }

        [Test]
        public void Test_NoAnimator_DefaultsToHeadHeight()
        {
            PrepareTrack(track, targetObject);

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
            PrepareTrack(track, targetObject);

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

        // Note: Testing Animator/Humanoid logic is difficult without a valid Humanoid Avatar asset.
        // We can simulate the "Animator found but not humanoid" case easily.
        // Simulating "IsHuman" requires an Avatar which is hard to create programmatically in a test without asset dependencies.
        // We will skip explicit Humanoid Bone tests here to avoid asset dependency issues,
        // relying on the manual verification we did and the structural correctness of the code.
        // However, we can test the fallback logic which is crucial.

        [Test]
        public void Test_LowAngle_NoAnimator_DefaultsToLowHeight()
        {
             PrepareTrack(track, targetObject);

            var clip = track.AddShotClip(0, 10, CinemachineShotType.LowAngle, 0, 5f);

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
