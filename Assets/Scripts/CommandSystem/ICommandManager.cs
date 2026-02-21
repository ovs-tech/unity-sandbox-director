using System;

namespace Systems.CommandSystem
{
    /// <summary>
    /// Interface for a manager that handles command execution and undo/redo stacks
    /// </summary>
    public interface ICommandManager
    {
        event Action<ICommand> OnCommandExecuted;
        event Action<ICommand> OnUndoPerformed;
        event Action<ICommand> OnRedoPerformed;
        event Action<bool, bool> OnStacksChanged; // (canUndo, canRedo)

        bool CanUndo { get; }
        bool CanRedo { get; }
        int UndoStackCount { get; }
        int RedoStackCount { get; }

        string NextUndoDescription { get; }
        string NextRedoDescription { get; }

        bool DebugLog { get; set; }

        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="merge">Whether to attempt merging with the last command</param>
        void ExecuteCommand(ICommand command, bool merge = false);

        /// <summary>
        /// Undo the last command
        /// </summary>
        void Undo();

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        void Redo();

        /// <summary>
        /// Clear all command history
        /// </summary>
        void Clear();

        /// <summary>
        /// Get the command history for display purposes
        /// </summary>
        /// <returns>Array of command descriptions, most recent first</returns>
        string[] GetUndoHistory();

        /// <summary>
        /// Get the redo history for display purposes
        /// </summary>
        /// <returns>Array of command descriptions, most recent first</returns>
        string[] GetRedoHistory();
    }
}
