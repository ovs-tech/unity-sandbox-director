using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.SceneSandbox.Data;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary
{
    /// <summary>
    /// ViewModel for the Scene Object Library UI.
    /// Provides bindable properties for filtering and selection, with pure C# filtering logic.
    /// </summary>
    public class SceneObjectLibraryViewModel
    {
        private readonly List<SceneObjectData> _allObjects;
        
        /// <summary>
        /// The filtered list of objects based on current filters and search query.
        /// </summary>
        public readonly BindableProperty<List<SceneObjectData>> FilteredObjects;
        
        /// <summary>
        /// The currently active type filter (null means "All").
        /// </summary>
        public readonly SettableBindableProperty<SceneObjectType?> ActiveTypeFilter;
        
        /// <summary>
        /// The currently active category filter (empty means "All").
        /// </summary>
        public readonly SettableBindableProperty<string> ActiveCategoryFilter;
        
        /// <summary>
        /// The current search query string.
        /// </summary>
        public readonly SettableBindableProperty<string> SearchQuery;
        
        /// <summary>
        /// The currently selected object (for highlighting in UI).
        /// </summary>
        public readonly SettableBindableProperty<SceneObjectData> SelectedObject;
        
        /// <summary>
        /// List of available categories extracted from the library.
        /// </summary>
        public readonly BindableProperty<List<string>> AvailableCategories;
        
        public SceneObjectLibraryViewModel(List<SceneObjectData> allObjects)
        {
            _allObjects = allObjects ?? new List<SceneObjectData>();
            
            // Initialize settable properties with default values
            ActiveTypeFilter = new SettableBindableProperty<SceneObjectType?>(null);
            ActiveCategoryFilter = new SettableBindableProperty<string>(string.Empty);
            SearchQuery = new SettableBindableProperty<string>(string.Empty);
            SelectedObject = new SettableBindableProperty<SceneObjectData>(null);
            
            // Initialize computed properties with filtering logic
            FilteredObjects = BindableProperty<List<SceneObjectData>>.Bind(() =>
            {
                return ApplyFilters(_allObjects);
            });
            
            AvailableCategories = BindableProperty<List<string>>.Bind(() => ExtractAvailableCategories());
        }
        
        /// <summary>
        /// Apply all active filters to the object list.
        /// </summary>
        private List<SceneObjectData> ApplyFilters(List<SceneObjectData> objects)
        {
            if (objects == null || objects.Count == 0)
                return new List<SceneObjectData>();
            
            var filtered = objects.ToList(); // Start with all objects
            
            // Apply type filter
            if (ActiveTypeFilter.Value.HasValue)
            {
                filtered = filtered.Where(o => o.objectType == ActiveTypeFilter.Value.Value).ToList();
            }
            
            // Apply category filter
            var categoryFilter = ActiveCategoryFilter.Value;
            if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All")
            {
                filtered = filtered.Where(o => o.category == categoryFilter).ToList();
            }
            
            // Apply search filter (case-insensitive, partial match)
            var searchQuery = SearchQuery.Value;
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filtered = filtered.Where(o =>
                    o.displayName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            
            return filtered;
        }
        
        /// <summary>
        /// Extract unique categories from all objects, sorted alphabetically.
        /// "Default" category appears first, followed by others in alphabetical order.
        /// </summary>
        private List<string> ExtractAvailableCategories()
        {
            var categories = new HashSet<string>();
            
            foreach (var obj in _allObjects)
            {
                if (!string.IsNullOrEmpty(obj.category))
                    categories.Add(obj.category);
            }
            
            var sorted = new List<string> { "All" };
            
            // Add "Default" first if it exists
            if (categories.Contains("Default"))
            {
                sorted.Add("Default");
                categories.Remove("Default");
            }
            
            // Add remaining categories in alphabetical order
            sorted.AddRange(categories.OrderBy(c => c));
            
            return sorted;
        }
    }
}
