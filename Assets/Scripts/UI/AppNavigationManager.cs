using UnityEngine;
using Unity.AppUI.Navigation;
using UnityEngine.UIElements;

public class AppNavigationManager : MonoBehaviour
{
  public UIDocument uiDocument;

  public NavGraphViewAsset graphAsset;

  void Start()
  {
    var navHost = new NavHost();
    navHost.navController.SetGraph(graphAsset);
    navHost.visualController = new AppNavVisualController();

    var panel = new Unity.AppUI.UI.Panel
    {
      scale = "large"
    };
    uiDocument.rootVisualElement.Add(panel);
    panel.StretchToParentSize();

    panel.Add(navHost);
    navHost.StretchToParentSize();
  }
}
