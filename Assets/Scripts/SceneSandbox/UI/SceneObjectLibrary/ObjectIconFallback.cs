using UnityEngine;
using Systems.SceneSandbox.Data;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary
{
    /// <summary>
    /// Helper for handling object icon fallbacks.
    /// Provides default colors and textures for objects without assigned icons.
    /// </summary>
    public static class ObjectIconFallback
    {
        /// <summary>
        /// Default colors for each object type when icons are missing.
        /// </summary>
        public static readonly Color[] TypeColors =
        {
            Color.blue,      // Actor (0)
            Color.green,     // Prop (1)
            Color.yellow,    // Camera (2)
            Color.white,     // Light (3)
        };
        
        /// <summary>
        /// Get the fallback color for an object type.
        /// </summary>
        public static Color GetTypeColor(SceneObjectType type)
        {
            int index = (int)type;
            if (index >= 0 && index < TypeColors.Length)
                return TypeColors[index];
            return Color.gray;
        }
        
        /// <summary>
        /// Get a display-friendly type name.
        /// </summary>
        public static string GetTypeName(SceneObjectType type)
        {
            return type switch
            {
                SceneObjectType.Actor => "Actor",
                SceneObjectType.Prop => "Prop",
                SceneObjectType.Camera => "Camera",
                SceneObjectType.Light => "Light",
                _ => "Unknown"
            };
        }
    }
}
