using UnityEngine.UIElements;

namespace Systems.UI
{
    public class ProjectItemView : VisualElement
    {
        ProjectItemViewModel m_ViewModel;

        ProjectCard m_ProjectCard;

        public ProjectItemViewModel viewModel
        {
            get => m_ViewModel;
            set
            {
                m_ViewModel = value;
                BindDataContext();
            }
        }

        public ProjectItemView()
        {
            m_ProjectCard = new ProjectCard();
            Add(m_ProjectCard);
        }

        private void BindDataContext()
        {
            if (m_ProjectCard == null)
                return;

            m_ProjectCard.dataSource = m_ViewModel?.Project;
            m_ProjectCard.SetBadge(string.Empty, false);
            m_ProjectCard.SetMenuVisible(true);
        }
    }
}