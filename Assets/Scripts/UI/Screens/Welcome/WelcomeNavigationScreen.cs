using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

[Preserve]
class WelcomeNavigationScreen : NavigationScreen
{

  public WelcomeNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
  {
    Add(uxmlAsset.CloneTree());
    // find login button and add click handler to navigate to login screen
    var loginButton = this.Q<Button>("login-button");
    loginButton.clicked += () =>
    {
      host.navController.Navigate("login");
    };
  }

  public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
  {
    base.OnEnter(controller, destination, args);

  }
}