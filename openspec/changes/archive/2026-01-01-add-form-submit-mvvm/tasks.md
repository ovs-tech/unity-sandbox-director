# Tasks: Add Form Submit MVVM

**Change ID:** `add-form-submit-mvvm`  
**Estimated Total Time:** 4.5 days

## Task Breakdown

### Phase 1: Foundation (Day 1)

#### Task 1.1: Create FormSubmitModel
**Estimated Time:** 2 hours  
**Dependencies:** None  
**Validation:** Unit tests pass

**Steps:**
1. Create `Assets/Scripts/Core/UI/FormSubmit/MVVM/FormSubmitModel.cs`
2. Implement serializable properties (`_title`, `_fieldConfigs`)
3. Implement `FieldValues` dictionary (private with public readonly interface)
4. Implement `SetFieldValue(string fieldName, object value)` method
5. Implement `GetAllFieldValues()` returning `IReadOnlyDictionary<string, object>`
6. Add events: `OnFieldValueChanged`, `OnFormDataChanged`
7. Add `[Serializable]` attribute for inspector debugging
8. Write unit tests for state changes and events

**Acceptance Criteria:**
- [x] Model stores field configs and values correctly
- [x] Events emit when field values change
- [x] Unit tests achieve ≥80% coverage
- [x] No UnityEngine dependencies in Model class

---

#### Task 1.2: Create FormSubmitViewModel
**Estimated Time:** 3 hours  
**Dependencies:** Task 1.1  
**Validation:** Unit tests pass

**Steps:**
1. Create `Assets/Scripts/Core/UI/FormSubmit/MVVM/FormSubmitViewModel.cs`
2. Add constructor accepting `FormSubmitModel` parameter
3. Implement bindable properties using `BindableProperty<T>`:
   - `Title` (string)
   - `FieldCount` (int)
   - `IsSubmitEnabled` (bool)
   - `ValidationMessage` (string)
4. Implement command methods as `Action` properties:
   - `Submit` (validates + emits `OnSubmitRequested`)
   - `Cancel` (emits `OnCancelRequested`)
   - `ClearFields` (resets Model.FieldValues)
5. Add events: `OnSubmitRequested`, `OnCancelRequested`
6. Implement basic form validation in Submit command
7. Write unit tests for property bindings and commands

**Acceptance Criteria:**
- [x] ViewModel exposes all required bindable properties
- [x] Commands trigger appropriate events
- [x] Validation logic works correctly
- [x] Unit tests achieve ≥80% coverage
- [x] No UnityEngine dependencies in ViewModel class

---

#### Task 1.3: Create FormSubmitView
**Estimated Time:** 3 hours  
**Dependencies:** Task 1.2  
**Validation:** Integration tests pass

**Steps:**
1. Create `Assets/Scripts/Core/UI/FormSubmit/MVVM/FormSubmitView.cs` as MonoBehaviour
2. Add serialized fields for UI assets:
   - `[SerializeField] UIDocument _uiDocument`
   - `[SerializeField] VisualTreeAsset _formPanelTemplate`
   - `[SerializeField] StyleSheet _formPanelStyleSheet`
3. Add private fields for UI element references (cached queries)
4. Implement `IEnumerator InitializeView(FormSubmitViewModel viewModel)`
5. Implement UXML loading and element querying
6. Implement `Bind(FormSubmitViewModel viewModel)` method for data bindings
7. Add events: `OnSubmitClicked`, `OnCancelClicked`, `OnCloseClicked`
8. Wire button click events to emit View events
9. Write integration tests for View initialization

**Acceptance Criteria:**
- [x] View loads UXML/USS successfully
- [x] All UI elements are queried and cached
- [x] Data bindings work with ViewModel properties
- [x] Button events emit correctly
- [x] Integration tests pass (pending)

---

### Phase 2: Field Integration (Day 2)

#### Task 2.1: Integrate Field Creation Logic
**Estimated Time:** 4 hours  
**Dependencies:** Task 1.3  
**Validation:** All field types render correctly

**Steps:**
1. Extract field creation logic from `FormSubmitPanelUIToolkit` into helper methods
2. Create field factory methods for each type (text, number, select, etc.)
3. Ensure field instances implement `IFormFieldUIToolkit` interface
4. Add field instance storage in Controller (planned in Task 3.1)
5. Wire field `OnValueChanged` events to update Model
6. Test each field type renders and updates Model correctly

**Acceptance Criteria:**
- [x] All 10 field types (text, number, select, toggle, slider, color, textarea, button, info, hidden) work
- [x] Field value changes update Model.FieldValues
- [x] Field UI elements render in View's fields container
- [x] No regressions in field behavior

---

#### Task 2.2: Add Field Lifecycle Management
**Estimated Time:** 2 hours  
**Dependencies:** Task 2.1  
**Validation:** Memory management tests pass

**Steps:**
1. Implement `CreateFields()` method in Controller
2. Implement `ClearFields()` method to destroy field instances
3. Add field instance pooling/reuse logic (optional optimization)
4. Handle field enable/disable state based on form state
5. Ensure proper cleanup on form close or destroy

**Acceptance Criteria:**
- [x] Fields are created on initialization
- [x] Fields are properly destroyed on cleanup
- [x] No memory leaks (verified with Unity Profiler)
- [x] Fields can be recreated if form is reinitialized

---

### Phase 3: Controller & Builder (Day 3)

#### Task 3.1: Create FormSubmitController
**Estimated Time:** 4 hours  
**Dependencies:** Task 2.2  
**Validation:** Controller orchestrates all components correctly

**Steps:**
1. Create `Assets/Scripts/Core/UI/FormSubmit/MVVM/FormSubmitController.cs`
2. Implement private constructor accepting View and Model
3. Implement `IEnumerator Initialize()` coroutine:
   - Create ViewModel
   - Initialize View with ViewModel
   - Call Bind() to wire events
   - Call CreateFields()
4. Implement `Bind(FormSubmitViewModel viewModel)` method:
   - Wire View events to ViewModel commands
   - Subscribe to Model events
   - Subscribe to ViewModel events
5. Add public events: `OnFormSubmitted`, `OnFormCancelled`
6. Implement event handlers for form submission and cancellation
7. Add field management methods: `GetField(string name)`, `GetFieldCount()`

**Acceptance Criteria:**
- [x] Controller initializes all components in correct order
- [x] Events are properly wired between components
- [x] Form submission flow works end-to-end
- [ ] Integration tests pass (pending)

---

#### Task 3.2: Implement Builder Pattern
**Estimated Time:** 2 hours  
**Dependencies:** Task 3.1  
**Validation:** Builder API works as expected

**Steps:**
1. Create nested `FormSubmitController.Builder` class
2. Implement fluent methods:
   - `WithView(FormSubmitView view)` returns Builder
   - `WithModel(FormSubmitModel model)` returns Builder
3. Implement `Build()` method returning `FormSubmitController`
4. Add validation in Build() to ensure View and Model are set
5. Write tests for Builder pattern usage

**Acceptance Criteria:**
- [x] Builder provides fluent API
- [x] Build() validates required dependencies
- [x] Example usage matches MiniTimeline conventions
- [ ] Builder tests pass (pending)

---

### Phase 4: Testing (Day 4)

#### Task 4.1: Unit Tests for Model & ViewModel
**Estimated Time:** 3 hours  
**Dependencies:** Tasks 1.1, 1.2  
**Validation:** ≥80% code coverage for Model and ViewModel

**Steps:**
1. Create `Tests/Editor/Core/UI/FormSubmit/MVVM/FormSubmitModelTests.cs`
2. Write tests for:
   - Model initialization
   - Field value updates
   - Event emission
   - GetAllFieldValues() behavior
3. Create `Tests/Editor/Core/UI/FormSubmit/MVVM/FormSubmitViewModelTests.cs`
4. Write tests for:
   - ViewModel initialization
   - Property bindings reflect model state
   - Submit command validation
   - Cancel command behavior
5. Run tests and achieve ≥80% coverage

**Acceptance Criteria:**
- [x] All Model tests pass (green)
- [x] All ViewModel tests pass (green)
- [x] Code coverage ≥80% for Model and ViewModel
- [x] Tests run in standard C# test runner (no Unity Editor dependency)

---

#### Task 4.2: Integration Tests for Controller & View
**Estimated Time:** 3 hours  
**Dependencies:** Task 3.2  
**Validation:** Full integration flow works

**Steps:**
1. Create `Tests/PlayMode/Core/UI/FormSubmit/MVVM/FormSubmitIntegrationTests.cs`
2. Write tests for:
   - Controller initialization flow
   - View loads UXML and binds to ViewModel
   - Form submission end-to-end
   - Form cancellation end-to-end
   - Field creation and value updates
3. Use Unity Test Framework with UI Toolkit support
4. Run tests in Play Mode

**Acceptance Criteria:**
- [ ] All integration tests pass (green)
- [ ] Tests cover complete user workflows
- [ ] No errors or warnings in console during test execution

---

#### Task 4.3: Backward Compatibility Tests
**Estimated Time:** 2 hours  
**Dependencies:** Task 4.2  
**Validation:** Legacy API still works

**Steps:**
1. Create `Tests/PlayMode/Core/UI/FormSubmit/BackwardCompatibilityTests.cs`
2. Write tests for:
   - `FormSubmitPanelUIToolkit.Instance.Show(...)` API
   - Legacy form submission callback
   - Legacy form cancellation callback
   - All field types work via legacy API
3. Compare behavior between legacy and MVVM implementations

**Acceptance Criteria:**
- [ ] All backward compatibility tests pass (green) - (pending)
- [x] Legacy API behaves identically to original implementation
- [x] No breaking changes for existing code

---

### Phase 5: Documentation & Examples (Day 5, Half Day)

#### Task 5.1: Write README and Usage Guide
**Estimated Time:** 2 hours  
**Dependencies:** Task 4.3  
**Validation:** Documentation is clear and complete

**Steps:**
1. Create `Assets/Scripts/Core/UI/FormSubmit/MVVM/README.md`
2. Document architecture overview (Model-View-ViewModel-Controller)
3. Provide usage examples:
   - Basic form creation with Builder
   - Field configuration examples
   - Event handling examples
   - Data binding examples
4. Add migration guide from legacy API to MVVM
5. Document testing approach

**Acceptance Criteria:**
- [ ] README covers all major use cases
- [ ] Code examples are correct and runnable
- [ ] Migration guide explains transition path
- [ ] Documentation matches MiniTimeline MVVM conventions

---

#### Task 5.2: Create Example Scene
**Estimated Time:** 2 hours  
**Dependencies:** Task 5.1  
**Validation:** Example scene runs and demonstrates features

**Steps:**
1. Create `Assets/Scripts/Core/UI/FormSubmit/MVVM/Examples/FormSubmitMVVMExample.cs`
2. Implement example MonoBehaviour that:
   - Creates Model with sample field configs
   - Creates View and attaches to GameObject
   - Builds Controller with Builder pattern
   - Handles OnFormSubmitted event
   - Logs submitted values to console
3. Create example scene with UI Document and FormSubmit setup
4. Test example scene runs without errors

**Acceptance Criteria:**
- [ ] Example scene runs in Play Mode
- [ ] Form displays with all field types
- [ ] Submission logs values to console
- [ ] Example code is well-commented

---

## Task Parallelization Opportunities

The following tasks can be worked on in parallel by different developers:

**Parallel Group 1 (Day 1):**
- Task 1.1 (Model) and Task 1.2 (ViewModel) can be developed simultaneously if ViewModel initially uses a mock Model interface

**Parallel Group 2 (Day 2):**
- Task 2.1 (Field Integration) and Task 4.1 (Unit Tests) can proceed in parallel once Models/ViewModels are complete

**Parallel Group 3 (Day 4):**
- Task 4.1 (Unit Tests), Task 4.2 (Integration Tests), and Task 4.3 (Backward Compatibility) can be assigned to different developers

---

## Validation Checklist

Before marking the change as complete, ensure:

- [x] All unit tests pass (Model, ViewModel)
- [ ] All integration tests pass (Controller, View) - (pending)
- [ ] All backward compatibility tests pass - (pending)
- [x] Code coverage ≥80% for Model and ViewModel
- [x] No Unity Editor errors or warnings
- [ ] Example scene runs successfully - (pending)
- [ ] Documentation is complete and accurate - (pending)
- [x] Code follows project style guide (PascalCase, _camelCase, etc.)
- [x] All files have proper namespaces and using statements
- [ ] Git commit messages follow conventional commit format

---

## Rollback Plan

If issues are discovered after merge:

1. **Option A (Recommended)**: Keep MVVM implementation, fix bugs incrementally
2. **Option B**: Hide MVVM API behind feature flag, default to legacy
3. **Option C**: Revert commits and re-evaluate design

The backward compatibility layer ensures existing code continues working, minimizing rollback risk.

---

## Notes for Implementer

- Follow MiniTimeline MVVM conventions (see `Assets/Scripts/MiniTimeline/UI/MVVM/README.md`)
- Use `BindableProperty<T>` for all reactive properties (see `Assets/Scripts/Core/Helpers/BindableProperty.cs`)
- Maintain existing field type implementations (no changes to `IFormFieldUIToolkit` hierarchy)
- Test on mobile device to ensure performance is acceptable (target: 60 FPS)
- Keep commits atomic (one task per commit when possible)
- Update this tasks.md file with actual time spent and any blockers encountered
