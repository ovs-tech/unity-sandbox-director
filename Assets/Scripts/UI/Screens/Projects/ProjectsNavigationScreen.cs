using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using AppUIButton = Unity.AppUI.UI.Button;

namespace Systems.UI
{
    [Preserve]
    class ProjectsNavigationScreen : NavigationScreen
    {
        private readonly NavHost host;
        private readonly AppUIButton newProductionButton;

        public ProjectsNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
        {
            this.host = host;
            uxmlAsset.CloneTree(this);
            this.newProductionButton = this.Q<AppUIButton>("new-production-button");
            var listProjectPageContainer = this.Q<VisualElement>("list-project-page-container");
            var listProjectPage = MyApp.current.services.GetRequiredService<ListProjectPage>();
            listProjectPageContainer.Add(listProjectPage);
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            if (newProductionButton != null)
            {
                newProductionButton.clicked += () =>
                {
                    host.navController.Navigate("create_project");
                };
            }
        }
    }
}
