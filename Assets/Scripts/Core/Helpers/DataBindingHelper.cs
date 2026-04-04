using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;

public static class DataBindingHelper {
    static string GetDefaultTargetProperty<T>() where T : VisualElement {
        return typeof(T) == typeof(Label) ? "text" : "value";
    }

    /// <summary>
    /// Core binding method that creates a binding with the specified mode
    /// </summary>
    static void Bind(VisualElement element, string propertyPath, BindingMode bindingMode, string targetProperty) {
        if (element == null) {
            Debug.LogWarning($"Cannot bind to null element for property: {propertyPath}");
            return;
        }

        if (element is not BindableElement bindableElement) {
            Debug.LogWarning($"Element type {element.GetType().Name} does not support binding. It must inherit from BindableElement.");
            return;
        }

        bindableElement.bindingPath = propertyPath;
        var binding = new DataBinding {
            dataSourcePath = new PropertyPath(propertyPath),
            bindingMode = bindingMode
        };

        element.SetBinding(targetProperty, binding);
    }

    /// <summary>
    /// Generic binding method that automatically uses the default target property for the element type
    /// </summary>
    public static void Bind<T>(T element, string propertyPath, BindingMode bindingMode) where T : VisualElement {
        string targetProperty = GetDefaultTargetProperty<T>();
        Bind((VisualElement)element, propertyPath, bindingMode, targetProperty);
    }

    /// <summary>
    /// Generic binding method with PropertyPath (supports nameof for type safety)
    /// </summary>
    public static void Bind<T>(T element, PropertyPath propertyPath, BindingMode bindingMode) where T : VisualElement {
        string targetProperty = GetDefaultTargetProperty<T>();
        Bind((VisualElement)element, propertyPath, bindingMode, targetProperty);
    }

    /// <summary>
    /// Generic binding method with explicit target property override
    /// </summary>
    public static void Bind<T>(T element, string propertyPath, BindingMode bindingMode, string targetProperty) where T : VisualElement {
        Bind((VisualElement)element, propertyPath, bindingMode, targetProperty);
    }

    /// <summary>
    /// Generic binding method with PropertyPath and explicit target property override
    /// </summary>
    public static void Bind<T>(T element, PropertyPath propertyPath, BindingMode bindingMode, string targetProperty) where T : VisualElement {
        if (element == null) {
            Debug.LogWarning($"Cannot bind to null element for property: {propertyPath}");
            return;
        }

        if (element is not BindableElement bindableElement) {
            Debug.LogWarning($"Element type {element.GetType().Name} does not support binding. It must inherit from BindableElement.");
            return;
        }

        bindableElement.bindingPath = propertyPath.ToString();
        var binding = new DataBinding {
            dataSourcePath = propertyPath,
            bindingMode = bindingMode
        };

        element.SetBinding(targetProperty, binding);
    }

    /// <summary>
    /// Creates a one-way binding from data source to UI element (data → UI)
    /// </summary>
    public static void BindOneWay<T>(T element, string propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.ToTarget);
    }

    /// <summary>
    /// Creates a one-way binding from data source to UI element (data → UI) using PropertyPath
    /// Use with nameof for type safety: BindOneWay(label, new PropertyPath(nameof(CharacterData.Health)))
    /// </summary>
    public static void BindOneWay<T>(T element, PropertyPath propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.ToTarget);
    }

    /// <summary>
    /// Creates a one-way binding from UI element to data source (UI → data)
    /// </summary>
    public static void BindToSource<T>(T element, string propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.ToSource);
    }

    /// <summary>
    /// Creates a one-way binding from UI element to data source (UI → data) using PropertyPath
    /// Use with nameof for type safety: BindToSource(field, new PropertyPath(nameof(CharacterData.Name)))
    /// </summary>
    public static void BindToSource<T>(T element, PropertyPath propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.ToSource);
    }

    /// <summary>
    /// Creates a two-way binding between data source and UI element (data ↔ UI)
    /// </summary>
    public static void BindTwoWay<T>(T element, string propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.TwoWay);
    }

    /// <summary>
    /// Creates a two-way binding between data source and UI element (data ↔ UI) using PropertyPath
    /// Use with nameof for type safety: BindTwoWay(slider, new PropertyPath(nameof(CharacterData.Health)))
    /// </summary>
    public static void BindTwoWay<T>(T element, PropertyPath propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.TwoWay);
    }

    /// <summary>
    /// Creates a one-time binding from data source to UI element (data → UI, initial value only)
    /// The UI is updated once when the binding is created, but won't update if the data changes later.
    /// </summary>
    public static void BindToTargetOnce<T>(T element, string propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.ToTargetOnce);
    }

    /// <summary>
    /// Creates a one-time binding from data source to UI element (data → UI, initial value only) using PropertyPath
    /// Use with nameof for type safety: BindToTargetOnce(label, new PropertyPath(nameof(CharacterData.InitialValue)))
    /// </summary>
    public static void BindToTargetOnce<T>(T element, PropertyPath propertyPath) where T : VisualElement {
        Bind(element, propertyPath, BindingMode.ToTargetOnce);
    }
}