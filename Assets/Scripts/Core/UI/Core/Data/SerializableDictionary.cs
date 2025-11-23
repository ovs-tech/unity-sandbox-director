using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TKey, TValue>>
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    // Native Dictionary - được wrap để có thể serialize
    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    /// <summary>
    /// Access to native Dictionary for full functionality
    /// All Dictionary methods available: ContainsKey, TryGetValue, etc.
    /// </summary>
    public Dictionary<TKey, TValue> Dictionary => dictionary;

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        dictionary.Clear();

        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
        {
            dictionary[keys[i]] = values[i];
        }
    }

    // Expose native Dictionary methods directly - no need to reimplement
    public void Add(TKey key, TValue value) => dictionary[key] = value;
    public bool Remove(TKey key) => dictionary.Remove(key);
    public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
    public void Clear() => dictionary.Clear();

    // Direct access to Dictionary properties
    public TValue this[TKey key]
    {
        get => dictionary[key];
        set => dictionary[key] = value;
    }

    public ICollection<TKey> Keys => dictionary.Keys;
    public ICollection<TValue> Values => dictionary.Values;
    public int Count => dictionary.Count;

    // Implement IEnumerable to work like native Dictionary
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => dictionary.GetEnumerator();
}

/// <summary>
/// Specialized dictionary for binding Unity Objects to string keys
/// Used for Timeline binding context
/// </summary>
[Serializable]
public class StringObjectDictionary : SerializableDictionary<string, UnityEngine.Object>
{
    /// <summary>
    /// Get bound object as specific type
    /// </summary>
    public T Get<T>(string key) where T : UnityEngine.Object
    {
        if (TryGetValue(key, out var obj))
        {
            return obj as T;
        }
        return null;
    }

    /// <summary>
    /// Bind an object with automatic key generation if key already exists
    /// </summary>
    public string Bind(string preferredKey, UnityEngine.Object obj)
    {
        string actualKey = preferredKey;
        int counter = 1;

        while (ContainsKey(actualKey))
        {
            actualKey = $"{preferredKey}_{counter}";
            counter++;
        }

        Add(actualKey, obj);
        return actualKey;
    }
}