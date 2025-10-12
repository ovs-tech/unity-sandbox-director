using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.FormSubmit.Fields
{
    /// <summary>
    /// Button form field that triggers actions
    /// </summary>
    public class ButtonFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition fieldDefinition;
        private Button button;
        private TextMeshProUGUI buttonText;
        private string actionValue;
        
        public void Initialize(FormFieldDefinition definition)
        {
            fieldDefinition = definition;
            CreateUI();
            
            // Get action from options
            if (definition.options != null && definition.options.ContainsKey("action"))
            {
                actionValue = definition.options["action"].ToString();
            }
        }
        
        private void CreateUI()
        {
            // Safety check for fieldDefinition
            if (fieldDefinition == null)
            {
                // Debug.LogError("ButtonFormField.CreateUI: fieldDefinition is null");
                return;
            }
            
            // Get or add RectTransform (don't add if it already exists)
            var rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }
            
            // Create button
            button = gameObject.AddComponent<Button>();
            var buttonImage = gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.6f, 0.9f, 0.8f);
            
            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = fieldDefinition.defaultValue?.ToString() ?? fieldDefinition.label;
            buttonText.fontSize = 14f;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            // Handle button click
            button.onClick.AddListener(() =>
            {
                // For button fields, we set a special action value that the form handler can detect
                if (!string.IsNullOrEmpty(actionValue))
                {
                    // Create a special form data entry that indicates this was a button click
                    var formPanel = GetComponentInParent<FormSubmitPanel>();
                    if (formPanel != null)
                    {
                        var formData = new Dictionary<string, object>
                        {
                            ["action"] = actionValue,
                            [fieldDefinition.name] = actionValue
                        };
                        
                        // Check if this button should close the form after action
                        bool shouldCloseForm = true;
                        if (fieldDefinition.options != null && fieldDefinition.options.ContainsKey("closeForm"))
                        {
                            bool.TryParse(fieldDefinition.options["closeForm"].ToString(), out shouldCloseForm);
                        }
                        
                        if (shouldCloseForm)
                        {
                            // Use the custom submission method which closes the form
                            formPanel.HandleCustomSubmission(formData);
                        }
                        else
                        {
                            // Trigger the event without closing the form
                            formPanel.TriggerFormSubmission(formData);
                        }
                    }
                }
            });
        }
        
        public object GetValue()
        {
            return actionValue ?? fieldDefinition.name;
        }
        
        public void SetValue(object value)
        {
            // Buttons don't have settable values
        }
        
        public string GetName()
        {
            return fieldDefinition?.name ?? "unknown";
        }
        
        public bool IsValid()
        {
            return true; // Buttons are always valid
        }
        
        public void Focus()
        {
            button?.Select();
        }
    }
}