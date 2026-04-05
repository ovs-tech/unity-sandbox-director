using System;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Systems.UI
{
    [Serializable]
    public class ProjectsDestinationTemplate : NavDestinationTemplate
    {
        [SerializeField]
        [Tooltip("The UXML asset to instantiate when the destination is reached.")]
        VisualTreeAsset m_UxmlAsset;

        public override INavigationScreen CreateScreen(NavHost host)
        {
            var screen = new ProjectsNavigationScreen(m_UxmlAsset, host);
            return screen;
        }
    }
}
