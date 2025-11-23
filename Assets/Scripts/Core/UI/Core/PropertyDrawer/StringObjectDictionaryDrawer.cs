using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StringObjectDictionary))]
    public class StringObjectDictionaryDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 20f;
        private const float SPACING = 2f;
        private const float BUTTON_WIDTH = 60f;
        private const float REMOVE_BUTTON_WIDTH = 25f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var keysProperty = property.FindPropertyRelative("keys");
            var valuesProperty = property.FindPropertyRelative("values");
            
            var rect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
            
            // Header with foldout
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            rect.y += LINE_HEIGHT + SPACING;
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Info line
                var infoRect = new Rect(rect.x, rect.y, rect.width, LINE_HEIGHT);
                EditorGUI.LabelField(infoRect, $"Binding Context ({keysProperty.arraySize} bindings)", EditorStyles.miniLabel);
                rect.y += LINE_HEIGHT + SPACING;
                
                // Add button
                var addButtonRect = new Rect(rect.x, rect.y, BUTTON_WIDTH, LINE_HEIGHT);
                if (GUI.Button(addButtonRect, "Add"))
                {
                    keysProperty.InsertArrayElementAtIndex(keysProperty.arraySize);
                    valuesProperty.InsertArrayElementAtIndex(valuesProperty.arraySize);
                    
                    var newKeyProp = keysProperty.GetArrayElementAtIndex(keysProperty.arraySize - 1);
                    newKeyProp.stringValue = GetUniqueKey(keysProperty);
                    
                    var newValueProp = valuesProperty.GetArrayElementAtIndex(valuesProperty.arraySize - 1);
                    newValueProp.objectReferenceValue = null;
                }
                
                rect.y += LINE_HEIGHT + SPACING;
                
                // Headers for Key and Value columns
                if (keysProperty.arraySize > 0)
                {
                    var keyHeaderRect = new Rect(rect.x, rect.y, rect.width * 0.35f - 5f, LINE_HEIGHT);
                    var valueHeaderRect = new Rect(rect.x + rect.width * 0.35f, rect.y, rect.width * 0.55f - 5f, LINE_HEIGHT);
                    
                    EditorGUI.LabelField(keyHeaderRect, "Binding Key", EditorStyles.miniLabel);
                    EditorGUI.LabelField(valueHeaderRect, "Bound Object", EditorStyles.miniLabel);
                    rect.y += LINE_HEIGHT + SPACING;
                }
                
                // Draw existing entries
                for (int i = 0; i < keysProperty.arraySize; i++)
                {
                    DrawEntry(rect, keysProperty, valuesProperty, i);
                    rect.y += LINE_HEIGHT + SPACING;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        private void DrawEntry(Rect rect, SerializedProperty keysProperty, SerializedProperty valuesProperty, int index)
        {
            var keyProperty = keysProperty.GetArrayElementAtIndex(index);
            var valueProperty = valuesProperty.GetArrayElementAtIndex(index);
            
            var keyRect = new Rect(rect.x, rect.y, rect.width * 0.35f - 5f, LINE_HEIGHT);
            var valueRect = new Rect(rect.x + rect.width * 0.35f, rect.y, rect.width * 0.55f - 5f, LINE_HEIGHT);
            var removeRect = new Rect(rect.x + rect.width * 0.9f, rect.y, REMOVE_BUTTON_WIDTH, LINE_HEIGHT);
            
            // Key field with validation
            EditorGUI.BeginChangeCheck();
            string newKey = EditorGUI.TextField(keyRect, keyProperty.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                // Validate key uniqueness
                if (!HasDuplicateKey(keysProperty, newKey, index))
                {
                    keyProperty.stringValue = newKey;
                }
                else
                {
                    Debug.LogWarning($"Binding key '{newKey}' already exists. Please use a unique key.");
                }
            }
            
            // Value field with type filtering
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
            
            // Remove button
            if (GUI.Button(removeRect, "×"))
            {
                keysProperty.DeleteArrayElementAtIndex(index);
                valuesProperty.DeleteArrayElementAtIndex(index);
            }
        }
        
        private string GetUniqueKey(SerializedProperty keysProperty)
        {
            string baseName = "new_binding";
            string uniqueName = baseName;
            int counter = 1;
            
            while (HasKey(keysProperty, uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }
            
            return uniqueName;
        }
        
        private bool HasKey(SerializedProperty keysProperty, string key)
        {
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                var keyProp = keysProperty.GetArrayElementAtIndex(i);
                if (keyProp.stringValue == key)
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool HasDuplicateKey(SerializedProperty keysProperty, string key, int excludeIndex)
        {
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                if (i == excludeIndex) continue;
                
                var keyProp = keysProperty.GetArrayElementAtIndex(i);
                if (keyProp.stringValue == key)
                {
                    return true;
                }
            }
            return false;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return LINE_HEIGHT;
            }
            
            var keysProperty = property.FindPropertyRelative("keys");
            int entries = keysProperty.arraySize;
            
            float height = LINE_HEIGHT; // Foldout header
            height += LINE_HEIGHT + SPACING; // Info line
            height += LINE_HEIGHT + SPACING; // Add button
            
            if (entries > 0)
            {
                height += LINE_HEIGHT + SPACING; // Column headers
                height += (LINE_HEIGHT + SPACING) * entries; // Entries
            }
            
            return height;
        }
    }