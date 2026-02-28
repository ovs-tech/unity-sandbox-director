using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Utility class for creating and managing UI Toolkit forms
    /// Provides helper methods for common form operations
    /// </summary>
    public static class FormSubmitUIToolkitUtils
    {
        /// <summary>
        /// Create a quick form using the UI Toolkit panel
        /// </summary>
        public static void ShowQuickForm(string title, List<FormFieldDefinition> fields, 
            Action<Dictionary<string, object>> onSubmit = null, Action onCancel = null, Transform parent = null)
        {
            var panel = FormSubmitPanelUIToolkit.Instance;
            panel.Show(title, fields, onSubmit, onCancel, parent);
        }

        /// <summary>
        /// Create a simple text input form
        /// </summary>
        public static void ShowTextInputForm(string title, string fieldName, string fieldLabel, 
            string defaultValue = "", Action<string> onSubmit = null, Action onCancel = null, Transform parent = null)
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition(fieldName, fieldLabel, "text", defaultValue)
                {
                    required = true
                }
            };

            ShowQuickForm(title, fields, 
                data => onSubmit?.Invoke(data[fieldName]?.ToString() ?? ""),
                onCancel, parent);
        }

        /// <summary>
        /// Create a confirmation dialog using the form system
        /// </summary>
        public static void ShowConfirmationDialog(string title, string message, 
            Action onConfirm = null, Action onCancel = null, Transform parent = null)
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("message", "", "info", message),
                new FormFieldDefinition("confirm", "Confirm", "button", "Confirm")
                {
                    options = new Dictionary<string, object> { { "action", "confirm" } }
                }
            };

            ShowQuickForm(title, fields, 
                data => {
                    if (data.ContainsKey("confirm"))
                    {
                        onConfirm?.Invoke();
                    }
                },
                onCancel, parent);
        }

        /// <summary>
        /// Load UXML and USS resources for the form panel
        /// </summary>
        public static (VisualTreeAsset uxml, StyleSheet uss) LoadFormResources()
        {
            var uxml = Resources.Load<VisualTreeAsset>("FormSubmitPanel");
            var uss = Resources.Load<StyleSheet>("FormSubmitPanel");
            
            if (uxml == null)
            {
                Debug.LogWarning("FormSubmitPanel.uxml not found in Resources folder");
            }
            
            if (uss == null)
            {
                Debug.LogWarning("FormSubmitPanel.uss not found in Resources folder");
            }

            return (uxml, uss);
        }

        /// <summary>
        /// Validate form data against field definitions
        /// </summary>
        public static bool ValidateFormData(Dictionary<string, object> formData, 
            List<FormFieldDefinition> fieldDefinitions, out List<string> errors)
        {
            errors = new List<string>();
            bool isValid = true;

            foreach (var fieldDef in fieldDefinitions)
            {
                if (fieldDef.required)
                {
                    if (!formData.ContainsKey(fieldDef.name) || 
                        string.IsNullOrEmpty(formData[fieldDef.name]?.ToString()))
                    {
                        errors.Add($"{fieldDef.label} is required");
                        isValid = false;
                    }
                }

                // Validate number ranges
                if (fieldDef.type.ToLower() == "number" && formData.ContainsKey(fieldDef.name))
                {
                    if (float.TryParse(formData[fieldDef.name].ToString(), out float value))
                    {
                        if (fieldDef.options != null)
                        {
                            if (fieldDef.options.ContainsKey("min") && 
                                float.TryParse(fieldDef.options["min"].ToString(), out float min) && 
                                value < min)
                            {
                                errors.Add($"{fieldDef.label} must be at least {min}");
                                isValid = false;
                            }

                            if (fieldDef.options.ContainsKey("max") && 
                                float.TryParse(fieldDef.options["max"].ToString(), out float max) && 
                                value > max)
                            {
                                errors.Add($"{fieldDef.label} must be at most {max}");
                                isValid = false;
                            }
                        }
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Convert form data to a specific type using reflection
        /// </summary>
        public static T ConvertFormDataToObject<T>(Dictionary<string, object> formData) where T : new()
        {
            var result = new T();
            var type = typeof(T);

            foreach (var kvp in formData)
            {
                var property = type.GetProperty(kvp.Key);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                        property.SetValue(result, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to convert {kvp.Key} to {property.PropertyType}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Create field definitions from an object's properties
        /// </summary>
        public static List<FormFieldDefinition> CreateFieldDefinitionsFromObject<T>(T obj = default(T))
        {
            var fields = new List<FormFieldDefinition>();
            var type = typeof(T);

            foreach (var property in type.GetProperties())
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                string fieldType = GetFieldTypeFromPropertyType(property.PropertyType);
                object defaultValue = obj != null ? property.GetValue(obj) : GetDefaultValueForType(property.PropertyType);

                var field = new FormFieldDefinition(
                    property.Name,
                    FormatPropertyNameAsLabel(property.Name),
                    fieldType,
                    defaultValue
                );

                fields.Add(field);
            }

            return fields;
        }

        private static string GetFieldTypeFromPropertyType(Type propertyType)
        {
            if (propertyType == typeof(string)) return "text";
            if (propertyType == typeof(int) || propertyType == typeof(float) || propertyType == typeof(double)) return "number";
            if (propertyType == typeof(bool)) return "toggle";
            if (propertyType == typeof(Color)) return "color";
            
            return "text"; // Default fallback
        }

        private static object GetDefaultValueForType(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private static string FormatPropertyNameAsLabel(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return "";

            // Convert PascalCase to space-separated words
            var result = System.Text.RegularExpressions.Regex.Replace(
                propertyName, 
                "([a-z])([A-Z])", 
                "$1 $2"
            );

            // Capitalize first letter
            if (result.Length > 0)
                result = char.ToUpper(result[0]) + result.Substring(1);

            return result;
        }
    }
}