using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSandbox.Serialization
{
    /// <summary>
    /// Factory for creating and managing sandbox objects and their serialization.
    /// Handles conversion between runtime GameObjects and serializable data.
    /// </summary>
    public static class SandboxObjectFactory
    {
        /// <summary>
        /// Dictionary mapping object type names to their handler functions
        /// </summary>
        private static readonly Dictionary<string, IObjectTypeHandler> _typeHandlers = new Dictionary<string, IObjectTypeHandler>();
        
        /// <summary>
        /// List of known object types
        /// </summary>
        private static readonly List<string> _knownObjectTypes = new List<string>();

        static SandboxObjectFactory()
        {
            InitializeBuiltInHandlers();
        }

        #region Public Methods

        /// <summary>
        /// Get all available object types
        /// </summary>
        /// <returns>List of object type names</returns>
        public static List<string> GetAvailableObjectTypes()
        {
            return new List<string>(_knownObjectTypes);
        }

        /// <summary>
        /// Check if an object type is supported
        /// </summary>
        /// <param name="objectType">Type name to check</param>
        /// <returns>True if supported</returns>
        public static bool IsObjectTypeSupported(string objectType)
        {
            return _typeHandlers.ContainsKey(objectType);
        }

        /// <summary>
        /// Create a new runtime object from a placed object data
        /// </summary>
        /// <param name="data">Object data to instantiate</param>
        /// <param name="parent">Parent transform (optional)</param>
        /// <returns>Created GameObject or null if failed</returns>
        public static GameObject CreateRuntimeObjectFromData(PlacedObjectData data, Transform parent = null)
        {
            try
            {
                if (!IsObjectTypeSupported(data.objectType))
                {
                    Debug.LogWarning($"[SandboxObjectFactory] Unsupported object type: {data.objectType}");
                    return CreateFallbackObject(data, parent);
                }

                var handler = _typeHandlers[data.objectType];
                var gameObject = handler.CreateRuntimeObject(data, parent);

                if (gameObject != null)
                {
                    // Set basic transform properties
                    gameObject.transform.position = data.position;
                    gameObject.transform.rotation = data.rotation;
                    gameObject.transform.localScale = data.scale;
                    
                    // Set name and tag
                    gameObject.name = string.IsNullOrEmpty(data.customName) ? $"{data.objectType}_{data.instanceId}" : data.customName;
                    if (!string.IsNullOrEmpty(data.tag))
                        gameObject.tag = data.tag;
                    
                    // Set layer
                    gameObject.layer = data.layer;
                    
                    // Apply custom properties if the object supports it
                    if (gameObject.TryGetComponent<ISandboxCustomizable>(out var customizable))
                    {
                        customizable.ApplyCustomProperties(data.customProperties);
                    }

                    Debug.Log($"[SandboxObjectFactory] Created runtime object: {gameObject.name} ({data.objectType})");
                    return gameObject;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxObjectFactory] Error creating runtime object from data: {e.Message}");
            }

            return CreateFallbackObject(data, parent);
        }

        /// <summary>
        /// Create placed object data from a runtime GameObject
        /// </summary>
        /// <param name="gameObject">GameObject to serialize</param>
        /// <param name="instanceId">Unique instance ID</param>
        /// <param name="objectType">Type override (optional)</param>
        /// <returns>Serializable object data</returns>
        public static PlacedObjectData CreateDataFromRuntimeObject(GameObject gameObject, string instanceId, string objectType = null)
        {
            try
            {
                // Determine object type if not provided
                if (string.IsNullOrEmpty(objectType))
                {
                    objectType = DetermineObjectType(gameObject);
                }

                var data = new PlacedObjectData
                {
                    instanceId = instanceId,
                    objectType = objectType,
                    position = gameObject.transform.position,
                    rotation = gameObject.transform.rotation,
                    scale = gameObject.transform.localScale,
                    customName = gameObject.name,
                    tag = gameObject.tag,
                    layer = gameObject.layer,
                    isActive = gameObject.activeInHierarchy,
                    customProperties = new Dictionary<string, object>()
                };

                // Extract custom properties if the object supports it
                if (gameObject.TryGetComponent<ISandboxCustomizable>(out var customizable))
                {
                    data.customProperties = customizable.GetCustomProperties();
                }

                // Use type-specific handler for additional data extraction
                if (IsObjectTypeSupported(objectType))
                {
                    var handler = _typeHandlers[objectType];
                    handler.ExtractAdditionalData(gameObject, data);
                }

                Debug.Log($"[SandboxObjectFactory] Created data from runtime object: {gameObject.name} ({objectType})");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxObjectFactory] Error creating data from runtime object: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update runtime object from changed data
        /// </summary>
        /// <param name="gameObject">Object to update</param>
        /// <param name="data">Updated data</param>
        /// <returns>True if successful</returns>
        public static bool UpdateRuntimeObjectFromData(GameObject gameObject, PlacedObjectData data)
        {
            try
            {
                // Update basic transform properties
                gameObject.transform.position = data.position;
                gameObject.transform.rotation = data.rotation;
                gameObject.transform.localScale = data.scale;

                // Update name, tag, layer
                gameObject.name = string.IsNullOrEmpty(data.customName) ? $"{data.objectType}_{data.instanceId}" : data.customName;
                if (!string.IsNullOrEmpty(data.tag))
                    gameObject.tag = data.tag;
                gameObject.layer = data.layer;
                gameObject.SetActive(data.isActive);

                // Apply custom properties
                if (gameObject.TryGetComponent<ISandboxCustomizable>(out var customizable))
                {
                    customizable.ApplyCustomProperties(data.customProperties);
                }

                // Use type-specific handler for additional updates
                if (IsObjectTypeSupported(data.objectType))
                {
                    var handler = _typeHandlers[data.objectType];
                    handler.UpdateRuntimeObject(gameObject, data);
                }

                Debug.Log($"[SandboxObjectFactory] Updated runtime object: {gameObject.name}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SandboxObjectFactory] Error updating runtime object: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get object type name from a GameObject
        /// </summary>
        /// <param name="gameObject">GameObject to analyze</param>
        /// <returns>Object type name</returns>
        public static string DetermineObjectType(GameObject gameObject)
        {
            // Check for explicit sandbox object component
            if (gameObject.TryGetComponent<SandboxObject>(out var sandboxObject))
            {
                return sandboxObject.ObjectType;
            }

            // Check for common Unity components
            if (gameObject.TryGetComponent<Light>(out _))
                return "Light";
            
            if (gameObject.TryGetComponent<Camera>(out _))
                return "Camera";
            
            if (gameObject.TryGetComponent<AudioSource>(out _))
                return "AudioSource";
            
            if (gameObject.TryGetComponent<ParticleSystem>(out _))
                return "ParticleSystem";
            
            if (gameObject.TryGetComponent<MeshRenderer>(out _))
                return "MeshRenderer";
            
            if (gameObject.TryGetComponent<SkinnedMeshRenderer>(out _))
                return "SkinnedMeshRenderer";

            // Check for UMA character
            if (gameObject.name.Contains("UMA") || gameObject.GetComponent("UMAData") != null)
                return "UMACharacter";

            // Default to generic object
            return "GameObject";
        }

        /// <summary>
        /// Register a custom object type handler
        /// </summary>
        /// <param name="objectType">Type name</param>
        /// <param name="handler">Handler implementation</param>
        public static void RegisterObjectTypeHandler(string objectType, IObjectTypeHandler handler)
        {
            _typeHandlers[objectType] = handler;
            if (!_knownObjectTypes.Contains(objectType))
            {
                _knownObjectTypes.Add(objectType);
            }
            
            Debug.Log($"[SandboxObjectFactory] Registered handler for object type: {objectType}");
        }

        /// <summary>
        /// Unregister an object type handler
        /// </summary>
        /// <param name="objectType">Type name to unregister</param>
        public static void UnregisterObjectTypeHandler(string objectType)
        {
            if (_typeHandlers.Remove(objectType))
            {
                _knownObjectTypes.Remove(objectType);
                Debug.Log($"[SandboxObjectFactory] Unregistered handler for object type: {objectType}");
            }
        }

        /// <summary>
        /// Get preview information for an object type
        /// </summary>
        /// <param name="objectType">Object type</param>
        /// <returns>Preview info or null</returns>
        public static ObjectPreviewInfo GetObjectPreviewInfo(string objectType)
        {
            if (IsObjectTypeSupported(objectType))
            {
                return _typeHandlers[objectType].GetPreviewInfo();
            }
            
            return null;
        }

        /// <summary>
        /// Validate an object configuration
        /// </summary>
        /// <param name="data">Object data to validate</param>
        /// <returns>Validation result</returns>
        public static ObjectValidationResult ValidateObjectData(PlacedObjectData data)
        {
            var result = new ObjectValidationResult { isValid = true };

            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(data.instanceId))
                {
                    result.isValid = false;
                    result.errors.Add("Instance ID cannot be empty");
                }

                if (string.IsNullOrEmpty(data.objectType))
                {
                    result.isValid = false;
                    result.errors.Add("Object type cannot be empty");
                }
                else if (!IsObjectTypeSupported(data.objectType))
                {
                    result.warnings.Add($"Object type '{data.objectType}' is not supported and will use fallback");
                }

                // Type-specific validation
                if (IsObjectTypeSupported(data.objectType))
                {
                    var handler = _typeHandlers[data.objectType];
                    var typeResult = handler.ValidateData(data);
                    
                    if (!typeResult.isValid)
                    {
                        result.isValid = false;
                        result.errors.AddRange(typeResult.errors);
                    }
                    
                    result.warnings.AddRange(typeResult.warnings);
                }
            }
            catch (Exception e)
            {
                result.isValid = false;
                result.errors.Add($"Validation error: {e.Message}");
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize built-in object type handlers
        /// </summary>
        private static void InitializeBuiltInHandlers()
        {
            // Register built-in handlers
            RegisterObjectTypeHandler("GameObject", new GameObjectHandler());
            RegisterObjectTypeHandler("Light", new LightHandler());
            RegisterObjectTypeHandler("Camera", new CameraHandler());
            RegisterObjectTypeHandler("AudioSource", new AudioSourceHandler());
            RegisterObjectTypeHandler("ParticleSystem", new ParticleSystemHandler());
            RegisterObjectTypeHandler("MeshRenderer", new MeshRendererHandler());
            RegisterObjectTypeHandler("SkinnedMeshRenderer", new SkinnedMeshRendererHandler());
            RegisterObjectTypeHandler("UMACharacter", new UMACharacterHandler());
            
            Debug.Log($"[SandboxObjectFactory] Initialized {_typeHandlers.Count} built-in object type handlers");
        }

        /// <summary>
        /// Create a fallback object when the specific type handler fails
        /// </summary>
        /// <param name="data">Object data</param>
        /// <param name="parent">Parent transform</param>
        /// <returns>Fallback GameObject</returns>
        private static GameObject CreateFallbackObject(PlacedObjectData data, Transform parent)
        {
            var fallbackObject = new GameObject($"FALLBACK_{data.objectType}_{data.instanceId}");
            
            if (parent != null)
                fallbackObject.transform.SetParent(parent);
                
            fallbackObject.transform.position = data.position;
            fallbackObject.transform.rotation = data.rotation;
            fallbackObject.transform.localScale = data.scale;

            // Add a visual indicator that this is a fallback object
            var renderer = fallbackObject.AddComponent<MeshRenderer>();
            var filter = fallbackObject.AddComponent<MeshFilter>();
            
            // Create a simple cube mesh as fallback
            filter.mesh = CreateFallbackMesh();
            renderer.material = CreateFallbackMaterial();

            Debug.LogWarning($"[SandboxObjectFactory] Created fallback object for unsupported type: {data.objectType}");
            return fallbackObject;
        }

        /// <summary>
        /// Create a simple cube mesh for fallback objects
        /// </summary>
        private static Mesh CreateFallbackMesh()
        {
            var mesh = new Mesh();
            mesh.name = "Fallback Cube";

            Vector3[] vertices = {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
            };

            int[] triangles = {
                0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 1, 5, 6,
                0, 7, 4, 0, 4, 3, 5, 4, 7, 5, 7, 6, 0, 6, 7, 0, 1, 6
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }

        /// <summary>
        /// Create a fallback material with bright color for visibility
        /// </summary>
        private static Material CreateFallbackMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.magenta; // Bright magenta to indicate fallback
            material.name = "Fallback Material";
            return material;
        }

        #endregion
    }

    #region Interfaces and Data Classes

    /// <summary>
    /// Interface for custom object type handlers
    /// </summary>
    public interface IObjectTypeHandler
    {
        /// <summary>
        /// Create a runtime GameObject from serialized data
        /// </summary>
        GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent);
        
        /// <summary>
        /// Extract additional type-specific data from a GameObject
        /// </summary>
        void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data);
        
        /// <summary>
        /// Update a runtime object with new data
        /// </summary>
        void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data);
        
        /// <summary>
        /// Validate object data for this type
        /// </summary>
        ObjectValidationResult ValidateData(PlacedObjectData data);
        
        /// <summary>
        /// Get preview information for this object type
        /// </summary>
        ObjectPreviewInfo GetPreviewInfo();
    }

    /// <summary>
    /// Interface for sandbox objects that support custom properties
    /// </summary>
    public interface ISandboxCustomizable
    {
        /// <summary>
        /// Get custom properties for serialization
        /// </summary>
        Dictionary<string, object> GetCustomProperties();
        
        /// <summary>
        /// Apply custom properties from deserialization
        /// </summary>
        void ApplyCustomProperties(Dictionary<string, object> properties);
    }

    /// <summary>
    /// Preview information for an object type
    /// </summary>
    [Serializable]
    public class ObjectPreviewInfo
    {
        public string displayName;
        public string description;
        public Texture2D icon;
        public string category;
        public bool requiresAssetReference;
        public string defaultAssetPath;
        public Vector3 defaultScale = Vector3.one;
        public bool supportsCustomProperties;
    }

    /// <summary>
    /// Result of object data validation
    /// </summary>
    public class ObjectValidationResult
    {
        public bool isValid = true;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
    }

    /// <summary>
    /// Component to mark GameObjects as sandbox objects
    /// </summary>
    public class SandboxObject : MonoBehaviour
    {
        [SerializeField] private string _objectType = "GameObject";
        [SerializeField] private string _instanceId;
        [SerializeField] private bool _isTemplate = false;
        
        public string ObjectType => _objectType;
        public string InstanceId => _instanceId;
        public bool IsTemplate => _isTemplate;

        public void Initialize(string objectType, string instanceId, bool isTemplate = false)
        {
            _objectType = objectType;
            _instanceId = instanceId;
            _isTemplate = isTemplate;
        }
    }

    #endregion

    #region Built-in Object Type Handlers

    /// <summary>
    /// Handler for basic GameObjects
    /// </summary>
    public class GameObjectHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"{data.objectType}_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            // Add sandbox object component
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            // Basic GameObject has no additional data to extract
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            // Basic GameObject has no additional updates needed
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "GameObject",
                description = "Basic Unity GameObject",
                category = "Basic",
                requiresAssetReference = false,
                supportsCustomProperties = true
            };
        }
    }

    /// <summary>
    /// Handler for Light objects
    /// </summary>
    public class LightHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"Light_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point; // Default type
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<Light>(out var light))
            {
                data.customProperties["lightType"] = light.type.ToString();
                data.customProperties["color"] = $"{light.color.r},{light.color.g},{light.color.b},{light.color.a}";
                data.customProperties["intensity"] = light.intensity;
                data.customProperties["range"] = light.range;
            }
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<Light>(out var light) && data.customProperties != null)
            {
                if (data.customProperties.TryGetValue("lightType", out var typeValue))
                    if (Enum.TryParse<LightType>(typeValue.ToString(), out var lightType))
                        light.type = lightType;

                if (data.customProperties.TryGetValue("color", out var colorValue))
                    light.color = ParseColor(colorValue.ToString());

                if (data.customProperties.TryGetValue("intensity", out var intensityValue))
                    if (float.TryParse(intensityValue.ToString(), out var intensity))
                        light.intensity = intensity;

                if (data.customProperties.TryGetValue("range", out var rangeValue))
                    if (float.TryParse(rangeValue.ToString(), out var range))
                        light.range = range;
            }
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "Light",
                description = "Unity Light component",
                category = "Lighting",
                requiresAssetReference = false,
                supportsCustomProperties = true
            };
        }

        private Color ParseColor(string colorString)
        {
            try
            {
                string[] parts = colorString.Split(',');
                if (parts.Length == 4)
                {
                    return new Color(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                }
            }
            catch (Exception) { }
            
            return Color.white;
        }
    }

    /// <summary>
    /// Handler for Camera objects
    /// </summary>
    public class CameraHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"Camera_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            var camera = gameObject.AddComponent<Camera>();
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<Camera>(out var camera))
            {
                data.customProperties["fieldOfView"] = camera.fieldOfView;
                data.customProperties["nearClipPlane"] = camera.nearClipPlane;
                data.customProperties["farClipPlane"] = camera.farClipPlane;
                data.customProperties["clearFlags"] = camera.clearFlags.ToString();
            }
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<Camera>(out var camera) && data.customProperties != null)
            {
                if (data.customProperties.TryGetValue("fieldOfView", out var fovValue))
                    if (float.TryParse(fovValue.ToString(), out var fov))
                        camera.fieldOfView = fov;

                if (data.customProperties.TryGetValue("nearClipPlane", out var nearValue))
                    if (float.TryParse(nearValue.ToString(), out var near))
                        camera.nearClipPlane = near;

                if (data.customProperties.TryGetValue("farClipPlane", out var farValue))
                    if (float.TryParse(farValue.ToString(), out var far))
                        camera.farClipPlane = far;
            }
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "Camera",
                description = "Unity Camera component",
                category = "Rendering",
                requiresAssetReference = false,
                supportsCustomProperties = true
            };
        }
    }

    /// <summary>
    /// Handler for AudioSource objects
    /// </summary>
    public class AudioSourceHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"AudioSource_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            var audioSource = gameObject.AddComponent<AudioSource>();
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<AudioSource>(out var audioSource))
            {
                data.customProperties["volume"] = audioSource.volume;
                data.customProperties["pitch"] = audioSource.pitch;
                data.customProperties["loop"] = audioSource.loop;
                data.customProperties["playOnAwake"] = audioSource.playOnAwake;
                
                if (audioSource.clip != null)
                    data.customProperties["clipName"] = audioSource.clip.name;
            }
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<AudioSource>(out var audioSource) && data.customProperties != null)
            {
                if (data.customProperties.TryGetValue("volume", out var volumeValue))
                    if (float.TryParse(volumeValue.ToString(), out var volume))
                        audioSource.volume = volume;

                if (data.customProperties.TryGetValue("pitch", out var pitchValue))
                    if (float.TryParse(pitchValue.ToString(), out var pitch))
                        audioSource.pitch = pitch;

                if (data.customProperties.TryGetValue("loop", out var loopValue))
                    if (bool.TryParse(loopValue.ToString(), out var loop))
                        audioSource.loop = loop;

                if (data.customProperties.TryGetValue("playOnAwake", out var playValue))
                    if (bool.TryParse(playValue.ToString(), out var playOnAwake))
                        audioSource.playOnAwake = playOnAwake;
            }
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "Audio Source",
                description = "Unity AudioSource component",
                category = "Audio",
                requiresAssetReference = false,
                supportsCustomProperties = true
            };
        }
    }

    /// <summary>
    /// Handler for ParticleSystem objects
    /// </summary>
    public class ParticleSystemHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"ParticleSystem_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            var particleSystem = gameObject.AddComponent<ParticleSystem>();
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<ParticleSystem>(out var particleSystem))
            {
                var main = particleSystem.main;
                data.customProperties["startLifetime"] = main.startLifetime.constant;
                data.customProperties["startSpeed"] = main.startSpeed.constant;
                data.customProperties["maxParticles"] = main.maxParticles;
                data.customProperties["loop"] = main.loop;
                data.customProperties["playOnAwake"] = main.playOnAwake;
            }
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<ParticleSystem>(out var particleSystem) && data.customProperties != null)
            {
                var main = particleSystem.main;
                
                if (data.customProperties.TryGetValue("maxParticles", out var maxValue))
                    if (int.TryParse(maxValue.ToString(), out var maxParticles))
                        main.maxParticles = maxParticles;

                if (data.customProperties.TryGetValue("loop", out var loopValue))
                    if (bool.TryParse(loopValue.ToString(), out var loop))
                        main.loop = loop;

                if (data.customProperties.TryGetValue("playOnAwake", out var playValue))
                    if (bool.TryParse(playValue.ToString(), out var playOnAwake))
                        main.playOnAwake = playOnAwake;
            }
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "Particle System",
                description = "Unity ParticleSystem component",
                category = "Effects",
                requiresAssetReference = false,
                supportsCustomProperties = true
            };
        }
    }

    /// <summary>
    /// Handler for MeshRenderer objects
    /// </summary>
    public class MeshRendererHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"MeshRenderer_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            
            // Set default cube mesh
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            meshRenderer.material = new Material(Shader.Find("Standard"));
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                if (meshRenderer.material != null)
                    data.customProperties["materialName"] = meshRenderer.material.name;
            }
            
            if (gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                if (meshFilter.mesh != null)
                    data.customProperties["meshName"] = meshFilter.mesh.name;
            }
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            // Material and mesh updates would require asset loading
            // This would be implementation-specific based on asset management system
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "Mesh Renderer",
                description = "GameObject with MeshRenderer component",
                category = "Rendering",
                requiresAssetReference = true,
                supportsCustomProperties = true
            };
        }
    }

    /// <summary>
    /// Handler for SkinnedMeshRenderer objects
    /// </summary>
    public class SkinnedMeshRendererHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"SkinnedMeshRenderer_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            var skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            if (gameObject.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
            {
                if (skinnedMeshRenderer.material != null)
                    data.customProperties["materialName"] = skinnedMeshRenderer.material.name;
                
                if (skinnedMeshRenderer.sharedMesh != null)
                    data.customProperties["meshName"] = skinnedMeshRenderer.sharedMesh.name;
            }
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            // Material and mesh updates would require asset loading
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "Skinned Mesh Renderer",
                description = "GameObject with SkinnedMeshRenderer component",
                category = "Characters",
                requiresAssetReference = true,
                supportsCustomProperties = true
            };
        }
    }

    /// <summary>
    /// Handler for UMA Character objects
    /// </summary>
    public class UMACharacterHandler : IObjectTypeHandler
    {
        public GameObject CreateRuntimeObject(PlacedObjectData data, Transform parent)
        {
            var gameObject = new GameObject($"UMACharacter_{data.instanceId}");
            
            if (parent != null)
                gameObject.transform.SetParent(parent);
                
            // Note: UMA creation would require UMA-specific code
            // This is a placeholder implementation
            
            var sandboxObject = gameObject.AddComponent<SandboxObject>();
            sandboxObject.Initialize(data.objectType, data.instanceId);
            
            return gameObject;
        }

        public void ExtractAdditionalData(GameObject gameObject, PlacedObjectData data)
        {
            // UMA-specific data extraction would go here
            // e.g., recipe data, wardrobe slots, DNA, etc.
        }

        public void UpdateRuntimeObject(GameObject gameObject, PlacedObjectData data)
        {
            // UMA-specific updates would go here
        }

        public ObjectValidationResult ValidateData(PlacedObjectData data)
        {
            return new ObjectValidationResult { isValid = true };
        }

        public ObjectPreviewInfo GetPreviewInfo()
        {
            return new ObjectPreviewInfo
            {
                displayName = "UMA Character",
                description = "Unity Multipurpose Avatar character",
                category = "Characters",
                requiresAssetReference = true,
                supportsCustomProperties = true
            };
        }
    }

    #endregion
}