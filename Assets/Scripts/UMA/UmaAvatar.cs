using UnityEngine;
using UMA.CharacterSystem;
using System.Collections.Generic;

public class UmaAvatar : MonoBehaviour
{
    private DynamicCharacterAvatar avatar;

    void Awake()
    {
        avatar = GetComponent<DynamicCharacterAvatar>();
    }

    public void SetWardrobe(Dictionary<string, string> recipes)
    {
        if (avatar == null)
        {
            Debug.LogError("DynamicCharacterAvatar not found on this GameObject.");
            return;
        }

        // Clear existing wardrobe
        avatar.ClearSlots();

        // Apply new wardrobe
        foreach (var recipe in recipes)
        {
            avatar.SetSlot(recipe.Key, recipe.Value);
        }

        avatar.BuildCharacter();
    }

    public void SetColors(Dictionary<string, Color> colors)
    {
        if (avatar == null)
        {
            Debug.LogError("DynamicCharacterAvatar not found on this GameObject.");
            return;
        }

        // Apply each color using the correct UMA API
        foreach (var color in colors)
        {
            avatar.SetColorValue(color.Key, color.Value);
        }

        avatar.BuildCharacter();
    }
}