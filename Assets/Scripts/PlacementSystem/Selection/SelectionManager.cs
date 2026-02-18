using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Selection
{
  /// <summary>
  /// Modular selection manager for PlacementSystem.
  /// Uses IInputProvider for selection input and IPlacementVisualizer for selection feedback.
  /// </summary>
  public class SelectionManager : MonoBehaviour
  {
    [Header("Dependencies")]
    private Camera _selectionCamera;

    [SerializeField, Tooltip("Layer mask for selectable objects")]
    private LayerMask _selectableLayer = -1;

    private PlacementController _placementController;

    private MonoBehaviour _inputProviderComponent;

    [Header("Selection Settings")]
    [SerializeField, Tooltip("Maximum raycast distance")]
    private float _maxRaycastDistance = 100f;

    [SerializeField, Tooltip("Selection is active and listening for input")]
    private bool _selectionEnabled = true;

    // Dependencies
    private IInputProvider _inputProvider;

    private void Awake()
    {
      if (_selectionCamera == null && _placementController != null)
        _selectionCamera = _placementController.PlacementCamera;

      if (_inputProviderComponent == null && _placementController != null)
        _inputProviderComponent = _placementController.InputProviderComponent;

      _inputProvider = _inputProviderComponent as IInputProvider;

      if (_inputProvider == null)
        Debug.LogError("SelectionManager: Input provider must implement IInputProvider");
      if (_placementController == null)
        Debug.LogError("SelectionManager: PlacementController not assigned");

      if (_selectionCamera == null)
        _selectionCamera = Camera.main;
    }

    private void Update()
    {
      if (!_selectionEnabled)
        return;

      if (_inputProvider == null)
        return;

      if (!_inputProvider.IsPlaceActionTriggered())
      {
        return;
      }

      GameObject selected = TrySelectObject(_selectionCamera, _selectableLayer);
      _placementController?.HandleSelectionClick(selected);
    }

    /// <summary>
    /// Enable or disable selection input processing.
    /// </summary>
    public void SetSelectionEnabled(bool enabled)
    {
      _selectionEnabled = enabled;
      if (!enabled)
        _placementController?.DeselectAll();
    }

    /// <summary>
    /// Assigns the PlacementController used for selection logic.
    /// </summary>
    public void SetPlacementController(PlacementController controller)
    {
      _placementController = controller;
      if (_selectionCamera == null && _placementController != null)
        _selectionCamera = _placementController.PlacementCamera;

      if (_inputProviderComponent == null && _placementController != null)
      {
        _inputProviderComponent = _placementController.InputProviderComponent;
        _inputProvider = _inputProviderComponent as IInputProvider;

        if (_inputProvider == null)
          Debug.LogError("SelectionManager: Input provider must implement IInputProvider");
      }
    }

    /// <summary>
    /// Assigns the input provider component used for selection input.
    /// </summary>
    public void SetInputProviderComponent(MonoBehaviour inputProviderComponent)
    {
      _inputProviderComponent = inputProviderComponent;
      _inputProvider = _inputProviderComponent as IInputProvider;

      if (_inputProvider == null)
        Debug.LogError("SelectionManager: Input provider must implement IInputProvider");
    }

    private GameObject TrySelectObject(Camera camera, LayerMask layerMask)
    {
      if (camera == null)
        return null;

      Vector2 pointerPosition = _inputProvider.GetPointerPosition();
      Ray ray = camera.ScreenPointToRay(pointerPosition);

      if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, layerMask))
      {
        GameObject hitObject = hit.collider.gameObject;
        var selectable = hitObject.GetComponentInParent<Selectable>();
        if (selectable != null)
          return selectable.gameObject;

        return hitObject;
      }

      return null;
    }

  }
}
