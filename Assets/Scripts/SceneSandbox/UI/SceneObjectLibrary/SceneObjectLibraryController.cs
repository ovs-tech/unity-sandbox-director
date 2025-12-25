using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Systems.SceneSandbox.Data;
using SceneObjectLibrarySO = Systems.SceneSandbox.Data.SceneObjectLibrary;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary
{
    /// <summary>
    /// Controller for the Scene Object Library UI.
    /// Mediates between Model, ViewModel, and View.
    /// Exposes OnObjectSelected event for external integration (e.g., SceneSandboxBuilder).
    /// </summary>
    public class SceneObjectLibraryController : MonoBehaviour
    {
        [SerializeField]
        private SceneObjectLibrarySO _objectLibrary;
        
        [SerializeField]
        private UIDocument _uiDocument;
        
        private SceneObjectLibraryModel _model;
        private SceneObjectLibraryViewModel _viewModel;
        private SceneObjectLibraryView _view;
        
        /// <summary>
        /// Fired when an object is selected from the library.
        /// Payload: the selected SceneObjectData.
        /// </summary>
        [SerializeField]
        public UnityEvent<SceneObjectData> OnObjectSelected = new UnityEvent<SceneObjectData>();
        
        /// <summary>
        /// Fired when an object is selected from the library.
        /// Payload: the selected object ID (string).
        /// </summary>
        [SerializeField]
        public UnityEvent<string> OnObjectIdSelected = new UnityEvent<string>();
        
        private void OnEnable()
        {
            StartCoroutine(InitializeAsync());
        }
        
        private void OnDisable()
        {
            Cleanup();
        }
        
        /// <summary>
        /// Initialize the library UI asynchronously.
        /// </summary>
        private IEnumerator InitializeAsync()
        {
            // Validate dependencies
            if (_objectLibrary == null)
            {
                Debug.LogError("SceneObjectLibrary not assigned to SceneObjectLibraryController!");
                yield break;
            }
            
            if (_uiDocument == null)
            {
                Debug.LogError("UIDocument not assigned to SceneObjectLibraryController!");
                yield break;
            }
            
            // Create Model from SceneObjectLibrary
            _model = new SceneObjectLibraryModel(_objectLibrary);
            
            // Check if library is valid
            if (!_model.IsValid)
            {
                Debug.LogWarning("SceneObjectLibrary is empty or invalid. Library UI will show empty state.");
            }
            
            // Create ViewModel with data from Model
            var allObjects = _model.GetAllObjects();
            _viewModel = new SceneObjectLibraryViewModel(allObjects);
            
            // Get or create View component
            _view = GetComponent<SceneObjectLibraryView>();
            if (_view == null)
            {
                _view = gameObject.AddComponent<SceneObjectLibraryView>();
            }
            
            // Initialize View with ViewModel and UIDocument
            yield return _view.InitializeView(_viewModel, _uiDocument);
            
            // Wire up View events
            _view.OnCardClicked += HandleCardClicked;
            _view.OnTypeFilterChanged += HandleTypeFilterChanged;
            _view.OnCategoryFilterChanged += HandleCategoryFilterChanged;
            _view.OnSearchQueryChanged += HandleSearchQueryChanged;
            _view.OnTagFilterChanged += HandleTagFilterChanged;
        }
        
        /// <summary>
        /// Handle object card selection.
        /// </summary>
        private void HandleCardClicked(SceneObjectData obj)
        {
            if (obj == null) return;
            
            _viewModel.SelectedObject.Value = obj;
            OnObjectSelected.Invoke(obj);
            OnObjectIdSelected.Invoke(obj.id);
        }
        
        /// <summary>
        /// Handle type filter changes.
        /// </summary>
        private void HandleTypeFilterChanged(SceneObjectType? typeFilter)
        {
            _viewModel.ActiveTypeFilter.Value = typeFilter;
            _view.RenderObjectGrid(_viewModel.FilteredObjects.Value);
        }
        
        /// <summary>
        /// Handle category filter changes.
        /// </summary>
        private void HandleCategoryFilterChanged(string categoryFilter)
        {
            _viewModel.ActiveCategoryFilter.Value = categoryFilter ?? string.Empty;
            _view.RenderObjectGrid(_viewModel.FilteredObjects.Value);
        }
        
        /// <summary>
        /// Handle search query changes.
        /// </summary>
        private void HandleSearchQueryChanged(string query)
        {
            _viewModel.SearchQuery.Value = query ?? string.Empty;
            _view.RenderObjectGrid(_viewModel.FilteredObjects.Value);
        }
        
        /// <summary>
        /// Handle tag filter changes.
        /// </summary>
        private void HandleTagFilterChanged(string tagFilter)
        {
            _viewModel.ActiveTagFilter.Value = tagFilter ?? string.Empty;
            _view.RenderObjectGrid(_viewModel.FilteredObjects.Value);
        }
        
        /// <summary>
        /// Clean up resources.
        /// </summary>
        private void Cleanup()
        {
            if (_view != null)
            {
                _view.OnCardClicked -= HandleCardClicked;
                _view.OnTypeFilterChanged -= HandleTypeFilterChanged;
                _view.OnCategoryFilterChanged -= HandleCategoryFilterChanged;
                _view.OnSearchQueryChanged -= HandleSearchQueryChanged;
                _view.OnTagFilterChanged -= HandleTagFilterChanged;
            }
        }
        
        /// <summary>
        /// Assign a SceneObjectLibrary to this controller.
        /// </summary>
        public void SetObjectLibrary(SceneObjectLibrarySO library)
        {
            _objectLibrary = library;
        }
        
        /// <summary>
        /// Get the current ViewModel (for testing).
        /// </summary>
        public SceneObjectLibraryViewModel GetViewModel() => _viewModel;
    }
}
