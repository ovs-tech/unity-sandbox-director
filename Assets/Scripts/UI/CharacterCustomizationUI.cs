using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class CharacterCustomizationUI : MonoBehaviour
{
    public UmaAvatar targetAvatar;

    [Header("UI Elements")]
    public Dropdown hairDropdown;
    public Dropdown chestDropdown;
    public Dropdown legsDropdown;
    public Dropdown feetDropdown;
    public Dropdown colorDropdown;

    // Sample data for available wardrobe recipes
    private Dictionary<string, List<string>> wardrobeOptions = new Dictionary<string, List<string>>
    {
        { "Hair", new List<string> { "None", "MaleHair1", "FemaleHair1" } },
        { "Chest", new List<string> { "None", "MaleShirt", "FemaleTop" } },
        { "Legs", new List<string> { "None", "MalePants", "FemaleSkirt" } },
        { "Feet", new List<string> { "None", "MaleShoes", "FemaleBoots" } }
    };

    // Sample data for available colors
    private Dictionary<string, Color> colorOptions = new Dictionary<string, Color>
    {
        { "Red", Color.red },
        { "Green", Color.green },
        { "Blue", Color.blue },
        { "White", Color.white },
        { "Black", Color.black }
    };

    void Start()
    {
        if (targetAvatar == null)
        {
            Debug.LogError("Target UMA Avatar is not assigned in the CharacterCustomizationUI.");
            enabled = false;
            return;
        }

        SetupWardrobeDropdown(hairDropdown, "Hair");
        SetupWardrobeDropdown(chestDropdown, "Chest");
        SetupWardrobeDropdown(legsDropdown, "Legs");
        SetupWardrobeDropdown(feetDropdown, "Feet");
        SetupColorDropdown(colorDropdown);
    }

    private void SetupWardrobeDropdown(Dropdown dropdown, string slot)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(wardrobeOptions[slot]);
        dropdown.onValueChanged.AddListener(delegate { OnWardrobeChanged(slot, dropdown); });
    }

    private void SetupColorDropdown(Dropdown dropdown)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(colorOptions.Keys.ToList());
        dropdown.onValueChanged.AddListener(delegate { OnColorChanged(dropdown); });
    }

    public void OnWardrobeChanged(string slot, Dropdown dropdown)
    {
        if (targetAvatar == null) return;

        string recipe = dropdown.options[dropdown.value].text;
        if (recipe == "None")
        {
            recipe = ""; // Use an empty string to clear the slot
        }

        var wardrobeUpdate = new Dictionary<string, string>
        {
            { slot, recipe }
        };
        targetAvatar.SetWardrobe(wardrobeUpdate);
    }

    public void OnColorChanged(Dropdown dropdown)
    {
        if (targetAvatar == null) return;

        string colorName = dropdown.options[dropdown.value].text;
        Color selectedColor = colorOptions[colorName];

        // This is a simplified approach. A real implementation would need to know
        // which shared color to target (e.g., "Hair", "Shirt", "Pants").
        // For this example, we'll just target a generic "MainColor".
        var colorUpdate = new Dictionary<string, Color>
        {
            { "MainColor", selectedColor }
        };
        targetAvatar.SetColors(colorUpdate);
    }

    public void SetWardrobe(string slot, string recipe)
    {
        if (targetAvatar == null) return;

        var wardrobeUpdate = new Dictionary<string, string>
        {
            { slot, recipe }
        };
        targetAvatar.SetWardrobe(wardrobeUpdate);
    }

    public void SetColor(string colorName, Color color)
    {
        if (targetAvatar == null) return;

        var colorUpdate = new Dictionary<string, Color>
        {
            { colorName, color }
        };
        targetAvatar.SetColors(colorUpdate);
    }
}