using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.Serialization;
using Systems.MiniTimeline.Tracks;

namespace Systems.MiniTimeline.Tests
{
    public class SerializationTest : MonoBehaviour
    {
        public void RunTest()
        {
            Debug.Log("[SerializationTest] Starting test...");

            // 1. Create a project with various tracks
            var project = new MiniTimelineProject
            {
                name = "Test Project",
                length = 20f,
                frameRate = 60f
            };

            // Add Metadata
            project.metadata.zoom = 1.5f;
            project.metadata.editorData.Add("TestString", "Hello");
            project.metadata.editorData.Add("TestInt", 42);
            project.metadata.editorData.Add("TestBool", true);

            // Add Movement Track
            var moveTrack = new MovementTrack
            {
                Id = "MoveTrack1",
                Name = "Movement",
                Enabled = true
            };
            var moveClip = new MovementClip
            {
                Id = "Clip1",
                Start = 0f,
                Duration = 5f,
                hasPosition = true,
                startPosition = Vector3.zero,
                endPosition = Vector3.one
            };
            moveTrack.AddClip(moveClip);
            project.tracks.Add(moveTrack);

            // Add Signal Track
            var signalTrack = new SignalTrack
            {
                Id = "SignalTrack1",
                Name = "Signals"
            };
            var signalClip = new SignalClip
            {
                Id = "Signal1",
                Start = 2f,
                eventId = "OnTest",
                payload = "Payload"
            };
            signalTrack.AddClip(signalClip);
            project.tracks.Add(signalTrack);

            // Add Morph Track
            var morphTrack = new MorphTrack
            {
                Id = "MorphTrack1",
                Name = "Morphs"
            };
            var morphClip = new MorphKeyClip
            {
                Id = "MorphClip1",
                Start = 5f,
                Duration = 2f,
                keys = new List<MorphKey>
                {
                    new MorphKey { id = "Smile", startValue = 0f, endValue = 100f }
                }
            };
            morphTrack.AddClip(morphClip);
            project.tracks.Add(morphTrack);

            // 2. Serialize
            string json = ProjectSerializer.SaveToJson(project);
            Debug.Log($"[SerializationTest] Serialized JSON length: {json.Length}");

            // 3. Deserialize
            var loadedProject = ProjectSerializer.LoadFromJson(json);

            // 4. Verify
            if (loadedProject == null)
            {
                Debug.LogError("[SerializationTest] Failed to load project!");
                return;
            }

            Debug.Log($"[SerializationTest] Loaded Name: {loadedProject.name}");
            Debug.Log($"[SerializationTest] Loaded Tracks: {loadedProject.tracks.Count}");

            if (loadedProject.tracks.Count != 3)
            {
                Debug.LogError($"[SerializationTest] Track count mismatch! Expected 3, got {loadedProject.tracks.Count}");
            }

            var loadedMoveTrack = loadedProject.tracks[0] as MovementTrack;
            if (loadedMoveTrack != null)
            {
                Debug.Log("[SerializationTest] MovementTrack loaded successfully");
                
                int clipCount = 0;
                foreach(var c in loadedMoveTrack.GetClips()) clipCount++;
                
                if (clipCount != 1) Debug.LogError($"[SerializationTest] MovementTrack clip count mismatch! Expected 1, got {clipCount}");
                else Debug.Log("[SerializationTest] MovementTrack clips OK");
            }
            else
            {
                Debug.LogError("[SerializationTest] First track is not MovementTrack");
            }
            
            var loadedMorphTrack = loadedProject.tracks[2] as MorphTrack;
            if (loadedMorphTrack != null)
            {
                Debug.Log("[SerializationTest] MorphTrack loaded successfully");
                var clips = loadedMorphTrack.GetClips().ToList();
                if (clips.Count == 1 && clips[0] is MorphKeyClip mkc)
                {
                    if (mkc.keys.Count == 1 && mkc.keys[0].id == "Smile")
                        Debug.Log("[SerializationTest] MorphKeyClip data OK");
                    else
                        Debug.LogError("[SerializationTest] MorphKeyClip data mismatch");
                }
                else
                {
                    Debug.LogError("[SerializationTest] MorphTrack clips mismatch");
                }
            }
            else
            {
                Debug.LogError("[SerializationTest] Third track is not MorphTrack");
            }

            // Verify Metadata
            if (loadedProject.metadata.editorData.ContainsKey("TestString"))
            {
                string val = loadedProject.metadata.editorData["TestString"] as string;
                if (val == "Hello") Debug.Log("[SerializationTest] Metadata String OK");
                else Debug.LogError($"[SerializationTest] Metadata String mismatch: {val}");
            }
            else
            {
                Debug.LogError("[SerializationTest] Metadata key 'TestString' missing");
            }
            
            if (loadedProject.metadata.editorData.ContainsKey("TestInt"))
            {
                object val = loadedProject.metadata.editorData["TestInt"];
                Debug.Log($"[SerializationTest] Metadata Int Type: {val.GetType()} Value: {val}");
            }

            Debug.Log("[SerializationTest] Test Complete.");
        }

        private void Start()
        {
            RunTest();
        }
    }
}
