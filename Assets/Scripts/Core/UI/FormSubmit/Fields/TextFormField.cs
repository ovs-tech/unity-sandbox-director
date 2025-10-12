using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.FormSubmit.Fields
{

    /// <summary>
    /// Text input form field
    /// </summary>
    public class TextFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private TMP_InputField inputField;
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

            // Input field
            var inputObj = new GameObject("Input");
            inputObj.transform.SetParent(transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.5f);
            inputRect.sizeDelta = Vector2.zero;
            inputRect.anchoredPosition = Vector2.zero;

            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            inputField = inputObj.AddComponent<TMP_InputField>();

            // Text component for input field
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "";
            textMesh.fontSize = 14f;
            textMesh.color = Color.white;

            inputField.textComponent = textMesh;
            inputField.targetGraphic = inputImage;

            // Set placeholder
            if (!string.IsNullOrEmpty(definition.placeholder))
            {
                var placeholderObj = new GameObject("Placeholder");
                placeholderObj.transform.SetParent(inputObj.transform, false);
                var placeholderRect = placeholderObj.AddComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;
                placeholderRect.offsetMin = new Vector2(10, 0);
                placeholderRect.offsetMax = new Vector2(-10, 0);

                var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
                placeholderText.text = definition.placeholder;
                placeholderText.fontSize = 14f;
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);

                inputField.placeholder = placeholderText;
            }

            // Set default value
            if (definition.defaultValue != null)
            {
                inputField.text = definition.defaultValue.ToString();
            }
        }

        public object GetValue()
        {
            return inputField.text;
        }

        public void SetValue(object value)
        {
            inputField.text = value?.ToString() ?? "";
        }

        public string GetName()
        {
            return definition.name;
        }

        public bool IsValid()
        {
            if (definition.required && string.IsNullOrEmpty(inputField.text))
                return false;

            return true;
        }

        public void Focus()
        {
            inputField.Select();
        }
    }

}