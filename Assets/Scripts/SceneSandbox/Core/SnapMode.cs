namespace SceneSandbox.Core
{
    /// <summary>
    /// Snap mode determines how snap grid settings are applied
    /// </summary>
    public enum SnapMode
    {
        /// <summary>
        /// Extend/inherit snap grid settings from SceneSandboxBuilder
        /// Uses global settings as base, can be overridden by local settings
        /// </summary>
        Extend,
        
        /// <summary>
        /// Use only this object's own snap grid settings
        /// Completely ignores SceneSandboxBuilder's global snap grid
        /// </summary>
        Self
    }
}
