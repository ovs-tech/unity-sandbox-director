using Systems.CommandSystem;
using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Default bridge from placement tools to the global command system.
    /// </summary>
    public class DefaultCommandExecutor : ICommandExecutor
    {
        public void Execute(ICommand command, bool merge, string commandNamespace)
        {
            if (command == null)
                return;

            if (CommandManager.HasInstance)
            {
                CommandManager.Instance.ExecuteCommand(command, merge, commandNamespace);
                return;
            }

            // Fallback for test/bootstrap scenarios where command manager is not initialized.
            try
            {
                command.Execute();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DefaultCommandExecutor: command execution failed: {ex.Message}");
            }
        }
    }
}
