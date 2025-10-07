using UnityEngine;
using UMA;
using UMA.CharacterSystem;
using System.Collections.Generic;

public class UmaAvatar : MonoBehaviour
{
    public enum WardrobeSlot
    {
        Hair,
        Chest,
        Legs,
        Feet
    }

    public static readonly List<string> WardrobeSlotNames = new List<string>
    {
        "Hair",
        "Chest",
        "Legs",
        "Feet"
    };

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

        // The SetSlot method is used to apply wardrobe recipes.
        // This approach allows for incremental changes to the avatar's wardrobe,
        // such as changing only the shirt without affecting the pants.
        // To clear a specific slot, an empty or null string can be passed as the recipe value.
        // This is in contrast to ClearSlots(), which would remove all wardrobe items.
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

        var overlayColorData = new UMA.OverlayColorData[colors.Count];
        int i = 0;
        foreach (var color in colors)
        {
            overlayColorData[i] = new UMA.OverlayColorData(color.Key, color.Value);
            i++;
        }

        avatar.SetColors(overlayColorData);
        avatar.BuildCharacter();
    }
}