using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Systems.Core.UI.FormSubmit.MVVM
{
    /// <summary>
    /// ViewModel exposing bindable properties and commands for form submit UI.
    /// Pure C# to keep it unit test friendly.
    /// </summary>
    public class FormSubmitViewModel
    {
        private readonly FormSubmitModel _model;
        private bool _isSubmitEnabled;
        private string _validationMessage;

        public readonly BindableProperty<string> Title;
        public readonly BindableProperty<int> FieldCount;
        public readonly BindableProperty<bool> IsSubmitEnabled;
        public readonly BindableProperty<string> ValidationMessage;

        public event Action<IReadOnlyDictionary<string, object>> OnSubmitRequested;
        public event Action OnCancelRequested;

        public FormSubmitViewModel(FormSubmitModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _isSubmitEnabled = ValidateForm(out _validationMessage);

            Title = BindableProperty<string>.Bind(() => _model.Title);
            FieldCount = BindableProperty<int>.Bind(() => _model.FieldConfigs.Count);
            IsSubmitEnabled = BindableProperty<bool>.Bind(() => _isSubmitEnabled);
            ValidationMessage = BindableProperty<string>.Bind(() => _validationMessage);

            _model.OnFormDataChanged += HandleFormDataChanged;
        }

        public void Submit()
        {
            RefreshValidation();
            if (!_isSubmitEnabled)
            {
                return;
            }

            var values = _model.GetAllFieldValues();
            OnSubmitRequested?.Invoke(new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(values)));
        }

        public void Cancel()
        {
            OnCancelRequested?.Invoke();
        }

        public void ClearFields()
        {
            _model.ClearFieldValues();
            RefreshValidation();
        }

        private void HandleFormDataChanged(IReadOnlyDictionary<string, object> _)
        {
            RefreshValidation();
        }

        private void RefreshValidation()
        {
            _isSubmitEnabled = ValidateForm(out _validationMessage);
        }

        private bool ValidateForm(out string validationMessage)
        {
            validationMessage = string.Empty;

            foreach (var definition in _model.FieldConfigs)
            {
                if (!definition.required)
                {
                    continue;
                }

                if (!_model.FieldValues.TryGetValue(definition.name, out var value) || string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    validationMessage = string.IsNullOrEmpty(definition.label)
                        ? $"{definition.name} is required"
                        : $"{definition.label} is required";
                    return false;
                }
            }

            return true;
        }
    }
}
