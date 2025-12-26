# Proposal: Add Form Submit MVVM Architecture

**Change ID:** `add-form-submit-mvvm`  
**Date:** 2025-12-26  
**Status:** Draft  

## Overview

This proposal introduces a new MVVM (Model-View-ViewModel-Controller) architecture for the FormSubmit UI system, following the established patterns used in the MiniTimeline MVVM implementation. The current `FormSubmitPanelUIToolkit` is a monolithic singleton that tightly couples UI logic, form state, and submission handling, making it difficult to test, maintain, and extend.

## Motivation

### Current Problems
1. **Tight Coupling**: `FormSubmitPanelUIToolkit` combines UI creation, state management, and business logic in a 747-line class
2. **No Separation of Concerns**: Form data, UI elements, and submission callbacks are intermingled
3. **Limited Testability**: Cannot unit test form logic without Unity Editor
4. **Hard to Extend**: Adding new field types or validation logic requires modifying the monolithic class
5. **Inconsistent Architecture**: Doesn't follow the MVVM pattern established in the project (see `MiniTimeline.UI.MVVM`)

### Benefits of MVVM
1. **Testability**: ViewModel and Model can be unit tested without Unity dependencies
2. **Maintainability**: Clear separation between UI (View), presentation logic (ViewModel), and data (Model)
3. **Reusability**: ViewModels can be reused across different views (e.g., mobile vs desktop layouts)
4. **Consistency**: Aligns with established project patterns (Timeline MVVM, Inventory MVVM)
5. **Data Binding**: Leverages Unity UI Toolkit's reactive data binding system with `BindableProperty<T>`

## Goals

1. Create a modular MVVM architecture for FormSubmit while preserving the existing API surface
2. Separate concerns into Model (form field data), ViewModel (UI-bindable properties), View (UI Toolkit elements), and Controller (orchestration)
3. Enable reactive UI updates through `BindableProperty<T>` and UI Toolkit data binding
4. Support all existing field types (text, number, select, toggle, slider, color, etc.)
5. Maintain backward compatibility with existing code that uses `FormSubmitPanelUIToolkit.Instance.Show(...)`

## Non-Goals

1. Changing the existing field type system or adding new field types
2. Modifying the UXML/USS templates for FormSubmit
3. Removing the legacy `FormSubmitPanelUIToolkit` (keep for backward compatibility)
4. Adding form validation beyond what currently exists

## Scope

### In Scope
- Create `FormSubmitModel` (holds form field configurations and values)
- Create `FormSubmitViewModel` (exposes bindable properties for UI)
- Create `FormSubmitView` (UI Toolkit view with UXML/USS loading)
- Create `FormSubmitController` (orchestrates Model-View-ViewModel interactions)
- Create `FormSubmitController.Builder` for fluent initialization
- Add unit tests for ViewModel and Model
- Add integration tests for Controller-View interaction

### Out of Scope
- Refactoring field type implementations (`IFormFieldUIToolkit` and subclasses)
- Adding new field types or validation rules
- Performance optimizations for large forms
- Accessibility features (ARIA labels, keyboard navigation improvements)

## Alternatives Considered

### Alternative 1: Refactor in-place
**Approach**: Break up `FormSubmitPanelUIToolkit` into smaller classes without introducing MVVM  
**Pros**: Simpler, less code churn  
**Cons**: Doesn't address testability, still tightly coupled, inconsistent with project patterns  
**Decision**: Rejected - doesn't solve the fundamental architecture problems

### Alternative 2: Use pure MonoBehaviour architecture
**Approach**: Create separate MonoBehaviour components for each concern  
**Pros**: Familiar Unity pattern, easy to understand  
**Cons**: Harder to test, doesn't leverage UI Toolkit data binding, inconsistent with MiniTimeline MVVM  
**Decision**: Rejected - project has standardized on MVVM for complex UI

### Alternative 3: Implement MVVM with full two-way binding
**Approach**: Use `SettableBindableProperty<T>` for all form fields to enable two-way binding  
**Pros**: More reactive, less manual synchronization  
**Cons**: Overkill for form submission pattern (one-time data capture), increased complexity  
**Decision**: Deferred - start with one-way binding, add two-way if needed

## Dependencies

- Existing `BindableProperty<T>` and `SettableBindableProperty<T>` helper classes
- Existing field type implementations (`IFormFieldUIToolkit`, `FormFieldConfig`)
- UI Toolkit data binding system (Unity 6000.2.6f2)
- Existing UXML/USS assets for FormSubmit panel

## Risks

1. **Breaking Changes**: New API surface might conflict with existing usage
   - *Mitigation*: Keep legacy `FormSubmitPanelUIToolkit` as a facade that delegates to new MVVM components
   
2. **Learning Curve**: Team needs to understand MVVM pattern
   - *Mitigation*: Comprehensive documentation, follow established MiniTimeline MVVM conventions
   
3. **Increased Complexity**: More classes and files to maintain
   - *Mitigation*: Clear naming conventions, thorough documentation, keep modules focused

## Success Criteria

1. All existing FormSubmit use cases work without code changes (backward compatibility)
2. ViewModel can be unit tested without Unity Editor
3. Form data can be bound reactively to UI Toolkit elements
4. New MVVM API follows MiniTimeline conventions (Controller.Builder pattern, BindableProperty usage)
5. Code coverage ≥ 80% for Model and ViewModel classes

## Related Changes

- **Timeline MVVM** (`modularize-timeline-ui-mvvm`): Established the MVVM pattern for the project
- **Inventory MVVM**: Original MVVM implementation referenced in project.md

## Timeline

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| Phase 1: Architecture | 1 day | Model, ViewModel, View, Controller scaffolding |
| Phase 2: Field Integration | 1 day | Wire existing field types into MVVM structure |
| Phase 3: Controller & Builder | 1 day | Controller with Builder pattern, initialization flow |
| Phase 4: Testing | 1 day | Unit tests, integration tests, backward compatibility tests |
| Phase 5: Documentation | 0.5 days | README, usage examples, migration guide |

**Total Estimated Time**: 4.5 days

## Open Questions

1. Should we deprecate the legacy `FormSubmitPanelUIToolkit` or keep it indefinitely?
   - *Recommendation*: Keep for 2-3 releases, then deprecate with warnings
   
2. Do we need two-way binding for form fields?
   - *Recommendation*: Start with one-way (Model → View), add two-way if use cases emerge
   
3. Should FormSubmit support nested forms or multi-step wizards?
   - *Recommendation*: Out of scope for this change, consider in future iteration

## Next Steps

1. Review and approve this proposal
2. Create detailed spec deltas in `specs/form-ui-mvvm/spec.md`
3. Break down implementation into atomic tasks in `tasks.md`
4. Validate proposal structure with `openspec validate add-form-submit-mvvm --strict`
5. Begin implementation after approval
