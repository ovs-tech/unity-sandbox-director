using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.UI;

namespace Systems.UI
{
    class AppNavVisualController : INavVisualController
    {
        public void SetupBottomNavBar(BottomNavBar bottomNavBar, NavDestination destination, NavController navController)
        {
        }

        public void SetupAppBar(AppBar appBar, NavDestination destination, NavController navController)
        {
            appBar.title = destination.label;
            appBar.stretch = true;
            appBar.expandedHeight = 92;
        }

        public void SetupDrawer(Drawer drawer, NavDestination destination, NavController navController)
        {
        }

        public void SetupNavigationRail(NavigationRail navigationRail, NavDestination destination, NavController navController)
        {
            // Not used in this sample
        }
    }
}