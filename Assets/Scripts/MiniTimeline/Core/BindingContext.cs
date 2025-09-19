using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Context for binding track keys to Unity objects
    /// Maps string keys to actual GameObjects/Components in the scene
    /// </summary>
    public class BindingContext
    {
        private readonly Dictionary<string, UnityEngine.Object> bindings = new Dictionary<string, UnityEngine.Object>();
        
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
            }
            
            bindings[key] = obj;
        }
        
        /// <summary>
        /// Resolve a binding key to a typed object
        /// </summary>
        /// <typeparam name="T">Expected object type</typeparam>
        /// <param name="key">Binding key</param>
        /// <returns>Resolved object or null if not found/wrong type</returns>
        public T Resolve<T>(string key) where T : UnityEngine.Object
        {
            if (TryResolve(key, out var obj))
            {
                if (obj is T typedObj)
                {
                    return typedObj;
                }
                
                // Try to get component if resolving to a component type
                if (obj is GameObject go && typeof(Component).IsAssignableFrom(typeof(T)))
                {
                    return go.GetComponent<T>();
                }
                
                Debug.LogWarning($"[BindingContext] Object bound to '{key}' is not of type {typeof(T).Name}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Try to resolve a binding key
        /// </summary>
        /// <param name="key">Binding key</param>
        /// <param name="obj">Output resolved object</param>
        /// <returns>True if key was found</returns>
        public bool TryResolve(string key, out UnityEngine.Object obj)
        {
            return bindings.TryGetValue(key, out obj);
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
    }
}