#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Systems.CommandSystem.Editor
{
    public class CommandManagerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string selectedNamespace = CommandManager.DEFAULT_NAMESPACE;

        [MenuItem("Tools/Command System/Command Manager History")]
        public static void ShowWindow()
        {
            var window = GetWindow<CommandManagerWindow>("Command History");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Command Manager only tracks history during Play Mode.", MessageType.Info);
                return;
            }

            var manager = CommandManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("CommandManager instance not found.", MessageType.Warning);
                return;
            }

            GUILayout.Label("Command Manager History", EditorStyles.boldLabel);

            // Namespace Selection
            var namespaces = manager.ActiveNamespaces.ToList();
            if (!namespaces.Contains(selectedNamespace))
            {
                namespaces.Add(selectedNamespace);
            }
            namespaces.Sort();

            int selectedIndex = namespaces.IndexOf(selectedNamespace);
            selectedIndex = EditorGUILayout.Popup("Namespace", selectedIndex, namespaces.ToArray());
            if (selectedIndex >= 0 && selectedIndex < namespaces.Count)
            {
                selectedNamespace = namespaces[selectedIndex];
            }

            EditorGUILayout.Space();

            // Actions for the selected namespace
            EditorGUILayout.LabelField($"Actions for: {selectedNamespace}", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            GUI.enabled = manager.CanUndoForNamespace(selectedNamespace);
            if (GUILayout.Button($"Undo ({manager.UndoStackCountForNamespace(selectedNamespace)})"))
            {
                manager.Undo(selectedNamespace);
            }
            GUI.enabled = manager.CanRedoForNamespace(selectedNamespace);
            if (GUILayout.Button($"Redo ({manager.RedoStackCountForNamespace(selectedNamespace)})"))
            {
                manager.Redo(selectedNamespace);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Clear History"))
            {
                manager.Clear(selectedNamespace);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Global Actions
            EditorGUILayout.LabelField("Global Actions", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUI.enabled = manager.CanUndo;
            if (GUILayout.Button($"Global Undo ({manager.UndoStackCount})"))
            {
                manager.Undo();
            }
            GUI.enabled = manager.CanRedo;
            if (GUILayout.Button($"Global Redo ({manager.RedoStackCount})"))
            {
                manager.Redo();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Clear Global History"))
            {
                manager.Clear();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // History List
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Undo History", EditorStyles.boldLabel);
            var undoHistory = manager.GetUndoHistory(selectedNamespace);
            if (undoHistory == null || undoHistory.Length == 0)
            {
                EditorGUILayout.LabelField("  [Empty]", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < undoHistory.Length; i++)
                {
                    EditorGUILayout.LabelField($"{i + 1}. {undoHistory[i]}");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Redo History", EditorStyles.boldLabel);
            var redoHistory = manager.GetRedoHistory(selectedNamespace);
            if (redoHistory == null || redoHistory.Length == 0)
            {
                EditorGUILayout.LabelField("  [Empty]", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < redoHistory.Length; i++)
                {
                    EditorGUILayout.LabelField($"{i + 1}. {redoHistory[i]}");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
