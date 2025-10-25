# Palette Resource Management in SandboxBuilderUIToolkit

## Overview
The `SandboxBuilderUIToolkit` now manages and assigns all necessary resources to the `ObjectPaletteUIToolkit` component, providing a centralized configuration point for the entire UI system.

## Architecture

### Resource Flow
```
SandboxBuilderUIToolkit (Inspector Configuration)
├─ Object Library
├─ Palette Template (UXML)
├─ Item Template (UXML)
└─ Palette StyleSheet (USS)
         │
         ├─ Assigned via Reflection
         ↓
ObjectPaletteUIToolkit (Runtime)
├─ _objectLibrary
├─ _paletteTemplate
├─ _itemTemplate
└─ _paletteStyleSheet
```

## New Serialized Fields

### Added to SandboxBuilderUIToolkit
```csharp
[Header("Palette Resources")]
[SerializeField] private SceneObjectLibrary _objectLibrary;
[SerializeField] private VisualTreeAsset _paletteTemplate;
[SerializeField] private VisualTreeAsset _itemTemplate;
[SerializeField] private StyleSheet _paletteStyleSheet;
```

**Purpose**: Centralize all palette-related resources in one inspector location for easier management.

## Implementation Details

### 1. AssignPaletteResources Method
Uses reflection to set private serialized fields on the ObjectPaletteUIToolkit:

```csharp
private void AssignPaletteResources()
{
    if (_objectPalette == null) return;
    
    var paletteType = typeof(ObjectPaletteUIToolkit);
    
    // Set object library
    if (_objectLibrary != null)
    {
        var objectLibraryField = paletteType.GetField("_objectLibrary", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (objectLibraryField != null)
        {
            objectLibraryField.SetValue(_objectPalette, _objectLibrary);
            Debug.Log("Assigned ObjectLibrary to palette.");
        }
    }
    
    // Similar for _paletteTemplate, _itemTemplate, _paletteStyleSheet
    // Also assigns _sandboxBuilder reference
}
```

**Key Features:**
- ✅ Uses reflection to access private fields
- ✅ Null checks before assignment
- ✅ Debug logging for each assignment
- ✅ Also assigns SandboxBuilder reference

### 2. Updated CreateObjectPalette
Now calls `AssignPaletteResources` after creating the component:

```csharp
private void CreateObjectPalette()
{
    GameObject paletteGO = new GameObject("ObjectPaletteUIToolkit");
    paletteGO.transform.SetParent(transform);
    
    _objectPalette = paletteGO.AddComponent<ObjectPaletteUIToolkit>();
    
    // Assign resources immediately after creation
    AssignPaletteResources();
    
    Debug.Log("Created new ObjectPaletteUIToolkit instance.");
}
```

### 3. Updated SetupPaletteIntegration
Ensures resources are assigned even for existing palettes:

```csharp
private void SetupPaletteIntegration()
{
    if (_objectPalette == null) return;
    
    // Ensure palette has all required resources
    AssignPaletteResources();
    
    // Subscribe to events...
    // Attach UI...
}
```

**Why?** Even if the palette was created in the scene manually, this ensures it has the latest resource references from the SandboxBuilder.

## Resource Assignments

### 1. Object Library (SceneObjectLibrary)
**Purpose**: Provides the data for all available objects in the palette
**Field**: `_objectLibrary` in ObjectPaletteUIToolkit
**Usage**: Source of SceneObjectData for palette items

### 2. Palette Template (VisualTreeAsset)
**Purpose**: UXML template for the palette container structure
**Field**: `_paletteTemplate` in ObjectPaletteUIToolkit
**Usage**: Defines filters, search, and layout structure

### 3. Item Template (VisualTreeAsset)
**Purpose**: UXML template for individual palette items
**Field**: `_itemTemplate` in ObjectPaletteUIToolkit
**Usage**: Visual structure for each object card/button

### 4. Palette StyleSheet (StyleSheet)
**Purpose**: USS styles for palette appearance
**Field**: `_paletteStyleSheet` in ObjectPaletteUIToolkit
**Usage**: Colors, sizes, layout rules for palette

### 5. Sandbox Builder (SceneSandboxBuilder)
**Purpose**: Core sandbox functionality
**Field**: `_sandboxBuilder` in ObjectPaletteUIToolkit
**Usage**: Enables object placement in scene

## Inspector Configuration

### In Unity Editor

```
GameObject: SandboxBuilderUI
└─ SandboxBuilderUIToolkit (Component)
   ├─ [UI Document]
   │  └─ UI Document: (auto-created or assigned)
   │
   ├─ [Visual Assets]
   │  ├─ Main UI Template: SandboxBuilderUIToolkit.uxml
   │  └─ Main UI StyleSheet: SandboxBuilderUIToolkit.uss
   │
   ├─ [Palette Resources] ← NEW SECTION
   │  ├─ Object Library: YourObjectLibrary.asset
   │  ├─ Palette Template: ObjectPaletteUIToolkit.uxml
   │  ├─ Item Template: ObjectPaletteItem.uxml
   │  └─ Palette StyleSheet: ObjectPalette.uss
   │
   └─ [References]
      ├─ Object Palette: (auto-detected or assigned)
      └─ Panel Pos: (Transform for dialogs)
```

## Benefits

### 1. Centralized Configuration 🎯
**Before**: Resources scattered across multiple components
**After**: All resources configured in one place (SandboxBuilderUIToolkit)

### 2. Dynamic Creation Support 🏗️
**Before**: Manually created palettes needed manual resource assignment
**After**: Resources automatically assigned whether palette is created or found

### 3. Easier Maintenance 🔧
**Before**: Update resources in multiple places
**After**: Update once in SandboxBuilder inspector

### 4. Better Flexibility 🔄
**Before**: Fixed resource references
**After**: Can swap resources at runtime or per scene

### 5. Clear Dependencies 📊
**Before**: Unclear what resources palette needs
**After**: All requirements visible in one inspector section

## Use Cases

### Use Case 1: Standard Setup
```csharp
// Just assign resources in inspector
// Everything works automatically
```

### Use Case 2: Runtime Palette Creation
```csharp
// SandboxBuilder finds no palette
// Creates new one
// Automatically assigns all resources
// ✅ Fully functional palette
```

### Use Case 3: Multiple Object Libraries
```csharp
// Inspector: Assign different ObjectLibrary
// Runtime: Palette refreshes with new objects
public void SwitchLibrary(SceneObjectLibrary newLibrary)
{
    _objectLibrary = newLibrary;
    AssignPaletteResources();
    if (_objectPalette != null)
    {
        _objectPalette.RefreshPalette();
    }
}
```

### Use Case 4: Theme Switching
```csharp
// Switch visual themes at runtime
public void ApplyTheme(StyleSheet newTheme)
{
    _paletteStyleSheet = newTheme;
    AssignPaletteResources();
    // Palette automatically uses new stylesheet
}
```

## Reflection Details

### Why Use Reflection?
1. **Private Fields**: ObjectPaletteUIToolkit fields are `[SerializeField] private`
2. **Runtime Assignment**: Need to set values after component creation
3. **Encapsulation**: Maintains proper encapsulation without exposing public setters

### Performance Considerations
- ✅ **One-time operation**: Only runs during initialization
- ✅ **Cached Type Info**: Could be optimized with static field caching
- ✅ **Minimal overhead**: Negligible impact on runtime performance

### Alternatives Considered

#### Option 1: Public Setters
```csharp
// In ObjectPaletteUIToolkit
public void SetObjectLibrary(SceneObjectLibrary library) { ... }
public void SetPaletteTemplate(VisualTreeAsset template) { ... }
```
**Drawback**: Breaks encapsulation, more API surface to maintain

#### Option 2: Constructor Parameters
```csharp
public ObjectPaletteUIToolkit(SceneObjectLibrary library, ...) { ... }
```
**Drawback**: Can't use with AddComponent, Unity doesn't support parametric constructors

#### Option 3: Initialization Method
```csharp
_objectPalette.Initialize(library, template, ...);
```
**Drawback**: Easy to forget calling Initialize, error-prone

**Chosen: Reflection** ✅
- Maintains encapsulation
- Works with Unity's component system
- Automatic assignment
- Clear intent

## Debug Logging

Each assignment logs success:
```
SandboxBuilderUIToolkit: Assigned ObjectLibrary to palette.
SandboxBuilderUIToolkit: Assigned PaletteTemplate to palette.
SandboxBuilderUIToolkit: Assigned ItemTemplate to palette.
SandboxBuilderUIToolkit: Assigned StyleSheet to palette.
SandboxBuilderUIToolkit: Assigned SandboxBuilder to palette.
```

**Benefits**:
- ✅ Easy to verify configuration
- ✅ Debug missing resources
- ✅ Track initialization flow

## Error Handling

### Null Resource Handling
```csharp
if (_objectLibrary != null)
{
    // Only assign if resource is available
}
```
**Result**: Palette works even with partial configuration (falls back to code-generated UI)

### Missing Field Handling
```csharp
if (objectLibraryField != null)
{
    // Only set if field exists
}
```
**Result**: Safe against API changes in ObjectPaletteUIToolkit

## Testing Checklist

- [ ] **Inspector Assignment**: Resources show in inspector
- [ ] **Auto-Creation**: Palette created with resources
- [ ] **Manual Palette**: Existing palette receives resources
- [ ] **Partial Config**: Works with some null resources
- [ ] **Runtime Switching**: Can change resources at runtime
- [ ] **Debug Logs**: All assignments logged correctly
- [ ] **Null Safety**: No errors with null resources
- [ ] **Reflection Safety**: No errors if fields renamed

## Future Enhancements

### 1. Resource Validation
```csharp
private bool ValidatePaletteResources()
{
    bool isValid = true;
    
    if (_objectLibrary == null)
    {
        Debug.LogWarning("Object Library not assigned");
        isValid = false;
    }
    
    // Validate other resources...
    return isValid;
}
```

### 2. Resource Presets
```csharp
[System.Serializable]
public class PaletteResourcePreset
{
    public string presetName;
    public SceneObjectLibrary objectLibrary;
    public VisualTreeAsset paletteTemplate;
    public VisualTreeAsset itemTemplate;
    public StyleSheet paletteStyleSheet;
}

[SerializeField] private PaletteResourcePreset[] _presets;
```

### 3. Hot Reload Support
```csharp
#if UNITY_EDITOR
[ContextMenu("Reload Palette Resources")]
private void ReloadPaletteResources()
{
    AssignPaletteResources();
    _objectPalette?.RefreshPalette();
}
#endif
```

### 4. Resource Caching
```csharp
private static readonly Dictionary<string, FieldInfo> _fieldCache = new();

private FieldInfo GetCachedField(Type type, string fieldName)
{
    string key = $"{type.FullName}.{fieldName}";
    
    if (!_fieldCache.TryGetValue(key, out FieldInfo field))
    {
        field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        _fieldCache[key] = field;
    }
    
    return field;
}
```

## Troubleshooting

### Resources Not Applied
**Symptoms**: Palette appears empty or unstyled
**Solutions**:
1. Check inspector - are resources assigned?
2. Check console - do assignment logs appear?
3. Verify field names match in ObjectPaletteUIToolkit
4. Call `RefreshPalette()` manually after assignment

### Reflection Errors
**Symptoms**: Exception about missing fields
**Solutions**:
1. Verify ObjectPaletteUIToolkit has matching field names
2. Check field access modifiers (should be private with SerializeField)
3. Update field names in AssignPaletteResources if they changed

### Resources Not Updating
**Symptoms**: Changed resources in inspector but palette unchanged
**Solutions**:
1. Ensure `AssignPaletteResources()` called in SetupPaletteIntegration
2. Call `RefreshPalette()` after resource changes
3. Check if palette was already initialized before assignment

## Related Files
- `SandboxBuilderUIToolkit.cs` - Main UI controller with resource management
- `ObjectPaletteUIToolkit.cs` - Palette component receiving resources
- `SceneObjectLibrary.cs` - Object data source
- `ObjectPaletteUIToolkit.uxml` - Palette template
- `ObjectPaletteItem.uxml` - Item template (if exists)
- `ObjectPalette.uss` - Palette stylesheet (if exists)
