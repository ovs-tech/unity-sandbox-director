using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace Systems.UI
{
    [UxmlFilePath("Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml", UxmlFilePathType.AssetDatabase)]
    public partial class ProjectCard : VisualElement
    {
        public ProjectCard()
        {
            UxmlCloneTree();
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
