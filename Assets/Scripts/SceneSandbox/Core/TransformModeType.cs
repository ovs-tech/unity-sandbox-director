namespace SceneSandbox.Core
{
    /// <summary>
    /// Unified transform mode for object manipulation
    /// Used by both placement system (SceneSandboxBuilder) and selection system (TransformableItem)
    /// </summary>
    public enum TransformModeType
    {
        None,       // No transform mode active
        Position,   // Move/translate object
        Rotation,   // Rotate object
        Scale      // Scale object
    }
}
