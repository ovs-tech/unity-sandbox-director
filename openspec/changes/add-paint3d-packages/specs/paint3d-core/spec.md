# Spec: Paint3D Core

## ADDED Requirements

### Requirement: In-Game and In-Editor Painting
The system SHALL let users paint `Paintable` objects at runtime with mouse, pen or touch (`PointerPainter`). It SHALL also let them paint in the Unity Scene view in edit or play mode (`Tools ▸ Paint3D ▸ Painter`), and export editor painting to PNG assets.

#### Scenario: Runtime pointer painting
- **WHEN** the user presses and drags over a paintable mesh in play mode with a `PointerPainter` in the scene
- **THEN** paint is applied along the dragged path using the brush color, radius, hardness and opacity

#### Scenario: Editor painting and export
- **WHEN** the user starts painting in the Paint3D window, paints in the Scene view, and clicks "Export PNGs"
- **THEN** each painted texture slot is written as a PNG asset in the export folder

### Requirement: Skinned Mesh Painting
The system SHALL paint `SkinnedMeshRenderer` objects in their current animated pose.

#### Scenario: Painting an animated character
- **WHEN** a dab hits a posed skinned character
- **THEN** paint appears at the hit location on the posed surface and stays attached to it as the character animates

### Requirement: Seamless UV Painting
The system SHALL hide UV seams by bleeding painted texels across UV island borders when `Paintable.Seamless` is enabled.

#### Scenario: Stroke across a UV seam
- **WHEN** a stroke crosses a UV seam
- **THEN** no unpainted line is visible along the seam

### Requirement: Smart Mirroring
The system SHALL mirror dabs across any combination of local X/Y/Z axes. It SHALL use each paintable's own space by default, or a shared pivot when one is set.

#### Scenario: X mirroring on a rotated model
- **WHEN** X mirroring is enabled and the user paints the left side of a rotated symmetric model
- **THEN** the same paint appears at the mirrored point on the right side in the model's local space

### Requirement: Undo and Redo
The system SHALL record each stroke as one undo step, and SHALL support undo and redo up to a configurable number of steps.

#### Scenario: Undo a stroke
- **WHEN** the user paints a stroke and presses Ctrl/Cmd+Z
- **THEN** every texture touched by that stroke returns to its state before the stroke, and Ctrl/Cmd+Y restores the stroke

### Requirement: Automatic Save and Load
The system SHALL persist paint as PNG through a replaceable storage backend. In Automatic mode it SHALL load on activation and save on disable, pause, quit or a configured interval.

#### Scenario: Paint survives a restart
- **WHEN** a paintable in Automatic mode is painted and the application is restarted
- **THEN** the previous paint is restored when the paintable activates

### Requirement: GPU Accelerated Painting
The system SHALL perform painting on the GPU using only vertex and fragment shaders (no compute shaders and no CPU texel loops).

#### Scenario: Large texture performance
- **WHEN** painting on a 2048×2048 texture
- **THEN** each dab costs a constant number of draw calls, whatever the texture resolution

### Requirement: Brush Modifiers
The brush SHALL support pressure-driven radius and opacity (with a response curve), radius, opacity and hue jitter, stroke spacing, and gradual (time-based) flow.

#### Scenario: Pen pressure
- **WHEN** pressure affects opacity and the user paints with half pressure
- **THEN** the dab opacity is the brush opacity scaled by the pressure curve evaluated at 0.5

### Requirement: Paint Any Shader
The system SHALL paint any texture property of any material index on a renderer. It SHALL support several slots per paintable, filtered by brush group.

#### Scenario: Multi-texture painting
- **WHEN** a paintable has slots for `_BaseMap` in group 0 and `_EmissionMap` in group 1, and the brush group is 1
- **THEN** only the emission texture is painted

### Requirement: Live Painting
The system SHALL show paint immediately on the rendered object without changing shared materials. It SHALL interpolate strokes so fast pointer movement leaves continuous lines.

#### Scenario: Fast stroke
- **WHEN** the pointer moves several brush radii in one frame
- **THEN** intermediate dabs are placed at the brush spacing along the path

### Requirement: Through Painting and Gradual Painting
The brush SHALL be able to paint every surface inside its volume regardless of facing (through painting). It SHALL also be able to build up opacity over time while held (gradual painting).

#### Scenario: Through painting
- **WHEN** paint-through is enabled and the brush sphere contains both sides of a thin surface
- **THEN** both sides receive paint

#### Scenario: Gradual painting
- **WHEN** gradual painting is enabled and the pointer is held still
- **THEN** paint opacity at that point increases every frame in proportion to the flow rate

### Requirement: Cross-Platform Support
The system SHALL run on desktop, mobile (GLES3/Vulkan/Metal), WebGL2 and XR, with the same visual result and the same API.

#### Scenario: WebGL build
- **WHEN** the project is built for WebGL
- **THEN** painting, undo/redo and save/load work without platform-specific code paths in user scripts
