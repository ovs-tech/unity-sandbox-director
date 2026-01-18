using UnityEngine.UIElements;

public class TabButton : Button
{
    private TabView _parentTabView;
    private int _index;

    private const string SELECTED_CLASS = "tab-button--selected";
    private const string UNSELECTED_CLASS = "tab-button--unselected";

    public TabButton(TabView parent, string title, int index)
    {
        _parentTabView = parent;
        _index = index;
        text = title;

        AddToClassList("tab-button");
        UnSelected();

        clicked += HandleClick;
    }

    private void HandleClick()
    {
        _parentTabView.Select(_index);
    }

    public void Select()
    {
        RemoveFromClassList(UNSELECTED_CLASS);
        AddToClassList(SELECTED_CLASS);
    }

    public void UnSelected()
    {
        RemoveFromClassList(SELECTED_CLASS);
        AddToClassList(UNSELECTED_CLASS);
    }
}
