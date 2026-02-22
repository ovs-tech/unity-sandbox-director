using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core.Components;

namespace Systems.PlacementSystem.Editor
{
    [CustomEditor(typeof(PlacementPart))]
    public class PartEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var placeable = (PlacementPart)target;
            EditorGUILayout.HelpBox($"Rules: {placeable.GetRules().Count}", MessageType.Info);
            
            if (GUILayout.Button("Open in Prefab Creator", GUILayout.Height(30)))
            {
                var window = EditorWindow.GetWindow(typeof(PrefabCreatorWindow)) as PrefabCreatorWindow;
                if (window != null)
                {
                    window.minSize = new Vector2(400, 600);
                    // Provide a way to select the prefab
                    UnityEditor.Selection.activeObject = placeable.gameObject;
                }
            }
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            var placeable = (PlacementPart)target;
            if (placeable == null) return;

            var sockets = placeable.GetComponentsInChildren<Systems.PlacementSystem.Sockets.Socket>();
            
            foreach (var socket in sockets)
            {
                EditorGUI.BeginChangeCheck();
                
                // Draw a label above the handle
                Handles.Label(socket.transform.position + Vector3.up * 0.2f, socket.gameObject.name);

                // Draw the position handle
                Vector3 newPos = Handles.PositionHandle(socket.transform.position, socket.transform.rotation);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(socket.transform, "Move Socket");
                    socket.transform.position = newPos;
                }
            }
        }
    }
}
