using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class IconToggle : VisualElement
{
    private const string DefaultIcon = "\uf00c";

    private Label _iconLabel;

    public event Action<bool> onValueChanged;

    private string _icon = DefaultIcon;
    [UxmlAttribute]
    public string icon
    {
        get => _icon;
        set
        {
            _icon = string.IsNullOrEmpty(value) ? DefaultIcon : value;
            UpdateVisual();
        }
    }

    private bool _value;
    [UxmlAttribute]
    public bool value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            UpdateVisual();
            onValueChanged?.Invoke(_value);
        }
    }

    public IconToggle()
    {
        // Load default stylesheet
        var styleSheet = Resources.Load<StyleSheet>("IconToggle");
        if (styleSheet == null)
        {
            // Try loading from the same directory as the script
            styleSheet = UnityEngine.Resources.Load<StyleSheet>("Core/UI/Core/Components/IconToggle");
        }
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }

        // Create label to show the icon
        _iconLabel = new Label(_icon);
        _iconLabel.AddToClassList("icon-toggle__label");
        hierarchy.Add(_iconLabel);

        // Click to toggle
        RegisterCallback<ClickEvent>(evt => { value = !value; });

        // Initial visual state
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_iconLabel != null)
            _iconLabel.text = value ? _icon : "";

        if (value)
        {
            EnableInClassList("icon-toggle--active", true);
        }
        else
        {
            EnableInClassList("icon-toggle--active", false);
        }
    }
}