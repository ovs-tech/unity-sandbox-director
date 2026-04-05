using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using AppUIButton = Unity.AppUI.UI.Button;

namespace Systems.UI
{
    [Preserve]
    class RegisterNavigationScreen : NavigationScreen
    {
        private readonly NavHost host;
        private readonly AppUIButton registerButton;
        private readonly AppUIButton loginButton;

        public RegisterNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
        {
            this.host = host;
            uxmlAsset.CloneTree(this);

            this.registerButton = this.Q<AppUIButton>("register-button");
            this.loginButton = this.Q<AppUIButton>("login-link-button");
        }

        public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
        {
            base.OnEnter(controller, destination, args);
            if (registerButton != null)
            {
                registerButton.clicked += () =>
                {
                    host.navController.Navigate("welcome");
                };
            }

            if (loginButton != null)
            {
                loginButton.clicked += () =>
                {
                    host.navController.Navigate("login");
                };
            }
        }
    }
}
