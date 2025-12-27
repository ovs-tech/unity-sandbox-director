using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit.MVVM
{
    /// <summary>
    /// Data model for form submit MVVM flow. Holds field configs and captured values without Unity dependencies.
    /// </summary>
    [Serializable]
    public class FormSubmitModel
    {
        private readonly List<FormFieldDefinition> _fieldConfigs;
        private readonly Dictionary<string, object> _fieldValues;

        public event Action<string, object> OnFieldValueChanged;
        public event Action<IReadOnlyDictionary<string, object>> OnFormDataChanged;

        public string Title { get; set; }
        public IReadOnlyList<FormFieldDefinition> FieldConfigs => _fieldConfigs;
        public IReadOnlyDictionary<string, object> FieldValues => new ReadOnlyDictionary<string, object>(_fieldValues);

        public FormSubmitModel(string title, List<FormFieldDefinition> fieldConfigs)
        {
            Title = title ?? string.Empty;
            _fieldConfigs = fieldConfigs ?? new List<FormFieldDefinition>();
            _fieldValues = new Dictionary<string, object>();
            SeedDefaultValues();
        }

        public FormSubmitModel() : this(string.Empty, new List<FormFieldDefinition>())
        {
        }

        public void SetFieldValue(string fieldName, object value)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
            }

            var definition = FindDefinition(fieldName);
            if (definition == null)
            {
                // Allow dynamic fields but still record value
                _fieldValues[fieldName] = value;
                OnFieldValueChanged?.Invoke(fieldName, value);
                OnFormDataChanged?.Invoke(new ReadOnlyDictionary<string, object>(_fieldValues));
                return;
            }

            if (!TryCoerceValue(definition, value, out var coercedValue))
            {
                throw new InvalidOperationException($"Value for field '{fieldName}' is not compatible with type '{definition.type}'.");
            }

            _fieldValues[fieldName] = coercedValue;
            OnFieldValueChanged?.Invoke(fieldName, coercedValue);
            OnFormDataChanged?.Invoke(new ReadOnlyDictionary<string, object>(_fieldValues));
        }

        public IReadOnlyDictionary<string, object> GetAllFieldValues()
        {
            return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(_fieldValues));
        }

        public void ClearFieldValues()
        {
            _fieldValues.Clear();
            SeedDefaultValues();
            OnFormDataChanged?.Invoke(new ReadOnlyDictionary<string, object>(_fieldValues));
        }

        private void SeedDefaultValues()
        {
            foreach (var definition in _fieldConfigs.Where(def => !string.IsNullOrWhiteSpace(def.name)))
            {
                _fieldValues[definition.name] = definition.defaultValue;
            }
        }

        private FormFieldDefinition FindDefinition(string fieldName)
        {
            return _fieldConfigs.FirstOrDefault(def => string.Equals(def.name, fieldName, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryCoerceValue(FormFieldDefinition definition, object value, out object coercedValue)
        {
            coercedValue = value;
            if (definition == null)
            {
                return true;
            }

            var normalizedType = (definition.type ?? string.Empty).ToLowerInvariant();
            switch (normalizedType)
            {
                case "number":
                case "slider":
                    if (value == null)
                    {
                        coercedValue = null;
                        return true;
                    }

                    if (value is IConvertible)
                    {
                        try
                        {
                            coercedValue = Convert.ToDouble(value);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    return false;

                case "toggle":
                case "checkbox":
                    if (value == null)
                    {
                        coercedValue = false;
                        return true;
                    }

                    if (value is bool boolValue)
                    {
                        coercedValue = boolValue;
                        return true;
                    }

                    if (bool.TryParse(value.ToString(), out var parsedBool))
                    {
                        coercedValue = parsedBool;
                        return true;
                    }
                    return false;

                case "select":
                case "selectbox":
                case "text":
                case "textarea":
                case "info":
                case "hidden":
                case "button":
                case "color":
                    coercedValue = value?.ToString();
                    return true;

                default:
                    coercedValue = value;
                    return true;
            }
        }
    }
}
