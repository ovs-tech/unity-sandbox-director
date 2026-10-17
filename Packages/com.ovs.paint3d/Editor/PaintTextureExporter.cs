using System.IO;
using UnityEditor;
using UnityEngine;

namespace Paint3D.Editing
{
    /// <summary>Exports live paint textures to PNG assets (the editor-side "save" for in-editor painting).</summary>
    public static class PaintTextureExporter
    {
        /// <summary>Writes the slot to <paramref name="assetPath"/> (under Assets/) and imports it.</summary>
        public static Texture2D ExportToAsset(PaintableTexture slot, string assetPath)
        {
            if (slot == null || !slot.IsAllocated || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            Texture2D readback = slot.ReadToTexture2D();
            try
            {
                string fullPath = Path.GetFullPath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, readback.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(readback);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.sRGBTexture = slot.IsColor;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>
        /// Exports every allocated slot of a paintable next to a chosen folder. When <paramref name="assignToMaterials"/>
        /// is set, the exported textures replace the textures on the (shared) materials, with undo support.
        /// </summary>
        public static int ExportAll(Paintable paintable, string folder, bool assignToMaterials)
        {
            if (paintable == null || !paintable.IsActivated)
            {
                return 0;
            }

            int exported = 0;
            Material[] materials = paintable.Renderer.sharedMaterials;
            foreach (PaintableTexture slot in paintable.Textures)
            {
                if (slot == null || !slot.IsAllocated)
                {
                    continue;
                }

                string fileName = PaintStorage.SanitizeKey($"{paintable.name}_{slot.MaterialIndex}{slot.ResolvedPropertyName}") + ".png";
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, fileName).Replace('\\', '/'));
                Texture2D texture = ExportToAsset(slot, assetPath);
                if (texture == null)
                {
                    continue;
                }
                exported++;

                if (assignToMaterials && slot.MaterialIndex < materials.Length && materials[slot.MaterialIndex] != null)
                {
                    Material material = materials[slot.MaterialIndex];
                    Undo.RecordObject(material, "Assign Painted Texture");
                    material.SetTexture(slot.ResolvedPropertyName, texture);
                    EditorUtility.SetDirty(material);
                }
            }
            return exported;
        }
    }
}
