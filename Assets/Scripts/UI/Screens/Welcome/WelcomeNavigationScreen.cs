using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using AppUIButton = Unity.AppUI.UI.Button;

[Preserve]
class WelcomeNavigationScreen : NavigationScreen
{
  private readonly NavHost host;
  private AppUIButton loginButton;

  public WelcomeNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
  {
    this.host = host;
    uxmlAsset.CloneTree(this);
  }

  public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
  {
    base.OnEnter(controller, destination, args);

    this.loginButton = this.Q<AppUIButton>("login-link-button");
    loginButton.clicked += () =>
    {
      host.navController.Navigate("login");
    };
  }
}