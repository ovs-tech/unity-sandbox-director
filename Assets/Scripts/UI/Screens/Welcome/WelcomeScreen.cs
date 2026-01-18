using Unity.AppUI.Navigation;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using UnityEngine.Scripting;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;
using UnityEditor;

[Preserve]
class WelcomeScreen : NavigationScreen
{
  public WelcomeScreen()
  {
    // var content = new Preloader();
    // content.StretchToParentSize();

    // hierarchy.Add(content);

    hierarchy.Add(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/UI/Screens/Welcome/WelcomeScreen.uxml").CloneTree());

    // schedule.Execute((timer) =>
    // {
    //   var loggedIn = PlayerPrefs.GetInt("logged_in", 0) == 1;
    //   if (!loggedIn)
    //     _ = this.FindNavController().Navigate(Actions.welcome_to_auth);
    //   else
    //     _ = this.FindNavController().Navigate(Actions.welcome_to_home);
    // }).ExecuteLater(3000);
  }
}