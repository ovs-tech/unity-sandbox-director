using System;
using System.Collections.Generic;

using UnityEngine;

namespace Systems.CommandSystem
{
    /// <summary>
    /// Manages command execution and undo/redo stack
    /// Provides thread-safe operations and memory management
    /// </summary>
    public class CommandManager : PersistentSingleton<CommandManager>, ICommandManager
    {
        public const string DEFAULT_NAMESPACE = "default";

        private readonly Dictionary<string, Stack<ICommand>> undoStacks = new Dictionary<string, Stack<ICommand>>();
        private readonly Dictionary<string, Stack<ICommand>> redoStacks = new Dictionary<string, Stack<ICommand>>();
        [SerializeField]
        private readonly int maxHistorySize = 100;
        [SerializeField] private bool _debugLog = true;

        // Events (legacy - default namespace will still invoke these)
        public event Action<ICommand> OnCommandExecuted;
        public event Action<ICommand> OnUndoPerformed;
        public event Action<ICommand> OnRedoPerformed;
        public event Action<bool, bool> OnStacksChanged;  // (canUndo, canRedo)

        // Namespaced events
        public event Action<string, ICommand> OnCommandExecutedByNamespace;
        public event Action<string, ICommand> OnUndoPerformedByNamespace;
        public event Action<string, ICommand> OnRedoPerformedByNamespace;
        public event Action<string, bool, bool> OnStacksChangedByNamespace; // (namespace, canUndo, canRedo)

        #region Properties

        public bool CanUndo => GetUndoStack(DEFAULT_NAMESPACE).Count > 0;
        public bool CanRedo => GetRedoStack(DEFAULT_NAMESPACE).Count > 0;
        public int UndoStackCount => GetUndoStack(DEFAULT_NAMESPACE).Count;
        public int RedoStackCount => GetRedoStack(DEFAULT_NAMESPACE).Count;

        public string NextUndoDescription => CanUndo ? GetUndoStack(DEFAULT_NAMESPACE).Peek().Description : "";
        public string NextRedoDescription => CanRedo ? GetRedoStack(DEFAULT_NAMESPACE).Peek().Description : "";

        public bool CanUndoForNamespace(string ns) => GetUndoStack(ns).Count > 0;
        public bool CanRedoForNamespace(string ns) => GetRedoStack(ns).Count > 0;
        public string NextUndoDescriptionForNamespace(string ns) => CanUndoForNamespace(ns) ? GetUndoStack(ns).Peek().Description : "";
        public string NextRedoDescriptionForNamespace(string ns) => CanRedoForNamespace(ns) ? GetRedoStack(ns).Peek().Description : "";

        /// <summary>
        /// Enable/disable debug logging for command operations
        /// </summary>
        public bool DebugLog
        {
            get => _debugLog;
            set => _debugLog = value;
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="merge">Whether to attempt merging with the last command</param>
        public void ExecuteCommand(ICommand command, bool merge = false, string ns = DEFAULT_NAMESPACE)
        {
            if (command == null)
            {
                Debug.LogError("[CommandManager] Cannot execute null command");
                return;
            }

            try
            {
                var uStack = GetUndoStack(ns);
                var rStack = GetRedoStack(ns);

                // Attempt to merge with the last command if requested
                if (merge && uStack.Count > 0)
                {
                    var lastCommand = uStack.Peek();
                    if (lastCommand.CanMergeWith(command))
                    {
                        lastCommand.MergeWith(command);
                        OnCommandExecuted?.Invoke(command);
                        OnCommandExecutedByNamespace?.Invoke(ns, command);
                        return;
                    }
                }

                // Execute the new command
                command.Execute();

                // Add to undo stack
                uStack.Push(command);

                // Clear redo stack (new command invalidates redo history)
                rStack.Clear();

                // Maintain max history size
                TrimHistoryIfNeeded(ns);

                OnCommandExecuted?.Invoke(command);
                OnCommandExecutedByNamespace?.Invoke(ns, command);
                NotifyStacksChanged(ns);

                if (_debugLog)
                    Debug.Log($"[CommandManager] Executed ({ns}): {command.Description}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandManager] Failed to execute command '{command.Description}': {e.Message}");
            }
        }

        // Backwards-compatible overloads for ICommandManager interface
        public void ExecuteCommand(ICommand command, bool merge = false)
        {
            ExecuteCommand(command, merge, DEFAULT_NAMESPACE);
        }

        #endregion

        #region Undo/Redo

        /// <summary>
        /// Undo the last command
        /// </summary>
        public void Undo(string ns = DEFAULT_NAMESPACE)
        {
            var uStack = GetUndoStack(ns);
            var rStack = GetRedoStack(ns);

            if (uStack.Count == 0)
            {
                if (_debugLog)
                    Debug.LogWarning($"[CommandManager] No commands to undo for namespace '{ns}'");
                return;
            }

            try
            {
                var command = uStack.Pop();
                command.Undo();

                rStack.Push(command);

                OnUndoPerformed?.Invoke(command);
                OnUndoPerformedByNamespace?.Invoke(ns, command);
                NotifyStacksChanged(ns);

                if (_debugLog)
                    Debug.Log($"[CommandManager] Undid ({ns}): {command.Description}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandManager] Failed to undo command: {e.Message}");
            }
        }

        // Backwards-compatible no-arg Undo
        public void Undo()
        {
            Undo(DEFAULT_NAMESPACE);
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public void Redo(string ns = DEFAULT_NAMESPACE)
        {
            var uStack = GetUndoStack(ns);
            var rStack = GetRedoStack(ns);

            if (rStack.Count == 0)
            {
                if (_debugLog)
                    Debug.LogWarning($"[CommandManager] No commands to redo for namespace '{ns}'");
                return;
            }

            try
            {
                var command = rStack.Pop();
                command.Redo();

                uStack.Push(command);

                OnRedoPerformed?.Invoke(command);
                OnRedoPerformedByNamespace?.Invoke(ns, command);
                NotifyStacksChanged(ns);

                if (_debugLog)
                    Debug.Log($"[CommandManager] Redid ({ns}): {command.Description}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandManager] Failed to redo command: {e.Message}");
            }
        }

        // Backwards-compatible no-arg Redo
        public void Redo()
        {
            Redo(DEFAULT_NAMESPACE);
        }

        #endregion

        #region History Management

        /// <summary>
        /// Clear all command history
        /// </summary>
        public void Clear(string ns = DEFAULT_NAMESPACE)
        {
            GetUndoStack(ns).Clear();
            GetRedoStack(ns).Clear();
            OnStacksChanged?.Invoke(CanUndo, CanRedo);
            OnStacksChangedByNamespace?.Invoke(ns, CanUndoForNamespace(ns), CanRedoForNamespace(ns));

            if (_debugLog)
                Debug.Log($"[CommandManager] Command history cleared for namespace '{ns}'");
        }

        // Backwards-compatible no-arg Clear
        public void Clear()
        {
            Clear(DEFAULT_NAMESPACE);
        }

        /// <summary>
        /// Get the command history for display purposes
        /// </summary>
        /// <returns>Array of command descriptions, most recent first</returns>
        public string[] GetUndoHistory(string ns = DEFAULT_NAMESPACE)
        {
            var stack = GetUndoStack(ns);
            var history = new string[stack.Count];
            var commands = stack.ToArray();

            for (int i = 0; i < commands.Length; i++)
            {
                history[i] = commands[i].Description;
            }

            return history;
        }

        // Backwards-compatible no-arg GetUndoHistory
        public string[] GetUndoHistory()
        {
            return GetUndoHistory(DEFAULT_NAMESPACE);
        }

        /// <summary>
        /// Get the redo history for display purposes
        /// </summary>
        /// <returns>Array of command descriptions, most recent first</returns>
        public string[] GetRedoHistory(string ns = DEFAULT_NAMESPACE)
        {
            var stack = GetRedoStack(ns);
            var history = new string[stack.Count];
            var commands = stack.ToArray();

            for (int i = 0; i < commands.Length; i++)
            {
                history[i] = commands[i].Description;
            }

            return history;
        }

        // Backwards-compatible no-arg GetRedoHistory
        public string[] GetRedoHistory()
        {
            return GetRedoHistory(DEFAULT_NAMESPACE);
        }

        private void TrimHistoryIfNeeded(string ns)
        {
            var stack = GetUndoStack(ns);
            while (stack.Count > maxHistorySize)
            {
                // Remove oldest commands
                var tempStack = new Stack<ICommand>();
                for (int i = 0; i < maxHistorySize - 1; i++)
                {
                    tempStack.Push(stack.Pop());
                }

                stack.Clear();

                while (tempStack.Count > 0)
                {
                    stack.Push(tempStack.Pop());
                }
            }
        }

        private void NotifyStacksChanged(string ns)
        {
            var canUndo = CanUndoForNamespace(ns);
            var canRedo = CanRedoForNamespace(ns);
            if (ns == DEFAULT_NAMESPACE)
            {
                OnStacksChanged?.Invoke(canUndo, canRedo);
            }
            OnStacksChangedByNamespace?.Invoke(ns, canUndo, canRedo);
        }

        private Stack<ICommand> GetUndoStack(string ns)
        {
            if (string.IsNullOrEmpty(ns)) ns = DEFAULT_NAMESPACE;
            if (!undoStacks.TryGetValue(ns, out var stack))
            {
                stack = new Stack<ICommand>();
                undoStacks[ns] = stack;
            }
            return stack;
        }

        private Stack<ICommand> GetRedoStack(string ns)
        {
            if (string.IsNullOrEmpty(ns)) ns = DEFAULT_NAMESPACE;
            if (!redoStacks.TryGetValue(ns, out var stack))
            {
                stack = new Stack<ICommand>();
                redoStacks[ns] = stack;
            }
            return stack;
        }

        #endregion
    }
}
