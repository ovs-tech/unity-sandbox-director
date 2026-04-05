using Unity.AppUI.MVVM;

namespace Systems.UI
{
    [ObservableObject]
    public partial class ProjectItemViewModel
    {
        [ObservableProperty]
        private Project m_Project;

        public ProjectItemViewModel(Project project)
        {
            this.m_Project = project;
        }
    }
}