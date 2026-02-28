using UnityEngine;
using Systems.CommandSystem;

namespace Systems.PlacementSystem.Tools.Commands
{
    public class MoveObjectCommand : CommandBase
    {
        private readonly GameObject _target;
        private readonly Vector3 _fromPosition;
        private readonly Quaternion _fromRotation;
        private Vector3 _toPosition;
        private Quaternion _toRotation;

        public MoveObjectCommand(GameObject target, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot)
            : base($"Move {target?.name ?? "Unknown"}")
        {
            _target = target;
            _fromPosition = fromPos;
            _fromRotation = fromRot;
            _toPosition = toPos;
            _toRotation = toRot;
        }

        protected override void ExecuteInternal()
        {
            if (_target == null)
                return;

            _target.transform.position = _toPosition;
            _target.transform.rotation = _toRotation;
        }

        protected override void UndoInternal()
        {
            if (_target == null)
                return;

            _target.transform.position = _fromPosition;
            _target.transform.rotation = _fromRotation;
        }

        public override bool CanMergeWith(ICommand other)
        {
            if (other is MoveObjectCommand mv && mv._target == _target)
                return true;
            return false;
        }

        public override void MergeWith(ICommand other)
        {
            if (other is MoveObjectCommand mv && mv._target == _target)
            {
                _toPosition = mv._toPosition;
                _toRotation = mv._toRotation;
            }
            else
            {
                base.MergeWith(other);
            }
        }
    }
}
