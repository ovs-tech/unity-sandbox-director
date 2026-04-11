using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Systems.UI.Tests
{
    public class ProjectCardTests
    {
        private const string CardMediaName = "card-media";
        private const string CardImageName = "card-image";
        private const string CardBadgeName = "card-badge";
        private const string CardMenuButtonName = "card-menu-button";
        private const string CardGradientName = "card-gradient";
        private const string ProjectNameLabelName = "project-name";
        private const string ProjectMetaLabelName = "project-meta";

        [Test]
        public void Constructor_ClonesExpectedElements()
        {
            var card = CreateCard();

            Assert.IsNotNull(card.Q<VisualElement>(CardMediaName));
            Assert.IsNotNull(card.Q<VisualElement>(CardImageName));
            Assert.IsNotNull(card.Q<Label>(CardBadgeName));
            Assert.IsNotNull(card.Q<VisualElement>(CardMenuButtonName));
            Assert.IsNotNull(card.Q<VisualElement>(CardGradientName));
            Assert.IsNotNull(card.Q<Label>(ProjectNameLabelName));
            Assert.IsNotNull(card.Q<Label>(ProjectMetaLabelName));
        }

        [Test]
        public void Initialize_UpdatesNameMetaAndImage_ClearsMetaWhenNull()
        {
            var card = CreateCard();
            var projectName = card.Q<Label>(ProjectNameLabelName);
            var projectMeta = card.Q<Label>(ProjectMetaLabelName);
            var cardImage = card.Q<VisualElement>(CardImageName);

            Assert.IsNotNull(projectName, "Expected project-name label to exist.");
            Assert.IsNotNull(projectMeta, "Expected project-meta label to exist.");
            Assert.IsNotNull(cardImage, "Expected card-image element to exist.");

            projectMeta.text = "stale-meta";

            var texture = new Texture2D(2, 2);
            try
            {
                InvokePublicInstanceMethod(
                    card,
                    "Initialize",
                    new[] { typeof(string), typeof(string), typeof(Texture2D) },
                    "Late Night Scene 01",
                    null,
                    texture);

                Assert.AreEqual("Late Night Scene 01", projectName.text);
                Assert.AreEqual(string.Empty, projectMeta.text, "Initialize should clear project-meta when meta is null.");
                Assert.AreEqual(texture, cardImage.style.backgroundImage.value.texture);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void SetBadge_UpdatesTextAndCanHideBadge()
        {
            var card = CreateCard();
            var badge = card.Q<Label>(CardBadgeName);

            Assert.IsNotNull(badge, "Expected card-badge label to exist.");

            InvokePublicInstanceMethod(card, "SetBadge", new[] { typeof(string), typeof(bool) }, "Featured", true);

            Assert.AreEqual("Featured", badge.text, "SetBadge should set badge text when visible is true.");
            Assert.AreEqual(DisplayStyle.Flex, badge.style.display.value, "SetBadge should show badge when visible is true.");

            InvokePublicInstanceMethod(card, "SetBadge", new[] { typeof(string), typeof(bool) }, string.Empty, false);

            Assert.AreEqual(DisplayStyle.None, badge.style.display.value, "SetBadge should hide badge when visible is false.");
        }

        [Test]
        public void SetMenuVisible_HidesMenuWithoutThrowing()
        {
            var card = CreateCard();
            var menuButton = card.Q<VisualElement>(CardMenuButtonName);

            Assert.IsNotNull(menuButton, "Expected card-menu-button element to exist.");

            Assert.DoesNotThrow(() => InvokePublicInstanceMethod(card, "SetMenuVisible", new[] { typeof(bool) }, false));
            Assert.AreEqual(DisplayStyle.None, menuButton.style.display.value);

            Assert.DoesNotThrow(() => InvokePublicInstanceMethod(card, "SetMenuVisible", new[] { typeof(bool) }, true));
            Assert.AreEqual(DisplayStyle.Flex, menuButton.style.display.value);
        }

        [Test]
        public void MenuButtonClick_RaisesMenuClickedOnce()
        {
            var card = CreateCard();
            var cardType = card.GetType();
            var menuClickedEvent = cardType.GetEvent("MenuClicked", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(menuClickedEvent, "ProjectCard should expose a public MenuClicked event.");

            var menuButton = card.Q<VisualElement>(CardMenuButtonName);
            Assert.IsNotNull(menuButton, "Expected card-menu-button element to exist.");

            var clickedCount = 0;
            Action handler = () => clickedCount++;
            var subscribed = false;

            try
            {
                Assert.DoesNotThrow(
                    () => menuClickedEvent.AddEventHandler(card, handler),
                    "Adding MenuClicked event handler should not throw.");
                subscribed = true;

                using var clickEvent = ClickEvent.GetPooled();
                menuButton.SendEvent(clickEvent);
            }
            finally
            {
                if (subscribed)
                {
                    Assert.DoesNotThrow(
                        () => menuClickedEvent.RemoveEventHandler(card, handler),
                        "Removing MenuClicked event handler should not throw.");
                }
            }

            Assert.AreEqual(1, clickedCount, "MenuClicked should raise exactly once when menu button is clicked.");
        }

        private static VisualElement CreateCard()
        {
            var projectCardType = FindProjectCardType();
            Assert.IsNotNull(projectCardType, "Could not locate Systems.UI.ProjectCard type. Ensure Systems.UI assembly is loaded.");

            var instance = Activator.CreateInstance(projectCardType);
            Assert.IsInstanceOf<VisualElement>(instance);
            return (VisualElement)instance;
        }

        private static Type FindProjectCardType()
        {
            var direct = Type.GetType("Systems.UI.ProjectCard, Systems.UI");
            if (direct != null)
                return direct;

            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType("Systems.UI.ProjectCard", false))
                .FirstOrDefault(t => t != null);
        }

        private static object InvokePublicInstanceMethod(object target, string methodName, Type[] argumentTypes, params object[] args)
        {
            Assert.IsNotNull(argumentTypes, $"Expected argumentTypes for method '{methodName}'.");

            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: argumentTypes,
                modifiers: null);

            Assert.IsNotNull(
                method,
                $"Expected public method '{methodName}({string.Join(", ", argumentTypes.Select(t => t.Name))})' on {target.GetType().Name}.");
            return method.Invoke(target, args);
        }
    }
}