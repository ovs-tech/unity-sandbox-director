using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.FormSubmit;
using Unity.Properties;

namespace Core.UI.FormSubmit.MVVM
{
    /// <summary>
    /// UI Toolkit view for the form submit panel. Loads UXML/USS and exposes UI element references.
    /// </summary>
    public class FormSubmitView : MonoBehaviour
    {
        // UXML element IDs (names)
        private const string BackgroundPanelId = "background-panel";
        private const string FormContainerId = "form-container";
        private const string ScrollViewId = "scroll-view";
        private const string FieldsContainerId = "fields-container";
        private const string TitleTextId = "title-text";
        private const string CloseButtonId = "close-button";
        private const string SubmitButtonId = "submit-button";
        private const string CancelButtonId = "cancel-button";

        // No UIDocument dependency; attach to provided VisualElement parent

        [Header("Visual Assets")]
        [SerializeField] private VisualTreeAsset _formPanelTemplate;
        [SerializeField] private StyleSheet _formPanelStyleSheet;

        public event Action OnSubmitClicked;
        public event Action OnCancelClicked;
        public event Action OnCloseClicked;

        public VisualElement RootElement { get; private set; }
        public VisualElement FieldsContainer { get; private set; }
        public Button SubmitButton { get; private set; }
        public Button CancelButton { get; private set; }
        public Button CloseButton { get; private set; }
        public bool IsVisible { get; private set; }

        private Label _titleLabel;
        private VisualElement _backgroundPanel;
    private VisualElement _parentContainer;

        public IEnumerator InitializeView(FormSubmitViewModel viewModel)
        {
            LoadTemplate();
            CacheUIElements();
            Bind(viewModel);
            yield return null;
        }
        public void Configure(VisualTreeAsset template, StyleSheet styleSheet)
        {
            _formPanelTemplate = template;
            _formPanelStyleSheet = styleSheet;
        }

        public void Configure(VisualTreeAsset template, StyleSheet styleSheet, VisualElement parentContainer)
        {
            _formPanelTemplate = template;
            _formPanelStyleSheet = styleSheet;
            _parentContainer = parentContainer;
        }

        public void Bind(FormSubmitViewModel viewModel)
        {
            if (viewModel == null)
            {
                Debug.LogWarning("FormSubmitView: viewModel is null; binding skipped.");
                return;
            }

            if (_titleLabel != null)
            {
                _titleLabel.SetBinding(nameof(Label.text), new DataBinding
                {
                    dataSource = viewModel.Title,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
                    bindingMode = BindingMode.ToTarget
                });
            }

            if (SubmitButton != null)
            {
                SubmitButton.clicked -= HandleSubmitClicked;
                SubmitButton.clicked += HandleSubmitClicked;
                SubmitButton.SetEnabled(viewModel.IsSubmitEnabled.Value);
            }

            if (CancelButton != null)
            {
                CancelButton.clicked -= HandleCancelClicked;
                CancelButton.clicked += HandleCancelClicked;
            }

            if (CloseButton != null)
            {
                CloseButton.clicked -= HandleCloseClicked;
                CloseButton.clicked += HandleCloseClicked;
            }
        }

        public void Show()
        {
            if (_backgroundPanel == null)
            {
                if (RootElement == null)
                {
                    Debug.LogWarning("FormSubmitView: Show() called but RootElement is null.");
                    return;
                }

                _backgroundPanel = RootElement.Q(BackgroundPanelId);
                if (_backgroundPanel == null)
                {
                    Debug.LogWarning("FormSubmitView: Show() could not find background panel.");
                    return;
                }
            }

            _backgroundPanel.style.display = DisplayStyle.Flex;
            IsVisible = true;
        }

        public void Hide()
        {
            if (_backgroundPanel == null)
            {
                if (RootElement == null)
                {
                    Debug.LogWarning("FormSubmitView: Hide() called but RootElement is null.");
                    return;
                }

                _backgroundPanel = RootElement.Q(BackgroundPanelId);
                if (_backgroundPanel == null)
                {
                    Debug.LogWarning("FormSubmitView: Hide() could not find background panel.");
                    return;
                }
            }

            _backgroundPanel.style.display = DisplayStyle.None;
            IsVisible = false;

            // Remove overlay when hiding
            if (_parentContainer != null && RootElement != null)
            {
                _parentContainer.Remove(RootElement);
            }
        }

        public void RefreshSubmitState(bool isEnabled)
        {
            SubmitButton?.SetEnabled(isEnabled);
        }

        // No UIDocument handling; parent VisualElement is required

        private void LoadTemplate()
        {
            EnsureAssetsLoaded();
            var baseRoot = _parentContainer;
            if (baseRoot == null)
            {
                Debug.LogWarning("FormSubmitView: No parent container provided for template load.");
                return;
            }

            var overlay = baseRoot.Q("form-submit-overlay");
            if (overlay == null)
            {
                overlay = new VisualElement { name = "form-submit-overlay" };
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0;
                overlay.style.top = 0;
                overlay.style.right = 0;
                overlay.style.bottom = 0;
                baseRoot.Add(overlay);
            }

            RootElement = overlay;
            RootElement?.Clear();

            if (_formPanelStyleSheet != null)
            {
                RootElement?.styleSheets.Add(_formPanelStyleSheet);
            }

            if (_formPanelTemplate != null)
            {
                _formPanelTemplate.CloneTree(RootElement);
            }
            else
            {
                Debug.LogWarning("FormSubmitView: No UXML template assigned.");
            }
        }

        private void EnsureAssetsLoaded()
        {
            if (_formPanelTemplate != null && _formPanelStyleSheet != null)
            {
                return;
            }

            var resources = FormSubmitUIToolkitUtils.LoadFormResources();
            if (_formPanelTemplate == null)
            {
                _formPanelTemplate = resources.uxml;
            }

            if (_formPanelStyleSheet == null)
            {
                _formPanelStyleSheet = resources.uss;
            }
        }

        private void CacheUIElements()
        {
            if (RootElement == null)
            {
                Debug.LogWarning("FormSubmitView: RootElement is null; cannot cache UI elements.");
                return;
            }

            _backgroundPanel = RootElement.Q(BackgroundPanelId);
            var formContainer = RootElement.Q(FormContainerId);
            var scrollView = RootElement.Q<ScrollView>(ScrollViewId);
            FieldsContainer = RootElement.Q(FieldsContainerId);
            _titleLabel = RootElement.Q<Label>(TitleTextId);
            CloseButton = RootElement.Q<Button>(CloseButtonId);
            SubmitButton = RootElement.Q<Button>(SubmitButtonId);
            CancelButton = RootElement.Q<Button>(CancelButtonId);

            if (_backgroundPanel == null || formContainer == null || scrollView == null || FieldsContainer == null ||
                _titleLabel == null || CloseButton == null || SubmitButton == null || CancelButton == null)
            {
                Debug.LogWarning("FormSubmitView: One or more UI elements are missing after template load.");
            }
        }

        private void HandleSubmitClicked()
        {
            OnSubmitClicked?.Invoke();
        }

        private void HandleCancelClicked()
        {
            OnCancelClicked?.Invoke();
        }

        private void HandleCloseClicked()
        {
            OnCloseClicked?.Invoke();
        }
    }
}
