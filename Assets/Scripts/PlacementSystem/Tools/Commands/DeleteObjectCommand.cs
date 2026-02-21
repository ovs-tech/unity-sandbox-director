using UnityEngine;
using Systems.CommandSystem;
using Systems.PlacementSystem.Sockets;

namespace Systems.PlacementSystem.Tools.Commands
{
    public class DeleteObjectCommand : CommandBase
    {
        private readonly GameObject _target;
        private readonly SnapManager _snapManager;

        public DeleteObjectCommand(GameObject target, SnapManager snapManager)
            : base($"Delete {target?.name ?? "Unknown"}")
        {
            _target = target;
            _snapManager = snapManager;
        }

        protected override void ExecuteInternal()
        {
            if (_target == null)
                return;

            if (_snapManager != null)
            {
                var sockets = _target.GetComponentsInChildren<Socket>();
                foreach (var s in sockets)
                {
                    _snapManager.UnregisterSocket(s);
                }
            }

            _target.SetActive(false);
        }

        protected override void UndoInternal()
        {
            if (_target == null)
                return;

            _target.SetActive(true);

            if (_snapManager != null)
            {
                var sockets = _target.GetComponentsInChildren<Socket>();
                foreach (var s in sockets)
                {
                    _snapManager.RegisterSocket(s);
                }
            }
        }
    }
}
