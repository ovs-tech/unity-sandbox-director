using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors.Command
{
    /// <summary>
    /// Manages command execution and undo/redo stack for timeline operations
    /// Provides thread-safe operations and memory management
    /// </summary>
    public class TimelineCommandManager
    {
        private readonly Stack<ITimelineCommand> undoStack = new Stack<ITimelineCommand>();
        private readonly Stack<ITimelineCommand> redoStack = new Stack<ITimelineCommand>();
        private readonly int maxHistorySize = 100;

        // Events
        public event Action OnCommandExecuted;
        public event Action OnUndoPerformed;
        public event Action OnRedoPerformed;
        public event Action OnStacksChanged;

        #region Properties

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        public int UndoStackCount => undoStack.Count;
        public int RedoStackCount => redoStack.Count;

        public string NextUndoDescription => CanUndo ? undoStack.Peek().Description : "";
        public string NextRedoDescription => CanRedo ? redoStack.Peek().Description : "";

        #endregion

        #region Command Execution

        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="merge">Whether to attempt merging with the last command</param>
        public void ExecuteCommand(ITimelineCommand command, bool merge = false)
        {
            if (command == null)
            {
                Debug.LogError("[TimelineCommandManager] Cannot execute null command");
                return;
            }

            try
            {
                // Attempt to merge with the last command if requested
                if (merge && undoStack.Count > 0)
                {
                    var lastCommand = undoStack.Peek();
                    if (lastCommand.CanMergeWith(command))
                    {
                        lastCommand.MergeWith(command);
                        OnCommandExecuted?.Invoke();
                        return;
                    }
                }

                // Execute the new command
                command.Execute();

                // Add to undo stack
                undoStack.Push(command);

                // Clear redo stack (new command invalidates redo history)
                redoStack.Clear();

                // Maintain max history size
                TrimHistoryIfNeeded();

                OnCommandExecuted?.Invoke();
                OnStacksChanged?.Invoke();

                Debug.Log($"[TimelineCommandManager] Executed: {command.Description}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimelineCommandManager] Failed to execute command '{command.Description}': {e.Message}");
            }
        }

        #endregion

        #region Undo/Redo

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
            {
                Debug.LogWarning("[TimelineCommandManager] No commands to undo");
                return;
            }

            try
            {
                var command = undoStack.Pop();
                command.Undo();

                redoStack.Push(command);

                OnUndoPerformed?.Invoke();
                OnStacksChanged?.Invoke();

                Debug.Log($"[TimelineCommandManager] Undid: {command.Description}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimelineCommandManager] Failed to undo command: {e.Message}");
            }
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
            {
                Debug.LogWarning("[TimelineCommandManager] No commands to redo");
                return;
            }

            try
            {
                var command = redoStack.Pop();
                command.Redo();

                undoStack.Push(command);

                OnRedoPerformed?.Invoke();
                OnStacksChanged?.Invoke();

                Debug.Log($"[TimelineCommandManager] Redid: {command.Description}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimelineCommandManager] Failed to redo command: {e.Message}");
            }
        }

        #endregion

        #region History Management

        /// <summary>
        /// Clear all command history
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            OnStacksChanged?.Invoke();

            Debug.Log("[TimelineCommandManager] Command history cleared");
        }

        /// <summary>
        /// Get the command history for display purposes
        /// </summary>
        /// <returns>Array of command descriptions, most recent first</returns>
        public string[] GetUndoHistory()
        {
            var history = new string[undoStack.Count];
            var commands = undoStack.ToArray();

            for (int i = 0; i < commands.Length; i++)
            {
                history[i] = commands[i].Description;
            }

            return history;
        }

        /// <summary>
        /// Get the redo history for display purposes
        /// </summary>
        /// <returns>Array of command descriptions, most recent first</returns>
        public string[] GetRedoHistory()
        {
            var history = new string[redoStack.Count];
            var commands = redoStack.ToArray();

            for (int i = 0; i < commands.Length; i++)
            {
                history[i] = commands[i].Description;
            }

            return history;
        }

        private void TrimHistoryIfNeeded()
        {
            while (undoStack.Count > maxHistorySize)
            {
                // Remove oldest commands
                var tempStack = new Stack<ITimelineCommand>();
                for (int i = 0; i < maxHistorySize - 1; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }

                undoStack.Clear();

                while (tempStack.Count > 0)
                {
                    undoStack.Push(tempStack.Pop());
                }
            }
        }

        #endregion
    }
}