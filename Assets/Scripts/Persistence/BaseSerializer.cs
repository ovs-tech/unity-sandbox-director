using System;
using UnityEngine;

namespace Systems.Persistence {
    public abstract class BaseSerializer : ScriptableObject, ISerializer {
        public abstract string Serialize<T>(T obj);
        public abstract T Deserialize<T>(string json);
    }
}
