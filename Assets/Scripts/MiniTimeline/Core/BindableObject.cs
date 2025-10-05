using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Types of bindable objects for categorization
    /// </summary>
    public enum BindableObjectType
    {
        Generic = 0,
        Character = 1,
        Player = 2,
        Camera = 3,
        Environment = 4,
        UI = 5,
        Audio = 6,
        Light = 7,
        Animated = 8,
        Effect = 9,
        Interactive = 10,
        Custom = 11
    }

    /// <summary>
    /// MonoBehaviour component that provides typing and discovery functionality for Unity objects
    /// Can be attached to GameObjects to make them discoverable through the binding system
    /// </summary>
    public class BindableObject : MonoBehaviour
    {
        [Header("Binding Configuration")]
        [SerializeField] private BindableObjectType objectType = BindableObjectType.Generic;
        [SerializeField] private string customTypeName = "";
        [SerializeField] private List<string> tags = new List<string>();
        
        /// <summary>
        /// The type of this bindable object
        /// </summary>
        public BindableObjectType ObjectType => objectType;
        
        /// <summary>
        /// Custom type name (used when ObjectType is Custom)
        /// </summary>
        public string CustomTypeName => customTypeName;
        
        /// <summary>
        /// Get the effective type name for binding purposes
        /// </summary>
        public string TypeName 
        {
            get
            {
                if (objectType == BindableObjectType.Custom && !string.IsNullOrEmpty(customTypeName))
                {
                    return customTypeName.ToLower();
                }
                return objectType.ToString().ToLower();
            }
        }
        
        /// <summary>
        /// Read-only list of tags associated with this object
        /// </summary>
        public IReadOnlyList<string> Tags => tags.AsReadOnly();
        
        /// <summary>
        /// The GameObject this component is attached to
        /// </summary>
        public GameObject Target => gameObject;
        
        /// <summary>
        /// Add a tag to this object
        /// </summary>
        /// <param name="tag">Tag to add</param>
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogWarning("[BindableObject] Cannot add empty or null tag");
                return;
            }
            
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }
        
        /// <summary>
        /// Add multiple tags to this object
        /// </summary>
        /// <param name="tagsToAdd">Tags to add</param>
        public void AddTags(params string[] tagsToAdd)
        {
            if (tagsToAdd == null) return;
            
            foreach (var tag in tagsToAdd)
            {
                AddTag(tag);
            }
        }
        
        /// <summary>
        /// Remove a tag from this object
        /// </summary>
        /// <param name="tag">Tag to remove</param>
        /// <returns>True if tag was removed</returns>
        public bool RemoveTag(string tag)
        {
            return tags.Remove(tag);
        }
        
        /// <summary>
        /// Check if this object has a specific tag
        /// </summary>
        /// <param name="tag">Tag to check for</param>
        /// <returns>True if object has the tag</returns>
        public bool HasTag(string tag)
        {
            return tags.Contains(tag);
        }
        
        /// <summary>
        /// Check if this object has any of the specified tags
        /// </summary>
        /// <param name="tagsToCheck">Tags to check for</param>
        /// <returns>True if object has any of the tags</returns>
        public bool HasAnyTag(params string[] tagsToCheck)
        {
            if (tagsToCheck == null) return false;
            return tagsToCheck.Any(HasTag);
        }
        
        /// <summary>
        /// Check if this object has all of the specified tags
        /// </summary>
        /// <param name="tagsToCheck">Tags to check for</param>
        /// <returns>True if object has all of the tags</returns>
        public bool HasAllTags(params string[] tagsToCheck)
        {
            if (tagsToCheck == null) return true;
            return tagsToCheck.All(HasTag);
        }
        
        /// <summary>
        /// Clear all tags from this object
        /// </summary>
        public void ClearTags()
        {
            tags.Clear();
        }
        
        /// <summary>
        /// Get the GameObject or a component as a specific type
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <returns>Cast object or null if cast fails</returns>
        public T GetAs<T>() where T : UnityEngine.Object
        {
            if (gameObject is T typedObj)
            {
                return typedObj;
            }
            
            // Try to get component if resolving to a component type
            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                return gameObject.GetComponent<T>();
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if the target object is of a specific type
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if target is of type T</returns>
        public bool IsOfType<T>() where T : UnityEngine.Object
        {
            return GetAs<T>() != null;
        }
        
        /// <summary>
        /// Check if the GameObject is valid (not null or destroyed)
        /// </summary>
        public bool IsValid => gameObject != null;
        
        /// <summary>
        /// Get a string representation of this object
        /// </summary>
        public override string ToString()
        {
            var targetName = gameObject != null ? gameObject.name : "null";
            var tagsList = tags.Count > 0 ? $" [{string.Join(", ", tags)}]" : "";
            return $"BindableObject({targetName}){tagsList}";
        }
        
        // Implicit conversion operator for convenience
        public static implicit operator bool(BindableObject bindableObject)
        {
            return bindableObject != null && bindableObject.IsValid;
        }
    }
}