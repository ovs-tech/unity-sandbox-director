using UnityEngine;
using UMA.CharacterSystem;
using MiniTimeline.Core;
using MiniTimeline.Tracks;
using System.Collections.Generic;

public class UmaWardrobeTest : MonoBehaviour
{
    void Start()
    {
        // Create UMA Avatar
        var avatarGO = new GameObject("TestUmaAvatar");
        var dca = avatarGO.AddComponent<DynamicCharacterAvatar>();
        avatarGO.AddComponent<UmaAvatar>();

        // Set up a basic UMA character
        dca.umaData = ScriptableObject.CreateInstance<UMA.UMAData>();
        dca.umaData.umaRecipe = ScriptableObject.CreateInstance<UMA.UMARecipeBase>();
        // NOTE: In a real scenario, you would assign a valid UMA recipe here.
        // For this test, we are just ensuring the components are present.

        // Create Timeline Director
        var directorGO = new GameObject("TestTimelineDirector");
        var director = directorGO.AddComponent<MiniTimelineDirector>();

        // Create Timeline Project
        var project = ScriptableObject.CreateInstance<MiniTimelineProject>();
        project.length = 10f;

        // Create UMA Wardrobe Track
        var wardrobeTrack = new UmaWardrobeTrack
        {
            Id = "wardrobeTrack1",
            BindKey = "TestUmaAvatar" // Binding to the UMA avatar GameObject
        };

        // Create UMA Wardrobe Clip
        var wardrobeClip = new UmaWardrobeClip
        {
            Id = "wardrobeClip1",
            Start = 1f,
            Duration = 5f,
            wardrobeJson = @"{
                ""wardrobe"": [
                    { ""slot"": ""Chest"", ""recipe"": ""MaleShirt"" },
                    { ""slot"": ""Legs"", ""recipe"": ""MalePants"" }
                ],
                ""colors"": [
                    { ""name"": ""ShirtColor"", ""color"": ""#FF0000"" }
                ]
            }"
        };
        wardrobeTrack.AddClip(wardrobeClip);

        // Add track to project and assign to director
        var trackData = new TrackData
        {
            id = wardrobeTrack.Id,
            type = MiniTimelineConstants.TRACK_UMA_WARDROBE,
            bindKey = wardrobeTrack.BindKey,
            clips = new List<ClipData>
            {
                new ClipData
                {
                    id = wardrobeClip.Id,
                    start = wardrobeClip.Start,
                    duration = wardrobeClip.Duration,
                    payload = new Dictionary<string, object>
                    {
                        { "wardrobeJson", wardrobeClip.wardrobeJson }
                    }
                }
            }
        };
        project.tracks.Add(trackData);

        director.SetProject(project);
        director.Bind("TestUmaAvatar", avatarGO);

        // Play the timeline
        director.Play();

        Debug.Log("UMA Wardrobe Test Setup Complete. Timeline is playing.");
    }
}