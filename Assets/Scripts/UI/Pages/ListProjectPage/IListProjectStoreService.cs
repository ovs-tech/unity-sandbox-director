using Unity.AppUI.Redux;

namespace Systems.UI
{
        public interface IListProjectStoreService
        {
                IStore<PartitionedState> store { get; }

                public string sliceName { get; }

                void SaveState();
        }
}