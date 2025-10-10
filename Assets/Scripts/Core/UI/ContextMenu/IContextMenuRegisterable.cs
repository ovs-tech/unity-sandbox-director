using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UI.ContextMenu
{
    /// <summary>
    /// Interface for UI components that can register context menu items
    /// </summary>
    public interface IContextMenuRegisterable
    {
        /// <summary>
        /// Gets the unique identifier for this registerable component
        /// </summary>
        string ComponentId { get; }
        
        /// <summary>
        /// Gets the context menu items this component wants to register
        /// This is called when the context menu needs to be built
        /// </summary>
        /// <param name="menuContext">Context information about where the menu is being shown</param>
        /// <returns>List of context menu items</returns>
        IEnumerable<ContextMenuItem> GetContextMenuItems(MenuContext menuContext);
        
        /// <summary>
        /// Called when this component should register its context menu items
        /// </summary>
        void RegisterContextMenuItems();
        
        /// <summary>
        /// Called when this component should unregister its context menu items
        /// </summary>
        void UnregisterContextMenuItems();
        
        /// <summary>
        /// Whether this component can provide context menu items for the given context
        /// </summary>
        /// <param name="menuContext">The menu context</param>
        /// <returns>True if this component can provide menu items</returns>
        bool CanProvideMenuItems(MenuContext menuContext);
    }

    /// <summary>
    /// Context information provided when building context menus
    /// </summary>
    public class MenuContext
    {
        public Vector2 ScreenPosition { get; set; }
        public Vector2 LocalPosition { get; set; }
        public object Target { get; set; }
        public string MenuType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public MenuContext(Vector2 screenPos, object target = null, string menuType = "default")
        {
            ScreenPosition = screenPos;
            Target = target;
            MenuType = menuType;
        }

        public T GetTarget<T>() where T : class
        {
            return Target as T;
        }

        public T GetProperty<T>(string key, T defaultValue = default(T))
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }
    }

    /// <summary>
    /// Enhanced context menu item with category and priority support
    /// </summary>
    [System.Serializable]
    public class ContextMenuItem
    {
        public string text;
        public Action action;
        public string icon;
        public bool enabled;
        public MenuCategory category;
        public int priority;
        public bool isSeparator;
        public Func<bool> isVisible;
        public string tooltip;
        public Color? textColor;

        public ContextMenuItem(string itemText, Action itemAction, string itemIcon = "", bool itemEnabled = true)
        {
            text = itemText;
            action = itemAction;
            icon = itemIcon;
            enabled = itemEnabled;
            category = MenuCategory.Action;
            priority = 0;
            isSeparator = false;
        }

        public ContextMenuItem(string itemText, Action itemAction, MenuCategory itemCategory, 
                               int itemPriority = 0, string itemIcon = "", bool itemEnabled = true)
        {
            text = itemText;
            action = itemAction;
            icon = itemIcon;
            enabled = itemEnabled;
            category = itemCategory;
            priority = itemPriority;
            isSeparator = false;
        }

        /// <summary>
        /// Creates a separator menu item
        /// </summary>
        /// <param name="category">Category for the separator</param>
        /// <param name="priority">Priority for the separator</param>
        /// <returns>A separator menu item</returns>
        public static ContextMenuItem CreateSeparator(MenuCategory category = MenuCategory.Action, int priority = 0)
        {
            return new ContextMenuItem("", null, category, priority)
            {
                isSeparator = true
            };
        }

        /// <summary>
        /// Sets a visibility condition for this menu item
        /// </summary>
        /// <param name="condition">Function that returns true if item should be visible</param>
        /// <returns>This menu item for chaining</returns>
        public ContextMenuItem WithVisibility(Func<bool> condition)
        {
            isVisible = condition;
            return this;
        }

        /// <summary>
        /// Sets a tooltip for this menu item
        /// </summary>
        /// <param name="tooltipText">Tooltip text</param>
        /// <returns>This menu item for chaining</returns>
        public ContextMenuItem WithTooltip(string tooltipText)
        {
            tooltip = tooltipText;
            return this;
        }

        /// <summary>
        /// Sets a custom text color for this menu item
        /// </summary>
        /// <param name="color">Text color</param>
        /// <returns>This menu item for chaining</returns>
        public ContextMenuItem WithTextColor(Color color)
        {
            textColor = color;
            return this;
        }

        /// <summary>
        /// Whether this menu item should be visible
        /// </summary>
        public bool IsVisible => isVisible?.Invoke() ?? true;
    }

    /// <summary>
    /// Categories for organizing context menu items
    /// </summary>
    public enum MenuCategory
    {
        Edit = 0,      // Cut, Copy, Paste, Delete
        Create = 1,    // Add new items
        Transform = 2, // Move, Resize, Rotate
        View = 3,      // Zoom, Pan, Focus
        Properties = 4,// Settings, Options, Properties
        Action = 5,    // Custom actions
        Debug = 6      // Debug options
    }

    /// <summary>
    /// Priority constants for common menu items
    /// </summary>
    public static class MenuPriority
    {
        public const int Highest = -1000;
        public const int High = -100;
        public const int Normal = 0;
        public const int Low = 100;
        public const int Lowest = 1000;
        
        // Common action priorities
        public const int Cut = -50;
        public const int Copy = -40;
        public const int Paste = -30;
        public const int Delete = -20;
        public const int Duplicate = -10;
        public const int Properties = 50;
    }
}