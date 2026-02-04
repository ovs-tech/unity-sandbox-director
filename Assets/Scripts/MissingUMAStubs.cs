using UnityEngine;
using System.Collections.Generic;

// Minimal stub to replace removed UMA package types so the project compiles.
// These are intentionally lightweight placeholders — they do not implement UMA behavior.

public class UmaAvatar : MonoBehaviour
{
    // Minimal methods expected by the project. They perform no real UMA logic.
    public void SetWardrobe(Dictionary<string, string> wardrobe)
    {
        // Stub: no-op
    }

    public void SetColors(Dictionary<string, Color> colors)
    {
        // Stub: no-op
    }
}

// Provide a global ExpressionPlayer for files that reference it without a using directive.
public static class ExpressionPlayer
{
    public static string[] PoseNames { get; } = new string[0];
}

namespace UMA.PoseTools
{
    public class UMAExpressionPlayer : MonoBehaviour
    {
        // The code expects a float[] property `Values` that can be read and written.
        public float[] Values { get; set; } = new float[0];
    }

    public static class ExpressionPlayer
    {
        public static string[] PoseNames { get; } = new string[0];
    }
}
