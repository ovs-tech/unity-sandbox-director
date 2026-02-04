using NUnit.Framework;
using UnityEngine;
using Systems.MiniTimeline.UI.Input;
using System;
using UnityEngine.TestTools;

namespace Systems.MiniTimeline.UI.Input.Tests
{
    public class InputInteractionManagerPerformanceTests
    {
        private class MockInteraction : IInputInteraction
        {
            public bool IsActive { get; set; } = false;
            public int Priority { get; set; } = 0;

            public void OnInteractionStart(Vector2 screenPosition) { IsActive = true; }
            public void OnInteractionUpdate(Vector2 screenPosition, float deltaTime) { }
            public void OnInteractionEnd(Vector2 screenPosition) { IsActive = false; }
            public void OnInteractionCancel() { IsActive = false; }
        }

        [Test]
        public void HasActiveInteraction_DoesNotAllocate()
        {
            var manager = new InputInteractionManager();
            manager.AddInteraction(new MockInteraction { Priority = 1, IsActive = true });
            manager.AddInteraction(new MockInteraction { Priority = 2, IsActive = false });

            // Warmup
            var active = manager.HasActiveInteraction;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long startMem = GC.GetTotalMemory(false);

            for (int i = 0; i < 1000; i++)
            {
                active = manager.HasActiveInteraction;
            }

            long endMem = GC.GetTotalMemory(false);
            long diff = endMem - startMem;

            // Should be 0
            Assert.Less(diff, 100, $"HasActiveInteraction allocated {diff} bytes");
        }

        [Test]
        public void OnPressUpdate_DoesNotAllocate()
        {
            var manager = new InputInteractionManager();
            manager.AddInteraction(new MockInteraction { Priority = 1 });
            manager.OnPressStart(Vector2.zero);

            // Warmup
            manager.OnPressUpdate(Vector2.zero, 0.1f);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long startMem = GC.GetTotalMemory(false);

            for (int i = 0; i < 1000; i++)
            {
                 manager.OnPressUpdate(Vector2.zero, 0.1f);
            }

            long endMem = GC.GetTotalMemory(false);
            long diff = endMem - startMem;

            Assert.Less(diff, 100, $"OnPressUpdate allocated {diff} bytes");
        }
    }
}
