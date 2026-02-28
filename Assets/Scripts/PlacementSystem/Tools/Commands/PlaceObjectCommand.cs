using UnityEngine;
using Systems.CommandSystem;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Selection;

namespace Systems.PlacementSystem.Tools.Commands
{
    public class PlaceObjectCommand : CommandBase
    {
        private readonly GameObject _prefab;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly bool _makeSelectable;
        private readonly SnapManager _snapManager;
        private readonly Socket _occupiedSocket;

        private GameObject _instance;

        public GameObject Instance => _instance;

        public PlaceObjectCommand(GameObject prefab, Vector3 position, Quaternion rotation, bool makeSelectable, SnapManager snapManager, Socket occupiedSocket = null)
            : base($"Place {_prefabName(prefab)}")
        {
            _prefab = prefab;
            _position = position;
            _rotation = rotation;
            _makeSelectable = makeSelectable;
            _snapManager = snapManager;
            _occupiedSocket = occupiedSocket;
        }

        private static string _prefabName(GameObject prefab)
        {
            return prefab != null ? prefab.name : "UnknownPrefab";
        }

        protected override void ExecuteInternal()
        {
            if (_prefab == null)
                return;

            _instance = Systems.ObjectPool.ObjectPool.Spawn(_prefab, _position, _rotation, null);
            _instance.name = _prefab.name;

            if (_makeSelectable && _instance.GetComponent<Selectable>() == null)
            {
                _instance.AddComponent<Selectable>();
            }

            if (_snapManager != null)
            {
                var sockets = _instance.GetComponentsInChildren<Socket>();
                foreach (var s in sockets)
                {
                    _snapManager.RegisterSocket(s);
                }
            }

            if (_occupiedSocket != null)
            {
                _occupiedSocket.IsOccupied = true;
            }
        }

        protected override void UndoInternal()
        {
            if (_instance == null)
                return;

            if (_snapManager != null)
            {
                var sockets = _instance.GetComponentsInChildren<Socket>();
                foreach (var s in sockets)
                {
                    _snapManager.UnregisterSocket(s);
                }
            }

            Systems.ObjectPool.ObjectPool.Release(_instance);
            _instance = null;

            if (_occupiedSocket != null)
            {
                _occupiedSocket.IsOccupied = false;
            }
        }

        protected override void RedoInternal()
        {
            if (_instance == null)
            {
                ExecuteInternal();
                return;
            }

            if (_snapManager != null)
            {
                var sockets = _instance.GetComponentsInChildren<Socket>();
                foreach (var s in sockets)
                {
                    _snapManager.RegisterSocket(s);
                }
            }

            _instance.SetActive(true);

            if (_occupiedSocket != null)
            {
                _occupiedSocket.IsOccupied = true;
            }
        }
    }
}
