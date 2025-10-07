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

    [System.Serializable]
    private class WardrobeData { public List<WardrobeItem> wardrobe; public List<ColorItem> colors; }
    [System.Serializable]
    private class WardrobeItem { public string slot; public string recipe; }
    [System.Serializable]
    private class ColorItem { public string name; public string color; }

    private DynamicCharacterAvatar avatar;

    void Awake()
    {
        avatar = GetComponent<DynamicCharacterAvatar>();
    }

    public void SetWardrobe(Dictionary<string, string> recipes, bool buildCharacter = true)
    {
        if (avatar == null)
        {
            Debug.LogError("DynamicCharacterAvatar not found on this GameObject.");
            return;
        }

        foreach (var recipe in recipes)
        {
            avatar.SetSlot(recipe.Key, recipe.Value);
        }

        if (buildCharacter)
        {
            avatar.BuildCharacter();
        }
    }

    public void SetColors(Dictionary<string, Color> colors, bool buildCharacter = true)
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

        if (buildCharacter)
        {
            avatar.BuildCharacter();
        }
    }

    public void LoadWardrobeFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<WardrobeData>(json);

            if (data.wardrobe != null && data.wardrobe.Count > 0)
            {
                var wardrobeRecipes = new Dictionary<string, string>();
                foreach (var item in data.wardrobe)
                {
                    wardrobeRecipes[item.slot] = item.recipe;
                }
                SetWardrobe(wardrobeRecipes, false);
            }

            if (data.colors != null && data.colors.Count > 0)
            {
                var wardrobeColors = new Dictionary<string, Color>();
                foreach (var item in data.colors)
                {
                    if (ColorUtility.TryParseHtmlString(item.color, out Color color))
                    {
                        wardrobeColors[item.name] = color;
                    }
                }
                SetColors(wardrobeColors, false);
            }

            avatar.BuildCharacter();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UmaAvatar] Failed to parse wardrobe JSON: {e.Message}");
        }
    }
}