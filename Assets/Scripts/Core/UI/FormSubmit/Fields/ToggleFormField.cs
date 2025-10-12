using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.FormSubmit.Fields
{
    /// <summary>
    /// Toggle/Checkbox form field
    /// </summary>
    public class ToggleFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private Toggle toggle;
        private TextMeshProUGUI label;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 40);
            
            // Toggle
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(transform, false);
            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 0);
            toggleRect.anchorMax = new Vector2(1, 1);
            toggleRect.sizeDelta = Vector2.zero;
            toggleRect.anchoredPosition = Vector2.zero;
            
            toggle = toggleObj.AddComponent<Toggle>();
            
            // Background
            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(toggleObj.transform, false);
            var backgroundRect = backgroundObj.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0.5f);
            backgroundRect.anchorMax = new Vector2(0, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(20, 20);
            
            var backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Checkmark
            var checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(backgroundObj.transform, false);
            var checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.sizeDelta = Vector2.zero;
            checkmarkRect.anchoredPosition = Vector2.zero;
            
            var checkmarkImage = checkmarkObj.AddComponent<Image>();
            checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            checkmarkImage.sprite = Resources.Load<Sprite>("UI/Skin/Checkmark");
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Setup toggle
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            
            // Set default value
            if (definition.defaultValue is bool boolValue)
            {
                toggle.isOn = boolValue;
            }
        }
        
        public object GetValue()
        {
            return toggle.isOn;
        }
        
        public void SetValue(object value)
        {
            if (value is bool boolValue)
                toggle.isOn = boolValue;
            else
                toggle.isOn = false;
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            return true; // Toggles are always valid
        }
        
        public void Focus()
        {
            toggle.Select();
        }
    }
}