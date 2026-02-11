using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Utility helpers to serialize/deserialize lists of polymorphic objects via SerializedWrapper.
    /// </summary>
    public static class SerializationUtils
    {
        public static List<SerializedWrapper> SerializeList<T>(IEnumerable<T> items)
        {
            var list = new List<SerializedWrapper>();
            if (items == null) return list;

            foreach (var item in items)
            {
                if (item == null) continue;
                list.Add(new SerializedWrapper
                {
                    type = item.GetType().AssemblyQualifiedName,
                    data = JsonUtility.ToJson(item)
                });
            }

            return list;
        }

        // Non-generic serializer for use with unknown/variant collections
        public static List<SerializedWrapper> SerializeEnumerable(System.Collections.IEnumerable items)
        {
            var list = new List<SerializedWrapper>();
            if (items == null) return list;

            foreach (var item in items)
            {
                if (item == null) continue;
                list.Add(new SerializedWrapper
                {
                    type = item.GetType().AssemblyQualifiedName,
                    data = JsonUtility.ToJson(item)
                });
            }

            return list;
        }

        public static List<T> DeserializeList<T>(List<SerializedWrapper> wrappers, string context = null)
        {
            var result = new List<T>();
            if (wrappers == null) return result;

            int added = 0;
            foreach (var wrapped in wrappers)
            {
                Type type = TypeResolver.ResolveType(wrapped.type);
                if (type == null)
                {
                    if (!string.IsNullOrEmpty(context)) Debug.LogWarning($"{context}: type not found: {wrapped.type}");
                    else Debug.LogWarning($"SerializationUtils.DeserializeList: type not found: {wrapped.type}");
                    continue;
                }

                try { wrapped.type = type.AssemblyQualifiedName; } catch { }

                var obj = JsonUtility.FromJson(wrapped.data, type);
                if (obj is T t)
                {
                    result.Add(t);
                    added++;
                }
                else
                {
                    Debug.LogWarning($"SerializationUtils.DeserializeList: deserialized object is not target type {typeof(T)} (actual={obj?.GetType()})");
                }
            }

            if (!string.IsNullOrEmpty(context)) Debug.Log($"{context}: deserialized={added}");
            return result;
        }

        // Deserialize into objects (non-generic) so callers can attempt casting to their generic param.
        public static List<object> DeserializeToObjects(List<SerializedWrapper> wrappers, string context = null)
        {
            var result = new List<object>();
            if (wrappers == null) return result;

            int added = 0;
            foreach (var wrapped in wrappers)
            {
                Type type = TypeResolver.ResolveType(wrapped.type);
                if (type == null)
                {
                    if (!string.IsNullOrEmpty(context)) Debug.LogWarning($"{context}: type not found: {wrapped.type}");
                    else Debug.LogWarning($"SerializationUtils.DeserializeToObjects: type not found: {wrapped.type}");
                    continue;
                }

                try { wrapped.type = type.AssemblyQualifiedName; } catch { }

                var obj = JsonUtility.FromJson(wrapped.data, type);
                result.Add(obj);
                added++;
            }

            if (!string.IsNullOrEmpty(context)) Debug.Log($"{context}: deserialized_objects={added}");
            return result;
        }
    }
}
