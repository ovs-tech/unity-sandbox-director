using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Systems.MiniTimeline.UI;
using Systems.MiniTimeline.Core;
using System.Reflection;

namespace Systems.MiniTimeline.UI.Tests
{
    public class ZoomToFitTests
    {
        private GameObject editorGO;
        private TimelineEditorUIToolkit editor;
        private UIDocument uiDoc;
        private MiniTimelineDirector director;

        [SetUp]
        public void Setup()
        {
            editorGO = new GameObject("Editor");
            uiDoc = editorGO.AddComponent<UIDocument>();

            // Create minimal UI hierarchy
            var root = uiDoc.rootVisualElement;
            var timelineContainer = new VisualElement { name = "timeline-container" };
            timelineContainer.AddToClassList("timeline-container");

            var timelineScrollView = new ScrollView { name = "timeline-scroll" };
            timelineScrollView.Add(timelineContainer);
            root.Add(timelineScrollView);

            // Add editor component
            editor = editorGO.AddComponent<TimelineEditorUIToolkit>();

            // Setup director
            var directorGO = new GameObject("Director");
            director = directorGO.AddComponent<MiniTimelineDirector>();

            // Create a dummy project
            var project = new MiniTimelineProject();
            project.length = 10f; // 10 seconds
            project.frameRate = 30;

            // Assign project
            director.SetProject(project);

            // Assign director to editor
            editor.SetDirector(director);
        }

        [TearDown]
        public void Teardown()
        {
            if (editorGO != null) Object.DestroyImmediate(editorGO);
            if (director != null && director.gameObject != null) Object.DestroyImmediate(director.gameObject);
        }

        [Test]
        public void ZoomToFit_CalculatesCorrectZoom()
        {
            // Arrange
            // Set view width via style (since layout system isn't running fully)
            var timelineScrollView = uiDoc.rootVisualElement.Q<ScrollView>("timeline-scroll");
            Assert.IsNotNull(timelineScrollView, "ScrollView not found");

            float viewWidth = 500f;
            timelineScrollView.style.width = viewWidth;

            // Set pixels per second via reflection if private (default is 100)

            // Act
            // Invoke ZoomToFit
            var zoomMethod = typeof(TimelineEditorUIToolkit).GetMethod("ZoomToFit", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(zoomMethod, "ZoomToFit method not found");
            zoomMethod.Invoke(editor, null);

            // Assert
            // Target Zoom = 500 / (10 * 50) = 1.0 (assuming BasePixelsPerSecond = 50)
            // Apply 0.95 margin
            float expectedZoom = 1.0f * 0.95f; // 0.95f

            // Allow small error for float comparison
            Assert.AreEqual(expectedZoom, editor.CurrentZoom, 0.001f, "Zoom level incorrect");
        }

        [Test]
        public void ZoomToFit_ClampsToMinZoom()
        {
            // Arrange
            var timelineScrollView = uiDoc.rootVisualElement.Q<ScrollView>("timeline-scroll");
            float viewWidth = 50f; // Very small view
            timelineScrollView.style.width = viewWidth;

            // Director length 100s. Base PPS 100.
            // Target Zoom = 50 / (100 * 100) = 50 / 10000 = 0.005
            // Min Zoom is 0.1f

            // Update director length
            director.Project.length = 100f;

            // Act
            var zoomMethod = typeof(TimelineEditorUIToolkit).GetMethod("ZoomToFit", BindingFlags.NonPublic | BindingFlags.Instance);
            zoomMethod.Invoke(editor, null);

            // Assert
            Assert.AreEqual(0.1f, editor.CurrentZoom, 0.001f, "Zoom level should be clamped to min");
        }

        [Test]
        public void ZoomToFit_ClampsToMaxZoom()
        {
            // Arrange
            var timelineScrollView = uiDoc.rootVisualElement.Q<ScrollView>("timeline-scroll");
            float viewWidth = 10000f; // Huge view
            timelineScrollView.style.width = viewWidth;

            // Director length 1s. Base PPS 100.
            // Target Zoom = 10000 / (1 * 100) = 100
            // Max Zoom is 5f

            // Update director length
            director.Project.length = 1f;

            // Act
            var zoomMethod = typeof(TimelineEditorUIToolkit).GetMethod("ZoomToFit", BindingFlags.NonPublic | BindingFlags.Instance);
            zoomMethod.Invoke(editor, null);

            // Assert
            Assert.AreEqual(5f, editor.CurrentZoom, 0.001f, "Zoom level should be clamped to max");
        }
    }
}
