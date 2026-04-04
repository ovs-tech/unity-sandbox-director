using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

[Preserve]
class WelcomeNavigationScreen : NavigationScreen
{
  private readonly NavHost host;
  private readonly Button loginButton;
  public WelcomeNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
  {
    this.host = host;
    style.flexDirection = FlexDirection.Column;
    uxmlAsset.CloneTree(this);
    // find login button and add click handler to navigate to login screen
    loginButton = this.Q<Button>("login-link-button");
  }

  public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
  {
    base.OnEnter(controller, destination, args);

    // loginButton.clicked += () =>
    // {
    //   host.navController.Navigate("login");
    // };
  }
}