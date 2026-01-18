using Unity.AppUI.Navigation;
using Unity.AppUI.UI;

class AppUIVisualController : INavVisualController
{
  public void SetupAppBar(AppBar appBar, NavDestination destination, NavController navController)
  {
    appBar.title = destination.label;
    appBar.stretch = true;
    appBar.expandedHeight = 92;
  }

  public void SetupBottomNavBar(BottomNavBar bottomNavBar, NavDestination destination, NavController navController)
  {
  }

  public void SetupDrawer(Drawer drawer, NavDestination destination, NavController navController)
  {
  }

  public void SetupNavigationRail(NavigationRail navigationRail, NavDestination destination, NavController navController)
  {
  }
}