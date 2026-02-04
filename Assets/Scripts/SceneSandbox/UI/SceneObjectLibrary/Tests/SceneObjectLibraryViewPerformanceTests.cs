using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.SceneSandbox.Data;
using Systems.SceneSandbox.UI.SceneObjectLibrary;
using System.Reflection;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary.Tests
{
    public class SceneObjectLibraryViewPerformanceTests
    {
        private SceneObjectLibraryView _view;
        private VisualElement _mockContainer;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TestView");
            _view = go.AddComponent<SceneObjectLibraryView>();

            // Manually setup the view components using reflection since we can't easily rely on UIDocument in this test context
            _mockContainer = new VisualElement();

            // Set _objectGridContainer via reflection
            var field = typeof(SceneObjectLibraryView).GetField("_objectGridContainer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(_view, _mockContainer);
            }
            else
            {
                Debug.LogError("Could not find _objectGridContainer field");
            }

            // Initialize _visibleCards and _cardPool if they are null (though field initializers should handle this)
            var visibleCardsField = typeof(SceneObjectLibraryView).GetField("_visibleCards", BindingFlags.NonPublic | BindingFlags.Instance);
             if (visibleCardsField != null && visibleCardsField.GetValue(_view) == null)
            {
                visibleCardsField.SetValue(_view, new Dictionary<SceneObjectData, VisualElement>());
            }

            var cardPoolField = typeof(SceneObjectLibraryView).GetField("_cardPool", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cardPoolField != null && cardPoolField.GetValue(_view) == null)
            {
                cardPoolField.SetValue(_view, new Queue<VisualElement>());
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_view != null)
            {
                GameObject.DestroyImmediate(_view.gameObject);
            }
        }

        [Test]
        public void RenderObjectGrid_Performance_Benchmark()
        {
            // Generate data
            int objectCount = 1000;
            var objects = new List<SceneObjectData>();
            for (int i = 0; i < objectCount; i++)
            {
                objects.Add(new SceneObjectData
                {
                    id = i.ToString(),
                    displayName = $"Object {i}",
                    objectType = SceneObjectType.Prop
                });
            }

            // Warmup
            _view.RenderObjectGrid(new List<SceneObjectData>());

            // Measure first run (allocation)
            Stopwatch sw = new Stopwatch();
            sw.Start();
            _view.RenderObjectGrid(objects);
            sw.Stop();
            long firstRunMs = sw.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"First run (Allocation): {firstRunMs}ms");

            // Measure second run (should be reuse)
            sw.Restart();
            _view.RenderObjectGrid(objects);
            sw.Stop();
            long secondRunMs = sw.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"Second run (Reuse): {secondRunMs}ms");

            // In the unoptimized version, second run will likely be similar or slower (due to GC) than first run if we ignore initial JIT.
            // But main issue is allocation.
        }
    }
}
