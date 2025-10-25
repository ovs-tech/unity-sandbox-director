# SandboxBuilderUIToolkit - ObjectPalette Integration

## Summary
Added automatic initialization and integration of `ObjectPaletteUIToolkit` in `SandboxBuilderUIToolkit` to ensure the object palette is properly set up and connected.

## Changes Applied

### 1. Enhanced `Start()` Method
Added automatic detection and initialization of the ObjectPalette:

```csharp
private void Start()
{
    // Find sandbox builder if not assigned
    if (_sandboxBuilder == null)
    {
        _sandboxBuilder = FindFirstObjectByType<SceneSandboxBuilder>();
    }
    
    // Initialize object palette if not assigned
    if (_objectPalette == null)
    {
        _objectPalette = FindFirstObjectByType<ObjectPaletteUIToolkit>();
        
        if (_objectPalette == null)
        {
            Debug.LogWarning("SandboxBuilderUIToolkit: ObjectPaletteUIToolkit not found in scene. Creating new instance.");
            CreateObjectPalette();
        }
    }
    
    // Setup palette integration
    if (_objectPalette != null)
    {
        SetupPaletteIntegration();
    }
    
    // ... rest of initialization
}
```

**Benefits:**
- ✅ Automatic palette detection in scene
- ✅ Creates palette if missing
- ✅ Sets up event integration automatically

### 2. Added `CreateObjectPalette()` Method
Creates a new ObjectPalette instance when not found:

```csharp
private void CreateObjectPalette()
{
    // Create a new GameObject for the palette
    GameObject paletteGO = new GameObject("ObjectPaletteUIToolkit");
    paletteGO.transform.SetParent(transform);
    
    // Add the ObjectPaletteUIToolkit component
    _objectPalette = paletteGO.AddComponent<ObjectPaletteUIToolkit>();
    
    Debug.Log("SandboxBuilderUIToolkit: Created new ObjectPaletteUIToolkit instance.");
}
```

**Benefits:**
- ✅ Ensures palette always exists
- ✅ Properly parents to SandboxBuilder
- ✅ Logs creation for debugging

### 3. Added `SetupPaletteIntegration()` Method
Subscribes to palette events and integrates it with the UI:

```csharp
private void SetupPaletteIntegration()
{
    if (_objectPalette == null) return;
    
    // Subscribe to palette events
    _objectPalette.OnObjectSelected += OnPaletteObjectSelected;
    _objectPalette.OnObjectDraggedToScene += OnPaletteObjectDraggedToScene;
    
    // Integration logging
    if (_leftPanel != null)
    {
        Debug.Log("SandboxBuilderUIToolkit: Palette integration setup complete.");
    }
}
```

**Benefits:**
- ✅ Connects palette events to sandbox builder
- ✅ Enables drag-and-drop workflow
- ✅ Centralizes event handling

### 4. Added Palette Event Handlers
New event handlers for palette interactions:

```csharp
private void OnPaletteObjectSelected(SceneObjectData objectData)
{
    Debug.Log($"SandboxBuilderUIToolkit: Object selected from palette: {objectData.displayName}");
    // Additional logic when an object is selected from palette
}

private void OnPaletteObjectDraggedToScene(SceneObjectData objectData, Vector2 screenPosition)
{
    Debug.Log($"SandboxBuilderUIToolkit: Object dragged to scene: {objectData.displayName}");
    // The ObjectPalette already handles placement, but we can add additional logic here
}
```

**Benefits:**
- ✅ Centralized object selection handling
- ✅ Logging for debugging workflow
- ✅ Extensible for future features

### 5. Added `SetObjectPalette()` Public Method
Allows external setting of palette reference:

```csharp
public void SetObjectPalette(ObjectPaletteUIToolkit palette)
{
    // Unsubscribe from old palette if exists
    if (_objectPalette != null)
    {
        _objectPalette.OnObjectSelected -= OnPaletteObjectSelected;
        _objectPalette.OnObjectDraggedToScene -= OnPaletteObjectDraggedToScene;
    }
    
    _objectPalette = palette;
    
    // Subscribe to new palette
    if (_objectPalette != null)
    {
        SetupPaletteIntegration();
    }
}
```

**Benefits:**
- ✅ Allows runtime palette replacement
- ✅ Proper cleanup of old subscriptions
- ✅ Prevents memory leaks

### 6. Enhanced `UpdatePanelVisibility()` Method
Controls both UI panel and palette GameObject visibility:

```csharp
private void UpdatePanelVisibility()
{
    if (_leftPanel != null)
    {
        _leftPanel.style.display = _isPaletteVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    // Also control the palette GameObject if it exists
    if (_objectPalette != null && _objectPalette.gameObject != null)
    {
        _objectPalette.gameObject.SetActive(_isPaletteVisible);
    }
}
```

**Benefits:**
- ✅ Synchronized visibility control
- ✅ Performance optimization (deactivates GameObject)
- ✅ Works with mobile toggle controls

### 7. Enhanced `OnDestroy()` Method
Proper cleanup of palette event subscriptions:

```csharp
private void OnDestroy()
{
    // Unsubscribe from sandbox builder events
    if (_sandboxBuilder != null)
    {
        // ... sandbox builder cleanup
    }
    
    // Unsubscribe from palette events
    if (_objectPalette != null)
    {
        _objectPalette.OnObjectSelected -= OnPaletteObjectSelected;
        _objectPalette.OnObjectDraggedToScene -= OnPaletteObjectDraggedToScene;
    }
}
```

**Benefits:**
- ✅ Prevents memory leaks
- ✅ Clean event unsubscription
- ✅ Safe destruction

## Integration Flow

```
┌─────────────────────────────────────────────────────────┐
│          SandboxBuilderUIToolkit.Start()                │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ├─► Find SceneSandboxBuilder
                      │
                      ├─► Find ObjectPaletteUIToolkit
                      │   │
                      │   ├─► Found? ─────────────┐
                      │   │                       │
                      │   └─► Not Found?          │
                      │       │                   │
                      │       └─► CreateObjectPalette()
                      │           │               │
                      │           └───────────────┤
                      │                           │
                      ├─► SetupPaletteIntegration()◄┘
                      │   │
                      │   ├─► Subscribe to OnObjectSelected
                      │   └─► Subscribe to OnObjectDraggedToScene
                      │
                      ├─► BindSandboxEvents()
                      └─► UpdateUI()

┌─────────────────────────────────────────────────────────┐
│                    Event Flow                           │
└─────────────────────────────────────────────────────────┘

ObjectPaletteUIToolkit                SandboxBuilderUIToolkit
       │                                      │
       ├─► OnObjectSelected ────────────────►│
       │                                      ├─► OnPaletteObjectSelected()
       │                                      │   └─► Log selection
       │                                      │
       ├─► OnObjectDraggedToScene ──────────►│
       │                                      ├─► OnPaletteObjectDraggedToScene()
       │                                      │   └─► Additional logic
       │                                      │
       │                                      │
```

## Usage Examples

### Scene Setup (Inspector)
```
GameObject: SandboxBuilderUI
├─ SandboxBuilderUIToolkit (Component)
│  ├─ UI Document: [Assigned]
│  ├─ Object Palette: [Auto-detected or assigned]
│  └─ Panel Pos: [Assigned]
```

### Runtime Setup (Code)
```csharp
// Automatic (preferred)
var builderUI = gameObject.AddComponent<SandboxBuilderUIToolkit>();
// Palette is automatically found or created in Start()

// Manual (if needed)
var builderUI = gameObject.AddComponent<SandboxBuilderUIToolkit>();
var palette = CreatePalette(); // Your palette creation logic
builderUI.SetObjectPalette(palette);
```

### Toggling Palette Visibility
```csharp
// From code
sandboxBuilderUI.TogglePalette();

// Or directly
sandboxBuilderUI.SetPaletteVisible(false);
```

## Testing Checklist

- [ ] **Scene with existing palette** - Verify auto-detection works
- [ ] **Scene without palette** - Verify automatic creation works
- [ ] **Palette events** - Verify OnObjectSelected fires correctly
- [ ] **Drag and drop** - Verify OnObjectDraggedToScene fires correctly
- [ ] **Toggle visibility** - Verify palette shows/hides correctly
- [ ] **Mobile controls** - Verify mobile toggle buttons work
- [ ] **Runtime replacement** - Test SetObjectPalette() with different palettes
- [ ] **Memory leaks** - Verify proper cleanup on destroy
- [ ] **Multiple instances** - Test behavior with multiple SandboxBuilders

## Configuration Requirements

### Required Components
1. **UIDocument** - For UI Toolkit rendering
2. **ObjectPaletteUIToolkit** - Auto-detected or created
3. **SceneSandboxBuilder** - Core sandbox functionality

### Optional Components
1. **SceneObjectLibrary** - Provides objects for palette
2. **FormSubmitPanelUIToolkit** - For dialogs and forms

## Known Limitations

1. **Automatic Creation**: When auto-creating the palette, it won't have references to:
   - UIDocument template
   - StyleSheet
   - Item template
   - Scene object library
   
   **Solution**: Assign these via inspector or set them programmatically after creation.

2. **Multiple Palettes**: Currently supports one palette at a time.

3. **UI Integration**: The palette manages its own UIDocument, not integrated into the main UI template.

## Future Enhancements

1. **Auto-assign Resources**: Automatically find and assign templates/stylesheets
2. **Multiple Palettes**: Support for switching between different object palettes
3. **Palette Presets**: Load different palette configurations
4. **Event Analytics**: Track palette usage for user behavior insights
5. **Undo/Redo**: Integration with command pattern for palette operations

## Related Files
- `SandboxBuilderUIToolkit.cs` - Main UI controller (updated)
- `ObjectPaletteUIToolkit.cs` - Palette component
- `SceneSandboxBuilder.cs` - Core sandbox builder
- `SceneObjectLibrary.cs` - Object data source
