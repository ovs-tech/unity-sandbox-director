using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Systems.UI
{
    [Preserve]
    public class ListProjectPage : VisualElement
    {
    const string k_ResourceName = "ListProjectPage";
    const string k_ResourcePathAlt = "UI/Pages/ListProjectPage/ListProjectPage";
    const string k_AssetPath = "Assets/Scripts/UI/Pages/ListProjectPage/ListProjectPage.uxml";

    public ListProjectPage()
    {
        var tree = Resources.Load<VisualTreeAsset>(k_ResourceName) ?? Resources.Load<VisualTreeAsset>(k_ResourcePathAlt);
#if UNITY_EDITOR
        if (tree == null)
            tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_AssetPath);
#endif

        if (tree != null)
            tree.CloneTree(this);
    }

    public void Initialize(VisualTreeAsset treeAsset)
    {
        if (treeAsset != null)
            treeAsset.CloneTree(this);
    }
    }
}
