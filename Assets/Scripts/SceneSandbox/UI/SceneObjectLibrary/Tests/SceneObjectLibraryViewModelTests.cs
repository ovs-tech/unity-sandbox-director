using System.Collections.Generic;
using NUnit.Framework;
using Systems.SceneSandbox.Data;
using Systems.SceneSandbox.UI.SceneObjectLibrary;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary.Tests
{
    /// <summary>
    /// Unit tests for SceneObjectLibraryViewModel filtering logic.
    /// Tests pure C# functionality without Unity dependencies.
    /// </summary>
    public class SceneObjectLibraryViewModelTests
    {
        private SceneObjectLibraryViewModel _viewModel;
        private List<SceneObjectData> _testObjects;
        
        [SetUp]
        public void SetUp()
        {
            // Create test data
            _testObjects = CreateTestObjects();
            _viewModel = new SceneObjectLibraryViewModel(_testObjects);
        }
        
        /// <summary>
        /// Create a set of test objects with various types and categories.
        /// </summary>
        private List<SceneObjectData> CreateTestObjects()
        {
            return new List<SceneObjectData>
            {
                // Actors
                new SceneObjectData { id = "actor1", displayName = "Hero", objectType = SceneObjectType.Actor, category = "Characters" },
                new SceneObjectData { id = "actor2", displayName = "Hero_Alt", objectType = SceneObjectType.Actor, category = "Characters" },
                new SceneObjectData { id = "actor3", displayName = "Villain", objectType = SceneObjectType.Actor, category = "Characters" },
                
                // Props
                new SceneObjectData { id = "prop1", displayName = "Table", objectType = SceneObjectType.Prop, category = "Furniture" },
                new SceneObjectData { id = "prop2", displayName = "Chair", objectType = SceneObjectType.Prop, category = "Furniture" },
                new SceneObjectData { id = "prop3", displayName = "Sword", objectType = SceneObjectType.Prop, category = "Weapons" },
                
                // Cameras
                new SceneObjectData { id = "cam1", displayName = "Main Camera", objectType = SceneObjectType.Camera, category = "Cameras" },
                
                // Lights
                new SceneObjectData { id = "light1", displayName = "Sun Light", objectType = SceneObjectType.Light, category = "Lighting" },
            };
        }
        
        [Test]
        public void FilterByType_ReturnsOnlyActors()
        {
            // Arrange
            _viewModel.ActiveTypeFilter.Value = SceneObjectType.Actor;
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(3, filtered.Count);
            foreach (var obj in filtered)
            {
                Assert.AreEqual(SceneObjectType.Actor, obj.objectType);
            }
        }
        
        [Test]
        public void FilterByType_ReturnsOnlyProps()
        {
            // Arrange
            _viewModel.ActiveTypeFilter.Value = SceneObjectType.Prop;
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(3, filtered.Count);
            foreach (var obj in filtered)
            {
                Assert.AreEqual(SceneObjectType.Prop, obj.objectType);
            }
        }
        
        [Test]
        public void FilterByCategory_ReturnsOnlyFurniture()
        {
            // Arrange
            _viewModel.ActiveCategoryFilter.Value = "Furniture";
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(2, filtered.Count);
            foreach (var obj in filtered)
            {
                Assert.AreEqual("Furniture", obj.category);
            }
        }
        
        [Test]
        public void Search_CaseInsensitive_PartialMatch()
        {
            // Arrange
            _viewModel.SearchQuery.Value = "hero";
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(2, filtered.Count);
            Assert.IsTrue(filtered.Exists(o => o.displayName == "Hero"));
            Assert.IsTrue(filtered.Exists(o => o.displayName == "Hero_Alt"));
        }
        
        [Test]
        public void CombinedFilters_TypeAndCategory()
        {
            // Arrange
            _viewModel.ActiveTypeFilter.Value = SceneObjectType.Prop;
            _viewModel.ActiveCategoryFilter.Value = "Furniture";
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(2, filtered.Count);
            foreach (var obj in filtered)
            {
                Assert.AreEqual(SceneObjectType.Prop, obj.objectType);
                Assert.AreEqual("Furniture", obj.category);
            }
        }
        
        [Test]
        public void CombinedFilters_TypeCategoryAndSearch()
        {
            // Arrange
            _viewModel.ActiveTypeFilter.Value = SceneObjectType.Prop;
            _viewModel.ActiveCategoryFilter.Value = "Furniture";
            _viewModel.SearchQuery.Value = "table";
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("Table", filtered[0].displayName);
        }
        
        [Test]
        public void NoResults_ReturnsEmptyList()
        {
            // Arrange
            _viewModel.SearchQuery.Value = "nonexistent";
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(0, filtered.Count);
        }
        
        [Test]
        public void ClearFilter_ReturnsAllObjects()
        {
            // Arrange
            _viewModel.ActiveTypeFilter.Value = SceneObjectType.Actor;
            _viewModel.ActiveTypeFilter.Value = null; // Clear filter
            
            // Act
            var filtered = _viewModel.FilteredObjects.Value;
            
            // Assert
            Assert.AreEqual(_testObjects.Count, filtered.Count);
        }
        
        [Test]
        public void AvailableCategories_SortedCorrectly()
        {
            // Act
            var categories = _viewModel.AvailableCategories.Value;
            
            // Assert
            Assert.AreEqual(5, categories.Count);
            Assert.AreEqual("All", categories[0]);
            Assert.AreEqual("Cameras", categories[1]);
            Assert.AreEqual("Characters", categories[2]);
            Assert.AreEqual("Furniture", categories[3]);
            Assert.AreEqual("Weapons", categories[4]);
        }
    }
}
