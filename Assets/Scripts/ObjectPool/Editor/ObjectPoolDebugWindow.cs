#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Systems.ObjectPool.Editor
{
    /// <summary>
    /// Editor window for visualizing and debugging object pools
    /// </summary>
    public class ObjectPoolDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.5; // seconds

        [MenuItem("Window/Object Pool Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<ObjectPoolDebugWindow>("Object Pool Debugger");
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawPoolStats();
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Repaint();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(110));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear All Pools", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Clear All Pools",
                    "Are you sure you want to clear all object pools? This will destroy all pooled instances.",
                    "Clear", "Cancel"))
                {
                    Systems.ObjectPool.ObjectPool.ClearAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPoolStats()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var stats = Systems.ObjectPool.ObjectPool.GetAllNamespaceStats();

            if (stats == null || stats.Length == 0)
            {
                EditorGUILayout.HelpBox("No active object pools. Pools are created when you first spawn objects.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Total Namespaces: {stats.Length}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                foreach (var ns in stats)
                {
                    DrawNamespaceStats(ns);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNamespaceStats(Systems.ObjectPool.PoolNamespaceStats stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Namespace: {stats.NamespaceName}", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Clear Namespace",
                    $"Clear all pools in namespace '{stats.NamespaceName}'? This will destroy all pooled instances in that namespace.",
                    "Clear", "Cancel"))
                {
                    Systems.ObjectPool.ObjectPool.ClearNamespace(stats.NamespaceName);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Pools: {stats.PoolCount}");
            EditorGUILayout.LabelField($"Active Instances: {stats.TotalActiveInstances}");
            EditorGUILayout.LabelField($"Inactive Instances: {stats.TotalInactiveInstances}");

            int total = stats.TotalActiveInstances + stats.TotalInactiveInstances;
            if (total > 0)
            {
                float utilization = (float)stats.TotalActiveInstances / total * 100f;
                EditorGUILayout.LabelField($"Utilization: {utilization:F1}%");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}
#endif
