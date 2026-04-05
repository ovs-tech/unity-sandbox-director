using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using System.ComponentModel;
using System;
using AppUIButton = Unity.AppUI.UI.Button;

namespace Systems.UI
{
    [Preserve]
    public class ListProjectPage : VisualElement
    {
        readonly ListProjectViewModel m_ViewModel;

        ListView m_ProjectListView;
        SearchBar m_SearchTextField;
        AppUIButton m_CreateProjectButton;

        const string k_ResourceName = "ListProjectPage";
        const string k_ResourcePathAlt = "UI/Pages/ListProjectPage/ListProjectPage";
        const string k_AssetPath = "Assets/Scripts/UI/Pages/ListProjectPage/ListProjectPage.uxml";

        public ListProjectPage(ListProjectViewModel viewModel)
        {
            m_ViewModel = viewModel;
            var tree = Resources.Load<VisualTreeAsset>(k_ResourceName) ?? Resources.Load<VisualTreeAsset>(k_ResourcePathAlt);
#if UNITY_EDITOR
            if (tree == null)
                tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_AssetPath);
#endif

            if (tree != null)
                tree.CloneTree(this);

            InitializeComponent();
            m_ViewModel.PropertyChanged += OnPropertyChanged;
            RefreshProjectList();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.Log($"Property changed: {e.PropertyName}");
            if(e.PropertyName == nameof(ListProjectViewModel.Projects))
            {
                RefreshProjectList();
            }
            else if(e.PropertyName == nameof(ListProjectViewModel.FilteredProjects))
            {
                RefreshProjectList();
            }
        }

        private void RefreshProjectList()
        {
            if (string.IsNullOrEmpty(m_SearchTextField.value))
            {
                m_ProjectListView.itemsSource = m_ViewModel.Projects;
            }
            else
            {
                m_ProjectListView.itemsSource = m_ViewModel.FilteredProjects;
            }
        }

        public void InitializeComponent()
        {
            m_SearchTextField = this.Q<SearchBar>("search-field");
            m_CreateProjectButton = this.Q<AppUIButton>("create-project-button");
            m_CreateProjectButton.clicked += OnCreateProjectButtonClicked;

            m_ProjectListView = this.Q<ListView>("list-project");
            m_ProjectListView.bindItem = BindItem;
            m_ProjectListView.makeItem = MakeItem;
            m_ProjectListView.unbindItem = UnbindItem;
        }

        private void OnCreateProjectButtonClicked()
        {
            var index = m_ProjectListView.itemsSource != null ? m_ProjectListView.itemsSource.Count : 0;
            m_ViewModel.CreateNewProjectCommand.Execute("New Project " + (index + 1));
        }

        private void UnbindItem(VisualElement element, int index)
        {
            var item = (ProjectItemView) element;
            var viewModel = (ProjectItemViewModel)item.viewModel;
            viewModel.PropertyChanged -= OnItemPropertyChanged;
            item.viewModel = null;
        }

        private VisualElement MakeItem()
        {
            return new ProjectItemView();
        }

        private void BindItem(VisualElement element, int index)
        {
            var project = (Project)m_ProjectListView.itemsSource[index];
            var item = (ProjectItemView) element;
            var viewModel = new ProjectItemViewModel(project);
            item.viewModel = viewModel;
            viewModel.PropertyChanged += OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           
        }
    }
}
