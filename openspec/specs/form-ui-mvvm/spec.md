# form-ui-mvvm Specification

## Purpose
TBD - created by archiving change add-form-submit-mvvm. Update Purpose after archive.
## Requirements
### Requirement: Form Submit Model

The system SHALL provide a `FormSubmitModel` class that holds form field configurations and captured values.

#### Scenario: Initialize model with field configurations
**Given** a user wants to create a form with specific fields  
**When** they instantiate `FormSubmitModel` with title and field configs  
**Then** the model SHALL store the title and field configurations  
**And** the model SHALL initialize an empty field values dictionary  
**And** the model SHALL be ready to accept field value updates

```csharp
var model = new FormSubmitModel("User Profile", fieldConfigs);
Assert.IsNotNull(model.FieldConfigs);
Assert.AreEqual("User Profile", model.Title);
Assert.AreEqual(0, model.FieldValues.Count);
```

#### Scenario: Update field value in model
**Given** a model with initialized field configurations  
**When** a field value is updated via `SetFieldValue(fieldName, value)`  
**Then** the model SHALL store the value in the field values dictionary  
**And** the model SHALL emit `OnFieldValueChanged` event with field name and value  

```csharp
model.OnFieldValueChanged += (fieldName, value) => {
    Assert.AreEqual("username", fieldName);
    Assert.AreEqual("john_doe", value);
};
model.SetFieldValue("username", "john_doe");
```

#### Scenario: Get all field values
**Given** a model with multiple updated field values  
**When** the user requests all field values via `GetAllFieldValues()`  
**Then** the model SHALL return a read-only dictionary of all field name-value pairs  
**And** external code SHALL NOT be able to modify the returned dictionary directly

```csharp
var values = model.GetAllFieldValues();
Assert.IsInstanceOf<IReadOnlyDictionary<string, object>>(values);
Assert.AreEqual(3, values.Count);
```

---

### Requirement: Form Submit ViewModel

The system SHALL provide a `FormSubmitViewModel` class that exposes UI-bindable properties and command methods.

#### Scenario: Bind ViewModel to Model
**Given** a `FormSubmitModel` with initialized data  
**When** a `FormSubmitViewModel` is constructed with the model  
**Then** the ViewModel SHALL expose `BindableProperty<T>` instances for reactive UI binding  
**And** the ViewModel SHALL NOT have direct UnityEngine dependencies (pure C#)

```csharp
var viewModel = new FormSubmitViewModel(model);
Assert.IsNotNull(viewModel.Title);
Assert.IsNotNull(viewModel.FieldCount);
Assert.IsInstanceOf<BindableProperty<string>>(viewModel.Title);
```

#### Scenario: ViewModel reflects model state changes
**Given** a ViewModel bound to a Model  
**When** the Model's field values are updated  
**Then** the ViewModel's bindable properties SHALL reflect the changes  
**And** UI elements bound to those properties SHALL update automatically via UI Toolkit data binding

```csharp
var titleBinding = viewModel.Title;
model.Title = "Updated Title";
Assert.AreEqual("Updated Title", titleBinding.Value);
```

#### Scenario: Execute Submit command
**Given** a ViewModel with valid form data  
**When** the `Submit` command is invoked  
**Then** the ViewModel SHALL validate form data  
**And** if validation passes, it SHALL emit `OnSubmitRequested` event with field values  
**And** if validation fails, it SHALL update `ValidationMessage` property with error details

```csharp
bool submitCalled = false;
viewModel.OnSubmitRequested += (values) => submitCalled = true;
viewModel.Submit();
Assert.IsTrue(submitCalled);
```

#### Scenario: Execute Cancel command
**Given** a ViewModel with any form state  
**When** the `Cancel` command is invoked  
**Then** the ViewModel SHALL emit `OnCancelRequested` event  
**And** the ViewModel SHALL NOT modify the Model state

```csharp
bool cancelCalled = false;
viewModel.OnCancelRequested += () => cancelCalled = true;
viewModel.Cancel();
Assert.IsTrue(cancelCalled);
```

---

### Requirement: Form Submit View

The system SHALL provide a `FormSubmitView` class that manages UI Toolkit elements and data bindings.

#### Scenario: Initialize View with UXML template
**Given** a `FormSubmitView` MonoBehaviour with assigned UXML template  
**When** `InitializeView(viewModel)` is called  
**Then** the View SHALL load the UXML template into the UI Document  
**And** the View SHALL query and cache references to UI elements (buttons, labels, containers)  
**And** the View SHALL return an `IEnumerator` for coroutine-based async initialization

```csharp
yield return view.InitializeView(viewModel);
Assert.IsNotNull(view.SubmitButton);
Assert.IsNotNull(view.FieldsContainer);
```

#### Scenario: Setup data bindings to ViewModel
**Given** a View with initialized UI elements and a ViewModel  
**When** `Bind(viewModel)` is called  
**Then** the View SHALL bind UI elements to ViewModel's `BindableProperty<T>` instances  
**And** UI elements SHALL update automatically when ViewModel properties change

```csharp
view.Bind(viewModel);
viewModel.Title = "New Title";
// UI Toolkit data binding automatically updates titleLabel.text
Assert.AreEqual("New Title", view.TitleLabel.text);
```

#### Scenario: Emit UI events
**Given** a View with initialized buttons  
**When** a user clicks the Submit button  
**Then** the View SHALL emit `OnSubmitClicked` event  
**And** the View SHALL NOT directly invoke business logic (delegate to Controller)

```csharp
bool submitEventEmitted = false;
view.OnSubmitClicked += () => submitEventEmitted = true;
view.SubmitButton.Click(); // Simulate click
Assert.IsTrue(submitEventEmitted);
```

---

### Requirement: Form Submit Controller

The system SHALL provide a `FormSubmitController` class that orchestrates Model-View-ViewModel interactions.

#### Scenario: Initialize Controller with Builder pattern
**Given** a `FormSubmitView` and `FormSubmitModel`  
**When** a Controller is built using the Builder pattern  
**Then** the Controller SHALL initialize the ViewModel  
**And** the Controller SHALL initialize the View with the ViewModel  
**And** the Controller SHALL wire events between Model, ViewModel, and View

```csharp
var controller = new FormSubmitController.Builder()
    .WithView(view)
    .WithModel(model)
    .Build();
    
Assert.IsNotNull(controller);
```

#### Scenario: Handle form submission
**Given** a Controller with initialized components  
**When** the View emits `OnSubmitClicked` event  
**Then** the Controller SHALL invoke ViewModel's `Submit` command  
**And** the Controller SHALL wait for ViewModel's `OnSubmitRequested` event  
**And** the Controller SHALL emit `OnFormSubmitted` event with field values for external consumption

```csharp
Dictionary<string, object> submittedValues = null;
controller.OnFormSubmitted += (values) => submittedValues = values;

view.SubmitButton.Click();

Assert.IsNotNull(submittedValues);
Assert.AreEqual("john_doe", submittedValues["username"]);
```

#### Scenario: Manage field lifecycle
**Given** a Controller with field configurations in the Model  
**When** the Controller initializes  
**Then** the Controller SHALL create field instances (`IFormFieldUIToolkit`) for each field config  
**And** the Controller SHALL add field UI elements to the View's fields container  
**And** the Controller SHALL wire field `OnValueChanged` events to update the Model

```csharp
// After controller initialization
Assert.AreEqual(3, controller.GetFieldCount());
Assert.AreEqual(3, view.FieldsContainer.childCount);
```

---

### Requirement: Backward Compatibility

The system SHALL maintain backward compatibility with existing `FormSubmitPanelUIToolkit` API.

#### Scenario: Legacy API delegates to MVVM components
**Given** existing code using `FormSubmitPanelUIToolkit.Instance.Show(...)`  
**When** the legacy API is invoked  
**Then** the legacy class SHALL create a Model from the provided parameters  
**And** the legacy class SHALL create or reuse a View instance  
**And** the legacy class SHALL build a Controller internally  
**And** the form SHALL function identically to the original implementation

```csharp
// Legacy code continues to work
FormSubmitPanelUIToolkit.Instance.Show(
    "Edit Profile",
    fieldConfigs,
    (values) => Debug.Log("Submitted: " + values)
);
```

#### Scenario: Transition to MVVM API
**Given** a developer wants to adopt the new MVVM architecture  
**When** they create Model, View, and Controller explicitly  
**Then** they SHALL have full control over initialization and lifecycle  
**And** they SHALL be able to unit test ViewModel and Model independently  
**And** they SHALL follow the same patterns as MiniTimeline MVVM

```csharp
// New MVVM approach
var model = new FormSubmitModel("Profile", fieldConfigs);
var view = viewGameObject.AddComponent<FormSubmitView>();

var controller = new FormSubmitController.Builder()
    .WithView(view)
    .WithModel(model)
    .Build();

controller.OnFormSubmitted += HandleSubmit;
```

---

### Requirement: Data Binding Integration

The system SHALL integrate with Unity UI Toolkit's data binding system using `BindableProperty<T>`.

#### Scenario: Bind ViewModel property to UI element
**Given** a ViewModel with `BindableProperty<string> Title`  
**When** the View binds a Label to the Title property  
**Then** the Label's text SHALL automatically update when the Model's title changes  
**And** no manual UI update code SHALL be required

```csharp
titleLabel.dataSource = viewModel.Title;
titleLabel.SetBinding(
    nameof(Label.text),
    new DataBinding {
        dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
        bindingMode = BindingMode.ToTarget
    }
);

model.Title = "Changed Title";
// titleLabel.text automatically updates to "Changed Title"
```

#### Scenario: Bind computed property
**Given** a ViewModel with a computed property (e.g., `FieldCount` based on `FieldConfigs.Count`)  
**When** the Model's field configurations change  
**Then** the ViewModel's computed property SHALL reflect the updated value  
**And** bound UI elements SHALL update automatically

```csharp
public readonly BindableProperty<int> FieldCount;

FieldCount = BindableProperty<int>.Bind(() => _model.FieldConfigs.Count);
```

---

### Requirement: Testability

The system SHALL support unit testing of Model and ViewModel without Unity Editor dependencies.

#### Scenario: Unit test Model state changes
**Given** a `FormSubmitModel` instance in a unit test  
**When** field values are updated  
**Then** the test SHALL verify events are emitted correctly  
**And** the test SHALL run in a standard C# test runner (NUnit) without Unity Editor

```csharp
[Test]
public void Model_SetFieldValue_EmitsEvent() {
    var model = new FormSubmitModel("Test", new List<FormFieldConfig>());
    string capturedFieldName = null;
    
    model.OnFieldValueChanged += (name, value) => capturedFieldName = name;
    model.SetFieldValue("email", "test@example.com");
    
    Assert.AreEqual("email", capturedFieldName);
}
```

#### Scenario: Unit test ViewModel commands
**Given** a `FormSubmitViewModel` with a mocked Model  
**When** the Submit command is invoked  
**Then** the test SHALL verify the command validates data and emits events  
**And** the test SHALL NOT require Unity MonoBehaviour or UI Toolkit components

```csharp
[Test]
public void ViewModel_Submit_EmitsOnSubmitRequested() {
    var mockModel = new FormSubmitModel("Test", new List<FormFieldConfig>());
    var viewModel = new FormSubmitViewModel(mockModel);
    
    bool submitRequested = false;
    viewModel.OnSubmitRequested += (values) => submitRequested = true;
    
    viewModel.Submit();
    
    Assert.IsTrue(submitRequested);
}
```

---

### Requirement: Field Type Support

The system SHALL support all existing field types from the legacy FormSubmit implementation.

#### Scenario: Create text field in MVVM architecture
**Given** a field configuration with type "text"  
**When** the Controller creates fields from configurations  
**Then** a TextField component SHALL be instantiated  
**And** the field SHALL update the Model when its value changes  
**And** the field SHALL use the existing `IFormFieldUIToolkit` implementations

```csharp
var config = new FormFieldConfig {
    name = "username",
    label = "Username",
    type = "text"
};

// Controller handles field creation
controller.CreateFields();

// Field instance exists and is wired to Model
var field = controller.GetField("username");
Assert.IsInstanceOf<IFormFieldUIToolkit>(field);
```

#### Scenario: Support all legacy field types
**Given** field configurations for all supported types (text, number, select, toggle, slider, color, textarea, button, info, hidden)  
**When** the Controller creates fields  
**Then** each field type SHALL be instantiated correctly  
**And** each field SHALL maintain its existing behavior from the legacy implementation

```csharp
var fieldTypes = new[] { "text", "number", "select", "toggle", "slider", "color", "textarea", "button", "info", "hidden" };

foreach (var type in fieldTypes) {
    var config = new FormFieldConfig { type = type, name = $"{type}_field" };
    var field = controller.CreateField(config);
    Assert.IsNotNull(field);
}
```

---

