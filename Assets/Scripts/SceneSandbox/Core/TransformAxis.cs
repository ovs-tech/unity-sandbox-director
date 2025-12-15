namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Axis selection for rotation and scale transform modes
    /// Allows user to choose which axis to modify
    /// </summary>
    public enum TransformAxis
    {
        All,    // Modify all axes (default)
        X,      // Modify X axis only
        Y,      // Modify Y axis only
        Z       // Modify Z axis only
    }
}
