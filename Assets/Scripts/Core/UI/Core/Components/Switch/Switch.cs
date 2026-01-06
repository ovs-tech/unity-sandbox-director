using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Switch : BaseBoolField
{
  [UxmlAttribute]
  public string Text { get => _label.text; set => _label.text = value; }
  [UxmlAttribute]
  public bool ShowLabel { get => _label.visible; set => SetShowLabel(value); }

  private Label _label;

  private VisualElement _border;
  private VisualElement _control;


  private const string StyleSheetPath = "Core/UI/Core/Components/Switch/Switch";

  public Switch() : base(null)
  {
    // Load default stylesheet
    var styleSheet = Resources.Load<StyleSheet>(StyleSheetPath);
    if (styleSheet != null)
    {
      styleSheets.Add(styleSheet);
    }

    _label = new Label("Name");
    _label.name = "Text";
    _border = new VisualElement();
    _border.name = "Border";
    _border.AddToClassList("switch__border");

    _control = new VisualElement();
    _control.name = "Control";
    _control.AddToClassList("switch__control");

    Add(_label);
    Add(_border);
    _border.Add(_control);

    // Initialize visual state to current value
    SetState(value);
  }

  private void SetShowLabel(bool show)
  {
    _label.visible = show;
    _label.EnableInClassList("switch__label--hidden", !show);
  }

  private void SetState(bool value)
  {
    _border.EnableInClassList("switch__border--on", value);
    _control.EnableInClassList("switch__control--on", value);
  }

  // Ensure visual state is updated whenever the value changes
  public override void SetValueWithoutNotify(bool newValue)
  {
    base.SetValueWithoutNotify(newValue);
    SetState(newValue);
  }
}