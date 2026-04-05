using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace Systems.UI
{
    [ObservableObject]
    public partial class ListProjectViewModel
    {
        readonly IListProjectStoreService m_StoreService;
        IDisposableSubscription m_Subscription;

        [ObservableProperty]
        private Project[] m_Projects;

        [ObservableProperty]
        private Project[] m_FilteredProjects;

        public ListProjectViewModel(IListProjectStoreService storeService)
        {
            m_StoreService = storeService;

            m_Projects = m_StoreService.store.GetState<ListProjectState>(m_StoreService.sliceName).projects;
            m_Subscription = m_StoreService.store.Subscribe(SelectAppSlice, OnStateChanged);
        }

        async void OnStateChanged(ListProjectState state)
        {
            Debug.Log("Redux state has changed:\n" + state);
            if(state.projects != Projects)
            {
                Projects = state.projects;
                await SearchInternal(state.searchInput, CancellationToken.None);
            }
        }

        ListProjectState SelectAppSlice(PartitionedState state)
        {
            return state.Get<ListProjectState>(m_StoreService.sliceName);
        }

        [ICommand]
        async Task SearchProject(string input, CancellationToken cancellationToken)
        {
            m_StoreService.store.Dispatch(ListProjectActions.setSearchInput, input);
            await SearchInternal(input, cancellationToken);
        }

        async Task SearchInternal(string input, CancellationToken cancellationToken)
        {
            var result = new List<Project>();
            foreach (var project in Projects)
            {
                if (string.IsNullOrEmpty(input) || project.name.Contains(input))
                {
                    result.Add(project);
                }
            }
            // Simulate a network request
            await Task.Delay(300, cancellationToken);

            FilteredProjects = result.ToArray();
        }

        [ICommand]
        async Task CreateNewProject(string input, CancellationToken cancellationToken)
        {
            m_StoreService.store.Dispatch(ListProjectActions.createProject, input);
        }
    }
}