using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Helpers
{
    /// <summary>
    /// Dynamically controls component properties via string expressions.
    /// Expression format: [ComponentName].[Property] = [Value]
    /// Example: "NavMeshAgent.isStopped=true"
    /// Can be called from UnityEvents in the Inspector.
    /// </summary>
    public class DynamicComponentController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Expression to execute when ControlDefault is called from UnityEvent")]
        private string _defaultExpression = "";

        /// <summary>
        /// Parses and executes a property assignment expression.
        /// This method can be called from UnityEvents with a string parameter.
        /// </summary>
        /// <param name="expression">Expression in format "ComponentName.Property=Value"</param>
        public void Control(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                Debug.LogError("DynamicComponentController: Expression is null or empty.");
                return;
            }

            // Remove whitespace
            expression = expression.Replace(" ", "");

            // Split by '=' to get left side (ComponentName.Property) and right side (Value)
            string[] parts = expression.Split('=');
            if (parts.Length != 2)
            {
                Debug.LogError($"DynamicComponentController: Invalid expression format. Expected 'ComponentName.Property=Value', got '{expression}'");
                return;
            }

            string leftSide = parts[0];
            string valueStr = parts[1];

            // Split left side by '.' to get component name and property name
            string[] leftParts = leftSide.Split('.');
            if (leftParts.Length != 2)
            {
                Debug.LogError($"DynamicComponentController: Invalid left side format. Expected 'ComponentName.Property', got '{leftSide}'");
                return;
            }

            string componentName = leftParts[0];
            string propertyName = leftParts[1];

            SetComponentProperty(componentName, propertyName, valueStr);
        }

        /// <summary>
        /// Executes the default expression set in the Inspector.
        /// Use this for UnityEvents that don't support parameters.
        /// </summary>
        public void ControlDefault()
        {
            Control(_defaultExpression);
        }

        /// <summary>
        /// Sets a component property by name.
        /// </summary>
        private void SetComponentProperty(string componentName, string propertyName, string valueStr)
        {
            // Get the component by name
            Component component = GetComponent(componentName);
            if (component == null)
            {
                Debug.LogError($"DynamicComponentController: Component '{componentName}' not found on GameObject '{gameObject.name}'");
                return;
            }

            Type componentType = component.GetType();

            // Try to find property first
            PropertyInfo propertyInfo = componentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                SetProperty(component, propertyInfo, valueStr);
                return;
            }

            // Try to find field if property not found
            FieldInfo fieldInfo = componentType.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                SetField(component, fieldInfo, valueStr);
                return;
            }

            Debug.LogError($"DynamicComponentController: Property or field '{propertyName}' not found or not writable on component '{componentName}'");
        }

        /// <summary>
        /// Sets a property value with type conversion.
        /// </summary>
        private void SetProperty(Component component, PropertyInfo propertyInfo, string valueStr)
        {
            try
            {
                object convertedValue = ConvertValue(valueStr, propertyInfo.PropertyType);
                propertyInfo.SetValue(component, convertedValue);
                Debug.Log($"DynamicComponentController: Set {component.GetType().Name}.{propertyInfo.Name} = {convertedValue}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"DynamicComponentController: Failed to set property '{propertyInfo.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a field value with type conversion.
        /// </summary>
        private void SetField(Component component, FieldInfo fieldInfo, string valueStr)
        {
            try
            {
                object convertedValue = ConvertValue(valueStr, fieldInfo.FieldType);
                fieldInfo.SetValue(component, convertedValue);
                Debug.Log($"DynamicComponentController: Set {component.GetType().Name}.{fieldInfo.Name} = {convertedValue}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"DynamicComponentController: Failed to set field '{fieldInfo.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Converts string value to target type.
        /// </summary>
        private object ConvertValue(string valueStr, Type targetType)
        {
            // Handle bool
            if (targetType == typeof(bool))
            {
                return bool.Parse(valueStr);
            }

            // Handle int
            if (targetType == typeof(int))
            {
                return int.Parse(valueStr);
            }

            // Handle float
            if (targetType == typeof(float))
            {
                return float.Parse(valueStr);
            }

            // Handle double
            if (targetType == typeof(double))
            {
                return double.Parse(valueStr);
            }

            // Handle string
            if (targetType == typeof(string))
            {
                return valueStr;
            }

            // Handle Vector3
            if (targetType == typeof(Vector3))
            {
                return ParseVector3(valueStr);
            }

            // Handle Vector2
            if (targetType == typeof(Vector2))
            {
                return ParseVector2(valueStr);
            }

            // Handle Color
            if (targetType == typeof(Color))
            {
                return ParseColor(valueStr);
            }

            // Handle enums
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, valueStr, true);
            }

            // Fallback to Convert.ChangeType
            return Convert.ChangeType(valueStr, targetType);
        }

        /// <summary>
        /// Parses a Vector3 from string format "(x,y,z)" or "x,y,z".
        /// </summary>
        private Vector3 ParseVector3(string str)
        {
            str = str.Trim('(', ')');
            string[] parts = str.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException($"Invalid Vector3 format: {str}");
            }

            return new Vector3(
                float.Parse(parts[0]),
                float.Parse(parts[1]),
                float.Parse(parts[2])
            );
        }

        /// <summary>
        /// Parses a Vector2 from string format "(x,y)" or "x,y".
        /// </summary>
        private Vector2 ParseVector2(string str)
        {
            str = str.Trim('(', ')');
            string[] parts = str.Split(',');
            if (parts.Length != 2)
            {
                throw new FormatException($"Invalid Vector2 format: {str}");
            }

            return new Vector2(
                float.Parse(parts[0]),
                float.Parse(parts[1])
            );
        }

        /// <summary>
        /// Parses a Color from hex string "#RRGGBB" or named color.
        /// </summary>
        private Color ParseColor(string str)
        {
            if (ColorUtility.TryParseHtmlString(str, out Color color))
            {
                return color;
            }

            // Try named colors
            PropertyInfo colorProperty = typeof(Color).GetProperty(str, BindingFlags.Public | BindingFlags.Static);
            if (colorProperty != null)
            {
                return (Color)colorProperty.GetValue(null);
            }

            throw new FormatException($"Invalid Color format: {str}");
        }
    }
}
