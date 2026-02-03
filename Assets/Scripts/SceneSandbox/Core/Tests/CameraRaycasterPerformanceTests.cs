using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Core.Tests
{
    public class CameraRaycasterPerformanceTests
    {
        private GameObject _cameraObject;
        private GameObject _raycasterObject;
        private CameraRaycaster _raycaster;

        [SetUp]
        public void SetUp()
        {
            if (Camera.main == null)
            {
                _cameraObject = new GameObject("MainCamera");
                _cameraObject.AddComponent<Camera>();
                _cameraObject.tag = "MainCamera";
            }
            else
            {
                _cameraObject = Camera.main.gameObject;
            }

            _raycasterObject = new GameObject("Raycaster");
            _raycaster = _raycasterObject.AddComponent<CameraRaycaster>();

            // Force null to ensure we test the fallback path initially
            _raycaster.TargetCamera = null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_raycasterObject != null) Object.DestroyImmediate(_raycasterObject);
            // Don't destroy camera if it wasn't created by us, but for this test environment it's likely safe or needed cleanup
            if (_cameraObject != null && _cameraObject.name == "MainCamera") Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void Measure_TargetCamera_Access_Performance()
        {
            // Reset to null to ensure we start from clean state
            _raycaster.TargetCamera = null;

            int iterations = 10000;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < iterations; i++)
            {
                var c = _raycaster.TargetCamera;
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log($"[Performance] Accessing TargetCamera {iterations} times took: {stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}
