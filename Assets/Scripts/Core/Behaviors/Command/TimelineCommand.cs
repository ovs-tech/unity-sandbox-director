using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Core.Behaviors.Command
{

    /// <summary>
    /// Base class for timeline commands that provides common functionality
    /// </summary>
    public abstract class TimelineCommandBase : ITimelineCommand
    {
        protected readonly string description;
        protected bool isExecuted = false;

        protected TimelineCommandBase(string commandDescription)
        {
            description = commandDescription ?? "Unknown Command";
        }

        public virtual string Description => description;

        public virtual void Execute()
        {
            if (isExecuted)
            {
                Debug.LogWarning($"[TimelineCommand] Command '{Description}' already executed");
                return;
            }

            ExecuteInternal();
            isExecuted = true;
        }

        public virtual void Undo()
        {
            if (!isExecuted)
            {
                Debug.LogWarning($"[TimelineCommand] Cannot undo non-executed command '{Description}'");
                return;
            }

            UndoInternal();
            isExecuted = false;
        }

        public virtual void Redo()
        {
            if (isExecuted)
            {
                Debug.LogWarning($"[TimelineCommand] Command '{Description}' already executed");
                return;
            }

            RedoInternal();
            isExecuted = true;
        }

        public virtual bool CanMergeWith(ITimelineCommand other)
        {
            return false; // Most commands cannot be merged by default
        }

        public virtual void MergeWith(ITimelineCommand other)
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

    /// <summary>
    /// Composite command that executes multiple commands as a single unit
    /// Useful for complex operations that involve multiple atomic changes
    /// </summary>
    public class CompositeCommand : TimelineCommandBase
    {
        private readonly List<ITimelineCommand> commands = new List<ITimelineCommand>();

        public CompositeCommand(string description, params ITimelineCommand[] commandList)
            : base(description)
        {
            commands.AddRange(commandList);
        }

        public void AddCommand(ITimelineCommand command)
        {
            if (isExecuted)
            {
                throw new InvalidOperationException("Cannot add commands to an already executed composite command");
            }

            commands.Add(command);
        }

        protected override void ExecuteInternal()
        {
            foreach (var command in commands)
            {
                command.Execute();
            }
        }

        protected override void UndoInternal()
        {
            // Undo in reverse order
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }

        protected override void RedoInternal()
        {
            foreach (var command in commands)
            {
                command.Redo();
            }
        }
    }
}