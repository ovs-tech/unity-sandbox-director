using UnityEngine.UIElements;

public class TabElement : VisualElement
{
  public new class UxmlFactory : UxmlFactory<TabElement, UxmlTraits> { }

  public new class UxmlTraits : VisualElement.UxmlTraits
  {
    UxmlStringAttributeDescription _title = new UxmlStringAttributeDescription { name = "title", defaultValue = "Tab" };
    UxmlBoolAttributeDescription _selected = new UxmlBoolAttributeDescription { name = "selected", defaultValue = false };

    public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
    {
      base.Init(ve, bag, cc);
      TabElement tabElement = ve as TabElement;
      tabElement.Title = _title.GetValueFromBag(bag, cc);
      tabElement.Selected = _selected.GetValueFromBag(bag, cc);
    }
  }

  public string Title { get; set; }
  public bool Selected { get; set; }

  public TabElement()
  {
    Title = "Tab";
    Selected = false;
  }
}

