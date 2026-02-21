using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;
using Systems.Core.UI.FormSubmit.MVVM;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Dynamic form panel using UI Toolkit that can build UI forms from JSON field definitions
    /// Singleton pattern ensures only one form panel exists at a time
    /// Supports various field types: text, textarea, selectbox, number, toggle, etc.
    /// </summary>
    public class FormSubmitPanelUIToolkit : PersistentSingleton<FormSubmitPanelUIToolkit>
    {
        #region Singleton
        // Inherits persistent singleton behavior: `Instance`, `HasInstance`, `Current`
        #endregion

        #region UI Components

        // No UIDocument: view attaches to a provided UIElements parent container

        [Header("Visual Assets")]
        [SerializeField] private VisualTreeAsset formPanelTemplate;
        [SerializeField] private StyleSheet formPanelStyleSheet;

        // MVVM bridge components
        private FormSubmitView mvvmView;
        private FormSubmitController mvvmController;

        // Initialization flag
        private bool isInitialized = false;

        #endregion

        #region Events

        public event Action<Dictionary<string, object>> OnFormSubmitted;
        public event Action OnFormCancelled;

        #endregion

        #region Properties

        public bool IsVisible { get; private set; }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            EnsureMvvmView();
            mvvmView?.Hide();
            isInitialized = true;
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Show the form panel with specified form definition
        /// </summary>
        public void Show(string title, List<FormFieldDefinition> fieldDefinitions, Action<Dictionary<string, object>> onSubmit = null, Action onCancel = null, Transform parent = null)
        {
            Debug.LogError("FormSubmitPanelUIToolkit: VisualElement parent required. Call Show(..., VisualElement parentElement).");
        }

        /// <summary>
        /// Show the form panel with a UI Toolkit parent container.
        /// </summary>
        public void Show(string title, List<FormFieldDefinition> fieldDefinitions, Action<Dictionary<string, object>> onSubmit, Action onCancel, VisualElement parentElement)
        {
            Initialize();
            EnsureMvvmView();
            if (mvvmView == null)
            {
                Debug.LogError("FormSubmitPanelUIToolkit: MVVM view not available. Cannot show form.");
                return;
            }

            // Configure view with UIElements parent container
            mvvmView.Configure(template: formPanelTemplate, styleSheet: formPanelStyleSheet, parentContainer: parentElement);

            try
            {
                var model = new FormSubmitModel(title, fieldDefinitions ?? new List<FormFieldDefinition>());

                mvvmController = new FormSubmitController.Builder()
                    .WithView(mvvmView)
                    .WithModel(model)
                    .Build();

                mvvmController.OnFormSubmitted += data =>
                {
                    onSubmit?.Invoke(data);
                    OnFormSubmitted?.Invoke(data);
                    IsVisible = false;
                };

                mvvmController.OnFormCancelled += () =>
                {
                    onCancel?.Invoke();
                    OnFormCancelled?.Invoke();
                    IsVisible = false;
                };

                mvvmView.Show();
                IsVisible = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FormSubmitPanelUIToolkit: Failed to show MVVM form with parent: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the form panel
        /// </summary>
        public void CloseForm()
        {
            if (mvvmView != null)
            {
                mvvmView.Hide();
            }
            IsVisible = false;
        }

        /// <summary>
        /// Force re-initialize the form UI if needed
        /// </summary>
        public void ForceReinitialize()
        {
            isInitialized = false;
            Initialize();
        }

        // Removed: TryShowWithMvvm (MVVM is handled directly in Show)

        private void EnsureMvvmView()
        {
            if (mvvmView == null)
            {
                mvvmView = GetComponent<FormSubmitView>();
                if (mvvmView == null)
                {
                    mvvmView = gameObject.AddComponent<FormSubmitView>();
                }
            }
            // Configuration is performed in Show().
        }

        /// <summary>
        /// Handle form submission with custom data (used by button fields)
        /// </summary>
        public void HandleCustomSubmission(Dictionary<string, object> customData)
        {
            OnFormSubmitted?.Invoke(customData);
            CloseForm();
        }

        /// <summary>
        /// Trigger form submission without closing the form (used by action buttons)
        /// </summary>
        public void TriggerFormSubmission(Dictionary<string, object> customData)
        {
            OnFormSubmitted?.Invoke(customData);
        }

        #endregion

        // Removed legacy form-building and handlers; MVVM handles rendering and interactions
    }
}