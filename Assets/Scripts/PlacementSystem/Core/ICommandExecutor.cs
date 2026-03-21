using Systems.CommandSystem;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Abstraction for command execution used by placement tools.
    /// </summary>
    public interface ICommandExecutor
    {
        void Execute(ICommand command, bool merge, string commandNamespace);
    }
}
