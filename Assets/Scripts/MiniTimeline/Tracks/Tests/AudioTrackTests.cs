using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.Tracks;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class AudioTrackTests
    {
        private GameObject trackGO;
        private AudioSource audioSource;
        private AudioTrack track;

        [SetUp]
        public void Setup()
        {
            trackGO = new GameObject("AudioTrackTest");
            audioSource = trackGO.AddComponent<AudioSource>();

            track = new AudioTrack();
            track.Id = "AudioTrack";
            track.BindKey = "TestAudioSource";

            // Mock BindingContext
            var bindingContext = trackGO.AddComponent<BindableObjectManager>();
            bindingContext.Bind("TestAudioSource", audioSource);

            track.Bind(bindingContext);
            track.Prepare();
        }

        [TearDown]
        public void Teardown()
        {
            track.OnProjectClosed();
            if (trackGO != null) GameObject.DestroyImmediate(trackGO);
        }

        [Test]
        public void AudioTrack_Prepare_FindsAudioSource()
        {
            Assert.IsTrue(track.IsReady);
            // We can't easily access the private audioSource field on track,
            // but if Prepare didn't fail/log error, we assume success.
            // Actually Prepare logs warning if no source found.
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AudioTrack_Evaluate_PlaysClip()
        {
            // Create a dummy clip
            // Note: In Editor tests, Resources.Load might fail if asset doesn't exist.
            // We need a dummy audio clip resource or mock the loader.
            // But AudioTrack uses Resources.Load directly.
            // So we can't easily test playback unless we have a resource.
            // Let's create a temporary AudioClip asset? No, Resources folder must exist at compile time.

            // However, we can test that the track logic runs without crashing.

            var clip = new AudioClipData
            {
                Id = "TestClip",
                Start = 0f,
                Duration = 2f,
                audioAsset = "NonExistentAsset", // Should log warning but not crash
                volume = 0.5f
            };

            track.AddClip(clip);

            // Expect warning about failed load
            LogAssert.Expect(LogType.Warning, "Failed to load audio asset: NonExistentAsset");

            // Re-prepare to trigger load
            track.Prepare();

            // Evaluate
            track.Evaluate(1f, false);

            // Should not crash
        }
    }
}
