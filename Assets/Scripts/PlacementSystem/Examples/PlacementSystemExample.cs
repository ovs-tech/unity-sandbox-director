using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Input;
using Systems.PlacementSystem.Strategies;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Visualization;
using Systems.PlacementSystem.Sockets;
using System.Linq;
using System;

namespace Systems.PlacementSystem.Examples
{
    /// <summary>
    /// Example script demonstrating how to set up and use the PlacementSystem programmatically.
    /// This can be used as a reference or attached to a manager GameObject.
    /// </summary>
    public class PlacementSystemExample : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField, Tooltip("Example prefabs to place")]
        private GameObject[] _placeablePrefabs;

        [Header("References")]
        [SerializeField]
        private PlacementController _placementController;

        [SerializeField]
        private Camera _placementCamera;

        [Header("UI Buttons Example")]
        [SerializeField, Tooltip("Index of prefab to place (for testing)")]
        private int _selectedPrefabIndex = 0;

        private void Start()
        {
            SetupPlacementSystemExample();
        }

        /// <summary>
        /// Example: How to set up the placement system programmatically.
        /// </summary>
        private void SetupPlacementSystemExample()
        {
            if (_placementController == null)
            {
                Debug.LogError("PlacementController not assigned!");
                return;
            }

            Debug.Log("PlacementSystem Example: Ready!");
            Debug.Log("Press '1' to start free placement");
            Debug.Log("Press '2' to start grid placement");
            Debug.Log("Press '3' to start hex placement");
            Debug.Log("Press 'S' to switch to selection tool");
            Debug.Log("Press 'M' to switch to move tool");
            Debug.Log("Press 'R' to switch to rotate tool");
            Debug.Log("Press 'Delete' to switch to delete tool");
        }

        private void Update()
        {
            // Example: Keyboard shortcuts to switch placement modes
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                StartFreePlacement();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                StartGridPlacement();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                StartHexPlacement();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                SetPlacementTool();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                SetSelectionTool();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                SetMoveTool();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                SetRotateTool();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Delete))
            {
                SetDeleteTool();
            }
        }

        private void SetPlacementTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Placement);
            _placementController.SetObjectToPlace(_placeablePrefabs[_selectedPrefabIndex]);
            _placementController.StartPlacement();
            Debug.Log("Switched to Placement tool");
        }

        /// <summary>
        /// Example: Start placement with free positioning.
        /// </summary>
        public void StartFreePlacement()
        {
            if (_placeablePrefabs.Length == 0)
            {
                Debug.LogWarning("No placeable prefabs assigned!");
                return;
            }

            // Remove any previously active placement mode
            RemovePreviousPlacementMode();

            var freeStrategy = GetOrAddComponent<FreePositionStrategy>();
            _placementController.SetPlacementStrategy(freeStrategy);

            Debug.Log("Started FREE placement mode");
        }

        /// <summary>
        /// Example: Start placement with grid snapping.
        /// </summary>
        public void StartGridPlacement()
        {
            if (_placeablePrefabs.Length == 0)
            {
                Debug.LogWarning("No placeable prefabs assigned!");
                return;
            }

            // Remove any previously active placement mode
            RemovePreviousPlacementMode();

            var gridStrategy = GetOrAddComponent<GridPlacementStrategy>();
            _placementController.SetPlacementStrategy(gridStrategy);

            Debug.Log("Started GRID placement mode");
        }

        /// <summary>
        /// Example: Start placement with hex grid snapping.
        /// </summary>
        public void StartHexPlacement()
        {
            if (_placeablePrefabs.Length == 0)
            {
                Debug.LogWarning("No placeable prefabs assigned!");
                return;
            }

            // Remove any previously active placement mode
            RemovePreviousPlacementMode();

            var hexStrategy = GetOrAddComponent<HexPlacementStrategy>();
            _placementController.SetPlacementStrategy(hexStrategy);

            Debug.Log("Started HEX placement mode");
        }

        /// <summary>
        /// Example: Cancel current placement.
        /// </summary>
        public void CancelPlacement()
        {
            _placementController.CancelPlacement();
            Debug.Log("Placement cancelled");
        }

        public void SetSelectionTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Selection);
            Debug.Log("Switched to Selection tool");
        }

        public void SetMoveTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Move);
            Debug.Log("Switched to Move tool");
        }

        public void SetRotateTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Rotate);
            Debug.Log("Switched to Rotate tool");
        }

        public void SetDeleteTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Delete);
            Debug.Log("Switched to Delete tool");
        }

        /// <summary>
        /// Example: Switch to next prefab.
        /// </summary>
        public void SelectNextPrefab()
        {
            if (_placeablePrefabs.Length == 0)
                return;

            _selectedPrefabIndex = (_selectedPrefabIndex + 1) % _placeablePrefabs.Length;
            Debug.Log($"Selected prefab: {_placeablePrefabs[_selectedPrefabIndex].name}");

            // If placement is active, switch to new prefab
            _placementController.SetObjectToPlace(_placeablePrefabs[_selectedPrefabIndex]);
        }

        /// <summary>
        /// Example: Create a socket programmatically.
        /// </summary>
        public Socket CreateSocket(Vector3 position, Quaternion rotation, SocketType socketType)
        {
            string name = socketType != null ? socketType.name : "Socket";
            GameObject socketObj = new GameObject($"Socket_{name}");
            socketObj.transform.position = position;
            socketObj.transform.rotation = rotation;

            // Ensure the socket has a collider so physics queries can detect it
            var collider = socketObj.AddComponent<SphereCollider>();
            collider.isTrigger = false;
            collider.radius = 0.25f;

            Socket socket = socketObj.AddComponent<Socket>();

            // Assign the private SocketType field via reflection (Socket exposes no setter)
            if (socketType != null)
            {
                SetPrivateField(socket, "_socketType", socketType);
            }

            // Register with SnapManager (create one if missing)
            var snapManager = FindObjectOfType<SnapManager>();
            if (snapManager == null)
            {
                var smObj = new GameObject("SnapManager");
                snapManager = smObj.AddComponent<SnapManager>();
            }

            snapManager.RegisterSocket(socket);
            snapManager.RefreshSocketCache();

            return socket;
        }

        /// <summary>
        /// Example: Create validation rules programmatically.
        /// </summary>
        public void SetupPlaceableObjectWithRules(GameObject prefab)
        {
            var placeableObject = prefab.GetComponent<PlaceableObject>();
            if (placeableObject == null)
            {
                placeableObject = prefab.AddComponent<PlaceableObject>();
            }

            // Create rules (normally you'd load these from assets)
            // ScriptableObjects should be created as assets in editor; creating at runtime is for demo only
            var clearance = ScriptableObject.CreateInstance<ClearanceRule>();
            // Configure clearance rule defaults via reflection
            SetPrivateField(clearance, "_checkBoxSize", new Vector3(1f, 1f, 1f));
            SetPrivateField(clearance, "_obstacleLayer", (LayerMask)Physics.DefaultRaycastLayers);
            SetPrivateField(clearance, "_checkOffset", Vector3.zero);
            SetPrivateField(clearance, "_maxAllowedOverlaps", 0);

            var surface = ScriptableObject.CreateInstance<RequireSurfaceRule>();
            SetPrivateField(surface, "_requiredSurfaceLayer", (LayerMask)Physics.DefaultRaycastLayers);
            SetPrivateField(surface, "_raycastDistance", 2f);
            SetPrivateField(surface, "_raycastStartOffset", 0.5f);
            SetPrivateField(surface, "_requireFlatSurface", true);
            SetPrivateField(surface, "_maxSurfaceAngle", 30f);

            // Attach rules to the placeable object
            placeableObject.AddRule(clearance);
            placeableObject.AddRule(surface);

            Debug.Log($"PlaceableObject setup on {prefab.name} with demo rules");
        }

        /// <summary>
        /// Helper: Get or add a component.
        /// </summary>
        private T GetOrAddComponent<T>() where T : Component
        {
            T component = _placementController.GetComponent<T>();
            if (component == null)
            {
                component = _placementController.gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Reflection helper: sets a private serialized field on a target object.
        /// Works for MonoBehaviour and ScriptableObject targets.
        /// </summary>
        private void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            if (target == null)
                return;

            var type = target.GetType();
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                // Try base types
                var bt = type.BaseType;
                while (field == null && bt != null)
                {
                    field = bt.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bt = bt.BaseType;
                }
            }

            if (field != null)
            {
                // Handle LayerMask conversion when assigning from int or LayerMask
                if (field.FieldType == typeof(LayerMask) && value is LayerMask lmValue)
                {
                    field.SetValue(target, lmValue);
                }
                else if (field.FieldType == typeof(LayerMask) && value is int intValue)
                {
                    field.SetValue(target, (LayerMask)intValue);
                }
                else
                {
                    field.SetValue(target, value);
                }
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on type {type.FullName}");
            }
        }

        /// <summary>
        /// Cancel any active placement and remove previous placement strategy components.
        /// </summary>
        private void RemovePreviousPlacementMode()
        {
            if (_placementController == null)
                return;

            // Cancel any active placement (destroys ghost, etc.)
            _placementController.CancelPlacement();

            // Remove any existing strategy components that implement IPlacementStrategy
            var strategies = _placementController.GetComponents<MonoBehaviour>()
                .Where(mb => mb is IPlacementStrategy)
                .ToArray();

            foreach (var s in strategies)
            {
                if (s == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(s);
                else
                    DestroyImmediate(s);
            }
        }

        // ===== Example UI Integration =====
        // These methods can be called from Unity UI Button OnClick events:

        public void UI_OnFreePlacementButtonClicked()
        {
            StartFreePlacement();
        }

        public void UI_OnGridPlacementButtonClicked()
        {
            StartGridPlacement();
        }

        public void UI_OnHexPlacementButtonClicked()
        {
            StartHexPlacement();
        }

        public void UI_OnCancelButtonClicked()
        {
            CancelPlacement();
        }

        public void UI_OnNextObjectButtonClicked()
        {
            SelectNextPrefab();
        }
    }
}
