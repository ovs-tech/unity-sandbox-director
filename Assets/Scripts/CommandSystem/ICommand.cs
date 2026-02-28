namespace Systems.CommandSystem
{
    /// <summary>
    /// Command interface for implementing undo/redo operations
    /// All operations that require undo/redo should implement this interface
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo the command (reverse the operation)
        /// </summary>
        void Undo();

        /// <summary>
        /// Redo the command (re-execute after undo)
        /// </summary>
        void Redo();

        /// <summary>
        /// Human-readable description of the command for UI display
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this command can be merged with another command of the same type
        /// Useful for continuous operations like dragging
        /// </summary>
        bool CanMergeWith(ICommand other);

        /// <summary>
        /// Merge this command with another command
        /// Only called if CanMergeWith returns true
        /// </summary>
        void MergeWith(ICommand other);
    }
}
