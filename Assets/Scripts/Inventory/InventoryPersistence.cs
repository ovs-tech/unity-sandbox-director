using System;
using Systems.Persistence.Core;

namespace Systems.Inventory
{
    public class InventoryPersistence : ISubsystemPersistence
    {
        readonly Inventory _inventory;

        public InventoryPersistence(Inventory inventory)
        {
            _inventory = inventory;
        }

        public string Namespace => "inventory";

        public string PersistentName => "inventory";

        public PersistenceTarget Target => PersistenceTarget.Embedded;

        public Type DataType => typeof(InventoryData);

        public object GetSaveData()
        {
            // Simply return the data from one of the inventories (they should be synced)
            // The original logic was to find any inventory and get its data.
            // If _inventory is the canonical one, use that.
            return _inventory.GetPersistenceData();
        }

        public void LoadData(object data)
        {
            if (data == null) return;
            var invData = data as InventoryData;
            if (invData == null) return;

            // Bind to all inventory instances in scene
            var inventories = UnityEngine.Object.FindObjectsByType<Inventory>();
            foreach (var inv in inventories)
            {
                inv.Bind(invData);
            }
        }
    }
}
