using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using AppUIButton = Unity.AppUI.UI.Button;

[Preserve]
class RegisterNavigationScreen : NavigationScreen
{
    private readonly NavHost host;
    private AppUIButton registerButton;
    private AppUIButton loginButton;

    public RegisterNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
    {
        this.host = host;
        uxmlAsset.CloneTree(this);
    }

    public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
    {
        base.OnEnter(controller, destination, args);

        this.registerButton = this.Q<AppUIButton>("register-button");
        if (registerButton != null)
        {
            registerButton.clicked += () =>
            {
                host.navController.Navigate("welcome");
            };
        }

        this.loginButton = this.Q<AppUIButton>("login-link-button");
        if (loginButton != null)
        {
            loginButton.clicked += () =>
            {
                host.navController.Navigate("login");
            };
        }
    }
}
