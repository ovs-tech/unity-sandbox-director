using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Editor
{
    [CustomEditor(typeof(PlacementController))]
    public class PlacementControllerEditor : UnityEditor.Editor
    {
        private bool _showCore = true;
        private bool _showPlacement = true;
        private bool _showSelection = true;
        private bool _showEvents = false;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Core Setup
            _showCore = EditorGUILayout.Foldout(_showCore, "1. Core Setup", true, EditorStyles.foldoutHeader);
            if (_showCore)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_objectToPlace"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_placementCamera"));
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_placementStrategy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_placementVisualizer"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_inputProviderComponent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_snapManager"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);
            
            // Placement Settings
            _showPlacement = EditorGUILayout.Foldout(_showPlacement, "2. Placement Settings", true, EditorStyles.foldoutHeader);
            if (_showPlacement)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxRaycastDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_placementSurface"));
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationSnapDegrees"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationIncrement"));
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_makeObjectsSelectable"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Selection Settings
            _showSelection = EditorGUILayout.Foldout(_showSelection, "3. Selection Settings", true, EditorStyles.foldoutHeader);
            if (_showSelection)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_selectionLayer"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_selectionMovementSurface"));
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_allowMultiSelection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_requireModifierForMultiSelect"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_moveSelectedWithPointer"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);

            // Events
            _showEvents = EditorGUILayout.Foldout(_showEvents, "4. Events", true, EditorStyles.foldoutHeader);
            if (_showEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlacementStartedEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlacementSuccessEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlacementFailedEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPlacementCancelledEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnToolChangedEvent"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            // Runtime details
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var controller = (PlacementController)target;
                
                var activeTool = "Unknown";
                // if (controller.ToolManager != null && controller.ToolManager.ActiveTool != null)
                // {
                //     activeTool = controller.ToolManager.ActiveTool.GetType().Name;
                // }
                
                EditorGUILayout.LabelField($"Active Tool: {activeTool}");
                
                int placedCount = 0;
                foreach(var obj in controller.GetPlacedObjects()) { if(obj != null) placedCount++; }
                EditorGUILayout.LabelField($"Placed Objects: {placedCount}");
                
                EditorGUILayout.EndVertical();
            }
        }
    }
}
