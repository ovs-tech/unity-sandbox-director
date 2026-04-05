using Unity.AppUI.Navigation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

[Preserve]
class LoginNavigationScreen : NavigationScreen
{

  public LoginNavigationScreen(VisualTreeAsset uxmlAsset, NavHost host)
  {
    uxmlAsset.CloneTree(this);
  }

  public override void OnEnter(NavController controller, NavDestination destination, Argument[] args)
  {
    base.OnEnter(controller, destination, args);

  }
}