# UI Toolkit Resources Setup Guide

To use the TrackUIToolkit and ClipUIToolkit with their UXML and USS files, you need to place the files in the correct Resources folder structure.

## Folder Structure

Create the following folder structure in your project:

```
Assets/
├── Resources/
│   └── UI/
│       ├── TrackUIToolkit.uxml
│       ├── TrackUIToolkit.uss
│       ├── ClipUIToolkit.uxml
│       └── ClipUIToolkit.uss
└── Scripts/MiniTimeline/UI/
    ├── TrackUIToolkit.uxml
    ├── TrackUIToolkit.uss
    ├── ClipUIToolkit.uxml
    └── ClipUIToolkit.uss
```

## Setup Steps

1. **Create Resources Folder**:
   - Right-click in your Assets folder
   - Create → Folder → Name it "Resources"

2. **Create UI Subfolder**:
   - Right-click in Resources folder
   - Create → Folder → Name it "UI"

3. **Copy UXML/USS Files**:
   - Copy the TrackUIToolkit.uxml and TrackUIToolkit.uss files to `Assets/Resources/UI/`
   - Copy the ClipUIToolkit.uxml and ClipUIToolkit.uss files to `Assets/Resources/UI/`

4. **Verify Resource Loading**:
   The TrackUIToolkit and ClipUIToolkit classes will automatically load these files using:
   ```csharp
   trackTemplate = Resources.Load<VisualTreeAsset>("UI/TrackUIToolkit");
   trackStyleSheet = Resources.Load<StyleSheet>("UI/TrackUIToolkit");
   ```

## Alternative Setup (Manual Assignment)

If you prefer not to use Resources loading, you can:

1. Create public fields in TimelineEditorUIToolkit:
   ```csharp
   [Header("UI Templates")]
   [SerializeField] private VisualTreeAsset trackTemplate;
   [SerializeField] private StyleSheet trackStyleSheet;
   [SerializeField] private VisualTreeAsset clipTemplate;
   [SerializeField] private StyleSheet clipStyleSheet;
   ```

2. Assign the UXML/USS files directly in the inspector

3. Pass these templates to the TrackUIToolkit and ClipUIToolkit classes during initialization

## Usage

Once properly set up, the timeline editor will automatically:

- Load track and clip templates from UXML files
- Apply styling from USS files
- Create rich, interactive UI elements for tracks and clips
- Support all the features defined in the USS (hover states, selection, drag/drop, etc.)

## Troubleshooting

- **Templates not loading**: Check that files are in `Assets/Resources/UI/` folder
- **Styling not applied**: Ensure USS files have the correct naming
- **Console errors**: Check that UXML structure matches the expected element names
- **Missing elements**: Verify that QueryClipElements() and QueryTrackElements() can find all required elements

## Benefits

Using UXML/USS provides:

- **Visual Design**: Easy to modify appearance without code changes
- **Responsive Layout**: Flexbox-based layout adapts to different sizes
- **Professional Look**: Rich styling with hover effects, animations, and states
- **Maintainability**: Separation of structure (UXML), style (USS), and logic (C#)
- **Performance**: GPU-accelerated rendering and efficient layout updates