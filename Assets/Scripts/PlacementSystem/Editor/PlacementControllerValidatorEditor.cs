using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Editor
{
    [CustomEditor(typeof(PlacementController))]
    public class PlacementControllerValidatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var validatorProperty = serializedObject.FindProperty("_placementValidatorAsset");
            if (validatorProperty != null && validatorProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "No validator asset assigned. PlacementController will fallback to DefaultPlacementValidator at runtime.",
                    MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
