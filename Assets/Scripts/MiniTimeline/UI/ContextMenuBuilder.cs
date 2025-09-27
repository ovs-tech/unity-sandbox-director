using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Builder class for constructing context menus with proper grouping and organization
    /// </summary>
    public class ContextMenuBuilder
    {
        private readonly List<ContextMenuItem> items = new List<ContextMenuItem>();
        private readonly MenuContext context;

        public ContextMenuBuilder(MenuContext menuContext)
        {
            context = menuContext;
        }

        /// <summary>
        /// Adds a menu item to the builder
        /// </summary>
        public ContextMenuBuilder AddItem(ContextMenuItem item)
        {
            if (item != null)
                items.Add(item);
            return this;
        }

        /// <summary>
        /// Adds a menu item with basic parameters
        /// </summary>
        public ContextMenuBuilder AddItem(string text, Action action, string icon = "", 
                                        MenuCategory category = MenuCategory.Action, int priority = MenuPriority.Normal)
        {
            return AddItem(new ContextMenuItem(text, action, category, priority, icon));
        }

        /// <summary>
        /// Adds a separator at the given category and priority
        /// </summary>
        public ContextMenuBuilder AddSeparator(MenuCategory category = MenuCategory.Action, int priority = MenuPriority.Normal)
        {
            return AddItem(ContextMenuItem.CreateSeparator(category, priority));
        }

        /// <summary>
        /// Adds multiple items from an enumerable
        /// </summary>
        public ContextMenuBuilder AddItems(IEnumerable<ContextMenuItem> menuItems)
        {
            if (menuItems != null)
            {
                foreach (var item in menuItems)
                {
                    AddItem(item);
                }
            }
            return this;
        }

        /// <summary>
        /// Adds items from a registerable component if it can provide items for this context
        /// </summary>
        public ContextMenuBuilder AddFromComponent(IContextMenuRegisterable component)
        {
            if (component?.CanProvideMenuItems(context) == true)
            {
                AddItems(component.GetContextMenuItems(context));
            }
            return this;
        }

        /// <summary>
        /// Builds the final menu item list with proper sorting and separator insertion
        /// </summary>
        public List<ContextMenuItem> Build()
        {
            var result = new List<ContextMenuItem>();
            
            // Filter out invisible items
            var visibleItems = items.Where(item => item.IsVisible).ToList();
            
            if (visibleItems.Count == 0)
                return result;

            // Group by category and sort within each category by priority
            var categorizedItems = visibleItems
                .GroupBy(item => item.category)
                .OrderBy(group => (int)group.Key)
                .ToList();

            bool isFirstCategory = true;
            
            foreach (var categoryGroup in categorizedItems)
            {
                // Add separator between categories (but not before the first one)
                if (!isFirstCategory)
                {
                    result.Add(ContextMenuItem.CreateSeparator());
                }
                isFirstCategory = false;

                // Sort items within category by priority (lower values first)
                var sortedItems = categoryGroup
                    .OrderBy(item => item.priority)
                    .ThenBy(item => item.text)
                    .ToList();

                // Add all items in this category
                bool addedAnyInCategory = false;
                foreach (var item in sortedItems)
                {
                    if (item.isSeparator)
                    {
                        // Only add separator if we have items before it in this category
                        if (addedAnyInCategory)
                        {
                            result.Add(item);
                        }
                    }
                    else
                    {
                        result.Add(item);
                        addedAnyInCategory = true;
                    }
                }
            }

            // Remove any trailing separators
            while (result.Count > 0 && result[result.Count - 1].isSeparator)
            {
                result.RemoveAt(result.Count - 1);
            }

            // Remove any duplicate adjacent separators
            for (int i = result.Count - 2; i >= 0; i--)
            {
                if (result[i].isSeparator && result[i + 1].isSeparator)
                {
                    result.RemoveAt(i + 1);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a standard edit menu section
        /// </summary>
        public static ContextMenuBuilder CreateStandardEditMenu(MenuContext context)
        {
            return new ContextMenuBuilder(context);
        }
    }

    /// <summary>
    /// Registry for managing context menu providers
    /// </summary>
    public class ContextMenuRegistry
    {
        private readonly Dictionary<string, IContextMenuRegisterable> providers = 
            new Dictionary<string, IContextMenuRegisterable>();

        private static ContextMenuRegistry instance;
        public static ContextMenuRegistry Instance => instance ??= new ContextMenuRegistry();

        /// <summary>
        /// Registers a context menu provider
        /// </summary>
        public void Register(IContextMenuRegisterable provider)
        {
            if (provider == null) return;

            // Only add to the providers dictionary - don't call RegisterContextMenuItems 
            // to avoid infinite recursion since RegisterContextMenuItems calls this method
            providers[provider.ComponentId] = provider;
        }

        /// <summary>
        /// Unregisters a context menu provider
        /// </summary>
        public void Unregister(IContextMenuRegisterable provider)
        {
            if (provider == null) return;

            // Only remove from providers dictionary - don't call UnregisterContextMenuItems
            // to avoid potential circular dependencies
            if (providers.ContainsKey(provider.ComponentId))
            {
                providers.Remove(provider.ComponentId);
            }
        }

        /// <summary>
        /// Unregisters a provider by ID
        /// </summary>
        public void Unregister(string componentId)
        {
            if (providers.TryGetValue(componentId, out var provider))
            {
                // Only remove from providers dictionary - don't call UnregisterContextMenuItems
                // to avoid potential circular dependencies
                providers.Remove(componentId);
            }
        }

        /// <summary>
        /// Gets all registered providers that can provide menu items for the given context
        /// </summary>
        public IEnumerable<IContextMenuRegisterable> GetProvidersForContext(MenuContext context)
        {
            return providers.Values.Where(provider => provider.CanProvideMenuItems(context));
        }

        /// <summary>
        /// Builds a context menu using all relevant registered providers
        /// </summary>
        public List<ContextMenuItem> BuildContextMenu(MenuContext context)
        {
            var builder = new ContextMenuBuilder(context);
            
            foreach (var provider in GetProvidersForContext(context))
            {
                builder.AddFromComponent(provider);
            }
            
            return builder.Build();
        }

        /// <summary>
        /// Clears all registered providers
        /// </summary>
        public void Clear()
        {
            // Simply clear the providers dictionary - don't call UnregisterContextMenuItems 
            // to avoid potential circular dependencies
            providers.Clear();
        }
    }
}