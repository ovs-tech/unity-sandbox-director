using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Editor
{
    public static class PlacementSetupWizard
    {
        [MenuItem("Tools/Placement System/Add To Scene")]
        public static void SetupScene()
        {
            var go = new GameObject("Placement Manager");
            go.AddComponent<PlacementController>();
            UnityEditor.Selection.activeGameObject = go;
            Debug.Log("Placement System setup complete!");
        }
    }
}
