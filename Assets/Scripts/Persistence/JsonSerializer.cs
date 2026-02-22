using Systems.Persistence.Core;
using UnityEngine;

namespace Systems.Persistence {
    [CreateAssetMenu(menuName = "Persistence/Json Serializer", fileName = "JsonSerializer")]
    public class JsonSerializer : BaseSerializer {
        public override string Serialize<T>(T obj) {
            return JsonUtility.ToJson(obj, true);
        }

        public override T Deserialize<T>(string json) {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
