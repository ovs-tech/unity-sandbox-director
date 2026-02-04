using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Context for binding track keys to Unity objects with tagging support
    /// Maps string keys to BindableObjects that wrap Unity objects and provide discovery functionality
    /// </summary>
    public class BindableObjectManager : MonoBehaviour
    {
        private readonly Dictionary<string, BindableObject> bindings = new Dictionary<string, BindableObject>();
        
        /// <summary>
        /// Bind a key to a Unity object
        /// </summary>
        /// <param name="key">Binding key (e.g., "charA", "mainCam")</param>
        /// <param name="obj">Target Unity object</param>
        public void Bind(string key, UnityEngine.Object obj)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[BindingContext] Cannot bind empty key");
                return;
            }
            
            if (obj == null)
            {
                Debug.LogWarning($"[BindingContext] Binding null object to key '{key}'");
                bindings[key] = null;
                return;
            }
            
            // If it's already a BindableObject, use it directly
            if (obj is BindableObject bindableObj)
            {
                bindings[key] = bindableObj;
            }
            // If it's a GameObject, try to get or add a BindableObject component
            else if (obj is GameObject go)
            {
                var bindable = go.GetComponent<BindableObject>();
                if (bindable == null)
                {
                    bindable = go.AddComponent<BindableObject>();
                }
                bindings[key] = bindable;
            }
            // For other components, get or add BindableObject to their GameObject
            else if (obj is Component comp)
            {
                var bindable = comp.gameObject.GetComponent<BindableObject>();
                if (bindable == null)
                {
                    bindable = comp.gameObject.AddComponent<BindableObject>();
                }
                bindings[key] = bindable;
            }
            else
            {
                Debug.LogWarning($"[BindingContext] Cannot bind object of type {obj.GetType().Name} - only GameObjects and Components are supported");
            }
        }
        
        /// <summary>
        /// Resolve a binding key to a typed object
        /// </summary>
        /// <typeparam name="T">Expected object type</typeparam>
        /// <param name="key">Binding key</param>
        /// <returns>Resolved object or null if not found/wrong type</returns>
        public T Resolve<T>(string key) where T : UnityEngine.Object
        {
            if (bindings.TryGetValue(key, out var bindableObj) && bindableObj != null)
            {
                return bindableObj.GetAs<T>();
            }
            
            return null;
        }
        
        /// <summary>
        /// Try to resolve a binding key to a BindableObject
        /// </summary>
        /// <param name="key">Binding key</param>
        /// <param name="bindableObj">Output resolved BindableObject</param>
        /// <returns>True if key was found</returns>
        public bool TryResolve(string key, out BindableObject bindableObj)
        {
            return bindings.TryGetValue(key, out bindableObj);
        }
        
        /// <summary>
        /// Try to resolve a binding key to a Unity object (backward compatibility)
        /// </summary>
        /// <param name="key">Binding key</param>
        /// <param name="obj">Output resolved object</param>
        /// <returns>True if key was found</returns>
        public bool TryResolve(string key, out UnityEngine.Object obj)
        {
            if (bindings.TryGetValue(key, out var bindableObj) && bindableObj != null)
            {
                obj = bindableObj.gameObject;
                return true;
            }
            
            obj = null;
            return false;
        }
        
        /// <summary>
        /// Remove a binding
        /// </summary>
        /// <param name="key">Key to remove</param>
        public void Unbind(string key)
        {
            bindings.Remove(key);
        }
        
        /// <summary>
        /// Clear all bindings
        /// </summary>
        public void Clear()
        {
            bindings.Clear();
        }
        
        /// <summary>
        /// Get all binding keys
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            return bindings.Keys;
        }
        
        /// <summary>
        /// Check if a key exists
        /// </summary>
        public bool HasKey(string key)
        {
            return bindings.ContainsKey(key);
        }
        
        /// <summary>
        /// Bind a key to a Unity object with tags
        /// </summary>
        /// <param name="key">Binding key</param>
        /// <param name="obj">Target Unity object</param>
        /// <param name="tags">Tags to associate with the object</param>
        public void BindWithTags(string key, UnityEngine.Object obj, params string[] tags)
        {
            Bind(key, obj);
            if (bindings.TryGetValue(key, out var bindableObj) && bindableObj != null)
            {
                bindableObj.AddTags(tags);
            }
        }
        
        /// <summary>
        /// Add tags to an existing binding
        /// </summary>
        /// <param name="key">Binding key</param>
        /// <param name="tags">Tags to add</param>
        public void AddTags(string key, params string[] tags)
        {
            if (bindings.TryGetValue(key, out var bindableObj) && bindableObj != null)
            {
                bindableObj.AddTags(tags);
            }
        }
        
        /// <summary>
        /// Remove tags from an existing binding
        /// </summary>
        /// <param name="key">Binding key</param>
        /// <param name="tags">Tags to remove</param>
        public void RemoveTags(string key, params string[] tags)
        {
            if (bindings.TryGetValue(key, out var bindableObj) && bindableObj != null)
            {
                foreach (var tag in tags)
                {
                    bindableObj.RemoveTag(tag);
                }
            }
        }
        
        /// <summary>
        /// Find all bindings that have any of the specified tags
        /// </summary>
        /// <param name="tags">Tags to search for</param>
        /// <returns>Dictionary of key-value pairs where objects have any of the specified tags</returns>
        public Dictionary<string, BindableObject> FindByTags(params string[] tags)
        {
            var results = new Dictionary<string, BindableObject>();
            
            foreach (var kvp in bindings)
            {
                if (kvp.Value != null && kvp.Value.HasAnyTag(tags))
                {
                    results[kvp.Key] = kvp.Value;
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Find all bindings that have all of the specified tags
        /// </summary>
        /// <param name="tags">Tags that must all be present</param>
        /// <returns>Dictionary of key-value pairs where objects have all specified tags</returns>
        public Dictionary<string, BindableObject> FindByAllTags(params string[] tags)
        {
            var results = new Dictionary<string, BindableObject>();
            
            foreach (var kvp in bindings)
            {
                if (kvp.Value != null && kvp.Value.HasAllTags(tags))
                {
                    results[kvp.Key] = kvp.Value;
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Find all bindings of a specific type
        /// </summary>
        /// <typeparam name="T">Type to search for</typeparam>
        /// <returns>Dictionary of key-value pairs where objects are of the specified type</returns>
        public Dictionary<string, T> FindByType<T>() where T : UnityEngine.Object
        {
            var results = new Dictionary<string, T>();
            
            foreach (var kvp in bindings)
            {
                if (kvp.Value != null)
                {
                    var obj = kvp.Value.GetAs<T>();
                    if (obj != null)
                    {
                        results[kvp.Key] = obj;
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Find all bindings of a specific type that have any of the specified tags
        /// </summary>
        /// <typeparam name="T">Type to search for</typeparam>
        /// <param name="tags">Tags to search for</param>
        /// <returns>Dictionary of key-value pairs matching both type and tag criteria</returns>
        public Dictionary<string, T> FindByTypeAndTags<T>(params string[] tags) where T : UnityEngine.Object
        {
            var results = new Dictionary<string, T>();
            
            foreach (var kvp in bindings)
            {
                if (kvp.Value != null && kvp.Value.HasAnyTag(tags))
                {
                    var obj = kvp.Value.GetAs<T>();
                    if (obj != null)
                    {
                        results[kvp.Key] = obj;
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Discover all BindableObject components in the scene (not just bound ones)
        /// </summary>
        /// <returns>All BindableObject components found in the scene</returns>
        public static BindableObject[] DiscoverAll()
        {
            return Object.FindObjectsByType<BindableObject>(FindObjectsSortMode.None);
        }
        
        /// <summary>
        /// Discover BindableObject components in the scene that have any of the specified tags
        /// </summary>
        /// <param name="tags">Tags to search for</param>
        /// <returns>BindableObject components that have any of the specified tags</returns>
        public static BindableObject[] DiscoverByTags(params string[] tags)
        {
            var allBindables = Object.FindObjectsByType<BindableObject>(FindObjectsSortMode.None);
            return allBindables.Where(b => b.HasAnyTag(tags)).ToArray();
        }
        
        /// <summary>
        /// Discover GameObjects in the scene that have BindableObject components with specific tags
        /// </summary>
        /// <typeparam name="T">Component type to find</typeparam>
        /// <param name="tags">Tags to search for</param>
        /// <returns>Components of type T on GameObjects that have BindableObject with specified tags</returns>
        public static T[] DiscoverByTypeAndTags<T>(params string[] tags) where T : Component
        {
            var bindables = DiscoverByTags(tags);
            var results = new List<T>();
            
            foreach (var bindable in bindables)
            {
                var component = bindable.GetComponent<T>();
                if (component != null)
                {
                    results.Add(component);
                }
            }
            
            return results.ToArray();
        }
        
        /// <summary>
        /// Auto-discover and bind all BindableObjects in the scene using their GameObject names as keys
        /// </summary>
        /// <param name="prefix">Optional prefix to add to binding keys</param>
        public void AutoBind(string prefix = "")
        {
            var bindables = DiscoverAll();

            Debug.Log($"[BindingContext] Auto-binding {bindables.Length} objects with prefix '{prefix}'");
            
            foreach (var bindable in bindables)
            {
                Debug.Log($"[BindingContext] Found bindable object: {bindable.name} with tags [{bindable.tag}]");
                var key = string.IsNullOrEmpty(prefix) ? bindable.name : $"{prefix}{bindable.name}";
                bindings[key] = bindable;
            }
        }
        
        /// <summary>
        /// Auto-discover and bind BindableObjects with specific tags
        /// </summary>
        /// <param name="tags">Tags to search for</param>
        /// <param name="prefix">Optional prefix to add to binding keys</param>
        public void AutoBindByTags(string[] tags, string prefix = "")
        {
            var bindables = DiscoverByTags(tags);
            
            foreach (var bindable in bindables)
            {
                var key = string.IsNullOrEmpty(prefix) ? bindable.name : $"{prefix}{bindable.name}";
                bindings[key] = bindable;
            }
        }
    }
}