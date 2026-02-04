using UnityEngine.UIElements;


public class TabContent : VisualElement
{
  private TabView _parentTabView;
  private int _index;

  public TabContent(TabView parent, int index)
  {
    _parentTabView = parent;
    _index = index;

    AddToClassList("tab-content");
  }
}

