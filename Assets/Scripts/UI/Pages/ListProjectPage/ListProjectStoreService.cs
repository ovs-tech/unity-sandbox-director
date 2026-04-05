
using Unity.AppUI.Redux;
using UnityEngine;

namespace Systems.UI
{
    class ListProjectStoreService : IListProjectStoreService
    {
        public string sliceName => "app";

        public IStore<PartitionedState> store { get; }

        public void SaveState()
        {
            Debug.Log("Saving state...");
        }

        public ListProjectStoreService()
        {
            var initialState = new ListProjectState();
            store = StoreFactory.CreateStore(new []
                {
                    StoreFactory.CreateSlice<ListProjectState>(sliceName, initialState, builder =>
                    {
                        builder
                            .AddCase(ListProjectActions.createProject, ListProjectReducers.CreateProjectReducer)
                            .AddCase(ListProjectActions.editProject, ListProjectReducers.EditProjectReducer)
                            .AddCase(ListProjectActions.deleteProject, ListProjectReducers.DeleteProjectReducer)
                            .AddCase(ListProjectActions.setSearchInput, ListProjectReducers.SetSearchInputReducer)
                            .AddCase(ListProjectActions.setFilter, ListProjectReducers.SetFilterReducer);
                    })
                });
        }
    }
}