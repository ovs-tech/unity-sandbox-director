using System;
using UnityEngine;

namespace Systems.CommandSystem
{
    /// <summary>
    /// Base class for commands that provides common functionality
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        protected readonly string description;
        protected bool isExecuted = false;

        protected CommandBase(string commandDescription)
        {
            description = commandDescription ?? "Unknown Command";
        }

        public virtual string Description => description;

        public virtual void Execute()
        {
            if (isExecuted)
            {
                Debug.LogWarning($"[CommandSystem] Command '{Description}' already executed");
                return;
            }

            ExecuteInternal();
            isExecuted = true;
        }

        public virtual void Undo()
        {
            if (!isExecuted)
            {
                Debug.LogWarning($"[CommandSystem] Cannot undo non-executed command '{Description}'");
                return;
            }

            UndoInternal();
            isExecuted = false;
        }

        public virtual void Redo()
        {
            if (isExecuted)
            {
                Debug.LogWarning($"[CommandSystem] Command '{Description}' already executed");
                return;
            }

            RedoInternal();
            isExecuted = true;
        }

        public virtual bool CanMergeWith(ICommand other)
        {
            return false; // Most commands cannot be merged by default
        }

        public virtual void MergeWith(ICommand other)
        {
            throw new NotImplementedException($"Command '{Description}' does not support merging");
        }

        /// <summary>
        /// Override this to implement the actual command execution
        /// </summary>
        protected abstract void ExecuteInternal();

        /// <summary>
        /// Override this to implement the actual command undo
        /// </summary>
        protected abstract void UndoInternal();

        /// <summary>
        /// Override this to implement custom redo behavior
        /// Default implementation calls ExecuteInternal()
        /// </summary>
        protected virtual void RedoInternal()
        {
            ExecuteInternal();
        }
    }
}
