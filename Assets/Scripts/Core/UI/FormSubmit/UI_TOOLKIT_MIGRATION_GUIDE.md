# FormSubmitPanel UI Toolkit Migration Guide

## Overview

This document explains how to migrate from the uGUI-based `FormSubmitPanel` to the new UI Toolkit-based `FormSubmitPanelUIToolkit`.

## Key Benefits of UI Toolkit Version

- **Better Performance**: UI Toolkit uses a retained-mode rendering system that's more efficient
- **Modern Architecture**: Built on web-standard technologies (similar to HTML/CSS)
- **Better Styling**: USS (Unity Style Sheets) provide more powerful styling capabilities
- **Improved Responsiveness**: Better support for different screen sizes and resolutions
- **Future-Proof**: UI Toolkit is Unity's modern UI solution going forward

## Migration Steps

### 1. Replace Class References

**Old (uGUI):**
```csharp
using Core.UI.FormSubmit;
var panel = FormSubmitPanelUIToolkit.Instance;
```

**New (UI Toolkit):**
```csharp
using Core.UI.FormSubmit;
var panel = FormSubmitPanelUIToolkit.Instance;
```

### 2. Update Method Calls

The API maintains full compatibility with the original version:

**Old (uGUI):**
```csharp
FormSubmitPanel.Instance.Show(title, fields, onSubmit, onCancel, parentTransform);
```

**New (UI Toolkit):**
```csharp
FormSubmitPanelUIToolkit.Instance.Show(title, fields, onSubmit, onCancel, parentTransform);
// Note: Parent transform parameter is maintained for compatibility
// In UI Toolkit, it's used for validation but doesn't affect hierarchy
```

### 3. Field Definitions Remain the Same

The `FormFieldDefinition` class and all field types remain unchanged:

```csharp
var fields = new List<FormFieldDefinition>
{
    new FormFieldDefinition("name", "Character Name", "text", "DefaultName"),
    new FormFieldDefinition("age", "Age", "number", 25),
    new FormFieldDefinition("isActive", "Active", "toggle", true)
};
```

### 4. Use Helper Utilities

The new version includes helpful utility methods:

```csharp
// Quick text input
FormSubmitUIToolkitUtils.ShowTextInputForm("Enter Name", "name", "Name", 
    defaultValue: "Player", 
    onSubmit: name => Debug.Log($"Name: {name}"));

// Confirmation dialog
FormSubmitUIToolkitUtils.ShowConfirmationDialog("Confirm", "Are you sure?",
    onConfirm: () => Debug.Log("Confirmed"),
    onCancel: () => Debug.Log("Cancelled"));
```

## Supported Field Types

All field types from the uGUI version are supported:

- **text**: Single-line text input (`TextField`)
- **textarea**: Multi-line text input (`TextField` with multiline=true)
- **number**: Numeric input (`FloatField`)
- **select/selectbox**: Dropdown selection (`DropdownField`)
- **toggle/checkbox**: Boolean input (`Toggle`)
- **slider**: Range input (`Slider`)
- **color**: Color picker (`Button` with color cycling)
- **button**: Action button (`Button`)
- **info**: Read-only information display (`TextField` disabled)
- **hidden**: Hidden value storage (invisible `VisualElement`)

## Styling and Theming

### UXML Template

The form structure is defined in `FormSubmitPanel.uxml`:

```xml
<ui:VisualElement name="background-panel" class="form-background">
    <ui:VisualElement name="form-container" class="form-container">
        <!-- Header, content, footer -->
    </ui:VisualElement>
</ui:VisualElement>
```

### USS Styling

Styles are defined in `FormSubmitPanel.uss`:

```css
.form-container {
    width: 400px;
    height: 500px;
    background-color: rgba(51, 51, 51, 0.95);
    border-radius: 8px;
}

.field-input {
    height: 30px;
    background-color: rgba(255, 255, 255, 0.1);
    border-radius: 4px;
}
```

### Custom Styling

You can override styles by:

1. Modifying the USS file
2. Adding custom CSS classes
3. Setting inline styles in code:

```csharp
element.style.backgroundColor = Color.red;
element.AddToClassList("my-custom-class");
```

## Performance Considerations

### UI Toolkit Advantages

- **Batched Rendering**: Multiple UI elements are rendered in fewer draw calls
- **Efficient Updates**: Only changed elements are re-rendered
- **Memory Efficient**: Lower memory overhead compared to GameObject-based UI

### Best Practices

1. **Reuse Forms**: Don't create/destroy forms frequently
2. **Cache References**: Store references to frequently accessed elements
3. **Minimize Callbacks**: Avoid excessive event subscriptions
4. **Use CSS Classes**: Prefer CSS styling over inline styles

## Troubleshooting

### Common Issues

**Form not appearing:**
- Check that UIDocument component is present
- Verify UXML and USS resources are loaded
- Ensure PanelSettings are configured correctly

**Styling issues:**
- Check USS file is applied to the root element
- Verify CSS class names match between UXML and USS
- Use Unity UI Debugger to inspect element hierarchy

**Field validation not working:**
- Ensure field definitions have correct validation rules
- Check that required fields are marked properly
- Verify min/max values for number fields

### Debug Tools

Use Unity's UI Toolkit Debugger:
1. Open Window > UI Toolkit > Debugger
2. Select your UI Document
3. Inspect element hierarchy and styles
4. Test style modifications in real-time

## Code Examples

### Basic Form Creation

```csharp
public void ShowExampleForm()
{
    var fields = new List<FormFieldDefinition>
    {
        new FormFieldDefinition("username", "Username", "text", "")
        {
            required = true,
            placeholder = "Enter username"
        },
        new FormFieldDefinition("email", "Email", "text", "")
        {
            required = true,
            placeholder = "user@example.com"
        },
        new FormFieldDefinition("age", "Age", "number", 18)
        {
            options = new Dictionary<string, object>
            {
                { "min", 0f },
                { "max", 120f }
            }
        }
    };

    FormSubmitPanelUIToolkit.Instance.Show(
        "User Registration",
        fields,
        OnFormSubmitted,
        OnFormCancelled
    );
}

private void OnFormSubmitted(Dictionary<string, object> data)
{
    string username = data["username"].ToString();
    string email = data["email"].ToString();
    float age = float.Parse(data["age"].ToString());
    
    // Process form data
    Debug.Log($"User: {username}, Email: {email}, Age: {age}");
}

private void OnFormCancelled()
{
    Debug.Log("Form cancelled");
}
```

### Custom Field Validation

```csharp
// Custom validation in field definition
var emailField = new FormFieldDefinition("email", "Email", "text", "")
{
    required = true,
    options = new Dictionary<string, object>
    {
        { "pattern", @"^[^@\s]+@[^@\s]+\.[^@\s]+$" }, // Email regex
        { "validationMessage", "Please enter a valid email address" }
    }
};
```

## Migration Checklist

- [ ] Replace `FormSubmitPanel` references with `FormSubmitPanelUIToolkit`
- [ ] Update method calls (remove parent transform parameter)
- [ ] Test all form field types
- [ ] Verify custom styling works correctly
- [ ] Update any custom form field implementations
- [ ] Test form validation
- [ ] Check performance in target platforms
- [ ] Update documentation and examples

## Resources

- [Unity UI Toolkit Documentation](https://docs.unity3d.com/Manual/UIElements.html)
- [UI Toolkit Styling Guide](https://docs.unity3d.com/Manual/UIE-USS.html)
- [Visual Element API Reference](https://docs.unity3d.com/ScriptReference/UIElements.VisualElement.html)

## Support

For issues or questions about the UI Toolkit migration:
1. Check the Unity Console for error messages
2. Use the UI Toolkit Debugger to inspect element hierarchy
3. Refer to the example scripts in the `Examples` folder
4. Review the Unity UI Toolkit documentation