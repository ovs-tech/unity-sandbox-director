using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using AppUIButton = Unity.AppUI.UI.Button;

namespace Systems.UI
{
    [Preserve]
    class LoginNavigationScreen : NavigationScreen
    {
        private readonly NavHost host;
        private readonly AppUIButton createAccountButton;

        public LoginNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
        {
            this.host = host;
            uxmlAsset.CloneTree(this);
            this.createAccountButton = this.Q<AppUIButton>("create-account-button");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            createAccountButton.clicked += () =>
            {
                host.navController.Navigate(Actions.go_to_register);
            };
        }
    }
}