using UnityEngine.UIElements;

[UxmlElement]
public partial class TabElement : VisualElement
{
  [UxmlAttribute]
  public string Title { get; set; } = "Tab";

  [UxmlAttribute]
  public bool Selected { get; set; } = false;

  public TabElement()
  {
  }
}

