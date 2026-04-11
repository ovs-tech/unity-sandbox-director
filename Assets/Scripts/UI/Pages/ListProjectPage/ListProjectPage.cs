using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using System.ComponentModel;
using AppUIButton = Unity.AppUI.UI.Button;

namespace Systems.UI
{
    [UxmlFilePath("Assets/Scripts/UI/Pages/ListProjectPage/ListProjectPage.uxml", UxmlFilePathType.AssetDatabase)]
    public partial class ListProjectPage : VisualElement
    {
        readonly ListProjectViewModel m_ViewModel;

        ListView m_ProjectListView;
        SearchBar m_SearchTextField;
        AppUIButton m_CreateProjectButton;

        public ListProjectPage(ListProjectViewModel viewModel)
        {
            m_ViewModel = viewModel;
            UxmlCloneTree();
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
            m_ProjectListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
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
