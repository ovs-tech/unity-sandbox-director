using System;
using System.Collections.Generic;
using Systems.Persistence;
using UnityEngine;

namespace Systems.Inventory {
    public class Inventory : MonoBehaviour, IBind<InventoryData> {
        [SerializeField] InventoryView view;
        [SerializeField] int capacity = 20;
        [SerializeField] List<ItemDetails> startingItems = new List<ItemDetails>();
        [field: SerializeField] public SerializableGuid Id { get; set; } = SerializableGuid.NewGuid();

        InventoryController controller;

        void Awake() {
            controller = new InventoryController.Builder(view)
                .WithStartingItems(startingItems)
                .WithCapacity(capacity)
                .Build();

            // Register inventory persistence adapter with central SaveLoadSystem if available
            try {
                var save = Systems.Persistence.GamePersistenceManager.Instance;
                if (save != null) {
                    var adapter = new InventoryPersistence(this);
                    save.RegisterSubsystem(adapter);
                }
            } catch (Exception) {
                // ignore when SaveLoadSystem is not present
            }
        }
         
        public void Bind(InventoryData data) {
            controller.Bind(data);
            data.Id = Id;
        }
    }
}