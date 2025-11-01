<!--
═══════════════════════════════════════════════════════════════════════════
SYNC IMPACT REPORT
═══════════════════════════════════════════════════════════════════════════
Version Change: N/A → 1.0.0 (Initial Constitution)

Constitution Created: 2025-10-30
- First ratification of Ero Director project constitution
- Established 7 core design & technical principles
- Defined Unity-specific development standards
- Set mobile-first performance & quality gates

Modified Principles: N/A (initial version)

Added Sections:
  ✓ Project Overview & Vision
  ✓ Core Design Principles (7 principles)
  ✓ Technical Foundation
  ✓ Development Standards
  ✓ Workflow & Pipeline
  ✓ Quality Assurance & Performance
  ✓ Governance

Template Dependencies:
  ⚠ .specify/templates/plan-template.md - Review "Constitution Check" section
  ⚠ .specify/templates/spec-template.md - Ensure alignment with modularity principles
  ⚠ .specify/templates/tasks-template.md - Verify task categorization reflects principles
  ⚠ .github/copilot-instructions.md - Already aligned, no changes needed

Follow-up TODOs: None - all placeholders filled
═══════════════════════════════════════════════════════════════════════════
-->

# Ero Director (Unity Sandbox) — Project Constitution

## 1. Project Overview

**Project Name**: Ero Director — Mobile & AR Sandbox Edition  
**Genre**: Adult sandbox simulation / Creative director tool  
**Platforms**: Mobile-first (Android, iOS), extensible to PC, VR, AR via DLC  
**Engine**: Unity 6000.2.6f2 (Unity 6), Universal Render Pipeline (URP)  
**Core Vision**: Empower players to become virtual directors—building, directing, and recording cinematic scenes with full creative control over actors, cameras, environments, and storytelling.

**Emotional Identity**: Creative freedom, intuitive control, shareability, and immersive sandbox simulation.

---

## 2. Core Design Principles

### I. Modular Architecture (NON-NEGOTIABLE)

**Rule**: Every system MUST be designed as a modular, independently testable component using interfaces, ScriptableObjects, or Addressables-based packages.

**Rationale**: The project is DLC-ready and multi-platform. Modularity ensures features can be added, tested, and deployed independently without breaking existing systems. This is critical for AR/VR extensions, asset packs, and input method variations.

**Requirements**:
- All gameplay systems expose interfaces (e.g., `ITrack`, `IInputHandler`, `IDLCModule`)
- Asset references use Addressables—never direct scene references
- DLC modules are self-contained ScriptableObject-based packages
- Each module has clear entry/exit contracts

### II. Mobile-First Performance (NON-NEGOTIABLE)

**Rule**: All features MUST target **45–60 FPS** on mid-range Android devices (equivalent to Snapdragon 730 or better). Performance degradation below 30 FPS is a **blocking issue**.

**Rationale**: The game is mobile-first. Poor performance destroys the creative flow and user experience. Optimization is not a "phase 2" task—it is baked into every feature from day one.

**Requirements**:
- No per-frame allocations (zero GC pressure)
- Object pooling for runtime-instantiated elements
- Addressables with async loading; show loading screens for heavy assets
- Profile every feature before merging: CPU, GPU, memory
- Timeline must handle 6–8 active tracks with 20+ clips at 60 FPS

### III. Command Pattern for Editor Actions (NON-NEGOTIABLE)

**Rule**: All timeline editor actions (add/remove clips, move tracks, scrub, bind actors) MUST be implemented using the **Command Pattern** to support undo/redo and action replay.

**Rationale**: Creative tools require reliable undo/redo. Players must feel confident experimenting. Command Pattern ensures every action is reversible, serializable, and debuggable.

**Requirements**:
- Each editor action is an `ICommand` with `Execute()` and `Undo()`
- Command history stack maintains session state
- Commands are JSON-serializable for replay/debugging
- UI buttons invoke commands, never mutate state directly

### IV. JSON-Based Timeline Serialization

**Rule**: All timeline projects MUST serialize to lightweight, versioned JSON files. Binary formats are prohibited. Asset references MUST use Addressables keys, not scene paths.

**Rationale**: JSON ensures portability, debuggability, and version migration. Players can share projects across devices. Developers can inspect and edit saved files for debugging.

**Requirements**:
- Timeline data format: `MiniTimelineProject` with version field (e.g., `"version": "1.0.0"`)
- Track/Clip data uses dictionaries for extensibility
- Asset references as strings: `"addr:Animations/Walk"`
- Migration scripts for version upgrades

### V. Unity Input System with Action Maps (NON-NEGOTIABLE)

**Rule**: All input MUST use Unity's Input System (.inputactions) with separate action maps for UI, Gameplay, and Timeline Editor contexts. No legacy Input Manager.

**Rationale**: Multi-platform support (mobile touch, PC mouse/keyboard, VR controllers) requires a unified input abstraction. Action maps allow seamless context switching without input conflicts.

**Requirements**:
- Action maps: `UIAndGameplayInputActions.inputactions`
- Separate handlers: `UIInputHandler`, `GameplayInputHandler`, `TimelineInputHandler`
- Gesture support: tap, drag, long-press, pinch-zoom (mobile)
- Rebinding UI for PC/VR controls

### VI. UMA Integration for Character System

**Rule**: All character generation and customization MUST use the **UMA (Unity Multipurpose Avatar)** system. Custom mesh generators are prohibited.

**Rationale**: UMA provides a proven, modular character pipeline with runtime morphing, clothing, and LOD support. Reinventing this wheel risks instability and poor performance.

**Requirements**:
- Characters instantiated via UMA APIs
- Morphs/expressions via UMA blendshapes
- Clothing/accessories as UMA recipes
- Performance: character generation <2 seconds, morph updates <5ms/frame

### VII. Editor Tooling First

**Rule**: Internal tools (timeline editor, scene builder, asset inspector) MUST receive the same engineering rigor as runtime gameplay. Editor UI uses Unity's UIToolkit or IMGUI with custom inspectors.

**Rationale**: The game IS a creative tool. If the editor is clunky, the game fails. Editor quality directly impacts player experience.

**Requirements**:
- Custom inspectors for all ScriptableObjects
- Timeline editor window with ruler, zoom, scrubbing, track visualization
- Real-time preview during editing
- Automated editor tests for critical workflows

---

## 3. Technical Foundation

**Unity Version**: Unity 6000.2.6f2 (Unity 6)  
**Render Pipeline**: Universal Render Pipeline (URP)  
**Target Platforms**: Android (primary), iOS, PC (Windows/macOS), VR/AR (DLC)  
**Character System**: UMA (Unity Multipurpose Avatar)  
**Input System**: Unity Input System (Action Maps)  
**Asset Management**: Addressables (remote + local catalogs)  
**Serialization**: JSON (Newtonsoft.Json or Unity's JsonUtility)  
**Animation**: Unity Playables API + Animator Controller  
**Version Control**: Git with LFS (large assets), GitFlow branching model

---

## 4. Development Standards

### Code Conventions (C#)

- **Classes**: `PascalCase` (e.g., `MiniTimelineDirector`)
- **Private fields**: `_camelCase` (e.g., `_currentTime`)
- **Public properties**: `PascalCase` (e.g., `CurrentTime`)
- **Interfaces**: `IPascalCase` (e.g., `ITrack`, `IInputHandler`)
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `TRACK_TYPE_ANIM`)
- **Namespaces**: Match folder structure (e.g., `MiniTimeline.Core`, `MiniTimeline.Tracks`)

### Dependency Injection

- Prefer constructor injection or property injection for testability
- Use Zenject (optional) or manual DI containers
- Avoid `GameObject.Find()` or `GetComponent()` in loops

### Async/Await

- All asset loading via Addressables MUST use `async/await`
- Show loading UI during async operations
- Handle cancellation tokens for scene transitions

### Comments & Documentation

- XML comments for all public APIs
- README.md in each major subsystem folder
- Architecture Decision Records (ADRs) for major design choices

---

## 5. Workflow & Pipeline

### Branching Strategy (GitFlow)

- **main**: Production-ready releases
- **develop**: Integration branch for features
- **feature/[name]**: Individual feature branches (e.g., `feature/timeline-camera-track`)
- **hotfix/[name]**: Emergency fixes for production issues

### Naming Conventions

- **Scenes**: `Scene_[Context]` (e.g., `Scene_TimelineEditor`, `Scene_CharacterCustomization`)
- **Prefabs**: `Prefab_[Type]_[Name]` (e.g., `Prefab_UI_TimelinePanel`, `Prefab_Character_Male`)
- **ScriptableObjects**: `SO_[Type]_[Name]` (e.g., `SO_Config_Timeline`, `SO_DLC_ARModule`)
- **Addressables**: Group by type and source (e.g., `Animations/Walk`, `Characters/Female/Casual`)

### Pull Request Requirements

- Code review required (minimum 1 approval)
- All tests passing (if tests exist)
- Performance profiling for runtime features (attach screenshots)
- Constitution compliance check (reference relevant principles)

### Asset Management

- **Addressables Groups**: Base, DLC_AR, DLC_VR, DLC_Assets
- **Remote hosting**: AWS S3 or Unity Cloud Content Delivery
- **Versioning**: Catalog versions match app version (e.g., `1.2.0`)

---

## 6. Quality Assurance & Performance

### Performance Targets (NON-NEGOTIABLE)

- **Frame Rate**: 45–60 FPS on Snapdragon 730-equivalent devices
- **Memory**: <1.5 GB peak usage on mobile
- **Loading Times**: <3 seconds for scene transitions, <2 seconds for character generation
- **Battery**: <15% drain per 30 minutes of active use

### Optimization Guidelines

- **Batching**: Use SRP Batcher, GPU Instancing
- **LOD**: Characters must have 3 LOD levels (high/medium/low)
- **Texture Compression**: ASTC (Android), PVRTC (iOS)
- **Audio**: MP3 for music, Vorbis for SFX, streaming for long tracks
- **Garbage Collection**: Zero allocations in Update/FixedUpdate loops

### Testing Standards (OPTIONAL, applied when tests are explicitly requested)

- **Unit Tests**: Critical logic (serialization, commands, timeline math)
- **Integration Tests**: Track evaluation, actor binding, asset loading
- **Performance Tests**: Automated profiling in CI (flag regressions >10%)
- **Manual QA**: Weekly playtests on target devices

---

## 7. Change Management (Governance)

### Constitution Authority

This constitution supersedes all other development practices. When conflicts arise, constitution principles take precedence. All team members and AI coding assistants (e.g., GitHub Copilot) MUST comply.

### Amendment Process

1. **Proposal**: Document proposed change with rationale
2. **Review**: Technical lead + team discussion (minimum 3 days)
3. **Approval**: Consensus or technical lead decision
4. **Migration**: Update affected code, templates, and documentation
5. **Version Bump**: Increment constitution version (see below)

### Versioning Policy

Constitution follows **semantic versioning**:
- **MAJOR**: Backward-incompatible changes (e.g., removing a principle, changing architecture)
- **MINOR**: New principles or expanded guidance (e.g., adding a new quality gate)
- **PATCH**: Clarifications, typos, non-semantic refinements

### Compliance Verification

- All PRs MUST include a "Constitution Compliance" checklist
- Code reviews verify adherence to principles (modularity, performance, command pattern)
- Quarterly architecture reviews audit codebase for drift
- Constitution violations require explicit justification and lead approval

### Runtime Guidance

For AI assistants and developers, runtime development guidance is provided in:
- `.github/copilot-instructions.md` (coding style, task patterns)
- `.specify/templates/*.md` (planning, specs, tasks workflows)

---

## 8. Closing Statement

This constitution defines the **non-negotiable foundation** of the Ero Director project. It balances creative ambition with technical discipline, ensuring the game is performant, extensible, and maintainable.

**Every feature, every commit, every design decision must honor these principles.**

When in doubt, refer to the constitution. When the constitution is unclear, propose an amendment.

---

**Version**: 1.0.0 | **Ratified**: 2025-10-30 | **Last Amended**: 2025-10-30
