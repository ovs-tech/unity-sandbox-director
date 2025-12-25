namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Sandbox operation mode - determines available functionality and input handling
    /// </summary>
    public enum SandboxMode
    {
        Build,  // Build mode - can place, select, and edit objects (all input actions enabled)
        Play    // Play mode - read-only preview, input actions disabled for performance
    }
}
