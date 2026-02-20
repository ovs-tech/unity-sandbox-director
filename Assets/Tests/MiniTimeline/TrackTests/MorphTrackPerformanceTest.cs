using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Systems.MiniTimeline.Tracks;
using UnityEngine;

namespace MiniTimeline.Tracks.Tests
{
    public class MorphTrackPerformanceTest
    {
        private MorphTrack track;
        private MethodInfo processOverrideClipsMethod;
        private List<IMorphClip> clips;
        private GameObject targetObject;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("Target");
            targetObject.AddComponent<SkinnedMeshRenderer>();

            track = new MorphTrack();
            // Need to set target object or prepare track so it doesn't return early?
            // ProcessOverrideClips takes clips as argument, so it doesn't depend on track state directly for clips,
            // but it writes to accumulatedValues.

            // We need to bypass OnPrepare or simulate it?
            // Reflection to set internal fields?
            // Actually ProcessOverrideClips is private and takes (List<IMorphClip> activeClips, float time)

            processOverrideClipsMethod = typeof(MorphTrack).GetMethod("ProcessOverrideClips", BindingFlags.NonPublic | BindingFlags.Instance);

            clips = new List<IMorphClip>();
            for (int i = 0; i < 100; i++)
            {
                var clip = new MorphKeyClip();
                clip.blendMode = MorphBlendMode.Override;
                clip.priority = i % 10;
                clip.Start = 0;
                clip.Duration = 10;
                // Add some keys
                clip.keys.Add(new MorphKey { id = "Head", startValue = 0, endValue = 100 });
                clips.Add(clip);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (targetObject != null)
                GameObject.DestroyImmediate(targetObject);
        }

        [Test]
        public void BenchmarkProcessOverrideClips_Allocations()
        {
            // Warmup
            processOverrideClipsMethod.Invoke(track, new object[] { clips, 5f });

            long startMem = GC.GetAllocatedBytesForCurrentThread();

            int iterations = 1000;
            for (int i = 0; i < iterations; i++)
            {
                 processOverrideClipsMethod.Invoke(track, new object[] { clips, 5f });
            }

            long endMem = GC.GetAllocatedBytesForCurrentThread();
            long totalAlloc = endMem - startMem;

            Debug.Log($"Total allocated bytes for {iterations} iterations: {totalAlloc}");
            Debug.Log($"Average bytes per call: {totalAlloc / (float)iterations}");

            // Assert that we are within budget.
            // Currently it allocates:
            // 1. List<IMorphClip> (Where(...).ToList())
            // 2. Iterator (Where(...))
            // 3. HashSet<string>
            // 4. Enumerator for HashSet? No.
            // 5. IMorphClip.GetAllMorphValues allocates Dictionary.

            // We want to reduce this. Ideally to 0 if possible, but IMorphClip implementation is out of scope?
            // No, I can modify MorphTrack.cs to fix the method.

            // With optimizations, we expect significant reduction.
        }
    }
}
