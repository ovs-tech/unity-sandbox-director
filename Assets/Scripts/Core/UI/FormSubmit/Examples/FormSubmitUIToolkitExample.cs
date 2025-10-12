using System.Collections.Generic;
using UnityEngine;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit.Examples
{
    /// <summary>
    /// Example demonstrating how to use the UI Toolkit FormSubmitPanel
    /// This replaces the uGUI version with modern UI Toolkit implementation
    /// </summary>
    public class FormSubmitUIToolkitExample : MonoBehaviour
    {
        [Header("Example Data")]
        public string characterName = "NewCharacter";
        public float characterAge = 25f;
        public bool isPlayerCharacter = true;

        void Start()
        {
            // Example 1: Simple form creation
            ShowCharacterCreationForm();
        }

        void Update()
        {
            // Demo controls
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowCharacterCreationForm();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ShowTextInputExample();
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ShowConfirmationExample();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                ShowComplexFormExample();
            }
        }

        public void ShowCharacterCreationForm()
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Character Name", "text", characterName)
                {
                    required = true,
                    placeholder = "Enter character name",
                    tooltip = "The display name for your character"
                },
                
                new FormFieldDefinition("age", "Age", "number", characterAge)
                {
                    required = true,
                    placeholder = "25",
                    tooltip = "Character's age in years",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 200f }
                    }
                },
                
                new FormFieldDefinition("isPlayer", "Is Player Character", "toggle", isPlayerCharacter)
                {
                    tooltip = "Whether this character is controlled by the player"
                },
                
                new FormFieldDefinition("class", "Character Class", "select", "Warrior")
                {
                    required = true,
                    tooltip = "Choose the character's class",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Warrior", "Mage", "Rogue", "Cleric", "Ranger" } }
                    }
                },
                
                new FormFieldDefinition("strength", "Strength", "slider", 10f)
                {
                    tooltip = "Character's physical strength",
                    options = new Dictionary<string, object>
                    {
                        { "min", 1f },
                        { "max", 20f }
                    }
                }
            };

            FormSubmitUIToolkitUtils.ShowQuickForm(
                "Create Character", 
                fields, 
                OnCharacterFormSubmitted, 
                OnCharacterFormCancelled
            );
        }

        public void ShowTextInputExample()
        {
            FormSubmitUIToolkitUtils.ShowTextInputForm(
                "Enter Name",
                "playerName",
                "Player Name",
                "DefaultPlayer",
                OnPlayerNameSubmitted,
                () => Debug.Log("Name input cancelled")
            );
        }

        public void ShowConfirmationExample()
        {
            FormSubmitUIToolkitUtils.ShowConfirmationDialog(
                "Confirm Action",
                "Are you sure you want to delete this character? This action cannot be undone.",
                () => Debug.Log("Character deleted!"),
                () => Debug.Log("Deletion cancelled")
            );
        }

        public void ShowComplexFormExample()
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("title", "Form Title", "info", "Advanced Character Settings"),
                
                new FormFieldDefinition("description", "Description", "textarea", "Enter a detailed character background...")
                {
                    placeholder = "Describe your character's backstory, motivations, and goals..."
                },
                
                new FormFieldDefinition("favoriteColor", "Favorite Color", "color", Color.blue),
                
                new FormFieldDefinition("hidden_id", "Character ID", "hidden", System.Guid.NewGuid().ToString()),
                
                new FormFieldDefinition("save", "Save Character", "button", "Save")
                {
                    options = new Dictionary<string, object> { { "action", "save" } }
                },
                
                new FormFieldDefinition("preview", "Preview Character", "button", "Preview")
                {
                    options = new Dictionary<string, object> { { "action", "preview" } }
                }
            };

            FormSubmitUIToolkitUtils.ShowQuickForm(
                "Advanced Settings",
                fields,
                OnAdvancedFormSubmitted,
                () => Debug.Log("Advanced form cancelled")
            );
        }

        private void OnCharacterFormSubmitted(Dictionary<string, object> formData)
        {
            Debug.Log("Character form submitted:");
            foreach (var kvp in formData)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            // Update local variables
            if (formData.ContainsKey("name"))
                characterName = formData["name"].ToString();
            
            if (formData.ContainsKey("age") && float.TryParse(formData["age"].ToString(), out float age))
                characterAge = age;
            
            if (formData.ContainsKey("isPlayer") && bool.TryParse(formData["isPlayer"].ToString(), out bool isPlayer))
                isPlayerCharacter = isPlayer;
        }

        private void OnCharacterFormCancelled()
        {
            Debug.Log("Character creation cancelled");
        }

        private void OnPlayerNameSubmitted(string playerName)
        {
            Debug.Log($"Player name set to: {playerName}");
        }

        private void OnAdvancedFormSubmitted(Dictionary<string, object> formData)
        {
            Debug.Log("Advanced form submitted:");
            
            if (formData.ContainsKey("save"))
            {
                Debug.Log("Saving character...");
                // Implement save logic
            }
            else if (formData.ContainsKey("preview"))
            {
                Debug.Log("Previewing character...");
                // Implement preview logic
            }

            foreach (var kvp in formData)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }

        void OnGUI()
        {
            // Show instructions
            GUI.Box(new Rect(10, 10, 300, 120), "UI Toolkit Form Examples");
            GUI.Label(new Rect(20, 35, 280, 20), "F1 - Character Creation Form");
            GUI.Label(new Rect(20, 55, 280, 20), "F2 - Text Input Example");
            GUI.Label(new Rect(20, 75, 280, 20), "F3 - Confirmation Dialog");
            GUI.Label(new Rect(20, 95, 280, 20), "F4 - Complex Form Example");
            GUI.Label(new Rect(20, 115, 280, 20), "Check Console for results");
        }
    }
}