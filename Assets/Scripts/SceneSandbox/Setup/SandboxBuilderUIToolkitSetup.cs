using UnityEngine;
using SceneSandbox.UI;

namespace SceneSandbox.Setup
{
    /// <summary>
    /// This script is responsible for setting up the UI Toolkit version of the Sandbox Builder UI.
    /// It will find the old UGUI-based UI, destroy it, and add the new UI Toolkit-based UI.
    /// </summary>
    public class SandboxBuilderUIToolkitSetup : MonoBehaviour
    {
        private void Awake()
        {
            // Find the old UI component
            var oldUI = FindObjectOfType<SandboxBuilderUI>();
            if (oldUI != null)
            {
                // Get the GameObject that the old UI is attached to
                var uiGameObject = oldUI.gameObject;

                // Destroy the old UI component
                Destroy(oldUI);

                // Add the new UI Toolkit component
                uiGameObject.AddComponent<SandboxBuilderUIToolkit>();
            }
        }
    }
}