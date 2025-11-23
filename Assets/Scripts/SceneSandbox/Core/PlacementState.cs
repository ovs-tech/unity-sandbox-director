namespace SceneSandbox.Core
{
    /// <summary>
    /// Placement state for drag-drop operations
    /// </summary>
    public enum PlacementState
    {
        Idle,           // No active placement
        Active,         // Dragging with preview
        Confirming,     // Transition to place
        Cancelling      // Transition to cancel
    }
}
