using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SceneSandbox.Core;
using SceneSandbox.UI;
using SceneSandbox.Data;

namespace SceneSandbox.Demo
{
    /// <summary>
    /// Quick demo setup script for Scene Sandbox Builder
    /// Creates a complete setup with UI when no prefabs are available
    /// </summary>
    public class QuickSandboxDemo : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool createOnStart = true;
        [SerializeField] private bool createDemoObjects = true;
        [SerializeField] private bool createDemoObjectLibrary = true;
        [SerializeField] private SceneObjectLibrary objectLibrary;
        
        [Header("Scene Setup")]
        [SerializeField] private string sceneTitle = "Quick Sandbox Demo";
        
        private void Start()
        {
            if (createOnStart)
            {
                CreateQuickDemo();
            }
        }
        
        [ContextMenu("Create Quick Demo")]
        public void CreateQuickDemo()
        {
            // Create EventSystem if none exists
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }
            
            // Create main canvas
            var canvasGO = CreateMainCanvas();
            
            if(objectLibrary == null && createDemoObjectLibrary)
            {
                objectLibrary = CreateDemoObjectLibrary();
            }
            
            // Create sandbox builder
            var sandboxBuilder = CreateSandboxBuilder(objectLibrary);
            
            // Create object palette
            var objectPalette = CreateObjectPalette(canvasGO.transform, objectLibrary);
            
            // Create sandbox builder UI
            var builderUI = CreateSandboxBuilderUI(canvasGO.transform);
            
            // Link the sandbox builder to the UI components
            LinkSandboxBuilderToUI(sandboxBuilder, objectPalette, builderUI);
            
            // Create stage area
            CreateStageArea();
            
            Debug.Log($"[QuickSandboxDemo] {sceneTitle} created successfully!");
            Debug.Log("- Object Palette: Left side of screen");
            Debug.Log("- Scene Controls: Top of screen");
            Debug.Log("- Properties Panel: Right side of screen (toggle with mobile controls)");
            Debug.Log("- Try dragging objects from the palette to the scene!");
        }
        
        private GameObject CreateMainCanvas()
        {
            var canvasGO = new GameObject("Sandbox UI Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            return canvasGO;
        }
        
        private SceneObjectLibrary CreateDemoObjectLibrary()
        {
            var library = ScriptableObject.CreateInstance<SceneObjectLibrary>();
            library.name = "Demo Object Library";
            
            if (createDemoObjects)
            {
                // Create some basic demo objects
                var demoObjects = new SceneObjectData[]
                {
                    new SceneObjectData("Demo Cube", null, SceneObjectType.Prop)
                    {
                        id = "demo_cube",
                        category = "Demo"
                    },
                    new SceneObjectData("Demo Sphere", null, SceneObjectType.Prop)
                    {
                        id = "demo_sphere",
                        category = "Demo"
                    },
                    new SceneObjectData("Demo Camera", null, SceneObjectType.Camera)
                    {
                        id = "demo_camera",
                        category = "Camera"
                    }
                };
                
                // Add objects to library using reflection (since sceneObjects is private)
                var sceneObjectsField = typeof(SceneObjectLibrary).GetField("sceneObjects", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sceneObjectsField != null)
                {
                    sceneObjectsField.SetValue(library, new System.Collections.Generic.List<SceneObjectData>(demoObjects));
                }
            }
            
            return library;
        }
        
        private Core.SceneSandboxBuilder CreateSandboxBuilder(SceneObjectLibrary objectLibrary)
        {
            var builderGO = new GameObject("Scene Sandbox Builder");
            var builder = builderGO.AddComponent<SceneSandboxBuilder>();
            
            // Assign the object library using the proper setter method
            builder.SetObjectLibrary(objectLibrary);
            
            return builder;
        }
        
        private ObjectPalette CreateObjectPalette(Transform canvasTransform, SceneObjectLibrary objectLibrary)
        {
            var paletteGO = new GameObject("Object Palette");
            paletteGO.transform.SetParent(canvasTransform, false);
            
            var palette = paletteGO.AddComponent<ObjectPalette>();
            
            // Assign object library
            var objectLibraryProperty = typeof(ObjectPalette).GetProperty("ObjectLibrary");
            if (objectLibraryProperty != null)
            {
                objectLibraryProperty.SetValue(palette, objectLibrary);
            }
            
            return palette;
        }
        
        private SandboxBuilderUI CreateSandboxBuilderUI(Transform canvasTransform)
        {
            var uiGO = new GameObject("Sandbox Builder UI");
            uiGO.transform.SetParent(canvasTransform, false);
            
            var builderUI = uiGO.AddComponent<SandboxBuilderUI>();
            
            return builderUI;
        }
        
        private void CreateStageArea()
        {
            // Create a simple stage area with a ground plane
            var stageGO = new GameObject("Stage Area");
            
            // Create ground plane
            var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "Ground";
            groundPlane.transform.SetParent(stageGO.transform);
            groundPlane.transform.localScale = Vector3.one * 10f;
            
            // Make the ground plane look like a stage
            var groundRenderer = groundPlane.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                groundRenderer.material.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
            
            // Position camera for good view
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 5, -10);
                mainCamera.transform.LookAt(Vector3.zero);
            }
            
            // Add some basic lighting
            if (FindFirstObjectByType<Light>() == null)
            {
                var lightGO = new GameObject("Main Light");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGO.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            }
        }
        
        /// <summary>
        /// Helper method to create primitive objects for demo purposes
        /// </summary>
        public static GameObject CreateDemoPrimitive(string objectId, Vector3 position)
        {
            GameObject primitive = null;
            
            switch (objectId)
            {
                case "demo_cube":
                    primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    primitive.name = "Demo Cube";
                    break;
                case "demo_sphere":
                    primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    primitive.name = "Demo Sphere";
                    break;
                case "demo_camera":
                    primitive = new GameObject("Demo Camera");
                    primitive.AddComponent<Camera>();
                    break;
                default:
                    primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    primitive.name = "Unknown Object";
                    break;
            }
            
            if (primitive != null)
            {
                primitive.transform.position = position;
                
                // Add some visual variety
                var renderer = primitive.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = new Color(
                        Random.Range(0.3f, 1f), 
                        Random.Range(0.3f, 1f), 
                        Random.Range(0.3f, 1f), 
                        1f
                    );
                    renderer.material = material;
                }
            }
            
            return primitive;
        }
        
        /// <summary>
        /// Link the sandbox builder to UI components using reflection
        /// </summary>
        private void LinkSandboxBuilderToUI(Core.SceneSandboxBuilder sandboxBuilder, ObjectPalette objectPalette, SandboxBuilderUI builderUI)
        {
            // Link sandbox builder to object palette
            var sandboxBuilderField = typeof(ObjectPalette).GetField("_sandboxBuilder", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sandboxBuilderField != null)
            {
                sandboxBuilderField.SetValue(objectPalette, sandboxBuilder);
            }
            
            // Link sandbox builder to builder UI
            var builderField = typeof(SandboxBuilderUI).GetField("_sandboxBuilder", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (builderField != null)
            {
                builderField.SetValue(builderUI, sandboxBuilder);
            }
            
            Debug.Log("[QuickSandboxDemo] Components linked successfully!");
        }
    }
}