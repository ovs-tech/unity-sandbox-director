using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Track for playing Audio clips
    /// </summary>
    [Serializable]
    public class AudioTrack : MiniTrackBase<AudioClipData>
    {
        private AudioSource audioSource;
        private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();

        protected override void OnPrepare()
        {
            // Resolve AudioSource
            if (targetObject != null)
            {
                if (targetObject is GameObject go)
                {
                    audioSource = go.GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = go.AddComponent<AudioSource>();
                    }
                }
                else if (targetObject is AudioSource source)
                {
                    audioSource = source;
                }
                else if (targetObject is Component comp)
                {
                    audioSource = comp.GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = comp.gameObject.AddComponent<AudioSource>();
                    }
                }
            }

            // If still null, we might need a dummy object or just warn
            if (audioSource == null)
            {
                Debug.LogWarning($"[AudioTrack] No AudioSource found for track '{Id}'. Audio will not play.");
            }
            else
            {
                audioSource.playOnAwake = false;
            }

            // Load clips
            LoadAssets();
        }

        private void LoadAssets()
        {
            foreach (var clip in clips)
            {
                if (string.IsNullOrEmpty(clip.audioAsset)) continue;

                if (!loadedClips.ContainsKey(clip.audioAsset))
                {
                    // Simple Resources.Load for now.
                    // To support Addressables, we'd need async loading which is more complex to fit into this sync flow without pre-loading.
                    // For "Mini" timeline, Resources is often sufficient or we can add Addressable support later.
                    // The AnimTrack does it async. I'll stick to Resources for simplicity in this first pass
                    // unless the string starts with "addr:".

                    if (clip.audioAsset.StartsWith(MiniTimelineConstants.ASSET_ADDRESSABLE))
                    {
                        Debug.LogWarning($"[AudioTrack] Addressable audio not fully supported in synchronous load: {clip.audioAsset}");
                    }
                    else
                    {
                        var audioClip = Resources.Load<AudioClip>(clip.audioAsset);
                        if (audioClip != null)
                        {
                            loadedClips[clip.audioAsset] = audioClip;
                        }
                        else
                        {
                            Debug.LogWarning($"[AudioTrack] Failed to load audio asset: {clip.audioAsset}");
                        }
                    }
                }
            }
        }

        protected override void OnEvaluate(float time, bool scrub)
        {
            if (audioSource == null) return;

            // Simple logic: Find the clip that should be playing
            // AudioSource can only play one clip. We pick the one that started most recently or has highest weight/priority.
            // For simplicity: Iterate clips, find first active one.

            AudioClipData activeClip = null;

            foreach (var clip in clips)
            {
                if (clip.Contains(time))
                {
                    activeClip = clip;
                    break; // Just take the first one found
                }
            }

            if (activeClip != null)
            {
                if (loadedClips.TryGetValue(activeClip.audioAsset, out var audioClip))
                {
                    float clipTime = time - activeClip.Start;

                    // Pitch scaling affects time
                    // But we want to seek to the correct position regardless of pitch?
                    // Usually timeline controls absolute time. Pitch speeds up playback.
                    // If pitch is 2, 1 second of timeline = 2 seconds of audio?
                    // No, usually pitch is just an effect.
                    // If we scrub, we set time.

                    if (audioSource.clip != audioClip)
                    {
                        audioSource.clip = audioClip;
                        audioSource.loop = activeClip.loop;
                        audioSource.Play();
                    }

                    if (!audioSource.isPlaying)
                    {
                        audioSource.Play();
                    }

                    // Sync time if drifting or scrubbing
                    if (scrub || Mathf.Abs(audioSource.time - clipTime) > 0.1f)
                    {
                        // Loop handling for time
                        if (activeClip.loop && audioClip.length > 0)
                        {
                            clipTime = clipTime % audioClip.length;
                        }

                        audioSource.time = Mathf.Clamp(clipTime, 0, audioClip.length);
                    }

                    // Volume blending
                    float volume = activeClip.volume;

                    // Fade In
                    if (activeClip.fadeIn > 0 && clipTime < activeClip.fadeIn)
                    {
                        volume *= (clipTime / activeClip.fadeIn);
                    }

                    // Fade Out
                    float timeLeft = activeClip.Duration - (time - activeClip.Start);
                    if (activeClip.fadeOut > 0 && timeLeft < activeClip.fadeOut)
                    {
                        volume *= (timeLeft / activeClip.fadeOut);
                    }

                    audioSource.volume = Mathf.Clamp01(volume);
                    audioSource.pitch = activeClip.pitch;
                }
            }
            else
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }
            }
        }

        protected override void OnExit(float time, bool scrub)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
