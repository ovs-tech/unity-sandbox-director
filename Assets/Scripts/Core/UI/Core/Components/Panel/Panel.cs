using System;
using Systems;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Panel : VisualElement
{
    private VisualElement _header;
    private VisualElement _body;
    private VisualElement _footer;
    private Button _closeButton;
    private VisualElement _resizeCorner;
    private Label _titleLabel;

    public event Action onClose;

    private string _title = "Panel";
    [UxmlAttribute]
    public string title
    {
        get => _title;
        set
        {
            _title = value;
            if (_titleLabel != null)
                _titleLabel.text = _title;
        }
    }

    private bool _showFooter = true;
    [UxmlAttribute]
    public bool showFooter
    {
        get => _showFooter;
        set
        {
            _showFooter = value;
            if (_footer != null)
                _footer.style.display = _showFooter ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private bool _resizable = true;
    [UxmlAttribute]
    public bool resizable
    {
        get => _resizable;
        set
        {
            _resizable = value;
            if (_resizeCorner != null)
                _resizeCorner.style.display = _resizable ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private bool _showCloseButton = true;
    [UxmlAttribute]
    public bool showCloseButton
    {
        get => _showCloseButton;
        set
        {
            _showCloseButton = value;
            if (_closeButton != null)
                _closeButton.style.display = _showCloseButton ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public VisualElement header => _header;
    public VisualElement body => _body;
    public VisualElement footer => _footer;

    public Panel()
    {
        // Load stylesheet
        var styleSheet = Resources.Load<StyleSheet>("Panel");
        if (styleSheet == null)
        {
            styleSheet = Resources.Load<StyleSheet>("Core/UI/Core/Components/Panel");
        }
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }

        AddToClassList("panel");

        // Create header
        _header = new VisualElement();
        _header.AddToClassList("panel__header");
        hierarchy.Add(_header);

        // Create title label
        _titleLabel = new Label(_title);
        _titleLabel.AddToClassList("panel__title");
        _header.Add(_titleLabel);

        // Create close button
        _closeButton = new Button(OnCloseClicked);
        _closeButton.AddToClassList("panel__close-button");
        _closeButton.text = "×";
        _header.Add(_closeButton);

        // Create body
        _body = new VisualElement();
        _body.AddToClassList("panel__body");
        hierarchy.Add(_body);

        // Create footer
        _footer = new VisualElement();
        _footer.AddToClassList("panel__footer");
        hierarchy.Add(_footer);

        // Create resize corner
        _resizeCorner = new VisualElement();
        _resizeCorner.AddToClassList("panel__resize-corner");
        hierarchy.Add(_resizeCorner);

        // Add resize manipulator
        this.AddManipulator(new PanelResizeManipulator(_resizeCorner));
        this.AddManipulator(new PanelDragManipulator());

        // Apply initial states
        UpdateVisibility();
    }

    private void OnCloseClicked()
    {
        onClose?.Invoke();
        // Optionally auto-remove from parent
        RemoveFromHierarchy();
    }

    private void UpdateVisibility()
    {
        if (_footer != null)
            _footer.style.display = _showFooter ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (_resizeCorner != null)
            _resizeCorner.style.display = _resizable ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (_closeButton != null)
            _closeButton.style.display = _showCloseButton ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
