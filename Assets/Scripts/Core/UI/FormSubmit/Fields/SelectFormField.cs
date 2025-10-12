using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.FormSubmit.Fields
{
    /// <summary>
    /// Select/Dropdown form field
    /// </summary>
    public class SelectFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private TMP_Dropdown dropdown;
        private TextMeshProUGUI label;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 60);
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            
            // Dropdown
            var dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(transform, false);
            var dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0, 0);
            dropdownRect.anchorMax = new Vector2(1, 0.5f);
            dropdownRect.sizeDelta = Vector2.zero;
            dropdownRect.anchoredPosition = Vector2.zero;
            
            var dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = dropdownImage;
            
            // Label for dropdown
            var dropdownLabelObj = new GameObject("Label");
            dropdownLabelObj.transform.SetParent(dropdownObj.transform, false);
            var dropdownLabelRect = dropdownLabelObj.AddComponent<RectTransform>();
            dropdownLabelRect.anchorMin = new Vector2(0, 0);
            dropdownLabelRect.anchorMax = new Vector2(1, 1);
            dropdownLabelRect.sizeDelta = Vector2.zero;
            dropdownLabelRect.offsetMin = new Vector2(10, 0);
            dropdownLabelRect.offsetMax = new Vector2(-25, 0);
            
            var dropdownLabelText = dropdownLabelObj.AddComponent<TextMeshProUGUI>();
            dropdownLabelText.text = "";
            dropdownLabelText.fontSize = 14f;
            dropdownLabelText.color = Color.white;
            dropdownLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            dropdown.captionText = dropdownLabelText;
            
            // Arrow
            var arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            var arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-12.5f, 0);
            arrowRect.sizeDelta = new Vector2(20, 20);
            
            var arrowImage = arrowObj.AddComponent<Image>();
            arrowImage.color = Color.white;
            arrowImage.sprite = Resources.Load<Sprite>("UI/Skin/DropdownArrow");
            
            // Template (for dropdown list)
            var templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            var templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.anchoredPosition = new Vector2(0, -25);
            templateRect.sizeDelta = new Vector2(0, 50);
            
            templateObj.SetActive(false);
            
            var templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            var templateScrollRect = templateObj.AddComponent<ScrollRect>();
            
            // Viewport
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            viewportRect.offsetMin = new Vector2(5, 5);
            viewportRect.offsetMax = new Vector2(-5, -5);
            
            var viewportMask = viewportObj.AddComponent<Mask>();
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);
            
            // Content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            
            // Item
            var itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0, 40);
            
            var itemToggle = itemObj.AddComponent<Toggle>();
            
            // Item Background
            var itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            var itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            itemBgRect.anchoredPosition = Vector2.zero;
            
            var itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Item Label
            var itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.sizeDelta = Vector2.zero;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);
            
            var itemLabelText = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabelText.text = "";
            itemLabelText.fontSize = 14f;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            itemToggle.targetGraphic = itemBgImage;
            
            // Setup dropdown references
            dropdown.template = templateRect;
            dropdown.captionText = dropdownLabelText;
            dropdown.itemText = itemLabelText;
            
            templateScrollRect.content = contentRect;
            templateScrollRect.viewport = viewportRect;
            templateScrollRect.horizontal = false;
            templateScrollRect.vertical = true;
            
            // Populate options
            PopulateOptions();
        }
        
        private void PopulateOptions()
        {
            dropdown.options.Clear();
            
            if (definition.options != null && definition.options.ContainsKey("items"))
            {
                if (definition.options["items"] is List<string> stringOptions)
                {
                    foreach (var option in stringOptions)
                    {
                        dropdown.options.Add(new TMP_Dropdown.OptionData(option));
                    }
                }
                else if (definition.options["items"] is List<object> objectOptions)
                {
                    foreach (var option in objectOptions)
                    {
                        dropdown.options.Add(new TMP_Dropdown.OptionData(option.ToString()));
                    }
                }
            }
            
            dropdown.RefreshShownValue();
            
            // Set default value
            if (definition.defaultValue != null)
            {
                var defaultStr = definition.defaultValue.ToString();
                var index = dropdown.options.FindIndex(o => o.text == defaultStr);
                if (index >= 0)
                {
                    dropdown.value = index;
                }
            }
        }
        
        public object GetValue()
        {
            if (dropdown.value >= 0 && dropdown.value < dropdown.options.Count)
                return dropdown.options[dropdown.value].text;
            return "";
        }
        
        public void SetValue(object value)
        {
            var valueStr = value?.ToString() ?? "";
            var index = dropdown.options.FindIndex(o => o.text == valueStr);
            if (index >= 0)
            {
                dropdown.value = index;
            }
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            if (definition.required && dropdown.value < 0)
                return false;
            
            return true;
        }
        
        public void Focus()
        {
            dropdown.Select();
        }
    }
}