using System;
using Unity.Properties;

/// <summary>
/// Represents a bindable property with getter.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class BindableProperty<T> {
    readonly Func<T> getter;
    // Add setter to make a 2 way binding Action<T> setter;

    BindableProperty(Func<T> getter) {
        this.getter = getter;
    }
    
    [CreateProperty] // Allows binding to the UI in UI Toolkit
    public T Value => getter();
    
    public static BindableProperty<T> Bind(Func<T> getter) => new BindableProperty<T>(getter);
}

/// <summary>
/// Represents a settable bindable property with both getter and setter.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class SettableBindableProperty<T> {
    private T _value;
    
    public SettableBindableProperty(T initialValue = default) {
        _value = initialValue;
    }
    
    [CreateProperty] // Allows binding to the UI in UI Toolkit
    public T Value {
        get => _value;
        set => _value = value;
    }
}