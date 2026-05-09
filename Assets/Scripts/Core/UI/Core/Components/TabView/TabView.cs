using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class TabView : VisualElement
{
  
  private const string USS_PATH = "Core/UI/Core/Components/TabView/TabView";

  public int CurrentTabIndex { get; private set; }

  public override VisualElement contentContainer => _tabContainer ?? base.contentContainer;

  private VisualElement _tabContainer;
  private VisualElement _contentContainer;
  private List<TabButton> _AllButtons { get; set; } = new List<TabButton>();
  private List<TabContent> _AllContents { get; set; } = new List<TabContent>();
  private VisualElement TabContainer => _tabContainer;
  private VisualElement ContentContainer => _contentContainer;

  public TabView()
  {
    SetupElement();
    InitializeUI();
  }

  private void SetupElement()
  {
    // import USS if available
    StyleSheet styleSheet = Resources.Load<StyleSheet>(USS_PATH);
    if (styleSheet != null)
    {
      styleSheets.Add(styleSheet);
    }

    // Build layout directly to avoid reliance on UXML assets
    _tabContainer = new VisualElement { name = nameof(TabContainer) };
    _tabContainer.AddToClassList("tab-container");

    _contentContainer = new VisualElement { name = nameof(ContentContainer) };
    _contentContainer.AddToClassList("content-container");

    // Use hierarchy.Add to bypass overridden contentContainer during construction
    hierarchy.Add(_tabContainer);
    hierarchy.Add(_contentContainer);
  }

  public void Select(int tabIndex)
  {
    CurrentTabIndex = tabIndex;

    ContentContainer.Clear();

    TabContent newContent = _AllContents[tabIndex];

    ContentContainer.Add(newContent);

    for (int i = 0; i < _AllButtons.Count; i++)
    {
      TabButton curr = _AllButtons[i];

      curr.UnSelected();
    }

    _AllButtons[tabIndex].Select();
  }

  private void InitializeUI()
  {
    CheckForTabs();
    TabContainer.RegisterCallback<GeometryChangedEvent>(HandleContentChanged);
    TabContainer.RegisterCallback<AttachToPanelEvent>(HandleAttachedToPanel);
  }

  private void HandleAttachedToPanel(AttachToPanelEvent evt)
  {
    CheckForTabs();
  }

  private void HandleContentChanged(GeometryChangedEvent evt)
  {
    CheckForTabs();
  }

  public void CheckForTabs()
  {
    int baseIndex = _AllButtons.Count;
    int desiredSelection = -1;
    List<TabButton> newTabs = new List<TabButton>();
    List<TabContent> newContents = new List<TabContent>();
    int index = 0;

    int i = 0;

    // the goal of this loop is to check if we have any "TabElement" children that haven't been converted to a "TabButton"-"TabContent" pair
    // if we find any , we convert them into a TabButton and TabContent and we remove the TabElement since it's now "Setup" correctly
    while (i < contentContainer.childCount)
    {
      VisualElement curr = contentContainer[i];

      // if we found an "uncoverted" tab
      // we do the setup
      if (curr is TabElement t)
      {
        // create button
        TabButton newTab = new TabButton(this, t.Title, index);

        // copy content
        TabContent tabContent = new TabContent(this, index);

        // we take the content form the TabElement and we move it to the TabContent
        while (t.contentContainer.childCount != 0)
        {
          VisualElement tabC = t.contentContainer[0];

          // NOTE : apprentyl the "Add" method already removes the element from the previous parent
          // so no need to do something like : oldParent.Remove(child) ; newParent.Add(child)
          // we just add directly
          tabContent.Add(tabC);
        }

        newTabs.Add(newTab);
        newContents.Add(tabContent);

        if (desiredSelection == -1 && t is { Selected: true })
        {
          desiredSelection = baseIndex + index;
        }

        index++;

        contentContainer.RemoveAt(i);
      }

      else
      {
        i++;
      }


    }


    // we add all the buttons to the "button" header
    // and to a buttons collection
    foreach (TabButton b in newTabs)
    {
      _AllButtons.Add(b);
      TabContainer.Add(b);
    }

    // add to a contents collection
    // then the THIS TabView will decide which tab content to show
    foreach (TabContent c in newContents)
    {
      _AllContents.Add(c);
    }

    // Apply selection preference once new tabs are wired
    if (newTabs.Count > 0)
    {
      int targetIndex = desiredSelection >= 0 ? desiredSelection : 0;
      if (targetIndex < _AllButtons.Count)
      {
        Select(targetIndex);
      }
    }
  }
}
