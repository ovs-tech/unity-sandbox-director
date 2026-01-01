# Project Context

## Purpose
**Ero Director (Mobile & AR Sandbox Edition)** is an adult sandbox simulation game where players act as directors, creating erotic/romantic scenarios with actors, objects, and AR/VR immersion. The project combines creative freedom with cinematic tools, allowing users to build environments, direct actors, set cameras, record scenes, and share their creations.

### Core Goals:
- Provide a mobile-first, intuitive sandbox for creating cinematic scenes
- Enable players to build, shoot, and share their own stories without filmmaking skills
- Support modular DLC architecture for AR/VR extensions and additional content
- Maintain 45-60 FPS performance on mid-range mobile devices
- Offer a lightweight, extensible timeline system for in-game scene creation

## Tech Stack
- **Engine:** Unity 6000.2.6f2 (C#)
- **Rendering:** Universal Render Pipeline (URP)
- **Character System:** UMA (Unity Multipurpose Avatar)
- **Input System:** Unity Input System (.inputactions)
- **Asset Management:** Addressables for efficient loading
- **Animation:** Unity Playables API for smooth blending
- **Serialization:** Odin Serializer for timeline data (supports polymorphic types, complex Unity objects); JSON format for human-readable projects
- **Build Target:** Mobile-first (Android/iOS), extendable to PC, VR, AR via DLC
- **Architecture:** Modular & DLC-ready (ScriptableObjects or Addressables)
- **MCP tools:** Using serena mcp tools

## Project Conventions

### Code Style
- **Language:** C# (Unity)
- **Classes:** `PascalCase`
- **Private Fields:** `_camelCase` (with underscore prefix)
- **Public Properties:** `PascalCase`
- **Methods:** `PascalCase`
- **Constants:** `UPPER_SNAKE_CASE`
- **Interfaces:** `IPascalCase` (prefix with I)

**Unity-Specific Conventions:**
- Serialize fields with `[SerializeField]` instead of making them public
- Use `[Header("Section Name")]` for Inspector organization
- Add `[Tooltip("Description")]` for all serialized fields
- Prefer `readonly` for fields initialized once
- Use `#region` for organizing large classes

### Architecture Patterns

#### 1. Command Pattern
- Applied for editor + gameplay actions (undo/redo support)
- All user actions (place, move, rotate, delete) are commands
- Enables binding, track operations, and state management
- Example: `ICommand`, `CommandInvoker`, `CommandHistory`

#### 2. Dependency Injection
- Prefer Zenject or custom DI for extensibility
- Avoid hardcoding dependencies
- Use interfaces for loose coupling
- Example: `IInputHandler`, `IDLCModule`

#### 3. MVVM (Model-View-ViewModel) Pattern

**Core Principles:**
- Separate UI logic from game logic for testability and maintainability
- Use Unity UI Toolkit data binding for reactive UI updates
- ViewModel acts as the bridge between Model and View

**Architecture Layers:**

**Model:**
- Contains business logic and data
- Independent of UI framework
- Raises events when data changes
- Example: `InventoryModel` with `ObservableArray<Item>` and `OnModelChanged` event

**View:**
- Unity UI Toolkit components (VisualElement hierarchy)
- References ViewModel via data binding
- Listens to ViewModel property changes
- Example: `InventoryView` with UI Toolkit bindings

**ViewModel:**
- Exposes data-bindable properties using `BindableProperty<T>`
- Provides UI-friendly data transformations (e.g., int → string)
- Contains no Unity-specific dependencies
- Example: `ViewModel` class with `Capacity` and `Coins` properties

**Controller/Mediator:**
- Coordinates between Model and View
- Handles user input and business logic
- Creates and initializes ViewModel
- Example: `InventoryController` manages initialization and event handling

**Implementation Guidelines:**

1. **Use BindableProperty<T> for Reactive Properties:**
   ```csharp
   public class ViewModel {
       public readonly BindableProperty<string> Coins;
       
       public ViewModel(Model model) {
           Coins = BindableProperty<string>.Bind(() => model.Coins.ToString());
       }
   }
   ```

2. **Unity UI Toolkit Data Binding:**
   ```csharp
   label.dataSource = viewModel.Coins;
   label.SetBinding(nameof(Label.text), new DataBinding {
       dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
       bindingMode = BindingMode.ToTarget
   });
   ```

3. **Model Event Propagation:**
   ```csharp
   public class Model {
       ObservableArray<Item> Items { get; }
       
       public event Action<Item[]> OnModelChanged {
           add => Items.AnyValueChanged += value;
           remove => Items.AnyValueChanged -= value;
       }
   }
   ```

4. **Controller Initialization:**
   ```csharp
   IEnumerator Initialize() {
       yield return view.InitializeView(new ViewModel(model, capacity));
       view.OnDrop += HandleDrop;
       model.OnModelChanged += HandleModelChanged;
       RefreshView();
   }
   ```

**When to Use MVVM:**
- Complex UI with dynamic data (Inventory, Timeline Editor, Actor Inspector)
- UI that needs to react to model changes automatically
- Systems requiring unit testing of UI logic without Unity Editor
- Cross-platform UI where logic can be shared

**When NOT to Use MVVM:**
- Simple static UI (menus, buttons with no state)
- Performance-critical real-time updates (use direct updates instead)
- One-time data displays without reactive updates

**Best Practices:**
- Keep ViewModel pure C# (no UnityEngine dependencies)
- Use `readonly` for ViewModel properties that don't change
- Prefer `BindingMode.ToTarget` (one-way) unless two-way binding is needed
- Initialize View with ViewModel via coroutine to ensure UI elements exist
- Use Builder pattern for complex Controller initialization
- Refresh View after Model changes to keep UI in sync

#### 4. Track-Based Timeline System
- Modular track architecture: `IMiniTrack`, `IMiniClip`
- Each track can bind to actors, objects, or cameras
- JSON-based serialization for save/load
- Extensible through `TrackFactory` registration

#### 5. ScriptableObjects for Configuration
- Use for actor configs, scene data, DLC metadata
- Prefer data-driven design over hardcoded values
- Enables hot-reload and runtime modifications

#### 6. State Machine
- Sandbox modes: Build Mode vs Play Mode
- Transform modes: Position, Rotation, Scale
- Input state management through enums and flags

### Testing Strategy

#### Unit Testing
- Test core systems independently (Timeline, Serialization, Command Pattern)
- Mock Unity components where possible
- Focus on business logic and data transformations

#### Integration Testing
- Test track evaluation with actual GameObjects
- Verify Addressables loading and caching
- Timeline playback and scrubbing accuracy

#### Performance Testing
- Target: 45-60 FPS with 6-8 active tracks and ~20 clips
- Profile GC allocations (zero per-frame allocation goal)
- Memory usage monitoring for Addressables
- Mobile device testing on mid-range Android hardware

#### Manual Testing
- Scene building and object placement
- Timeline editing and playback
- Input system responsiveness
- Mode switching (Build/Play)
- Transform axis toggling

### Git Workflow

#### Branching Strategy
- **main:** Production-ready code
- **develop:** Integration branch for features
- **feature/*:** New features (e.g., `feature/integrate-scene-builder-and-timeline`)
- **bugfix/*:** Bug fixes
- **hotfix/*:** Critical production fixes

#### Commit Conventions
Follow Conventional Commits format:
- `feat:` New features
- `fix:` Bug fixes
- `refactor:` Code refactoring
- `docs:` Documentation updates
- `test:` Test additions or modifications
- `chore:` Build process or tool changes
- `perf:` Performance improvements

Example: `feat: add transform axis toggle for rotation and scale modes`

## Domain Context

### Filmmaking & Cinematography
- **Director Role:** Players control all aspects of scene creation
- **Shot Composition:** Camera angles, framing, movement paths
- **Lighting:** Intensity, color, range, spot angles, volumetric effects
- **Timeline Editing:** Frame-based editing with scrubbing support

### Game Sandbox Mechanics
- **Build Mode:** Full editing capabilities (place, select, transform objects)
- **Play Mode:** Read-only preview, performance-optimized
- **Transform Modes:** Position, Rotation, Scale with axis control (X, Y, Z, All)
- **Asset Library:** Props, actors, lights, cameras organized by categories

### Animation & Character Control
- **UMA Integration:** Dynamic character generation and customization
- **Animation Tracks:** AnimationClip playback with crossfading
- **Morph Tracks:** Blendshape control for facial expressions and body morphs
- **IK Tracks:** Inverse kinematics for limb and eye control
- **Pose System:** Preset poses and custom pose creation

### Adult Content Considerations
- **Content Rating:** Adult-oriented game with appropriate warnings
- **Moderation:** Community guidelines for shared content
- **Privacy:** User-generated content export and sharing controls

## Important Constraints

### Performance Constraints
- **Mobile Optimization:** Must run at 45-60 FPS on mid-range Android devices
- **Memory Budget:** Efficient asset loading via Addressables
- **GC Allocation:** Zero per-frame allocation in critical paths
- **Track Capacity:** Support 6-8 active tracks with ~20 clips simultaneously

### Technical Constraints
- **Unity Version:** 6000.2.6f2 (Unity 6)
- **URP Requirement:** Universal Render Pipeline for rendering
- **Input System:** New Unity Input System (no legacy input)
- **Platform Support:** Mobile-first, PC/VR/AR as DLC

### Content Constraints
- **Asset References:** All assets via Addressables (`addr:`) or scene references (`scene:`)
- **JSON Serialization:** Project files must be lightweight and human-readable
- **Version Compatibility:** Forward and backward compatibility for save files

### User Experience Constraints
- **Touch-Friendly UI:** Designed for mobile touch input (tap, drag, long-press, pinch)
- **Intuitive Controls:** As simple as The Sims, as powerful as Unreal Sequencer
- **Real-time Feedback:** Immediate visual response to all user actions
- **Undo/Redo Support:** All destructive actions must be reversible

## External Dependencies

### Unity Packages
- **Unity Input System** (com.unity.inputsystem): New input handling
- **Universal Render Pipeline** (com.unity.render-pipelines.universal): Rendering
- **Addressables** (com.unity.addressables): Asset management and loading
- **Timeline** (com.unity.timeline): Reference for timeline architecture
- **Animation Rigging** (com.unity.animation.rigging): IK system
- **Cinemachine** (com.unity.cinemachine): Advanced camera control
- **TextMesh Pro** (com.unity.textmeshpro): UI text rendering
- **UI Toolkit** (com.unity.ui): Modern UI framework

### Third-Party Assets
- **UMA (Unity Multipurpose Avatar):** Character generation and customization
- **Odin Serializer:** Advanced serialization supporting polymorphic types, Unity objects, and direct runtime instance serialization
- **LeanTween:** Lightweight tweening library for animations

### External Services (Future)
- **Cloud Sync:** Firebase or Supabase for project backup (planned)
- **Workshop Marketplace:** User-generated content sharing (planned)
- **Analytics:** Player behavior and performance monitoring (planned)

### Build Tools
- **Unity Hub:** Version management
- **Unity Build Profiles:** Platform-specific build configurations
- **Addressables Build System:** Asset bundling and optimization

### Development Tools
- **Visual Studio Code / Rider:** Primary IDE
- **Git:** Version control
- **OpenSpec:** Change proposal and specification system
