using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit.MVVM
{
    /// <summary>
    /// Orchestrates the FormSubmit MVVM flow. Initializes Model/View/ViewModel, wires events, and manages field lifecycle.
    /// </summary>
    public class FormSubmitController
    {
        private readonly FormSubmitView _view;
        private readonly FormSubmitModel _model;
        private FormSubmitViewModel _viewModel;
        private readonly List<IFormFieldUIToolkit> _fieldInstances = new List<IFormFieldUIToolkit>();

        public event Action<Dictionary<string, object>> OnFormSubmitted;
        public event Action OnFormCancelled;

        private FormSubmitController(FormSubmitView view, FormSubmitModel model)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _view.StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            _viewModel = new FormSubmitViewModel(_model);
            yield return _view.InitializeView(_viewModel);
            Bind();
            CreateFields();
            _view.Show();
        }

        private void Bind()
        {
            _view.OnSubmitClicked += HandleSubmitClicked;
            _view.OnCancelClicked += HandleCancelClicked;
            _view.OnCloseClicked += HandleCancelClicked;

            _viewModel.OnSubmitRequested += HandleSubmitRequested;
            _viewModel.OnCancelRequested += HandleCancelRequested;

            _model.OnFormDataChanged += HandleFormDataChanged;

            UpdateSubmitState();
        }

        private void HandleSubmitClicked()
        {
            _viewModel.Submit();
        }

        private void HandleCancelClicked()
        {
            _viewModel.Cancel();
        }

        private void HandleSubmitRequested(IReadOnlyDictionary<string, object> values)
        {
            OnFormSubmitted?.Invoke(new Dictionary<string, object>(values));
            _view.Hide();
        }

        private void HandleCancelRequested()
        {
            OnFormCancelled?.Invoke();
            _view.Hide();
        }

        private void HandleFormDataChanged(IReadOnlyDictionary<string, object> _)
        {
            UpdateSubmitState();
        }

        private void UpdateSubmitState()
        {
            _view.RefreshSubmitState(_viewModel.IsSubmitEnabled.Value);
        }

        private void CreateFields()
        {
            ClearFields();

            if (_view.FieldsContainer == null)
            {
                Debug.LogWarning("FormSubmitController: Fields container is null; cannot render fields.");
                return;
            }

            foreach (var fieldDef in _model.FieldConfigs)
            {
                var fieldElement = CreateField(fieldDef);
                if (fieldElement == null)
                {
                    continue;
                }

                _view.FieldsContainer.Add(fieldElement);
                var formField = fieldElement.userData as IFormFieldUIToolkit;
                if (formField != null)
                {
                    _fieldInstances.Add(formField);
                    SafeSetModelValue(formField);
                }
            }

            FocusFirstField();
        }

        private void ClearFields()
        {
            _view.FieldsContainer?.Clear();
            _fieldInstances.Clear();
        }

        private void FocusFirstField()
        {
            if (_fieldInstances.Count > 0)
            {
                _fieldInstances[0].Focus();
            }
        }

        private void SafeSetModelValue(IFormFieldUIToolkit formField)
        {
            try
            {
                _model.SetFieldValue(formField.GetName(), formField.GetValue());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"FormSubmitController: Failed to set default value for field '{formField.GetName()}': {ex.Message}");
            }
        }

        private VisualElement CreateField(FormFieldDefinition fieldDef)
        {
            if (fieldDef == null)
            {
                return null;
            }

            switch (fieldDef.type?.ToLowerInvariant())
            {
                case "text":
                    return CreateTextField(fieldDef);
                case "textarea":
                    return CreateTextAreaField(fieldDef);
                case "number":
                    return CreateNumberField(fieldDef);
                case "select":
                case "selectbox":
                    return CreateSelectField(fieldDef);
                case "toggle":
                case "checkbox":
                    return CreateToggleField(fieldDef);
                case "slider":
                    return CreateSliderField(fieldDef);
                case "color":
                    return CreateColorField(fieldDef);
                case "button":
                    return CreateButtonField(fieldDef);
                case "info":
                    return CreateInfoField(fieldDef);
                case "hidden":
                    return CreateHiddenField(fieldDef);
                default:
                    Debug.LogWarning($"FormSubmitController: Unsupported field type '{fieldDef.type}', defaulting to text field.");
                    return CreateTextField(fieldDef);
            }
        }

        private VisualElement CreateTextField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var textField = new TextField();
            textField.value = fieldDef.defaultValue?.ToString() ?? string.Empty;
            textField.AddToClassList("field-input");

            var formField = new TextFormFieldUIToolkit();
            formField.Initialize(fieldDef, textField);
            container.userData = formField;

            textField.RegisterValueChangedCallback(_ => UpdateModelFromField(formField));

            container.Add(label);
            container.Add(textField);
            return container;
        }

        private VisualElement CreateTextAreaField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var textField = new TextField
            {
                multiline = true,
                value = fieldDef.defaultValue?.ToString() ?? string.Empty
            };
            textField.AddToClassList("field-textarea");
            textField.style.height = 100;

            var formField = new TextAreaFormFieldUIToolkit();
            formField.Initialize(fieldDef, textField);
            container.userData = formField;

            textField.RegisterValueChangedCallback(_ => UpdateModelFromField(formField));

            container.Add(label);
            container.Add(textField);
            return container;
        }

        private VisualElement CreateNumberField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var floatField = new FloatField();
            if (fieldDef.defaultValue != null && float.TryParse(fieldDef.defaultValue.ToString(), out var defaultVal))
            {
                floatField.value = defaultVal;
            }
            floatField.AddToClassList("field-input");

            var formField = new NumberFormFieldUIToolkit();
            formField.Initialize(fieldDef, floatField);
            container.userData = formField;

            floatField.RegisterValueChangedCallback(_ => UpdateModelFromField(formField));

            container.Add(label);
            container.Add(floatField);
            return container;
        }

        private VisualElement CreateSelectField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var dropdownField = new DropdownField();

            if (fieldDef.options != null && fieldDef.options.TryGetValue("items", out var itemsObj) && itemsObj is List<string> items)
            {
                dropdownField.choices = new List<string>(items);
                if (!string.IsNullOrEmpty(fieldDef.defaultValue?.ToString()) && items.Contains(fieldDef.defaultValue.ToString()))
                {
                    dropdownField.value = fieldDef.defaultValue.ToString();
                }
                else if (items.Count > 0)
                {
                    dropdownField.value = items[0];
                }
            }

            dropdownField.AddToClassList("field-input");

            var formField = new SelectFormFieldUIToolkit();
            formField.Initialize(fieldDef, dropdownField);
            container.userData = formField;

            dropdownField.RegisterValueChangedCallback(_ => UpdateModelFromField(formField));

            container.Add(label);
            container.Add(dropdownField);
            return container;
        }

        private VisualElement CreateToggleField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            var toggle = new Toggle();
            if (fieldDef.defaultValue is bool boolVal)
            {
                toggle.value = boolVal;
            }
            toggle.AddToClassList("field-toggle");
            toggle.style.marginRight = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");

            var formField = new ToggleFormFieldUIToolkit();
            formField.Initialize(fieldDef, toggle);
            container.userData = formField;

            toggle.RegisterValueChangedCallback(_ => UpdateModelFromField(formField));

            container.Add(toggle);
            container.Add(label);
            return container;
        }

        private VisualElement CreateSliderField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var slider = new Slider();
            if (fieldDef.options != null)
            {
                if (fieldDef.options.TryGetValue("min", out var minObj) && float.TryParse(minObj.ToString(), out var min))
                {
                    slider.lowValue = min;
                }
                if (fieldDef.options.TryGetValue("max", out var maxObj) && float.TryParse(maxObj.ToString(), out var max))
                {
                    slider.highValue = max;
                }
            }

            if (fieldDef.defaultValue != null && float.TryParse(fieldDef.defaultValue.ToString(), out var defaultVal))
            {
                slider.value = defaultVal;
            }

            slider.AddToClassList("field-slider");

            var formField = new SliderFormFieldUIToolkit();
            formField.Initialize(fieldDef, slider);
            container.userData = formField;

            slider.RegisterValueChangedCallback(_ => UpdateModelFromField(formField));

            container.Add(label);
            container.Add(slider);
            return container;
        }

        private VisualElement CreateColorField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var colorButton = new Button
            {
                text = "Select Color"
            };
            colorButton.AddToClassList("field-color-button");
            colorButton.style.height = 30;

            var formField = new ColorFormFieldUIToolkit();
            formField.Initialize(fieldDef, colorButton);
            container.userData = formField;

            colorButton.clicked += () => UpdateModelFromField(formField);

            container.Add(label);
            container.Add(colorButton);
            return container;
        }

        private VisualElement CreateButtonField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var button = new Button
            {
                text = fieldDef.label
            };
            button.AddToClassList("field-button");

            var formField = new ButtonFormFieldUIToolkit();
            formField.Initialize(fieldDef, button);
            container.userData = formField;

            button.clicked += () =>
            {
                UpdateModelFromField(formField);
                var actionData = new Dictionary<string, object>
                {
                    [fieldDef.name] = formField.GetValue()
                };
                OnFormSubmitted?.Invoke(actionData);
            };

            container.Add(button);
            return container;
        }

        private VisualElement CreateInfoField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var textField = new TextField
            {
                value = fieldDef.defaultValue?.ToString() ?? string.Empty,
                isReadOnly = true
            };
            textField.SetEnabled(false);
            textField.AddToClassList("field-info");

            var formField = new InfoFormFieldUIToolkit();
            formField.Initialize(fieldDef, textField);
            container.userData = formField;

            container.Add(label);
            container.Add(textField);
            return container;
        }

        private VisualElement CreateHiddenField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.style.display = DisplayStyle.None;

            var formField = new HiddenFormFieldUIToolkit();
            formField.Initialize(fieldDef, container);
            container.userData = formField;

            return container;
        }

        private void UpdateModelFromField(IFormFieldUIToolkit formField)
        {
            if (formField == null)
            {
                return;
            }

            try
            {
                _model.SetFieldValue(formField.GetName(), formField.GetValue());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"FormSubmitController: Failed to update model for field '{formField.GetName()}': {ex.Message}");
            }
        }

        public class Builder
        {
            private FormSubmitView _view;
            private FormSubmitModel _model;

            public Builder WithView(FormSubmitView view)
            {
                _view = view;
                return this;
            }

            public Builder WithModel(FormSubmitModel model)
            {
                _model = model;
                return this;
            }

            public FormSubmitController Build()
            {
                if (_view == null)
                {
                    throw new InvalidOperationException("FormSubmitController.Builder requires a FormSubmitView.");
                }

                if (_model == null)
                {
                    throw new InvalidOperationException("FormSubmitController.Builder requires a FormSubmitModel.");
                }

                return new FormSubmitController(_view, _model);
            }
        }
    }
}
