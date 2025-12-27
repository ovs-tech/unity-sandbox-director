using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.Core;
using MiniTimeline.UI.MVVM.Track;
using MiniTimeline.UI.MVVM.Ruler;
using MiniTimeline.UI.Commands;
using MiniTimeline.UI.FormDefinitions;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;

namespace MiniTimeline.UI.MVVM.Timeline
{
  /// <summary>
  /// Controller for Timeline Editor MVVM.
  /// Orchestrates View-ViewModel interactions, wires commands, manages state, and handles track controllers.
  /// </summary>
  public class TimelineEditorController
  {
    readonly TimelineEditorView _view;
    readonly TimelineEditorModel _model;
    ViewModel _viewModel;
    readonly Dictionary<string, TrackController> _trackControllers = new Dictionary<string, TrackController>();
    TimelineRulerController _rulerController;
    TimelineRulerModel _rulerModel;

    TimelineEditorController(TimelineEditorView view, TimelineEditorModel model)
    {
      Debug.Assert(view != null, "View is null");
      Debug.Assert(model != null, "Model is null");
      _view = view;
      _model = model;

      _view.StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
      _viewModel = new ViewModel(_model);
      yield return _view.InitializeView(_viewModel);
      InitializeRuler();
      Bind(_viewModel);
      _viewModel.RefreshTracks();
    }

    public void Bind(ViewModel vm)
    {
      // Delegate UI bindings to the View
      _view.Bind(vm, _model, _rulerModel);

      // Subscribe to model state changes
      _model.OnCommandExecuted += vm.RefreshUndoRedoButtons;
      _model.OnCommandStacksChanged += vm.RefreshUndoRedoButtons;

      // Subscribe to track changes from model
      _model.OnTracksChanged += vm.RefreshTracks;
      _model.OnTrackAdded += track => {
        if (track != null) {
          vm.AddTrackToArray(track);
          Debug.Log($"[TimelineEditorController] Track added: {track.Id}");
        }
      };
      _model.OnTrackRemoved += (track, trackId) => {
        if (track != null) {
          vm.RemoveTrackFromArray(track);
          Debug.Log($"[TimelineEditorController] Track removed: {track.Id}");
        }
      };
      _model.OnTrackUpdated += track => {
        if (track != null) {
          vm.RefreshTracks();
          Debug.Log($"[TimelineEditorController] Track updated: {track.Id}");
        }
      };
      
      // Subscribe to clip changes from model
      _model.OnClipsChanged += vm.RefreshTracks;
      _model.OnClipAdded += (clip, trackId) => vm.RefreshTracks();
      _model.OnClipRemoved += (clip, trackId) => vm.RefreshTracks();
      _model.OnClipUpdated += (clip, trackId) => vm.RefreshTracks();
      
      // Subscribe to zoom changes to sync with all tracks
      _model.OnZoomChanged += zoom => {
        SyncZoomToAllTracks(zoom);
      };
      
      // Listen to observable array changes for track controller registration
      vm.Tracks.AnyValueChanged += tracks =>
      {
        Debug.Log("Tracks array changed - syncing track controllers");
        SyncTrackControllers(tracks);
      };

      // Provide a UIElements parent container for forms
      vm.FormHost = _view?.Root;
    }

    /// <summary>
    /// Initialize the ruler by delegating to the view's render method.
    /// </summary>
    private void InitializeRuler()
    {
      var (rulerController, rulerModel) = _view.InitializeRuler(_model);
      _rulerController = rulerController;
      _rulerModel = rulerModel;
    }

    // UI element wiring is handled by TimelineEditorView.Bind

    private string FormatTime(float time)
    {
      int minutes = Mathf.FloorToInt(time / 60f);
      int seconds = Mathf.FloorToInt(time % 60f);
      int frames = Mathf.FloorToInt((time % 1f) * 30f);
      return $"{minutes:00}:{seconds:00}:{frames:00}";
    }

    /// <summary>
    /// Syncs zoom level to all track controllers.
    /// </summary>
    private void SyncZoomToAllTracks(float zoom)
    {
      foreach (var trackController in _trackControllers.Values)
      {
        if (trackController != null)
        {
          trackController.UpdateZoom(zoom, _model.PixelsPerSecond);
        }
      }
    }

    /// <summary>
    /// Syncs track controllers based on the current state of the observable tracks array.
    /// Removes controllers for tracks no longer in the array and creates controllers for new tracks.
    /// </summary>
    private void SyncTrackControllers(IMiniTrack[] currentTracks)
    {
      if (currentTracks == null) return;

      // Get the set of track IDs currently in the array
      var trackIdsInArray = new HashSet<string>();
      foreach (var track in currentTracks)
      {
        if (track != null)
        {
          trackIdsInArray.Add(track.Id);
          
          // Create controller if it doesn't exist
          if (!_trackControllers.ContainsKey(track.Id))
          {
            _view.StartCoroutine(DelayedCreateTrackController(track.Id));
          }
        }
      }

      // Remove controllers for tracks no longer in the array
      var tracksToRemove = new List<string>();
      foreach (var trackId in _trackControllers.Keys)
      {
        if (!trackIdsInArray.Contains(trackId))
        {
          tracksToRemove.Add(trackId);
        }
      }

      foreach (var trackId in tracksToRemove)
      {
        UnregisterTrackController(trackId);
      }
    }

    /// <summary>
    /// Delayed coroutine to create and register a track controller.
    /// This ensures the track is fully initialized before creating its controller.
    /// </summary>
    private IEnumerator DelayedCreateTrackController(string trackId)
    {
      // Wait one frame to ensure the track is fully added to the director
      yield return null;
      CreateAndRegisterTrackController(trackId);
    }

    /// <summary>
    /// Registers a TrackController for a given track ID.
    /// </summary>
    public void RegisterTrackController(string trackId, TrackController trackController)
    {
      if (string.IsNullOrEmpty(trackId) || trackController == null)
      {
        Debug.LogWarning("Cannot register track controller: Invalid trackId or controller");
        return;
      }

      if (_trackControllers.ContainsKey(trackId))
      {
        Debug.LogWarning($"TrackController already registered for track {trackId}");
        return;
      }

      _trackControllers[trackId] = trackController;
      Debug.Log($"Registered TrackController for track: {trackId}");
    }

    /// <summary>
    /// Unregisters a TrackController for a given track ID.
    /// </summary>
    public void UnregisterTrackController(string trackId)
    {
      if (_trackControllers.ContainsKey(trackId))
      {
        _trackControllers.Remove(trackId);
        _view.RemoveTrackElement(trackId);
        Debug.Log($"Unregistered TrackController for track: {trackId}");
      }
    }

    /// <summary>
    /// Gets a TrackController by track ID.
    /// </summary>
    public TrackController GetTrackController(string trackId)
    {
      return _trackControllers.TryGetValue(trackId, out var controller) ? controller : null;
    }

    /// <summary>
    /// Gets all registered TrackControllers.
    /// </summary>
    public IReadOnlyDictionary<string, TrackController> GetAllTrackControllers()
    {
      return _trackControllers;
    }

    /// <summary>
    /// Clears all registered TrackControllers.
    /// </summary>
    public void ClearTrackControllers()
    {
      _trackControllers.Clear();
      Debug.Log("Cleared all TrackControllers");
    }

    /// <summary>
    /// Creates and registers a TrackController for a given track ID by delegating rendering to the view.
    /// </summary>
    public TrackController CreateAndRegisterTrackController(string trackId)
    {
      if (string.IsNullOrEmpty(trackId))
      {
        Debug.LogWarning("Cannot create TrackController: Invalid trackId");
        return null;
      }

      if (_trackControllers.ContainsKey(trackId))
      {
        Debug.LogWarning($"TrackController already exists for track {trackId}");
        return _trackControllers[trackId];
      }

      if (_model.Director == null)
      {
        Debug.LogWarning($"Cannot create TrackController: No director available");
        return null;
      }

      var track = _model.Director.GetTrack(trackId);
      if (track == null)
      {
        Debug.LogWarning($"Cannot create TrackController: Track '{trackId}' not found in director");
        return null;
      }

      // Delegate rendering to the view
      var trackController = _view.CreateAndRenderTrackController(
        track,
        _model.Director,
        _model.PixelsPerSecond,
        _model.Length,
        _model.Zoom
      );

      if (trackController != null)
      {
        RegisterTrackController(trackId, trackController);
        Debug.Log($"Successfully created and registered TrackController for track: {trackId}");
      }

      return trackController;
    }

    public class ViewModel
    {
      public readonly BindableProperty<bool> IsPlaying;
      public readonly BindableProperty<float> Time;
      public readonly BindableProperty<float> Length;
      public readonly BindableProperty<float> Zoom;
      public readonly BindableProperty<string> StatusText;
      public readonly BindableProperty<bool> CanUndo;
      public readonly BindableProperty<bool> CanRedo;
      public readonly BindableProperty<int> TrackCount;

      // Observable array for managing tracks
      public readonly ObservableArray<IMiniTrack> Tracks;
      public VisualElement FormHost { get; set; }

      readonly TimelineEditorModel _model;

      public ViewModel(TimelineEditorModel model)
      {
        _model = model;
        IsPlaying = BindableProperty<bool>.Bind(() => _model.IsPlaying);
        Time = BindableProperty<float>.Bind(() => _model.Time);
        Length = BindableProperty<float>.Bind(() => _model.Length);
        Zoom = BindableProperty<float>.Bind(() => _model.Zoom);
        StatusText = BindableProperty<string>.Bind(() => _model.StatusText);
        CanUndo = BindableProperty<bool>.Bind(() => _model.CanUndo);
        CanRedo = BindableProperty<bool>.Bind(() => _model.CanRedo);
        TrackCount = BindableProperty<int>.Bind(() => _model.Director?.Project?.tracks.Count ?? 0);

        // Initialize observable array with initial tracks if director is loaded
        var initialTracks = _model.Director?.Tracks != null
            ? new List<IMiniTrack>(_model.Director.Tracks)
            : new List<IMiniTrack>();
        Tracks = new ObservableArray<IMiniTrack>(20, initialTracks);

        // Subscribe to track changes to keep the observable array in sync
        _model.OnTracksChanged += SyncTracks;
        _model.OnTrackAdded += track => {
          if (track != null) {
            AddTrackToArray(track);
          }
        };
        _model.OnTrackRemoved += (track, trackId) => {
          if (track != null) {
            RemoveTrackFromArray(track);
          }
        };
        _model.OnTrackUpdated += track => {
          if (track != null) {
            SyncTracks();
          }
        };
      }

      private void SyncTracks()
      {
        if (_model.Director?.Tracks == null)
        {
          Tracks.Clear();
          return;
        }

        // Clear existing tracks
        Tracks.Clear();

        // Add all current tracks from director
        foreach (var track in _model.Director.Tracks)
        {
          Tracks.TryAdd(track);
        }
      }

      public void Play() => _model.Play();
      public void Pause() => _model.Pause();
      public void Stop() => _model.Stop();
      public void SetTime(float time) => _model.SetTime(time);
      public void SetZoom(float zoom) => _model.SetZoom(zoom);

      public void Undo() => _model.Undo();
      public void Redo() => _model.Redo();

      public void RefreshUndoRedoButtons()
      {
        // UI will update automatically via BindableProperty binding
      }

      public void RefreshTracks()
      {
        // UI will update automatically via BindableProperty binding
        SyncTracks();
      }

      // Track array management
      public void AddTrackToArray(IMiniTrack track)
      {
        if (track != null && System.Array.IndexOf(Tracks.items, track) == -1)
        {
          Tracks.TryAdd(track);
        }
      }

      public void RemoveTrackFromArray(IMiniTrack track)
      {
        if (track != null)
        {
          Tracks.TryRemove(track);
        }
      }

      public void RemoveTrackFromArrayById(string trackId)
      {
        var track = GetTrackById(trackId);
        if (track != null)
        {
          Tracks.TryRemove(track);
        }
      }

      public IMiniTrack GetTrackById(string trackId)
      {
        for (int i = 0; i < Tracks.Length; i++)
        {
          if (Tracks[i]?.Id == trackId)
            return Tracks[i];
        }
        return null;
      }

      public void SwapTracks(int index1, int index2)
      {
        if (index1 >= 0 && index1 < Tracks.Length && index2 >= 0 && index2 < Tracks.Length)
        {
          Tracks.Swap(index1, index2);
        }
      }

      public int GetTrackCount() => Tracks.Count;

      public IMiniTrack GetTrackAt(int index)
      {
        return index >= 0 && index < Tracks.Length ? Tracks[index] : null;
      }

      // Track operations
      public void ShowAddTrackForm()
      {
        if (_model.Director?.Project == null)
        {
          Debug.LogError("Cannot add track: No project loaded");
          return;
        }

        var fieldDefinitions = TrackFormDefinitions.GetCreateTrackFields();
        
        FormSubmitPanelUIToolkit.Instance.Show(
          "Add Track",
          fieldDefinitions,
          OnAddTrackFormSubmitted,
          OnAddTrackFormCancelled,
          FormHost
        );
      }

      private void OnAddTrackFormSubmitted(Dictionary<string, object> formData)
      {
        try
        {
          string trackType = formData.ContainsKey("trackType") ? formData["trackType"].ToString() : MiniTimelineConstants.TRACK_ANIM;
          string trackName = formData.ContainsKey("trackName") ? formData["trackName"].ToString() : "New Track";
          string bindKey = formData.ContainsKey("bindKey") ? formData["bindKey"].ToString() : "";
          bool enabled = formData.ContainsKey("enabled") ? Convert.ToBoolean(formData["enabled"]) : true;

          var addTrackCommand = new AddTrackCommand(_model.Director, trackType, trackName, bindKey, enabled);
          _model.ExecuteCommand(addTrackCommand);
          Debug.Log($"Added new {trackType} track: {trackName}");
        }
        catch (Exception ex)
        {
          Debug.LogError($"Failed to create track: {ex.Message}");
        }
      }

      private void OnAddTrackFormCancelled()
      {
        Debug.Log("Add track cancelled");
      }

      public void ShowBindingManager()
      {
        if (_model.Director?.BindingContext == null)
        {
          Debug.LogError("Cannot show binding manager: No binding context available");
          return;
        }

        var bindingContext = _model.Director.BindingContext;
        var fieldDefinitions = new List<FormFieldDefinition>
        {
          new FormFieldDefinition
          {
            name = "bindingList",
            type = "textarea",
            label = "Current Scene Bindings",
            required = false,
            defaultValue = FormatBindingsForDisplay(bindingContext),
            tooltip = "List of currently registered scene object bindings"
          },
          new FormFieldDefinition
          {
            name = "newBindingKey",
            type = "text",
            label = "New Binding Key",
            required = false,
            placeholder = "Enter binding key...",
            tooltip = "Unique key name for the new binding"
          },
          new FormFieldDefinition
          {
            name = "newBindingObject",
            type = "text",
            label = "Target Object Name",
            required = false,
            placeholder = "Enter GameObject name...",
            tooltip = "Name of the GameObject to bind"
          }
        };

        FormSubmitPanelUIToolkit.Instance.Show(
          "Binding Manager",
          fieldDefinitions,
          OnBindingManagerFormSubmitted,
          null,
          FormHost
        );
      }

      private void OnBindingManagerFormSubmitted(Dictionary<string, object> formData)
      {
        try
        {
          if (formData.ContainsKey("newBindingKey") && formData.ContainsKey("newBindingObject"))
          {
            string key = formData["newBindingKey"]?.ToString();
            string objectName = formData["newBindingObject"]?.ToString();
            
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(objectName))
            {
              var targetObject = GameObject.Find(objectName);
              if (targetObject != null)
              {
                _model.Director.BindingContext.Bind(key, targetObject);
                Debug.Log($"Registered binding: {key} -> {objectName}");
              }
              else
              {
                Debug.LogWarning($"GameObject '{objectName}' not found in scene");
              }
            }
          }
        }
        catch (Exception ex)
        {
          Debug.LogError($"Failed to update bindings: {ex.Message}");
        }
      }

      private string FormatBindingsForDisplay(BindableObjectManager bindingContext)
      {
        if (bindingContext == null) return "No bindings";
        var keys = bindingContext.GetKeys();
        if (keys == null || !keys.Any()) return "No bindings registered";
        return string.Join("\n", keys.Select(key => 
        {
          var obj = bindingContext.Resolve<GameObject>(key);
          return $"{key}: {(obj != null ? obj.name : "null")}";
        }));
      }

      public void ShowSaveProjectForm()
      {
        if (_model.Director?.Project == null)
        {
          Debug.LogError("Cannot save project: No project loaded");
          return;
        }

        var fieldDefinitions = new List<FormFieldDefinition>
        {
          new FormFieldDefinition
          {
            name = "filename",
            type = "text",
            label = "Filename",
            required = true,
            defaultValue = _model.Director.Project.name ?? "timeline_project",
            placeholder = "Enter project filename...",
            tooltip = "Name of the file to save (without extension)"
          },
          new FormFieldDefinition
          {
            name = "prettyPrint",
            type = "checkbox",
            label = "Pretty Print JSON",
            required = false,
            defaultValue = true,
            tooltip = "Format JSON for better readability"
          }
        };

        FormSubmitPanelUIToolkit.Instance.Show(
          "Save Project",
          fieldDefinitions,
          OnSaveProjectFormSubmitted,
          null,
          FormHost
        );
      }

      private void OnSaveProjectFormSubmitted(Dictionary<string, object> formData)
      {
        try
        {
          string filename = formData.ContainsKey("filename") ? formData["filename"].ToString() : "timeline_project";
          if (!filename.EndsWith(".json"))
          {
            filename += ".json";
          }

          bool success = _model.Director.SaveProject(filename);
          if (success)
          {
            Debug.Log($"Project '{filename}' saved successfully");
          }
          else
          {
            Debug.LogError($"Failed to save project '{filename}'");
          }
        }
        catch (Exception ex)
        {
          Debug.LogError($"Failed to save project: {ex.Message}");
        }
      }

      public void ShowLoadProjectForm()
      {
        var projectFiles = GetAvailableProjectFiles();
        var fieldDefinitions = new List<FormFieldDefinition>
        {
          new FormFieldDefinition
          {
            name = "filename",
            type = projectFiles.Length > 0 ? "selectbox" : "text",
            label = projectFiles.Length > 0 ? "Select Project File" : "Enter Filename",
            required = true,
            placeholder = "Enter project filename...",
            tooltip = "Select or enter the filename to load",
            options = projectFiles.Length > 0 ? new Dictionary<string, object>
            {
              { "items", projectFiles.ToList() }
            } : null
          },
          new FormFieldDefinition
          {
            name = "replaceBindings",
            type = "checkbox",
            label = "Auto-Update Scene Bindings",
            required = false,
            defaultValue = true,
            tooltip = "Automatically update scene bindings after loading"
          }
        };

        FormSubmitPanelUIToolkit.Instance.Show(
          "Load Project",
          fieldDefinitions,
          OnLoadProjectFormSubmitted,
          null,
          FormHost
        );
      }

      private void OnLoadProjectFormSubmitted(Dictionary<string, object> formData)
      {
        try
        {
          string filename = formData.ContainsKey("filename") ? formData["filename"].ToString() : "timeline_project";
          if (!filename.EndsWith(".json"))
          {
            filename += ".json";
          }

          bool success = _model.Director.LoadProject(filename);
          if (success)
          {
            Debug.Log($"Project '{filename}' loaded successfully");
          }
          else
          {
            Debug.LogError($"Failed to load project '{filename}'");
          }
        }
        catch (Exception ex)
        {
          Debug.LogError($"Failed to load project: {ex.Message}");
        }
      }

      private string[] GetAvailableProjectFiles()
      {
        try
        {
          string projectPath = Application.persistentDataPath;
          if (System.IO.Directory.Exists(projectPath))
          {
            var files = System.IO.Directory.GetFiles(projectPath, "*.json")
              .Select(System.IO.Path.GetFileName)
              .ToArray();
            return files;
          }
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"Failed to get available project files: {ex.Message}");
        }
        return new string[0];
      }
    }

    public class Builder
    {
      TimelineEditorView _view;
      TimelineEditorModel _model;
      MiniTimelineDirector _director;

      public Builder(TimelineEditorView view) { _view = view; }
      public Builder WithModel(TimelineEditorModel model) { _model = model; return this; }
      public Builder WithDirector(MiniTimelineDirector director) { _director = director; return this; }

      public TimelineEditorController Build()
      {
        if (_model == null) _model = new TimelineEditorModel();
        if (_director != null) _model.Initialize(_director);
        return new TimelineEditorController(_view, _model);
      }
    }
  }
}