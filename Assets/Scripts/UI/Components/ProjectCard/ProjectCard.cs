using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace Systems.UI
{
    [UxmlFilePath("Assets/Scripts/UI/Components/ProjectCard/ProjectCard.uxml", UxmlFilePathType.AssetDatabase)]
    public partial class ProjectCard : VisualElement
    {
        private readonly VisualElement _cardImage;
        private readonly Label _cardBadge;
        private readonly VisualElement _cardMenuButton;
        private readonly Label _projectName;
        private readonly Label _projectMeta;
        private readonly EventCallback<ClickEvent> _menuClickHandler;

        public event Action MenuClicked;

        public ProjectCard()
        {
            UxmlCloneTree();
            AddToClassList("ProjectCard");

            _cardImage = this.Q<VisualElement>("card-image");
            _cardBadge = this.Q<Label>("card-badge");
            _cardMenuButton = this.Q<VisualElement>("card-menu-button");
            _projectName = this.Q<Label>("project-name");
            _projectMeta = this.Q<Label>("project-meta");

            _menuClickHandler = _ => MenuClicked?.Invoke();

            if (_cardMenuButton != null)
            {
                _cardMenuButton.RegisterCallback(_menuClickHandler);
            }

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (_cardMenuButton != null)
            {
                _cardMenuButton.UnregisterCallback(_menuClickHandler);
            }
        }

        public void Initialize(string projectName, string meta = null, Texture2D image = null)
        {
            if (_projectName != null)
                _projectName.text = projectName;

            if (_projectMeta != null)
                _projectMeta.text = meta ?? string.Empty;

            if (image != null)
                SetImage(image);
        }

        public void SetImage(Texture2D image)
        {
            if (image == null || _cardImage == null)
                return;

            _cardImage.style.backgroundImage = new StyleBackground(image);
        }

        public void SetBadge(string text, bool visible)
        {
            if (_cardBadge == null)
                return;

            _cardBadge.text = text ?? string.Empty;
            _cardBadge.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetMenuVisible(bool visible)
        {
            if (_cardMenuButton == null)
                return;

            _cardMenuButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public new class UxmlFactory : UxmlFactory<ProjectCard, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription _name = new UxmlStringAttributeDescription { name = "project-name", defaultValue = "" };
            UxmlStringAttributeDescription _meta = new UxmlStringAttributeDescription { name = "project-meta", defaultValue = "" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var card = ve as ProjectCard;
                var name = _name.GetValueFromBag(bag, cc);
                var meta = _meta.GetValueFromBag(bag, cc);
                if (card != null && (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(meta)))
                    card.Initialize(name, meta, null);
            }
        }
    }
}
