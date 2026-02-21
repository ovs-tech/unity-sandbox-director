using System;
using System.Linq;
using Systems.Persistence;
using UnityEngine;

namespace Systems.Inventory {
    public class InventoryPersistence : ISubsystemPersistence {
        readonly Inventory _inventory;

        public InventoryPersistence(Inventory inventory) {
            _inventory = inventory;
        }

        public string Namespace => "inventory";

        public Type DataType => typeof(InventoryData);

        public object GetSaveData() {
            try {
                var sls = Systems.Persistence.SaveLoadSystem.Instance;
                if (sls != null && sls.gameData != null) return sls.gameData.inventoryData;
            } catch (Exception) { }
            return null;
        }

        public void LoadData(object data) {
            if (data == null) return;
            var invData = data as InventoryData;
            if (invData == null) return;

            // Store into central gameData if available
            try {
                var sls = Systems.Persistence.SaveLoadSystem.Instance;
                if (sls != null) {
                    sls.gameData.inventoryData = invData;
                }
            } catch (Exception) { }

            // Bind to all inventory instances in scene
            try {
                var inventories = UnityEngine.Object.FindObjectsOfType<Inventory>();
                foreach (var inv in inventories) {
                    inv.Bind(invData);
                }
            } catch (Exception) { }
        }
    }
}
