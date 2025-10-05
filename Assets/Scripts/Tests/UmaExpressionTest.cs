using UnityEngine;
using UMA.CharacterSystem;
using MiniTimeline.Core;
using MiniTimeline.Tracks;
using System.Collections.Generic;

public class UmaExpressionTest : MonoBehaviour
{
    void Start()
    {
        // Create UMA Avatar
        var avatarGO = new GameObject("TestUmaAvatar");
        var dca = avatarGO.AddComponent<DynamicCharacterAvatar>();
        avatarGO.AddComponent<UMAExpressionPlayer>();

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

        // Create UMA Expression Track
        var expressionTrack = new UMAExpressionTrack
        {
            Id = "expressionTrack1",
            BindKey = "TestUmaAvatar" // Binding to the UMA avatar GameObject
        };

        // Create UMA Expression Clip
        var expressionClip = new UMAExpressionClip
        {
            Id = "expressionClip1",
            Start = 1f,
            Duration = 5f,
            expression = "shy_smile"
        };
        expressionTrack.AddClip(expressionClip);

        // Add track to project and assign to director
        var trackData = new TrackData
        {
            id = expressionTrack.Id,
            type = MiniTimelineConstants.TRACK_UMA_EXPRESSION,
            bindKey = expressionTrack.BindKey,
            clips = new List<ClipData>
            {
                new ClipData
                {
                    id = expressionClip.Id,
                    start = expressionClip.Start,
                    duration = expressionClip.Duration,
                    payload = new Dictionary<string, object>
                    {
                        { "expression", expressionClip.expression }
                    }
                }
            }
        };
        project.tracks.Add(trackData);

        director.SetProject(project);
        director.Bind("TestUmaAvatar", avatarGO);

        // Play the timeline
        director.Play();

        Debug.Log("UMA Expression Test Setup Complete. Timeline is playing.");
    }
}