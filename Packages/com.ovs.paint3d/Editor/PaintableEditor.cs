using UnityEditor;
using UnityEngine;

namespace Paint3D.Editing
{
    [CustomEditor(typeof(Paintable))]
    [CanEditMultipleObjects]
    public class PaintableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length != 1)
            {
                return;
            }

            var paintable = (Paintable)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Painting", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State", paintable.IsActivated ? (paintable.IsDirty ? "Active (modified)" : "Active") : "Inactive");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Painter Window"))
                {
                    Paint3DWindow.Open();
                }

                if (!paintable.IsActivated)
                {
                    if (GUILayout.Button("Activate"))
                    {
                        paintable.Activate();
                        SceneView.RepaintAll();
                    }
                }
                else if (GUILayout.Button("Deactivate (discard)"))
                {
                    paintable.Deactivate();
                    SceneView.RepaintAll();
                }
            }

            if (!paintable.IsActivated)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Paint"))
                {
                    paintable.ResetPainting();
                }

                if (GUILayout.Button("Export PNG..."))
                {
                    string folder = EditorUtility.SaveFolderPanel("Export painted textures", "Assets", "");
                    string assetFolder = Paint3DWindow.ToAssetFolder(folder);
                    if (assetFolder != null)
                    {
                        PaintTextureExporter.ExportAll(paintable, assetFolder, false);
                    }
                }
            }

            if (Application.isPlaying && paintable.SaveMode != PaintSaveMode.None)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save"))
                    {
                        paintable.Save();
                    }
                    if (GUILayout.Button("Load"))
                    {
                        paintable.Load();
                    }
                    if (GUILayout.Button("Delete Saved"))
                    {
                        paintable.ClearSavedData();
                    }
                }
            }

            foreach (PaintableTexture slot in paintable.Textures)
            {
                if (slot != null && slot.IsAllocated)
                {
                    Rect rect = GUILayoutUtility.GetRect(96f, 96f, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, slot.Current);
                }
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying && ((Paintable)target).IsActivated;
    }
}
