using UnityEngine;
using UnityEditor;
using SceneSandbox.Core;

namespace SceneSandbox.Editor
{
    [CustomEditor(typeof(TransformableItem))]
    [CanEditMultipleObjects]
    public class TransformableItemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Don't show test buttons when multiple objects are selected
            if (targets.Length > 1)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Gizmo control is only available for single object selection.", MessageType.Info);
                return;
            }

            TransformableItem item = (TransformableItem)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gizmo Control", EditorStyles.boldLabel);

            // In Edit Mode, allow changing transform mode to see different gizmos
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Select the object to see gizmos in Scene view.\n" +
                    "Change Transform Mode to see different gizmo types:",
                    MessageType.Info
                );

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Position Gizmo"))
                {
                    Undo.RecordObject(item, "Set Transform Mode");
                    item.SetTransformModeType(TransformModeType.Position);
                    EditorUtility.SetDirty(item);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Rotation Gizmo"))
                {
                    Undo.RecordObject(item, "Set Transform Mode");
                    item.SetTransformModeType(TransformModeType.Rotation);
                    EditorUtility.SetDirty(item);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Scale Gizmo"))
                {
                    Undo.RecordObject(item, "Set Transform Mode");
                    item.SetTransformModeType(TransformModeType.Scale);
                    EditorUtility.SetDirty(item);
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Play Mode controls
                EditorGUILayout.HelpBox("Play Mode: Use buttons to test gizmo functionality", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select & Show Gizmo"))
                {
                    item.SetSelectedState(true);
                    item.SetTransformModeType(TransformModeType.Position);
                    Debug.Log("Object selected and Position mode activated");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Position Mode"))
                {
                    item.SetTransformModeType(TransformModeType.Position);
                }
                if (GUILayout.Button("Rotation Mode"))
                {
                    item.SetTransformModeType(TransformModeType.Rotation);
                }
                if (GUILayout.Button("Scale Mode"))
                {
                    item.SetTransformModeType(TransformModeType.Scale);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Deselect"))
                {
                    item.SetSelectedState(false);
                    item.SetTransformModeType(TransformModeType.None);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
