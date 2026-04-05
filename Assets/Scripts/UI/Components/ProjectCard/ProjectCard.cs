using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Systems.UI
{
    [Preserve]
    public class ProjectCard : VisualElement
    {
    const string k_ResourceName = "ProjectCard";
    const string k_AssetPath = "Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml";

    public ProjectCard()
    {
        var tree = Resources.Load<VisualTreeAsset>(k_ResourceName);
#if UNITY_EDITOR
        if (tree == null)
            tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_AssetPath);
#endif
        if (tree != null)
            tree.CloneTree(this);
    }

    public void Initialize(string projectName, string meta = null, Texture2D image = null)
    {
        var nameLabel = this.Q<Label>("project-name");
        if (nameLabel != null)
            nameLabel.text = projectName;

        if (!string.IsNullOrEmpty(meta))
        {
            var metaLabel = this.Q<Label>("project-meta");
            if (metaLabel != null)
                metaLabel.text = meta;
        }

        if (image != null)
            SetImage(image);
    }

    public void SetImage(Texture2D image)
    {
        var img = this.Q<VisualElement>("card-image");
        if (img != null)
            img.style.backgroundImage = new StyleBackground(image);
    }

    public new class UxmlFactory : UxmlFactory<ProjectCard, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = "project-name", defaultValue = "" };
        UxmlStringAttributeDescription m_Meta = new UxmlStringAttributeDescription { name = "project-meta", defaultValue = "" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var card = ve as ProjectCard;
            var name = m_Name.GetValueFromBag(bag, cc);
            var meta = m_Meta.GetValueFromBag(bag, cc);
            if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(meta))
                card.Initialize(name, meta, null);
        }
    }
    }
}
