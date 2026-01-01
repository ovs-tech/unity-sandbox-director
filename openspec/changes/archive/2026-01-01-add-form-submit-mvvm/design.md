# Design: Form Submit MVVM Architecture

**Change ID:** `add-form-submit-mvvm`  
**Date:** 2025-12-26

## Architecture Overview

The FormSubmit MVVM architecture follows the established pattern from MiniTimeline MVVM, with clear separation between Model (data), ViewModel (presentation logic), View (UI), and Controller (orchestration).

```
┌─────────────────────────────────────────────────────────────┐
│                        Controller                            │
│  - Orchestrates initialization                               │
│  - Wires events between Model, ViewModel, View              │
│  - Provides Builder pattern for fluent API                   │
└───┬───────────────────────────────────┬─────────────────────┘
    │                                   │
    ▼                                   ▼
┌───────────────────┐           ┌──────────────────────────────┐
│       Model       │           │         ViewModel            │
│  - Field configs  │◄──────────│  - BindableProperty<T>       │
│  - Field values   │           │  - UI-friendly transforms    │
│  - Events         │           │  - Command methods           │
└───────────────────┘           └────────┬─────────────────────┘
                                         │
                                         ▼
                                ┌─────────────────────────────┐
                                │          View               │
                                │  - UXML/USS loading         │
                                │  - UI element refs          │
                                │  - Data binding setup       │
                                └─────────────────────────────┘
```

## Component Design

### 1. FormSubmitModel

**Purpose**: Holds form field configurations and captured values

**Responsibilities**:
- Store field configurations (`List<FormFieldConfig>`)
- Store field values (`Dictionary<string, object>`)
- Emit events when data changes
- Validate field values (basic type checking)

**Key Properties**:
```csharp
[Serializable]
public class FormSubmitModel {
    [SerializeField] private string _title;
    [SerializeField] private List<FormFieldConfig> _fieldConfigs;
    
    private Dictionary<string, object> _fieldValues;
    
    public event Action<Dictionary<string, object>> OnFormDataChanged;
    public event Action<string, object> OnFieldValueChanged;
    
    public string Title { get; set; }
    public IReadOnlyList<FormFieldConfig> FieldConfigs { get; }
    public IReadOnlyDictionary<string, object> FieldValues { get; }
}
```

**Design Decisions**:
- Keep serializable for inspector debugging
- Use `IReadOnly*` interfaces for public API to prevent external mutation
- Emit granular events (per-field) for reactive UI updates

### 2. FormSubmitViewModel

**Purpose**: Exposes UI-bindable properties and command methods

**Responsibilities**:
- Provide `BindableProperty<T>` for reactive UI binding
- Transform model data to UI-friendly formats (e.g., int → string)
- Expose command methods (Submit, Cancel, ClearFields)
- Track UI state (IsSubmitEnabled, HasValidationErrors)

**Key Properties**:
```csharp
public class FormSubmitViewModel {
    private readonly FormSubmitModel _model;
    
    // Bindable properties
    public readonly BindableProperty<string> Title;
    public readonly BindableProperty<int> FieldCount;
    public readonly BindableProperty<bool> IsSubmitEnabled;
    public readonly BindableProperty<string> ValidationMessage;
    
    // Commands
    public Action Submit { get; }
    public Action Cancel { get; }
    public Action ClearFields { get; }
    
    public FormSubmitViewModel(FormSubmitModel model) {
        _model = model;
        
        // Bind properties
        Title = BindableProperty<string>.Bind(() => _model.Title);
        FieldCount = BindableProperty<int>.Bind(() => _model.FieldConfigs.Count);
        IsSubmitEnabled = BindableProperty<bool>.Bind(() => ValidateForm());
        ValidationMessage = BindableProperty<string>.Bind(() => GetValidationMessage());
        
        // Bind commands
        Submit = () => { /* validation + emit OnSubmit */ };
        Cancel = () => { /* emit OnCancel */ };
    }
}
```

**Design Decisions**:
- Pure C# (no UnityEngine dependencies) for testability
- One-way binding (Model → View) via `BindableProperty<T>`
- Commands as `Action` properties (consistent with MiniTimeline)

### 3. FormSubmitView

**Purpose**: UI Toolkit view that loads UXML/USS and exposes UI element references

**Responsibilities**:
- Load UXML template and apply USS styles
- Query and expose UI element references (buttons, containers, labels)
- Provide initialization coroutine for async loading
- Wire up data bindings to ViewModel properties

**Key Structure**:
```csharp
public class FormSubmitView : MonoBehaviour {
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private VisualTreeAsset _formPanelTemplate;
    [SerializeField] private StyleSheet _formPanelStyleSheet;
    
    // UI element references
    private VisualElement _rootElement;
    private VisualElement _formContainer;
    private ScrollView _scrollView;
    private VisualElement _fieldsContainer;
    private Label _titleText;
    private Button _submitButton;
    private Button _cancelButton;
    private Button _closeButton;
    
    // Events
    public event Action OnSubmitClicked;
    public event Action OnCancelClicked;
    public event Action OnCloseClicked;
    
    public IEnumerator InitializeView(FormSubmitViewModel viewModel) {
        // Load UXML/USS
        // Query UI elements
        // Setup data bindings
        // Wire button events
        yield return null;
    }
}
```

**Design Decisions**:
- Inherit from `MonoBehaviour` to leverage Unity lifecycle
- Return `IEnumerator` from `InitializeView` for async initialization (consistent with MiniTimeline)
- Expose events for user actions (delegate to Controller for handling)

### 4. FormSubmitController

**Purpose**: Orchestrates Model-View-ViewModel interactions and manages lifecycle

**Responsibilities**:
- Initialize Model, ViewModel, and View
- Wire events between components
- Handle form submission and cancellation
- Manage field creation and updates
- Provide Builder pattern for fluent API

**Key Structure**:
```csharp
public class FormSubmitController {
    private readonly FormSubmitView _view;
    private readonly FormSubmitModel _model;
    private FormSubmitViewModel _viewModel;
    
    private readonly List<IFormFieldUIToolkit> _fieldInstances;
    
    private FormSubmitController(FormSubmitView view, FormSubmitModel model) {
        _view = view;
        _model = model;
        
        _view.StartCoroutine(Initialize());
    }
    
    private IEnumerator Initialize() {
        _viewModel = new FormSubmitViewModel(_model);
        yield return _view.InitializeView(_viewModel);
        Bind(_viewModel);
        CreateFields();
    }
    
    private void Bind(FormSubmitViewModel vm) {
        // Wire View events to ViewModel commands
        _view.OnSubmitClicked += vm.Submit;
        _view.OnCancelClicked += vm.Cancel;
        
        // Subscribe to model events
        _model.OnFormDataChanged += HandleFormDataChanged;
    }
    
    public class Builder {
        private FormSubmitView _view;
        private FormSubmitModel _model;
        
        public Builder WithView(FormSubmitView view) { _view = view; return this; }
        public Builder WithModel(FormSubmitModel model) { _model = model; return this; }
        
        public FormSubmitController Build() {
            return new FormSubmitController(_view, _model);
        }
    }
}
```

**Design Decisions**:
- Private constructor + Builder pattern (consistent with MiniTimeline)
- Initialize via coroutine to ensure UI elements are ready
- Keep field creation logic in Controller (View is passive)

## Data Flow

### Initialization Flow
```
1. User creates Builder
   ↓
2. Builder.WithView(view).WithModel(model).Build()
   ↓
3. Controller constructor → StartCoroutine(Initialize())
   ↓
4. Initialize(): Create ViewModel
   ↓
5. View.InitializeView(viewModel) → Load UXML, setup bindings
   ↓
6. Bind(viewModel) → Wire events
   ↓
7. CreateFields() → Instantiate field components
```

### Submit Flow
```
1. User clicks Submit button
   ↓
2. View emits OnSubmitClicked event
   ↓
3. Controller receives event → calls ViewModel.Submit()
   ↓
4. ViewModel validates form data
   ↓
5. ViewModel updates Model with field values
   ↓
6. Model emits OnFormDataChanged event
   ↓
7. Controller invokes OnFormSubmitted callback
```

### Field Update Flow
```
1. User edits a field (e.g., TextField)
   ↓
2. Field component (IFormFieldUIToolkit) detects change
   ↓
3. Field emits OnValueChanged event
   ↓
4. Controller receives event → updates Model.FieldValues
   ↓
5. Model emits OnFieldValueChanged event
   ↓
6. ViewModel recalculates IsSubmitEnabled
   ↓
7. UI Toolkit data binding updates Submit button enabled state
```

## Design Patterns

### 1. MVVM Pattern
- **Model**: Pure data + events
- **View**: UI representation + UI Toolkit bindings
- **ViewModel**: Presentation logic + bindable properties
- **Controller**: Orchestration + event wiring

### 2. Builder Pattern
```csharp
var controller = new FormSubmitController.Builder()
    .WithView(view)
    .WithModel(model)
    .Build();
```

### 3. Observer Pattern
- Model emits events when data changes
- ViewModel subscribes to Model events
- View emits events for user actions
- Controller subscribes to View events

### 4. Dependency Injection
- Controller receives View and Model via constructor
- ViewModel receives Model via constructor
- No hardcoded singletons (except for backward compatibility facade)

## Backward Compatibility Strategy

Keep the existing `FormSubmitPanelUIToolkit` as a facade that delegates to the new MVVM components:

```csharp
public class FormSubmitPanelUIToolkit : MonoBehaviour {
    private FormSubmitController _controller;
    
    public static FormSubmitPanelUIToolkit Instance { get; }
    
    public void Show(string title, List<FormFieldConfig> fields, 
                     Action<Dictionary<string, object>> onSubmit) {
        // Create Model from parameters
        var model = new FormSubmitModel(title, fields);
        
        // Create View (or reuse existing)
        var view = GetComponent<FormSubmitView>();
        
        // Build Controller
        _controller = new FormSubmitController.Builder()
            .WithView(view)
            .WithModel(model)
            .Build();
        
        // Wire callback
        _controller.OnFormSubmitted += onSubmit;
    }
}
```

This allows existing code to continue using the old API while new code adopts the MVVM pattern.

## Testing Strategy

### Unit Tests (Model & ViewModel)
- Test Model state changes and events
- Test ViewModel property bindings and transformations
- Test ViewModel command execution
- Mock Model for ViewModel tests

### Integration Tests (Controller & View)
- Test Controller initialization flow
- Test event wiring between components
- Test form submission and cancellation
- Use Unity Test Framework with UI Toolkit support

### Backward Compatibility Tests
- Test legacy API still works
- Test facade delegates to MVVM components correctly
- Test existing field types work with new architecture

## File Structure

```
Assets/Scripts/Core/UI/FormSubmit/
├── FormSubmit/                       (existing)
│   ├── FormSubmitPanel.cs            (legacy, keep for compatibility)
│   ├── FormSubmitPanelUIToolkit.cs   (facade, delegates to MVVM)
│   └── Fields/                       (existing field implementations)
└── MVVM/                             (new)
    ├── FormSubmitModel.cs
    ├── FormSubmitViewModel.cs
    ├── FormSubmitView.cs
    ├── FormSubmitController.cs
    ├── README.md
    └── Examples/
        └── FormSubmitMVVMExample.cs
```

## Open Design Questions

### Q1: Should we use ObservableArray for field configs?
**Options**:
- Use `ObservableArray<FormFieldConfig>` (like Timeline tracks)
- Use `List<FormFieldConfig>` + manual event emission

**Recommendation**: Use `List<FormFieldConfig>` for now since field configs are typically set once during initialization, not dynamically modified. Add `ObservableArray` if dynamic field addition/removal becomes a requirement.

### Q2: How should field validation work?
**Options**:
- A) Validation in Model (data layer)
- B) Validation in ViewModel (presentation layer)
- C) Validation in Field components (UI layer)

**Recommendation**: Start with basic type checking in Model, add field-level validation in ViewModel if needed. Keep validation logic testable (not in View).

### Q3: Should we support form state serialization?
**Options**:
- Serialize Model state to JSON for autosave
- Keep in-memory only

**Recommendation**: Out of scope for initial implementation. The current FormSubmit is session-based (no persistence). Add serialization if use cases emerge.

## Performance Considerations

- **Field Creation**: Reuse field instances when possible (object pooling for large forms)
- **Data Binding**: Use one-way binding to minimize update overhead
- **Event Emission**: Batch field value changes if multiple fields update simultaneously
- **UI Updates**: Leverage UI Toolkit's efficient dirty-tracking system

## Accessibility

Maintain existing accessibility features:
- Keyboard navigation (tab order)
- Focus management (FocusFirstField)
- Screen reader support (via UI Toolkit's built-in support)

## Future Enhancements

1. **Two-way Binding**: Add `SettableBindableProperty<T>` for fields that need real-time synchronization
2. **Form Validation Framework**: Add fluent validation API (e.g., `Field("email").Required().Email()`)
3. **Multi-step Forms**: Support wizard-style forms with navigation
4. **Field Groups**: Organize fields into collapsible sections
5. **Dynamic Field Types**: Plugin system for custom field types
