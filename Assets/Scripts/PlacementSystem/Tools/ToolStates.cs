using UnityEngine;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Shared selection input state for downstream tools.
    /// </summary>
    public class SelectionToolState
    {
        public bool WasSelectionInput { get; private set; }
        public GameObject ClickedObject { get; private set; }
        public bool WasPrimarySelection { get; private set; }

        public void Clear()
        {
            WasSelectionInput = false;
            ClickedObject = null;
            WasPrimarySelection = false;
        }

        public void RecordSelectionInput(GameObject clickedObject, bool wasPrimarySelection)
        {
            WasSelectionInput = true;
            ClickedObject = clickedObject;
            WasPrimarySelection = wasPrimarySelection;
        }
    }

    /// <summary>
    /// Shared move state for tools that need to gate selection input.
    /// </summary>
    public class MoveToolState
    {
        public bool IsMoveActive { get; set; }
        public bool IsMoveToolEnabled { get; set; }
    }

    /// <summary>
    /// Shared snap request/result state for tools that support snapping.
    /// </summary>
    public class SnapToolState
    {
        public bool HasRequest { get; private set; }
        public Vector3 RequestPosition { get; private set; }
        public Quaternion RequestRotation { get; private set; }
        public GameObject RequestObject { get; private set; }
        public Sockets.SocketType RequiredSocketType { get; private set; }
        public float SnapRange { get; private set; }

        public bool HasSnap { get; private set; }
        public Vector3 SnappedPosition { get; private set; }
        public Quaternion SnappedRotation { get; private set; }
        public Sockets.Socket SnappedSocket { get; private set; }

        public void SetRequest(
            GameObject requestObject,
            Vector3 requestPosition,
            Quaternion requestRotation,
            Sockets.SocketType requiredSocketType,
            float snapRange)
        {
            HasRequest = true;
            RequestObject = requestObject;
            RequestPosition = requestPosition;
            RequestRotation = requestRotation;
            RequiredSocketType = requiredSocketType;
            SnapRange = snapRange;
        }

        public void ClearRequest()
        {
            HasRequest = false;
            RequestObject = null;
            RequiredSocketType = null;
            SnapRange = 0f;
        }

        public void SetSnap(Sockets.Socket snappedSocket)
        {
            HasSnap = snappedSocket != null;
            SnappedSocket = snappedSocket;

            if (snappedSocket != null)
            {
                SnappedPosition = snappedSocket.transform.position;
                SnappedRotation = snappedSocket.transform.rotation;
            }
        }

        public void ClearSnap()
        {
            HasSnap = false;
            SnappedSocket = null;
        }
    }
}
