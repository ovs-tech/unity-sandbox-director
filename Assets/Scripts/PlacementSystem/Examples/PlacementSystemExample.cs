using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Strategies;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Core.Components;

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

        private ScriptableObject _currentStrategy;

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
                return;
            }
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
        }

        /// <summary>
        /// Example: Start placement with free positioning.
        /// </summary>
        public void StartFreePlacement()
        {
            if (_placeablePrefabs.Length == 0)
            {
                return;
            }

            // Remove any previously active placement mode
            RemovePreviousPlacementMode();

            var freeStrategy = ScriptableObject.CreateInstance<FreePositionStrategy>();
            _currentStrategy = freeStrategy;
            _placementController.SetPlacementStrategy(freeStrategy);
        }

        /// <summary>
        /// Example: Start placement with grid snapping.
        /// </summary>
        public void StartGridPlacement()
        {
            if (_placeablePrefabs.Length == 0)
            {
                return;
            }

            // Remove any previously active placement mode
            RemovePreviousPlacementMode();

            var gridStrategy = ScriptableObject.CreateInstance<GridPlacementStrategy>();
            _currentStrategy = gridStrategy;
            _placementController.SetPlacementStrategy(gridStrategy);
        }

        /// <summary>
        /// Example: Start placement with hex grid snapping.
        /// </summary>
        public void StartHexPlacement()
        {
            if (_placeablePrefabs.Length == 0)
            {
                return;
            }

            // Remove any previously active placement mode
            RemovePreviousPlacementMode();

            var hexStrategy = ScriptableObject.CreateInstance<HexPlacementStrategy>();
            _currentStrategy = hexStrategy;
            _placementController.SetPlacementStrategy(hexStrategy);
        }

        /// <summary>
        /// Example: Cancel current placement.
        /// </summary>
        public void CancelPlacement()
        {
            _placementController.CancelPlacement();
        }

        public void SetSelectionTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Selection);
        }

        public void SetMoveTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Move);
        }

        public void SetRotateTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Rotate);
        }

        public void SetDeleteTool()
        {
            _placementController.SetActiveTool(PlacementController.PlacementToolType.Delete);
        }

        /// <summary>
        /// Example: Switch to next prefab.
        /// </summary>
        public void SelectNextPrefab()
        {
            if (_placeablePrefabs.Length == 0)
                return;

            _selectedPrefabIndex = (_selectedPrefabIndex + 1) % _placeablePrefabs.Length;

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
            var snapManager = FindFirstObjectByType<SnapManager>();
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
        public void SetupPartWithRules(GameObject prefab)
        {
            var part = prefab.GetComponent<PlacementPart>();
            if (part == null)
            {
                part = prefab.AddComponent<PlacementPart>();
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
            part.AddRule(clearance);
            part.AddRule(surface);

            
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

            // Destroy the runtime generated ScriptableObject strategy
            if (_currentStrategy != null)
            {
                if (Application.isPlaying)
                    Destroy(_currentStrategy);
                else
                    DestroyImmediate(_currentStrategy);
                
                _currentStrategy = null;
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
