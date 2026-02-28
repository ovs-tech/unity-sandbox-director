using UnityEngine;
using Systems.CommandSystem;

namespace Systems.PlacementSystem.Tools.Commands
{
    public class RotateObjectCommand : CommandBase
    {
        private readonly GameObject _target;
        private readonly Quaternion _fromRotation;
        private Quaternion _toRotation;

        public RotateObjectCommand(GameObject target, Quaternion fromRot, Quaternion toRot)
            : base($"Rotate {target?.name ?? "Unknown"}")
        {
            _target = target;
            _fromRotation = fromRot;
            _toRotation = toRot;
        }

        protected override void ExecuteInternal()
        {
            if (_target == null)
                return;

            _target.transform.rotation = _toRotation;
        }

        protected override void UndoInternal()
        {
            if (_target == null)
                return;

            _target.transform.rotation = _fromRotation;
        }

        public override bool CanMergeWith(ICommand other)
        {
            if (other is RotateObjectCommand rt && rt._target == _target)
                return true;
            return false;
        }

        public override void MergeWith(ICommand other)
        {
            if (other is RotateObjectCommand rt && rt._target == _target)
            {
                _toRotation = rt._toRotation;
            }
            else
            {
                base.MergeWith(other);
            }
        }
    }
}
