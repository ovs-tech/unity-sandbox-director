using UnityEngine;

namespace Systems.PlacementSystem.Persistence
{
    /// <summary>
    /// Lightweight component to store instance metadata for persistence.
    /// </summary>
    public class PlacedObjectInfo : MonoBehaviour
    {
        [SerializeField] string _id;
        [SerializeField] string _prefabName;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string PrefabName
        {
            get => _prefabName;
            set => _prefabName = value;
        }
    }
}
