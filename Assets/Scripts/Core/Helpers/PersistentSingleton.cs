using UnityEngine;

public class PersistentSingleton<T> : MonoBehaviour where T : Component
{
  [Tooltip("if this is true, this singleton will auto detach if it finds itself parented on awake")]
  public bool UnparentOnAwake = true;

  public static bool HasInstance => instance != null;
  public static T Current => instance;

  protected static T instance;
  private static bool _isQuitting;

  public static T Instance
  {
    get
    {
      if (_isQuitting)
      {
        return null;
      }

      if (instance == null)
      {
        try
        {
          instance = FindAnyObjectByType<T>();
          if (instance == null)
          {
            GameObject obj = new GameObject();
            obj.name = typeof(T).Name + "AutoCreated";
            instance = obj.AddComponent<T>();
          }
        }
        catch (UnityException)
        {
          // FindFirstObjectByType is not allowed during serialization
          // Return null and let the instance be resolved after deserialization
          return null;
        }
      }

      return instance;
    }
  }

  protected virtual void Awake()
  {
    _isQuitting = false;
    InitializeSingleton();
  }

  protected virtual void OnApplicationQuit()
  {
    _isQuitting = true;
  }

  protected virtual void InitializeSingleton()
  {
    if (!Application.isPlaying)
    {
      return;
    }

    if (UnparentOnAwake)
    {
      transform.SetParent(null);
    }

    if (instance == null)
    {
      instance = this as T;
      DontDestroyOnLoad(transform.gameObject);
      enabled = true;
    }
    else
    {
      if (this != instance)
      {
        Destroy(this.gameObject);
      }
    }
  }
}