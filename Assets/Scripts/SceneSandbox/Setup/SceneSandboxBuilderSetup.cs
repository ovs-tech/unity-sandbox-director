using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using SceneSandbox.Core;
using SceneSandbox.Data;
using SceneSandbox.UI;
using SceneSandbox.Input;

namespace SceneSandbox.Setup
{
    /// <summary>
    /// Helper script for setting up the Scene Sandbox Builder in a Unity scene
    /// </summary>
    public class SceneSandboxBuilderSetup : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private SceneObjectLibrary objectLibrary;
        [SerializeField] private Transform stageParent;
        [SerializeField] private Transform uiCanvas;
        
        [Header("UI Prefabs")]
        [SerializeField] private GameObject objectPalettePrefab;
        [SerializeField] private GameObject sandboxBuilderUIPrefab;
        
        [Header("Input Settings")]
        [SerializeField] private InputActionAsset inputActions;
        
        [Header("Grid Settings")]
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Material gridMaterial;
        
        private Core.SceneSandboxBuilder sandboxBuilder;
        private SandboxInputHandler inputHandler;
        private ObjectPalette objectPalette;
        private SandboxBuilderUI builderUI;
        
        private void Start()
        {
            SetupSceneSandboxBuilder();
        }
        
        private void SetupSceneSandboxBuilder()
        {
            // Create stage parent if not assigned
            if (stageParent == null)
            {
                GameObject stageObject = new GameObject("Stage");
                stageParent = stageObject.transform;
            }
            
            // Create UI canvas if not assigned
            if (uiCanvas == null)
            {
                GameObject canvasObject = new GameObject("SandboxUI");
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                uiCanvas = canvasObject.transform;
            }
            
            // Setup input handler
            SetupInputHandler();
            
            // Setup main sandbox builder
            SetupSandboxBuilder();
            
            // Setup UI components
            SetupUI();
            
            // Setup grid visualization
            if (showGrid)
            {
                SetupGrid();
            }
        }
        
        private void SetupInputHandler()
        {
            GameObject inputObject = new GameObject("SandboxInputHandler");
            inputHandler = inputObject.AddComponent<SandboxInputHandler>();
            
            if (inputActions != null)
            {
                inputHandler.Initialize(inputActions);
            }
        }
        
        private void SetupSandboxBuilder()
        {
            GameObject builderObject = new GameObject("SceneSandboxBuilder");
            sandboxBuilder = builderObject.AddComponent<Core.SceneSandboxBuilder>();
            
            // Set references via reflection or SerializedObject since they're private fields
            // For now, just ensure the object library is available in the scene
        }
        
        private void SetupUI()
        {
            // Create Object Palette
            if (objectPalettePrefab != null)
            {
                GameObject paletteObject = Instantiate(objectPalettePrefab, uiCanvas);
                objectPalette = paletteObject.GetComponent<ObjectPalette>();
                // Note: ObjectPalette expects _objectLibrary to be assigned in inspector
            }
            else
            {
                // Create basic object palette
                GameObject paletteObject = new GameObject("ObjectPalette");
                paletteObject.transform.SetParent(uiCanvas);
                objectPalette = paletteObject.AddComponent<ObjectPalette>();
                // Note: ObjectPalette expects _objectLibrary to be assigned in inspector
            }
            
            // Create Sandbox Builder UI
            if (sandboxBuilderUIPrefab != null)
            {
                GameObject uiObject = Instantiate(sandboxBuilderUIPrefab, uiCanvas);
                builderUI = uiObject.GetComponent<SandboxBuilderUI>();
                // Note: SandboxBuilderUI expects references to be assigned in inspector
            }
            else
            {
                // Create basic builder UI
                GameObject uiObject = new GameObject("SandboxBuilderUI");
                uiObject.transform.SetParent(uiCanvas);
                builderUI = uiObject.AddComponent<SandboxBuilderUI>();
                // Note: SandboxBuilderUI expects references to be assigned in inspector
            }
        }
        
        private void SetupGrid()
        {
            GameObject gridObject = new GameObject("GridVisualization");
            gridObject.transform.SetParent(stageParent);
            
            // Create a simple grid plane
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(gridObject.transform);
            plane.transform.localScale = Vector3.one * 10f; // 10x10 grid
            
            // Apply grid material if available
            if (gridMaterial != null)
            {
                var renderer = plane.GetComponent<Renderer>();
                renderer.material = gridMaterial;
            }
            
            // Remove collider to avoid interference
            var collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
        }
        
        #region Editor Helpers
        
        [ContextMenu("Auto-Setup Scene")]
        public void AutoSetupScene()
        {
            // Find existing components in scene
            if (objectLibrary == null)
            {
                objectLibrary = Resources.FindObjectsOfTypeAll<SceneObjectLibrary>()
                    .FirstOrDefault();
            }
            
            if (inputActions == null)
            {
                inputActions = Resources.FindObjectsOfTypeAll<InputActionAsset>()
                    .FirstOrDefault(asset => asset.name.Contains("UIAndGameplay"));
            }
            
            // Create basic stage setup
            if (stageParent == null)
            {
                var stageObject = GameObject.Find("Stage") ?? new GameObject("Stage");
                stageParent = stageObject.transform;
            }
            
            // Create UI canvas
            if (uiCanvas == null)
            {
                var canvasObject = FindFirstObjectByType<Canvas>()?.gameObject ?? CreateUICanvas();
                uiCanvas = canvasObject.transform;
            }
            
            Debug.Log("Scene auto-setup completed. Run the scene to initialize the Sandbox Builder.");
        }
        
        private GameObject CreateUICanvas()
        {
            GameObject canvasObject = new GameObject("SandboxUI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            return canvasObject;
        }
        
        #endregion
    }
}