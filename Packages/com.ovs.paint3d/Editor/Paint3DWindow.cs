using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Paint3D.Editing
{
    /// <summary>
    /// In-editor painting: paint any <see cref="Paintable"/> directly in the Scene view (edit or play mode) with the
    /// same brush, mirroring, undo / redo and modifiers used at runtime, then export the result to PNG assets.
    /// </summary>
    public class Paint3DWindow : EditorWindow
    {
        private const string MENU_PATH = "Tools/Paint3D/Painter";

        [SerializeField] private PaintBrush _brush = new PaintBrush();
        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();
        [SerializeField] private LayerMask _layers = ~0;
        [SerializeField] private bool _paintAllOverlapping = true;
        [SerializeField] private string _exportFolder = "Assets/Paint3D Exports";

        private readonly List<Paintable> _targets = new List<Paintable>();
        private SerializedObject _serializedWindow;
        private Vector2 _scroll;
        private bool _painting;
        private bool _stroking;
        private bool _hasHit;
        private PaintHit _hoverHit;
        private bool _hasLastPoint;
        private Vector3 _lastPoint;
        private float _distanceToNextDab;

        [MenuItem(MENU_PATH)]
        public static void Open()
        {
            GetWindow<Paint3DWindow>("Paint3D").Show();
        }

        #region Lifecycle

        private void OnEnable()
        {
            _serializedWindow = new SerializedObject(this);
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseEditModeTargets;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            PaintHistory.Changed += Repaint;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseEditModeTargets;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            PaintHistory.Changed -= Repaint;
            StopPainting();
            ReleaseEditModeTargets();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.ExitingPlayMode)
            {
                StopPainting();
                ReleaseEditModeTargets();
            }
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            _serializedWindow.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSessionControls();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_serializedWindow.FindProperty("_brush"), new GUIContent("Brush"), true);
            EditorGUILayout.PropertyField(_serializedWindow.FindProperty("_mirror"), new GUIContent("Mirror"), true);
            EditorGUILayout.PropertyField(_serializedWindow.FindProperty("_layers"));
            EditorGUILayout.PropertyField(_serializedWindow.FindProperty("_paintAllOverlapping"));

            EditorGUILayout.Space();
            DrawHistoryControls();
            EditorGUILayout.Space();
            DrawExportControls();

            EditorGUILayout.EndScrollView();
            _serializedWindow.ApplyModifiedProperties();
        }

        private void DrawSessionControls()
        {
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Left mouse: paint   •   Ctrl/Cmd+Z / Ctrl/Cmd+Y: undo / redo   •   [ / ]: brush size   •   Alt: orbit camera",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = _painting ? new Color(1f, 0.55f, 0.5f) : new Color(0.55f, 1f, 0.6f);
                if (GUILayout.Button(_painting ? "Stop Painting" : "Start Painting", GUILayout.Height(28f)))
                {
                    if (_painting)
                    {
                        StopPainting();
                    }
                    else
                    {
                        StartPainting();
                    }
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Refresh Targets", GUILayout.Height(28f), GUILayout.Width(120f)))
                {
                    RefreshTargets();
                }
            }

            EditorGUILayout.LabelField($"Targets: {_targets.Count} paintable(s)");
        }

        private void DrawHistoryControls()
        {
            EditorGUILayout.LabelField("History", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = PaintHistory.CanUndo;
                if (GUILayout.Button($"Undo ({PaintHistory.UndoCount})"))
                {
                    PaintHistory.Undo();
                    SceneView.RepaintAll();
                }
                GUI.enabled = PaintHistory.CanRedo;
                if (GUILayout.Button($"Redo ({PaintHistory.RedoCount})"))
                {
                    PaintHistory.Redo();
                    SceneView.RepaintAll();
                }
                GUI.enabled = true;
            }
            PaintHistory.MaxSteps = EditorGUILayout.IntSlider("Max Steps", PaintHistory.MaxSteps, 1, 64);
        }

        private void DrawExportControls()
        {
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
            _exportFolder = EditorGUILayout.TextField("Export Folder", _exportFolder);

            GUI.enabled = HasActiveTargets();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export PNGs"))
                {
                    ExportTargets(false);
                }
                if (GUILayout.Button("Export & Assign To Materials"))
                {
                    if (EditorUtility.DisplayDialog("Assign painted textures",
                            "Exported textures will replace the textures of the shared materials. Other objects using those materials will change too.",
                            "Assign", "Cancel"))
                    {
                        ExportTargets(true);
                    }
                }
            }

            if (GUILayout.Button("Reset Paint On Targets"))
            {
                foreach (Paintable paintable in _targets)
                {
                    if (paintable != null && paintable.IsActivated)
                    {
                        paintable.ResetPainting();
                    }
                }
                SceneView.RepaintAll();
            }
            GUI.enabled = true;
        }

        #endregion

        #region Session

        private void StartPainting()
        {
            RefreshTargets();
            foreach (Paintable paintable in _targets)
            {
                paintable.Activate();
            }
            _painting = true;
            Tools.hidden = true;
            SceneView.RepaintAll();
        }

        private void StopPainting()
        {
            EndStroke();
            _painting = false;
            _hasHit = false;
            Tools.hidden = false;
            SceneView.RepaintAll();
        }

        private void RefreshTargets()
        {
            _targets.Clear();
            Paintable[] selected = Selection.GetFiltered<Paintable>(SelectionMode.Deep);
            if (selected.Length > 0)
            {
                _targets.AddRange(selected);
            }
            else
            {
                _targets.AddRange(FindObjectsByType<Paintable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            }

            if (_painting)
            {
                foreach (Paintable paintable in _targets)
                {
                    paintable.Activate();
                }
            }
        }

        /// <summary>Edit mode paint textures do not survive domain reloads; release them cleanly.</summary>
        private void ReleaseEditModeTargets()
        {
            if (Application.isPlaying)
            {
                return;
            }

            bool hadUnsaved = false;
            foreach (Paintable paintable in _targets)
            {
                if (paintable != null && paintable.IsActivated)
                {
                    hadUnsaved |= paintable.IsDirty;
                    paintable.Deactivate();
                }
            }
            PaintHistory.Clear();

            if (hadUnsaved)
            {
                Debug.LogWarning("[Paint3D] Edit mode paint was released without being exported. Use 'Export PNGs' to keep in-editor painting.");
            }
        }

        private bool HasActiveTargets()
        {
            foreach (Paintable paintable in _targets)
            {
                if (paintable != null && paintable.IsActivated)
                {
                    return true;
                }
            }
            return false;
        }

        private void ExportTargets(bool assign)
        {
            int count = 0;
            foreach (Paintable paintable in _targets)
            {
                count += PaintTextureExporter.ExportAll(paintable, _exportFolder, assign);
            }
            Debug.Log($"[Paint3D] Exported {count} texture(s) to {_exportFolder}.");
        }

        /// <summary>Converts an absolute folder inside the project to an "Assets/..." path (null when outside).</summary>
        public static string ToAssetFolder(string absoluteFolder)
        {
            if (string.IsNullOrEmpty(absoluteFolder))
            {
                return null;
            }

            string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            string folder = Path.GetFullPath(absoluteFolder).Replace('\\', '/');
            if (!folder.StartsWith(dataPath))
            {
                Debug.LogWarning("[Paint3D] Choose a folder inside the project's Assets folder.");
                return null;
            }
            return "Assets" + folder.Substring(dataPath.Length);
        }

        #endregion

        #region Scene Painting

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_painting)
            {
                return;
            }

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            if (HandleShortcuts(e))
            {
                return;
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                _hasHit = PaintRaycaster.Raycast(ray, _targets, out _hoverHit, Mathf.Infinity, _layers, true);
                sceneView.Repaint();
            }

            bool leftButton = e.button == 0 && !e.alt;
            switch (e.type)
            {
                case EventType.MouseDown when leftButton:
                    GUIUtility.hotControl = controlId;
                    BeginStroke();
                    if (_hasHit)
                    {
                        StrokeTo(_hoverHit, sceneView.camera.transform.position, PressureOf(e));
                    }
                    e.Use();
                    break;

                case EventType.MouseDrag when leftButton && GUIUtility.hotControl == controlId:
                    if (_hasHit)
                    {
                        StrokeTo(_hoverHit, sceneView.camera.transform.position, PressureOf(e));
                    }
                    else
                    {
                        _hasLastPoint = false;
                    }
                    e.Use();
                    break;

                case EventType.MouseUp when leftButton && GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    EndStroke();
                    e.Use();
                    break;

                case EventType.Repaint:
                    DrawBrushPreview();
                    break;
            }
        }

        private bool HandleShortcuts(Event e)
        {
            if (e.type != EventType.KeyDown)
            {
                return false;
            }

            bool action = e.control || e.command;
            if (action && e.keyCode == KeyCode.Z && !e.shift)
            {
                PaintHistory.Undo();
            }
            else if (action && (e.keyCode == KeyCode.Y || (e.keyCode == KeyCode.Z && e.shift)))
            {
                PaintHistory.Redo();
            }
            else if (e.keyCode == KeyCode.LeftBracket)
            {
                _brush.Radius *= 0.9f;
            }
            else if (e.keyCode == KeyCode.RightBracket)
            {
                _brush.Radius *= 1.1f;
            }
            else
            {
                return false;
            }

            e.Use();
            Repaint();
            SceneView.RepaintAll();
            return true;
        }

        private static float PressureOf(Event e) => e.pressure > 0f ? e.pressure : 1f;

        private void DrawBrushPreview()
        {
            if (!_hasHit)
            {
                return;
            }

            Color color = _brush.Color;
            color.a = 1f;
            Handles.color = color;
            Handles.DrawWireDisc(_hoverHit.Point, _hoverHit.Normal, _brush.Radius);
            Handles.color = new Color(color.r, color.g, color.b, 0.15f);
            Handles.DrawSolidDisc(_hoverHit.Point, _hoverHit.Normal, _brush.Radius);

            if (_mirror == null || !_mirror.Enabled)
            {
                return;
            }

            var reflections = new List<Matrix4x4>();
            Transform space = _mirror.Pivot != null ? _mirror.Pivot : _hoverHit.Paintable.MirrorSpace;
            MirrorSettings.AppendReflections(space.localToWorldMatrix, _mirror.Axes, reflections);
            Handles.color = new Color(color.r, color.g, color.b, 0.6f);
            foreach (Matrix4x4 reflection in reflections)
            {
                Handles.DrawWireDisc(reflection.MultiplyPoint3x4(_hoverHit.Point), reflection.MultiplyVector(_hoverHit.Normal), _brush.Radius);
            }
        }

        private void BeginStroke()
        {
            _stroking = true;
            _hasLastPoint = false;
            PaintHistory.BeginStroke();
        }

        private void EndStroke()
        {
            if (!_stroking)
            {
                return;
            }
            _stroking = false;
            _hasLastPoint = false;
            PaintHistory.EndStroke();
        }

        private void StrokeTo(PaintHit hit, Vector3 eye, float pressure)
        {
            ResolvedBrush resolved = _brush.Resolve(pressure, _brush.Modifiers.Gradual ? 1f / 60f : 0f);
            float spacing = _brush.GetSpacing(resolved.Radius);

            if (!_hasLastPoint || _brush.Modifiers.Gradual || Vector3.Distance(_lastPoint, hit.Point) > resolved.Radius * 16f)
            {
                Dab(hit.Paintable, hit.Point, eye, resolved);
                _lastPoint = hit.Point;
                _hasLastPoint = true;
                _distanceToNextDab = spacing;
                return;
            }

            Vector3 delta = hit.Point - _lastPoint;
            float distance = delta.magnitude;
            float travelled = _distanceToNextDab;
            while (travelled <= distance)
            {
                Vector3 point = _lastPoint + (delta * (travelled / Mathf.Max(distance, 1e-6f)));
                Dab(hit.Paintable, point, eye, _brush.Resolve(pressure));
                travelled += spacing;
            }

            _distanceToNextDab = travelled - distance;
            _lastPoint = hit.Point;
        }

        private void Dab(Paintable target, Vector3 point, Vector3 eye, in ResolvedBrush brush)
        {
            var operation = new SpherePaintOperation(point, (point - eye).normalized, brush);
            if (_paintAllOverlapping)
            {
                Transform mirrorSpace = _mirror != null && _mirror.Pivot != null ? _mirror.Pivot : target.MirrorSpace;
                PaintEngine.PaintAll(_targets, operation, _mirror, _layers, mirrorSpace);
            }
            else
            {
                PaintEngine.Paint(target, operation, _mirror);
            }
        }

        #endregion
    }
}
