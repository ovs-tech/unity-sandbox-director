using UnityEngine;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Visual gizmo for transform manipulation - rendering only
    /// Input and mode management handled by TransformControlManager
    /// </summary>
    public class TransformableItemGizmo : MonoBehaviour
    {
        public enum SpaceMode { World, Local }

        [Header("Target")]
        public Transform target;
        
        [Header("Auto-Sync")]
        [SerializeField] private bool _autoSync = true;

        [Header("Gizmo Settings")]
        public float screenScale = 0.12f;
        public LayerMask gizmoLayer;

        [Header("Visuals")]
        public Color xColor = new Color(1, 0.2f, 0.2f);
        public Color yColor = new Color(0.2f, 1, 0.2f);
        public Color zColor = new Color(0.2f, 0.6f, 1f);
        public Color highlight = Color.yellow;
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        // Public properties for external control
        public TransformMode CurrentMode { get; private set; } = TransformMode.None;
        public SpaceMode CurrentSpaceMode { get; private set; } = SpaceMode.World;
        public bool IsVisible { get; private set; } = false;

        Transform root;
        AxisHandle xHandle, yHandle, zHandle;
        Camera cam;
        
        // Reference to the TransformableItem for auto-sync
        private TransformableItem _transformableItem;

        // Events for interaction
        public System.Action<Vector3> OnAxisDragStarted;
        public System.Action<Vector3, float> OnAxisDragUpdated; // axis, amount
        public System.Action OnAxisDragEnded;

        void Awake()
        {
            cam = Camera.main;
            
            // Auto-find target and DraggableItem if not set
            if (target == null)
                target = transform;
                
            // Find TransformableItem for auto-sync
            if (_autoSync)
            {
                _transformableItem = target.GetComponent<TransformableItem>();
                if (_transformableItem == null)
                {
                    // Try parent objects
                    _transformableItem = target.GetComponentInParent<TransformableItem>();
                }
            }
            
            CreateRig();
            SetVisible(false);
        }

        void Update()
        {
            if (!target || !cam) 
            { 
                SetVisible(false); 
                return; 
            }

            // Auto-sync with TransformableItem's current mode
            if (_autoSync && _transformableItem != null)
            {
                var itemMode = _transformableItem.CurrentTransformMode;
                var itemInTransformMode = _transformableItem.IsInTransformMode;
                
                // Show gizmo when item is in transform mode, hide when not
                SetVisible(itemInTransformMode);
                
                // Sync the mode
                if (CurrentMode != itemMode)
                {
                    CurrentMode = itemMode;
                }
            }

            if (!IsVisible) 
            { 
                return; 
            }

            // Keep gizmo aligned & scaled
            root.position = target.position;
            root.rotation = (CurrentSpaceMode == SpaceMode.Local) ? target.rotation : Quaternion.identity;
            float dist = Vector3.Distance(cam.transform.position, root.position);
            root.localScale = Vector3.one * Mathf.Max(0.0001f, dist * screenScale);

            UpdateVisuals();
        }

        #region Public Control Methods

        /// <summary>
        /// Set the gizmo mode (controlled by TransformControlManager or auto-synced from TransformableItem)
        /// </summary>
        public void SetMode(TransformMode mode)
        {
            // Only allow manual mode setting if auto-sync is disabled
            if (!_autoSync)
            {
                CurrentMode = mode;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Set the space mode (controlled by TransformControlManager)
        /// </summary>
        public void SetSpaceMode(SpaceMode spaceMode)
        {
            CurrentSpaceMode = spaceMode;
        }

        /// <summary>
        /// Show or hide the gizmo (controlled by TransformControlManager or auto-synced from TransformableItem)
        /// </summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (!root) return;
            
            root.gameObject.SetActive(visible);
            if (visible)
            {
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Highlight a specific axis
        /// </summary>
        public void SetAxisHighlight(int axisIndex, bool highlight)
        {
            var handles = new[] { xHandle, yHandle, zHandle };
            if (axisIndex >= 0 && axisIndex < handles.Length)
            {
                handles[axisIndex].SetHighlight(highlight, this.highlight);
            }
        }

        /// <summary>
        /// Clear all highlights
        /// </summary>
        public void ClearHighlights()
        {
            xHandle?.SetHighlight(false, highlight);
            yHandle?.SetHighlight(false, highlight);
            zHandle?.SetHighlight(false, highlight);
        }

        /// <summary>
        /// Get axis handle for raycast checking (called by TransformControlManager)
        /// </summary>
        public int GetAxisFromRaycast(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, gizmoLayer))
            {
                var axisHandle = hit.collider.GetComponentInParent<AxisHandle>();
                if (axisHandle == xHandle) return 0;
                if (axisHandle == yHandle) return 1;
                if (axisHandle == zHandle) return 2;
            }
            return -1;
        }

        /// <summary>
        /// Get world axis direction for the given axis index
        /// </summary>
        public Vector3 GetWorldAxis(int axisIndex)
        {
            Vector3 localAxis = axisIndex switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                2 => Vector3.forward,
                _ => Vector3.zero
            };

            return (CurrentSpaceMode == SpaceMode.Local) 
                ? target.TransformDirection(localAxis) 
                : localAxis;
        }

        /// <summary>
        /// Enable or disable auto-sync with TransformableItem
        /// </summary>
        public void SetAutoSync(bool enabled)
        {
            _autoSync = enabled;
            
            if (enabled && _transformableItem == null && target != null)
            {
                // Try to find TransformableItem again
                _transformableItem = target.GetComponent<TransformableItem>();
                if (_transformableItem == null)
                {
                    _transformableItem = target.GetComponentInParent<TransformableItem>();
                }
            }
        }

        #endregion

        #region Private Methods

        private void UpdateVisuals()
        {
            if (!root) return;

            xHandle?.Show(CurrentMode);
            yHandle?.Show(CurrentMode);
            zHandle?.Show(CurrentMode);
        }

        // ---------- Rig ----------
        void CreateRig()
        {
            root = new GameObject("RuntimeGizmo").transform;
            root.SetParent(transform, false);

            xHandle = CreateAxis("X", Vector3.right, xColor);
            yHandle = CreateAxis("Y", Vector3.up, yColor);
            zHandle = CreateAxis("Z", Vector3.forward, zColor);
        }

        AxisHandle CreateAxis(string name, Vector3 axis, Color col)
        {
            var go = new GameObject($"{name}Handle");
            go.layer = LayerMaskToLayer(gizmoLayer);
            go.transform.SetParent(root, false);

            var h = go.AddComponent<AxisHandle>();
            h.localAxis = axis;
            h.BuildArrow();
            h.BuildRing();
            h.BuildBox(); // For scale mode
            h.SetBaseColor(col);
            return h;
        }

        int LayerMaskToLayer(LayerMask mask)
        {
            int m = mask.value;
            for (int i = 0; i < 32; i++) if ((m & (1 << i)) != 0) return i;
            return 0;
        }

        #endregion

        // -------- Nested helper component --------
        class AxisHandle : MonoBehaviour
        {
            public Vector3 localAxis = Vector3.right;

            MeshRenderer arrowMr, ringMr, boxMr;
            Color baseColor = Color.white;
            Collider arrowCol, ringCol, boxCol;

            public void BuildArrow()
            {
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.transform.SetParent(transform, false);
                shaft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                shaft.transform.localPosition = localAxis * 0.6f;
                shaft.transform.localScale = new Vector3(0.04f, 0.6f, 0.04f);

                var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tip.transform.SetParent(transform, false);
                tip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                tip.transform.localPosition = localAxis * 1.3f;
                tip.transform.localScale = new Vector3(0.10f, 0.2f, 0.10f);

                arrowMr = shaft.GetComponent<MeshRenderer>();
                var tipMr = tip.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Unlit/Color"));
                arrowMr.material = mat;
                tipMr.material = mat;

                arrowCol = tip.GetComponent<Collider>();
                shaft.GetComponent<Collider>().enabled = false;
            }

            public void BuildRing()
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.SetParent(transform, false);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                ring.transform.localScale = new Vector3(1.2f, 0.01f, 1.2f);

                ringMr = ring.GetComponent<MeshRenderer>();
                ringMr.material = new Material(Shader.Find("Unlit/Color"));

                ringCol = ring.GetComponent<Collider>();
            }

            public void BuildBox()
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(transform, false);
                box.transform.localPosition = localAxis * 1.0f;
                box.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

                boxMr = box.GetComponent<MeshRenderer>();
                boxMr.material = new Material(Shader.Find("Unlit/Color"));

                boxCol = box.GetComponent<Collider>();
            }

            public void SetBaseColor(Color c)
            {
                baseColor = c;
                if (arrowMr) arrowMr.sharedMaterial.color = c;
                if (ringMr) ringMr.sharedMaterial.color = c;
                if (boxMr) boxMr.sharedMaterial.color = c;
            }

            public void SetHighlight(bool on, Color hl)
            {
                Color c = on ? hl : baseColor;
                if (arrowMr) arrowMr.sharedMaterial.color = c;
                if (ringMr) ringMr.sharedMaterial.color = c;
                if (boxMr) boxMr.sharedMaterial.color = c;
            }

            public void Show(TransformMode mode)
            {
                bool showArrow = mode == TransformMode.Move;
                bool showRing = mode == TransformMode.Rotate;
                bool showBox = mode == TransformMode.Scale;
                
                // Hide all when mode is None
                if (mode == TransformMode.None)
                {
                    showArrow = showRing = showBox = false;
                }

                if (arrowMr) arrowMr.enabled = showArrow;
                if (arrowCol) arrowCol.enabled = showArrow;
                
                if (ringMr) ringMr.enabled = showRing;
                if (ringCol) ringCol.enabled = showRing;
                
                if (boxMr) boxMr.enabled = showBox;
                if (boxCol) boxCol.enabled = showBox;
            }
        }
    }
}