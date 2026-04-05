using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using AppUIButton =Unity.AppUI.UI.Button;

namespace Systems.UI
{
    [Preserve]
    class WelcomeNavigationScreen : NavigationScreen
    {
        private readonly NavHost host;
        private readonly AppUIButton loginButton;
        private readonly AppUIButton startButton;

        public WelcomeNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
        {
            this.host = host;
            uxmlAsset.CloneTree(this);

            this.loginButton = this.Q<AppUIButton>("login-link-button");
            this.startButton = this.Q<AppUIButton>("start-directing-button");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            loginButton.clicked += () =>
            {
                host.navController.Navigate(Actions.go_to_login);
            };
            startButton.clicked += () =>
            {
                host.navController.Navigate(Actions.go_to_projects);
            };
        }
    }
}