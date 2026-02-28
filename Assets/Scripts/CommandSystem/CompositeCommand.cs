using System;
using System.Collections.Generic;

namespace Systems.CommandSystem
{
    /// <summary>
    /// Composite command that executes multiple commands as a single unit
    /// Useful for complex operations that involve multiple atomic changes
    /// </summary>
    public class CompositeCommand : CommandBase
    {
        private readonly List<ICommand> commands = new List<ICommand>();

        public CompositeCommand(string description, params ICommand[] commandList)
            : base(description)
        {
            commands.AddRange(commandList);
        }

        public void AddCommand(ICommand command)
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
