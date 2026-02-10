using System.Collections;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Core.Tests
{
    public class TransformableItemPerformanceTests
    {
        private GameObject _itemObject;
        private TransformableItem _item;
        private MethodInfo _updateGizmoMethod;
        private GameObject[] _dummyObjects;

        [SetUp]
        public void SetUp()
        {
            // Create dummy objects to increase scene complexity for FindFirstObjectByType
            _dummyObjects = new GameObject[1000];
            for (int i = 0; i < 1000; i++)
            {
                _dummyObjects[i] = new GameObject($"Dummy_{i}");
            }

            // Ensure NO camera exists in the scene
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam.gameObject.tag == "MainCamera") cam.gameObject.tag = "Untagged";
                Object.DestroyImmediate(cam.gameObject);
            }

            _itemObject = new GameObject("TransformableItem");
            _item = _itemObject.AddComponent<TransformableItem>();

            // Enable gizmo settings
            _item.EnableTransformControls = true;
            _item.ToggleGizmo(true);

            // Force _camera to null just in case OnEnable found one before we destroyed them (unlikely due to order but safe)
            var cameraField = typeof(TransformableItem).GetField("_camera", BindingFlags.NonPublic | BindingFlags.Instance);
            cameraField.SetValue(_item, null);

            // Reflection to get UpdateGizmo
            _updateGizmoMethod = typeof(TransformableItem).GetMethod("UpdateGizmo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Force selection so gizmo updates run (UpdateGizmo checks _isSelected)
            // Wait, does UpdateGizmo require selection?
            // "bool shouldShow = _isSelected && _isInTransformModeType && _currentTransformModeType != TransformModeType.None;"
            // But the camera lookup happens BEFORE that check:
            /*
            // Find camera if not cached
            if (_camera == null)
            {
                _camera = Camera.main ?? FindFirstObjectByType<Camera>();
                if (_camera == null)
                    return;
            }
            */
            // So camera lookup happens regardless of selection?
            // Let's verify in the code.
        }

        [TearDown]
        public void TearDown()
        {
            if (_itemObject != null) Object.DestroyImmediate(_itemObject);
            if (_dummyObjects != null)
            {
                foreach (var obj in _dummyObjects)
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                }
            }
        }

        [Test]
        public void Measure_UpdateGizmo_Performance_NoCamera()
        {
            int iterations = 1000;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < iterations; i++)
            {
                _updateGizmoMethod.Invoke(_item, null);
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log($"[Performance] UpdateGizmo (No Camera) {iterations} times took: {stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}
